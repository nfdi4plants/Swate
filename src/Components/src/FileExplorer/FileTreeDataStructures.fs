namespace Swate.Components.FileExplorer.FileTreeDataStructures

open System

// ============================================================================
// DATA STRUCTURES
// ============================================================================

type FileItem = {
    Id: string
    Name: string
    IconPath: string
    IsExpanded: bool
    Children: FileItem list option
    IdRel: string option
    IsDirectory: bool
    IsLFS: bool option
    IsLFSPointer: bool option
    Checkout: string option
    Downloaded: bool option
    Size: int64 option
    SizeFormatted: string option
    ItemType: string
    Label: string option
    Selectable: bool
}

// Helper type for file tree creation using a predefined config
type FileItemConfig = {
    Name: string
    IconPath: string
    IsDirectory: bool
    ItemType: string
}


// ============================================================================
// FILE TREE OPERATIONS
// ============================================================================
module FileTree =
    let private generateId () = Guid.NewGuid().ToString()

    let formatSize (size: int64) : string =
        let log = Math.Floor(Math.Log(float size) / Math.Log 1024.0)
        let log = Math.Max(0.0, Math.Min(4.0, log)) |> int
        let suffixes = [| "B"; "KB"; "MB"; "GB"; "TB" |]
        let divisor = Math.Pow(1024.0, float log)
        sprintf "%.0f %s" (float size / divisor) suffixes.[log]

    let createFile (name: string) (iconPath: string) : FileItem = {
        Id = generateId ()
        Name = name
        IconPath = iconPath
        IsExpanded = false
        Children = None
        IdRel = None
        IsDirectory = false
        IsLFS = None
        IsLFSPointer = None
        Checkout = None
        Downloaded = None
        Size = None
        SizeFormatted = None
        ItemType = "node"
        Label = Some name
        Selectable = false
    }

    let createFolder (name: string) (iconPath: string) : FileItem = {
        Id = generateId ()
        Name = name
        IconPath = iconPath
        IsExpanded = false
        Children = Some []
        IdRel = None
        IsDirectory = true
        IsLFS = None
        IsLFSPointer = None
        Checkout = None
        Downloaded = None
        Size = None
        SizeFormatted = None
        ItemType = "node"
        Label = Some name
        Selectable = false
    }

    let createFromConfig (config: FileItemConfig) (id: string) : FileItem = {
        Id = id
        Name = config.Name
        IconPath = config.IconPath
        IsExpanded = false
        Children = if config.IsDirectory then Some [] else None
        IdRel = None
        IsDirectory = config.IsDirectory
        IsLFS = None
        IsLFSPointer = None
        Checkout = None
        Downloaded = None
        Size = None
        SizeFormatted = None
        ItemType = config.ItemType
        Label = Some config.Name
        Selectable = false
    }

    let rec addChild parentId child items =
        items
        |> List.map (fun item ->
            if item.Id = parentId then
                match item.Children with
                | Some children -> {
                    item with
                        Children = Some(children @ [ child ])
                  }
                | None -> item
            else
                match item.Children with
                | Some children -> {
                    item with
                        Children = Some(addChild parentId child children)
                  }
                | None -> item
        )

    let rec removeItem itemId items =
        items
        |> List.filter (fun item -> item.Id <> itemId)
        |> List.map (fun item ->
            match item.Children with
            | Some children -> {
                item with
                    Children = Some(removeItem itemId children)
              }
            | None -> item
        )

    let rec toggleExpanded itemId items =
        items
        |> List.map (fun item ->
            if item.Id = itemId then
                {
                    item with
                        IsExpanded = not item.IsExpanded
                }
            else
                match item.Children with
                | Some children -> {
                    item with
                        Children = Some(toggleExpanded itemId children)
                  }
                | None -> item
        )

    let rec getPath itemId items currentPath =
        items
        |> List.tryPick (fun i ->
            if i.Id = itemId then
                Some(currentPath @ [ i ])
            else
                match i.Children with
                | Some children -> getPath itemId children (currentPath @ [ i ])
                | None -> None
        )

    let rec findItem itemId items =
        items
        |> List.tryPick (fun item ->
            if item.Id = itemId then
                Some item
            else
                match item.Children with
                | Some children -> findItem itemId children
                | None -> None
        )

    let rec updateItem itemId updateFn items =
        items
        |> List.map (fun item ->
            if item.Id = itemId then
                updateFn item
            else
                match item.Children with
                | Some children -> {
                    item with
                        Children = Some(updateItem itemId updateFn children)
                  }
                | None -> item
        )

    let rec renameItem itemId newName items =
        updateItem
            itemId
            (fun item -> {
                item with
                    Name = newName
                    Label = Some newName
            })
            items

    let rec updateLFSInfo itemId isDownloaded items =
        updateItem
            itemId
            (fun item -> {
                item with
                    Downloaded = Some isDownloaded
                    IsLFSPointer = if isDownloaded then Some false else item.IsLFSPointer
            })
            items

    let sortItems (items: FileItem list) (enforcedOrder: string list) =
        items
        |> List.sortWith (fun a b ->
            let aIdx = enforcedOrder |> List.tryFindIndex ((=) a.Name)
            let bIdx = enforcedOrder |> List.tryFindIndex ((=) b.Name)

            match aIdx, bIdx with
            | Some ai, Some bi -> compare ai bi
            | Some _, None -> -1
            | None, Some _ -> 1
            | None, None ->
                match a.IsDirectory, b.IsDirectory with
                | true, false -> -1
                | false, true -> 1
                | _ -> compare a.Name b.Name
        )

    let createEmptyPlaceholder (parentId: string) : FileItem = {
        Id = sprintf "%s/empty$%s" parentId (generateId ())
        Name = "empty"
        IconPath = "block"
        IsExpanded = false
        Children = None
        IdRel = None
        IsDirectory = false
        IsLFS = None
        IsLFSPointer = None
        Checkout = None
        Downloaded = None
        Size = None
        SizeFormatted = None
        ItemType = "empty"
        Label = Some "empty"
        Selectable = false
    }




// ============================================================================
// FILE EXPLORER COMPONENT CONTEXT MENU
// This menu contains the model and the actions (update actions) that the useReducer
// hook will use to update the state
// ============================================================================

type ContextMenuItem = {
    Label: string
    Icon: string
    OnClick: unit -> unit
    Disabled: bool option
}

module FileExplorerLogic =
    type Model = {
        Items: FileItem list
        SelectedId: string option
        BreadcrumbPath: FileItem list
        ExpandedIds: Set<string>
        ContextMenuVisible: bool
        ContextMenuX: float
        ContextMenuY: float
        ContextMenuItems: ContextMenuItem list
    }

    type Msg =
        | ToggleExpanded of string
        | SelectItem of string
        | NavigateTo of string
        | ShowContextMenu of float * float * ContextMenuItem list
        | HideContextMenu
        | UpdateItems of FileItem list
        | AddChild of parentId: string * child: FileItem
        | RemoveItem of string
        | RenameItem of string * string
        | ToggleLFSDownload of string

    let init items = {
        Items = items
        SelectedId = None
        BreadcrumbPath = []
        ExpandedIds = Set.empty
        ContextMenuVisible = false
        ContextMenuX = 0.0
        ContextMenuY = 0.0
        ContextMenuItems = []
    }

    let rec update msg model =
        match msg with
        | ToggleExpanded itemId ->
            let newItems = FileTree.toggleExpanded itemId model.Items

            let newExpanded =
                if model.ExpandedIds.Contains itemId then
                    model.ExpandedIds.Remove itemId
                else
                    model.ExpandedIds.Add itemId

            {
                model with
                    Items = newItems
                    ExpandedIds = newExpanded
            }

        | SelectItem itemId ->
            let path =
                match FileTree.getPath itemId model.Items [] with
                | Some p -> p
                | None -> []

            {
                model with
                    SelectedId = Some itemId
                    BreadcrumbPath = path
            }

        | NavigateTo itemId ->
            if itemId = "" then
                {
                    model with
                        SelectedId = None
                        BreadcrumbPath = []
                }
            else
                update (SelectItem itemId) model

        | ShowContextMenu(x, y, menuItems) -> {
            model with
                ContextMenuVisible = true
                ContextMenuX = x
                ContextMenuY = y
                ContextMenuItems = menuItems
          }

        | HideContextMenu -> {
            model with
                ContextMenuVisible = false
          }

        | UpdateItems items -> { model with Items = items }

        | AddChild(parentId, child) ->
            let newItems = FileTree.addChild parentId child model.Items
            { model with Items = newItems }

        | RemoveItem itemId ->
            let newItems = FileTree.removeItem itemId model.Items
            { model with Items = newItems }

        | RenameItem(itemId, newName) ->
            let newItems = FileTree.renameItem itemId newName model.Items
            { model with Items = newItems }

        | ToggleLFSDownload itemId ->
            let newItems =
                FileTree.updateItem
                    itemId
                    (fun item ->
                        match item.Downloaded with
                        | Some false -> {
                            item with
                                Downloaded = Some true
                                IsLFSPointer = Some false
                          }
                        | _ -> item
                    )
                    model.Items

            { model with Items = newItems }