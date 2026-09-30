module ElectronRenderer.GitDiffPageWorkflowTests

open System.Collections.Generic
open Elmish
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Renderer.Context.GitWorkflow
open Renderer.Types
open Swate.Components.Page.GitSidebarTypes
open Swate.Electron.Shared.VersionControlTypes
open Vitest

module Paged = Swate.Components.Page.GitComparison.GitPagedDiffTypes

[<Import("renderToStaticMarkup", "react-dom/server")>]
let private renderToStaticMarkup (element: ReactElement) : string = jsNative

// The diff target reaches the IPC bridges through the Git state context. The preload is
// absent in tests, so the bridges are replaced by empty objects that no test calls.
Vitest.vi.mock (
    "./src/Electron/src/Renderer/Api.js",
    box (fun () ->
        createObj [
            "ipcGitLabApi" ==> createObj []
            "ipcVersionControlApi" ==> createObj []
            "ipcArcVaultApi" ==> createObj []
            "ipcAuthApi" ==> createObj []
            "ipcTemplateApi" ==> createObj []
            "ipcValidationPackageApi" ==> createObj []
        ]
    )
)
|> ignore

let private never<'T> (name: string) : JS.Promise<'T> = promise { return failwith $"unexpected call: {name}" }

let private reply value = promise { return Ok value }

let private succeeded value : OperationResultDto<'T> =
    OperationResultDto.Succeeded {
        Value = value
        Effect = OperationEffectDto.Performed
        Warnings = [||]
        AffectedPaths = [||]
        ResultingRevision = None
        ResultingWorkspaceVersion = None
        Publication = PublicationStateDto.NotApplicable
    }

let private failedWith code (detail: DiffContentBlockedDto option) : OperationResultDto<'T> =
    OperationResultDto.Failed {
        Category = FailureCategoryDto.ProviderError
        Code = code
        Message = code
        StateChanged = false
        Retryable = false
        AffectedPaths = [||]
        RecoveryAction = None
        Details = [||]
        RevisionEvidence = [||]
        DiffDetail = detail
    }

/// Records every text diff request and answers it with the reply function the test set.
type private FakeDiffClient() =
    let mutable operationCount = 0

    member val Opens = ResizeArray<OpenTextDiffRequestDto>()
    member val Reads = ResizeArray<ReadTextDiffPageRequestDto>()
    member val Replays = ResizeArray<ReplayTextDiffPageRequestDto>()
    member val Expands = ResizeArray<ExpandTextDiffRequestDto>()
    member val Lines = ResizeArray<ReadTextDiffLineRequestDto>()
    member val Closes = ResizeArray<TextDiffHandleRequestDto>()
    member val Cancels = ResizeArray<string>()
    member val PageStates = ResizeArray<PageState option>()

    member val OpenReply: OpenTextDiffRequestDto -> OperationResultDto<ResumableOpenDto> =
        fun _ -> failwith "open" with get, set

    member val ReadReply: ReadTextDiffPageRequestDto -> OperationResultDto<ResumablePageDto> =
        fun _ -> failwith "read" with get, set

    member val ReplayReply: ReplayTextDiffPageRequestDto -> OperationResultDto<DiffPageDto> =
        fun _ -> failwith "replay" with get, set

    member val ExpandReply: ExpandTextDiffRequestDto -> OperationResultDto<ResumablePartsDto> =
        fun _ -> failwith "expand" with get, set

    member val LineReply: ReadTextDiffLineRequestDto -> OperationResultDto<ResumableLineDto> =
        fun _ -> failwith "line" with get, set

    /// The page state the app shows, changed by plain sets and by functional updates.
    member val CurrentPageState: PageState option = None with get, set

    member this.SetPageState(page: PageState option) =
        this.CurrentPageState <- page
        this.PageStates.Add page

    member this.UpdatePageState(update: PageState option -> PageState option) =
        this.SetPageState(update this.CurrentPageState)

    member this.Dependencies: GitDependencies = {
        getSessionInfo = fun _ -> never "getSessionInfo"
        getStatus = fun _ -> never "getStatus"
        listRefs = fun _ -> never "listRefs"
        getRepositoryWebUrl = fun _ -> never "getRepositoryWebUrl"
        getStoragePolicySettings = fun _ -> never "getStoragePolicySettings"
        setStoragePolicySettings = fun _ -> never "setStoragePolicySettings"
        loadConflictPage = fun _ _ _ -> never "loadConflictPage"
        updatePageState = fun update -> this.UpdatePageState update
        initializeWorkspace = fun _ -> never "initializeWorkspace"
        bindWorkspace = fun _ -> never "bindWorkspace"
        createRemoteProject = fun _ -> never "createRemoteProject"
        renameOpenArcRoot = fun _ -> never "renameOpenArcRoot"
        checkDependencies = fun _ -> never "checkDependencies"
        installDependency = fun _ -> never "installDependency"
        refreshSynchronization = fun _ -> never "refreshSynchronization"
        synchronize = fun _ -> never "synchronize"
        cancelOperation =
            fun request ->
                this.Cancels.Add request.OperationId
                reply true
        cloneWorkspace = fun _ -> never "cloneWorkspace"
        createRef = fun _ -> never "createRef"
        preflightSwitchRef = fun _ -> never "preflightSwitchRef"
        switchRef = fun _ -> never "switchRef"
        createRevision = fun _ -> never "createRevision"
        restorePaths = fun _ -> never "restorePaths"
        resolveConflict = fun _ -> never "resolveConflict"
        finalizeConflict = fun _ -> never "finalizeConflict"
        cancelConflict = fun _ -> never "cancelConflict"
        listObjects = fun _ -> never "listObjects"
        materializeObject = fun _ -> never "materializeObject"
        pruneStorage = fun _ -> never "pruneStorage"
        deduplicateStorage = fun _ -> never "deduplicateStorage"
        clearStaleLock = fun _ -> never "clearStaleLock"
        textDiff = {
            openTextDiff =
                fun request ->
                    this.Opens.Add request
                    reply (this.OpenReply request)
            readTextDiffPage =
                fun request ->
                    this.Reads.Add request
                    reply (this.ReadReply request)
            replayTextDiffPage =
                fun request ->
                    this.Replays.Add request
                    reply (this.ReplayReply request)
            expandTextDiff =
                fun request ->
                    this.Expands.Add request
                    reply (this.ExpandReply request)
            readTextDiffLine =
                fun request ->
                    this.Lines.Add request
                    reply (this.LineReply request)
            closeTextDiff =
                fun request ->
                    this.Closes.Add request
                    reply (succeeded ())
        }
        hasUsableAccount = fun () -> true
        delay = fun _ -> promise { return () }
        newOperationId =
            fun () ->
                operationCount <- operationCount + 1
                $"op-{operationCount}"
        confirmLfsPrune = fun _ -> false
        confirmInstall = fun _ -> false
        reportError = ignore
    }

let private collectMessages (cmd: Cmd<Msg>) = promise {
    let messages = ResizeArray<Msg>()
    cmd |> List.iter (fun sub -> sub messages.Add)
    do! Promise.sleep 0
    return messages.ToArray()
}

/// Applies the message and every message its commands produce until none are left.
let private run (fake: FakeDiffClient) (msg: Msg) (state: GitState) = promise {
    let queue = Queue<Msg>([ msg ])
    let mutable current = state

    while queue.Count > 0 do
        let next, cmd = update fake.Dependencies fake.SetPageState (queue.Dequeue()) current
        current <- next
        let! messages = collectMessages cmd

        for message in messages do
            queue.Enqueue message

    return current
}

let private runningState = {
    GitState.Empty with
        CurrentArcPath = Some "C:/arc"
        ArcSessionId = 1
}

let private change path : GitSidebarChange = {
    Path = path
    OriginalPath = None
    IndexStatus = "M"
    WorkingTreeStatus = "."
    IsConflicted = false
}

let private select path =
    SelectChangeRequested(change path, ignore)

let private diffOf (state: GitState) =
    match state.DiffPage with
    | Some page -> page
    | None -> failwith "Expected an open diff page."

let private diffMsg msg = DiffPageMsg msg

let private handle: DiffHandleDto = { Id = "diff-1"; Version = "1" }

let private sourceInfo path : DiffSourceInfoDto = {
    Path = path
    Revision = None
    IsAbsent = false
    ByteLength = "10"
    LineCount = Some "2"
    Encoding = Some "utf-8"
    EncodingWasChosen = false
    HasBom = false
}

let private progress complete : ScanProgressDto = {
    ValidatedBytes = "10"
    TotalBytes = "10"
    ScanComplete = complete
}

let private lineRange start count : LineRangeDto = { Start = start; Count = count }

let private textLine (number: int) (text: string) : DiffLineDto = {
    Number = string number
    Ending = LineEndingDto.LF
    Slice = {
        OffsetUtf16 = "0"
        TotalUtf16 = Some(string text.Length)
        Text = text
        Highlights = [||]
    }
}

let private changedRow id : DiffRowDto = {
    Id = id
    Kind = DiffRowKindDto.Replaced
    Previous = Some(textLine 0 "old")
    Current = Some(textLine 0 "new")
}

let private hunkWith id (rows: DiffRowDto[]) =
    DiffPartDto.Hunk {
        HunkId = id
        PreviousRange = lineRange "0" "1"
        CurrentRange = lineRange "0" "1"
        StartsHunk = true
        EndsHunk = true
        Body = HunkBodyDto.AlignedRows rows
    }

let private hunk id =
    hunkWith id [| changedRow $"{id}-row" |]

let private gap id =
    DiffPartDto.HiddenEqual {
        GapId = id
        PreviousRange = lineRange "1" "10"
        CurrentRange = lineRange "1" "10"
    }

let private diffPage pageId (nextCursor: string option) parts : DiffPageDto = {
    PageId = pageId
    NextCursor = nextCursor
    Parts = parts
    Progress = progress nextCursor.IsNone
    OutputComplete = nextCursor.IsNone
    Pending = None
}

let private openedWith first =
    succeeded (ResumableOpenDto.Ready(OpenDiffResultDto.Opened(handle, sourceInfo "a.txt", sourceInfo "a.txt", first)))

let private openedPage page =
    openedWith (ResumablePageDto.Ready page)

/// The worker hands out a new handle for every open, so a reopened diff has a handle of its own.
let private handleOfOpen (openCount: int) : DiffHandleDto = {
    Id = $"diff-{openCount}"
    Version = "1"
}

let private openedOn (openHandle: DiffHandleDto) page =
    succeeded (
        ResumableOpenDto.Ready(
            OpenDiffResultDto.Opened(openHandle, sourceInfo "a.txt", sourceInfo "a.txt", ResumablePageDto.Ready page)
        )
    )

let private isFailed (status: GitDiffPageStatus) =
    match status with
    | GitDiffPageStatus.Failed _ -> true
    | _ -> false

let private describePart (part: Paged.PagedPart) =
    match part with
    | Paged.PagedPart.HunkRows(hunkId, _, _, _, _, _) -> $"hunk:{hunkId}"
    | Paged.PagedPart.UnalignedRegion(hunkId, _, _, _, _) -> $"unaligned:{hunkId}"
    | Paged.PagedPart.HiddenGap(gapId, _, _) -> $"gap:{gapId}"
    | Paged.PagedPart.ExpandedRows(gapId, rows) -> $"expanded:{gapId}:{rows.Length}"
    | Paged.PagedPart.EvictedPage(pageId, _) -> $"evicted:{pageId}"

let private pageParts (page: GitDiffWindowPage) = page.Parts |> Array.map describePart

let private openFirstPage (fake: FakeDiffClient) (first: DiffPageDto) = promise {
    fake.OpenReply <- fun _ -> openedPage first
    return! run fake (select "a.txt") runningState
}

let private currentLine (page: GitDiffPageData) =
    page.Pages.[0].Parts
    |> Array.pick (
        function
        | Paged.PagedPart.HunkRows(_, _, _, _, _, rows) -> rows.[0].Current
        | _ -> None
    )

let private noChangesShown (page: GitDiffPageData) =
    let markup =
        renderToStaticMarkup (Renderer.Components.MainContent.GitDiffTarget.Main page)

    markup.Contains("data-testid=\"renderer-git-diff-no-changes\"")

Vitest.describe (
    "Git diff page workflow",
    fun () ->
        Vitest.test (
            "Opening follows the preparation continuation, then reads the first page from the page continuation",
            fun () -> promise {
                let fake = FakeDiffClient()

                fake.OpenReply <-
                    fun request ->
                        match request.Continuation with
                        | None -> succeeded (ResumableOpenDto.Scanning(progress false, "open-continuation", None))
                        | Some _ -> openedWith (ResumablePageDto.Scanning(progress false, "page-continuation", None))

                fake.ReadReply <- fun _ -> succeeded (ResumablePageDto.Ready(diffPage "p1" None [| hunk "h1" |]))

                let renamed = {
                    change "new.txt" with
                        OriginalPath = Some "old.txt"
                        IndexStatus = "R"
                }

                let! state = run fake (SelectChangeRequested(renamed, ignore)) runningState
                let page = diffOf state

                Vitest.expect(fake.Opens.Count).toBe (2)
                Vitest.expect(fake.Opens.[0].Path).toBe ("new.txt")
                Vitest.expect(fake.Opens.[0].PreviousPath).toEqual (Some "old.txt")
                Vitest.expect(fake.Opens.[0].ContextLines).toBe (3)
                Vitest.expect(fake.Opens.[0].Continuation).toEqual (None)

                Vitest
                    .expect(
                        {
                            fake.Opens.[1] with
                                OperationId = ""
                                Continuation = None
                        }
                    )
                    .toEqual ({ fake.Opens.[0] with OperationId = "" })

                Vitest.expect(fake.Opens.[1].Continuation).toEqual (Some "open-continuation")
                Vitest.expect(fake.Reads.Count).toBe (1)
                Vitest.expect(fake.Reads.[0].Cursor).toBe ("page-continuation")
                Vitest.expect(fake.Reads.[0].HandleId).toBe (handle.Id)
                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)
                Vitest.expect(page.Handle).toEqual (Some handle)
                Vitest.expect(page.SourceInfos.IsSome).toBe (true)
                Vitest.expect(page.Pages |> Array.map pageParts).toEqual ([| [| "hunk:h1" |] |])
                Vitest.expect(fake.PageStates.[fake.PageStates.Count - 1]).toEqual (Some(PageState.GitDiffPage page))
            }
        )

        Vitest.test (
            "A diff that cannot be shown opens a blocked page for the side the library names",
            fun () -> promise {
                let cases = [
                    DiffBlockerDto.Binary(DiffSideDto.Previous, "nul byte"),
                    GitDiffPageStatus.Blocked(Some DiffSideDto.Previous, GitDiffBlockReason.Binary "nul byte")
                    DiffBlockerDto.LocalContentUnavailable(DiffSideDto.Current, Some "oid-1"),
                    GitDiffPageStatus.Blocked(
                        Some DiffSideDto.Current,
                        GitDiffBlockReason.LocalContentUnavailable(Some "oid-1")
                    )
                    DiffBlockerDto.NotRegularFile DiffSideDto.Current,
                    GitDiffPageStatus.Blocked(Some DiffSideDto.Current, GitDiffBlockReason.NotRegularFile)
                    DiffBlockerDto.ProviderUnsupported,
                    GitDiffPageStatus.Blocked(None, GitDiffBlockReason.ProviderUnsupported)
                ]

                for blocker, expected in cases do
                    let fake = FakeDiffClient()
                    fake.OpenReply <- fun _ -> succeeded (ResumableOpenDto.Ready(OpenDiffResultDto.NotDiffable blocker))
                    let! state = run fake (select "a.txt") runningState
                    Vitest.expect((diffOf state).Status).toEqual (expected)
                    Vitest.expect((diffOf state).Handle).toEqual (None)
            }
        )

        Vitest.test (
            "An encoding pick reopens with the preparation token, and the other side's choice follows",
            fun () -> promise {
                let fake = FakeDiffClient()
                let token: PreparationTokenDto = { Id = "token-1" }

                let previousCandidates: EncodingCandidateDto[] = [|
                    {
                        Encoding = "windows-1252"
                        Preview = "a"
                    }
                |]

                let currentCandidates: EncodingCandidateDto[] = [| { Encoding = "latin1"; Preview = "b" } |]

                let encodingRequired side candidates =
                    succeeded (
                        ResumableOpenDto.Ready(
                            OpenDiffResultDto.NotDiffable(DiffBlockerDto.EncodingRequired(side, token, candidates))
                        )
                    )

                fake.OpenReply <-
                    fun request ->
                        match request.PreviousEncoding, request.CurrentEncoding with
                        | None, _ -> encodingRequired DiffSideDto.Previous previousCandidates
                        | Some _, None -> encodingRequired DiffSideDto.Current currentCandidates
                        | Some _, Some _ -> openedPage (diffPage "p1" None [| hunk "h1" |])

                let! state = run fake (select "a.txt") runningState
                let generation = (diffOf state).Generation

                Vitest
                    .expect((diffOf state).Status)
                    .toEqual (GitDiffPageStatus.EncodingChoice(DiffSideDto.Previous, token, previousCandidates))

                let! state =
                    run
                        fake
                        (diffMsg (GitDiffMsg.ChooseEncoding(generation, DiffSideDto.Previous, "windows-1252")))
                        state

                Vitest.expect(fake.Opens.[1].PreparationTokenId).toEqual (Some token.Id)
                Vitest.expect(fake.Opens.[1].PreviousEncoding).toEqual (Some "windows-1252")
                Vitest.expect(fake.Opens.[1].CurrentEncoding).toEqual (None)

                Vitest
                    .expect((diffOf state).Status)
                    .toEqual (GitDiffPageStatus.EncodingChoice(DiffSideDto.Current, token, currentCandidates))

                let! state =
                    run fake (diffMsg (GitDiffMsg.ChooseEncoding(generation, DiffSideDto.Current, "latin1"))) state

                Vitest.expect(fake.Opens.[2].PreparationTokenId).toEqual (Some token.Id)
                Vitest.expect(fake.Opens.[2].PreviousEncoding).toEqual (Some "windows-1252")
                Vitest.expect(fake.Opens.[2].CurrentEncoding).toEqual (Some "latin1")
                Vitest.expect((diffOf state).Status).toEqual (GitDiffPageStatus.Ready)
            }
        )

        Vitest.test (
            "The next page is read with the cursor of the last page and joins the window",
            fun () -> promise {
                let fake = FakeDiffClient()
                let! state = openFirstPage fake (diffPage "p1" (Some "cursor-1") [| hunk "h1" |])
                fake.ReadReply <- fun _ -> succeeded (ResumablePageDto.Ready(diffPage "p2" None [| hunk "h2" |]))

                let! state = run fake (diffMsg (GitDiffMsg.LoadNext (diffOf state).Generation)) state
                let page = diffOf state

                Vitest.expect(fake.Reads.[0].Cursor).toBe ("cursor-1")
                Vitest.expect(page.Pages |> Array.map _.PageId).toEqual ([| "p1"; "p2" |])
                Vitest.expect(page.NextCursor).toEqual (None)
                Vitest.expect(page.OutputComplete).toBe (true)
                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)
            }
        )

        Vitest.test (
            "An expansion passes its continuation back and replaces the gap with the returned parts",
            fun () -> promise {
                let fake = FakeDiffClient()
                let! state = openFirstPage fake (diffPage "p1" None [| hunk "h1"; gap "g1"; hunk "h2" |])

                fake.ExpandReply <-
                    fun request ->
                        match request.Continuation with
                        | None -> succeeded (ResumablePartsDto.Scanning(progress false, "expand-continuation", None))
                        | Some _ ->
                            succeeded (
                                ResumablePartsDto.Ready [|
                                    DiffPartDto.ExpandedContext("g1", [| changedRow "e1"; changedRow "e2" |])
                                    gap "g2"
                                |]
                            )

                let! state = run fake (diffMsg (GitDiffMsg.Expand((diffOf state).Generation, "g1", true))) state
                let page = diffOf state

                Vitest.expect(fake.Expands.[0].GapId).toBe ("g1")
                Vitest.expect(fake.Expands.[0].FromStart).toBe (true)
                Vitest.expect(fake.Expands.[0].Continuation).toEqual (None)

                Vitest
                    .expect(
                        {
                            fake.Expands.[1] with
                                OperationId = ""
                                Continuation = None
                        }
                    )
                    .toEqual (
                        {
                            fake.Expands.[0] with
                                OperationId = ""
                        }
                    )

                Vitest.expect(fake.Expands.[1].Continuation).toEqual (Some "expand-continuation")

                Vitest.expect(pageParts page.Pages.[0]).toEqual ([| "hunk:h1"; "expanded:g1:2"; "gap:g2"; "hunk:h2" |])

                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)
            }
        )

        Vitest.test (
            "Consecutive line slices are appended to the displayed line",
            fun () -> promise {
                let fake = FakeDiffClient()

                let longRow = {
                    changedRow "r1" with
                        Current =
                            Some {
                                textLine 0 "abc" with
                                    Slice = {
                                        OffsetUtf16 = "0"
                                        TotalUtf16 = None
                                        Text = "abc"
                                        Highlights = [||]
                                    }
                            }
                }

                let! state = openFirstPage fake (diffPage "p1" None [| hunkWith "h1" [| longRow |] |])

                fake.LineReply <-
                    fun request ->
                        let text, total =
                            if request.OffsetUtf16 = "3" then
                                "def", None
                            else
                                "ghi", Some "9"

                        succeeded (
                            ResumableLineDto.Ready {
                                Number = request.Line
                                Ending = LineEndingDto.LF
                                Slice = {
                                    OffsetUtf16 = request.OffsetUtf16
                                    TotalUtf16 = total
                                    Text = text
                                    Highlights = [|
                                        {
                                            Start = 0
                                            Length = 1
                                            Kind = HighlightKindDto.ChangedText
                                        }
                                    |]
                                }
                            }
                        )

                let generation = (diffOf state).Generation

                let! state =
                    run fake (diffMsg (GitDiffMsg.LoadLineSlice(generation, DiffSideDto.Current, 0.0, 3.0))) state

                let! state =
                    run fake (diffMsg (GitDiffMsg.LoadLineSlice(generation, DiffSideDto.Current, 0.0, 6.0))) state

                let line = currentLine (diffOf state)

                Vitest.expect(fake.Lines.[0].Side).toEqual (DiffSideDto.Current)
                Vitest.expect(fake.Lines.[0].Line).toBe ("0")
                Vitest.expect(fake.Lines.[0].OffsetUtf16).toBe ("3")
                Vitest.expect(fake.Lines.[1].OffsetUtf16).toBe ("6")
                Vitest.expect(line.Text).toBe ("abcdefghi")
                Vitest.expect(line.TotalUtf16).toEqual (Some 9.0)
                Vitest.expect(line.Highlights |> Array.map _.Start).toEqual ([| 3; 6 |])
            }
        )

        Vitest.test (
            "A ninth page evicts the farthest page, and a replay puts it back in place",
            fun () -> promise {
                let fake = FakeDiffClient()

                let pageDto index =
                    diffPage $"p{index}" (Some $"cursor-{index}") [| hunk $"h{index}" |]

                let! state = openFirstPage fake (pageDto 1)

                fake.ReadReply <-
                    fun request ->
                        let index = int (request.Cursor.Replace("cursor-", "")) + 1
                        succeeded (ResumablePageDto.Ready(pageDto index))

                let current = ref state

                for _ in 2..9 do
                    let! next = run fake (diffMsg (GitDiffMsg.LoadNext (diffOf current.Value).Generation)) current.Value
                    current.Value <- next

                let loadedIds (page: GitDiffPageData) =
                    page.Pages
                    |> Array.filter (fun windowPage -> not windowPage.IsEvicted)
                    |> Array.map _.PageId

                let state = current.Value
                let page = diffOf state
                Vitest.expect(page.Pages.Length).toBe (9)
                Vitest.expect(pageParts page.Pages.[0]).toEqual ([| "evicted:p1" |])
                Vitest.expect(loadedIds page).toEqual ([| for index in 2..9 -> $"p{index}" |])

                fake.ReplayReply <-
                    fun request ->
                        let index = int (request.PageId.Replace("p", ""))
                        succeeded (pageDto index)

                let! state = run fake (diffMsg (GitDiffMsg.Replay(page.Generation, "p1", []))) state
                let page = diffOf state

                Vitest.expect(fake.Replays.[0].PageId).toBe ("p1")
                Vitest.expect(pageParts page.Pages.[0]).toEqual ([| "hunk:h1" |])
                Vitest.expect(pageParts page.Pages.[8]).toEqual ([| "evicted:p9" |])
                Vitest.expect(loadedIds page).toEqual ([| for index in 1..8 -> $"p{index}" |])
                Vitest.expect(page.NextCursor).toEqual (Some "cursor-9")
            }
        )

        Vitest.test (
            "Loaded pages above 8 MiB evict the page farthest from the requested page, and a single oversized page stays loaded",
            fun () -> promise {
                let fake = FakeDiffClient()

                let bigPage (index: int) =
                    let row = {
                        changedRow $"big-{index}" with
                            Current = Some(textLine index (String.replicate (3 * 1024 * 1024) "x"))
                    }

                    diffPage $"p{index}" (if index < 3 then Some $"cursor-{index}" else None) [|
                        hunkWith $"h{index}" [| row |]
                    |]

                let! state = openFirstPage fake (bigPage 1)

                fake.ReadReply <-
                    fun request ->
                        succeeded (ResumablePageDto.Ready(bigPage (int (request.Cursor.Replace("cursor-", "")) + 1)))

                let! state = run fake (diffMsg (GitDiffMsg.LoadNext (diffOf state).Generation)) state
                Vitest.expect((diffOf state).Pages |> Array.map _.IsEvicted).toEqual ([| false; false |])

                let! state = run fake (diffMsg (GitDiffMsg.LoadNext (diffOf state).Generation)) state
                Vitest.expect((diffOf state).Pages |> Array.map _.IsEvicted).toEqual ([| true; false; false |])

                let oversized = FakeDiffClient()

                let hugeRow = {
                    changedRow "huge" with
                        Current = Some(textLine 0 (String.replicate (9 * 1024 * 1024) "x"))
                }

                let! single = openFirstPage oversized (diffPage "p1" None [| hunkWith "h1" [| hugeRow |] |])
                Vitest.expect((diffOf single).Pages |> Array.map _.IsEvicted).toEqual ([| false |])
            }
        )

        Vitest.test (
            "When the user switches files quickly, the late open of the first file closes its handle and the second file stays ready",
            fun () -> promise {
                let fake = FakeDiffClient()

                fake.OpenReply <-
                    fun request ->
                        openedOn
                            {
                                Id = $"diff-{request.Path}"
                                Version = "1"
                            }
                            (diffPage "p1" None [| hunk "h1" |])

                let first, firstCmd =
                    update fake.Dependencies fake.SetPageState (select "a.txt") runningState

                let second, secondCmd =
                    update fake.Dependencies fake.SetPageState (select "b.txt") first

                let firstOpenId = (diffOf first).RunningOperations |> List.head
                let! lateMessages = collectMessages firstCmd
                let! secondMessages = collectMessages secondCmd
                let current = ref second

                for message in secondMessages do
                    let! next = run fake message current.Value
                    current.Value <- next

                Vitest.expect((diffOf current.Value).Status).toEqual (GitDiffPageStatus.Ready)
                let pageStatesBefore = fake.PageStates.Count

                for message in lateMessages do
                    let! next = run fake message current.Value
                    current.Value <- next

                let page = diffOf current.Value
                Vitest.expect(page.Path).toBe ("b.txt")
                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)
                Vitest.expect(page.Handle).toEqual (Some { Id = "diff-b.txt"; Version = "1" })
                Vitest.expect(fake.Cancels.Contains firstOpenId).toBe (true)
                Vitest.expect(fake.Closes |> Seq.map _.HandleId |> Seq.toArray).toEqual ([| "diff-a.txt" |])
                Vitest.expect(fake.PageStates.Count).toBe (pageStatesBefore)
            }
        )

        Vitest.test (
            "A handle that arrives after the user left the diff is closed at once",
            fun () -> promise {
                let fake = FakeDiffClient()
                fake.OpenReply <- fun _ -> openedPage (diffPage "p1" None [| hunk "h1" |])

                let opening, openCmd =
                    update fake.Dependencies fake.SetPageState (select "a.txt") runningState

                let! left = run fake (diffMsg (GitDiffMsg.PageStateObserved(Some(PageState.TextPage "notes")))) opening

                let! lateMessages = collectMessages openCmd
                let current = ref left

                for message in lateMessages do
                    let! next = run fake message current.Value
                    current.Value <- next

                Vitest.expect(current.Value.DiffPage).toEqual (None)
                Vitest.expect(fake.Closes |> Seq.map _.HandleId |> Seq.toArray).toEqual ([| handle.Id |])
            }
        )

        Vitest.test (
            "Leaving an opened diff page closes its handle",
            fun () -> promise {
                let fake = FakeDiffClient()
                let! state = openFirstPage fake (diffPage "p1" None [| hunk "h1" |])

                let! state = run fake (diffMsg (GitDiffMsg.PageStateObserved None)) state

                Vitest.expect(state.DiffPage).toEqual (None)
                Vitest.expect(fake.Closes |> Seq.map _.HandleId |> Seq.toArray).toEqual ([| handle.Id |])
            }
        )

        Vitest.test (
            "Changed sources, a failed worker and content that is not text get their own statuses",
            fun () -> promise {
                let detail: DiffContentBlockedDto = {
                    Side = DiffSideDto.Current
                    Evidence = "invalid utf-8"
                }

                let cases = [
                    failedWith "source_changed" None, GitDiffPageStatus.SourceChanged
                    failedWith "diff_worker_failed" None, GitDiffPageStatus.WorkerFailed "diff_worker_failed"
                    failedWith "diff_content_not_text" (Some detail),
                    GitDiffPageStatus.Blocked(Some DiffSideDto.Current, GitDiffBlockReason.NotText "invalid utf-8")
                    failedWith "git_failure" None, GitDiffPageStatus.Failed "git_failure"
                ]

                for failure, expected in cases do
                    let fake = FakeDiffClient()
                    let! state = openFirstPage fake (diffPage "p1" (Some "cursor-1") [| hunk "h1" |])
                    fake.ReadReply <- fun _ -> failure
                    let! state = run fake (diffMsg (GitDiffMsg.LoadNext (diffOf state).Generation)) state
                    Vitest.expect((diffOf state).Status).toEqual (expected)
            }
        )

        Vitest.test (
            "An expansion during a running next page request neither repeats the request nor adds its page twice",
            fun () -> promise {
                let fake = FakeDiffClient()
                let! state = openFirstPage fake (diffPage "p1" (Some "cursor-1") [| hunk "h1"; gap "g1" |])
                let generation = (diffOf state).Generation
                fake.ReadReply <- fun _ -> succeeded (ResumablePageDto.Ready(diffPage "p2" None [| hunk "h2" |]))

                fake.ExpandReply <-
                    fun _ ->
                        succeeded (
                            ResumablePartsDto.Ready [|
                                DiffPartDto.ExpandedContext("g1", [| changedRow "e1" |])
                            |]
                        )

                let step msg state =
                    update fake.Dependencies fake.SetPageState (diffMsg msg) state

                let loading, loadCmd = step (GitDiffMsg.LoadNext generation) state
                let expanding, expandCmd = step (GitDiffMsg.Expand(generation, "g1", true)) loading
                let again, againCmd = step (GitDiffMsg.LoadNext generation) expanding

                let! pageMessages = collectMessages loadCmd
                let! expandMessages = collectMessages expandCmd
                let! repeatedMessages = collectMessages againCmd
                let current = ref again

                // The page answer is delivered twice, as if the same cursor had been read twice.
                for message in
                    Array.concat [
                        pageMessages
                        repeatedMessages
                        pageMessages
                        expandMessages
                    ] do
                    let! next = run fake message current.Value
                    current.Value <- next

                let page = diffOf current.Value

                Vitest.expect(fake.Reads.Count).toBe (1)
                Vitest.expect(page.Pages |> Array.map _.PageId).toEqual ([| "p1"; "p2" |])
                Vitest.expect(pageParts page.Pages.[0]).toEqual ([| "hunk:h1"; "expanded:g1:1" |])
                Vitest.expect(page.NextRequest).toEqual (None)
                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)
            }
        )

        Vitest.test (
            "A repeated line slice request is sent once, and an overlapping answer appends only the text beyond the displayed end",
            fun () -> promise {
                let fake = FakeDiffClient()

                let sliceLine (offset: string) (text: string) (total: string option) highlights : DiffLineDto = {
                    Number = "0"
                    Ending = LineEndingDto.LF
                    Slice = {
                        OffsetUtf16 = offset
                        TotalUtf16 = total
                        Text = text
                        Highlights = highlights
                    }
                }

                let longRow = {
                    changedRow "r1" with
                        Current = Some(sliceLine "0" "abc" None [||])
                }

                let! state = openFirstPage fake (diffPage "p1" None [| hunkWith "h1" [| longRow |] |])
                let generation = (diffOf state).Generation
                fake.LineReply <- fun _ -> succeeded (ResumableLineDto.Ready(sliceLine "3" "def" None [||]))

                let request =
                    diffMsg (GitDiffMsg.LoadLineSlice(generation, DiffSideDto.Current, 0.0, 3.0))

                let first, firstCmd = update fake.Dependencies fake.SetPageState request state
                let second, secondCmd = update fake.Dependencies fake.SetPageState request first
                let! messages = collectMessages (Cmd.batch [ firstCmd; secondCmd ])

                Vitest.expect(fake.Lines.Count).toBe (1)
                Vitest.expect((diffOf second).PendingLineSlices.Length).toBe (1)

                let current = ref second

                for message in messages do
                    let! next = run fake message current.Value
                    current.Value <- next

                let answer line =
                    diffMsg (
                        GitDiffMsg.LineCompleted(
                            generation,
                            fake.Lines.[0],
                            Ok(succeeded (ResumableLineDto.Ready line))
                        )
                    )

                let overlapping =
                    sliceLine "3" "defghi" (Some "9") [|
                        {
                            Start = 4
                            Length = 2
                            Kind = HighlightKindDto.ChangedText
                        }
                    |]

                let! state = run fake (answer overlapping) current.Value
                let! state = run fake (answer (sliceLine "0" "abc" (Some "9") [||])) state
                let line = currentLine (diffOf state)

                Vitest.expect(line.Text).toBe ("abcdefghi")
                Vitest.expect(line.TotalUtf16).toEqual (Some 9.0)

                Vitest
                    .expect(
                        line.Highlights
                        |> Array.map (fun highlight -> highlight.Start, highlight.Length)
                    )
                    .toEqual ([| 7, 2 |])

                Vitest.expect((diffOf state).PendingLineSlices).toEqual ([])
            }
        )

        Vitest.test (
            "A Scanning open answer that arrives after the user left the diff is abandoned once",
            fun () -> promise {
                let fake = FakeDiffClient()

                fake.OpenReply <-
                    fun request ->
                        match request.Continuation with
                        | None -> succeeded (ResumableOpenDto.Scanning(progress false, "open-continuation", None))
                        | Some _ -> failedWith "operation_canceled" None

                let opening, openCmd =
                    update fake.Dependencies fake.SetPageState (select "a.txt") runningState

                let! left = run fake (diffMsg (GitDiffMsg.PageStateObserved(Some(PageState.TextPage "notes")))) opening
                let! lateMessages = collectMessages openCmd
                let current = ref left

                for message in lateMessages do
                    let! next = run fake message current.Value
                    current.Value <- next

                let abandons =
                    fake.Opens
                    |> Seq.filter (fun request -> request.Continuation = Some "open-continuation")
                    |> Seq.toArray

                Vitest.expect(abandons.Length).toBe (1)

                Vitest
                    .expect(
                        {
                            abandons.[0] with
                                OperationId = ""
                                Continuation = None
                        }
                    )
                    .toEqual ({ fake.Opens.[0] with OperationId = "" })

                Vitest
                    .expect(
                        fake.Cancels
                        |> Seq.filter (fun id -> id = abandons.[0].OperationId)
                        |> Seq.length
                    )
                    .toBe (1)

                Vitest.expect(current.Value.DiffPage).toEqual (None)
            }
        )

        Vitest.test (
            "Expansions on one page stay within 8 MiB by collapsing the least recently expanded gap, which expands again",
            fun () -> promise {
                let fake = FakeDiffClient()
                let gaps = [| for index in 1..5 -> gap $"g{index}" |]
                let! state = openFirstPage fake (diffPage "p1" None (Array.append [| hunk "h1" |] gaps))
                let generation = (diffOf state).Generation
                let bigText = String.replicate (2 * 1024 * 1024) "x"

                fake.ExpandReply <-
                    fun request ->
                        let row = {
                            changedRow $"{request.GapId}-row" with
                                Current = Some(textLine 0 bigText)
                        }

                        succeeded (ResumablePartsDto.Ready [| DiffPartDto.ExpandedContext(request.GapId, [| row |]) |])

                let loadedBytes (page: GitDiffPageData) =
                    page.Pages
                    |> Array.filter (fun windowPage -> not windowPage.IsEvicted)
                    |> Array.sumBy _.PayloadBytes

                let current = ref state

                for gapId in [ "g1"; "g2"; "g3"; "g4"; "g5"; "g1" ] do
                    let! next = run fake (diffMsg (GitDiffMsg.Expand(generation, gapId, true))) current.Value
                    current.Value <- next
                    Vitest.expect(loadedBytes (diffOf next)).toBeLessThanOrEqual (GitDiffPageLoader.MaxLoadedBytes)

                let page = diffOf current.Value

                Vitest
                    .expect(fake.Expands |> Seq.map _.GapId |> Seq.toArray)
                    .toEqual ([| "g1"; "g2"; "g3"; "g4"; "g5"; "g1" |])

                Vitest
                    .expect(pageParts page.Pages.[0])
                    .toEqual (
                        [|
                            "hunk:h1"
                            "expanded:g1:1"
                            "gap:g2"
                            "gap:g3"
                            "expanded:g4:1"
                            "expanded:g5:1"
                        |]
                    )
            }
        )

        Vitest.test (
            "Expanding the remainder of one gap again and again collapses the oldest rows, which expand again with the same lines",
            fun () -> promise {
                let fake = FakeDiffClient()

                let familyGap (index: int) =
                    DiffPartDto.HiddenEqual {
                        GapId = $"g{index}"
                        PreviousRange = lineRange (string index) (string (1000 - index))
                        CurrentRange = lineRange (string index) (string (1000 - index))
                    }

                let! state = openFirstPage fake (diffPage "p1" None [| hunk "h1"; familyGap 0 |])
                let generation = (diffOf state).Generation
                // Both sides carry the text, so each expansion adds a little over 2 MiB.
                let bigText = String.replicate (1024 * 1024) "x"

                // The library answers an already expanded gap with its recorded first result.
                let recorded = Dictionary<string, ResumablePartsDto>()

                fake.ExpandReply <-
                    fun request ->
                        let index = int (request.GapId.Replace("g", ""))

                        if not (recorded.ContainsKey request.GapId) then
                            let row: DiffRowDto = {
                                Id = $"row-{index}"
                                Kind = DiffRowKindDto.Context
                                Previous = Some(textLine index bigText)
                                Current = Some(textLine index bigText)
                            }

                            recorded.[request.GapId] <-
                                ResumablePartsDto.Ready [|
                                    DiffPartDto.ExpandedContext(request.GapId, [| row |])
                                    familyGap (index + 1)
                                |]

                        succeeded recorded.[request.GapId]

                let loadedBytes (page: GitDiffPageData) =
                    page.Pages
                    |> Array.filter (fun windowPage -> not windowPage.IsEvicted)
                    |> Array.sumBy _.PayloadBytes

                let newestChunkBytes (page: GitDiffPageData) =
                    page.Pages.[0].ExpandedGaps |> List.last |> _.PayloadBytes

                let expanded (page: GitDiffPageData) (gapId: string) =
                    page.Pages.[0].Parts
                    |> Array.pick (
                        function
                        | Paged.PagedPart.ExpandedRows(id, rows) when id = gapId -> Some rows
                        | _ -> None
                    )

                let current = ref state

                for gapId in [ "g0"; "g1"; "g2"; "g3"; "g4"; "g0" ] do
                    let! next = run fake (diffMsg (GitDiffMsg.Expand(generation, gapId, true))) current.Value
                    current.Value <- next
                    let page = diffOf next

                    Vitest
                        .expect(loadedBytes page)
                        .toBeLessThanOrEqual (GitDiffPageLoader.MaxLoadedBytes + newestChunkBytes page)

                    if gapId = "g4" then
                        Vitest
                            .expect(pageParts page.Pages.[0])
                            .toEqual (
                                [|
                                    "hunk:h1"
                                    "gap:g0"
                                    "gap:g1"
                                    "expanded:g2:1"
                                    "expanded:g3:1"
                                    "expanded:g4:1"
                                    "gap:g5"
                                |]
                            )

                        Vitest
                            .expect(
                                page.Pages.[0].Parts
                                |> Array.pick (
                                    function
                                    | Paged.PagedPart.HiddenGap("g0", previous, current) -> Some(previous, current)
                                    | _ -> None
                                )
                            )
                            .toEqual (
                                ({ Start = 0.0; Count = 1.0 }: Paged.PagedRange),
                                ({ Start = 0.0; Count = 1.0 }: Paged.PagedRange)
                            )

                let page = diffOf current.Value
                let rows = expanded page "g0"

                Vitest
                    .expect(pageParts page.Pages.[0])
                    .toEqual (
                        [|
                            "hunk:h1"
                            "expanded:g0:1"
                            "gap:g1"
                            "gap:g2"
                            "expanded:g3:1"
                            "expanded:g4:1"
                            "gap:g5"
                        |]
                    )

                Vitest.expect(rows |> Array.map _.Id).toEqual ([| "row-0" |])

                Vitest
                    .expect(rows |> Array.map (fun row -> row.Current |> Option.map _.Number))
                    .toEqual ([| Some 0.0 |])
            }
        )

        Vitest.test (
            "No changes is shown only when the scan and the output are complete and no hunk exists",
            fun () -> promise {
                let fake = FakeDiffClient()
                let! state = openFirstPage fake (diffPage "p1" None [| gap "g1" |])
                let complete = diffOf state

                let withParts parts = {
                    complete with
                        Pages = [|
                            {
                                complete.Pages.[0] with
                                    Parts = parts |> Array.map Renderer.GitDiffPresentation.part
                            }
                        |]
                }

                Vitest.expect(noChangesShown complete).toBe (true)

                Vitest.expect(noChangesShown { complete with OutputComplete = false }).toBe (false)

                Vitest
                    .expect(
                        noChangesShown {
                            complete with
                                Progress = Some(progress false)
                        }
                    )
                    .toBe (false)

                Vitest.expect(noChangesShown (withParts [| hunk "h1" |])).toBe (false)

                let evicted = {
                    complete with
                        Pages = [|
                            {
                                complete.Pages.[0] with
                                    Parts = [| Paged.PagedPart.EvictedPage("p1", 3) |]
                            }
                        |]
                }

                Vitest.expect(noChangesShown evicted).toBe (false)
            }
        )

        Vitest.test (
            "An expired session during a next page request reopens the diff and reads forward to the requested page",
            fun () -> promise {
                let fake = FakeDiffClient()

                let pageDto index =
                    diffPage $"p{index}" (Some $"cursor-{index}") [| hunk $"h{index}" |]

                fake.OpenReply <- fun _ -> openedOn (handleOfOpen fake.Opens.Count) (pageDto 1)

                fake.ReadReply <-
                    fun request ->
                        if request.HandleId = "diff-1" && request.Cursor = "cursor-2" then
                            failedWith "diff_session_closed" None
                        else
                            let index = int (request.Cursor.Replace("cursor-", "")) + 1
                            succeeded (ResumablePageDto.Ready(pageDto index))

                let! state = run fake (select "a.txt") runningState
                let generation = (diffOf state).Generation
                let! state = run fake (diffMsg (GitDiffMsg.LoadNext generation)) state
                Vitest.expect((diffOf state).RequestedPageIndex).toBe (1)

                let! state = run fake (diffMsg (GitDiffMsg.LoadNext generation)) state
                let page = diffOf state

                Vitest.expect(fake.Opens.Count).toBe (2)

                Vitest
                    .expect({ fake.Opens.[1] with OperationId = "" })
                    .toEqual ({ fake.Opens.[0] with OperationId = "" })

                Vitest
                    .expect(fake.Reads |> Seq.map (fun read -> read.HandleId, read.Cursor) |> Seq.toArray)
                    .toEqual (
                        [|
                            "diff-1", "cursor-1"
                            "diff-1", "cursor-2"
                            "diff-2", "cursor-1"
                        |]
                    )

                Vitest.expect(page.Handle).toEqual (Some(handleOfOpen 2))
                Vitest.expect(page.Pages |> Array.map _.PageId).toEqual ([| "p1"; "p2" |])
                Vitest.expect(page.RequestedPageIndex).toBe (1)
                Vitest.expect(page.NextCursor).toEqual (Some "cursor-2")
                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)
                Vitest.expect(state.DiffReopen).toEqual (None)
                Vitest.expect(fake.Closes |> Seq.map _.HandleId |> Seq.toArray).toEqual ([| "diff-1" |])
            }
        )

        Vitest.test (
            "An expired session during an expansion reopens the diff with the gap collapsed",
            fun () -> promise {
                let fake = FakeDiffClient()
                let first = diffPage "p1" None [| hunk "h1"; gap "g1" |]
                fake.OpenReply <- fun _ -> openedOn (handleOfOpen fake.Opens.Count) first
                fake.ExpandReply <- fun _ -> failedWith "diff_session_closed" None

                let! state = run fake (select "a.txt") runningState
                let! state = run fake (diffMsg (GitDiffMsg.Expand((diffOf state).Generation, "g1", true))) state
                let page = diffOf state

                Vitest.expect(fake.Expands.Count).toBe (1)
                Vitest.expect(fake.Opens.Count).toBe (2)
                Vitest.expect(fake.Opens.[1].PreparationTokenId).toEqual (None)
                Vitest.expect(page.Handle).toEqual (Some(handleOfOpen 2))
                Vitest.expect(page.Pages |> Array.map pageParts).toEqual ([| [| "hunk:h1"; "gap:g1" |] |])
                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)
            }
        )

        Vitest.test (
            "A stale preparation token after an encoding pick reopens with the chosen encoding and no token",
            fun () -> promise {
                let fake = FakeDiffClient()
                let token: PreparationTokenDto = { Id = "token-1" }

                let candidates: EncodingCandidateDto[] = [|
                    {
                        Encoding = "windows-1252"
                        Preview = "a"
                    }
                |]

                fake.OpenReply <-
                    fun request ->
                        match request.PreviousEncoding, request.PreparationTokenId with
                        | None, _ ->
                            succeeded (
                                ResumableOpenDto.Ready(
                                    OpenDiffResultDto.NotDiffable(
                                        DiffBlockerDto.EncodingRequired(DiffSideDto.Previous, token, candidates)
                                    )
                                )
                            )
                        | Some _, Some _ -> failedWith "preparation_mismatch" None
                        | Some _, None -> openedPage (diffPage "p1" None [| hunk "h1" |])

                let! state = run fake (select "a.txt") runningState
                let generation = (diffOf state).Generation

                let! state =
                    run
                        fake
                        (diffMsg (GitDiffMsg.ChooseEncoding(generation, DiffSideDto.Previous, "windows-1252")))
                        state

                Vitest.expect(fake.Opens.Count).toBe (3)
                Vitest.expect(fake.Opens.[1].PreparationTokenId).toEqual (Some token.Id)
                Vitest.expect(fake.Opens.[2].PreparationTokenId).toEqual (None)
                Vitest.expect(fake.Opens.[2].PreviousEncoding).toEqual (Some "windows-1252")
                Vitest.expect(fake.Opens.[2].Continuation).toEqual (None)
                Vitest.expect((diffOf state).Status).toEqual (GitDiffPageStatus.Ready)
                Vitest.expect((diffOf state).Handle).toEqual (Some handle)
            }
        )

        Vitest.test (
            "A reopen whose session closes again settles the page as failed without reopening a second time",
            fun () -> promise {
                let fake = FakeDiffClient()

                let pageDto index =
                    diffPage $"p{index}" (Some $"cursor-{index}") [| hunk $"h{index}" |]

                fake.OpenReply <- fun _ -> openedOn (handleOfOpen fake.Opens.Count) (pageDto 1)

                fake.ReadReply <-
                    fun request ->
                        match request.HandleId, request.Cursor with
                        | "diff-1", "cursor-1" -> succeeded (ResumablePageDto.Ready(pageDto 2))
                        | _ -> failedWith "diff_session_closed" None

                let! state = run fake (select "a.txt") runningState
                let generation = (diffOf state).Generation
                let! state = run fake (diffMsg (GitDiffMsg.LoadNext generation)) state
                let! state = run fake (diffMsg (GitDiffMsg.LoadNext generation)) state
                let page = diffOf state

                Vitest.expect(fake.Opens.Count).toBe (2)

                Vitest
                    .expect(fake.Reads |> Seq.map (fun read -> read.HandleId, read.Cursor) |> Seq.toArray)
                    .toEqual (
                        [|
                            "diff-1", "cursor-1"
                            "diff-1", "cursor-2"
                            "diff-2", "cursor-1"
                        |]
                    )

                Vitest.expect(isFailed page.Status).toBe (true)
                Vitest.expect(state.DiffReopen).toEqual (None)
            }
        )

        Vitest.test (
            "Leaving the diff while it reopens cancels the reopen and closes the handle it returns",
            fun () -> promise {
                let fake = FakeDiffClient()
                let first = diffPage "p1" None [| hunk "h1"; gap "g1" |]
                fake.OpenReply <- fun _ -> openedOn (handleOfOpen fake.Opens.Count) first
                fake.ExpandReply <- fun _ -> failedWith "diff_session_closed" None

                let! state = run fake (select "a.txt") runningState

                let expanding, expandCmd =
                    update
                        fake.Dependencies
                        fake.SetPageState
                        (diffMsg (GitDiffMsg.Expand((diffOf state).Generation, "g1", true)))
                        state

                let! expandMessages = collectMessages expandCmd
                let reopeningRef = ref expanding
                let reopenCmds = ResizeArray<Cmd<Msg>>()

                // The commands of the expansion answer hold the reopen and stay unrun until the user left.
                for message in expandMessages do
                    let next, cmd =
                        update fake.Dependencies fake.SetPageState message reopeningRef.Value

                    reopeningRef.Value <- next
                    reopenCmds.Add cmd

                let reopening = reopeningRef.Value
                let reopenId = (diffOf reopening).RunningOperations |> List.head
                Vitest.expect(reopening.DiffReopen.IsSome).toBe (true)

                let! left =
                    run fake (diffMsg (GitDiffMsg.PageStateObserved(Some(PageState.TextPage "notes")))) reopening

                let! lateMessages = collectMessages (Cmd.batch (List.ofSeq reopenCmds))
                let current = ref left

                for message in lateMessages do
                    let! next = run fake message current.Value
                    current.Value <- next

                Vitest.expect(fake.Opens.[1].OperationId).toBe (reopenId)
                Vitest.expect(fake.Cancels.Contains reopenId).toBe (true)
                Vitest.expect(fake.Closes |> Seq.map _.HandleId |> Seq.toArray).toEqual ([| "diff-1"; "diff-2" |])
                Vitest.expect(current.Value.DiffPage).toEqual (None)
            }
        )

        Vitest.test (
            "A reopen keeps the rows on screen and reads forward to the viewed page without evicting its neighborhood",
            fun () -> promise {
                let fake = FakeDiffClient()

                let pageDto index =
                    diffPage $"p{index}" (if index < 10 then Some $"cursor-{index}" else None) [|
                        hunk $"h{index}"
                        gap $"g{index}"
                    |]

                fake.OpenReply <- fun _ -> openedOn (handleOfOpen fake.Opens.Count) (pageDto 1)

                fake.ReadReply <-
                    fun request ->
                        succeeded (ResumablePageDto.Ready(pageDto (int (request.Cursor.Replace("cursor-", "")) + 1)))

                fake.ExpandReply <- fun _ -> failedWith "diff_session_closed" None

                let! state = run fake (select "a.txt") runningState
                let generation = (diffOf state).Generation
                let current = ref state

                for _ in 2..10 do
                    let! next = run fake (diffMsg (GitDiffMsg.LoadNext generation)) current.Value
                    current.Value <- next

                Vitest.expect((diffOf current.Value).Pages.Length).toBe (10)
                let statesBefore = fake.PageStates.Count

                // The user expands a gap on the sixth page after the session expired.
                let! state = run fake (diffMsg (GitDiffMsg.Expand(generation, "g6", true))) current.Value
                let page = diffOf state

                let shownWhileReopening =
                    fake.PageStates
                    |> Seq.skip statesBefore
                    |> Seq.choose (
                        function
                        | Some(PageState.GitDiffPage shown) ->
                            match shown.Status with
                            | GitDiffPageStatus.Reopening _ -> Some shown
                            | _ -> None
                        | _ -> None
                    )
                    |> Seq.toArray

                Vitest.expect(shownWhileReopening.Length).toBeGreaterThan (0)

                for shown in shownWhileReopening do
                    Vitest.expect(shown.RequestedPageIndex).toBe (5)
                    Vitest.expect(shown.Pages.Length).toBe (10)

                    Vitest
                        .expect([| for index in 4..6 -> shown.Pages.[index].IsEvicted |])
                        .toEqual ([| false; false; false |])

                Vitest.expect(fake.Replays.Count).toBe (0)
                Vitest.expect(fake.Opens.Count).toBe (2)
                Vitest.expect(page.Handle).toEqual (Some(handleOfOpen 2))
                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)
                Vitest.expect(page.RequestedPageIndex).toBe (5)
                Vitest.expect(page.Pages |> Array.map _.PageId).toEqual ([| for index in 1..6 -> $"p{index}" |])
                Vitest.expect(pageParts page.Pages.[5]).toEqual ([| "hunk:h6"; "gap:g6" |])
                Vitest.expect(state.DiffReopen).toEqual (None)
            }
        )

        Vitest.test (
            "An abandoned Scanning open whose answer carries a handle closes that handle",
            fun () -> promise {
                let fake = FakeDiffClient()

                fake.OpenReply <-
                    fun request ->
                        match request.Continuation with
                        | None -> succeeded (ResumableOpenDto.Scanning(progress false, "open-continuation", None))
                        | Some _ -> openedPage (diffPage "p1" None [| hunk "h1" |])

                let opening, openCmd =
                    update fake.Dependencies fake.SetPageState (select "a.txt") runningState

                let! left = run fake (diffMsg (GitDiffMsg.PageStateObserved(Some(PageState.TextPage "notes")))) opening
                let! lateMessages = collectMessages openCmd
                let current = ref left

                for message in lateMessages do
                    let! next = run fake message current.Value
                    current.Value <- next

                do! Promise.sleep 0

                Vitest
                    .expect(
                        fake.Opens
                        |> Seq.filter (fun request -> request.Continuation = Some "open-continuation")
                        |> Seq.length
                    )
                    .toBe (1)

                Vitest.expect(fake.Closes |> Seq.map _.HandleId |> Seq.toArray).toEqual ([| handle.Id |])
                Vitest.expect(current.Value.DiffPage).toEqual (None)
            }
        )

        Vitest.test (
            "A diff answer that arrives after the user went to another page leaves that page shown",
            fun () -> promise {
                let fake = FakeDiffClient()
                let! state = openFirstPage fake (diffPage "p1" (Some "cursor-1") [| hunk "h1" |])
                fake.ReadReply <- fun _ -> succeeded (ResumablePageDto.Ready(diffPage "p2" None [| hunk "h2" |]))

                let loading, loadCmd =
                    update
                        fake.Dependencies
                        fake.SetPageState
                        (diffMsg (GitDiffMsg.LoadNext (diffOf state).Generation))
                        state

                let! pageMessages = collectMessages loadCmd
                // Another part of the app shows a text page before the workflow observes it.
                fake.SetPageState(Some(PageState.TextPage "notes"))
                let current = ref loading

                for message in pageMessages do
                    let! next = run fake message current.Value
                    current.Value <- next

                Vitest.expect((diffOf current.Value).Pages.Length).toBe (2)
                Vitest.expect(fake.CurrentPageState).toEqual (Some(PageState.TextPage "notes"))
            }
        )

        Vitest.test (
            "Two failed requests and a late page on the old handle during a reopen lead to one reopen without old parts",
            fun () -> promise {
                let fake = FakeDiffClient()

                let first =
                    diffPage "p1" (Some "cursor-1") [| hunk "h1"; gap "g1"; hunk "h2"; gap "g2" |]

                fake.OpenReply <- fun _ -> openedOn (handleOfOpen fake.Opens.Count) first
                fake.ExpandReply <- fun _ -> failedWith "diff_session_closed" None
                fake.ReadReply <- fun _ -> succeeded (ResumablePageDto.Ready(diffPage "p2" None [| hunk "old-h3" |]))

                let! state = run fake (select "a.txt") runningState
                let generation = (diffOf state).Generation

                let step msg state =
                    update fake.Dependencies fake.SetPageState (diffMsg msg) state

                let expandingFirst, firstCmd =
                    step (GitDiffMsg.Expand(generation, "g1", true)) state

                let expandingBoth, secondCmd =
                    step (GitDiffMsg.Expand(generation, "g2", true)) expandingFirst

                let loading, readCmd = step (GitDiffMsg.LoadNext generation) expandingBoth
                let! firstFailure = collectMessages firstCmd
                let! secondFailure = collectMessages secondCmd
                let! latePage = collectMessages readCmd

                // The first failure starts the reopen, whose open stays unanswered while the
                // other answers of the old handle arrive.
                let reopening, reopenCmd =
                    update fake.Dependencies fake.SetPageState firstFailure.[0] loading

                let current = ref reopening

                for message in Array.append secondFailure latePage do
                    let! next = run fake message current.Value
                    current.Value <- next

                let! reopenMessages = collectMessages reopenCmd

                for message in reopenMessages do
                    let! next = run fake message current.Value
                    current.Value <- next

                let page = diffOf current.Value
                Vitest.expect(fake.Opens.Count).toBe (2)
                Vitest.expect(page.Handle).toEqual (Some(handleOfOpen 2))
                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)

                Vitest
                    .expect(page.Pages |> Array.map pageParts)
                    .toEqual ([| [| "hunk:h1"; "gap:g1"; "hunk:h2"; "gap:g2" |] |])

                Vitest.expect(current.Value.DiffReopen).toEqual (None)
            }
        )

        Vitest.test (
            "An expired session during a replay reopens the diff up to the replayed page",
            fun () -> promise {
                let fake = FakeDiffClient()

                let pageDto index =
                    diffPage $"p{index}" (Some $"cursor-{index}") [| hunk $"h{index}" |]

                fake.OpenReply <- fun _ -> openedOn (handleOfOpen fake.Opens.Count) (pageDto 1)

                fake.ReadReply <-
                    fun request ->
                        succeeded (ResumablePageDto.Ready(pageDto (int (request.Cursor.Replace("cursor-", "")) + 1)))

                fake.ReplayReply <- fun _ -> failedWith "diff_session_closed" None

                let! state = run fake (select "a.txt") runningState
                let generation = (diffOf state).Generation
                let current = ref state

                for _ in 2..9 do
                    let! next = run fake (diffMsg (GitDiffMsg.LoadNext generation)) current.Value
                    current.Value <- next

                Vitest.expect((diffOf current.Value).RequestedPageIndex).toBe (8)
                Vitest.expect((diffOf current.Value).Pages.[0].IsEvicted).toBe (true)
                let readsBefore = fake.Reads.Count

                let! state = run fake (diffMsg (GitDiffMsg.Replay(generation, "p1", [ "p1" ]))) current.Value
                let page = diffOf state

                Vitest.expect(fake.Opens.Count).toBe (2)
                Vitest.expect(fake.Reads.Count).toBe (readsBefore)
                Vitest.expect(page.Handle).toEqual (Some(handleOfOpen 2))
                Vitest.expect(page.Pages |> Array.map _.PageId).toEqual ([| "p1" |])
                Vitest.expect(pageParts page.Pages.[0]).toEqual ([| "hunk:h1" |])
                Vitest.expect(page.RequestedPageIndex).toBe (0)
                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)
            }
        )

        Vitest.test (
            "An expired session during a line slice reopens the diff up to the page that shows the line",
            fun () -> promise {
                let fake = FakeDiffClient()

                let pageDto (index: int) =
                    let row = {
                        changedRow $"r{index}" with
                            Current =
                                Some {
                                    textLine (index * 10) "abc" with
                                        Slice = {
                                            OffsetUtf16 = "0"
                                            TotalUtf16 = None
                                            Text = "abc"
                                            Highlights = [||]
                                        }
                                }
                    }

                    diffPage $"p{index}" (Some $"cursor-{index}") [| hunkWith $"h{index}" [| row |] |]

                fake.OpenReply <- fun _ -> openedOn (handleOfOpen fake.Opens.Count) (pageDto 1)

                fake.ReadReply <-
                    fun request ->
                        succeeded (ResumablePageDto.Ready(pageDto (int (request.Cursor.Replace("cursor-", "")) + 1)))

                fake.LineReply <- fun _ -> failedWith "diff_session_closed" None

                let! state = run fake (select "a.txt") runningState
                let generation = (diffOf state).Generation
                let current = ref state

                for _ in 2..4 do
                    let! next = run fake (diffMsg (GitDiffMsg.LoadNext generation)) current.Value
                    current.Value <- next

                Vitest.expect((diffOf current.Value).RequestedPageIndex).toBe (3)

                // Line 20 is on the second page.
                let! state =
                    run
                        fake
                        (diffMsg (GitDiffMsg.LoadLineSlice(generation, DiffSideDto.Current, 20.0, 3.0)))
                        current.Value

                let page = diffOf state

                Vitest.expect(fake.Opens.Count).toBe (2)

                Vitest
                    .expect(
                        fake.Reads
                        |> Seq.filter (fun read -> read.HandleId = "diff-2")
                        |> Seq.map _.Cursor
                        |> Seq.toArray
                    )
                    .toEqual ([| "cursor-1" |])

                Vitest.expect(page.Pages |> Array.map _.PageId).toEqual ([| "p1"; "p2" |])
                Vitest.expect(page.RequestedPageIndex).toBe (1)
                Vitest.expect(page.Status).toEqual (GitDiffPageStatus.Ready)
            }
        )

        Vitest.test (
            "Line slices merged past 8 MiB evict the page farthest from the line",
            fun () -> promise {
                let fake = FakeDiffClient()

                let longRow = {
                    changedRow "r1" with
                        Current =
                            Some {
                                textLine 0 "abc" with
                                    Slice = {
                                        OffsetUtf16 = "0"
                                        TotalUtf16 = None
                                        Text = "abc"
                                        Highlights = [||]
                                    }
                            }
                }

                let! state = openFirstPage fake (diffPage "p1" (Some "cursor-1") [| hunkWith "h1" [| longRow |] |])
                fake.ReadReply <- fun _ -> succeeded (ResumablePageDto.Ready(diffPage "p2" None [| hunk "h2" |]))
                let! state = run fake (diffMsg (GitDiffMsg.LoadNext (diffOf state).Generation)) state
                let generation = (diffOf state).Generation
                let sliceLength = 3 * 1024 * 1024

                fake.LineReply <-
                    fun request ->
                        succeeded (
                            ResumableLineDto.Ready {
                                Number = request.Line
                                Ending = LineEndingDto.LF
                                Slice = {
                                    OffsetUtf16 = request.OffsetUtf16
                                    TotalUtf16 = None
                                    Text = String.replicate sliceLength "y"
                                    Highlights = [||]
                                }
                            }
                        )

                let current = ref state

                for step in 0..2 do
                    let offset = 3.0 + float (step * sliceLength)

                    let! next =
                        run
                            fake
                            (diffMsg (GitDiffMsg.LoadLineSlice(generation, DiffSideDto.Current, 0.0, offset)))
                            current.Value

                    current.Value <- next

                let page = diffOf current.Value
                Vitest.expect((currentLine page).Text.Length).toBe (3 + 3 * sliceLength)
                Vitest.expect(page.Pages |> Array.map _.IsEvicted).toEqual ([| false; true |])
            }
        )

        Vitest.test (
            "Expanding a second gap keeps the first one running, and a running gap is not requested again",
            fun () -> promise {
                let fake = FakeDiffClient()
                let! state = openFirstPage fake (diffPage "p1" None [| hunk "h1"; gap "g1"; hunk "h2"; gap "g2" |])
                let generation = (diffOf state).Generation

                fake.ExpandReply <-
                    fun request ->
                        succeeded (
                            ResumablePartsDto.Ready [|
                                DiffPartDto.ExpandedContext(request.GapId, [| changedRow $"{request.GapId}-row" |])
                            |]
                        )

                let step msg state =
                    update fake.Dependencies fake.SetPageState (diffMsg msg) state

                let expandingFirst, firstCmd =
                    step (GitDiffMsg.Expand(generation, "g1", true)) state

                let! _ = collectMessages firstCmd

                let expandingBoth, secondCmd =
                    step (GitDiffMsg.Expand(generation, "g2", false)) expandingFirst

                let! secondAnswer = collectMessages secondCmd

                let repeated, repeatedCmd =
                    step (GitDiffMsg.Expand(generation, "g1", false)) expandingBoth

                let! repeatedAnswer = collectMessages repeatedCmd

                Vitest.expect(fake.Expands |> Seq.map _.GapId |> Seq.toArray).toEqual ([| "g1"; "g2" |])
                Vitest.expect(repeatedAnswer.Length).toBe (0)
                Vitest.expect((diffOf repeated).ExpandingGaps |> List.sort).toEqual ([ "g1"; "g2" ])

                let current = ref repeated

                for message in secondAnswer do
                    let! next = run fake message current.Value
                    current.Value <- next

                Vitest.expect((diffOf current.Value).ExpandingGaps).toEqual ([ "g1" ])
            }
        )

        Vitest.test (
            "Replays of the pages in view never evict one another, so each visible page is replayed once",
            fun () -> promise {
                let fake = FakeDiffClient()

                let rows (index: int) = [|
                    for row in 0..2 ->
                        {
                            changedRow $"p{index}-r{row}" with
                                Current = Some(textLine (index * 3 + row) "new")
                        }
                |]

                let pageDto (index: int) =
                    diffPage $"p{index}" (if index < 20 then Some $"cursor-{index}" else None) [|
                        hunkWith $"h{index}" (rows index)
                    |]

                let! state = openFirstPage fake (pageDto 1)
                let generation = (diffOf state).Generation

                fake.ReadReply <-
                    fun request ->
                        succeeded (ResumablePageDto.Ready(pageDto (int (request.Cursor.Replace("cursor-", "")) + 1)))

                fake.ReplayReply <- fun request -> succeeded (pageDto (int (request.PageId.Replace("p", ""))))
                let current = ref state

                for _ in 2..20 do
                    let! next = run fake (diffMsg (GitDiffMsg.LoadNext generation)) current.Value
                    current.Value <- next

                // The viewport shows the first twelve pages, three rows each.
                let visible = [ for index in 1..12 -> $"p{index}" ]

                let firstVisibleEvicted () =
                    (diffOf current.Value).Pages |> Array.take 12 |> Array.tryFind _.IsEvicted

                Vitest.expect((firstVisibleEvicted ()).IsSome).toBe (true)
                let steps = ref 0

                while steps.Value < 40 && (firstVisibleEvicted ()).IsSome do
                    let pageId = (firstVisibleEvicted ()).Value.PageId
                    let! next = run fake (diffMsg (GitDiffMsg.Replay(generation, pageId, visible))) current.Value
                    current.Value <- next
                    steps.Value <- steps.Value + 1

                let replayed = fake.Replays |> Seq.map _.PageId |> Seq.toArray

                Vitest.expect(replayed.Length).toBeLessThanOrEqual (12)
                Vitest.expect(replayed |> Array.distinct).toEqual (replayed)
                Vitest.expect((firstVisibleEvicted ()).IsNone).toBe (true)
            }
        )
)
