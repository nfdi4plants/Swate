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
        onRefresh = gitStateCtx.refresh,
        onFetch = gitStateCtx.fetch,
        onPull = gitStateCtx.pull,
        onPush = gitStateCtx.push,
        onSync = gitStateCtx.sync,
        onCreateBranch = gitStateCtx.createBranch,
        onSelectChange = gitStateCtx.selectChange
    )
