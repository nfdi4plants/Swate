module Renderer.Components.LeftSidebar.GitSidebarPanel

open Browser.Dom
open Feliz
open Renderer.Components.Helper.ArcVaultHelper
open Swate.Components.Primitive.ErrorModal.Context

let mutable private gitVersionCheckStarted = false

[<ReactComponent>]
let Main () =

    let gitStateCtx = Renderer.Context.GitStateContext.useGitStateCtx ()
    let authState = Renderer.Context.AuthStateContext.useAuthStateCtx ()
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let runStatus = Renderer.Context.GitWorkflow.currentRunStatus gitStateCtx.state
    let remoteProjectName, setRemoteProjectName = React.useState ""
    let errorCtx = useErrorModalCtx ()
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()

    let onOpenArcError =
        createErrorModalCallback errorCtx.enqueue "Error opening ARC" appStateCtx

    React.useEffectOnce (fun () ->
        if not gitVersionCheckStarted then
            gitVersionCheckStarted <- true

            Renderer.GitApiClient.checkGitVersions ()
            |> Promise.map (
                function
                | Ok() -> ()
                | Error message -> Browser.Dom.window.alert message
            )
            |> ignore
    )

    let remoteActionsEnabled = authState.UsableActiveUser().IsSome

    let remoteActionsWarning =
        if remoteActionsEnabled then
            None
        else
            Some "Sign in to a DataHub account to use fetch, pull, push, update, and remote bootstrap."

    let normalizedRemoteProjectName =
        remoteProjectName.Trim()
        |> function
            | "" -> None
            | value -> Some value

    let openArc =
        fun _ ->
            Renderer.Components.Helper.ArcVaultHelper.openArc onOpenArcError
            |> Promise.start

    match gitStateCtx.state.CurrentArcPath with
    | None ->
        Renderer.Components.LeftSidebar.GitSidebarEmptyState.Main(
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
        Renderer.Components.LeftSidebar.GitSidebarEmptyState.Main(
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
                OnClick =
                    (fun () ->
                        gitStateCtx.initRepository (
                            if remoteActionsEnabled then
                                normalizedRemoteProjectName
                            else
                                None
                        )
                    )
            },
            extraContent =
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.label [
                            prop.className "swt:flex swt:flex-col swt:gap-1"
                            prop.children [
                                Html.span [
                                    prop.className "swt:text-xs swt:font-medium swt:text-base-content/70"
                                    prop.text "DataHub repository name"
                                ]
                                Html.input [
                                    prop.testId "GitSidebarRemoteProjectNameInput"
                                    prop.className "swt:input swt:input-bordered swt:w-full"
                                    prop.disabled (gitStateCtx.state.BusyOperation.IsSome || not remoteActionsEnabled)
                                    prop.value remoteProjectName
                                    prop.placeholder "my-arc"
                                    prop.onChange setRemoteProjectName
                                ]
                            ]
                        ]
                        Html.p [
                            prop.className "swt:text-xs swt:text-base-content/60"
                            prop.text
                                "If you provide a name, Swate creates a project on the active DataHub and adds it as origin right after git init."
                        ]
                    ]
                ],
            ?infoText = remoteActionsWarning
        )
    | Some _ ->
        Swate.Components.Page.GitSidebar.Main(
            status = gitStateCtx.state.Status,
            changedFiles = gitStateCtx.state.ChangedFiles,
            branchOptions = gitStateCtx.state.BranchOptions,
            ?runStatus = runStatus,
            ?selectedFile = gitStateCtx.state.SelectedChangePath,
            ?errorNotice = gitStateCtx.state.ErrorNotice,
            ?warningNotice = gitStateCtx.state.WarningNotice,
            ?pendingConfirmation = gitStateCtx.state.PendingConfirmation,
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
                OnDiscardSelection = gitStateCtx.discardSelection
                OnConfirmPendingRemoteAction = gitStateCtx.confirmPendingRemoteAction
                OnCancelPendingRemoteAction = gitStateCtx.cancelPendingRemoteAction
                OnSaveDownloadLargeFiles = gitStateCtx.saveDownloadLargeFiles
                OnSaveLfsAutoTrackThreshold = gitStateCtx.saveLfsAutoTrackThreshold
                OnCreateBranch = gitStateCtx.createBranch
                OnSwitchBranch = gitStateCtx.switchBranch
                OnSelectChange = gitStateCtx.selectChange
                OnPruneLfsCache = gitStateCtx.pruneLfsCache
                OnDedupLfsStorage = gitStateCtx.dedupLfsStorage
            },
            downloadLargeFiles = gitStateCtx.state.DownloadLargeFiles,
            lfsAutoTrackThresholdMb = gitStateCtx.state.LfsAutoTrackThresholdMb,
            remoteActionsEnabled = remoteActionsEnabled,
            canOpenRemoteRepository = gitStateCtx.state.OriginRemoteRepositoryWebUrl.IsSome,
            onOpenRemoteRepository =
                (fun () ->
                    match gitStateCtx.state.OriginRemoteRepositoryWebUrl with
                    | Some repositoryWebUrl -> window.``open`` (repositoryWebUrl, "_blank") |> ignore
                    | None -> ()
                ),
            ?remoteActionsWarning = remoteActionsWarning
        )