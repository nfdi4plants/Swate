module ElectronCore.VersionControlSessionHostTests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron
open Main
open Main.Bindings.Path
open Main.VersionControl
open Swate.Components.Composite.Authentication.Types
open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions
open Vitest
open ElectronCore.TestHelpers

let private electronMock: obj = import "__electronMock" "electron"

[<Emit("require('node:child_process').execFileSync($0, $1, { cwd: $2, stdio: 'pipe' }).toString()")>]
let private execFile (_file: string) (_args: string[]) (_cwd: string) : string = jsNative

let private git (cwd: string) (args: string list) = execFile "git" (List.toArray args) cwd

let private writeText (filePath: string) (content: string) =
    Main.Bindings.Filesystem.writeFileSync filePath content Main.Bindings.Filesystem.TextEncoding.Utf8

let private noAccounts: DataHubStrategies.DataHubAccountSource = {
    GetState = fun () -> AuthStateDto.Empty
    TryGetTokenForAccount = fun _ -> None
    TryGetTokenForHost = fun _ -> None
}

let private memoryBindings () =
    let mutable content: string option = None

    WorkspaceBindingStore.create
        CaseInsensitive
        (fun () -> content)
        (fun next ->
            content <- Some next
            Ok()
        )

let private createRuntime (settingsRoot: string) : VersionControlRuntime.VersionControlRuntime = {
    Catalog =
        ProviderComposition.createCatalog [
            ProviderComposition.createGitFactory noAccounts
            ProviderComposition.createLakeFsFactory
                (ProviderComposition.lakeFsOptions settingsRoot CaseInsensitive)
                VersionControlService.LakeFs.LakeFsCredentials.unconfigured
        ]
    Bindings = memoryBindings ()
    PathCaseSensitivity = CaseInsensitive
}

let private expectValue (operation: string) (result: OperationResult<'T>) =
    match result with
    | Succeeded outcome
    | PartiallySucceeded(outcome, _) -> outcome.Value
    | Failed failure -> failwith $"{operation} failed ({failure.Code}): {failure.Message}"

let private expectFailure (operation: string) (result: OperationResult<'T>) =
    match result with
    | Failed failure -> failure
    | _ -> failwith $"{operation} unexpectedly succeeded."

let private expectDtoValue (operation: string) (result: Result<OperationResultDto<'T>, exn>) =
    match result with
    | Ok(OperationResultDto.Succeeded outcome)
    | Ok(OperationResultDto.PartiallySucceeded(outcome, _)) -> outcome
    | Ok(OperationResultDto.Failed failure) -> failwith $"{operation} failed ({failure.Code}): {failure.Message}"
    | Error error -> failwith $"{operation} threw: {error.Message}"

let private expectDtoFailure (operation: string) (result: Result<OperationResultDto<'T>, exn>) =
    match result with
    | Ok(OperationResultDto.Failed failure) -> failure
    | Ok _ -> failwith $"{operation} unexpectedly succeeded."
    | Error error -> failwith $"{operation} threw: {error.Message}"

/// A git repository with one published base revision and a local bare remote, plus
/// an unmanaged folder next to it.
type private Fixture = {
    Root: string
    RepoRoot: string
    RemoteRoot: string
    PlainRoot: string
    Runtime: VersionControlRuntime.VersionControlRuntime
    Host: WorkspaceSessionHost.WorkspaceSessionHost
}

let private createFixture () = promise {
    let! root = createTempDirectoryAsync "swate-vc-host-"
    let repoRoot = join [| root; "repo" |]
    let remoteRoot = join [| root; "remote.git" |]
    let plainRoot = join [| root; "plain" |]
    let settingsRoot = join [| root; "settings" |]

    for folder in [ repoRoot; remoteRoot; plainRoot; settingsRoot ] do
        Main.Bindings.Filesystem.mkdirSync folder (Main.Bindings.Filesystem.MkdirOptions(recursive = true))

    git remoteRoot [ "init"; "--bare"; "--initial-branch=main" ] |> ignore
    git repoRoot [ "init"; "--initial-branch=main" ] |> ignore
    git repoRoot [ "config"; "user.name"; "Fixture" ] |> ignore
    git repoRoot [ "config"; "user.email"; "fixture@example.org" ] |> ignore
    writeText (join [| repoRoot; "base.txt" |]) "base\n"
    git repoRoot [ "add"; "base.txt" ] |> ignore
    git repoRoot [ "commit"; "-m"; "base" ] |> ignore
    git repoRoot [ "remote"; "add"; "origin"; remoteRoot ] |> ignore
    git repoRoot [ "push"; "-u"; "origin"; "main" ] |> ignore

    let runtime = createRuntime settingsRoot
    let host = WorkspaceSessionHost.WorkspaceSessionHost(runtime)
    WorkspaceSessionHost.initialize host
    VersionControlRuntime.initialize runtime

    return {
        Root = root
        RepoRoot = repoRoot
        RemoteRoot = remoteRoot
        PlainRoot = plainRoot
        Runtime = runtime
        Host = host
    }
}

let private withFixture (body: Fixture -> JS.Promise<unit>) = promise {
    let! fixture = createFixture ()

    try
        do! body fixture
        do! fixture.Host.CloseAll() |> Async.StartAsPromise
        do! removeDirectoryAsync fixture.Root
    with error ->
        do! fixture.Host.CloseAll() |> Async.StartAsPromise
        do! removeDirectoryAsync fixture.Root
        return raise error
}

let private detached name = OperationContext.detached name

Vitest.describe (
    "Workspace session host",
    fun () ->
        Vitest.test (
            "adopts an unbound git repository, persists the binding and reuses the open session",
            fun () ->
                withFixture (fun fixture -> promise {
                    let! first =
                        fixture.Host.OpenSession(fixture.RepoRoot, detached "open-1")
                        |> Async.StartAsPromise

                    let hosted = expectValue "first open" first

                    Vitest.expect(ProviderId.value hosted.Session.Descriptor.ProviderId).toBe "git"

                    let persisted =
                        fixture.Runtime.Bindings.TryFind fixture.RepoRoot
                        |> Option.defaultWith (fun () -> failwith "Binding was not persisted.")

                    Vitest.expect(ProviderId.value persisted.ProviderId).toBe "git"

                    let! second =
                        fixture.Host.OpenSession(fixture.RepoRoot, detached "open-2")
                        |> Async.StartAsPromise

                    Vitest.expect((expectValue "second open" second).SessionId).toBe hosted.SessionId

                    do! fixture.Host.CloseSession fixture.RepoRoot |> Async.StartAsPromise

                    let! third =
                        fixture.Host.OpenSession(fixture.RepoRoot, detached "open-3")
                        |> Async.StartAsPromise

                    let reopened = expectValue "reopen" third
                    Vitest.expect(reopened.SessionId).not.toBe hosted.SessionId
                    Vitest.expect(reopened.Binding).toEqual persisted
                })
        )

        Vitest.test (
            "a renamed vault closes its session and its binding follows the new root",
            fun () ->
                withFixture (fun fixture -> promise {
                    let! opened =
                        fixture.Host.OpenSession(fixture.RepoRoot, detached "open")
                        |> Async.StartAsPromise

                    let hosted = expectValue "open" opened
                    let renamedRoot = join [| fixture.Root; "renamed" |]

                    do!
                        fixture.Host.WorkspaceRenamed(fixture.RepoRoot, renamedRoot)
                        |> Async.StartAsPromise

                    Vitest.expect(fixture.Host.TryGetSessionById hosted.SessionId).toEqual None
                    Vitest.expect(fixture.Runtime.Bindings.TryFind fixture.RepoRoot).toEqual None

                    let moved =
                        fixture.Runtime.Bindings.TryFind renamedRoot
                        |> Option.defaultWith (fun () -> failwith "Binding did not follow the rename.")

                    Vitest.expect(moved.WorkspaceRoot).toBe renamedRoot
                    Vitest.expect(moved.Location).toEqual hosted.Binding.Location
                })
        )

        Vitest.test (
            "a plain folder is reported as unmanaged with a structured failure",
            fun () ->
                withFixture (fun fixture -> promise {
                    let! opened =
                        fixture.Host.OpenSession(fixture.PlainRoot, detached "open-plain")
                        |> Async.StartAsPromise

                    let failure = expectFailure "open plain" opened
                    Vitest.expect(failure.Category).toEqual NotFound
                    Vitest.expect(failure.Code).toBe VersionControlCodes.WorkspaceUnmanaged
                })
        )

        Vitest.test (
            "operations are cancelable by operation id before any progress and keyed by session",
            fun () ->
                withFixture (fun fixture -> promise {
                    let mutable reported: VersionControlProgressDto list = []

                    let tracked =
                        fixture.Host.BeginOperation(
                            "session-a",
                            "op-1",
                            fun progress -> reported <- progress :: reported
                        )

                    Vitest.expect(fixture.Host.IsIdle "session-a").toBe false
                    Vitest.expect(fixture.Host.Cancel("session-b", "op-1")).toBe false
                    Vitest.expect(tracked.Context.Cancellation.IsCancellationRequested()).toBe false
                    Vitest.expect(fixture.Host.Cancel("", "op-1")).toBe true
                    Vitest.expect(tracked.Context.Cancellation.IsCancellationRequested()).toBe true

                    tracked.Context.ReportProgress {
                        PhaseCode = "transfer"
                        Item = None
                        Completed = Some 1.0
                        Total = Some 2.0
                        DisplayMessage = None
                    }

                    Vitest
                        .expect(reported |> List.map (fun progress -> progress.SessionId, progress.OperationId))
                        .toEqual
                        [ "session-a", "op-1" ]

                    tracked.Complete()
                    Vitest.expect(fixture.Host.IsIdle "session-a").toBe true
                    Vitest.expect(fixture.Host.Cancel("session-a", "op-1")).toBe false
                })
        )

        Vitest.test (
            "a library operation run with a canceled context fails structurally as canceled",
            fun () ->
                withFixture (fun fixture -> promise {
                    let! opened =
                        fixture.Host.OpenSession(fixture.RepoRoot, detached "open")
                        |> Async.StartAsPromise

                    let hosted = expectValue "open" opened
                    let tracked = fixture.Host.BeginOperation(hosted.SessionId, "op-cancel", ignore)
                    fixture.Host.Cancel(hosted.SessionId, "op-cancel") |> ignore

                    let synchronization =
                        hosted.Session.Synchronization
                        |> Option.defaultWith (fun () -> failwith "no sync service")

                    let! refreshed = synchronization.Refresh tracked.Context |> Async.StartAsPromise
                    tracked.Complete()
                    let failure = expectFailure "canceled refresh" refreshed
                    Vitest.expect(failure.Category).toEqual Canceled
                })
        )
)

let private ipcEvent (windowId: int) : IpcMainInvokeEvent =
    createObj [ "sender" ==> createObj [ "id" ==> windowId ] ]
    |> unbox<IpcMainInvokeEvent>

let private registerVault (windowId: int) (arcPath: string) =
    let window = testWindow ()
    window?id <- windowId
    let vault = ArcVault(window)
    vault.path <- Some arcPath
    ARC_VAULTS.Vaults.[windowId] <- vault

    electronMock?setBrowserWindowFromWebContents (fun (webContents: obj) ->
        if unbox<int> webContents?id = windowId then
            box window
        else
            null
    )
    |> ignore

    vault

let private request (operationId: string) : OperationRequestDto = { OperationId = operationId }

Vitest.describe (
    "Version control IPC over a git vault",
    fun () ->
        Vitest.afterEach (fun () ->
            electronMock?reset () |> ignore
            ARC_VAULTS.Vaults.Clear()
        )

        Vitest.test (
            "session info, status, selected revision, publish and refresh run through the package",
            fun () ->
                withFixture (fun fixture -> promise {
                    registerVault 41 fixture.RepoRoot |> ignore
                    let api = Main.IPC.IVersionControlApi.api (ipcEvent 41)

                    let! info = api.getSessionInfo (request "info")
                    let session = (expectDtoValue "session info" info).Value
                    Vitest.expect(session.ProviderId).toBe "git"
                    Vitest.expect(session.Services.Synchronization).toBe true
                    Vitest.expect(session.Services.ConflictResolution).toBe true
                    Vitest.expect(session.WorkspaceRoot.Replace('\\', '/')).toBe (fixture.RepoRoot.Replace('\\', '/'))

                    writeText (join [| fixture.RepoRoot; "assay.txt" |]) "assay\n"
                    writeText (join [| fixture.RepoRoot; "other.txt" |]) "other\n"

                    let! status = api.getStatus (request "status")
                    let statusValue = (expectDtoValue "status" status).Value

                    Vitest.expect(statusValue.Changes |> Array.map _.Path |> Array.sort).toEqual [|
                        "assay.txt"
                        "other.txt"
                    |]

                    let! created =
                        api.createRevision {
                            OperationId = "commit"
                            Message = "Add assay"
                            Paths = [| "assay.txt" |]
                            ExpectedWorkspaceVersion = statusValue.WorkspaceVersion
                        }

                    let revisionOutcome = expectDtoValue "create revision" created
                    Vitest.expect(revisionOutcome.ResultingRevision.IsSome).toBe true
                    Vitest.expect(revisionOutcome.Publication).toEqual PublicationStateDto.LocalOnly

                    let! afterCommit = api.getStatus (request "status-2")
                    let afterCommitValue = (expectDtoValue "status after commit" afterCommit).Value
                    Vitest.expect(afterCommitValue.Changes |> Array.map _.Path).toEqual [| "other.txt" |]

                    let! refreshed = api.refreshSynchronization (request "refresh")
                    let refreshedValue = (expectDtoValue "refresh" refreshed).Value
                    Vitest.expect(refreshedValue.Relationship).toEqual RevisionRelationshipDto.LocalAhead

                    let! published =
                        api.publish {
                            OperationId = "publish"
                            ExpectedWorkspaceVersion = afterCommitValue.WorkspaceVersion
                            ExpectedTargetRevision = refreshedValue.TargetRevision
                        }

                    let publishOutcome = expectDtoValue "publish" published
                    Vitest.expect(publishOutcome.Publication).toEqual PublicationStateDto.Published
                    Vitest.expect(publishOutcome.Value.Relationship).toEqual RevisionRelationshipDto.UpToDate

                    Vitest.expect(git fixture.RemoteRoot [ "log"; "-1"; "--format=%s"; "main" ] |> _.Trim()).toBe
                        "Add assay"
                })
        )

        Vitest.test (
            "a stale workspace version is refused structurally before anything changes",
            fun () ->
                withFixture (fun fixture -> promise {
                    registerVault 42 fixture.RepoRoot |> ignore
                    let api = Main.IPC.IVersionControlApi.api (ipcEvent 42)
                    writeText (join [| fixture.RepoRoot; "assay.txt" |]) "assay\n"

                    let! created =
                        api.createRevision {
                            OperationId = "commit-stale"
                            Message = "Add assay"
                            Paths = [| "assay.txt" |]
                            ExpectedWorkspaceVersion = "stale-version"
                        }

                    let failure = expectDtoFailure "stale create revision" created
                    Vitest.expect(failure.Category).toEqual FailureCategoryDto.Concurrency
                    Vitest.expect(failure.Code).toBe VersionControlCodes.PreconditionFailed
                    Vitest.expect(failure.StateChanged).toBe false
                    Vitest.expect(git fixture.RepoRoot [ "log"; "--format=%s" ] |> _.Trim()).toBe "base"
                })
        )

        Vitest.test (
            "an operation can be canceled right after the call started",
            fun () ->
                withFixture (fun fixture -> promise {
                    registerVault 43 fixture.RepoRoot |> ignore
                    let api = Main.IPC.IVersionControlApi.api (ipcEvent 43)

                    let running =
                        api.update {
                            OperationId = "update-cancel"
                            ExpectedWorkspaceVersion = "any"
                        }

                    let! canceled =
                        api.cancelOperation {
                            SessionId = ""
                            OperationId = "update-cancel"
                        }

                    Vitest.expect(canceled).toEqual (Ok true)

                    let! result = running
                    let failure = expectDtoFailure "canceled update" result
                    Vitest.expect(failure.Category).toEqual FailureCategoryDto.Canceled

                    let! unknown =
                        api.cancelOperation {
                            SessionId = ""
                            OperationId = "never-started"
                        }

                    Vitest.expect(unknown).toEqual (Ok false)
                })
        )

        Vitest.test (
            "a stale index lock is removed only while the session is idle, then the status is refreshed",
            fun () ->
                withFixture (fun fixture -> promise {
                    registerVault 44 fixture.RepoRoot |> ignore
                    let api = Main.IPC.IVersionControlApi.api (ipcEvent 44)
                    let lockPath = join [| fixture.RepoRoot; ".git"; "index.lock" |]
                    writeText lockPath ""

                    let! cleared = api.clearStaleLock (request "clear-lock")
                    let outcome = expectDtoValue "clear lock" cleared
                    Vitest.expect(outcome.AffectedPaths).toEqual [| lockPath |]
                    Vitest.expect(Main.Bindings.Filesystem.existsSync lockPath).toBe false
                    Vitest.expect(outcome.Value.ActiveConflictSession).toEqual None

                    let! again = api.clearStaleLock (request "clear-lock-2")
                    let noOp = expectDtoValue "clear lock again" again
                    Vitest.expect(noOp.AffectedPaths).toEqual [||]

                    let session =
                        fixture.Host.TryGetSession fixture.RepoRoot
                        |> Option.defaultWith (fun () -> failwith "session missing")

                    let other = fixture.Host.BeginOperation(session.SessionId, "busy", ignore)
                    let! refused = api.clearStaleLock (request "clear-lock-3")
                    other.Complete()
                    let failure = expectDtoFailure "refused lock removal" refused
                    Vitest.expect(failure.Code).toBe VersionControlCodes.LockRemovalRefused
                    Vitest.expect(failure.Retryable).toBe true
                })
        )

        Vitest.test (
            "an unmanaged vault folder reports a structured not found failure",
            fun () ->
                withFixture (fun fixture -> promise {
                    registerVault 45 fixture.PlainRoot |> ignore
                    let api = Main.IPC.IVersionControlApi.api (ipcEvent 45)
                    let! info = api.getSessionInfo (request "info-plain")
                    let failure = expectDtoFailure "plain session info" info
                    Vitest.expect(failure.Category).toEqual FailureCategoryDto.NotFound
                    Vitest.expect(failure.Code).toBe VersionControlCodes.WorkspaceUnmanaged
                })
        )

        Vitest.test (
            "initialize creates a bound git workspace and clone into a non-empty folder is refused",
            fun () ->
                withFixture (fun fixture -> promise {
                    let api = Main.IPC.IVersionControlApi.api (ipcEvent 46)
                    let freshRoot = join [| fixture.Root; "fresh" |]
                    writeText (join [| fixture.PlainRoot; "keep.txt" |]) "keep\n"

                    let! initialized =
                        api.initializeWorkspace {
                            OperationId = "init"
                            TargetPath = fixture.PlainRoot
                        }

                    let initializedRoot = (expectDtoValue "initialize" initialized).Value
                    Vitest.expect(initializedRoot.Replace('\\', '/')).toBe (fixture.PlainRoot.Replace('\\', '/'))

                    Vitest
                        .expect(
                            fixture.Runtime.Bindings.TryFind fixture.PlainRoot
                            |> Option.map (fun binding -> ProviderId.value binding.ProviderId)
                        )
                        .toEqual (Some "git")

                    Vitest.expect(Main.Bindings.Filesystem.existsSync (join [| fixture.PlainRoot; "keep.txt" |])).toBe
                        true

                    let! refused =
                        api.cloneWorkspace {
                            OperationId = "clone-refused"
                            ProviderLocation = "ftp://example.org/repo.git"
                            DisplayName = None
                            TargetPath = freshRoot
                            TargetRef = None
                            MaterializeAllObjects = false
                        }

                    let locationFailure = expectDtoFailure "unsupported clone" refused
                    Vitest.expect(locationFailure.Code).toBe VersionControlCodes.LocationUnsupported

                    let! cloned =
                        api.cloneWorkspace {
                            OperationId = "clone"
                            ProviderLocation = "https://example.invalid/never/reached.git"
                            DisplayName = Some "reached"
                            TargetPath = fixture.PlainRoot
                            TargetRef = None
                            MaterializeAllObjects = false
                        }

                    let cloneFailure = expectDtoFailure "clone into non-empty" cloned
                    Vitest.expect(cloneFailure.Code).toBe VersionControlCodes.TargetNotEmpty
                })
        )
)
