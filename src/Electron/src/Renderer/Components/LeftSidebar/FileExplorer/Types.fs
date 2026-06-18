module Renderer.Components.LeftSidebar.FileExplorer.Types

open Swate.Components.Shared
open Swate.Components.Page.FileExplorer.Types
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

type AssignableNoteRef = {
    SourceFolderPath: string
    NoteFolderName: string
    Label: string
}
