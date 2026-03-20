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

    /// TODO: This is a pure UI type and should only be used in the renderer process. It is not meant to be sent over IPC, but rather to represent the state of the page after receiving data from the main process.
    [<RequireQualifiedAccess>]
    type PageState =
        | ArcFileData of fileType: ArcFilesDiscriminate * json: string
        | Text of string
        | Unknown
        | LandingDraft
        | NotesDraft
        | NotesSearch
        | Error of string

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
    pickPaths: IpcMainEvent -> JS.Promise<Result<string [], exn>>

    openFile: IpcMainEvent -> string -> JS.Promise<Result<PageState, exn>>
    readNotes: IpcMainEvent -> JS.Promise<Result<NoteSearch[], exn>>
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
    authAccountsUpdate: AuthAccountSummary[] -> unit
    fileTreeUpdate: System.Collections.Generic.Dictionary<string, FileEntry> -> unit
    arcExplorerTreeUpdate: ArcExplorerNode list -> unit
    gitProgressUpdate: GitProgressDto -> unit
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
