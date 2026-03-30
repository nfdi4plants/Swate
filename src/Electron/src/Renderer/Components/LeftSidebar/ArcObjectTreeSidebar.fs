module Renderer.Components.LeftSidebar.ArcObjectTreeSidebar

open Feliz
open Swate.Components
open Swate.Components.FileExplorerTypes
open Renderer.Types

[<ReactComponent>]
let Main () =
    let appStateCtx = Renderer.Context.AppStateCtx.useAppState ()
    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerCtx.useArcObjectExplorer ()

    let visibleKinds =
        Swate.Components.ARCObjectWidget.SelectedKindLabels arcObjectCtx.state.SelectedKindIndices

    let filteredTree =
        ArcObjectExplorerContent.FilterArcExplorerTreeByKinds visibleKinds arcObjectCtx.state.Nodes

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

    match appStateCtx.state, filteredTree with
    | None, _
    | _, [] ->
        Html.div [
            prop.className "swt:p-4 swt:text-sm swt:opacity-70"
            prop.text "No ARC objects found."
        ]
    | Some rootRepoPath, _ ->
        Swate.Components.ARCExplorer.CreateArcExplorer
            rootRepoPath
            filteredTree
            arcObjectCtx.state.SelectedExplorerItemId
            fileStateCtx.state.SelectedTreeItemPath
            arcObjectCtx.setSelectedExplorerItemId
            fileStateCtx.setSelectedTreeItemPath
            services
