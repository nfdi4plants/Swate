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

    let iconColorClassForItemType =
        function
        | "ARC" -> Some "swt:text-base-content/70"
        | "Group" -> Some "swt:text-base-content/60"
        | "Study" -> Some "swt:text-secondary"
        | "Assay" -> Some "swt:text-success"
        | "Workflow" -> Some "swt:text-primary"
        | "Run" -> Some "swt:text-warning"
        | "Table" -> Some "swt:text-info"
        | "DataMap" -> Some "swt:text-accent"
        | "Note" -> Some "swt:text-error"
        | "Sample" -> Some "swt:text-base-content/70"
        | _ -> None

    let iconColorClass (item: FileItem) =
        iconColorClassForItemType item.ItemType

    let private iconForNode (node: ArcExplorerNode) =
        match node.kind with
        | ArcExplorerNodeKind.Arc
        | ArcExplorerNodeKind.Group -> "swt:fluent--folder-24-regular"
        | ArcExplorerNodeKind.Table -> "swt:fluent--table-24-regular"
        | ArcExplorerNodeKind.DataMap -> "swt:fluent--database-24-regular"
        | ArcExplorerNodeKind.Sample -> "swt:fluent--tag-24-regular"
        | ArcExplorerNodeKind.Note
        | ArcExplorerNodeKind.Study
        | ArcExplorerNodeKind.Assay
        | ArcExplorerNodeKind.Workflow
        | ArcExplorerNodeKind.Run -> "swt:fluent--document-24-regular"

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

    let rec private toFileItem (node: ArcExplorerNode) =
        let children = node.children |> List.map toFileItem

        let isDirectory =
            node.kind = ArcExplorerNodeKind.Arc
            || node.kind = ArcExplorerNodeKind.Group
            || not (List.isEmpty children)

        if isDirectory then
            {
                FileTree.createFolder node.name node.path (iconForNode node) with
                    Id = node.id
                    ItemType = ArcExplorerNodeKind.label node.kind
                    IsExpanded = node.kind = ArcExplorerNodeKind.Arc
                    IsLFS = node.isLfs
                    Selectable = node.isSelectable
                    Children = Some children
            }
        else
            {
                FileTree.createFile node.name node.path (iconForNode node) with
                    Id = node.id
                    ItemType = ArcExplorerNodeKind.label node.kind
                    IsLFS = node.isLfs
                    Selectable = node.isSelectable
            }

    let toFileItems (nodes: ArcExplorerNode list) = nodes |> List.map toFileItem

    let getSelectedItemId
        (nodes: ArcExplorerNode list)
        (selectedExplorerItemId: string option)
        (selectedTreeItemPath: string option)
        =
        selectedExplorerItemId
        |> Option.orElseWith (fun () ->
            selectedTreeItemPath |> Option.bind (fun path -> tryFindNodeIdByPath path nodes))

    let createOpenPreviewHandler
        (setSelectedExplorerItemId: string option -> unit)
        (setSelectedTreeItemPath: string option -> unit)
        (services: ARCExplorerServices)
        (item: FileItem)
        =
        promise {
            match item.Path with
            | None ->
                setSelectedTreeItemPath None
                setSelectedExplorerItemId (Some item.Id)
                services.setStatusMessage None
            | Some path ->
                let selectedPath = normalizePath path
                let previewPath = PathHelpers.resolveArcPreviewPath path

                if previewPath <> selectedPath then
                    console.log ($"[Renderer] Redirecting Datamap click to file: {previewPath}")
                else
                    console.log ($"[Renderer] Opening file: {previewPath}")

                setSelectedTreeItemPath (Some selectedPath)
                setSelectedExplorerItemId (Some item.Id)
                let! result = services.openPreview previewPath

                match result with
                | Ok () -> ()
                | Error errorMessage ->
                    console.log ($"[Renderer] Error: {errorMessage}")
                    services.setStatusMessage (Some $"Could not open preview for '{item.Name}': {errorMessage}")
        }

    [<ReactComponent>]
    let CreateArcExplorer
        (rootRepoPath: string)
        (nodes: ArcExplorerNode list)
        (selectedExplorerItemId: string option)
        (selectedTreeItemPath: string option)
        (setSelectedExplorerItemId: string option -> unit)
        (setSelectedTreeItemPath: string option -> unit)
        (services: ARCExplorerServices)
        =
        let selectedItemId = getSelectedItemId nodes selectedExplorerItemId selectedTreeItemPath

        let toggleLfsMark =
            FileExplorerGitLfsHelper.ToggleLfsMark(services.setStatusMessage, services.runToggleLfsMark rootRepoPath)

        let contextMenuItems (item: FileItem) =
            FileExplorerGitLfsHelper.ContextMenuItems(item, toggleLfsMark)

        let openPreview item = promise { createOpenPreviewHandler setSelectedExplorerItemId setSelectedTreeItemPath services item |> Promise.start } |> Promise.start

        if List.isEmpty nodes then
            Html.none
        else
            let items = toFileItems nodes

            (
                Swate.Components.FileExplorer.FileExplorer(
                    initialItems = items,
                    onItemClick = openPreview,
                    onContextMenu = contextMenuItems,
                    ?selectedItemId = Some selectedItemId,
                    showBreadcrumbs = false,
                    directoryInteractionMode = DirectoryInteractionMode.ToggleOnSingleClickSelectOnDoubleClick,
                    useDirectoryChevronToggle = true,
                    getItemIconClass = iconColorClass
                )
            )
