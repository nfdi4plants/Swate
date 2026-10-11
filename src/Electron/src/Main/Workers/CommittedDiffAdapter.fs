/// Supplies committed current-side files to the existing workspace diff worker.
/// All classification, paging, storage budgets and handle operations remain in the library.
module Main.Workers.CommittedDiffAdapter

open System
open System.Collections.Generic
open Fable.Core
open VersionControlService.Abstractions
open VersionControlService.Git.TextDiff.TextDiffProtocol
open VersionControlService.Git.TextDiff.TextDiffWorkerDispatcher

module Sources = Main.VersionControl.CommittedDiffSources
module Worker = VersionControlService.Git.TextDiff.TextDiffWorker
module Fs = VersionControlService.Runtime.Node.FileSystem
module Files = VersionControlService.Runtime.Node.PositionalFile
module Path = VersionControlService.Runtime.Node.Path
module Interop = VersionControlService.Runtime.Node.Interop
module Supervisor = VersionControlService.Git.TextDiff.TextDiffSupervisor
module Resolver = VersionControlService.Git.TextDiff.TextDiffSourceResolver

exception private CommittedObjectUnavailable of string

[<AllowNullLiteral>]
type private Timer =
    abstract unref: unit -> unit

[<Import("setTimeout", "node:timers")>]
let private setTimeout (callback: unit -> unit) (milliseconds: int) : Timer = jsNative

type private Snapshot = {
    Selection: Sources.Selection
    RepositoryRoot: string
    Directory: string
    RelativePath: RepositoryPath
    PreviousPath: RepositoryPath option
    mutable KeepUntil: DateTime
}

type Adapter() =
    let handler = Worker.createDefaultHandler ()

    let snapshots =
        Dictionary<string * string * string * RepositoryPath option, Snapshot>()

    let sessions = Dictionary<int, Snapshot>()
    let pendingSnapshots = Dictionary<string, WorkerHost>()

    let active snapshot =
        sessions.Values |> Seq.exists (fun item -> obj.ReferenceEquals(item, snapshot))

    let collect () =
        for KeyValue(key, snapshot) in snapshots |> Seq.toArray do
            if not (active snapshot) && snapshot.KeepUntil <= DateTime.UtcNow then
                snapshots.Remove key |> ignore
                // Only adapter-created UUID folders beneath the supervisor's worker directory.
                Fs.rmAsync snapshot.Directory (Fs.RmOptions(recursive = true, force = true))
                |> Promise.catch (fun _ -> ())
                |> Promise.start

    let short (host: WorkerHost) root args = promise {
        let! result = host.SpawnShort(root, args)

        if host.IsCanceled() then
            return raise (OperationCanceledException())
        elif result.ExitCode <> Some 0 || result.Error.IsSome then
            match args with
            | [| "cat-file"; "-s"; oid |] when result.Error.IsNone && Resolver.stderrReportsMissingObject result.Stderr ->
                return raise (CommittedObjectUnavailable oid)
            | _ -> return failwith (result.Error |> Option.defaultValue result.Stderr)
        else
            return Text.Encoding.UTF8.GetString result.Stdout
    }

    let materializeSnapshot
        (host: WorkerHost)
        (request: OpenDiffRequest)
        (owner: TextDiffOwner)
        (selection: Sources.Selection)
        =
        promise {
            let folder = "history-" + Interop.randomUuid ()
            let directory = Path.join [| host.TempDirectory; folder |]
            do! Files.mkdirRecursive directory

            try
                let! listing =
                    short host owner.WorkspaceRoot [|
                        "ls-tree"
                        "-z"
                        "--full-tree"
                        selection.Revision
                        "--"
                        selection.Path
                    |]

                let entries = listing.Split('\000') |> Array.filter (fun value -> value.Length > 0)

                if entries.Length > 1 then
                    failwith "The committed diff path does not identify one file."

                if entries.Length = 1 then
                    let separator = entries[0].IndexOf '\t'

                    let header =
                        if separator > 0 then
                            entries[0].Substring(0, separator).Split(' ')
                        else
                            [||]

                    if header.Length <> 3 || entries[0].Substring(separator + 1) <> selection.Path then
                        failwith "Git returned an invalid committed file entry."
                    elif header[0] <> "100644" && header[0] <> "100755" then
                        // Let the stock resolver check Previous first and classify this
                        // nonregular Current source with the same blocker precedence.
                        do! Files.mkdirRecursive (Path.join [| directory; "current" |])
                    elif header[1] <> "blob" || not (Sources.validRevision header[2]) then
                        failwith "The committed file is not a valid Git blob."
                    else
                        let! sizeText = short host owner.WorkspaceRoot [| "cat-file"; "-s"; header[2] |]
                        let size = Int64.Parse(sizeText.Trim())
                        let! free = Files.tryFreeBytes host.TempDirectory
                        // Leave a small reserve for the snapshot itself. The stock handler still
                        // decides disk versus memory using the configured policy after this read.
                        let reserve = 16_777_216L

                        if free |> Option.exists (fun bytes -> bytes - float size < float reserve) then
                            failwith "There is not enough temporary disk space for the committed file snapshot."

                        if host.IsCanceled() then
                            return raise (OperationCanceledException())

                        let spool =
                            Path.join [|
                                host.TempDirectory
                                "history-spool-" + Interop.randomUuid ()
                            |]

                        let! exit = host.SpawnBlob(owner.WorkspaceRoot, header[2], spool)

                        if host.IsCanceled() then
                            return raise (OperationCanceledException())

                        if exit.ExitCode <> Some 0 || exit.Signal.IsSome || exit.SpawnError.IsSome then
                            failwith (exit.SpawnError |> Option.defaultValue exit.Stderr)

                        let! stat = Files.lstat spool

                        if stat.Size <> size then
                            failwith "Git returned an incomplete committed file snapshot."

                        // Keep the exact identity across encoding-token retries, which outlive a session.
                        // The supervisor still owns the worker directory and removes it on worker exit.
                        do! Fs.renameAsync spool (Path.join [| directory; "current" |])

                let relative =
                    RepositoryPath.tryCreate (folder + "/current") |> Result.defaultWith failwith

                return {
                    Selection = selection
                    RepositoryRoot = owner.WorkspaceRoot
                    Directory = directory
                    RelativePath = relative
                    PreviousPath = request.PreviousPath
                    KeepUntil = DateTime.UtcNow.AddMinutes 5.
                }
            with error ->
                do! Fs.rmAsync directory (Fs.RmOptions(recursive = true, force = true))
                return raise error
        }

    let createSnapshot (host: WorkerHost) request owner selection = promise {
        pendingSnapshots[host.RequestId] <- host

        try
            return! materializeSnapshot host request owner selection
        finally
            pendingSnapshots.Remove host.RequestId |> ignore
    }

    let mapInfos snapshot (previous: DiffSourceInfo) (current: DiffSourceInfo) =
        let path =
            RepositoryPath.tryCreate snapshot.Selection.Path |> Result.defaultWith failwith

        let revision =
            RevisionId.tryCreate snapshot.Selection.Revision |> Result.defaultWith failwith

        let parent =
            snapshot.Selection.ParentRevision
            |> Option.map (fun value -> RevisionId.tryCreate value |> Result.defaultWith failwith)

        {
            previous with
                Path = snapshot.PreviousPath |> Option.defaultValue path
                Revision = parent
        },
        {
            current with
                Path = path
                Revision = Some revision
        }

    let mapOpen snapshot result =
        match result with
        | Resumable.Ready(OpenDiffResult.Opened(handle, previous, current, first, storage)) ->
            let previous, current = mapInfos snapshot previous current
            Resumable.Ready(OpenDiffResult.Opened(handle, previous, current, first, storage))
        | _ -> result

    member _.Handler =
        { new ITextDiffRequestHandler with
            member _.Open(host, request, owner) = promise {
                match Sources.tryDecode (RepositoryPath.value request.Path) with
                | None ->
                    let path =
                        RepositoryPath.value request.Path
                        |> Sources.decodeWorkspacePath
                        |> RepositoryPath.tryCreate
                        |> Result.defaultWith failwith

                    return! handler.Open(host, { request with Path = path }, owner)
                | Some selection ->
                    collect ()

                    try
                        let key =
                            owner.WorkspaceRoot,
                            owner.WindowOwner,
                            RepositoryPath.value request.Path,
                            request.PreviousPath

                        let! snapshot =
                            match snapshots.TryGetValue key with
                            | true, cached -> Promise.lift cached
                            | _ -> promise {
                                let! snapshot = createSnapshot host request owner selection
                                snapshots[key] <- snapshot
                                return snapshot
                              }

                        sessions[host.Generation] <- snapshot

                        let! result =
                            handler.Open(
                                host,
                                {
                                    request with
                                        Path = snapshot.RelativePath
                                },
                                {
                                    owner with
                                        WorkspaceRoot = host.TempDirectory
                                }
                            )

                        let keepsPreparation =
                            match result with
                            | Ok(Resumable.Ready(OpenDiffResult.NotDiffable(DiffBlocker.EncodingRequired _))) -> true
                            | _ -> false

                        if keepsPreparation then
                            snapshot.KeepUntil <- DateTime.UtcNow.AddMinutes 5.
                            (setTimeout collect 300001).unref ()
                        else
                            snapshot.KeepUntil <- DateTime.MinValue

                        match result with
                        | Error _
                        | Ok(Resumable.Ready(OpenDiffResult.NotDiffable _)) -> sessions.Remove host.Generation |> ignore
                        | _ -> ()

                        collect ()
                        return result |> Result.map (mapOpen snapshot)
                    with
                    | _ when host.IsCanceled() -> return Error(canceledFailure ())
                    | :? OperationCanceledException -> return Error(canceledFailure ())
                    | CommittedObjectUnavailable oid ->
                        return
                            Ok(
                                Resumable.Ready(
                                    OpenDiffResult.NotDiffable(
                                        DiffBlocker.LocalContentUnavailable(DiffSide.Current, Some oid)
                                    )
                                )
                            )
                    | error ->
                        return
                            Error(OperationFailure.create ProviderError TextDiffFailureCodes.ReadFailed error.Message)
            }

            member _.ReadPage(host, request) = handler.ReadPage(host, request)
            member _.ReplayPage(host, request) = handler.ReplayPage(host, request)
            member _.Expand(host, request) = handler.Expand(host, request)
            member _.ReadLine(host, request) = handler.ReadLine(host, request)

            member _.GetSourceInfo(host, request) = promise {
                let! result = handler.GetSourceInfo(host, request)

                match sessions.TryGetValue host.Generation with
                | true, snapshot ->
                    return
                        result
                        |> Result.map (fun (previous, current) -> mapInfos snapshot previous current)
                | _ -> return result
            }

            member _.Close(host, handle) = handler.Close(host, handle)

            member _.Cancel requestId =
                match pendingSnapshots.TryGetValue requestId with
                | true, host -> host.ReleaseRequest()
                | _ -> ()

                handler.Cancel requestId
        }

    /// Rewrites only source-resolution commands. Output limits and all lifecycle messages
    /// are preserved, so memory sessions retain the same blob limits as workspace diffs.
    member _.Post(message: obj, forward: obj -> unit, reply: obj -> unit) =
        let snapshotOf (sessionId: string) =
            match Int32.TryParse sessionId with
            | true, generation ->
                match sessions.TryGetValue generation with
                | true, snapshot -> Some snapshot
                | _ -> None
            | _ -> None

        let answer callId exitCode (stdout: string) =
            TextDiffMessage.SpawnResult(
                callId,
                SpawnOutcome.Short {
                    ExitCode = Some exitCode
                    Stdout = Text.Encoding.UTF8.GetBytes stdout
                    Stderr = ""
                    Error = None
                }
            )
            |> encode
            |> reply

        match decodeMessage message with
        | Ok(TextDiffMessage.SpawnShort(callId, owner, _, arguments, outputLimit)) when
            snapshotOf owner.SessionId |> Option.isSome
            ->
            let snapshot = snapshotOf owner.SessionId |> Option.get

            match arguments, snapshot.Selection.ParentRevision with
            | [| "rev-parse"; "--verify"; "--quiet"; "HEAD^{commit}" |], Some parent ->
                TextDiffMessage.SpawnShort(
                    callId,
                    owner,
                    snapshot.RepositoryRoot,
                    [|
                        "rev-parse"
                        "--verify"
                        "--quiet"
                        parent + "^{commit}"
                    |],
                    outputLimit
                )
                |> encode
                |> forward
            | [| "rev-parse"; "--verify"; "--quiet"; "HEAD^{commit}" |], None -> answer callId 1 ""
            | [| "symbolic-ref"; "-q"; "HEAD" |], None -> answer callId 0 "refs/heads/swate-empty-history\n"
            | [| "for-each-ref"; "--format=%(refname)"; "--"; "refs/heads/swate-empty-history" |], None ->
                answer callId 0 ""
            | _ ->
                TextDiffMessage.SpawnShort(callId, owner, snapshot.RepositoryRoot, arguments, outputLimit)
                |> encode
                |> forward
        | Ok(TextDiffMessage.SpawnBlob(callId, owner, _, oid, spool)) when snapshotOf owner.SessionId |> Option.isSome ->
            let snapshot = snapshotOf owner.SessionId |> Option.get

            TextDiffMessage.SpawnBlob(callId, owner, snapshot.RepositoryRoot, oid, spool)
            |> encode
            |> forward
        | Ok(TextDiffMessage.ReleaseSession(_, sessionId)) ->
            match Int32.TryParse sessionId with
            | true, generation -> sessions.Remove generation |> ignore
            | _ -> ()

            forward message
            collect ()
        | _ -> forward message
