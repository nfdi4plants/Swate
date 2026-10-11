module ElectronCore.CommittedDiffAdapterTests

open System
open Fable.Core
open VersionControlService.Abstractions
open VersionControlService.Git.TextDiff.TextDiffProtocol
open VersionControlService.Git.TextDiff.TextDiffTransport
open Vitest
open ElectronCore.TestHelpers

module Sources = Main.VersionControl.CommittedDiffSources
module Pool = VersionControlService.Git.TextDiff.TextDiffPool
module Supervisor = VersionControlService.Git.TextDiff.TextDiffSupervisor
module Transport = VersionControlService.Git.TextDiff.TextDiffInProcessTransport.InProcessTransport

[<Emit("process.cwd()")>]
let private workingDirectory () : string = jsNative

let private git root args =
    execFile "git" (Array.append [| "--no-optional-locks"; "--no-replace-objects" |] args) root

let private repositoryRoot () =
    git (workingDirectory ()) [| "rev-parse"; "--show-toplevel" |] |> _.Trim()

let private nonemptyLines (text: string) =
    text.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)

let private path value =
    RepositoryPath.tryCreate value |> Result.defaultWith failwith

let private revision value =
    RevisionId.tryCreate value |> Result.defaultWith failwith

let private context () = detached "committed-diff-integration"

[<Import("writeFileSync", "node:fs")>]
let private writeBytes (file: string) (content: byte[]) : unit = jsNative

type private TransportHooks = {
    ToWorker: obj -> (obj -> unit) -> unit
    FromWorker: obj -> (obj -> unit) -> (obj -> unit) -> unit
}

let private unchangedTransport = {
    ToWorker = fun message forward -> forward message
    FromWorker = fun message forward _ -> forward message
}

let private withPoolHooks root hooks body = promise {
    let! temp = createTempDirectoryAsync "swate-committed-diff-"

    let! supervisor =
        Supervisor.create {
            TempRoot = temp
            GitExecutable = None
            OnEvent = None
        }

    let factory _ =
        let adapter = Main.Workers.CommittedDiffAdapter.Adapter()
        let raw = Transport.create adapter.Handler

        { new ITextDiffWorkerTransport with
            member _.Post message = hooks.ToWorker message raw.Post

            member _.OnMessage callback =
                raw.OnMessage(fun message ->
                    hooks.FromWorker message (fun message -> adapter.Post(message, callback, raw.Post)) raw.Post
                )

            member _.OnError callback = raw.OnError callback
            member _.OnExit callback = raw.OnExit callback
            member _.Terminate() = raw.Terminate()
        }

    let options = Pool.TextDiffPoolOptions.create factory supervisor
    let pool = Pool.create { options with MaxWorkers = 1 }

    let owner: TextDiffOwner = {
        WorkspaceRoot = root
        LfsMediaDirectory = Main.Bindings.Path.resolve [| root; ".git"; "lfs"; "objects" |]
        WindowOwner = "committed-diff-test-window"
    }

    try
        do! body (pool.Service owner)
        do! pool.Dispose()
        do! removeDirectoryAsync temp
    with error ->
        do! pool.Dispose()
        do! removeDirectoryAsync temp
        return raise error
}

let private withPool root body =
    withPoolHooks root unchangedTransport body

let private request selection previous storage : OpenDiffRequest = {
    Path = path (Sources.encode selection)
    PreviousPath = Some(path previous)
    Preparation = None
    PreviousEncoding = None
    CurrentEncoding = None
    ContextLines = 3
    Continuation = None
    Storage = storage
}

let rec private openReady (service: TextDiffService) request attempts = promise {
    if attempts = 0 then
        failwith "The committed diff did not finish preparing."

    let! result = service.Open request (context ()) |> Async.StartAsPromise

    match expectValue "open committed diff" result with
    | Resumable.Ready result -> return result
    | Resumable.Scanning(_, continuation, _) ->
        return!
            openReady
                service
                {
                    request with
                        Continuation = Some continuation
                }
                (attempts - 1)
}

let rec private readLine (service: TextDiffService) request attempts = promise {
    if attempts = 0 then
        failwith "The line did not finish scanning."

    let! result = service.ReadLine request (context ()) |> Async.StartAsPromise

    match expectValue "read committed line" result with
    | Resumable.Ready line -> return line
    | Resumable.Scanning(_, continuation, _) ->
        return!
            readLine
                service
                {
                    request with
                        Continuation = Some continuation
                }
                (attempts - 1)
}

let rec private pages (service: TextDiffService) handle next attempts collected = promise {
    if attempts = 0 then
        failwith "The committed diff did not finish paging."

    match next with
    | Resumable.Ready page ->
        let collected = Array.append collected [| page |]

        match page.NextCursor with
        | None -> return collected
        | Some cursor ->
            let! result =
                service.ReadPage
                    {
                        Handle = handle
                        Cursor = cursor
                        Background = false
                    }
                    (context ())
                |> Async.StartAsPromise

            return! pages service handle (expectValue "next committed page" result) (attempts - 1) collected
    | Resumable.Scanning(_, cursor, _) ->
        let! result =
            service.ReadPage
                {
                    Handle = handle
                    Cursor = cursor
                    Background = false
                }
                (context ())
            |> Async.StartAsPromise

        return! pages service handle (expectValue "scan committed page" result) (attempts - 1) collected
}

let private sourceLines (content: string) =
    let lines = content.Replace("\r\n", "\n").Split('\n')

    if content.EndsWith("\n") then
        lines[0 .. lines.Length - 2]
    else
        lines

let private assertLine (expected: string[]) (line: DiffLine) =
    Vitest.expect(line.Number >= 0L && line.Number < int64 expected.Length).toBe true
    let complete = expected[int line.Number]
    let offset = int line.Slice.OffsetUtf16

    Vitest
        .expect(line.Slice.Text)
        .toBe (complete.Substring(offset, min line.Slice.Text.Length (complete.Length - offset)))

let private visibleRows =
    function
    | DiffPart.Hunk hunk ->
        match hunk.Body with
        | HunkBody.AlignedRows rows -> rows
        | HunkBody.UnalignedSides(previous, current) ->
            Array.append
                (previous
                 |> Array.map (fun line -> {
                     Id = ""
                     Kind = DiffRowKind.Removed
                     Previous = Some line
                     Current = None
                 }))
                (current
                 |> Array.map (fun line -> {
                     Id = ""
                     Kind = DiffRowKind.Added
                     Previous = None
                     Current = Some line
                 }))
    | DiffPart.ExpandedContext(_, rows) -> rows
    | DiffPart.HiddenEqual _ -> [||]

let private verifyOpenedWithStorage
    root
    (service: TextDiffService)
    (selection: Sources.Selection)
    previousPath
    storagePolicy
    =
    promise {
        let currentAbsent =
            git root [| "ls-tree"; selection.Revision; "--"; selection.Path |] = ""

        let previousAbsent =
            selection.ParentRevision
            |> Option.forall (fun parent -> git root [| "ls-tree"; parent; "--"; previousPath |] = "")

        let currentText =
            if currentAbsent then
                ""
            else
                git root [| "show"; selection.Revision + ":" + selection.Path |]

        let previousText =
            selection.ParentRevision
            |> Option.filter (fun _ -> not previousAbsent)
            |> Option.map (fun parent -> git root [| "show"; parent + ":" + previousPath |])
            |> Option.defaultValue ""

        let previousLines, currentLines = sourceLines previousText, sourceLines currentText
        let! result = openReady service (request selection previousPath storagePolicy) 1000

        match result with
        | OpenDiffResult.NotDiffable blocker -> return failwith $"Expected text, got {blocker}."
        | OpenDiffResult.Opened(handle, previous, current, first, storage) ->
            Vitest.expect(RepositoryPath.value previous.Path).toBe previousPath
            Vitest.expect(RepositoryPath.value current.Path).toBe selection.Path
            Vitest.expect(previous.Revision).toEqual (selection.ParentRevision |> Option.map revision)
            Vitest.expect(current.Revision).toEqual (Some(revision selection.Revision))
            Vitest.expect(previous.IsAbsent).toBe previousAbsent
            Vitest.expect(current.IsAbsent).toBe currentAbsent
            Vitest.expect(previous.ByteLength).toBe (int64 (Text.Encoding.UTF8.GetBytes(previousText).Length))
            Vitest.expect(current.ByteLength).toBe (int64 (Text.Encoding.UTF8.GetBytes(currentText).Length))

            let expectedStorage =
                match storagePolicy with
                | DiffStoragePolicy.MemoryOnly budget -> DiffStorage.InMemory budget
                | DiffStoragePolicy.PreferDisk _ -> DiffStorage.OnDisk

            Vitest.expect(storage).toEqual expectedStorage

            let! allPages = pages service handle first 10000 [||]
            Vitest.expect(allPages.Length > 0).toBe true
            Vitest.expect((Array.last allPages).OutputComplete).toBe true
            let parts = allPages |> Array.collect _.Parts
            let rows = parts |> Array.collect visibleRows
            Vitest.expect(rows.Length > 0).toBe true

            for row in rows do
                row.Previous |> Option.iter (assertLine previousLines)
                row.Current |> Option.iter (assertLine currentLines)

            let! replay =
                service.ReplayPage
                    {
                        Handle = handle
                        PageId = allPages[0].PageId
                    }
                    (context ())
                |> Async.StartAsPromise

            Vitest.expect(expectValue "replay committed page" replay).toEqual allPages[0]

            for side, expected, absent in
                [|
                    DiffSide.Previous, previousLines, previousAbsent
                    DiffSide.Current, currentLines, currentAbsent
                |] do
                if expected.Length > 0 && not absent then
                    for number in [| 0; expected.Length - 1 |] |> Array.distinct do
                        let! line =
                            readLine
                                service
                                {
                                    Handle = handle
                                    Side = side
                                    Line = int64 number
                                    OffsetUtf16 = 0L
                                    MaxUtf16 = 8192
                                    Continuation = None
                                }
                                1000

                        assertLine expected line
                        Vitest.expect(line.Slice.Text).toBe expected[number]

            let gap =
                parts
                |> Array.tryPick (
                    function
                    | DiffPart.HiddenEqual gap -> Some gap
                    | _ -> None
                )

            match gap with
            | None -> ()
            | Some gap ->
                let! expansion =
                    service.Expand
                        {
                            Handle = handle
                            GapId = gap.GapId
                            FromStart = true
                            Count = 5
                            Continuation = None
                        }
                        (context ())
                    |> Async.StartAsPromise

                match expectValue "expand committed context" expansion with
                | Resumable.Ready expanded ->
                    let rows = expanded |> Array.collect visibleRows
                    Vitest.expect(rows.Length > 0 && rows.Length <= 5).toBe true

                    for row in rows do
                        row.Previous |> Option.iter (assertLine previousLines)
                        row.Current |> Option.iter (assertLine currentLines)
                | Resumable.Scanning _ -> failwith "Verified context unexpectedly needed further scanning."

            let! infos = service.GetSourceInfo { Handle = handle } (context ()) |> Async.StartAsPromise
            let previousInfo, currentInfo = expectValue "committed source metadata" infos
            Vitest.expect(previousInfo.Path).toEqual previous.Path
            Vitest.expect(currentInfo.Path).toEqual current.Path
            Vitest.expect(previousInfo.Revision).toEqual previous.Revision
            Vitest.expect(currentInfo.Revision).toEqual current.Revision
            let! closed = service.Close handle (context ()) |> Async.StartAsPromise
            expectValue "close committed diff" closed |> ignore
    }

let private verifyOpened root service selection previousPath =
    verifyOpenedWithStorage root service selection previousPath (DiffStoragePolicy.PreferDisk(16_777_216L, 67_108_864L))

Vitest.describe (
    "Committed sources in the shared text diff worker",
    fun () ->
        Vitest.test (
            "round trips pinned paths and rejects malformed source envelopes",
            fun () ->
                let selection: Sources.Selection = {
                    Revision = String.replicate 40 "a"
                    ParentRevision = Some(String.replicate 40 "b")
                    Path = "folder/Grüße % #.txt"
                }

                Vitest.expect(Sources.tryDecode (Sources.encode selection)).toEqual (Some selection)

                Vitest
                    .expect(Sources.tryDecode (Sources.encode { selection with ParentRevision = None }))
                    .toEqual (Some { selection with ParentRevision = None })

                for input in
                    [
                        "ordinary.txt"
                        "__swate_committed_diff__/invalid/root/a.txt"
                        Sources.encode {
                            selection with
                                Path = "../outside.txt"
                        }
                    ] do
                    Vitest.expect(Sources.tryDecode input).toEqual None
        )

        Vitest.test (
            "round trips literal workspace paths that resemble internal source envelopes",
            fun () ->
                let selection: Sources.Selection = {
                    Revision = String.replicate 40 "a"
                    ParentRevision = None
                    Path = "folder/file%.txt"
                }

                let reserved = [|
                    Sources.encode selection
                    "__swate_committed_diff__/literal%.txt"
                    "__swate_workspace_diff__/literal%"
                |]

                for literal in reserved do
                    let encoded = Sources.encodeWorkspacePath literal
                    Vitest.expect(encoded <> literal).toBe true
                    Vitest.expect(Sources.tryDecode encoded).toEqual None
                    Vitest.expect(Sources.decodeWorkspacePath encoded).toBe literal

                let ordinary = "folder/Grüße % #.txt"
                Vitest.expect(Sources.encodeWorkspacePath ordinary).toBe ordinary
                Vitest.expect(Sources.decodeWorkspacePath ordinary).toBe ordinary
        )

        Vitest.test (
            "reads two historical versions through paging, replay, line reads and context expansion without changing Git state",
            fun () -> promise {
                let root = repositoryRoot ()
                let before = git root [| "status"; "--porcelain=v1"; "-z" |]
                let head = git root [| "rev-parse"; "HEAD" |]
                let index = git root [| "rev-parse"; "--git-path"; "index" |] |> _.Trim()

                let! indexBefore =
                    Main.Bindings.Filesystem.readFileBase64Async (Main.Bindings.Path.resolve [| root; index |])

                let file = "src/Components/src/Page/GitComparison/GitPagedDiffViewer.fs"

                let commits =
                    git root [|
                        "log"
                        "--format=%H"
                        "--max-count=2"
                        "HEAD"
                        "--"
                        file
                    |]
                    |> nonemptyLines

                Vitest.expect(commits.Length).toBe 2

                do!
                    withPool
                        root
                        (fun service -> promise {
                            for commit in commits do
                                let parent = git root [| "rev-parse"; commit + "^" |] |> _.Trim()

                                do!
                                    verifyOpened
                                        root
                                        service
                                        {
                                            Revision = commit
                                            ParentRevision = Some parent
                                            Path = file
                                        }
                                        file
                        })

                let! indexAfter =
                    Main.Bindings.Filesystem.readFileBase64Async (Main.Bindings.Path.resolve [| root; index |])

                Vitest.expect(indexAfter).toEqual indexBefore
                Vitest.expect(git root [| "rev-parse"; "HEAD" |]).toBe head
                Vitest.expect(git root [| "status"; "--porcelain=v1"; "-z" |]).toBe before
            }
        )

        Vitest.test (
            "reads a root commit addition against an absent previous source",
            fun () -> promise {
                let root = repositoryRoot ()

                let commit =
                    git root [| "rev-list"; "--max-parents=0"; "--max-count=1"; "HEAD" |]
                    |> _.Trim()

                do!
                    withPool
                        root
                        (fun service ->
                            verifyOpened
                                root
                                service
                                {
                                    Revision = commit
                                    ParentRevision = None
                                    Path = "README.md"
                                }
                                "README.md"
                        )
            }
        )

        Vitest.test (
            "uses the original committed path for deleted and renamed files",
            fun () -> promise {
                let root = repositoryRoot ()

                do!
                    withPool
                        root
                        (fun service -> promise {
                            for filter in [| "D"; "R" |] do
                                let commit =
                                    git root [|
                                        "log"
                                        "--diff-filter=" + filter
                                        "--format=%H"
                                        "--max-count=1"
                                        "HEAD"
                                        "--"
                                        "*.fs"
                                    |]
                                    |> _.Trim()

                                let parent = git root [| "rev-parse"; commit + "^" |] |> _.Trim()

                                let fields =
                                    git root [|
                                        "diff-tree"
                                        "-r"
                                        "--no-commit-id"
                                        "--name-status"
                                        "-z"
                                        "-M"
                                        parent
                                        commit
                                        "--"
                                        "*.fs"
                                    |]
                                    |> fun output -> output.Split('\000')

                                let mutable index = 0
                                let mutable selected = None

                                while index < fields.Length && fields[index] <> "" do
                                    let status = fields[index]
                                    let renamed = status.StartsWith("R") || status.StartsWith("C")

                                    if selected.IsNone && status.StartsWith(filter) then
                                        let oldPath = fields[index + 1]
                                        let newPath = if renamed then fields[index + 2] else oldPath
                                        selected <- Some(oldPath, newPath)

                                    index <- index + (if renamed then 3 else 2)

                                let previous, current =
                                    selected
                                    |> Option.defaultWith (fun () ->
                                        failwith $"Expected a historical {filter} F# file fixture."
                                    )

                                do!
                                    verifyOpened
                                        root
                                        service
                                        {
                                            Revision = commit
                                            ParentRevision = Some parent
                                            Path = current
                                        }
                                        previous
                        })
            }
        )

        Vitest.test (
            "reuses the exact temporary source identity when retrying a chosen encoding",
            fun () -> promise {
                let root = repositoryRoot ()
                let commit = git root [| "rev-parse"; "HEAD" |] |> _.Trim()
                let parent = git root [| "rev-parse"; "HEAD^" |] |> _.Trim()
                let file = "package-lock.json"

                let size =
                    git root [| "cat-file"; "-s"; commit + ":" + file |] |> _.Trim() |> Int32.Parse

                Vitest.expect(size > 196_608).toBe true
                let bytes = Array.create size 97uy
                // This byte lies after the first 64 KiB classification sample and
                // before the middle sample. The first page discovers the mismatch.
                bytes[70_000] <- 233uy
                bytes[70_001] <- 10uy
                let mutable snapshotSpawns = 0
                let mutable pending: (int * string) option = None

                let hooks = {
                    ToWorker =
                        fun message forward ->
                            match decodeMessage message, pending with
                            | Ok(TextDiffMessage.SpawnResult(callId, SpawnOutcome.Blob exit)), Some(expected, spool) when
                                callId = expected && exit.ExitCode = Some 0
                                ->
                                // Script only the adapter-owned temporary spool. Git objects and
                                // the checkout are never altered by this encoding fixture.
                                writeBytes spool bytes
                                pending <- None
                            | _ -> ()

                            forward message
                    FromWorker =
                        fun message forward _ ->
                            match decodeMessage message with
                            | Ok(TextDiffMessage.SpawnBlob(callId, _, _, _, spool)) when
                                spool.Contains("history-spool-")
                                ->
                                snapshotSpawns <- snapshotSpawns + 1
                                pending <- Some(callId, spool)
                            | _ -> ()

                            forward message
                }

                do!
                    withPoolHooks
                        root
                        hooks
                        (fun service -> promise {
                            let original =
                                request
                                    {
                                        Revision = commit
                                        ParentRevision = Some parent
                                        Path = file
                                    }
                                    file
                                    (DiffStoragePolicy.PreferDisk(16_777_216L, 67_108_864L))

                            let! first = openReady service original 1000

                            let token, encoding =
                                match first with
                                | OpenDiffResult.NotDiffable(DiffBlocker.EncodingRequired(side, token, candidates)) ->
                                    Vitest.expect(side).toEqual DiffSide.Current
                                    Vitest.expect(candidates.Length > 0).toBe true

                                    let candidate =
                                        candidates |> Array.find (fun candidate -> candidate.Encoding = "windows-1252")

                                    Vitest.expect(candidate.Preview.Contains "é").toBe true
                                    token, candidate.Encoding
                                | other -> failwith $"Expected an encoding choice, got {other}."

                            let! retry =
                                openReady
                                    service
                                    {
                                        original with
                                            Preparation = Some token
                                            CurrentEncoding = Some encoding
                                    }
                                    1000

                            match retry with
                            | OpenDiffResult.Opened(handle, _, current, _, _) ->
                                Vitest.expect(current.EncodingWasChosen).toBe true
                                Vitest.expect(current.Encoding).toEqual (Some encoding)
                                Vitest.expect(current.Revision).toEqual (Some(revision commit))
                                Vitest.expect(snapshotSpawns).toBe 1

                                let! line =
                                    readLine
                                        service
                                        {
                                            Handle = handle
                                            Side = DiffSide.Current
                                            Line = 0L
                                            OffsetUtf16 = 70_000L
                                            MaxUtf16 = 1
                                            Continuation = None
                                        }
                                        1000

                                Vitest.expect(line.Slice.Text).toBe "é"
                                let! closed = service.Close handle (context ()) |> Async.StartAsPromise
                                expectValue "close encoding retry" closed |> ignore
                            | other -> failwith $"The encoding retry failed: {other}."
                        })
            }
        )

        Vitest.test (
            "cancels snapshot preparation, releases its request and removes its temporary folder before the worker is reused",
            fun () -> promise {
                let root = repositoryRoot ()

                let commit =
                    git root [| "rev-list"; "--max-parents=0"; "--max-count=1"; "HEAD" |]
                    |> _.Trim()

                let cancellation = OperationCancellation.Source()
                let released, resolveReleased = deferred ()
                let mutable tempDirectory = ""
                let mutable canceledSnapshot = ""
                let mutable held: int option = None
                let mutable armed = true

                let hooks = {
                    ToWorker =
                        fun message forward ->
                            match decodeMessage message with
                            | Ok(TextDiffMessage.Init(_, _, directory)) -> tempDirectory <- directory
                            | _ -> ()

                            forward message
                    FromWorker =
                        fun message forward reply ->
                            match decodeMessage message with
                            | Ok(TextDiffMessage.SpawnBlob(callId, _, _, _, spool)) when
                                armed && spool.Contains("history-spool-")
                                ->
                                armed <- false
                                held <- Some callId

                                let folder =
                                    Main.Bindings.Filesystem.readdirSync tempDirectory
                                    |> Array.find (fun name ->
                                        name.StartsWith("history-") && not (name.StartsWith("history-spool-"))
                                    )

                                canceledSnapshot <- Main.Bindings.Path.resolve [| tempDirectory; folder |]
                                // Hold the child response until the worker observes cancellation.
                                cancellation.Cancel()
                            | Ok(TextDiffMessage.ReleaseRequest _) when held.IsSome ->
                                forward message

                                TextDiffMessage.SpawnResult(
                                    held.Value,
                                    SpawnOutcome.Blob {
                                        ExitCode = Some 0
                                        Signal = None
                                        Stderr = ""
                                        StderrTruncated = false
                                        SpawnError = None
                                    }
                                )
                                |> encode
                                |> reply

                                held <- None
                                resolveReleased ()
                            | _ -> forward message
                }

                do!
                    withPoolHooks
                        root
                        hooks
                        (fun service -> promise {
                            let original =
                                request
                                    {
                                        Revision = commit
                                        ParentRevision = None
                                        Path = "README.md"
                                    }
                                    "README.md"
                                    (DiffStoragePolicy.PreferDisk(16_777_216L, 67_108_864L))

                            let! canceled =
                                service.Open
                                    original
                                    (OperationContext.create "cancel-snapshot" cancellation.Cancellation ignore)
                                |> Async.StartAsPromise

                            match canceled with
                            | Failed failure -> Vitest.expect(failure.Category).toEqual FailureCategory.Canceled
                            | other -> failwith $"Expected cancellation, got {other}."

                            do! released
                            // One worker makes this open wait for the canceled preparation to finish.
                            let! reopened = openReady service original 1000
                            Vitest.expect(canceledSnapshot <> "").toBe true
                            Vitest.expect(Main.Bindings.Filesystem.existsSync canceledSnapshot).toBe false

                            match reopened with
                            | OpenDiffResult.Opened(handle, _, _, _, _) ->
                                let! closed = service.Close handle (context ()) |> Async.StartAsPromise
                                expectValue "close after cancellation" closed |> ignore
                            | other -> failwith $"The worker could not reopen after cancellation: {other}."
                        })
            }
        )

        Vitest.test (
            "uses the shared previous-first blocker precedence for nonregular committed sources",
            fun () -> promise {
                let root = repositoryRoot ()
                let file = "src/Components/src/Page/GitComparison/GitPagedDiffViewer.fs"

                let commit =
                    git root [|
                        "log"
                        "--format=%H"
                        "--max-count=1"
                        "HEAD"
                        "--"
                        file
                    |]
                    |> _.Trim()

                let parent = git root [| "rev-parse"; commit + "^" |] |> _.Trim()

                for nonregularPrevious in [| false; true |] do
                    let listingCalls = System.Collections.Generic.HashSet<int>()
                    let mutable rewritten = 0

                    let hooks = {
                        ToWorker =
                            fun message forward ->
                                match decodeMessage message with
                                | Ok(TextDiffMessage.SpawnResult(callId, SpawnOutcome.Short result)) when
                                    listingCalls.Remove callId && result.ExitCode = Some 0
                                    ->
                                    let listing = Text.Encoding.UTF8.GetString result.Stdout

                                    Vitest.expect(listing.StartsWith("100644 ") || listing.StartsWith("100755 ")).toBe
                                        true
                                    // Change only the native Git listing mode, preserving its
                                    // blob identity and repository path for the stock resolver.
                                    let nonregular = "120000" + listing.Substring 6
                                    rewritten <- rewritten + 1

                                    TextDiffMessage.SpawnResult(
                                        callId,
                                        SpawnOutcome.Short {
                                            result with
                                                Stdout = Text.Encoding.UTF8.GetBytes nonregular
                                        }
                                    )
                                    |> encode
                                    |> forward
                                | _ -> forward message
                        FromWorker =
                            fun message forward _ ->
                                match decodeMessage message with
                                | Ok(TextDiffMessage.SpawnShort(callId,
                                                                _,
                                                                _,
                                                                [| "ls-tree"
                                                                   "-z"
                                                                   "--full-tree"
                                                                   selected
                                                                   "--"
                                                                   selectedPath |],
                                                                _)) when
                                    selectedPath = file
                                    && (selected = commit || (nonregularPrevious && selected = parent))
                                    ->
                                    listingCalls.Add callId |> ignore
                                | _ -> ()

                                forward message
                    }

                    do!
                        withPoolHooks
                            root
                            hooks
                            (fun service -> promise {
                                let! result =
                                    openReady
                                        service
                                        (request
                                            {
                                                Revision = commit
                                                ParentRevision = Some parent
                                                Path = file
                                            }
                                            file
                                            (DiffStoragePolicy.PreferDisk(16_777_216L, 67_108_864L)))
                                        1000

                                match result with
                                | OpenDiffResult.NotDiffable(DiffBlocker.NotRegularFile side) ->
                                    Vitest
                                        .expect(side)
                                        .toEqual (
                                            if nonregularPrevious then
                                                DiffSide.Previous
                                            else
                                                DiffSide.Current
                                        )

                                    Vitest.expect(rewritten).toBe (if nonregularPrevious then 2 else 1)
                                | other -> failwith $"Expected a nonregular source blocker, got {other}."
                            })
            }
        )

        Vitest.test (
            "blocks a large parent with a small memory budget and reads it with sufficient budget",
            fun () -> promise {
                let root = repositoryRoot ()
                let file = "src/Components/src/Page/GitComparison/GitPagedDiffViewer.fs"

                let commit =
                    git root [|
                        "log"
                        "--format=%H"
                        "--max-count=1"
                        "HEAD"
                        "--"
                        file
                    |]
                    |> _.Trim()

                let parent = git root [| "rev-parse"; commit + "^" |] |> _.Trim()

                let bytes =
                    git root [| "cat-file"; "-s"; parent + ":" + file |] |> _.Trim() |> Int64.Parse

                Vitest.expect(bytes > 65_536L).toBe true

                do!
                    withPool
                        root
                        (fun service -> promise {
                            let! result =
                                openReady
                                    service
                                    (request
                                        {
                                            Revision = commit
                                            ParentRevision = Some parent
                                            Path = file
                                        }
                                        file
                                        (DiffStoragePolicy.MemoryOnly 655_360L))
                                    1000

                            match result with
                            | OpenDiffResult.NotDiffable(DiffBlocker.BlobTooLargeForMemory(side, actualBytes, limit)) ->
                                Vitest.expect(side).toEqual DiffSide.Previous
                                Vitest.expect(actualBytes).toBe bytes
                                Vitest.expect(limit).toBe 65_536L
                            | other -> failwith $"Expected the shared memory blocker, got {other}."

                            do!
                                verifyOpenedWithStorage
                                    root
                                    service
                                    {
                                        Revision = commit
                                        ParentRevision = Some parent
                                        Path = file
                                    }
                                    file
                                    (DiffStoragePolicy.MemoryOnly 5_242_880L)
                        })
            }
        )
)
