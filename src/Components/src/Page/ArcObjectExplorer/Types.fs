module Swate.Components.Page.ARCObjectExplorer.Types

open Swate.Components.Page.FileExplorer
open Swate.Components.Page.FileExplorer.Types

type ARCObjectExplorerVisibleItem = {
    Item: FileItem
    Depth: int
    Lineage: string list
}

type ARCObjectExplorerContextItem = { Item: FileItem; IsCurrent: bool }

type ARCObjectExplorerSection = {
    Label: string
    Description: string
    Items: ARCObjectExplorerVisibleItem list
}

type ARCObjectExplorerItems = {
    SourceName: string
    SourceId: string
    ContextItems: ARCObjectExplorerContextItem list
    Sections: ARCObjectExplorerSection list
}
