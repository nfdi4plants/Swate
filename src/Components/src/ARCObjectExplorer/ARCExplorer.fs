namespace Swate.Components

open Browser.Dom
open Swate.Components.Shared
open Swate.Components.FileExplorerTypes
open Fable.Core
open Feliz

//Replace with ARCtrl
[<RequireQualifiedAccess>]
module ARCExplorer =

    let private normalizePath = PathHelpers.normalizePath

    type ArcExplorerAppearance = {
        Icon: FileItemIcon
        IconTone: FileItemIconTone option
    }

    let appearanceForNodeKind =
        function
        | ArcExplorerNodeKind.Arc ->
            {
                Icon = FileItemIcon.Folder
                IconTone = Some FileItemIconTone.BaseMuted
            }
        | ArcExplorerNodeKind.Group ->
            {
                Icon = FileItemIcon.Folder
                IconTone = Some FileItemIconTone.BaseSubtle
            }
        | ArcExplorerNodeKind.Table ->
            {
                Icon = FileItemIcon.Table
                IconTone = Some FileItemIconTone.Info
            }
        | ArcExplorerNodeKind.DataMap ->
            {
                Icon = FileItemIcon.Database
                IconTone = Some FileItemIconTone.Accent
            }
        | ArcExplorerNodeKind.Sample ->
            {
                Icon = FileItemIcon.Tag
                IconTone = Some FileItemIconTone.BaseMuted
            }
        | ArcExplorerNodeKind.Note ->
            {
                Icon = FileItemIcon.Document
                IconTone = Some FileItemIconTone.Error
            }
        | ArcExplorerNodeKind.Study ->
            {
                Icon = FileItemIcon.Document
                IconTone = Some FileItemIconTone.Secondary
            }
        | ArcExplorerNodeKind.Assay ->
            {
                Icon = FileItemIcon.Document
                IconTone = Some FileItemIconTone.Success
            }
        | ArcExplorerNodeKind.Workflow ->
            {
                Icon = FileItemIcon.Document
                IconTone = Some FileItemIconTone.Primary
            }
        | ArcExplorerNodeKind.Run ->
            {
                Icon = FileItemIcon.Document
                IconTone = Some FileItemIconTone.Warning
            }

    let private fileItemForNode
        (createItem: string -> string option -> FileItemIcon -> FileItem)
        (node: ArcExplorerNode)
        =
        let appearance = appearanceForNodeKind node.kind

        {
            createItem node.name node.path appearance.Icon with
                Id = node.id
                ItemType = ArcExplorerNodeKind.label node.kind
                IconTone = appearance.IconTone
                IsLFS = node.isLfs
                Selectable = node.isSelectable
        }

    let rec private toFileItem (node: ArcExplorerNode) =
        let children = node.children |> List.map toFileItem

        let isDirectory =
            node.kind = ArcExplorerNodeKind.Arc
            || node.kind = ArcExplorerNodeKind.Group
            || not (List.isEmpty children)

        if isDirectory then
            {
                fileItemForNode FileTree.createFolder node with
                    IsExpanded = node.kind = ArcExplorerNodeKind.Arc
                    Children = Some children
            }
        else
            fileItemForNode FileTree.createFile node

    let rec private tryFindNodeIdByPath (path: string) (nodes: ArcExplorerNode list) =
        let normalizedTargetPath = normalizePath path

        let rec collectMatches (nodes: ArcExplorerNode list) =
            nodes
            |> List.collect (fun node ->
                let currentMatch =
                    match node.path with
                    | Some nodePath when normalizePath nodePath = normalizedTargetPath -> [ node ]
                    | _ -> []

                currentMatch @ collectMatches node.children)

        let matches = collectMatches nodes

        matches
        |> List.tryFind (fun node -> not node.isReference)
        |> Option.orElseWith (fun () -> matches |> List.tryHead)
        |> Option.map (fun node -> node.id)

    let rec tryFindNodeById (nodeId: string) (nodes: ArcExplorerNode list) =
        nodes
        |> List.tryPick (fun node ->
            if node.id = nodeId then
                Some node
            else
                tryFindNodeById nodeId node.children)

    let toFileItems (nodes: ArcExplorerNode list) = nodes |> List.map toFileItem

    let getSelectedItemId (nodes: ArcExplorerNode list) (selection: ArcSelection) =
        selection.ExplorerNodeId
        |> Option.bind (fun nodeId -> tryFindNodeById nodeId nodes |> Option.map _.id)
        |> Option.orElseWith (fun () ->
            selection.TreePath |> Option.bind (fun path -> tryFindNodeIdByPath path nodes))

    let createOpenPreviewHandler
        (setSelection: ArcSelection -> unit)
        (services: ARCExplorerServices)
        (item: FileItem)
        =
        promise {
            match item.Path with
            | None ->
                setSelection (ArcSelection.forExplorerNode item.Id None)
                services.setStatusMessage None
            | Some path ->
                let selectedPath = normalizePath path
                let viewPath = PathHelpers.resolveArcViewPath path

                if viewPath <> selectedPath then
                    console.log ($"[Renderer] Redirecting Datamap click to file: {viewPath}")
                else
                    console.log ($"[Renderer] Opening file: {viewPath}")

                setSelection (ArcSelection.forExplorerNode item.Id (Some selectedPath))
                let! result = services.openView viewPath

                match result with
                | Ok () -> ()
                | Error errorMessage ->
                    console.log ($"[Renderer] Error: {errorMessage}")
                    services.setStatusMessage (Some $"Could not open view for '{item.Name}': {errorMessage}")
        }

    [<ReactComponent>]
    let CreateArcExplorer
        (rootRepoPath: string)
        (nodes: ArcExplorerNode list)
        (selection: ArcSelection)
        (setSelection: ArcSelection -> unit)
        (services: ARCExplorerServices)
        =
        let selectedItemId = getSelectedItemId nodes selection

        let toggleLfsMark =
            FileExplorerGitLfsHelper.ToggleLfsMark(services.setStatusMessage, services.runToggleLfsMark rootRepoPath)

        let contextMenuItems (item: FileItem) =
            FileExplorerGitLfsHelper.ContextMenuItems(item, toggleLfsMark)

        let openView item = promise { createOpenPreviewHandler setSelection services item |> Promise.start } |> Promise.start

        if List.isEmpty nodes then
            Html.none
        else
            let items = toFileItems nodes

            (
                Swate.Components.FileExplorer.FileExplorer(
                    initialItems = items,
                    onItemClick = openView,
                    onContextMenu = contextMenuItems,
                    ?selectedItemId = Some selectedItemId,
                    showBreadcrumbs = false,
                    directoryInteractionMode = DirectoryInteractionMode.ToggleOnSingleClickSelectOnDoubleClick,
                    useDirectoryChevronToggle = true
                )
            )
