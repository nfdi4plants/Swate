namespace Swate.Components.FileExplorer.FileTreeDataStructures

open System

// ============================================================================
// DATA STRUCTURES
// ============================================================================
type FileTreeNode<'T> =
    | Leaf of 'T
    | Branch of 'T * FileTreeNode<'T> list

type FileItem = {
    Id: string
    Name: string
    IconPath: string
    IsExpanded: bool
    Children: FileItem list option
}




// ============================================================================
// FILE TREE OPERATIONS
// ============================================================================
module FileTree =
    let private generateId () = Guid.NewGuid().ToString()

    let createFile (name: string) (iconPath: string) : FileItem = {
        Id = generateId ()
        Name = name
        IconPath = iconPath
        IsExpanded = false
        Children = None
    }

    let createFolder (name: string) (iconPath: string) : FileItem = {
        Id = generateId ()
        Name = name
        IconPath = iconPath
        IsExpanded = false
        Children = Some []
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