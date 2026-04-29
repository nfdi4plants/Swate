module Renderer.Components.LeftSidebar.ArcObjectTreeSidebar

open Feliz
open Swate.Components
open Swate.Components.ARCObjectExplorer
open Swate.Components.ARCObjectExplorer.Model

[<ReactComponent>]
let Main () =
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()

    let arcObjectCtx =
        Renderer.Context.ArcObjectExplorerContext.useArcObjectExplorerCtx ()

    let viewModel =
        Swate.Components.ARCObjectExplorer.Model.create
            arcObjectCtx.state.Nodes
            fileStateCtx.state.Selection
            KindFilter.arcObjectExplorerOptions
            arcObjectCtx.state.SelectedKindIndices

    let services =
        Renderer.Components.ARCHelper.createArcExplorerServices
            pageStateCtx.setState
            arcObjectCtx.setArcFileState
            arcObjectCtx.setPreviewState
            arcObjectCtx.setStatusMessage

    Html.div [
        prop.className "swt:flex swt:h-full swt:min-h-0 swt:flex-col"
        prop.children [
            match appStateCtx, viewModel.FilteredTree with
            | None, _
            | _, [] ->
                Html.div [
                    prop.className "swt:p-4 swt:text-sm swt:opacity-70"
                    prop.text "No ARC objects found."
                ]
            | Some rootRepoPath, _ ->
                Html.div [
                    prop.className
                        "swt:flex-1 swt:min-h-0 swt:overflow-y-auto swt:overflow-x-hidden swt:[scrollbar-gutter:stable]"
                    prop.children [
                        ARCExplorer.CreateArcExplorer
                            rootRepoPath
                            viewModel.FilteredTree
                            fileStateCtx.state.Selection
                            fileStateCtx.setSelection
                            services
                    ]
                ]
        ]
    ]
