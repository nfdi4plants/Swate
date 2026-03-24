module Swate.Electron.Shared.FileIOTypes

open ARCtrl
open System.Collections.Generic

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

        static member create(name: string, path: string, isDirectory: bool, ?isLfs: bool option) = {
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