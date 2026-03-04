module Swate.Electron.Shared.IPCTypes

open System.Collections.Generic
open Fable.Core
open Fable.Electron

open Swate.Components

open ARCtrl.ARCtrlHelper


type PreviewData =
    | ArcFileData of fileType: ArcFilesDiscriminate * json: string
    | Text of string
    | Unknown

type SaveArcFileRequest = {
        FileType: ArcFilesDiscriminate
        Json: string
    }

type WriteFileRequest = {
        RelativePath: string
        Content: string
    }

[<RequireQualifiedAccess>]
type GitFailureKind =
    | Unauthorized
    | Forbidden
    | Network
    | Timeout
    | Canceled
    | Unknown

type GitFileStatusDto = {
    Path: string
    Index: string
    WorkingDir: string
    OriginalPath: string option
}

type GitStatusDto = {
    Current: string option
    Tracking: string option
    Ahead: int
    Behind: int
    IsClean: bool
    Files: GitFileStatusDto[]
}

type GitDiffSummaryDto = {
    Changed: int
    Insertions: int
    Deletions: int
}

type GitOperationResult = {
    Success: bool
    Message: string option
    FailureKind: GitFailureKind option
    Path: string option
}

type GitProgressDto = {
    Method: string option
    Stage: string option
    Progress: float option
    Processed: float option
    Total: float option
}

type GitRemoteOperationRequest = {
    Remote: string option
    Branch: string option
}

type GitCloneRepositoryRequest = {
    RemoteUrl: string
    TargetPath: string
    Branch: string option
}

type GitPathspecRequest = {
    Pathspecs: string[]
}

type GitCommitRequest = {
    Message: string
}

type GitCreateBranchRequest = {
    Name: string
    StartPoint: string option
}

type GitCheckoutBranchRequest = {
    Name: string
}

[<RequireQualifiedAccess>]
type SaveBeforeQuitDecision =
    | SaveAndClose
    | CloseWithoutSaving
    | CancelClose

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
    getRecentARCs: unit -> JS.Promise<SelectorTypes.ARCPointer []>
    checkForARC: string -> JS.Promise<bool>

    openFile: IpcMainEvent -> string -> JS.Promise<Result<PreviewData, exn>>
    saveArcFile: IpcMainEvent -> SaveArcFileRequest -> JS.Promise<Result<PreviewData, exn>>
    writeFile: IpcMainEvent -> WriteFileRequest -> JS.Promise<Result<unit, exn>>
    syncARC: IpcMainEvent -> SaveArcFileRequest -> JS.Promise<Result<unit, exn>>
}

/// Two Way Bridge: Renderer <-> Main
type IGitApi = {
    getGitStatus: IpcMainEvent -> JS.Promise<Result<GitStatusDto, exn>>
    getGitDiffSummary: IpcMainEvent -> JS.Promise<Result<GitDiffSummaryDto, exn>>
    gitFetch: IpcMainEvent -> GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitPull: IpcMainEvent -> GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitPush: IpcMainEvent -> GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitInitRepository: IpcMainEvent -> string -> JS.Promise<Result<GitOperationResult, exn>>
    gitCloneRepository: IpcMainEvent -> GitCloneRepositoryRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitStagePaths: IpcMainEvent -> GitPathspecRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitUnstagePaths: IpcMainEvent -> GitPathspecRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitCommit: IpcMainEvent -> GitCommitRequest -> JS.Promise<Result<GitOperationResult, exn>>
    createBranch: IpcMainEvent -> GitCreateBranchRequest -> JS.Promise<Result<GitOperationResult, exn>>
    checkoutBranch: IpcMainEvent -> GitCheckoutBranchRequest -> JS.Promise<Result<GitOperationResult, exn>>
}

type FileEntry = {
    name: string
    path: string
    isDirectory: bool
}

[<AutoOpen>]
module FileEntryExtensions =

    let createFileEntryTree (fileEntries: FileEntry[]) =
        let dic = Dictionary<string, FileEntry>()
        fileEntries |> Array.iter (fun fileEntry -> dic.Add(fileEntry.path, fileEntry))
        dic

    type FileEntry with

        static member create(name: string, path: string, isDirectory: bool) = {
            name = name
            path = path
            isDirectory = isDirectory
        }

type ISaveBeforeQuitApi = {
    resolveCloseRequest: IpcMainEvent -> SaveBeforeQuitDecision -> JS.Promise<Result<unit, exn>>
}

type FileItemDTO = {
    name: string
    isDirectory: bool
    path: string
    children: Dictionary<string, FileItemDTO>
}

[<AutoOpen>]
module FileItemDTOExtensions =

    type FileItemDTO with

        static member create(name: string, isDirectory: bool, path: string, children: Dictionary<string, FileItemDTO>) = {
            name = name
            isDirectory = isDirectory
            path = path
            children = children
        }

/// One Way Bridge: Main -> Renderer
type IMainUpdateRendererApi = {
    pathChange: string option -> unit
    recentARCsUpdate: SelectorTypes.ARCPointer[] -> unit
    fileTreeUpdate: System.Collections.Generic.Dictionary<string, FileEntry> -> unit
    gitProgressUpdate: GitProgressDto -> unit
}

// Todo: What should filewatcher do when detecting changes?
/// One Way Bridge: Main -> Renderer
type IArcFileWatcherApi = {
    /// This function is called when ARC is reloaded due to local file changes.
    IsLoadingChanges: bool -> unit
}

type IMainSaveBeforeQuitApi = { requestSaveBeforeQuit: unit -> unit }
