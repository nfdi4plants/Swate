module VersionControlLakeFsTests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron
open Main
open Main.Bindings.Path
open Main.VersionControl
open Swate.Components.Composite.Authentication.Types
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions
open Vitest
open ElectronCore.TestHelpers

module LakeFsApi = VersionControlService.LakeFs.LakeFsApi
module LakeFsCredentials = VersionControlService.LakeFs.LakeFsCredentials
module LakeFsTypes = VersionControlService.LakeFs.LakeFsTypes
module LakeFsWorkspaceSession = VersionControlService.LakeFs.LakeFsWorkspaceSession
module RuntimeNodeInterop = VersionControlService.Runtime.Node.Interop

let private electronMock: obj = import "__electronMock" "electron"

[<Emit("require('node:child_process').execFileSync($0, $1, { cwd: $2, stdio: 'pipe' }).toString()")>]
let private execFile (_file: string) (_args: string[]) (_cwd: string) : string = jsNative

let private writeText (filePath: string) (content: string) =
    Main.Bindings.Filesystem.writeFileSync filePath content Main.Bindings.Filesystem.TextEncoding.Utf8

[<Emit("process.env[$0] ?? null")>]
let private getEnvironmentVariable (_name: string) : string = jsNative

[<Emit("(() => { let resolve; const promise = new Promise((r) => { resolve = r; }); return [promise, resolve]; })()")>]
let private deferred () : JS.Promise<unit> * (unit -> unit) = jsNative

let private integrationEnabled () =
    getEnvironmentVariable "LAKEFS_INTEGRATION" = "1"

let private lakeFsConnection () : LakeFsTypes.LakeFsConnection = {
    Endpoint =
        getEnvironmentVariable "LAKEFS_INTEGRATION_ENDPOINT"
        |> Option.ofObj
        |> Option.defaultValue "http://127.0.0.1:8000"
    AccessKeyId =
        getEnvironmentVariable "LAKEFS_INTEGRATION_ACCESS_KEY_ID"
        |> Option.ofObj
        |> Option.defaultValue "integration-access"
    SecretAccessKey =
        getEnvironmentVariable "LAKEFS_INTEGRATION_SECRET_ACCESS_KEY"
        |> Option.ofObj
        |> Option.defaultValue "integration-secret"
}

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

let private createRuntimeWithFactory
    (settingsRoot: string)
    (bindings: WorkspaceBindingStore.IWorkspaceBindingStore)
    (lakeFsFactory: ProviderFactory)
    : VersionControlRuntime.VersionControlRuntime =
    {
        Catalog =
            ProviderComposition.createCatalog [
                ProviderComposition.createGitFactory noAccounts
                lakeFsFactory
            ]
        Bindings = bindings
        PathCaseSensitivity = CaseInsensitive
    }

let private createRuntime
    (settingsRoot: string)
    (connection: LakeFsTypes.LakeFsConnection)
    (bindings: WorkspaceBindingStore.IWorkspaceBindingStore)
    : VersionControlRuntime.VersionControlRuntime =
    createRuntimeWithFactory
        settingsRoot
        bindings
        (ProviderComposition.createLakeFsFactory
            (ProviderComposition.lakeFsOptions settingsRoot CaseInsensitive)
            (LakeFsCredentials.fixedConnection connection))

let private createRuntimeWithHooks
    (settingsRoot: string)
    (connection: LakeFsTypes.LakeFsConnection)
    (bindings: WorkspaceBindingStore.IWorkspaceBindingStore)
    (hooks: LakeFsWorkspaceSession.LakeFsSessionHooks)
    : VersionControlRuntime.VersionControlRuntime =
    createRuntimeWithFactory
        settingsRoot
        bindings
        (LakeFsWorkspaceSession.createFactoryWithHooks
            (ProviderComposition.lakeFsOptions settingsRoot CaseInsensitive)
            hooks
            (LakeFsCredentials.fixedConnection connection))

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

let private expectDtoFailureOrPartial (operation: string) (result: Result<OperationResultDto<'T>, exn>) =
    match result with
    | Ok(OperationResultDto.Failed failure)
    | Ok(OperationResultDto.PartiallySucceeded(_, failure)) -> failure
    | Ok _ -> failwith $"{operation} unexpectedly succeeded."
    | Error error -> failwith $"{operation} threw: {error.Message}"

let private expectLakeFs (operation: string) (result: Result<'T, OperationFailure>) =
    match result with
    | Ok value -> value
    | Error failure -> failwith $"{operation} failed ({failure.Code}): {failure.Message}"

let private expectServiceUnavailable operation result =
    let failure = expectDtoFailure operation result
    Vitest.expect(failure.Category).toEqual FailureCategoryDto.Unsupported
    Vitest.expect(failure.Code).toBe VersionControlCodes.ServiceUnavailable

type private Fixture = {
    Root: string
    Workspace: string
    Second: string
    Settings: string
    Repository: string
    Bindings: WorkspaceBindingStore.IWorkspaceBindingStore
    Runtime: VersionControlRuntime.VersionControlRuntime
    Host: WorkspaceSessionHost.WorkspaceSessionHost
}

let private createRepository (connection: LakeFsTypes.LakeFsConnection) (repository: string) = promise {
    let payload =
        JS.JSON.stringify (
            createObj [
                "name" ==> repository
                "storage_namespace" ==> $"local://{repository}"
                "default_branch" ==> "main"
            ]
        )

    let! result =
        LakeFsApi.requestChecked
            connection
            "POST"
            "/repositories"
            (Some("application/json", payload))
            (OperationContext.detached $"create-repository-{repository}")
        |> Async.StartAsPromise

    expectLakeFs "create repository" result |> ignore
    printfn "lakeFS repository: %s" repository
}

let private createFixtureWithRuntime
    (makeRuntime:
        string
            -> LakeFsTypes.LakeFsConnection
            -> WorkspaceBindingStore.IWorkspaceBindingStore
            -> VersionControlRuntime.VersionControlRuntime)
    =
    promise {
        let! root = createTempDirectoryAsync "swate-vc-lakefs-"
        let workspace = join [| root; "workspace" |]
        let second = join [| root; "second" |]
        let settings = join [| root; "settings" |]
        let connection = lakeFsConnection ()

        for folder in [ workspace; second; settings ] do
            Main.Bindings.Filesystem.mkdirSync folder (Main.Bindings.Filesystem.MkdirOptions(recursive = true))

        let bindings = memoryBindings ()
        let runtime = makeRuntime settings connection bindings
        let host = WorkspaceSessionHost.WorkspaceSessionHost(runtime)
        WorkspaceSessionHost.initialize host
        VersionControlRuntime.initialize runtime

        let repositoryToken =
            RuntimeNodeInterop.randomUuid().Replace("-", "").ToLowerInvariant()

        let repository = $"swate-{repositoryToken}"

        try
            do! createRepository connection repository

            return {
                Root = root
                Workspace = workspace
                Second = second
                Settings = settings
                Repository = repository
                Bindings = bindings
                Runtime = runtime
                Host = host
            }
        with error ->
            do! removeDirectoryAsync root
            return raise error
    }

let private createFixture () =
    createFixtureWithRuntime (fun settings connection bindings -> createRuntime settings connection bindings)

let private createHookFixture hooks =
    createFixtureWithRuntime (fun settings connection bindings ->
        createRuntimeWithHooks settings connection bindings hooks
    )

let private cleanupFixture (fixture: Fixture) = promise {
    do! fixture.Host.CloseAll() |> Async.StartAsPromise
    do! removeDirectoryAsync fixture.Root
}

let private withFixtureFrom (create: unit -> JS.Promise<Fixture>) (body: Fixture -> JS.Promise<unit>) = promise {
    let! fixture = create ()

    try
        do! body fixture
        do! cleanupFixture fixture
    with error ->
        do! cleanupFixture fixture
        return raise error
}

let private withFixture body = withFixtureFrom createFixture body

let private withHookFixture hooks body =
    withFixtureFrom (fun () -> createHookFixture hooks) body

let private detached name = OperationContext.detached name

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

let private bindWorkspaceViaClone
    (fixture: Fixture)
    (windowId: int)
    (operationId: string)
    : JS.Promise<IVersionControlApi> =
    promise {
        registerVault windowId fixture.Workspace |> ignore
        let api = Main.IPC.IVersionControlApi.api (ipcEvent windowId)

        let! cloned =
            api.cloneWorkspace {
                OperationId = operationId
                ProviderLocation = $"lakefs://{fixture.Repository}/main"
                DisplayName = Some fixture.Repository
                TargetPath = fixture.Workspace
                TargetRef = None
                MaterializeAllObjects = false
            }

        expectDtoValue "clone lakeFS workspace" cloned |> ignore
        return api
    }

let private uploadTextObject
    (fixture: Fixture)
    (connection: LakeFsTypes.LakeFsConnection)
    (branch: string)
    (path: string)
    (content: string)
    (operationId: string)
    =
    promise {
        let sourcePath =
            join [|
                fixture.Second
                $"lakefs-upload-{RuntimeNodeInterop.randomUuid ()}.txt"
            |]

        writeText sourcePath content

        let! uploaded =
            LakeFsApi.uploadObjectFromFile connection fixture.Repository branch path sourcePath (detached operationId)
            |> Async.StartAsPromise

        expectLakeFs $"upload {path}" uploaded |> ignore
    }

let private commitServer
    (fixture: Fixture)
    (connection: LakeFsTypes.LakeFsConnection)
    (branch: string)
    (message: string)
    (operationId: string)
    =
    promise {
        let! committed =
            LakeFsApi.commit connection fixture.Repository branch message (detached operationId)
            |> Async.StartAsPromise

        expectLakeFs "commit server changes" committed |> ignore
    }

let private serverObjects
    (fixture: Fixture)
    (connection: LakeFsTypes.LakeFsConnection)
    (reference: string)
    (operationId: string)
    =
    promise {
        let! listed =
            LakeFsApi.listObjects connection fixture.Repository reference "" (detached operationId)
            |> Async.StartAsPromise

        return expectLakeFs "list server objects" listed
    }

let private downloadServerText
    (fixture: Fixture)
    (connection: LakeFsTypes.LakeFsConnection)
    (reference: string)
    (path: string)
    (operationId: string)
    =
    promise {
        let targetPath =
            join [|
                fixture.Second
                $"lakefs-download-{RuntimeNodeInterop.randomUuid ()}.txt"
            |]

        let! downloaded =
            LakeFsApi.downloadObjectToFile
                connection
                fixture.Repository
                reference
                path
                targetPath
                (detached operationId)
            |> Async.StartAsPromise

        expectLakeFs $"download {path}" downloaded |> ignore
        return Main.Bindings.Filesystem.readFileSync targetPath Main.Bindings.Filesystem.TextEncoding.Utf8
    }

let private commitAndPublish
    (fixture: Fixture)
    (api: IVersionControlApi)
    (path: string)
    (content: string)
    (operationPrefix: string)
    =
    promise {
        writeText (join [| fixture.Workspace; path |]) content

        let! statusResult = api.getStatus (request $"{operationPrefix}-status")
        let status = (expectDtoValue "status before revision" statusResult).Value

        let! created =
            api.createRevision {
                OperationId = $"{operationPrefix}-revision"
                Message = $"Add {path}"
                Paths = [| path |]
                ExpectedWorkspaceVersion = status.WorkspaceVersion
            }

        expectDtoValue "create local revision" created |> ignore

        let! afterCommit = api.getStatus (request $"{operationPrefix}-status-after")
        let afterCommitStatus = (expectDtoValue "status after revision" afterCommit).Value
        let! refreshed = api.refreshSynchronization (request $"{operationPrefix}-refresh")
        let synchronization = (expectDtoValue "refresh synchronization" refreshed).Value

        let! published =
            api.publish {
                OperationId = $"{operationPrefix}-publish"
                ExpectedWorkspaceVersion = afterCommitStatus.WorkspaceVersion
                ExpectedTargetRevision = synchronization.TargetRevision
            }

        return (expectDtoValue "publish local revision" published).Value
    }

Vitest.describe (
    "Workspace session host",
    fun () ->
        if not (integrationEnabled ()) then
            printfn "lakeFS integration skipped: set LAKEFS_INTEGRATION=1"

        Vitest.afterEach (fun () ->
            electronMock?reset () |> ignore
            ARC_VAULTS.Vaults.Clear()
        )

        Vitest.test (
            "initializes a lakeFS workspace, persists the binding and reopens the same session",
            TestOptions(timeout = 120000, skip = not (integrationEnabled ())),
            fun () -> promise {
                if not (integrationEnabled ()) then
                    return failwith "lakeFS integration skipped: set LAKEFS_INTEGRATION=1"

                return!
                    withFixture (fun fixture -> promise {
                        let! _ = bindWorkspaceViaClone fixture 61 "lakefs-clone"

                        let persisted =
                            fixture.Runtime.Bindings.TryFind fixture.Workspace
                            |> Option.defaultWith (fun () -> failwith "Binding was not persisted.")

                        Vitest.expect(ProviderId.value persisted.ProviderId).toBe "lakefs"

                        let! first =
                            fixture.Host.OpenSession(fixture.Workspace, detached "lakefs-open-1")
                            |> Async.StartAsPromise

                        let hosted = expectValue "first lakeFS open" first
                        Vitest.expect(ProviderId.value hosted.Session.Descriptor.ProviderId).toBe "lakefs"

                        let! second =
                            fixture.Host.OpenSession(fixture.Workspace, detached "lakefs-open-2")
                            |> Async.StartAsPromise

                        Vitest.expect((expectValue "second lakeFS open" second).SessionId).toBe hosted.SessionId
                    })
            }
        )
)

Vitest.describe (
    "Version control IPC over a lakeFS vault",
    fun () ->
        Vitest.afterEach (fun () ->
            electronMock?reset () |> ignore
            ARC_VAULTS.Vaults.Clear()
        )

        Vitest.test (
            "advertises synchronization and conflict resolution only",
            TestOptions(timeout = 120000, skip = not (integrationEnabled ())),
            fun () -> promise {
                if not (integrationEnabled ()) then
                    return failwith "lakeFS integration skipped: set LAKEFS_INTEGRATION=1"

                return!
                    withFixture (fun fixture -> promise {
                        let! api = bindWorkspaceViaClone fixture 62 "lakefs-services-clone"
                        let! info = api.getSessionInfo (request "lakefs-services-info")
                        let session = (expectDtoValue "lakeFS session info" info).Value
                        let services = session.Services

                        Vitest.expect(services.Synchronization).toBe true
                        Vitest.expect(services.ConflictResolution).toBe true
                        Vitest.expect(services.TextDiff).toBe false
                        Vitest.expect(services.ObjectMaterialization).toBe false
                        Vitest.expect(services.StoragePolicy).toBe false
                        Vitest.expect(services.Maintenance).toBe false
                        Vitest.expect(services.RepositoryBrowser).toBe false

                        let! textDiff =
                            api.getTextDiff {
                                OperationId = "lakefs-text-diff"
                                Path = "data.txt"
                            }

                        let! wordDiff =
                            api.getWordDiff {
                                OperationId = "lakefs-word-diff"
                                Path = "data.txt"
                            }

                        let! baseContent =
                            api.getBaseContent {
                                OperationId = "lakefs-base-content"
                                Path = "data.txt"
                            }

                        let! objects = api.listObjects (request "lakefs-list-objects")

                        let! materialize =
                            api.materializeObject {
                                OperationId = "lakefs-materialize"
                                Path = "data.txt"
                            }

                        let! dematerialize =
                            api.dematerializeObject {
                                OperationId = "lakefs-dematerialize"
                                Path = "data.txt"
                            }

                        let! getPolicy = api.getStoragePolicySettings (request "lakefs-get-policy")

                        let! setPolicy =
                            api.setStoragePolicySettings {
                                OperationId = "lakefs-set-policy"
                                Settings = {
                                    AutoPolicyThresholdMb = Some 2
                                    MaterializeLargeObjects = false
                                }
                            }

                        let! setPathPolicy =
                            api.setPathStoragePolicy {
                                OperationId = "lakefs-set-path-policy"
                                Path = "data.txt"
                                UseLargeObjectStorage = false
                            }

                        let! prune = api.pruneStorage (request "lakefs-prune")
                        let! deduplicate = api.deduplicateStorage (request "lakefs-deduplicate")
                        let! repositoryUrl = api.getRepositoryWebUrl (request "lakefs-repository-url")

                        expectServiceUnavailable "getTextDiff" textDiff
                        expectServiceUnavailable "getWordDiff" wordDiff
                        expectServiceUnavailable "getBaseContent" baseContent
                        expectServiceUnavailable "listObjects" objects
                        expectServiceUnavailable "materializeObject" materialize
                        expectServiceUnavailable "dematerializeObject" dematerialize

                        let policy = expectDtoValue "getStoragePolicySettings" getPolicy
                        Vitest.expect(policy.Value.AutoPolicyThresholdMb).toEqual (Some 1)
                        Vitest.expect(policy.Value.MaterializeLargeObjects).toBe false
                        expectDtoValue "setStoragePolicySettings" setPolicy |> ignore

                        expectServiceUnavailable "setPathStoragePolicy" setPathPolicy
                        expectServiceUnavailable "pruneStorage" prune
                        expectServiceUnavailable "deduplicateStorage" deduplicate
                        expectServiceUnavailable "getRepositoryWebUrl" repositoryUrl
                    })
            }
        )

        Vitest.test (
            "commits selected paths and publishes them to the repository",
            TestOptions(timeout = 120000, skip = not (integrationEnabled ())),
            fun () -> promise {
                if not (integrationEnabled ()) then
                    return failwith "lakeFS integration skipped: set LAKEFS_INTEGRATION=1"

                return!
                    withFixture (fun fixture -> promise {
                        let! api = bindWorkspaceViaClone fixture 63 "lakefs-commit-clone"
                        writeText (join [| fixture.Workspace; "data.txt" |]) "local data\n"

                        let! before = api.getStatus (request "lakefs-commit-status-before")
                        let beforeValue = (expectDtoValue "status before commit" before).Value
                        Vitest.expect(beforeValue.Changes |> Array.map _.Path).toEqual [| "data.txt" |]

                        let! created =
                            api.createRevision {
                                OperationId = "lakefs-create-revision"
                                Message = "Add data"
                                Paths = [| "data.txt" |]
                                ExpectedWorkspaceVersion = beforeValue.WorkspaceVersion
                            }

                        let createdOutcome = expectDtoValue "create revision" created
                        Vitest.expect(createdOutcome.Publication).toEqual PublicationStateDto.LocalOnly

                        let! afterCommit = api.getStatus (request "lakefs-commit-status-after")
                        let afterCommitValue = (expectDtoValue "status after commit" afterCommit).Value
                        Vitest.expect(afterCommitValue.Changes).toEqual [||]

                        let! refreshed = api.refreshSynchronization (request "lakefs-commit-refresh")
                        let refreshedValue = (expectDtoValue "refresh before publish" refreshed).Value

                        let! published =
                            api.publish {
                                OperationId = "lakefs-publish"
                                ExpectedWorkspaceVersion = afterCommitValue.WorkspaceVersion
                                ExpectedTargetRevision = refreshedValue.TargetRevision
                            }

                        let publishedValue = (expectDtoValue "publish" published).Value
                        Vitest.expect(publishedValue.Relationship).toEqual RevisionRelationshipDto.UpToDate
                        Vitest.expect(publishedValue.LocalRevisionCount).toEqual None

                        let! objects = serverObjects fixture (lakeFsConnection ()) "main" "lakefs-list-main"
                        Vitest.expect(objects |> Array.exists (fun item -> item.Path = "data.txt")).toBe true
                    })
            }
        )

        Vitest.test (
            "updates the workspace from a revision made on the server",
            TestOptions(timeout = 120000, skip = not (integrationEnabled ())),
            fun () -> promise {
                if not (integrationEnabled ()) then
                    return failwith "lakeFS integration skipped: set LAKEFS_INTEGRATION=1"

                return!
                    withFixture (fun fixture -> promise {
                        let! api = bindWorkspaceViaClone fixture 64 "lakefs-update-clone"
                        let! _ = commitAndPublish fixture api "data.txt" "local data\n" "lakefs-update-base"

                        do!
                            uploadTextObject
                                fixture
                                (lakeFsConnection ())
                                "main"
                                "remote.txt"
                                "remote content\n"
                                "lakefs-update-upload"

                        do!
                            commitServer
                                fixture
                                (lakeFsConnection ())
                                "main"
                                "Add remote file"
                                "lakefs-update-remote-commit"

                        let! refreshed = api.refreshSynchronization (request "lakefs-update-refresh")
                        let refreshedValue = (expectDtoValue "refresh remote revision" refreshed).Value
                        Vitest.expect(refreshedValue.TargetRevision.IsSome).toBe true

                        Vitest
                            .expect(
                                refreshedValue.RemoteChangedPaths
                                |> Option.defaultValue [||]
                                |> Array.contains "remote.txt"
                            )
                            .toBe
                            true

                        let! preview = api.previewUpdate (request "lakefs-update-preview")
                        let previewValue = (expectDtoValue "preview update" preview).Value
                        Vitest.expect(previewValue.WouldCreateConflictSession).toBe false
                        Vitest.expect(previewValue.HasDataLossRisk).toBe false

                        let! status = api.getStatus (request "lakefs-update-status")
                        let statusValue = (expectDtoValue "status before update" status).Value

                        let! updated =
                            api.update {
                                OperationId = "lakefs-update"
                                ExpectedWorkspaceVersion = statusValue.WorkspaceVersion
                            }

                        expectDtoValue "update" updated |> ignore

                        Vitest
                            .expect(
                                Main.Bindings.Filesystem.readFileSync
                                    (join [| fixture.Workspace; "remote.txt" |])
                                    Main.Bindings.Filesystem.TextEncoding.Utf8
                            )
                            .toBe
                            "remote content\n"
                    })
            }
        )

        Vitest.test (
            "opens a conflict session on a diverged file and finalizes the resolution",
            TestOptions(timeout = 120000, skip = not (integrationEnabled ())),
            fun () -> promise {
                if not (integrationEnabled ()) then
                    return failwith "lakeFS integration skipped: set LAKEFS_INTEGRATION=1"

                return!
                    withFixture (fun fixture -> promise {
                        let! api = bindWorkspaceViaClone fixture 65 "lakefs-conflict-clone"
                        let! _ = commitAndPublish fixture api "shared.txt" "base" "lakefs-conflict-base"

                        do!
                            uploadTextObject
                                fixture
                                (lakeFsConnection ())
                                "main"
                                "shared.txt"
                                "server"
                                "lakefs-conflict-server-upload"

                        do!
                            commitServer
                                fixture
                                (lakeFsConnection ())
                                "main"
                                "Server shared change"
                                "lakefs-conflict-server-commit"

                        writeText (join [| fixture.Workspace; "shared.txt" |]) "local"
                        let! localStatus = api.getStatus (request "lakefs-conflict-local-status")
                        let localStatusValue = (expectDtoValue "conflict local status" localStatus).Value

                        let! localRevision =
                            api.createRevision {
                                OperationId = "lakefs-conflict-local-revision"
                                Message = "Local shared change"
                                Paths = [| "shared.txt" |]
                                ExpectedWorkspaceVersion = localStatusValue.WorkspaceVersion
                            }

                        expectDtoValue "create local conflict revision" localRevision |> ignore

                        let! beforeUpdate = api.getStatus (request "lakefs-conflict-update-status")

                        let beforeUpdateValue =
                            (expectDtoValue "status before conflict update" beforeUpdate).Value

                        let! updateResult =
                            api.update {
                                OperationId = "lakefs-conflict-update"
                                ExpectedWorkspaceVersion = beforeUpdateValue.WorkspaceVersion
                            }

                        let updateFailure = expectDtoFailureOrPartial "conflict update" updateResult
                        Vitest.expect(updateFailure.Category).toEqual FailureCategoryDto.Conflict
                        Vitest.expect(updateFailure.Code).toBe VersionControlCodes.ConflictsDetected

                        let! conflictStatus = api.getStatus (request "lakefs-conflict-status")

                        let conflictStatusValue =
                            (expectDtoValue "status with conflict" conflictStatus).Value

                        let conflict =
                            conflictStatusValue.ActiveConflictSession
                            |> Option.defaultWith (fun () -> failwith "Expected an active lakeFS conflict session.")

                        Vitest.expect(conflict.Items |> Array.exists (fun item -> item.Path = "shared.txt")).toBe true

                        let conflictItem =
                            conflict.Items |> Array.find (fun item -> item.Path = "shared.txt")

                        Vitest.expect(conflictItem.SupportsResolvedContent).toBe true

                        let! resolved =
                            api.resolveConflict {
                                OperationId = "lakefs-resolve-conflict"
                                Handle = conflict.Handle
                                ExpectedWorkspaceVersion = conflictStatusValue.WorkspaceVersion
                                Path = "shared.txt"
                                Resolution = ConflictResolutionDto.SupplyResolvedContent "merged"
                            }

                        let resolvedValue = (expectDtoValue "resolve conflict" resolved).Value
                        Vitest.expect(resolvedValue.RemainingItems).toEqual [||]

                        let! afterResolve = api.getStatus (request "lakefs-conflict-finalize-status")
                        let afterResolveValue = (expectDtoValue "status before finalize" afterResolve).Value

                        let! finalized =
                            api.finalizeConflict {
                                OperationId = "lakefs-finalize-conflict"
                                Handle = resolvedValue.RefreshedHandle
                                ExpectedWorkspaceVersion = afterResolveValue.WorkspaceVersion
                                Message = Some "Finalize merged shared file"
                            }

                        expectDtoValue "finalize conflict" finalized |> ignore

                        let! afterFinalize = api.getStatus (request "lakefs-conflict-publish-status")

                        let afterFinalizeValue =
                            (expectDtoValue "status before conflict publish" afterFinalize).Value

                        let! target = api.refreshSynchronization (request "lakefs-conflict-publish-refresh")
                        let targetValue = (expectDtoValue "refresh before conflict publish" target).Value

                        let! published =
                            api.publish {
                                OperationId = "lakefs-publish-conflict"
                                ExpectedWorkspaceVersion = afterFinalizeValue.WorkspaceVersion
                                ExpectedTargetRevision = targetValue.TargetRevision
                            }

                        expectDtoValue "publish resolved conflict" published |> ignore

                        let! serverContent =
                            downloadServerText
                                fixture
                                (lakeFsConnection ())
                                "main"
                                "shared.txt"
                                "lakefs-conflict-download"

                        Vitest.expect(serverContent).toBe "merged"
                    })
            }
        )

        Vitest.test (
            "reports an unreachable endpoint with a stable failure code",
            TestOptions(timeout = 120000, skip = not (integrationEnabled ())),
            fun () -> promise {
                if not (integrationEnabled ()) then
                    return failwith "lakeFS integration skipped: set LAKEFS_INTEGRATION=1"

                return!
                    withFixture (fun fixture -> promise {
                        let! _ = bindWorkspaceViaClone fixture 66 "lakefs-unreachable-clone"
                        do! fixture.Host.CloseAll() |> Async.StartAsPromise

                        let badConnection = {
                            lakeFsConnection () with
                                Endpoint = "http://127.0.0.1:9"
                        }

                        let badRuntime = createRuntime fixture.Settings badConnection fixture.Bindings
                        let badHost = WorkspaceSessionHost.WorkspaceSessionHost(badRuntime)
                        WorkspaceSessionHost.initialize badHost
                        VersionControlRuntime.initialize badRuntime

                        try
                            registerVault 66 fixture.Workspace |> ignore
                            let api = Main.IPC.IVersionControlApi.api (ipcEvent 66)
                            let! info = api.getSessionInfo (request "lakefs-unreachable-info")
                            let failure = expectDtoFailure "unreachable session info" info
                            Vitest.expect(failure.Category).toEqual FailureCategoryDto.Network
                            Vitest.expect(failure.Code).toBe "network_failure"
                            do! badHost.CloseAll() |> Async.StartAsPromise
                            WorkspaceSessionHost.initialize fixture.Host
                            VersionControlRuntime.initialize fixture.Runtime
                        with error ->
                            do! badHost.CloseAll() |> Async.StartAsPromise
                            WorkspaceSessionHost.initialize fixture.Host
                            VersionControlRuntime.initialize fixture.Runtime
                            return raise error
                    })
            }
        )

        Vitest.test (
            "cancels a running operation through its operation key",
            TestOptions(timeout = 120000, skip = not (integrationEnabled ())),
            fun () -> promise {
                if not (integrationEnabled ()) then
                    return failwith "lakeFS integration skipped: set LAKEFS_INTEGRATION=1"

                let entered, signalEntered = deferred ()
                let gate, releaseGate = deferred ()

                let hooks: LakeFsWorkspaceSession.LakeFsSessionHooks = {
                    Barrier =
                        Some(fun _ point _ -> async {
                            if point = "publish-connect" then
                                signalEntered ()
                                do! Async.AwaitPromise gate
                        })
                }

                return!
                    withHookFixture
                        hooks
                        (fun fixture -> promise {
                            let! api = bindWorkspaceViaClone fixture 67 "lakefs-cancel-clone"
                            writeText (join [| fixture.Workspace; "data.txt" |]) "local data\n"
                            let! setupStatus = api.getStatus (request "lakefs-cancel-setup-status")
                            let setupStatusValue = (expectDtoValue "cancel setup status" setupStatus).Value

                            let! created =
                                api.createRevision {
                                    OperationId = "lakefs-cancel-setup-revision"
                                    Message = "Add data for cancellation"
                                    Paths = [| "data.txt" |]
                                    ExpectedWorkspaceVersion = setupStatusValue.WorkspaceVersion
                                }

                            expectDtoValue "cancel setup revision" created |> ignore
                            let! status = api.getStatus (request "lakefs-cancel-status")
                            let statusValue = (expectDtoValue "cancel status" status).Value

                            let running =
                                api.publish {
                                    OperationId = "lakefs-cancel-publish"
                                    ExpectedWorkspaceVersion = statusValue.WorkspaceVersion
                                    ExpectedTargetRevision = None
                                }

                            try
                                do! Async.AwaitPromise entered |> Async.StartAsPromise

                                let! canceled =
                                    api.cancelOperation {
                                        SessionId = ""
                                        OperationId = "lakefs-cancel-publish"
                                    }

                                Vitest.expect(canceled).toEqual (Ok true)
                                releaseGate ()

                                let! result = running
                                let failure = expectDtoFailureOrPartial "canceled lakeFS publish" result
                                Vitest.expect(failure.Category).toEqual FailureCategoryDto.Canceled
                            with error ->
                                releaseGate ()
                                return raise error
                        })
            }
        )
)
