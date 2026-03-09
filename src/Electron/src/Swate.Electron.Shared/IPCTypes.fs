/// This module SHOULD only contain the exact IPC communication types.
module Swate.Electron.Shared.IPCTypes

open System.Collections.Generic
open Fable.Core
open Fable.Electron

open Swate.Components

open ARCtrl.ARCtrlHelper
open GitTypes
open FileIOTypes

module IPCTypesHelper =

    /// TODO: This is a pure UI type and should only be used in the renderer process. It is not meant to be sent over IPC, but rather to represent the state of the page after receiving data from the main process.
    [<RequireQualifiedAccess>]
    type PageState =
        | ArcFileData of fileType: ArcFilesDiscriminate * json: string
        | Text of string
        | Unknown
        | LandingDraft
        | Error of string

    [<RequireQualifiedAccess>]
    type SaveBeforeQuitDecision =
        | SaveAndClose
        | CloseWithoutSaving
        | CancelClose

open IPCTypesHelper

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
    getRecentARCs: unit -> JS.Promise<SelectorTypes.ARCPointer[]>
    checkForARC: string -> JS.Promise<bool>

    openFile: IpcMainEvent -> string -> JS.Promise<Result<PageState, exn>>
    saveArcFile: IpcMainEvent -> SaveArcFileRequest -> JS.Promise<Result<PageState, exn>>
    writeFile: IpcMainEvent -> WriteFileRequest -> JS.Promise<Result<unit, exn>>
    syncARC: IpcMainEvent -> SaveArcFileRequest -> JS.Promise<Result<unit, exn>>
    runGitLfs: IpcMainEvent -> GitLfsRequest -> JS.Promise<Result<GitLfsResult, exn>>
    cancelGitLfs: IpcMainEvent -> string -> JS.Promise<Result<string, exn>>
    resolveCloseRequest: IpcMainEvent -> SaveBeforeQuitDecision -> JS.Promise<Result<unit, exn>>
}

type IGitLfsApi = {
    runChannel: IpcMainEvent -> GitLfsRequest -> JS.Promise<Result<GitLfsResult, exn>>
    cancelChannel: IpcMainEvent -> string -> JS.Promise<Result<string, exn>>
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

/// One Way Bridge: Main -> Renderer
type IMainUpdateRendererApi = {
    pathChange: string option -> unit
    recentARCsUpdate: SelectorTypes.ARCPointer[] -> unit
    fileTreeUpdate: System.Collections.Generic.Dictionary<string, FileEntry> -> unit
    gitProgressUpdate: GitProgressDto -> unit
}

// TODO: What should filewatcher do when detecting changes?
/// One Way Bridge: Main -> Renderer
type IArcFileWatcherApi = {
    /// This function is called when ARC is reloaded due to local file changes.
    IsLoadingChanges: bool -> unit
}

type IMainSaveBeforeQuitApi = { requestSaveBeforeQuit: unit -> unit }