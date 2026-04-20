module Renderer.Components.LeftSidebar.ArcObjectTreeSidebar

open Feliz
open Swate.Components
open Swate.Components.ARCObjectExplorer

[<ReactComponent>]
let Main () =
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerContext.useArcObjectExplorerCtx ()

    let viewModel =
        ArcObjectExplorerView.create
            arcObjectCtx.state.Nodes
            fileStateCtx.state.Selection
            KindFilter.ArcObjectExplorerOptions
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
        ARCExplorer.CreateArcExplorer
            rootRepoPath
            viewModel.FilteredTree
            fileStateCtx.state.Selection
            fileStateCtx.setSelection
            services
