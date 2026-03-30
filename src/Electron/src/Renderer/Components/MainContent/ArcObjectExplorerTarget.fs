module Renderer.Components.MainContent.ArcObjectExplorerTarget

open Feliz
open Swate.Components
open Swate.Components.FileExplorerTypes
open Renderer.Types

[<ReactComponent>]
let Main () =
    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()

    let visibleKinds =
        Swate.Components.ARCObjectWidget.SelectedKindLabels arcObjectCtx.state.SelectedKindIndices

    let filteredTree =
        ArcObjectExplorerContent.FilterArcExplorerTreeByKinds visibleKinds arcObjectCtx.state.Nodes

    let explorerItems = Swate.Components.ARCExplorer.toFileItems filteredTree

    let selectedItemId =
        Swate.Components.ARCExplorer.getSelectedItemId
            filteredTree
            arcObjectCtx.state.SelectedExplorerItemId
            fileStateCtx.state.SelectedTreeItemPath

    let selectedNode =
        ArcObjectExplorerContent.TryGetSelectedNode
            filteredTree
            arcObjectCtx.state.SelectedExplorerItemId
            fileStateCtx.state.SelectedTreeItemPath

    let services: ARCExplorerServices = {
        openPreview =
            fun path -> promise {
                let! result = Renderer.Components.ARCHelper.openPreview path

                match result with
                | Ok loaded ->
                    Renderer.Components.ARCHelper.applyLoadedPreview
                        pageStateCtx.setState
                        arcObjectCtx.setArcFileState
                        arcObjectCtx.setPreviewState
                        arcObjectCtx.setStatusMessage
                        loaded
                    return Ok()
                | Error errorMessage ->
                    Renderer.Components.ARCHelper.applyPreviewError
                        pageStateCtx.setState
                        arcObjectCtx.setArcFileState
                        arcObjectCtx.setPreviewState
                        arcObjectCtx.setStatusMessage
                        errorMessage
                    return Error errorMessage
            }
        setStatusMessage = arcObjectCtx.setStatusMessage
        runToggleLfsMark =
            fun _rootRepoPath relativePath markAsLfs ->
                Renderer.Components.ARCHelper.runToggleLfsMark relativePath markAsLfs
    }

    let handleExplorerSelection =
        Swate.Components.ARCExplorer.createOpenPreviewHandler
            arcObjectCtx.setSelectedExplorerItemId
            fileStateCtx.setSelectedTreeItemPath
            services

    let searchItems =
        ArcObjectExplorerContent.SearchableArcExplorerItems filteredTree explorerItems

    let selectedTitle =
        selectedNode
        |> Option.map _.name
        |> Option.defaultValue "No visible selection"

    let selectedSubtitle =
        selectedNode
        |> Option.map (fun node ->
            let role = if node.isReference then "Reference" else "Canonical"
            $"{ArcObjectExplorerContent.NodeKindLabel node.kind} | {role}")
        |> Option.defaultValue "Selection"

    let searchAction =
        Swate.Components.ARCObjectWidget.SearchAction(
            searchItems,
            (fun (name, _, _) -> name),
            (fun (_, _, item) -> promise { do! handleExplorerSelection item } |> Promise.start),
            itemSubtitle = (fun (_, subtitle, _) -> subtitle)
        )

    Html.div [
        prop.id "arc-object-target"
        prop.className "swt:size-full swt:flex swt:flex-col swt:gap-3 swt:p-4"
        prop.children [
            Swate.Components.ARCObjectWidget.Navbar(
                selectedTitle,
                selectedSubtitle,
                arcObjectCtx.state.SelectedKindIndices,
                arcObjectCtx.setSelectedKindIndices,
                rightActions = searchAction
            )
            match arcObjectCtx.state.StatusMessage with
            | Some statusMessage ->
                Html.div [
                    prop.role.alert
                    prop.className "swt:alert swt:alert-warning swt:text-sm"
                    prop.text statusMessage
                ]
            | None -> Html.none
            Swate.Components.ARCObjectPanel.Main(
                "ARC Object Explorer",
                content =
                    Swate.Components.ARCObjectWidget.ExplorerContent(
                        explorerItems,
                        ?selectedItemId = selectedItemId,
                        onItemClick =
                            (fun item ->
                                if item.Selectable then
                                    promise { do! handleExplorerSelection item } |> Promise.start)
                    )
            )
        ]
    ]
