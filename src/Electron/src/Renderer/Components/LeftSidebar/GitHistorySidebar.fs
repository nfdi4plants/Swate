module Renderer.Components.LeftSidebar.GitHistorySidebar

open Feliz
open Renderer.Context.GitHistoryContext
open Swate.Components.Page.GitHistory

[<ReactComponent>]
let Main () =
    let history = useGitHistoryCtx ()
    let root = Renderer.Context.AppStateContext.useAppStateCtx ()
    let state = history.state

    React.useEffect (
        (fun () -> history.ensureLoaded ()),
        [|
            box root
            box state.Initialized
            box state.Stale
            box state.Loading
        |]
    )

    let commits = state.Page |> Option.map _.Commits |> Option.defaultValue [||]

    GitHistory.GitHistory(
        commits = commits,
        changes = state.Changes,
        expandedRevisions = state.ExpandedRevisions,
        onToggleCommit = history.toggleCommit,
        onSelectFile = history.selectFile,
        onLoadMore = history.loadMore,
        onRefresh = history.refresh,
        ?branchName = (state.Page |> Option.bind _.BranchName),
        ?selectedRevision = state.SelectedRevision,
        ?selectedPath = state.SelectedPath,
        loading = (state.Loading || not state.Initialized),
        hasMore = (state.Page |> Option.bind _.NextSkip |> Option.isSome),
        ?error = state.Error,
        scrollTop = history.scrollTop (),
        onScroll = history.saveScroll
    )
