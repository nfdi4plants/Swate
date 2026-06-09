/// This module SHOULD only contain the exact IPC communication types.
module Swate.Electron.Shared.IPCTypes

open Fable.Core
open Swate.Components.Api.GitLabApi
open Swate.Components.Composite.Authentication.Types
open Swate.Components.Page.DataHub.DataHubTypes
open Swate.Components.Shared
open Swate.Electron.Shared.DTOs.NoteSearchDto
open AuthTypes
open FileIOTypes
open GitTypes

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
    openARC: unit -> JS.Promise<Result<string option, exn>>
    /// Open ARC at a known path (e.g. recent-ARC click). Main decides disposition.
    openARCByPath: string -> JS.Promise<Result<string, exn>>
    /// Create ARC via folder dialog. Main decides disposition.
    createARC: string -> JS.Promise<Result<string, exn>>
    /// Ensure ARC notes scaffolding exists for the ARCVault root path.
    ensureNotesFolder: unit -> JS.Promise<Result<unit, exn>>
    closeARC: unit -> JS.Promise<Result<unit, exn>>
    getOpenPath: unit -> JS.Promise<string option>
    getRecentARCs: unit -> JS.Promise<ARCPointer[]>
    removeRecentARC: ARCPointer -> JS.Promise<Result<unit, exn>>

    pickArcPaths: unit -> JS.Promise<Result<string[], exn>>
    pickDirectory: unit -> JS.Promise<Result<string, exn>>
    pickAbsolutePaths: unit -> JS.Promise<Result<string[], exn>>
    pickExternalTextFiles: unit -> JS.Promise<Result<ImportedTextFile[], exn>>
    getFileTree: unit -> JS.Promise<Result<System.Collections.Generic.Dictionary<string, FileEntry>, exn>>
    openFile: string -> JS.Promise<Result<FileContentDTO, exn>>
    openArcFolderInFileExplorer: unit -> JS.Promise<Result<unit, exn>>
    showPathInFileExplorer: string -> JS.Promise<Result<unit, exn>>
    openPathWithDefaultApplication: string -> JS.Promise<Result<unit, exn>>
    readNotes: unit -> JS.Promise<Result<NoteSearchDto[], exn>>
    /// Persists the active in-memory ARC scaffold to disk.
    saveArcFile: unit -> JS.Promise<Result<unit, exn>>
    /// Applies ARC file changes to the active vault's in-memory ARC without writing to disk.
    setArcFileInMemory: FileContentDTO -> JS.Promise<Result<unit, exn>>
    /// Adds a new ARC entity from the file tree. The file watcher performs the follow-up merge and file-tree update.
    addArcFile: FileContentDTO -> JS.Promise<Result<unit, exn>>
    /// Creates a generic file or folder inside a safe ARC directory.
    createFileSystemItem: CreateFileSystemItemRequest -> JS.Promise<Result<string, exn>>
    /// Checks if there are unsaved changes in the in-memory ARC scaffold compared to the last saved state on disk. Does not trigger a save or write to disk.
    getHasUnsavedArcChanges: unit -> JS.Promise<Result<bool, exn>>
    deletePath: string -> JS.Promise<Result<unit, exn>>
    renamePath: RenamePathRequest -> JS.Promise<Result<unit, exn>>
    writeFile: FileContentDTO -> JS.Promise<Result<unit, exn>>
    runGitLfs: GitLfsRequest -> JS.Promise<Result<GitLfsResult, exn>>
    cancelGitLfs: string -> JS.Promise<Result<string, exn>>
    resolveCloseRequest: SaveBeforeQuitDecision -> JS.Promise<Result<unit, exn>>
}

/// Two Way Bridge: Renderer <-> Main
type IGitApi = {
    checkGitVersions: unit -> JS.Promise<Result<unit, exn>>
    getGitStatus: unit -> JS.Promise<Result<GitStatusDto, exn>>
    getGitBranches: unit -> JS.Promise<Result<GitBranchRefDto[], exn>>
    getOriginRepositoryWebUrl: unit -> JS.Promise<Result<string option, exn>>
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
    gitDiscardPaths: GitPathspecRequest -> JS.Promise<Result<GitOperationResult, exn>>
    gitCommit: GitCommitRequest -> JS.Promise<Result<GitOperationResult, exn>>
    setGitLfsSettings: GitLfsSettingsDto -> JS.Promise<Result<GitOperationResult, exn>>
    gitLfsPrune: unit -> JS.Promise<Result<GitOperationResult, exn>>
    gitLfsDedup: unit -> JS.Promise<Result<GitOperationResult, exn>>
    gitLfsFreeLocalCopy: GitLfsFreeLocalCopyRequest -> JS.Promise<Result<GitOperationResult, exn>>
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

/// One Way Bridge: Main -> Renderer
module MainToRendererIpc =

    type IPathChangeRendererApi = { pathChange: string option -> unit }

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

    type IGitLfsProgressRendererApi = {
        gitLfsProgressUpdate: GitLfsProgressDto -> unit
    }

    type IHasUnsavedArcChangesRendererApi = {
        arcUnsavedChangesUpdate: bool -> unit
    }

// TODO: What should filewatcher do when detecting changes?
/// One Way Bridge: Main -> Renderer
type IArcFileWatcherApi = {
    /// This function is called when ARC is reloaded due to local file changes.
    IsLoadingChanges: bool -> unit
}

type IMainSaveBeforeQuitApi = { requestSaveBeforeQuit: unit -> unit }
