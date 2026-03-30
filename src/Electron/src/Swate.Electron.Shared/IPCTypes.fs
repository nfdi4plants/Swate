/// This module SHOULD only contain the exact IPC communication types.
module Swate.Electron.Shared.IPCTypes

open Fable.Core
open Fable.Electron

open Swate.Components
open Swate.Components.NoteTypes

open ARCtrl.ARCtrlHelper
open AuthTypes
open GitTypes
open FileIOTypes

module IPCTypesHelper =

    [<RequireQualifiedAccess>]
    type SaveBeforeQuitDecision =
        | SaveAndClose
        | CloseWithoutSaving
        | CancelClose

open IPCTypesHelper

/// Two Way Bridge: Renderer <-> Main
type IArcVaultsApi = {
    /// Open ARC via folder dialog. Main decides: current window / new window / focus existing.
    openARC: IpcMainEvent -> JS.Promise<Result<string, exn>>
    /// Open ARC at a known path (e.g. recent-ARC click). Main decides disposition.
    openARCByPath: IpcMainEvent -> string -> JS.Promise<Result<string, exn>>
    /// Create ARC via folder dialog. Main decides disposition.
    createARC: IpcMainEvent -> string -> JS.Promise<Result<string, exn>>
    closeARC: IpcMainEvent -> JS.Promise<Result<unit, exn>>
    getOpenPath: IpcMainEvent -> JS.Promise<string option>
    getRecentARCs: unit -> JS.Promise<SelectorTypes.ARCPointer[]>
    removeRecentARC: SelectorTypes.ARCPointer -> JS.Promise<Result<unit, exn>>

    pickArcPaths: IpcMainEvent -> JS.Promise<Result<string[], exn>>
    pickAbsolutePaths: IpcMainEvent -> JS.Promise<Result<string[], exn>>
    pickExternalTextFiles: IpcMainEvent -> JS.Promise<Result<ImportedTextFile[], exn>>
    getArcObjectTree: IpcMainEvent -> JS.Promise<Result<ArcExplorerNode list, exn>>
    openFile: IpcMainEvent -> string -> JS.Promise<Result<FileContentDTO, exn>>
    readNotes: IpcMainEvent -> JS.Promise<Result<NoteSearch[], exn>>
    /// This IPC call is used to set changes to an ARC based on a smaller ArcFiles object. It can be used to trigger UpdateContract changes and write these changes to disc.
    saveArcFile: IpcMainEvent -> FileContentDTO -> JS.Promise<Result<unit, exn>>
    writeFile: IpcMainEvent -> FileContentDTO -> JS.Promise<Result<unit, exn>>
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
    getGitWordDiff: IpcMainEvent -> GitPathspecRequest -> JS.Promise<Result<string, exn>>
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
    authAccountsUpdate: AuthAccountSummary[] -> unit
    fileTreeUpdate: System.Collections.Generic.Dictionary<string, FileEntry> -> unit
    gitProgressUpdate: GitProgressDto -> unit
} with

    static member empty = {
        pathChange = ignore
        recentARCsUpdate = ignore
        authAccountsUpdate = ignore
        fileTreeUpdate = ignore
        gitProgressUpdate = ignore
    }

// TODO: What should filewatcher do when detecting changes?
/// One Way Bridge: Main -> Renderer
type IArcFileWatcherApi = {
    /// This function is called when ARC is reloaded due to local file changes.
    IsLoadingChanges: bool -> unit
}

type IMainSaveBeforeQuitApi = { requestSaveBeforeQuit: unit -> unit }

/// Two Way Bridge: Renderer <-> Main
type IAuthApi = {
    signIn: AuthSignInRequest -> Fable.Core.JS.Promise<Result<AuthResult, exn>>
    getAuthState: unit -> Fable.Core.JS.Promise<Result<AuthStateDto, exn>>
    signOut: unit -> Fable.Core.JS.Promise<Result<unit, exn>>
    revalidate: unit -> Fable.Core.JS.Promise<Result<AuthResult, exn>>
    listAccounts: unit -> Fable.Core.JS.Promise<Result<AuthAccountSummary array, exn>>
    setActiveAccount: string -> Fable.Core.JS.Promise<Result<AuthStateDto, exn>>
    removeAccount: string -> Fable.Core.JS.Promise<Result<unit, exn>>
}
