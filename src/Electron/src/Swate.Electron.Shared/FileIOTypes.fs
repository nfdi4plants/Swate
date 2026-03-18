module Swate.Electron.Shared.FileIOTypes

open ARCtrl
open System.Collections.Generic

type SaveArcFileRequest = {
    FileType: ArcFilesDiscriminate
    Json: string
}

type WriteFileRequest = {
    RelativePath: string
    Content: string
}


type FileEntry = {
    name: string
    path: string
    isDirectory: bool
    isLfs: bool option
}

[<RequireQualifiedAccess>]
type ArcExplorerNodeKind =
    | Arc
    | Group
    | Study
    | Assay
    | Workflow
    | Run
    | DataMap
    | Note
    | Sample

type ArcExplorerNode = {
    id: string
    name: string
    kind: ArcExplorerNodeKind
    path: string option
    isSelectable: bool
    isReference: bool
    isLfs: bool option
    children: ArcExplorerNode list
} with

    static member create
        (
            id: string,
            name: string,
            kind: ArcExplorerNodeKind,
            ?path: string option,
            ?isSelectable: bool,
            ?isReference: bool,
            ?isLfs: bool option,
            ?children: ArcExplorerNode list
        ) =
        {
            id = id
            name = name
            kind = kind
            path = defaultArg path None
            isSelectable = defaultArg isSelectable true
            isReference = defaultArg isReference false
            isLfs = defaultArg isLfs None
            children = defaultArg children []
        }

[<AutoOpen>]
module FileEntryExtensions =

    let createFileEntryTree (fileEntries: FileEntry[]) =
        let dic = Dictionary<string, FileEntry>()
        fileEntries |> Array.iter (fun fileEntry -> dic.Add(fileEntry.path, fileEntry))
        dic

    type FileEntry with

        static member create(name: string, path: string, isDirectory: bool, ?isLfs: bool option) = {
            name = name
            path = path
            isDirectory = isDirectory
            isLfs = defaultArg isLfs None
        }

type FileItemDTO = {
    name: string
    isDirectory: bool
    path: string
    isLfs: bool option
    children: Dictionary<string, FileItemDTO>
} with

    static member create
        (name: string, isDirectory: bool, path: string, children: Dictionary<string, FileItemDTO>, ?isLfs: bool option)
        =
        {
            name = name
            isDirectory = isDirectory
            path = path
            isLfs = defaultArg isLfs None
            children = children
        }
