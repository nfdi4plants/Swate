module Renderer.Components.LeftSidebar.Git.GitSidebarPanel

open Browser.Dom
open Feliz
open Renderer.Components.Helper.ArcVaultHelper
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Electron.Shared.VersionControlTypes

let mutable private dependencyCheckStarted = false

/// The message shown when a version control dependency is missing or too old. The
/// library decides what is required (Git 2.38, Git LFS 3.7 and the LFS filter
/// configuration) and says how to fix it.
let dependencyProblemMessage (statuses: DependencyStatusDto[]) : string option =
    let problems =
        statuses
        |> Array.filter (fun status -> not status.Installed || not status.Compatible)
        |> Array.map (fun status ->
            let state =
                if not status.Installed then
                    "is not installed"
                else
                    match status.Version with
                    | Some version -> $"version {version} is not supported"
                    | None -> "is not supported"

            let remediation =
                status.Remediation
                |> Option.map (fun text -> $" {text}")
                |> Option.defaultValue ""

            $"{status.Component} {state}.{remediation}"
        )

    if problems.Length = 0 then
        None
    else
        Some(String.concat "\n" problems)

/// Remote transfer operations run cancellable git processes, so the cancel button is offered for all of them.
let private isCancellableBusyOperation (busyOperation: Renderer.Context.GitWorkflow.GitBusyOperation) =
    match busyOperation with
    | Renderer.Context.GitWorkflow.GitBusyOperation.FetchingFromRemote
    | Renderer.Context.GitWorkflow.GitBusyOperation.PullingFromRemote
    | Renderer.Context.GitWorkflow.GitBusyOperation.PushingToRemote
    | Renderer.Context.GitWorkflow.GitBusyOperation.CloningRepository _ -> true
    | _ -> false

[<ReactComponent>]
let Main () =

    let gitStateCtx = Renderer.Context.GitStateContext.useGitStateCtx ()

    let leftSidebarCtx =
        Swate.Components.Composite.Layout.LeftSidebarContext.useLeftSidebarCtx ()

    let authState = Renderer.Context.AuthStateContext.useAuthStateCtx ()
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let runStatus = Renderer.Context.GitWorkflow.currentRunStatus gitStateCtx.state
    let errorCtx = useErrorModalCtx ()
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()

    React.useEffect ((fun () -> fun () -> gitStateCtx.sidebarVisibilityChanged false), [||])

    React.useEffect (
        (fun () -> gitStateCtx.sidebarVisibilityChanged leftSidebarCtx.state),
        [| box leftSidebarCtx.state |]
    )

    let onOpenArcError =
        createErrorModalCallback errorCtx.enqueue "Error opening ARC" appStateCtx

    React.useEffectOnce (fun () ->
        if not dependencyCheckStarted then
            dependencyCheckStarted <- true

            Renderer.VersionControlApiClient.checkDependencies {
                OperationId = Renderer.VersionControlApiClient.newOperationId ()
            }
            |> Promise.map (fun result ->
                let statuses =
                    match result with
                    | Ok(OperationResultDto.Succeeded outcome)
                    | Ok(OperationResultDto.PartiallySucceeded(outcome, _)) -> outcome.Value
                    | _ -> [||]

                match result, dependencyProblemMessage statuses with
                | _, Some message ->
                    errorCtx.enqueue (ErrorModalRequest.create (message, title = "Version control dependencies"))
                | Ok(OperationResultDto.Failed failure), None
                | Ok(OperationResultDto.PartiallySucceeded(_, failure)), None ->
                    errorCtx.enqueue (
                        ErrorModalRequest.create (
                            Renderer.Context.GitWorkflow.failureMessage failure,
                            title = "Could not verify Git installation"
                        )
                    )
                | Error message, None ->
                    errorCtx.enqueue (ErrorModalRequest.create (message, title = "Could not verify Git installation"))
                | _ -> ()
            )
            |> ignore
    )

    let remoteActionsEnabled = authState.UsableActiveUser().IsSome

    let remoteActionsWarning =
        if remoteActionsEnabled then
            None
        else
            Some "Sign in to a DataHub account to use fetch, pull, push, update, and remote bootstrap."

    let openArc =
        fun _ ->
            Renderer.Components.Helper.ArcVaultHelper.openArc onOpenArcError
            |> Promise.start

    match gitStateCtx.state.CurrentArcPath with
    | None ->
        Renderer.Components.LeftSidebar.Git.GitSidebarEmptyState.Main(
            title = "Open an ARC to use Git features",
            description = "Source control becomes available after you open or download an ARC.",
            iconClassName = "swt:fluent--folder-open-24-regular",
            primaryAction = {
                Label = "Open ARC"
                IconClassName = "swt:fluent--folder-open-24-regular"
                Disabled = false
                OnClick = openArc
            },
            secondaryAction = {
                Label = "Download ARC"
                IconClassName = "swt:fluent--cloud-arrow-down-24-regular"
                Disabled = false
                OnClick = (fun () -> pageStateCtx.setState (Some Renderer.Types.PageState.DataHubBrowser))
            }
        )
    | Some _ when
        gitStateCtx.state.RepositoryAvailability = Renderer.Context.GitWorkflow.GitRepositoryAvailability.MissingRepository
        ->
        Renderer.Components.LeftSidebar.Git.GitSidebarEmptyState.Main(
            title = "Initialize Git for this ARC",
            description = "The selected ARC folder is not a Git repository yet.",
            iconClassName = "swt:fluent--branch-fork-24-regular",
            primaryAction = {
                Label =
                    if
                        gitStateCtx.state.BusyOperation = Some
                            Renderer.Context.GitWorkflow.GitBusyOperation.InitializingRepository
                    then
                        "Initializing..."
                    else
                        "Initialize Repository"
                IconClassName = "swt:fluent--branch-fork-24-regular"
                Disabled = gitStateCtx.state.BusyOperation.IsSome
                OnClick = fun () -> gitStateCtx.initRepository ()
            }
        )
    | Some _ ->
        let canCancelOperation =
            gitStateCtx.state.BusyOperation |> Option.exists isCancellableBusyOperation

        Swate.Components.Page.GitSidebar.Main(
            status = gitStateCtx.state.Status,
            changedFiles = gitStateCtx.state.ChangedFiles,
            branchOptions = gitStateCtx.state.BranchOptions,
            ?runStatus = runStatus,
            ?hasRemote = Some(gitStateCtx.state.OriginRemoteRepositoryWebUrl.IsSome),
            ?selectedFile = gitStateCtx.state.SelectedChangePath,
            ?errorNotice = gitStateCtx.state.ErrorNotice,
            ?warningNotice = gitStateCtx.state.WarningNotice,
            ?pendingConfirmation = gitStateCtx.state.PendingConfirmation,
            ?publishRenamePrompt =
                (gitStateCtx.state.PendingPublishRename
                 |> Option.map (fun prompt -> {
                     CurrentName = prompt.CurrentName
                     Message = prompt.Message
                 })),
            callbacks = {
                OnRefresh = gitStateCtx.refresh
                OnFetch = gitStateCtx.fetch
                OnPull = gitStateCtx.pull
                OnPush = gitStateCtx.push
                OnUpdateFromOnline = gitStateCtx.updateFromOnline
                OnPrimarySaveSelection = gitStateCtx.primarySaveSelection
                OnPrimarySaveAll = gitStateCtx.primarySaveAll
                OnCommitSelection = gitStateCtx.commitSelection
                OnCommitAll = gitStateCtx.commitAll
                OnDiscardSelection =
                    fun paths ->
                        let conflictedPaths =
                            gitStateCtx.state.ChangedFiles
                            |> Array.filter _.IsConflicted
                            |> Array.map _.Path
                            |> Set.ofArray

                        let discardablePaths =
                            paths |> Array.filter (fun path -> not (Set.contains path conflictedPaths))

                        if discardablePaths.Length > 0 then
                            gitStateCtx.discardSelection discardablePaths
                OnConfirmPendingRemoteAction = gitStateCtx.confirmPendingRemoteAction
                OnCancelPendingRemoteAction = gitStateCtx.cancelPendingRemoteAction
                OnSaveDownloadLargeFiles = gitStateCtx.saveDownloadLargeFiles
                OnSaveLfsAutoTrackThreshold = gitStateCtx.saveLfsAutoTrackThreshold
                OnCreateBranch = gitStateCtx.createBranch
                OnSwitchBranch = gitStateCtx.switchBranch
                OnSelectChange = gitStateCtx.selectChange
                OnPruneLfsCache = gitStateCtx.pruneLfsCache
                OnDedupLfsStorage = gitStateCtx.dedupLfsStorage
                OnCancelOperation = gitStateCtx.cancelOperation
            },
            downloadLargeFiles = gitStateCtx.state.DownloadLargeFiles,
            lfsAutoTrackThresholdMb = gitStateCtx.state.LfsAutoTrackThresholdMb,
            remoteActionsEnabled = remoteActionsEnabled,
            canCancelOperation = canCancelOperation,
            canOpenRemoteRepository = gitStateCtx.state.OriginRemoteRepositoryWebUrl.IsSome,
            onSubmitPublishRename = gitStateCtx.submitPublishRename,
            onCancelPublishRename = gitStateCtx.cancelPublishRename,
            onOpenRemoteRepository =
                (fun () ->
                    match gitStateCtx.state.OriginRemoteRepositoryWebUrl with
                    | Some repositoryWebUrl -> window.``open`` (repositoryWebUrl, "_blank") |> ignore
                    | None -> ()
                ),
            ?remoteActionsWarning = remoteActionsWarning
        )
