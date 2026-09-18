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

/// A session that could not be opened keeps its resolution code (unmanaged or
/// ambiguous) and otherwise reports that the provider refused to open it.
let private sessionUnavailable (failure: OperationFailure) : OperationFailure =
    if
        failure.Code = VersionControlCodes.WorkspaceUnmanaged
        || failure.Code = VersionControlCodes.WorkspaceAmbiguous
    then
        failure
    else
        {
            failure with
                Code = VersionControlCodes.SessionUnavailable
                Details = Array.append [| $"provider code: {failure.Code}" |] failure.Details
        }

let private failedDto (failure: OperationFailure) : OperationResultDto<'T> =
    OperationResultDto.Failed(Mappings.failure failure)

let private validationFailed (code: string) (message: string) : OperationResult<'T> =
    Failed(OperationFailure.create Validation code message)

/// Runs one tracked operation against a session-independent target.
let private runTracked
    (host: WorkspaceSessionHost.WorkspaceSessionHost)
    (bridge: RendererBridge)
    (sessionId: string)
    (operationId: string)
    (operation: OperationContext -> Async<OperationResult<'T>>)
    : JS.Promise<OperationResult<'T>> =
    promise {
        let tracked = host.BeginOperation(sessionId, operationId, bridge.Progress)
        bridge.Started tracked.Key

        try
            return! operation tracked.Context |> Async.StartAsPromise
        finally
            tracked.Complete()
    }

/// Opens the vault session of the calling window and runs the operation in it. The
/// operation is registered before the first await, so a cancel that arrives right
/// after the call started is honored even while the session is still opening.
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
                let! opened = host.OpenSession(arcPath, tracked.Context) |> Async.StartAsPromise

                match opened with
                | Succeeded outcome
                | PartiallySucceeded(outcome, _) ->
                    let hosted = outcome.Value
                    host.AssignSession(operationId, hosted.SessionId)

                    bridge.Started {
                        SessionId = hosted.SessionId
                        OperationId = operationId
                    }

                    let! result = operation hosted tracked.Context |> Async.StartAsPromise
                    return Ok(Mappings.result mapValue result)
                | Failed failure -> return Ok(failedDto (sessionUnavailable failure))
            finally
                tracked.Complete()
    }

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
                withBusyWriting
                    vault
                    (fun () -> promise {
                        let! result = withSession event operationId operation mapValue

                        let changed =
                            match result with
                            | Ok(OperationResultDto.Succeeded outcome) -> outcome.Effect = OperationEffectDto.Performed
                            | Ok(OperationResultDto.PartiallySucceeded _) -> true
                            | Ok(OperationResultDto.Failed failure) -> failure.StateChanged
                            | Error _ -> false

                        if refreshTree && changed then
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

let private withPaths (paths: string[]) (call: RepositoryPath[] -> Async<OperationResult<'T>>) =
    match Mappings.tryRepositoryPaths paths with
    | Ok repositoryPaths -> call repositoryPaths
    | Error failure -> async { return Failed failure }

let private withProviderRef (value: string) (call: ProviderRef -> Async<OperationResult<'T>>) =
    match Mappings.tryProviderRef value with
    | Ok reference -> call reference
    | Error failure -> async { return Failed failure }

let private tryFactoryFor (host: WorkspaceSessionHost.WorkspaceSessionHost) (providerId: ProviderId) =
    ProviderResolver.tryGetFactory host.Runtime.Catalog providerId

let private persistBinding (host: WorkspaceSessionHost.WorkspaceSessionHost) (binding: WorkspaceBinding) =
    match host.Runtime.Bindings.Save binding with
    | Ok() -> Succeeded(OperationOutcome.performed binding)
    | Error message -> Failed(OperationFailure.create ProviderError "binding_not_persisted" message)

/// A provisioning step that produced a binding. The binding is persisted before the
/// caller sees the result so a crash after provisioning still leaves the vault bound.
let private provision
    (host: WorkspaceSessionHost.WorkspaceSessionHost)
    (bridge: RendererBridge)
    (operationId: string)
    (run: OperationContext -> Async<OperationResult<WorkspaceBinding>>)
    : JS.Promise<Result<OperationResultDto<string>, exn>> =
    promise {
        let! result =
            runTracked
                host
                bridge
                ""
                operationId
                (fun context -> async {
                    let! provisioned = run context

                    match provisioned with
                    | Succeeded outcome ->
                        match persistBinding host outcome.Value with
                        | Succeeded _ -> return Succeeded outcome
                        | other -> return other
                    | PartiallySucceeded(outcome, failure) ->
                        match persistBinding host outcome.Value with
                        | Succeeded _ -> return PartiallySucceeded(outcome, failure)
                        | other -> return other
                    | Failed failure -> return Failed failure
                })

        return Ok(Mappings.result (fun (binding: WorkspaceBinding) -> binding.WorkspaceRoot) result)
    }

let private removeExistingFile (path: string) =
    if Main.Bindings.Filesystem.existsSync path then
        Main.Bindings.Filesystem.unlinkSync path
        true
    else
        false

let api (event: IpcMainInvokeEvent) : IVersionControlApi = {
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
                    match ProviderComposition.tryCreateLocation request.ProviderLocation request.DisplayName with
                    | Error message -> return validationFailed VersionControlCodes.LocationUnsupported message
                    | Ok location ->
                        match tryFactoryFor host location.ProviderId with
                        | None ->
                            return
                                validationFailed
                                    VersionControlCodes.LocationUnsupported
                                    $"No provider is registered for '{ProviderId.value location.ProviderId}'."
                        | Some factory ->
                            let targetRef =
                                request.TargetRef
                                |> Option.map ProviderRef.tryCreate
                                |> Option.bind (
                                    function
                                    | Ok reference -> Some reference
                                    | Error _ -> None
                                )

                            return!
                                factory.Clone
                                    {
                                        Location = location
                                        TargetPath = request.TargetPath
                                        TargetRef = targetRef
                                        MaterializeAllObjects = request.MaterializeAllObjects
                                    }
                                    context
                })
    initializeWorkspace =
        fun request ->
            let host = WorkspaceSessionHost.get ()
            let bridge = tryBridgeFromEvent event

            provision
                host
                bridge
                request.OperationId
                (fun context -> async {
                    match tryFactoryFor host ProviderComposition.defaultProviderId with
                    | None ->
                        return
                            validationFailed
                                VersionControlCodes.LocationUnsupported
                                "The default version control provider is not registered."
                    | Some factory ->
                        return!
                            factory.Initialize
                                {
                                    TargetPath = request.TargetPath
                                    Location = None
                                }
                                context
                })
    bindWorkspace =
        fun request -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let host = WorkspaceSessionHost.get ()
                let bridge = tryBridgeFromEvent event

                let! bound =
                    runTracked
                        host
                        bridge
                        ""
                        request.OperationId
                        (fun context -> async {
                            match
                                ProviderComposition.tryCreateLocation request.ProviderLocation request.DisplayName
                            with
                            | Error message -> return validationFailed VersionControlCodes.LocationUnsupported message
                            | Ok location ->
                                match tryFactoryFor host location.ProviderId with
                                | None ->
                                    return
                                        validationFailed
                                            VersionControlCodes.LocationUnsupported
                                            $"No provider is registered for '{ProviderId.value location.ProviderId}'."
                                | Some factory ->
                                    let! bindResult =
                                        factory.Bind
                                            {
                                                WorkspaceRoot = arcPath
                                                Location = location
                                            }
                                            context

                                    match bindResult with
                                    | Succeeded outcome ->
                                        match persistBinding host outcome.Value with
                                        | Succeeded _ ->
                                            // The open session keeps the location it was opened with.
                                            let! reopened = host.ReopenSession(arcPath, context)
                                            return reopened
                                        | Failed failure -> return Failed failure
                                        | PartiallySucceeded(_, failure) -> return Failed failure
                                    | PartiallySucceeded(_, failure)
                                    | Failed failure -> return Failed failure
                        })

                return
                    Ok(
                        Mappings.result
                            (fun (hosted: WorkspaceSessionHost.HostedSession) ->
                                Mappings.sessionInfo hosted.SessionId hosted.Session
                            )
                            bound
                    )
        }
    cancelOperation =
        fun key -> promise {
            let host = WorkspaceSessionHost.get ()
            return Ok(host.Cancel(key.SessionId, key.OperationId))
        }
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
                            if failure.IsNone then
                                let! checked = factory.CheckDependencies context

                                match checked with
                                | Succeeded outcome
                                | PartiallySucceeded(outcome, _) -> statuses <- Array.append statuses outcome.Value
                                | Failed error -> failure <- Some error

                        return
                            match failure with
                            | Some error -> Failed error
                            | None -> OperationResult.succeeded statuses
                    })

            return Ok(Mappings.result (Array.map Mappings.dependencyStatus) result)
        }
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
                        // The first provider that supports the component installs it.
                        let factories = ProviderResolver.factories host.Runtime.Catalog
                        let mutable outcome: OperationResult<DependencyStatus> option = None

                        for factory in factories do
                            match outcome with
                            | Some(Succeeded _) -> ()
                            | _ ->
                                let! installed = factory.InstallDependency request.Component context

                                match installed, outcome with
                                | Failed failure, Some _ when failure.Category = Unsupported -> ()
                                | result, _ -> outcome <- Some result

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
                    let baseRef =
                        request.BaseRef
                        |> Option.map ProviderRef.tryCreate
                        |> Option.bind (
                            function
                            | Ok reference -> Some reference
                            | Error _ -> None
                        )

                    hosted.Session.Core.CreateRef
                        {
                            Name = request.Name
                            BaseRef = baseRef
                            SwitchTo = request.SwitchTo
                            ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                        }
                        context
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
                        let expectedTarget =
                            request.ExpectedTargetRevision
                            |> Option.map RevisionId.tryCreate
                            |> Option.bind (
                                function
                                | Ok revision -> Some revision
                                | Error _ -> None
                            )

                        service.Publish
                            {
                                ExpectedWorkspaceVersion = request.ExpectedWorkspaceVersion
                                ExpectedTargetRevision = expectedTarget
                            }
                            context
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
                (withService _.StoragePolicy "storage policies" (fun service context -> service.GetSettings context))
                Mappings.storageSettings
    setStoragePolicySettings =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                false
                (withService
                    _.StoragePolicy
                    "storage policies"
                    (fun service context ->
                        service.SetSettings (Mappings.storageSettingsFromDto request.Settings) context
                    ))
                id
    setPathStoragePolicy =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (withService
                    _.StoragePolicy
                    "storage policies"
                    (fun service context ->
                        withPath
                            request.Path
                            (fun path -> service.SetPathPolicy path request.UseLargeObjectStorage context)
                    ))
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
    clearStaleLock =
        fun request ->
            withMutatingSession
                event
                request.OperationId
                true
                (fun hosted context -> async {
                    let host = WorkspaceSessionHost.get ()

                    // The operation that carries this request is the only one allowed to run.
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
                                        AffectedPaths = removed
                                        Effect =
                                            if removed.Length > 0 then
                                                Performed
                                            else
                                                NoOp(Some "no stale lock")
                                }
                            | other -> other
                })
                Mappings.workspaceStatus
}
