module Renderer.Components.LeftSidebar.GitSidebarPanel

open Feliz

[<ReactComponent>]
let Main () =

    let gitStateCtx = Renderer.Context.GitStateCtx.useGitState ()

    Swate.Components.GitSidebar.Main(
        status = gitStateCtx.state.Status,
        changedFiles = gitStateCtx.state.ChangedFiles,
        branchOptions = gitStateCtx.state.BranchOptions,
        ?currentProgress = gitStateCtx.state.CurrentProgress,
        ?selectedFile = gitStateCtx.state.SelectedChangePath,
        ?busyNotice = gitStateCtx.state.BusyNotice,
        ?errorNotice = gitStateCtx.state.ErrorNotice,
        ?warningNotice = gitStateCtx.state.WarningNotice,
        onRefresh = gitStateCtx.refresh,
        onFetch = gitStateCtx.fetch,
        onPull = gitStateCtx.pull,
        onPush = gitStateCtx.push,
        onSync = gitStateCtx.sync,
        onCommitSelection = gitStateCtx.commitSelection,
        onCommitAll = gitStateCtx.commitAll,
        downloadLargeFiles = gitStateCtx.state.DownloadLargeFiles,
        onSaveDownloadLargeFiles = gitStateCtx.saveDownloadLargeFiles,
        lfsAutoTrackThresholdMb = gitStateCtx.state.LfsAutoTrackThresholdMb,
        onSaveLfsAutoTrackThreshold = gitStateCtx.saveLfsAutoTrackThreshold,
        onCreateBranch = gitStateCtx.createBranch,
        onSwitchBranch = gitStateCtx.switchBranch,
        onSelectChange = gitStateCtx.selectChange
    )
