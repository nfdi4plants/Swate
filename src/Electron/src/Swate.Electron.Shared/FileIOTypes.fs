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