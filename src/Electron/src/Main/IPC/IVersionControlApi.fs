/// Provider-neutral version control IPC. Every handler resolves the vault of the calling
/// window, opens (or reuses) its library session, registers the operation for
/// cancellation, forwards progress to the window and maps the structured result.
/// Mutations run under the vault's busy flag so the file watcher does not merge
/// Swate's own writes, and they refresh the file tree afterwards.
module Main.IPC.IVersionControlApi

open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Fable.Electron.Remoting.Main
open Main
open Main.VersionControl
open Main.VersionControl.VersionControlSettings
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions

type private RendererBridge = {
    Progress: VersionControlProgressDto -> unit
    Started: OperationKeyDto -> unit
}

let private silentBridge: RendererBridge = { Progress = ignore; Started = ignore }

let private bridgeForWindow (window: BrowserWindow) : RendererBridge =
    let rendererApi =
        Remoting.createIpc ()
        |> Remoting.withWindow window
        |> Remoting.buildProxySender<IVersionControlRendererApi>

    {
        Progress = fun progress -> rendererApi.versionControlProgress progress
        Started = fun key -> rendererApi.versionControlOperationStarted key
    }

let private tryBridgeFromEvent (event: IpcMainInvokeEvent) =
    windowFromIpcEvent event
    |> Option.map bridgeForWindow
    |> Option.defaultValue silentBridge

let private serviceUnavailable (service: string) : OperationResult<'T> =
    Failed(
        OperationFailure.create
            Unsupported
            VersionControlCodes.ServiceUnavailable
            $"The workspace provider does not offer {service}."
    )

/// A session that could not be opened keeps the provider's category and code. A detail
/// line marks that the failure came from opening the session, not from the operation.
let private sessionUnavailable (failure: OperationFailure) : OperationFailure = {
    failure with
        Details = Array.append [| "The workspace session could not be opened." |] failure.Details
}

/// A provider or strategy that throws is reported as a structured failure, so the
/// renderer's result contract holds even then.
let private unexpectedFailure (error: exn) : OperationFailure =
    OperationFailure.createRedacted ProviderError "unexpected_exception" error.Message

let private failedDto (failure: OperationFailure) : OperationResultDto<'T> =
    OperationResultDto.Failed(Mappings.failure failure)

let private validationFailed (code: string) (message: string) : OperationResult<'T> =
    Failed(OperationFailure.create Validation code message)

let private storagePolicySettingsDto (settings: VersionControlSettings) : StoragePolicySettingsDto = {
    AutoPolicyThresholdMb = Some settings.AutoTrackThresholdMb
    MaterializeLargeObjects = settings.DownloadLargeFiles
}

/// Runs one tracked operation against a session-independent target. The operation is
/// registered and announced before any await, and every exception becomes a failure.
let private runTracked
    (host: WorkspaceSessionHost.WorkspaceSessionHost)
    (bridge: RendererBridge)
    (sessionId: string)
    (operationId: string)
    (operation: OperationContext -> Async<OperationResult<'T>>)
    : JS.Promise<OperationResult<'T>> =
    promise {
        let tracked = host.BeginOperation(sessionId, operationId, bridge.Progress)

        try
            try
                bridge.Started tracked.Key
                return! operation tracked.Context |> Async.StartAsPromise
            with error ->
                return Failed(unexpectedFailure error)
        finally
            tracked.Complete()
    }

/// Opens the vault session of the calling window and runs the operation in it. The
/// operation is registered and announced before the first await, so a cancel that
/// arrives right after the call started is honored even while the session opens.
let private withSession
    (event: IpcMainInvokeEvent)
    (operationId: string)
    (operation: WorkspaceSessionHost.HostedSession -> OperationContext -> Async<OperationResult<'T>>)
    (mapValue: 'T -> 'U)
    : JS.Promise<Result<OperationResultDto<'U>, exn>> =
    promise {
        match tryGetVaultAndArcPath event with
        | Error error -> return Error error
        | Ok(_, arcPath) ->
            let host = WorkspaceSessionHost.get ()
            let bridge = tryBridgeFromEvent event

            let knownSessionId =
                host.TryGetSession arcPath
                |> Option.map (fun hosted -> hosted.SessionId)
                |> Option.defaultValue ""

            let tracked = host.BeginOperation(knownSessionId, operationId, bridge.Progress)

            try
                try
                    bridge.Started tracked.Key
                    let! opened = host.OpenSession(arcPath, tracked.Context) |> Async.StartAsPromise

                    match opened with
                    | Succeeded outcome
                    | PartiallySucceeded(outcome, _) ->
                        let hosted = outcome.Value
                        host.AssignSession(operationId, hosted.SessionId)
                        let! result = operation hosted tracked.Context |> Async.StartAsPromise
                        return Ok(Mappings.result mapValue result)
                    | Failed failure -> return Ok(failedDto (sessionUnavailable failure))
                with error ->
                    return Ok(failedDto (unexpectedFailure error))
            finally
                tracked.Complete()
    }

let private withBusyWritingNested (vault: ArcVault) (operation: unit -> JS.Promise<'T>) : JS.Promise<'T> =
    withBusyWritingScope vault operation

/// Whether a result reports a change of the workspace, which is when the file tree
/// is refreshed after a mutation.
let resultChangedState (result: Result<OperationResultDto<'U>, exn>) =
    match result with
    | Ok(OperationResultDto.Succeeded outcome) -> outcome.Effect = OperationEffectDto.Performed
    | Ok(OperationResultDto.PartiallySucceeded _) -> true
    | Ok(OperationResultDto.Failed failure) -> failure.StateChanged
    | Error _ -> false

/// Same as withSession, with the vault marked busy for the duration and the file tree
/// refreshed afterwards when the operation changed the workspace.
let private withMutatingSession
    (event: IpcMainInvokeEvent)
    (operationId: string)
    (refreshTree: bool)
    (operation: WorkspaceSessionHost.HostedSession -> OperationContext -> Async<OperationResult<'T>>)
    (mapValue: 'T -> 'U)
    : JS.Promise<Result<OperationResultDto<'U>, exn>> =
    promise {
        match tryGetVaultAndArcPath event with
        | Error error -> return Error error
        | Ok(vault, arcPath) ->
            return!
                withBusyWritingNested
                    vault
                    (fun () -> promise {
                        let! result = withSession event operationId operation mapValue

                        if refreshTree && resultChangedState result then
                            let! fileTree = getFileTree arcPath
                            vault.SetFileTree fileTree

                        return result
                    })
    }

let private withService
    (select: WorkspaceSession -> 'S option)
    (serviceName: string)
    (call: 'S -> OperationContext -> Async<OperationResult<'T>>)
    (hosted: WorkspaceSessionHost.HostedSession)
    (context: OperationContext)
    : Async<OperationResult<'T>> =
    match select hosted.Session with
    | Some service -> call service context
    | None -> async { return serviceUnavailable serviceName }

let private withPath (path: string) (call: RepositoryPath -> Async<OperationResult<'T>>) : Async<OperationResult<'T>> =
    match Mappings.tryRepositoryPath path with
    | Ok repositoryPath -> call repositoryPath
    | Error failure -> async { return Failed failure }

let private tryGetRegularFileSize (workspaceRoot: string) (relativePath: string) : Async<int64 option> = async {
    try
        let! stats =
            Main.Bindings.Filesystem.statAsync (Main.Bindings.Path.join [| workspaceRoot; relativePath |])
            |> Async.AwaitPromise

        if stats.isFile () then
            return Some(int64 stats.size)
        else
            return None
    with _ ->
        return None
}

let private withPaths (paths: string[]) (call: RepositoryPath[] -> Async<OperationResult<'T>>) =
    match Mappings.tryRepositoryPaths paths with
    | Ok repositoryPaths -> call repositoryPaths
    | Error failure -> async { return Failed failure }

let private withProviderRef (value: string) (call: ProviderRef -> Async<OperationResult<'T>>) =
    match Mappings.tryProviderRef value with
    | Ok reference -> call reference
    | Error failure -> async { return Failed failure }

/// An optional ref or revision the renderer sent is either valid or a validation
/// failure. It is never dropped, because the provider treats these values as exact.
let private withOptionalProviderRef (value: string option) (call: ProviderRef option -> Async<OperationResult<'T>>) =
    match value with
    | None -> call None
    | Some text -> withProviderRef text (Some >> call)

let private withOptionalRevision (value: string option) (call: RevisionId option -> Async<OperationResult<'T>>) =
    match value with
    | None -> call None
    | Some text ->
        match Mappings.tryRevisionId text with
        | Ok revision -> call (Some revision)
        | Error failure -> async { return Failed failure }

let private tryFactoryFor (host: WorkspaceSessionHost.WorkspaceSessionHost) (providerId: ProviderId) =
    ProviderResolver.tryGetFactory host.Runtime.Catalog providerId

/// Persists a binding the provider produced. When the store fails after the provider
/// already changed state, the result is a partial success with StateChanged and a
/// recovery action, never a plain failure.
let private persistProvisionedBinding
    (host: WorkspaceSessionHost.WorkspaceSessionHost)
    (result: OperationResult<WorkspaceBinding>)
    : OperationResult<WorkspaceBinding> =
    let persistFailure (message: string) = {
        OperationFailure.create ProviderError VersionControlCodes.BindingNotPersisted message with
            StateChanged = true
            Retryable = true
            RecoveryAction =
                Some {
                    Code = "reopen_workspace"
                    Instructions =
                        Some
                            "The workspace was provisioned but its binding could not be saved. Open the workspace again so it is bound."
                }
    }

    match result with
    | Succeeded outcome ->
        match host.Runtime.Bindings.Save outcome.Value with
        | Ok() -> Succeeded outcome
        | Error message -> PartiallySucceeded(outcome, persistFailure message)
    | PartiallySucceeded(outcome, failure) ->
        match host.Runtime.Bindings.Save outcome.Value with
        | Ok() -> PartiallySucceeded(outcome, failure)
        | Error message -> PartiallySucceeded(outcome, persistFailure message)
    | Failed failure -> Failed failure

let initializeLocalWorkspace
    (host: WorkspaceSessionHost.WorkspaceSessionHost)
    (targetPath: string)
    (context: OperationContext)
    : Async<OperationResult<WorkspaceBinding>> =
    async {
        match tryFactoryFor host ProviderComposition.defaultProviderId with
        | None ->
            return
                validationFailed
                    VersionControlCodes.LocationUnsupported
                    "The default version control provider is not registered."
        | Some factory ->
            let! initialized =
                factory.Initialize
                    {
                        TargetPath = targetPath
                        Location = None
                    }
                    context

            return persistProvisionedBinding host initialized
    }

/// Runs a provisioning operation and maps its binding to the workspace root.
let private provision
    (host: WorkspaceSessionHost.WorkspaceSessionHost)
    (bridge: RendererBridge)
    (operationId: string)
    (run: OperationContext -> Async<OperationResult<WorkspaceBinding>>)
    : JS.Promise<Result<OperationResultDto<string>, exn>> =
    promise {
        let! result = runTracked host bridge "" operationId run

        return Ok(Mappings.result (fun (binding: WorkspaceBinding) -> binding.WorkspaceRoot) result)
    }

let private removeExistingFile (path: string) =
    if Main.Bindings.Filesystem.existsSync path then
        Main.Bindings.Filesystem.unlinkSync path
        true
    else
        false

let private locationFor
    (host: WorkspaceSessionHost.WorkspaceSessionHost)
    (providerLocation: string)
    (displayName: string option)
    =
    match ProviderComposition.tryCreateLocation providerLocation displayName with
    | Error message -> Error(OperationFailure.create Validation VersionControlCodes.LocationUnsupported message)
    | Ok location ->
        match tryFactoryFor host location.ProviderId with
        | Some factory -> Ok(location, factory)
        | None ->
            Error(
                OperationFailure.create
                    Validation
                    VersionControlCodes.LocationUnsupported
                    $"No provider is registered for '{ProviderId.value location.ProviderId}'."
            )

let api (event: IpcMainInvokeEvent) : IVersionControlApi = {
    // Opening the session adopts an unbound workspace and persists its binding. That
    // side effect is intended: the first read of a vault is what binds it.
    getSessionInfo =
        fun request ->
            withSession
                event
                request.OperationId
                (fun hosted _ -> async { return OperationResult.succeeded hosted })
                (fun hosted -> Mappings.sessionInfo hosted.SessionId hosted.Session)
    cloneWorkspace =
        fun request ->
            let host = WorkspaceSessionHost.get ()
            let bridge = tryBridgeFromEvent event

            provision
                host
                bridge
                request.OperationId
                (fun context -> async {
                    let! cloned =
                        match locationFor host request.ProviderLocation request.DisplayName with
                        | Error failure -> async { return Failed failure }
                        | Ok(location, factory) ->
                            withOptionalProviderRef
                                request.TargetRef
                                (fun targetRef ->
                                    factory.Clone
                                        {
                                            Location = location
                                            TargetPath = request.TargetPath
                                            TargetRef = targetRef
                                            MaterializeAllObjects = request.MaterializeAllObjects
                                        }
                                        context
                                )

                    return persistProvisionedBinding host cloned
                })
    initializeWorkspace =
        fun request ->
            let host = WorkspaceSessionHost.get ()
            let bridge = tryBridgeFromEvent event

            provision
                host
                bridge
                request.OperationId
                (fun context -> initializeLocalWorkspace host request.TargetPath context)
    // Bind changes the vault's repository configuration, so it runs as a mutation of
    // the open vault and refreshes the tree afterwards.
    bindWorkspace =
        fun request -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                let host = WorkspaceSessionHost.get ()
                let bridge = tryBridgeFromEvent event
                let savedSettings = host.GetSettings arcPath

                let restoreSavedSettings context = async {
                    try
                        let! restored = host.SetSettings(arcPath, savedSettings, context)

                        match restored with
                        | Failed failure ->
                            Browser.Dom.console.error (
                                $"Could not restore version-control settings after binding ({failure.Code}): {failure.Message}"
                            )
                        | Succeeded _
                        | PartiallySucceeded _ -> ()
                    with error ->
                        Browser.Dom.console.error (
                            $"Could not restore version-control settings after binding: {error.Message}"
                        )
                }

                let reopenWithSavedSettings context = async {
                    let! reopened = host.ReopenSession(arcPath, context)

                    match reopened with
                    | Succeeded _
                    | PartiallySucceeded _ ->
                        if savedSettings <> VersionControlSettings.defaults then
                            do! restoreSavedSettings context
                    | _ -> ()

                    return reopened
                }

                return!
                    withBusyWritingNested
                        vault
                        (fun () -> promise {
                            let! bound =
                                runTracked
                                    host
                                    bridge
                                    ""
                                    request.OperationId
                                    (fun context -> async {
                                        match locationFor host request.ProviderLocation request.DisplayName with
                                        | Error failure -> return Failed failure
                                        | Ok(location, factory) ->
                                            let! bindResult =
                                                factory.Bind
                                                    {
                                                        WorkspaceRoot = arcPath
                                                        Location = location
                                                    }
                                                    context

                                            match persistProvisionedBinding host bindResult with
                                            | Succeeded _ ->
                                                // The open session keeps the location it was opened with.
                                                return! reopenWithSavedSettings context
                                            | PartiallySucceeded(_, failure) when
                                                failure.Code = VersionControlCodes.BindingNotPersisted
                                                ->
                                                return Failed failure
                                            | PartiallySucceeded(_, failure) ->
                                                // The binding is persisted, so the session is reopened and
                                                // the provider's partial failure rides along.
                                                let! reopened = reopenWithSavedSettings context

                                                return
                                                    match reopened with
                                                    | Succeeded outcome -> PartiallySucceeded(outcome, failure)
                                                    | other -> other
                                            | Failed failure -> return Failed failure
                                    })

                            let result =
                                Ok(
                                    Mappings.result
                                        (fun (hosted: WorkspaceSessionHost.HostedSession) ->
                                            Mappings.sessionInfo hosted.SessionId hosted.Session
                                        )
                                        bound
                                )

                            if resultChangedState result then
                                let! fileTree = getFileTree arcPath
                                vault.SetFileTree fileTree

                            return result
                        })
        }
    cancelOperation =
        fun key -> promise {
            let host = WorkspaceSessionHost.get ()
            return Ok(host.Cancel(key.SessionId, key.OperationId))
        }
    // Every registered provider reports its components. A provider whose check fails
    // does not hide the others: its failure rides along as the partial failure.
    checkDependencies =
        fun request -> promise {
            let host = WorkspaceSessionHost.get ()
            let bridge = tryBridgeFromEvent event

            let! result =
                runTracked
                    host
                    bridge
                    ""
                    request.OperationId
                    (fun context -> async {
                        let factories = ProviderResolver.factories host.Runtime.Catalog
                        let mutable statuses: DependencyStatus[] = [||]
                        let mutable failure: OperationFailure option = None

                        for factory in factories do
                            if not (context.Cancellation.IsCancellationRequested()) then
                                let! reported = factory.CheckDependencies context

                                match reported with
                                | Succeeded outcome
                                | PartiallySucceeded(outcome, _) -> statuses <- Array.append statuses outcome.Value
                                | Failed error when error.Category = Canceled -> failure <- Some error
                                | Failed error ->
                                    if failure.IsNone then
                                        failure <- Some error

                        return
                            match failure with
                            | Some error when error.Category = Canceled -> Failed error
                            | _ when context.Cancellation.IsCancellationRequested() ->
                                OperationResult.canceled "The dependency check was canceled."
                            | Some error ->
                                PartiallySucceeded(
                                    OperationOutcome.performed statuses,
                                    {
                                        error with
                                            StateChanged = false
                                            RecoveryAction =
                                                Some {
                                                    Code = "check_dependencies"
                                                    Instructions =
                                                        Some "One provider could not report its dependencies."
                                                }
                                    }
                                )
                            | None -> OperationResult.succeeded statuses
                    })

            return Ok(Mappings.result (Array.map Mappings.dependencyStatus) result)
        }
    // The first provider that does not answer Unsupported owns the component, whether
    // its installation succeeded or failed.
    installDependency =
        fun request -> promise {
            let host = WorkspaceSessionHost.get ()
            let bridge = tryBridgeFromEvent event

            let! result =
                runTracked
                    host
                    bridge
                    ""
                    request.OperationId
                    (fun context -> async {
                        let factories = ProviderResolver.factories host.Runtime.Catalog
                        let mutable outcome: OperationResult<DependencyStatus> option = None

                        for factory in factories do
                            if outcome.IsNone then
                                let! installed = factory.InstallDependency request.Component context

                                match installed with
                                | Failed failure when failure.Category = Unsupported -> ()
                                | result -> outcome <- Some result

                        return
                            outcome
                            |> Option.defaultValue (serviceUnavailable $"installation of '{request.Component}'")
                    })

            return Ok(Mappings.result Mappings.dependencyStatus result)
        }
    getStatus =
        fun request ->
            withSession
                event
                request.OperationId
                (fun hosted context -> hosted.Session.Core.GetStatus context)
                Mappings.workspaceStatus
    listRefs =
        fun request ->
            withSession
                event
                request.OperationId
                (fun hosted context -> hosted.Session.Core.ListRefs context)
                (Array.map Mappings.logicalRef)
    createRef =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                request.SwitchTo
                (fun hosted context ->
                    withOptionalProviderRef
                        request.BaseRef
                        (fun baseRef ->
                            hosted.Session.Core.CreateRef
                                {
                                    Name = request.Name
                                    BaseRef = baseRef
                                    SwitchTo = request.SwitchTo
                                    ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                                }
                                context
                        )
                )
                Mappings.logicalRef
    preflightSwitchRef =
        fun request ->
            withSession
                event
                request.OperationId
                (fun hosted context ->
                    withProviderRef
                        request.TargetRef
                        (fun targetRef ->
                            hosted.Session.Core.PreflightSwitchRef
                                {
                                    TargetRef = targetRef
                                    ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                                }
                                context
                        )
                )
                Mappings.switchPreflight
    switchRef =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (fun hosted context ->
                    withProviderRef
                        request.TargetRef
                        (fun targetRef ->
                            hosted.Session.Core.SwitchRef
                                {
                                    TargetRef = targetRef
                                    ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                                }
                                context
                        )
                )
                Mappings.workspaceStatus
    createRevision =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                false
                (fun hosted context ->
                    withPaths
                        request.Paths
                        (fun paths ->
                            hosted.Session.Core.CreateRevision
                                {
                                    Message = request.Message
                                    Paths = paths
                                    ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                                }
                                context
                        )
                )
                RevisionId.value
    restorePaths =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (fun hosted context ->
                    withPaths
                        request.Paths
                        (fun paths ->
                            hosted.Session.Core.RestorePaths
                                {
                                    Paths = paths
                                    ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                                }
                                context
                        )
                )
                id
    getDiffSummary =
        fun request ->
            withSession
                event
                request.OperationId
                (fun hosted context -> hosted.Session.Core.GetDiffSummary context)
                Mappings.diffSummary
    getTextDiff =
        fun request ->
            withSession
                event
                request.OperationId
                (withService
                    _.TextDiff
                    "text diffs"
                    (fun service context -> withPath request.Path (fun path -> service.GetDiff path context)))
                Mappings.contentView
    getWordDiff =
        fun request ->
            withSession
                event
                request.OperationId
                (withService
                    _.TextDiff
                    "text diffs"
                    (fun service context -> withPath request.Path (fun path -> service.GetWordDiff path context)))
                Mappings.contentView
    getBaseContent =
        fun request ->
            withSession
                event
                request.OperationId
                (withService
                    _.TextDiff
                    "text diffs"
                    (fun service context -> withPath request.Path (fun path -> service.GetBaseContent path context)))
                Mappings.contentView
    refreshSynchronization =
        fun request ->
            withSession
                event
                request.OperationId
                (withService _.Synchronization "synchronization" (fun service context -> service.Refresh context))
                Mappings.synchronizationState
    previewUpdate =
        fun request ->
            withSession
                event
                request.OperationId
                (withService _.Synchronization "synchronization" (fun service context -> service.PreviewUpdate context))
                Mappings.updatePreview
    update =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (withService
                    _.Synchronization
                    "synchronization"
                    (fun service context ->
                        service.Update
                            {
                                ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                            }
                            context
                    ))
                Mappings.synchronizationState
    publish =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                false
                (withService
                    _.Synchronization
                    "synchronization"
                    (fun service context ->
                        withOptionalRevision
                            request.ExpectedTargetRevision
                            (fun expectedTarget ->
                                service.Publish
                                    {
                                        ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                                        ExpectedTargetRevision = expectedTarget
                                    }
                                    context
                            )
                    ))
                Mappings.synchronizationState
    getActiveConflictSession =
        fun request ->
            withSession
                event
                request.OperationId
                (withService
                    _.ConflictResolution
                    "conflict resolution"
                    (fun service context -> service.GetActiveSession context))
                (Option.map Mappings.conflictSession)
    resolveConflict =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (withService
                    _.ConflictResolution
                    "conflict resolution"
                    (fun service context ->
                        withPath
                            request.Path
                            (fun path ->
                                service.Resolve
                                    {
                                        Handle = Mappings.conflictHandleFromDto request.Handle
                                        ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                                        Path = path
                                        Resolution = Mappings.conflictResolutionFromDto request.Resolution
                                    }
                                    context
                            )
                    ))
                Mappings.conflictOutcome
    finalizeConflict =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (withService
                    _.ConflictResolution
                    "conflict resolution"
                    (fun service context ->
                        service.Finalize
                            {
                                Handle = Mappings.conflictHandleFromDto request.Handle
                                ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                                Message = request.Message
                            }
                            context
                    ))
                (Option.map RevisionId.value)
    cancelConflict =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (withService
                    _.ConflictResolution
                    "conflict resolution"
                    (fun service context ->
                        service.Cancel
                            {
                                Handle = Mappings.conflictHandleFromDto request.Handle
                                ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                            }
                            context
                    ))
                id
    listObjects =
        fun request ->
            withSession
                event
                request.OperationId
                (withService
                    _.ObjectMaterialization
                    "large object materialization"
                    (fun service context -> service.ListObjects context))
                (Array.map Mappings.objectState)
    materializeObject =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (withService
                    _.ObjectMaterialization
                    "large object materialization"
                    (fun service context -> withPath request.Path (fun path -> service.Materialize path context)))
                id
    dematerializeObject =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (withService
                    _.ObjectMaterialization
                    "large object materialization"
                    (fun service context -> withPath request.Path (fun path -> service.Dematerialize path context)))
                id
    getStoragePolicySettings =
        fun request ->
            withSession
                event
                request.OperationId
                (fun hosted _ -> async {
                    let host = WorkspaceSessionHost.get ()

                    return
                        OperationResult.succeeded (
                            storagePolicySettingsDto (host.GetSettings hosted.Binding.WorkspaceRoot)
                        )
                })
                id
    setStoragePolicySettings =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                false
                (fun hosted context ->
                    let host = WorkspaceSessionHost.get ()
                    let current = host.GetSettings hosted.Binding.WorkspaceRoot

                    let settings = {
                        AutoTrackThresholdMb =
                            request.Settings.AutoPolicyThresholdMb
                            |> Option.defaultValue current.AutoTrackThresholdMb
                        DownloadLargeFiles = request.Settings.MaterializeLargeObjects
                    }

                    host.SetSettings(hosted.Binding.WorkspaceRoot, settings, context)
                )
                id
    // The DataHub ruleset is checked here as well as in the renderer, because the main
    // process is the trust boundary. The main process reads the file size from the
    // workspace before checking the size rule.
    setPathStoragePolicy =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (fun hosted context ->
                    match hosted.Session.StoragePolicy with
                    | None -> async { return serviceUnavailable "storage policies" }
                    | Some service ->
                        withPath
                            request.Path
                            (fun path -> async {
                                let relativePath = RepositoryPath.value path
                                let! fileSize = tryGetRegularFileSize hosted.Binding.WorkspaceRoot relativePath

                                match
                                    Swate.Components.Shared.GitLfsRules.tryGetToggleBlockedReason
                                        relativePath
                                        fileSize
                                        request.UseLargeObjectStorage
                                with
                                | Some reason ->
                                    return validationFailed VersionControlCodes.StoragePolicyBlocked reason
                                | None -> return! service.SetPathPolicy path request.UseLargeObjectStorage context
                            })
                )
                id
    pruneStorage =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                false
                (withService _.Maintenance "storage maintenance" (fun service context -> service.Prune context))
                id
    deduplicateStorage =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                false
                (withService _.Maintenance "storage maintenance" (fun service context -> service.Deduplicate context))
                id
    getRepositoryWebUrl =
        fun request ->
            withSession
                event
                request.OperationId
                (withService
                    _.RepositoryBrowser
                    "a repository browser"
                    (fun service context -> service.GetRepositoryWebUrl context))
                id
    // Removes a stale provider lock only while this operation is the only one of the
    // session, then refreshes and returns the status. Removed files are reported as
    // warnings whose message is the absolute path, because they are not repository
    // paths and a success outcome has no other field for them.
    clearStaleLock =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (fun hosted context -> async {
                    let host = WorkspaceSessionHost.get ()

                    let otherOperations =
                        host.RunningOperationIds hosted.SessionId
                        |> Array.filter (fun id -> id <> request.OperationId)

                    if otherOperations.Length > 0 then
                        return
                            Failed {
                                OperationFailure.create
                                    Concurrency
                                    VersionControlCodes.LockRemovalRefused
                                    "Another operation is still running on this workspace. Wait for it to finish, then try again." with
                                    Retryable = true
                            }
                    else
                        let lockPaths =
                            ProviderComposition.staleLockPaths hosted.Binding.ProviderId hosted.Binding.WorkspaceRoot

                        let removed = lockPaths |> Array.filter removeExistingFile

                        match hosted.Session.Synchronization with
                        | Some synchronization ->
                            let! _ = synchronization.Refresh context
                            ()
                        | None -> ()

                        let! status = hosted.Session.Core.GetStatus context

                        return
                            match status with
                            | Succeeded outcome ->
                                Succeeded {
                                    outcome with
                                        Effect =
                                            if removed.Length > 0 then
                                                Performed
                                            else
                                                NoOp(Some "no stale lock")
                                        Warnings =
                                            removed
                                            |> Array.map (fun path -> {
                                                Code = "lock_removed"
                                                Message = path
                                            })
                                }
                            | other -> other
                })
                Mappings.workspaceStatus
}
