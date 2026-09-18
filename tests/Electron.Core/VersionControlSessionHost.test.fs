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
            "concurrent opens of one root share a single session",
            fun () ->
                withFixture (fun fixture -> promise {
                    let first =
                        fixture.Host.OpenSession(fixture.RepoRoot, detached "open-a")
                        |> Async.StartAsPromise

                    let second =
                        fixture.Host.OpenSession(fixture.RepoRoot, detached "open-b")
                        |> Async.StartAsPromise

                    let! firstResult = first
                    let! secondResult = second
                    let firstSession = expectValue "first open" firstResult
                    let secondSession = expectValue "second open" secondResult
                    Vitest.expect(secondSession.SessionId).toBe firstSession.SessionId
                    Vitest.expect(fixture.Host.RunningOperationIds firstSession.SessionId).toEqual [||]
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

                    let! moved =
                        fixture.Host.WorkspaceRenamed(fixture.RepoRoot, renamedRoot)
                        |> Async.StartAsPromise

                    Vitest.expect(moved).toEqual (Ok())

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

                    // An operation whose session is not known yet counts as busy for every
                    // session and can be canceled with any session id.
                    let unassigned = fixture.Host.BeginOperation("", "op-unassigned", ignore)
                    Vitest.expect(fixture.Host.IsIdle "session-b").toBe false
                    Vitest.expect(fixture.Host.Cancel("session-b", "op-unassigned")).toBe true
                    unassigned.Complete()
                    Vitest.expect(fixture.Host.IsIdle "session-b").toBe true
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

/// Restores of the fake core provider wait on these gates, keyed by the first path.
let private restoreGates =
    System.Collections.Generic.Dictionary<string, JS.Promise<unit>>()

[<Emit("(() => { let resolve; const promise = new Promise((r) => { resolve = r; }); return [promise, resolve]; })()")>]
let private deferred () : JS.Promise<unit> * (unit -> unit) = jsNative

/// Two fake providers: "fake.core" owns coreOnlyRoot and opens a core-only session,
/// "fake.broken" owns failingRoot, fails to open and reports a failing dependency check.
let private fakeProviderRuntime
    (coreOnlyRoot: string)
    (failingRoot: string)
    : VersionControlRuntime.VersionControlRuntime =
    let providerId id =
        ProviderId.tryCreate id |> Result.defaultWith failwith

    let unsupported () = async { return Failed(OperationFailure.create Unsupported "operation_not_supported" "fake") }

    let bindingFor id root : WorkspaceBinding = {
        SchemaVersion = WorkspaceBinding.CurrentSchemaVersion
        ProviderId = providerId id
        WorkspaceRoot = root
        ProviderStateRef = None
        Location = {
            ProviderId = providerId id
            DisplayName = None
            ProviderLocation = root
            ConnectionProfileId = None
        }
        ConnectionProfileId = None
    }

    let coreStatus: WorkspaceStatus = {
        CurrentRef = None
        WorkspaceVersion = "v1"
        Changes = [||]
        ActiveConflictSession = None
        Synchronization = None
    }

    let core: CoreVersionControl = {
        GetStatus = fun _ -> async { return OperationResult.succeeded coreStatus }
        ListRefs = fun _ -> async { return OperationResult.succeeded [||] }
        CreateRef = fun _ _ -> unsupported ()
        PreflightSwitchRef = fun _ _ -> unsupported ()
        SwitchRef = fun _ _ -> unsupported ()
        CreateRevision = fun _ _ -> unsupported ()
        RestorePaths =
            fun restoreRequest _ -> async {
                // A restore waits until the test releases its first path, so two
                // restores can overlap and the busy flag can be observed in between.
                match restoreRequest.Paths |> Array.tryHead |> Option.map RepositoryPath.value with
                | Some path when restoreGates.ContainsKey path ->
                    do! Async.AwaitPromise restoreGates.[path]
                    return OperationResult.succeeded ()
                | _ -> return OperationResult.succeeded ()
            }
        GetDiffSummary = fun _ -> async { return OperationResult.succeeded { Entries = [||] } }
    }

    let factory
        id
        root
        (openResult: WorkspaceBinding -> OperationResult<WorkspaceSession>)
        (dependencies: OperationResult<DependencyStatus[]>)
        : ProviderFactory =
        {
            Id = providerId id
            Probe =
                fun path -> async {
                    // The resolver probes with a normalized path; the fixture root is a native path.
                    let detected = WorkspaceBindingStore.rootsEqual CaseInsensitive path root

                    return if detected then Detected(root, 50, None) else NotDetected
                }
            VerifyLocation = fun _ _ -> unsupported ()
            Initialize = fun _ _ -> unsupported ()
            Clone = fun _ _ -> unsupported ()
            Adopt =
                fun adoptRequest _ -> async {
                    return OperationResult.succeeded (bindingFor id adoptRequest.WorkspaceRoot)
                }
            Bind =
                fun bindRequest _ -> async {
                    // A partial bind: the location is retargeted, but the provider reports a
                    // failure of a later step, so the host must still reopen the session.
                    let binding = {
                        bindingFor id bindRequest.WorkspaceRoot with
                            Location = bindRequest.Location
                    }

                    return
                        PartiallySucceeded(
                            OperationOutcome.performed binding,
                            {
                                OperationFailure.create ProviderError "fake_bind_partial" "The fake fetch failed." with
                                    StateChanged = true
                            }
                        )
                }
            Open = fun binding _ -> async { return openResult binding }
            CheckDependencies = fun _ -> async { return dependencies }
            InstallDependency = fun _ _ -> unsupported ()
        }

    // Registered under the git id so an https location resolves to it.
    let coreFactory =
        factory
            "git"
            coreOnlyRoot
            (fun binding ->
                OperationResult.succeeded (
                    WorkspaceSession.createCoreOnly
                        {
                            ProviderId = binding.ProviderId
                            WorkspaceRoot = binding.WorkspaceRoot
                            Location = Some binding.Location
                        }
                        core
                )
            )
            (OperationResult.succeeded [|
                {
                    Component = "fake-tool"
                    Installed = true
                    Version = Some "1.0"
                    Compatible = true
                    Remediation = None
                }
            |])

    let brokenFactory =
        factory
            "fake.broken"
            failingRoot
            (fun _ -> Failed(OperationFailure.create DependencyMissing "tool_missing" "The fake tool is missing."))
            (Failed(OperationFailure.create ProviderError "check_failed" "The fake check failed."))

    {
        Catalog = ProviderComposition.createCatalog [ coreFactory; brokenFactory ]
        Bindings = memoryBindings ()
        PathCaseSensitivity = CaseInsensitive
    }

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
                    Vitest.expect(outcome.Effect).toEqual OperationEffectDto.Performed
                    Vitest.expect(outcome.AffectedPaths).toEqual [||]
                    Vitest.expect(outcome.Warnings |> Array.map _.Code).toEqual [| "lock_removed" |]
                    Vitest.expect(outcome.Warnings |> Array.map _.Message).toEqual [| lockPath |]
                    Vitest.expect(Main.Bindings.Filesystem.existsSync lockPath).toBe false
                    Vitest.expect(outcome.Value.ActiveConflictSession).toEqual None

                    let! again = api.clearStaleLock (request "clear-lock-2")
                    let noOp = expectDtoValue "clear lock again" again
                    Vitest.expect(noOp.Warnings).toEqual [||]
                    Vitest.expect(noOp.Effect).toEqual (OperationEffectDto.NoOp(Some "no stale lock"))

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
            "bind persists the new location and reopens the session under a new id",
            fun () ->
                withFixture (fun fixture -> promise {
                    registerVault 47 fixture.RepoRoot |> ignore
                    let api = Main.IPC.IVersionControlApi.api (ipcEvent 47)

                    let! before = api.getSessionInfo (request "info-before")
                    let sessionBefore = (expectDtoValue "session before bind" before).Value

                    let! bound =
                        api.bindWorkspace {
                            OperationId = "bind"
                            ProviderLocation = "https://example.invalid/never/reached.git"
                            DisplayName = Some "reached"
                        }

                    let sessionAfter = (expectDtoValue "bind" bound).Value
                    Vitest.expect(sessionAfter.SessionId).not.toBe sessionBefore.SessionId

                    Vitest
                        .expect(sessionAfter.Location |> Option.map _.ProviderLocation)
                        .toEqual (Some "https://example.invalid/never/reached.git")

                    let persisted =
                        fixture.Runtime.Bindings.TryFind fixture.RepoRoot
                        |> Option.defaultWith (fun () -> failwith "binding missing")

                    Vitest.expect(persisted.Location.ProviderLocation).toBe "https://example.invalid/never/reached.git"

                    Vitest.expect(git fixture.RepoRoot [ "remote"; "get-url"; "origin" ] |> _.Trim()).toBe
                        "https://example.invalid/never/reached.git"
                })
        )

        Vitest.test (
            "a mutation marks the vault busy while it runs and releases it on failure",
            fun () ->
                withFixture (fun fixture -> promise {
                    let vault = registerVault 48 fixture.RepoRoot
                    let api = Main.IPC.IVersionControlApi.api (ipcEvent 48)
                    Vitest.expect(vault.isBusyWriting).toBe false

                    let running =
                        api.restorePaths {
                            OperationId = "restore-stale"
                            Paths = [| "base.txt" |]
                            ExpectedWorkspaceVersion = "stale"
                        }

                    Vitest.expect(vault.isBusyWriting).toBe true
                    let! result = running
                    expectDtoFailure "stale restore" result |> ignore
                    Vitest.expect(vault.isBusyWriting).toBe false
                })
        )

        Vitest.test (
            "an unparseable expected target revision or base ref is a validation failure, never dropped",
            fun () ->
                withFixture (fun fixture -> promise {
                    registerVault 49 fixture.RepoRoot |> ignore
                    let api = Main.IPC.IVersionControlApi.api (ipcEvent 49)
                    let! status = api.getStatus (request "status")
                    let version = (expectDtoValue "status" status).Value.WorkspaceVersion

                    let! published =
                        api.publish {
                            OperationId = "publish-bad-revision"
                            ExpectedWorkspaceVersion = version
                            ExpectedTargetRevision = Some "   "
                        }

                    let publishFailure = expectDtoFailure "publish with blank revision" published
                    Vitest.expect(publishFailure.Category).toEqual FailureCategoryDto.Validation
                    Vitest.expect(publishFailure.Code).toBe "invalid_revision"

                    let! created =
                        api.createRef {
                            OperationId = "branch-bad-base"
                            Name = "feature"
                            BaseRef = Some ""
                            SwitchTo = false
                            ExpectedWorkspaceVersion = version
                        }

                    let refFailure = expectDtoFailure "create ref with blank base" created
                    Vitest.expect(refFailure.Code).toBe "invalid_ref"
                    Vitest.expect(git fixture.RepoRoot [ "branch"; "--list"; "feature" ] |> _.Trim()).toBe ""
                })
        )

        Vitest.test (
            "an absent optional service is reported with a stable code and an open failure keeps its provider code",
            fun () ->
                withFixture (fun fixture -> promise {
                    let coreOnlyRoot = join [| fixture.Root; "core-only" |]

                    Main.Bindings.Filesystem.mkdirSync
                        coreOnlyRoot
                        (Main.Bindings.Filesystem.MkdirOptions(recursive = true))

                    let failingRoot = join [| fixture.Root; "failing" |]

                    Main.Bindings.Filesystem.mkdirSync
                        failingRoot
                        (Main.Bindings.Filesystem.MkdirOptions(recursive = true))

                    let fakeRuntime = fakeProviderRuntime coreOnlyRoot failingRoot
                    let fakeHost = WorkspaceSessionHost.WorkspaceSessionHost(fakeRuntime)
                    WorkspaceSessionHost.initialize fakeHost

                    try
                        registerVault 50 coreOnlyRoot |> ignore
                        let api = Main.IPC.IVersionControlApi.api (ipcEvent 50)
                        let! url = api.getRepositoryWebUrl (request "url")
                        let failure = expectDtoFailure "web url without browser service" url
                        Vitest.expect(failure.Category).toEqual FailureCategoryDto.Unsupported
                        Vitest.expect(failure.Code).toBe VersionControlCodes.ServiceUnavailable

                        registerVault 51 failingRoot |> ignore
                        let failingApi = Main.IPC.IVersionControlApi.api (ipcEvent 51)
                        let! info = failingApi.getSessionInfo (request "info-failing")
                        let openFailure = expectDtoFailure "open of a failing provider" info
                        Vitest.expect(openFailure.Category).toEqual FailureCategoryDto.DependencyMissing
                        Vitest.expect(openFailure.Code).toBe "tool_missing"
                        Vitest.expect(openFailure.Details.[0]).toBe "The workspace session could not be opened."

                        let! dependencies = failingApi.checkDependencies (request "deps")

                        match dependencies with
                        | Ok(OperationResultDto.PartiallySucceeded(outcome, failure)) ->
                            Vitest.expect(outcome.Value |> Array.map _.Component).toEqual [| "fake-tool" |]
                            Vitest.expect(failure.Code).toBe "check_failed"
                        | other -> failwith $"Expected a partial dependency report, got {other}"
                    finally
                        WorkspaceSessionHost.initialize fixture.Host
                })
        )

        Vitest.test (
            "a partial bind reopens the session over the persisted binding and keeps the provider failure",
            fun () ->
                withFixture (fun fixture -> promise {
                    let coreOnlyRoot = join [| fixture.Root; "core-bind" |]

                    Main.Bindings.Filesystem.mkdirSync
                        coreOnlyRoot
                        (Main.Bindings.Filesystem.MkdirOptions(recursive = true))

                    let failingRoot = join [| fixture.Root; "failing-bind" |]

                    Main.Bindings.Filesystem.mkdirSync
                        failingRoot
                        (Main.Bindings.Filesystem.MkdirOptions(recursive = true))

                    let fakeRuntime = fakeProviderRuntime coreOnlyRoot failingRoot
                    let fakeHost = WorkspaceSessionHost.WorkspaceSessionHost(fakeRuntime)
                    WorkspaceSessionHost.initialize fakeHost

                    try
                        registerVault 53 coreOnlyRoot |> ignore
                        let api = Main.IPC.IVersionControlApi.api (ipcEvent 53)
                        let! before = api.getSessionInfo (request "info-before-bind")
                        let sessionBefore = (expectDtoValue "session before bind" before).Value

                        let! bound =
                            api.bindWorkspace {
                                OperationId = "bind-partial"
                                ProviderLocation = "https://example.invalid/partial.git"
                                DisplayName = None
                            }

                        match bound with
                        | Ok(OperationResultDto.PartiallySucceeded(outcome, failure)) ->
                            Vitest.expect(outcome.Value.SessionId).not.toBe sessionBefore.SessionId

                            Vitest
                                .expect(outcome.Value.Location |> Option.map _.ProviderLocation)
                                .toEqual (Some "https://example.invalid/partial.git")

                            Vitest.expect(failure.Code).toBe "fake_bind_partial"
                        | other -> failwith $"Expected a partial bind, got {other}"

                        Vitest
                            .expect(
                                fakeRuntime.Bindings.TryFind coreOnlyRoot
                                |> Option.map (fun binding -> binding.Location.ProviderLocation)
                            )
                            .toEqual (Some "https://example.invalid/partial.git")
                    finally
                        WorkspaceSessionHost.initialize fixture.Host
                })
        )

        Vitest.test (
            "overlapping mutations keep the vault busy until the last one ends",
            fun () ->
                withFixture (fun fixture -> promise {
                    let coreOnlyRoot = join [| fixture.Root; "core-overlap" |]

                    Main.Bindings.Filesystem.mkdirSync
                        coreOnlyRoot
                        (Main.Bindings.Filesystem.MkdirOptions(recursive = true))

                    let failingRoot = join [| fixture.Root; "failing-overlap" |]

                    Main.Bindings.Filesystem.mkdirSync
                        failingRoot
                        (Main.Bindings.Filesystem.MkdirOptions(recursive = true))

                    let fakeHost =
                        WorkspaceSessionHost.WorkspaceSessionHost(fakeProviderRuntime coreOnlyRoot failingRoot)

                    WorkspaceSessionHost.initialize fakeHost

                    try
                        let vault = registerVault 52 coreOnlyRoot
                        let api = Main.IPC.IVersionControlApi.api (ipcEvent 52)
                        let firstGate, releaseFirst = deferred ()
                        let secondGate, releaseSecond = deferred ()
                        restoreGates.["first.txt"] <- firstGate
                        restoreGates.["second.txt"] <- secondGate

                        let first =
                            api.restorePaths {
                                OperationId = "restore-first"
                                Paths = [| "first.txt" |]
                                ExpectedWorkspaceVersion = "v1"
                            }

                        let second =
                            api.restorePaths {
                                OperationId = "restore-second"
                                Paths = [| "second.txt" |]
                                ExpectedWorkspaceVersion = "v1"
                            }

                        Vitest.expect(vault.isBusyWriting).toBe true
                        releaseFirst ()
                        let! _ = first
                        Vitest.expect(vault.isBusyWriting).toBe true
                        releaseSecond ()
                        let! _ = second
                        Vitest.expect(vault.isBusyWriting).toBe false
                    finally
                        restoreGates.Clear()
                        WorkspaceSessionHost.initialize fixture.Host
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
