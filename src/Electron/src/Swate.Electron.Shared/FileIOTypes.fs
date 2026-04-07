module Swate.Electron.Shared.FileIOTypes

open System.Collections.Generic
open Swate.Components.Shared

type FileEntry = {
    name: string
    isDirectory: bool
    path: string
    isLfs: bool option
}

[<AutoOpen>]
module FileEntryExtensions =

    let createFileEntryTree (fileEntries: FileEntry[]) =
        let dic = Dictionary<string, FileEntry>()
        fileEntries |> Array.iter (fun fileEntry -> dic.Add(fileEntry.path, fileEntry))
        dic

    type FileEntry with

        static member create(name: string, path: string, isDirectory: bool, ?isLfs: bool option) : FileEntry = {
            name = name
            path = path
            isDirectory = isDirectory
            isLfs = defaultArg isLfs None
        }

type FileTreeNode = {
    name: string
    isDirectory: bool
    path: string
    isLfs: bool option
    children: Dictionary<string, FileTreeNode>
} with

    static member create
        (name: string, isDirectory: bool, path: string, children: Dictionary<string, FileTreeNode>, ?isLfs: bool option)
        =
        {
            name = name
            isDirectory = isDirectory
            path = path
            isLfs = defaultArg isLfs None
            children = children
        }

open ARCtrl.Contract

type FileContentDTO = {|
    fileType: DTOType
    content: string
    path: string
|}


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

type ArcSelection = {
    TreePath: string option
    ExplorerNodeId: string option
} with
    static member Empty = {
        TreePath = None
        ExplorerNodeId = None
    }

type ArcObjectExplorerProps = {
    rootRepoPath: string option
    nodes: ArcExplorerNode list
    selection: ArcSelection
    arcFileState: ArcFiles option
    previewState: PageState option
    setArcFileState: ArcFiles option -> unit
    setSelection: ArcSelection -> unit
    services: ARCExplorerServices
}

type NoteTarget =
    | Root
    | Study of string
    | Assay of string
    | Workflow of string
    | Run of string

type NoteEntry = {
    Name: string
    RelativePath: string
    Path: string
    Target: NoteTarget
    IsLfs: bool option
}
