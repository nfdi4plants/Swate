module Swate.Electron.Shared.FileIOTypes

open System.Collections.Generic

type FileEntry = {
    name: string
    isDirectory: bool
    path: string
    isLfs: bool option
    isLfsPointer: bool option
    downloaded: bool option
    lfsSizeBytes: float option
}

[<AutoOpen>]
module FileEntryExtensions =

    let createFileEntryTree (fileEntries: FileEntry[]) =
        let dic = Dictionary<string, FileEntry>()
        fileEntries |> Array.iter (fun fileEntry -> dic.Add(fileEntry.path, fileEntry))
        dic

    type FileEntry with

        static member create
            (
                name: string,
                path: string,
                isDirectory: bool,
                ?isLfs: bool option,
                ?isLfsPointer: bool option,
                ?downloaded: bool option,
                ?lfsSizeBytes: float option
            )
            : FileEntry =
            {
            name = name
            path = path
            isDirectory = isDirectory
            isLfs = defaultArg isLfs None
            isLfsPointer = defaultArg isLfsPointer None
            downloaded = defaultArg downloaded None
            lfsSizeBytes = defaultArg lfsSizeBytes None
        }

type FileTreeNode = {
    name: string
    isDirectory: bool
    path: string
    isLfs: bool option
    isLfsPointer: bool option
    downloaded: bool option
    lfsSizeBytes: float option
    children: Dictionary<string, FileTreeNode>
} with

    static member create
        (
            name: string,
            isDirectory: bool,
            path: string,
            children: Dictionary<string, FileTreeNode>,
            ?isLfs: bool option,
            ?isLfsPointer: bool option,
            ?downloaded: bool option,
            ?lfsSizeBytes: float option
        )
        =
        {
            name = name
            isDirectory = isDirectory
            path = path
            isLfs = defaultArg isLfs None
            isLfsPointer = defaultArg isLfsPointer None
            downloaded = defaultArg downloaded None
            lfsSizeBytes = defaultArg lfsSizeBytes None
            children = children
        }

open ARCtrl.Contract

type FileContentDTO = {|
    fileType: DTOType
    content: string
    path: string
|}

type NoteTarget =
    | Root
    | Study of string
    | Assay of string
    | Workflow of string
    | Run of string
