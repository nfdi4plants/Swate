module Renderer.Components.LeftSidebar.GitSidebarPanel

open Feliz

[<ReactComponent>]
let Main () =

    let gitStateCtx = Renderer.Context.GitStateCtx.useGitState ()
    let runStatus = Renderer.Context.GitWorkflow.currentRunStatus gitStateCtx.state

    Swate.Components.GitSidebar.Main(
        status = gitStateCtx.state.Status,
        changedFiles = gitStateCtx.state.ChangedFiles,
        branchOptions = gitStateCtx.state.BranchOptions,
        ?runStatus = runStatus,
        ?selectedFile = gitStateCtx.state.SelectedChangePath,
        ?errorNotice = gitStateCtx.state.ErrorNotice,
        ?warningNotice = gitStateCtx.state.WarningNotice,
        callbacks = {
            OnRefresh = gitStateCtx.refresh
            OnFetch = gitStateCtx.fetch
            OnPull = gitStateCtx.pull
            OnPush = gitStateCtx.push
            OnSync = gitStateCtx.sync
            OnCommitSelection = gitStateCtx.commitSelection
            OnCommitAll = gitStateCtx.commitAll
            OnSaveDownloadLargeFiles = gitStateCtx.saveDownloadLargeFiles
            OnSaveLfsAutoTrackThreshold = gitStateCtx.saveLfsAutoTrackThreshold
            OnCreateBranch = gitStateCtx.createBranch
            OnSwitchBranch = gitStateCtx.switchBranch
            OnSelectChange = gitStateCtx.selectChange
        },
        downloadLargeFiles = gitStateCtx.state.DownloadLargeFiles,
        lfsAutoTrackThresholdMb = gitStateCtx.state.LfsAutoTrackThresholdMb
    )
