module Renderer.Components.DetailsSidebar.ArcObjectDetailsSidebar

open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.ARCObjectExplorer
open Swate.Components.ARCObjectExplorer.Model

[<ReactComponent>]
let Main () =
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerContext.useArcObjectExplorerCtx ()

    let viewModel =
        Swate.Components.ARCObjectExplorer.Model.create
            arcObjectCtx.state.Nodes
            fileStateCtx.state.Selection
            KindFilter.arcObjectExplorerOptions
            arcObjectCtx.state.SelectedKindIndices

    Html.div [
        prop.className "swt:p-4 swt:h-full"
        prop.children [
            ARCObjectPanel.Main(
                "ARC Object Details",
                content =
                    ArcObjectExplorerContent.ARCObjectDetailsContent(
                        Swate.Components.ARCObjectExplorer.Model.selectedNode viewModel,
                        Swate.Components.ARCObjectExplorer.Model.selectedAncestors viewModel,
                        arcObjectCtx.state.PageState,
                        arcObjectCtx.state.ArcFileState,
                        arcObjectCtx.setArcFileState,
                        (fun nodeId ->
                            match ARCExplorer.tryFindNodeById nodeId arcObjectCtx.state.Nodes with
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

