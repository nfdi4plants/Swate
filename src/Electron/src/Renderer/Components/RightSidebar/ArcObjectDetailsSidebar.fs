module Renderer.Components.RightSidebar.ArcObjectDetailsSidebar

open Feliz
open Swate.Components

[<ReactComponent>]
let Main () =
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()

    let viewModel =
        ArcObjectExplorerView.create
            arcObjectCtx.state.Nodes
            arcObjectCtx.state.SelectedExplorerItemId
            fileStateCtx.state.SelectedTreeItemPath
            arcObjectCtx.state.SelectedKindIndices

    Html.div [
        prop.className "swt:p-4 swt:h-full"
        prop.children [
            Swate.Components.ARCObjectPanel.Main(
                "ARC Object Details",
                content =
                    ArcObjectExplorerContent.ARCObjectDetailsContent
                        viewModel.SelectedNode
                        viewModel.SelectedAncestors
                        arcObjectCtx.state.PreviewState
                        arcObjectCtx.state.ArcFileState
                        arcObjectCtx.setArcFileState
                        false
            )
        ]
    ]
