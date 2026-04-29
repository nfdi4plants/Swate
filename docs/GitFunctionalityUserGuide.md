# Swate Git Functionality Guide

This guide describes the current Git implementation used by Swate Electron. It is for developers who need to call, extend, test, or troubleshoot Git functionality in this repository.

## 1. Architecture

Git functionality is implemented in the Electron Main process and exposed to Renderer through the typed IPC bridge.

Primary files:

- Shared IPC and DTO contracts: `src/Electron/src/Swate.Electron.Shared/IPCTypes.fs`, `src/Electron/src/Swate.Electron.Shared/GitTypes.fs`
- Main IPC implementation: `src/Electron/src/Main/IPC/IGitApi.fs`
- Main Git implementation: `src/Electron/src/Main/Git`
- Renderer wrapper: `src/Electron/src/Renderer/GitApiClient.fs`
- Renderer workflow/state machine: `src/Electron/src/Renderer/Context/GitWorkflow.fs`
- Renderer wiring: `src/Electron/src/Renderer/Context/GitStateContext.fs`

Use `Renderer.GitApiClient` from Renderer code. It wraps the raw `IGitApi` bridge, supplies the Electron event placeholder required by the remoting library, and maps `Result<'T, exn>` to `Result<'T, string>`.

Do not call `Api.ipcGitApi` directly from feature code unless you are extending the wrapper itself.

## 2. Current Renderer API

`Renderer.GitApiClient` exposes:

```fsharp
getGitStatus: unit -> JS.Promise<Result<GitStatusDto, string>>
getGitBranches: unit -> JS.Promise<Result<GitBranchRefDto[], string>>
getGitLfsSettings: unit -> JS.Promise<Result<GitLfsSettingsDto, string>>
getGitDiffViewData: string -> JS.Promise<Result<GitPageLoadResultDto<GitDiffViewDataDto>, string>>
getGitMergeConflictViewData: string -> JS.Promise<Result<GitPageLoadResultDto<GitMergeConflictViewDataDto>, string>>
installGitLfs: unit -> JS.Promise<Result<GitOperationResult, string>>
previewGitPull: GitRemoteOperationRequest -> JS.Promise<Result<GitPullPreflightResult, string>>
gitFetch: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, string>>
gitPull: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, string>>
gitPush: GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, string>>
gitInitRepository: string -> JS.Promise<Result<string, string>>
gitAddRemote: GitRemoteConfigRequest -> JS.Promise<Result<GitOperationResult, string>>
gitCloneRepository: GitCloneRepositoryRequest -> JS.Promise<Result<GitOperationResult, string>>
createBranch: GitCreateBranchRequest -> JS.Promise<Result<GitOperationResult, string>>
checkoutBranch: GitCheckoutBranchRequest -> JS.Promise<Result<GitOperationResult, string>>
gitStagePaths: GitPathspecRequest -> JS.Promise<Result<GitOperationResult, string>>
gitUnstagePaths: GitPathspecRequest -> JS.Promise<Result<GitOperationResult, string>>
gitCommit: GitCommitRequest -> JS.Promise<Result<GitOperationResult, string>>
setGitLfsSettings: GitLfsSettingsDto -> JS.Promise<Result<GitOperationResult, string>>
confirmGitMergeResolution: GitConfirmMergeResolutionRequest -> JS.Promise<Result<GitConfirmMergeResolutionResult, string>>
```

Most app code should access these through `GitWorkflow.GitDependencies`, which is populated in `GitStateContext.fs`. That dependency layer maps diff and merge page-load DTOs to `PageState`.

## 3. Result Shapes

Most write and sync operations return `GitOperationResult`:

```fsharp
type GitOperationResult = {
    Success: bool
    Message: string option
    FailureKind: GitFailureKind option
    WarningMessage: string option
    WarningKind: GitFailureKind option
    Path: string option
}
```

Use it as follows:

- `Ok op` and `op.Success = true`: operation completed.
- `Ok op` and `op.Success = false`: Git operation failed; inspect `FailureKind` and `Message`.
- `Ok op` with `WarningMessage = Some ...`: main operation completed, but follow-up work had a recoverable warning.
- `Error message`: IPC or wrapper-level failure.
- `Path`: normalized path returned by provisioning operations. `gitInitRepository` maps success directly to `Result<string, string>` in the renderer wrapper.

Read operations return typed DTOs:

```fsharp
type GitStatusDto = {
    Current: string option
    Tracking: string option
    Ahead: int
    Behind: int
    IsClean: bool
    Conflicted: string[]
    IsMergeInProgress: bool
    Files: GitFileStatusDto[]
}

type GitBranchRefDto = {
    RefName: string
    DisplayLabel: string
    Kind: GitBranchRefKind
    IsCurrent: bool
    IsTracking: bool
}

type GitDiffSummaryDto = {
    Changed: int
    Insertions: int
    Deletions: int
}
```

Failure kinds are `Unauthorized`, `Forbidden`, `Network`, `Timeout`, `Canceled`, `LfsInstallRequired`, and `Unknown`.

## 4. Common Calls

Refresh status, branches, and LFS settings:

```fsharp
promise {
    let! statusResult = Renderer.GitApiClient.getGitStatus ()
    let! branchResult = Renderer.GitApiClient.getGitBranches ()
    let! lfsSettingsResult = Renderer.GitApiClient.getGitLfsSettings ()

    match statusResult, branchResult, lfsSettingsResult with
    | Ok status, Ok branches, Ok settings ->
        Browser.Dom.console.log($"Branch: {status.Current}")
        Browser.Dom.console.log($"Changes: {status.Files.Length}")
        Browser.Dom.console.log($"Branches: {branches.Length}")
        Browser.Dom.console.log($"LFS threshold: {settings.AutoTrackThresholdMb} MB")
    | _ ->
        Browser.Dom.console.warn("Could not refresh all Git state.")
}
```

Initialize an ARC folder as a repository:

```fsharp
promise {
    let! result = Renderer.GitApiClient.gitInitRepository arcPath

    match result with
    | Ok normalizedPath -> Browser.Dom.console.log($"Initialized: {normalizedPath}")
    | Error message -> Browser.Dom.console.error(message)
}
```

Clone a repository:

```fsharp
let request: GitCloneRepositoryRequest = {
    RemoteUrl = "https://git.nfdi4plants.org/group/project.git"
    TargetPath = @"C:\ARCs\project"
    Branch = None
    DownloadLargeFiles = true
}

promise {
    let! result = Renderer.GitApiClient.gitCloneRepository request

    match result with
    | Ok op when op.Success -> Browser.Dom.console.log(op.Path)
    | Ok op -> Browser.Dom.console.error(op.Message |> Option.defaultValue "Clone failed.")
    | Error message -> Browser.Dom.console.error(message)
}
```

Commit selected files:

```fsharp
promise {
    let! stageResult =
        Renderer.GitApiClient.gitStagePaths {
            Pathspecs = [| "assays/a1/dataset.xlsx"; "README.md" |]
        }

    match stageResult with
    | Ok op when op.Success ->
        let! commitResult =
            Renderer.GitApiClient.gitCommit {
                Message = "Update assay metadata"
            }

        match commitResult with
        | Ok commit when commit.Success -> Browser.Dom.console.log(commit.Message)
        | Ok commit -> Browser.Dom.console.error(commit.Message)
        | Error message -> Browser.Dom.console.error(message)
    | Ok op -> Browser.Dom.console.error(op.Message)
    | Error message -> Browser.Dom.console.error(message)
}
```

Fetch, preview pull, pull, and push:

```fsharp
let remoteRequest: GitRemoteOperationRequest = {
    Remote = None
    Branch = None
}

promise {
    let! preview = Renderer.GitApiClient.previewGitPull remoteRequest

    match preview with
    | Ok { Status = GitPullPreflightStatus.SafeToPull } ->
        let! pull = Renderer.GitApiClient.gitPull remoteRequest
        Browser.Dom.console.log(pull)
    | Ok { Status = GitPullPreflightStatus.WouldRequireMergeResolution; Message = message } ->
        Browser.Dom.console.warn(defaultArg message "Pull would require merge resolution.")
    | Ok { Status = GitPullPreflightStatus.Indeterminate; Message = message } ->
        Browser.Dom.console.warn(defaultArg message "Pull preview was inconclusive.")
    | Error message ->
        Browser.Dom.console.error(message)
}
```

## 5. Main Git Services

`GitService.fs` owns Git operations for the active ARC repository path:

- Status and refs: `getStatus`, `getBranches`
- Diff and page data: `getDiffSummary`, `getDiff`, `getWordDiff`, `getDiffViewData`, `getMergeConflictViewData`
- Remote sync: `fetch`, `previewPull`, `pull`, `push`
- Local writes: `stagePaths`, `unstagePaths`, `commit`, `createBranch`, `checkoutBranch`, `addRemote`
- LFS settings: `getLfsSettings`, `setLfsSettings`
- Merge resolution: `confirmMergeResolution`

`GitProvisioningService.fs` owns path-driven operations that do not require an active ARC:

- `initRepository`
- `cloneRepository`

`GitLfsService.fs` owns Git LFS command orchestration and push planning:

- System install/probe: `installSystem`, `isSystemInstalled`
- Tracking: `track`, `isTrackedByAttributes`
- Push support: `planOutboundPush`, `uploadObjects`, `collectPushDiagnostics`

`GitAuthAdapter.fs` builds scoped auth config and redacts secrets. `GitTokenProvider.fs` is the process-wide token lookup hook installed by `AuthService`.

## 6. Validation and Security Rules

Branch-like names are validated by `GitService.ensureValidBranchLikeName`.

Pathspecs are validated by `GitService.ensureValidPathspec` and must be ARC-relative. Empty values, absolute paths, traversal segments (`.` or `..`), and null characters are rejected.

Remote names are validated by `GitService.validateRemoteName`; blank input defaults to `origin`.

Remote URLs are validated by `GitService.ensureAllowedRemoteUrl`. Only full `https://` and `ssh://` URLs are accepted. These are rejected:

- `file://`
- `ext::`
- `fd::`
- protocol override attempts such as `-c protocol...`
- SCP-style SSH URLs such as `git@git.nfdi4plants.org:group/project.git`

Use `ssh://git@git.nfdi4plants.org/group/project.git` instead of SCP-style SSH.

All simple-git instances are created through `GitInternals.createGit`, which applies `GIT_TERMINAL_PROMPT=0`. Credentials are injected per command through config entries or command environment and are not persisted to repository config.

## 7. Authentication

`AuthService.fs` installs the active `GitTokenProvider` after sign-in. Git services extract the host from the remote URL and call `tryGetAccessToken host`.

Current behavior:

- `fetch`, `previewPull`, `pull`, and `push` require a token for the selected remote host. If none is available, they fail with `Unauthorized`.
- `cloneRepository` uses a token when one is available. If no token is available, clone runs unauthenticated.
- Authenticated clone failures are returned as failures. There is no unauthenticated retry/fallback after an authenticated clone failure.
- `initRepository`, local status/diff/stage/commit/branch operations, and `addRemote` do not need a token.

Auth config is scoped through `GitAuthAdapter.buildAuthArgs` and `GitAuthAdapter.applyAuth`. Error messages and diagnostics must pass through `redactToken` or the shared failure path before crossing IPC.

## 8. Git LFS

Swate uses Git LFS in three places:

- Stage-time auto tracking for selected files larger than `swate.lfs.autotrackthresholdmb`.
- Commit-time validation that oversized staged blobs are tracked by LFS.
- Pull/clone hydration of LFS content when `swate.lfs.downloadlargefiles` is true.
- Push-time explicit upload of outbound LFS objects before the git ref push.

Settings are stored in local repository config:

- `swate.lfs.autotrackthresholdmb`: integer, default `1`, maximum `100`.
- `swate.lfs.downloadlargefiles`: boolean, default `true` in Main Git service.

Renderer state starts with `DownloadLargeFiles = false` until repository settings are loaded. Use `getGitLfsSettings` after opening an ARC to get the effective repository values.

When Git LFS is required but unavailable, operations return `FailureKind = Some GitFailureKind.LfsInstallRequired` with a message suitable for the install prompt. The renderer workflow calls `installGitLfs` and retries the original operation after a successful install.

Clone always sets `GIT_LFS_SKIP_SMUDGE=1` first. If `DownloadLargeFiles = true`, clone then persists the setting and runs `git lfs pull` to hydrate content.

Pull applies `GIT_LFS_SKIP_SMUDGE` when large-file download is disabled. When enabled, it hydrates with `git lfs pull` after the git pull.

Push uses `GitLfsService.planOutboundPush` to detect outbound LFS pointer objects. If needed, `GitLfsService.uploadObjects` uploads exact object IDs before the git push; if exact upload is unsupported by the installed git-lfs, it falls back to refspec upload.

## 9. Branches and Pull Workflow

`getGitBranches` returns local branches and remote branch refs. The renderer maps remote branch switches to:

```fsharp
{
    Name = derivedLocalBranchName
    StartPoint = Some remoteRefName
}
```

`checkoutBranch` behavior:

- `StartPoint = None`: switch to an existing local branch only.
- `StartPoint = Some ref`: create/check out `Name` from the provided start point.

`createBranch` creates and switches to a new local branch. After branch creation or checkout, Main reconciles tracking against `origin/<branch>` when that remote branch exists.

`previewGitPull` fetches the remote and runs `git merge-tree --write-tree HEAD <upstream>` to classify the pull:

- `SafeToPull`: renderer may continue directly.
- `WouldRequireMergeResolution`: renderer should ask before opening merge resolution flow.
- `Indeterminate`: renderer should ask because the preflight could not classify safely.

## 10. Diff and Merge Resolution

The renderer loads diff pages through `getGitDiffViewData`. Main returns previous content, current content, and porcelain word-diff metadata. Explicitly unsupported binary-like extensions and likely binary buffers return `GitPageLoadResultDto.Unsupported`, which the IPC layer maps to an unsupported-content page instead of throwing.

Merge conflict flow:

1. `getGitStatus` exposes `Conflicted` and `IsMergeInProgress`.
2. `getGitMergeConflictViewData path` loads the current conflicted file content.
3. Renderer edits the resolved content.
4. `confirmGitMergeResolution` checks that the file still matches the expected conflict content, writes the resolved content, stages the path, and optionally commits when no conflicts remain.

The expected-content guard prevents overwriting a file that changed after the renderer opened it.

## 11. IPC Busy and Progress Behavior

`Main.IPC.IGitApi` maps `GitService.GitResult<'T>` to shared DTOs.

Operations wrapped in `withBusyWriting`:

- `gitPull`
- `gitStagePaths`
- `gitUnstagePaths`
- `gitCommit`
- `createBranch`
- `checkoutBranch`
- `confirmGitMergeResolution`

Operations not wrapped:

- Read-only calls: `getGitStatus`, `getGitBranches`, `getGitLfsSettings`, diff view loaders, merge conflict view loader.
- Remote metadata/sync calls without working tree writes: `gitFetch`, `gitPush`, `previewGitPull`.
- Provisioning calls: `gitInitRepository`, `gitCloneRepository`.
- System Git LFS install.

Progress is sent through `IMainUpdateRendererApi.gitProgressUpdate`. Fetch, preview pull, pull, push, and clone can report progress. Clone only reports progress when Main can resolve a vault from the IPC window id; otherwise the clone still runs.

## 12. Extending Git Functionality

When adding a new Git operation:

1. Add or reuse DTOs in `Swate.Electron.Shared.GitTypes`.
2. Add the IPC function to `IGitApi` in `IPCTypes.fs`.
3. Implement Main handling in `src/Electron/src/Main/IPC/IGitApi.fs`.
4. Put Git command logic in the appropriate Main service:
   - Active ARC repo operation: `GitService.fs`
   - Init/clone/path provisioning: `GitProvisioningService.fs`
   - Git LFS orchestration: `GitLfsService.fs`
   - Low-level spawned git: `GitLfsAdapter.fs`
5. Add a wrapper in `Renderer.GitApiClient.fs`.
6. Wire it into `GitWorkflow.GitDependencies` if renderer workflow state needs it.
7. Add or update tests in `tests/Electron.Core`.
8. Update this guide if behavior or consumer usage changes.

Keep validation in Main even if Renderer already validates input. Renderer validation is for UX; Main validation is the trust boundary.

## 13. Troubleshooting

`Unauthorized` on fetch, preview pull, pull, or push:

- Confirm an account is signed in and `AuthService` has installed a token provider.
- Confirm the provider returns a token for the remote host.
- Confirm the remote URL is `https://` or full-form `ssh://`.

Clone fails although the repository is public:

- If a token is available, clone runs authenticated and does not retry unauthenticated after failure.
- Sign out or adjust the active account if you intentionally need unauthenticated clone behavior.
- Check target path rules: target must be missing or an empty directory, not a symlink/junction.

Git LFS install prompt appears:

- The operation needs Git LFS but `git lfs` is not available.
- Use `installGitLfs` through the renderer workflow; after success, retry the original operation.

Pathspec rejected:

- Use ARC-relative paths with `/`.
- Do not pass absolute paths, `.` or `..` segments, empty values, or null characters.

Remote branch checkout fails:

- For remote-only branches, call `checkoutBranch` with `StartPoint = Some "origin/branch"` and `Name = "branch"`.
- Calling with `StartPoint = None` only works for existing local branches.

Unsupported diff or merge content:

- Binary files and explicitly unsupported extensions are intentionally routed to the unsupported-content page.
- Text-based diff and merge views only support files Main can safely read as text.
