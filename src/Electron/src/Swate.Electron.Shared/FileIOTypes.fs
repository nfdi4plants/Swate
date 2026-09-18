module Swate.Electron.Shared.FileIOTypes

open System.Collections.Generic
open Fable.Core

type GitLfsLsFileInfo = {
    name: string
    size: float
    checkout: bool
    downloaded: bool
    ``oid_type``: string
    oid: string
    version: string
}

/// A large object of the workspace (for Git a file tracked with LFS): whether its
/// content is present in the file and in the local store, its size and object id.
type LargeObjectState = {
    path: string
    sizeBytes: float option
    isMaterialized: bool
    isLocallyAvailable: bool
    objectId: string option
}

type FileEntry = {
    name: string
    isDirectory: bool
    path: string
    largeObject: LargeObjectState option
}

[<AutoOpen>]
module FileEntryExtensions =

    let createFileEntryTree (fileEntries: FileEntry[]) =
        let dic = Dictionary<string, FileEntry>()
        fileEntries |> Array.iter (fun fileEntry -> dic.Add(fileEntry.path, fileEntry))
        dic

    type FileEntry with

        static member create
            (name: string, path: string, isDirectory: bool, ?largeObject: LargeObjectState option)
            : FileEntry =
            {
                name = name
                path = path
                isDirectory = isDirectory
                largeObject = defaultArg largeObject None
            }

type FileTreeNode = {
    name: string
    isDirectory: bool
    path: string
    largeObject: LargeObjectState option
    children: Dictionary<string, FileTreeNode>
} with

    static member create
        (
            name: string,
            isDirectory: bool,
            path: string,
            children: Dictionary<string, FileTreeNode>,
            ?largeObject: LargeObjectState option
        ) =
        {
            name = name
            isDirectory = isDirectory
            path = path
            largeObject = defaultArg largeObject None
            children = children
        }

[<RequireQualifiedAccess; StringEnum>]
type FileContentType =
    | JSON
    | YAML
    | CWL
    | PlainText
    | Markdown
    | ISA_Investigation
    | ISA_Study
    | ISA_Assay
    | ISA_Run
    | ISA_Workflow
    | ISA_Datamap
    | CLI

type FileContentDTO = {|
    fileType: FileContentType
    content: string
    path: string
|}

type RenamePathRequest = {
    relativePath: string
    newName: string
}

type MovePathRequest = {
    sourceRelativePath: string
    targetRelativePath: string
    overwrite: bool
}

[<RequireQualifiedAccess>]
type FileSystemItemKind =
    | File
    | Folder

type CreateFileSystemItemRequest = {
    parentPath: string
    name: string
    kind: FileSystemItemKind
}

type ImportExternalFilesRequest = {
    requestId: string
    targetRelativePath: string
    authorizationId: string
}

[<RequireQualifiedAccess>]
type ImportExternalFilesResult =
    | Completed
    | Cancelled

[<RequireQualifiedAccess>]
type FileImportPhase =
    | Copying
    | Finalizing

type ActiveFileImportState = {
    requestId: string
    phase: FileImportPhase
}
