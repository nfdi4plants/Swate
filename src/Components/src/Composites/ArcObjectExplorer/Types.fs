module Swate.Components.ARCObjectExplorer.Types

open Swate.Components.FileExplorer
open Swate.Components.FileExplorer.Types

type ARCObjectExplorerVisibleItem = {
    Item: FileItem
    Depth: int
    Lineage: string list
}

type ARCObjectExplorerContextItem = {
    Item: FileItem
    IsCurrent: bool
}

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
