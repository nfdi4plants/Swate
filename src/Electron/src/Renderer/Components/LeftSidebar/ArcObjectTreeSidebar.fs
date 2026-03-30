module Renderer.Components.LeftSidebar.ArcObjectTreeSidebar

open Feliz
open Swate.Components

[<ReactComponent>]
let Main () =
    let appStateCtx = Renderer.Context.AppStateCtx.useAppState ()
    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()

    let viewModel =
        ArcObjectExplorerView.create
            arcObjectCtx.state.Nodes
            arcObjectCtx.state.SelectedExplorerItemId
            fileStateCtx.state.SelectedTreeItemPath
            arcObjectCtx.state.SelectedKindIndices

    let services =
        Renderer.Components.ARCHelper.createArcExplorerServices
            pageStateCtx.setState
            arcObjectCtx.setArcFileState
            arcObjectCtx.setPreviewState
            arcObjectCtx.setStatusMessage

    match appStateCtx.state, viewModel.FilteredTree with
    | None, _
    | _, [] ->
        Html.div [
            prop.className "swt:p-4 swt:text-sm swt:opacity-70"
            prop.text "No ARC objects found."
        ]
    | Some rootRepoPath, _ ->
        Swate.Components.ARCExplorer.CreateArcExplorer
            rootRepoPath
            viewModel.FilteredTree
            arcObjectCtx.state.SelectedExplorerItemId
            fileStateCtx.state.SelectedTreeItemPath
            arcObjectCtx.setSelectedExplorerItemId
            fileStateCtx.setSelectedTreeItemPath
            services
