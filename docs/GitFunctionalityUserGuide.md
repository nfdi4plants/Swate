# Swate Git Guide (Electron)

This guide is about the implemented Git functionality in Swate Electron: how to call it, how auth is wired, and how the Main/IPC Git modules behave.

## 1. Quick Use

### Where to call from

Renderer code calls Git via the `IGitApi` IPC bridge:

- Contract: `src/Electron/src/Swate.Electron.Shared/IPCTypes.fs` (`type IGitApi`)
- Renderer client binding: `src/Electron/src/Swate.Electron.Shared/Api.fs` (`gitApi`)

Note: `Api.fs` only exposes the raw `IGitApi` client binding (`gitApi`) and no per-endpoint Git helper functions.

All `IGitApi` methods are `IpcMainEvent -> ...`, where the `IpcMainEvent` is supplied by Electron/Main. Renderer code must **not** pass a placeholder event argument (it would get sent over IPC and shift the real arguments). Instead, coerce each endpoint once to a signature **without** the event parameter using `unbox` (see examples).

Avoid calling endpoints as `gitApi.someMethod null ...` / `gitApi.someMethod Unchecked.defaultof<IpcMainEvent> ...` — this shifts arguments for methods that take parameters.

### `IGitApi` endpoints

- `getGitStatus`
- `getGitDiffSummary`
- `gitFetch`
- `gitPull`
- `gitPush`
- `gitInitRepository`
- `gitCloneRepository`
- `gitStagePaths`
- `gitUnstagePaths`
- `gitCommit`
- `createBranch`
- `checkoutBranch`

### Core result shape

Most write/sync operations return:

```fsharp
type GitOperationResult = {
    Success: bool
    Message: string option
    FailureKind: GitFailureKind option
    Path: string option
}
```

Interpretation:

- `Success=true`: operation completed.
- `Success=false`: Git operation failed; inspect `FailureKind` and `Message`.
- outer `Result.Error exn`: IPC-level failure.
- `Path` is used by provisioning endpoints:
  - `gitInitRepository` success -> normalized path
  - `gitCloneRepository` success -> normalized path
  - other operations -> `None`

### Read operation result shapes

`getGitStatus` and `getGitDiffSummary` return their own typed DTOs, not `GitOperationResult`:

```fsharp
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
```

Return type: `JS.Promise<Result<GitStatusDto, exn>>` / `JS.Promise<Result<GitDiffSummaryDto, exn>>`.

Note: on failure, these operations return `Result.Error exn` with the failure kind embedded in the exception message (e.g., `"git status failed (Network): ..."`) — unlike write/sync operations which return structured `GitOperationResult` with a typed `FailureKind` field.

---

## 2. Usage Examples

### 2.0 Check repository status

```fsharp
open Fable.Core
open Swate.Electron.Shared.IPCTypes
open Api

let getGitStatus : unit -> JS.Promise<Result<GitStatusDto, exn>> =
    unbox gitApi.getGitStatus

let getGitDiffSummary : unit -> JS.Promise<Result<GitDiffSummaryDto, exn>> =
    unbox gitApi.getGitDiffSummary

let checkStatus () =
    promise {
        let! statusRes = getGitStatus ()
        match statusRes with
        | Ok status ->
            Browser.Dom.console.log($"Branch: {status.Current}, Clean: {status.IsClean}")
            Browser.Dom.console.log($"Ahead: {status.Ahead}, Behind: {status.Behind}")
            for file in status.Files do
                Browser.Dom.console.log($"  {file.Index}{file.WorkingDir} {file.Path}")
        | Error ex ->
            Browser.Dom.console.error($"Status error: {ex.Message}")

        let! diffRes = getGitDiffSummary ()
        match diffRes with
        | Ok diff ->
            Browser.Dom.console.log($"Changed: {diff.Changed}, +{diff.Insertions}, -{diff.Deletions}")
        | Error ex ->
            Browser.Dom.console.error($"Diff error: {ex.Message}")
    }
```

### 2.1 Init a new repository

```fsharp
open Fable.Core
open Swate.Electron.Shared.IPCTypes
open Api

let gitInitRepository : string -> JS.Promise<Result<GitOperationResult, exn>> =
    unbox gitApi.gitInitRepository

let initRepo (targetPath: string) =
    promise {
        let! response = gitInitRepository targetPath
        match response with
        | Error ex ->
            Browser.Dom.console.error($"IPC error: {ex.Message}")
        | Ok op when op.Success ->
            Browser.Dom.console.log($"Initialized at {op.Path}")
        | Ok op ->
            Browser.Dom.console.error($"Init failed: {op.FailureKind} {op.Message}")
    }
```

### 2.2 Clone a repository

```fsharp
open Fable.Core
open Swate.Electron.Shared.IPCTypes
open Api

let gitCloneRepository : GitCloneRepositoryRequest -> JS.Promise<Result<GitOperationResult, exn>> =
    unbox gitApi.gitCloneRepository

let cloneRepo () =
    promise {
        let request = {
            RemoteUrl = "https://github.com/org/repo.git"
            TargetPath = @"C:\repos\repo"
            Branch = Some "main"
        }

        let! response = gitCloneRepository request
        match response with
        | Error ex ->
            Browser.Dom.console.error($"IPC error: {ex.Message}")
        | Ok op when op.Success ->
            Browser.Dom.console.log($"Cloned to {op.Path}")
        | Ok op ->
            Browser.Dom.console.error($"Clone failed: {op.FailureKind} {op.Message}")
    }
```

### 2.3 Fetch/pull/push

```fsharp
open Fable.Core
open Swate.Electron.Shared.IPCTypes
open Api

let gitFetch : GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, exn>> =
    unbox gitApi.gitFetch

let gitPull : GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, exn>> =
    unbox gitApi.gitPull

let gitPush : GitRemoteOperationRequest -> JS.Promise<Result<GitOperationResult, exn>> =
    unbox gitApi.gitPush

let syncRemote () =
    promise {
        let req = { Remote = Some "origin"; Branch = Some "main" }

        let! fetchRes = gitFetch req
        let! pullRes  = gitPull req
        let! pushRes  = gitPush req

        let report name result =
            match result with
            | Error ex -> Browser.Dom.console.error($"{name} IPC error: {ex.Message}")
            | Ok op when op.Success -> Browser.Dom.console.log($"{name} ok")
            | Ok op -> Browser.Dom.console.error($"{name} failed: {op.FailureKind} {op.Message}")

        report "fetch" fetchRes
        report "pull" pullRes
        report "push" pushRes
    }
```

### 2.4 Stage/unstage/commit

```fsharp
open Fable.Core
open Swate.Electron.Shared.IPCTypes
open Api

let gitStagePaths : GitPathspecRequest -> JS.Promise<Result<GitOperationResult, exn>> =
    unbox gitApi.gitStagePaths

let gitCommit : GitCommitRequest -> JS.Promise<Result<GitOperationResult, exn>> =
    unbox gitApi.gitCommit

let commitFlow () =
    promise {
        let! stageRes = gitStagePaths { Pathspecs = [| "README.md" |] }
        match stageRes with
        | Ok s when s.Success ->
            let! commitRes = gitCommit { Message = "Update README" }
            match commitRes with
            | Ok c when c.Success -> Browser.Dom.console.log("Commit complete")
            | Ok c -> Browser.Dom.console.error($"Commit failed: {c.Message}")
            | Error ex -> Browser.Dom.console.error($"Commit IPC error: {ex.Message}")
        | Ok s -> Browser.Dom.console.error($"Stage failed: {s.Message}")
        | Error ex -> Browser.Dom.console.error($"Stage IPC error: {ex.Message}")
    }
```

### 2.5 Branch operations

`createBranch` both creates and switches to the new branch (via `checkoutLocalBranch` or `checkoutBranch` in simple-git). There is no need to call `checkoutBranch` afterwards.

`checkoutBranch` switches to an existing local branch. It fails if the branch does not exist locally.

```fsharp
open Fable.Core
open Swate.Electron.Shared.IPCTypes
open Api

let createBranch : GitCreateBranchRequest -> JS.Promise<Result<GitOperationResult, exn>> =
    unbox gitApi.createBranch

let checkoutBranch : GitCheckoutBranchRequest -> JS.Promise<Result<GitOperationResult, exn>> =
    unbox gitApi.checkoutBranch

// Create a new branch and switch to it in one step:
let createAndSwitch () =
    promise {
        let! result = createBranch { Name = "feature/my-change"; StartPoint = None }
        match result with
        | Ok op when op.Success -> Browser.Dom.console.log("Created and switched to feature/my-change")
        | Ok op -> Browser.Dom.console.error($"Create failed: {op.FailureKind} {op.Message}")
        | Error ex -> Browser.Dom.console.error($"IPC error: {ex.Message}")
    }

// Switch to an existing local branch:
let switchBranch () =
    promise {
        let! result = checkoutBranch { Name = "main" }
        match result with
        | Ok op when op.Success -> Browser.Dom.console.log("Switched to main")
        | Ok op -> Browser.Dom.console.error($"Checkout failed: {op.FailureKind} {op.Message}")
        | Error ex -> Browser.Dom.console.error($"IPC error: {ex.Message}")
    }
```

---

## 3. Token Provider (`GitTokenProvider.fs`)

File:

- `src/Electron/src/Main/Git/GitTokenProvider.fs`

### 3.1 How it works

```fsharp
type GitTokenProvider = { TryGetAccessToken: string -> JS.Promise<string option> }
```

- Main process stores an active provider in memory.
- Default provider always returns `None`.
- Git services call `tryGetAccessToken host`.
- Host comes from `tryExtractHostFromRemoteUrl` and must be from `https://` or `ssh://` URL.

### 3.2 How to add your own provider

```fsharp
open Fable.Core
open Main.Git.GitTokenProvider

let provider : GitTokenProvider = {
    TryGetAccessToken = fun host -> promise {
        match host with
        | "github.com" -> return Some "ghp_..."
        | _ -> return None
    }
}

setTokenProvider provider
```

Call this during Main startup before user-triggered Git operations.

### 3.3 Behavior impact

- Fetch/pull/push always require a token from the configured provider. If no token is available for the extracted host, the operation fails with `Unauthorized` — even for public remotes. There is no unauthenticated fallback for these operations.
- Clone provisioning uses token-first strategy with one unauthenticated fallback for auth failures only. If no token is available, clone runs unauthenticated directly.

---

## 4. `GitAuthAdapter.fs` internals

File:

- `src/Electron/src/Main/Git/GitAuthAdapter.fs`

### What it does

1. Applies non-interactive env:
   - `GIT_TERMINAL_PROMPT=0`
2. Builds per-operation auth config:
   - `-c http.extraHeader=Authorization: Bearer <token>`
3. Redacts secrets in text/args.

### Key functions

- `createNonInteractiveEnv`
- `applyNonInteractiveEnv`
- `buildAuthArgs` (note: the `host` parameter is currently unused — host-specific auth argument shaping is reserved for future use)
- `toConfigEntries`
- `applyAuth`
- `redactToken`
- `redactArgs`

Important detail: auth is injected in-memory per operation; token is not persisted to repo config files.

---

## 5. `GitService.fs` internals (ARC-scoped operations)

File:

- `src/Electron/src/Main/Git/GitService.fs`

### 5.1 Role

Handles Git operations for the active ARC repository path.

### 5.2 Validation layer

- `ensureValidBranchLikeName`
- `ensureValidPathspec`
- `validatePathspecs`
- `validateRemoteName`
- `ensureAllowedRemoteUrl`

These block invalid refs, traversal, blocked protocols, and protocol override attempts.

### 5.3 Failure classification

`classifyFailureKind` maps text to:

- `Unauthorized`
- `Forbidden`
- `Network`
- `Timeout`
- `Canceled`
- `Unknown`

Error text is redacted in shared failure pipeline.

### 5.4 Execution wrappers

- `withLocalGit`: ensures repo exists at ARC path, then runs operation.
- `withAuthenticatedGit`: resolves authenticated git instance then runs operation.

`createAuthenticatedGit` steps:

1. read remote URL (`remote get-url`)
2. URL policy check
3. host extraction
4. token lookup
5. auth-scoped git instance creation

### 5.5 Public operations

- Read:
  - `getStatus`
  - `getDiffSummary`
- Remote sync:
  - `fetch`
  - `pull`
  - `push`
- Local modifications:
  - `stagePaths`
  - `unstagePaths`
  - `commit`
  - `createBranch`
  - `checkoutBranch`

---

## 6. `GitProvisioningService.fs` internals (init/clone)

File:

- `src/Electron/src/Main/Git/GitProvisioningService.fs`

### 6.1 `initRepository`

Flow:

1. validate target path (non-empty, no null byte)
2. normalize absolute path
3. if target exists:
   - reject non-directory targets (files, symlinks/junctions)
   - reject when already a git repository
4. if missing, create directory recursively
5. run `git init`
6. return normalized path

### 6.2 `cloneRepository`

Flow:

1. validate remote URL policy
2. extract host
3. validate/normalize target path
4. validate optional branch
5. ensure parent path exists and is a directory (create if missing; reject non-directory/symlink)
6. reject non-directory clone target (files, symlinks/junctions)
7. enforce strict target emptiness (missing or empty directory only)
8. set clone git `baseDir = targetParent`
9. run token-first auth strategy

### 6.3 Clone auth/fallback behavior

- token present:
  - try authenticated clone
  - on `Forbidden`: retry once unauthenticated
  - on `Unauthorized`: retry once unauthenticated **only** for common auth failures (HTTP 401/auth prompts/SSH publickey); otherwise no retry
- token missing:
  - unauthenticated clone directly
- token provider throws:
  - fail immediately

### 6.4 Retry cleanup behavior

Before the unauthenticated retry (after an auth failure), the service performs a guarded cleanup to avoid retrying against a dirty partially-cloned state.

Current guard rules:

- if target path is missing: nothing to clean
- if target path is an empty directory: nothing to clean (keep it)
- if target directory contains **only** a `.git` entry (case-insensitive): delete `.git` **only**
- otherwise: refuse cleanup and fail the retry (unexpected files, symlinks/junctions, `.git` not a directory, concurrent directory changes)

---

## 7. IPC integration (`IGitApi`)

Files:

- `src/Electron/src/Swate.Electron.Shared/IPCTypes.fs` (shared contract)
- `src/Electron/src/Main/IPC/IGitApi.fs`
- `src/Electron/src/Main/main.fs` (Main registration)
- `src/Electron/src/Preload/preload.fs` (Preload bridge)

### 7.1 Added provisioning endpoints

- `gitInitRepository`
- `gitCloneRepository`

These are path-driven and do not require active ARC path.

### 7.2 Result mapping

`toGitOperationResult` maps `GitService.GitResult<'T>` into shared DTO and now supports optional success path projection.

- init/clone set `Path = Some normalizedPath` on success
- existing operations keep `Path = None`

### 7.3 Progress behavior

- fetch/pull/push: progress reporter from active vault.
- clone: progress reporter only if vault can be resolved from window id; otherwise clone still runs without progress callback.

### 7.4 Busy-writing policy

Operations wrapped with `withBusyWriting`:
- `gitPull`
- `gitStagePaths`
- `gitUnstagePaths`
- `gitCommit`
- `createBranch`
- `checkoutBranch`

Operations **not** wrapped:
- `gitFetch`, `gitPush` (no working tree edits; remote sync / `.git` metadata only)
- `getGitStatus`, `getGitDiffSummary` (read-only)
- `gitInitRepository`, `gitCloneRepository` (provisioning, no active ARC vault)

---

## 8. Troubleshooting

### 8.1 Unauthorized on fetch/pull/push

Check:

1. `setTokenProvider` registration exists in Main startup.
2. provider returns token for extracted host.
3. token is valid for target remote.

### 8.2 Clone fallback still fails

Possible reasons:

- cleanup before fallback retry failed
- `Unauthorized` did not match an auth-failure signal -> no fallback retry
- non-auth failure kind (network/timeout/canceled/unknown) -> no fallback retry
- remote blocked by URL policy

### 8.3 Remote URL rejected

Only `https://` and `ssh://` full-URI forms are accepted.

SCP-style SSH URLs (e.g., `git@github.com:org/repo.git`) are **not** accepted. Use the full URI form instead: `ssh://git@github.com/org/repo.git`.

### 8.4 Pathspec rejected

Pathspecs must be relative, non-empty, and must not include traversal segments (`.` or `..`), null characters, or Windows-style absolute prefixes (e.g., `C:/`).

### 8.5 `checkoutBranch` fails for remote-only branches

`checkoutBranch` only works for branches that already exist locally. If you need to switch to a remote branch, fetch first, then create a local tracking branch with `createBranch`.

---

## 9. Security and scope boundaries

Current behavior enforces:

- no token persistence in repository files
- non-interactive git env (`GIT_TERMINAL_PROMPT=0`)
- redaction of bearer credentials/credential URLs in error paths
- no automatic ARC opening/window switching after clone
- no auto stage/commit/push after init

### Additional notes

- All git instances are created through `GitInternals.createGit`, which always applies `applyNonInteractiveEnv` to enforce `GIT_TERMINAL_PROMPT=0`.
- `maxConcurrentProcesses = 1` serializes operations per repository instance to avoid overlapping write/sync races.
- Cancellation/abort support is intentionally deferred. No cancel IPC contract exists in this milestone.
