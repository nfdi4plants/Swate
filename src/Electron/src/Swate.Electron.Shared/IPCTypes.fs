module Swate.Electron.Shared.IPCTypes

open System.Collections.Generic
open Fable.Core // Promise type
open Fable.Electron

open Swate.Components
open ARCtrl

[<RequireQualifiedAccess>]
type ArcFileType =
    | Investigation
    | Study
    | Assay
    | Run
    | Workflow
    | DataMap

type PreviewData =
    | ArcFileData of fileType: ArcFileType * json: string
    | Text of string
    | Unknown

type DataHolder =
    {
        path: string
        event: IpcMainEvent
    }

let createDataHolder path event : DataHolder =
    {
        path = path
        event = event
    }

/// Two Way Bridge: Renderer <-> Main
type IArcVaultsApi = {
    /// Will open ARC in same window
    openARC: IpcMainEvent -> JS.Promise<Result<string, exn>>
    createARC: IpcMainEvent -> string -> JS.Promise<Result<string, exn>>
    focusExistingARCWindow: string -> JS.Promise<Result<unit, exn>>
    /// Will open ARC in a new window
    openARCInNewWindow: unit -> JS.Promise<Result<unit, exn>>
    createARCInNewWindow: string -> JS.Promise<Result<unit, exn>>
    closeARC: IpcMainEvent -> JS.Promise<Result<unit, exn>>
    getOpenPath: IpcMainEvent -> JS.Promise<string option>
    getRecentARCs: unit -> JS.Promise<Swate.Components.Types.SelectorTypes.ARCPointer []>
    checkForARC: string -> JS.Promise<bool>
    openFile: DataHolder -> JS.Promise<Result<PreviewData, exn>>

    updateAssay: string -> Result<unit, exn>
    updateStudy: string -> Result<unit, exn>
    updateWorkflows: string -> Result<unit, exn>




    updateARC: unit -> unit
}

type FileEntry =
    {
        name: string
        path: string
        isDirectory: bool
    }

[<AutoOpen>]
module FileEntryExtensions =

    let createFileEntryTree(fileEntries: FileEntry []) =
        let dic = Dictionary<string, FileEntry>()
        fileEntries
        |> Array.iter (fun fileEntry -> dic.Add(fileEntry.path, fileEntry))
        dic

    type FileEntry with

        static member create (name: string, path: string, isDirectory: bool) =
            {
                name = name
                path = path
                isDirectory = isDirectory
            }

type FileItemDTO =
    {
        name: string
        isDirectory: bool
        path: string
        children: Dictionary<string, FileItemDTO>
    }

[<AutoOpen>]
module FileItemDTOExtensions =

    type FileItemDTO with

        static member create (name: string, isDirectory: bool, path: string, children: Dictionary<string, FileItemDTO>) =
            {
                name = name
                isDirectory = isDirectory
                path = path
                children = children
            }

/// One Way Bridge: Main -> Renderer
type IMainUpdateRendererApi = {
    pathChange: string option -> unit
    recentARCsUpdate: ARCPointer [] -> unit
    fileTreeUpdate: System.Collections.Generic.Dictionary<string, FileEntry> -> unit
}

// Todo: What should filewatcher do when detecting changes?
/// One Way Bridge: Main -> Renderer
type IArcFileWatcherApi = {
    /// This function is called when ARC is reloaded due to local file changes.
    IsLoadingChanges: bool -> unit
}
