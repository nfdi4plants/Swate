/// This module SHOULD only contain the exact IPC communication types.
module Swate.Electron.Shared.IPCTypes

open Fable.Core
open Fable.Electron
open Swate.Components.Api.GitLabApi
open Swate.Components.Authentication.Types
open Swate.Components.DataHubTypes
open Swate.Components.Shared
open AuthTypes
open FileIOTypes
open GitTypes
open Swate.Components.NoteTypes

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
    openARC: unit -> JS.Promise<Result<string, exn>>
    /// Open ARC at a known path (e.g. recent-ARC click). Main decides disposition.
    openARCByPath: string -> JS.Promise<Result<string, exn>>
    /// Create ARC via folder dialog. Main decides disposition.
    createARC: string -> JS.Promise<Result<string, exn>>
    closeARC: unit -> JS.Promise<Result<unit, exn>>
    getOpenPath: unit -> JS.Promise<string option>
    getRecentARCs: unit -> JS.Promise<ARCPointer[]>
    removeRecentARC: ARCPointer -> JS.Promise<Result<unit, exn>>

    pickArcPaths: unit -> JS.Promise<Result<string[], exn>>
    pickDirectory: unit -> JS.Promise<Result<string, exn>>
    pickAbsolutePaths: unit -> JS.Promise<Result<string[], exn>>
    pickExternalTextFiles: unit -> JS.Promise<Result<ImportedTextFile[], exn>>
    getFileTree: unit -> JS.Promise<Result<System.Collections.Generic.Dictionary<string, FileEntry>, exn>>
    getArcObjectTree: unit -> JS.Promise<Result<ArcExplorerNode list, exn>>
    openFile: string -> JS.Promise<Result<FileContentDTO, exn>>
    readNotes: unit -> JS.Promise<Result<Note[], exn>>
    /// This IPC call is used to set changes to an ARC based on a smaller ArcFiles object. It can be used to trigger UpdateContract changes and write these changes to disc.
    saveArcFile: FileContentDTO -> JS.Promise<Result<unit, exn>>
    writeFile: FileContentDTO -> JS.Promise<Result<unit, exn>>
    runGitLfs: GitLfsRequest -> JS.Promise<Result<GitLfsResult, exn>>
    cancelGitLfs: string -> JS.Promise<Result<string, exn>>
    resolveCloseRequest: SaveBeforeQuitDecision -> JS.Promise<Result<unit, exn>>
}

type IGitLfsApi = {
    runChannel: GitLfsRequest -> JS.Promise<Result<GitLfsResult, exn>>
    cancelChannel: string -> JS.Promise<Result<string, exn>>
}

/// Two Way Bridge: Renderer <-> Main
type IGitApi = {
    checkGitVersions: unit -> JS.Promise<Result<unit, exn>>
    getGitStatus: unit -> JS.Promise<Result<GitStatusDto, exn>>
    getGitBranches: unit -> JS.Promise<Result<GitBranchRefDto[], exn>>
    getGitLfsSettings: unit -> JS.Promise<Result<GitLfsSettingsDto, exn>>
    previewGitPull: GitRemoteOperationRequest -> JS.Promise<Result<GitPullPreflightResult, exn>>
    getGitDiffSummary: unit -> JS.Promise<Result<GitDiffSummaryDto, exn>>
    getGitWordDiff: GitPathspecRequest -> JS.Promise<Result<string, exn>>
    getGitDiffViewData: string -> JS.Promise<Result<GitPageLoadResultDto<GitDiffViewDataDto>, exn>>
    getGitMergeConflictViewData: string -> JS.Promise<Result<GitPageLoadResultDto<GitMergeConflictViewDataDto>, exn>>
    installGitLfs: unit -> JS.Promise<Result<GitOperationResult, exn>>
    gitFetch: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitPull: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitPush: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitInitRepository: string -> JS.Promise<Result<GitOperationResult, exn>>
    gitAddRemote: GitRemoteConfigRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitCloneRepository: GitCloneRepositoryRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitStagePaths: GitPathspecRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitUnstagePaths: GitPathspecRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitCommit: GitCommitRequest -> JS.Promise<Result<GitOperationResult, exn>>
    setGitLfsSettings: GitLfsSettingsDto -> JS.Promise<Result<GitOperationResult, exn>>
    createBranch: GitCreateBranchRequest -> JS.Promise<Result<GitOperationResult, exn>>
    checkoutBranch: GitCheckoutBranchRequest -> JS.Promise<Result<GitOperationResult, exn>>
    confirmGitMergeResolution:
        GitConfirmMergeResolutionRequest -> JS.Promise<Result<GitConfirmMergeResolutionResult, exn>>
}

/// Two Way Bridge: Renderer <-> Main
type IGitLabApi = {
    loadAllRepos: ExploreRepoQuery -> JS.Promise<Result<PagedResponse<ExploreProjectDto>, GitLabError>>
    loadMostStarredRepos: ExploreMostStarredQuery -> JS.Promise<Result<PagedResponse<ExploreProjectDto>, GitLabError>>
    loadUserRepos: ExploreRepoQuery -> JS.Promise<Result<PagedResponse<ExploreProjectDto>, GitLabError>>
    loadOrganisationGroups: ExploreGroupsQuery -> JS.Promise<Result<PagedResponse<GroupDto>, GitLabError>>
    loadOrganisationRepos:
        ExploreGroupProjectsQuery -> JS.Promise<Result<PagedResponse<ExploreProjectDto>, GitLabError>>
    createProject: string -> JS.Promise<Result<ExploreProjectDto, GitLabError>>
}

/// One Way Bridge: Main -> Renderer
module MainToRendererIpc =

    type IPathChangeRendererApi = {
        pathChange: string option -> unit
    }

    type IRecentArcsRendererApi = {
        recentARCsUpdate: ARCPointer[] -> unit
    }

    type IAuthAccountsRendererApi = {
        authAccountsUpdate: AuthStateDto -> unit
    }

    type IFileTreeRendererApi = {
        fileTreeUpdate: System.Collections.Generic.Dictionary<string, FileEntry> -> unit
    }

    type IGitProgressRendererApi = {
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
    listAccounts: unit -> Fable.Core.JS.Promise<Result<AccountSummary array, exn>>
    setActiveAccount: string -> Fable.Core.JS.Promise<Result<AuthStateDto, exn>>
    removeAccount: string -> Fable.Core.JS.Promise<Result<unit, exn>>
}
