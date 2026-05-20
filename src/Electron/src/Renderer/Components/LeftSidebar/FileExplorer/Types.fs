module Renderer.Components.LeftSidebar.FileExplorer.Types

open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes

type ArcCreateDraft = { ArcFile: ArcFiles; Path: string }

type FileSystemCreateDraft = {
    Parent: FileItem
    Kind: FileSystemItemKind
}

type ArcRenameDraft = {
    Item: FileItem
    SourcePath: string
    InitialName: string
}

module FileExplorerItemPath =

    let tryGetRelativePath (item: FileItem) =
        item.Path
        |> Option.map PathHelpers.normalizeCanonicalRelativePath
