module Renderer.Components.RightSidebar.ArcObjectDetailsSidebar

open Feliz
open Swate.Components

[<ReactComponent>]
let Main () =
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()

    let visibleKinds =
        Swate.Components.ARCObjectWidget.SelectedKindLabels arcObjectCtx.state.SelectedKindIndices

    let filteredTree =
        ArcObjectExplorerContent.FilterArcExplorerTreeByKinds visibleKinds arcObjectCtx.state.Nodes

    let selectedNodeLineage =
        ArcObjectExplorerContent.TryGetSelectedNodeLineage
            filteredTree
            arcObjectCtx.state.SelectedExplorerItemId
            fileStateCtx.state.SelectedTreeItemPath

    let selectedNode =
        selectedNodeLineage
        |> Option.map fst

    let selectedAncestors =
        selectedNodeLineage
        |> Option.map snd
        |> Option.defaultValue []

    Html.div [
        prop.className "swt:p-4 swt:h-full"
        prop.children [
            Swate.Components.ARCObjectPanel.Main(
                "ARC Object Details",
                content =
                    ArcObjectExplorerContent.ARCObjectDetailsContent
                        selectedNode
                        selectedAncestors
                        arcObjectCtx.state.PreviewState
                        arcObjectCtx.state.ArcFileState
                        arcObjectCtx.setArcFileState
                        false
            )
        ]
    ]
