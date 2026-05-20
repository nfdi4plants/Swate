module Swate.Electron.Shared.FileIOTypes

open System.Collections.Generic

type GitLfsLsFileInfo = {
    name: string
    size: float
    checkout: bool
    downloaded: bool
    ``oid_type``: string
    oid: string
    version: string
}

type FileEntry = {
    name: string
    isDirectory: bool
    path: string
    lfs: GitLfsLsFileInfo option
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
                ?lfs: GitLfsLsFileInfo option
            )
            : FileEntry =
            {
            name = name
            path = path
            isDirectory = isDirectory
            lfs = defaultArg lfs None
        }

type FileTreeNode = {
    name: string
    isDirectory: bool
    path: string
    lfs: GitLfsLsFileInfo option
    children: Dictionary<string, FileTreeNode>
} with

    static member create
        (
            name: string,
            isDirectory: bool,
            path: string,
            children: Dictionary<string, FileTreeNode>,
            ?lfs: GitLfsLsFileInfo option
        )
        =
        {
            name = name
            isDirectory = isDirectory
            path = path
            lfs = defaultArg lfs None
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

type RenamePathRequest = {
    relativePath: string
    newName: string
}
