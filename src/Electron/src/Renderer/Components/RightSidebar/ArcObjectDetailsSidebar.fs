module Renderer.Components.DetailsSidebar.ArcObjectDetailsSidebar

open Feliz
open Swate.Components
open Swate.Components.Shared

[<ReactComponent>]
let Main () =
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()

    let viewModel =
        ArcObjectExplorerView.create
            arcObjectCtx.state.Nodes
            fileStateCtx.state.Selection
            arcObjectCtx.state.SelectedKindIndices

    Html.div [
        prop.className "swt:p-4 swt:h-full"
        prop.children [
            Swate.Components.ARCObjectPanel.Main(
                "ARC Object Details",
                content =
                    ArcObjectExplorerContent.ARCObjectDetailsContent(
                        viewModel.SelectedNode,
                        viewModel.SelectedAncestors,
                        arcObjectCtx.state.PageState,
                        arcObjectCtx.state.ArcFileState,
                        arcObjectCtx.setArcFileState,
                        (fun nodeId ->
                            match Swate.Components.ARCExplorer.tryFindNodeById nodeId arcObjectCtx.state.Nodes with
                            | Some node -> fileStateCtx.setSelection (ArcSelection.forExplorerNode node.id node.path)
                            | None ->
                                fileStateCtx.setSelection (
                                    {
                                        fileStateCtx.state.Selection with
                                            ExplorerNodeId = Some nodeId
                                    }
                                    |> ArcSelection.normalize
                                )),
                        false
                    )
            )
        ]
    ]
