module Renderer.Components.LeftSidebar.FileExplorer.Types

open Fable.Core
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


[<StringEnum>]
type ArcExplorerNodeKind =
    | Arc
    | Group
    | Study
    | Assay
    | Workflow
    | Run
    | Table
    | DataMap
    | Note
    | Sample

[<RequireQualifiedAccess>]
module ArcExplorerNodeKind =

    let label =
        function
        | ArcExplorerNodeKind.Arc -> "ARC"
        | ArcExplorerNodeKind.Group -> "Group"
        | ArcExplorerNodeKind.Study -> "Study"
        | ArcExplorerNodeKind.Assay -> "Assay"
        | ArcExplorerNodeKind.Workflow -> "Workflow"
        | ArcExplorerNodeKind.Run -> "Run"
        | ArcExplorerNodeKind.Table -> "Table"
        | ArcExplorerNodeKind.DataMap -> "DataMap"
        | ArcExplorerNodeKind.Note -> "Note"
        | ArcExplorerNodeKind.Sample -> "Sample"
