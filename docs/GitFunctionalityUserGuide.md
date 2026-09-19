# Swate version control guide

This guide describes how Swate Electron talks to a version control provider. It is for developers who call, extend, test or troubleshoot that functionality. Git is the provider Swate provisions today. The code paths above the Electron composition root are provider neutral, so a second provider (lakeFS is compiled in without a configured connection) needs no change in the IPC or the renderer.

## 1. Architecture

Version control operations run in the Electron main process through the VersionControlService NuGet package and reach the renderer through the typed IPC bridge.

- Library: the `VersionControlService` package (Abstractions, Git and lakeFS providers). Its `WorkspaceSession` record exposes the core service plus optional services (synchronization, text diff, conflict resolution, object materialization, storage policy, maintenance, repository browser).
- Composition root: `src/Electron/src/Main/VersionControl/ProviderComposition.fs` builds the Git factory (credentials and commit identity from the DataHub accounts, see `DataHubStrategies.fs`) and the lakeFS factory, and `VersionControlRuntime.fs` holds the catalog, the binding store and the resolver. This folder is the only place that knows provider ids or provider types.
- Session host: `src/Electron/src/Main/VersionControl/WorkspaceSessionHost.fs` keeps one open session per vault root, adopts unbound repositories on first open, persists the workspace binding, owns the registry of running operations for cancellation, and holds the per-session settings.
- IPC handler: `src/Electron/src/Main/IPC/IVersionControlApi.fs` implements `IVersionControlApi` from `src/Electron/src/Swate.Electron.Shared/IPCTypes.fs` over the DTOs in `VersionControlTypes.fs`. `Mappings.fs` translates every library type to its DTO and validates paths, refs and revisions before a provider call.
- Renderer client: `src/Electron/src/Renderer/VersionControlApiClient.fs` wraps the bridge, generates operation ids and maps `Result<'T, exn>` to `Result<'T, string>`.
- Renderer workflow: `src/Electron/src/Renderer/Context/GitWorkflow.fs` is the Elmish state machine behind the Git sidebar, wired in `GitStateContext.fs`. Feature code reaches it through `GitWorkflow.GitDependencies`.

The DataHub specific pieces stay in Swate: the GitLab API client (`Main/Auth/GitLabApi.fs`), the account store, and the Git LFS ruleset in `src/Shared/GitLfsRules.fs`.

## 2. Renderer API

Every function of `Renderer.VersionControlApiClient` takes a request DTO that carries an `OperationId`. The renderer allocates the id with `VersionControlApiClient.newOperationId ()` before the call, so the cancel button knows the key before the main process has started the operation.

```fsharp
getSessionInfo: OperationRequestDto -> JS.Promise<Result<OperationResultDto<WorkspaceSessionInfoDto>, string>>
cloneWorkspace: CloneWorkspaceRequestDto -> JS.Promise<Result<OperationResultDto<string>, string>>
initializeWorkspace: InitializeWorkspaceRequestDto -> JS.Promise<Result<OperationResultDto<string>, string>>
bindWorkspace: BindWorkspaceRequestDto -> JS.Promise<Result<OperationResultDto<WorkspaceSessionInfoDto>, string>>
cancelOperation: OperationKeyDto -> JS.Promise<Result<bool, string>>
checkDependencies: OperationRequestDto -> JS.Promise<Result<OperationResultDto<DependencyStatusDto[]>, string>>
installDependency: InstallDependencyRequestDto -> JS.Promise<Result<OperationResultDto<DependencyStatusDto>, string>>
getStatus, listRefs, createRef, preflightSwitchRef, switchRef
createRevision, restorePaths
getDiffSummary, getTextDiff, getWordDiff, getBaseContent
refreshSynchronization, previewUpdate, update, publish
getActiveConflictSession, resolveConflict, finalizeConflict, cancelConflict
listObjects, materializeObject, dematerializeObject
getStoragePolicySettings, setStoragePolicySettings, setPathStoragePolicy, pruneStorage, deduplicateStorage
getRepositoryWebUrl, clearStaleLock
```

The main process sends two events to the renderer (`MainToRendererIpc.IVersionControlRendererApi`): `versionControlOperationStarted` with the final `OperationKeyDto` (session id and operation id) as soon as the operation is registered, and `versionControlProgress` with float `Completed` and `Total` counters plus a phase code.

Do not call `Api.ipcVersionControlApi` from feature code. Go through the client, or through `GitWorkflow.GitDependencies` when the sidebar state has to follow.

## 3. Result shapes

Every call returns `Result<OperationResultDto<'T>, string>`. The outer `Error` is a transport failure (the IPC call itself failed). The inner value has three shapes:

```fsharp
type OperationResultDto<'T> =
    | Succeeded of OperationOutcomeDto<'T>
    | PartiallySucceeded of OperationOutcomeDto<'T> * OperationFailureDto
    | Failed of OperationFailureDto
```

`OperationOutcomeDto` carries the value, the effect (`Performed` or `NoOp` with a reason), warnings, affected paths and the resulting revision and workspace version. `OperationFailureDto` carries `Category`, `Code`, `Message`, `StateChanged`, `Retryable`, `AffectedPaths`, `RecoveryAction` (a code plus optional instructions), `Details` and `RevisionEvidence`.

Routing is structural. Code that decides what to do next keys on `Category`, `Code`, `RecoveryAction.Code`, `StateChanged` or `AffectedPaths`. Never classify a failure by its message text.

Categories: `Validation`, `NotFound`, `Concurrency`, `Authentication`, `Authorization`, `DependencyMissing`, `Network`, `Timeout`, `Canceled`, `Conflict`, `Unsupported`, `ProviderError`.

Codes the renderer handles are literals in `VersionControlCodes` (`VersionControlTypes.fs`). Library codes include `identity_missing`, `publish_target_missing`, `target_unreachable`, `precondition_failed`, `conflicts_detected`, `conflict_session_active`, `target_not_empty` and `operation_canceled`. Swate host codes include `service_unavailable` (the provider has no such optional service), `session_unavailable`, `workspace_unmanaged`, `workspace_ambiguous`, `location_unsupported`, `lock_removal_refused`, `binding_not_persisted`, `transport_error` (the IPC call failed before a structured result existed) and `storage_policy_blocked` (the DataHub ruleset refused a manual storage policy change, see section 8). A threshold outside 1 to 100 MiB is refused with the library's `invalid_lfs_threshold`.

Recovery codes (`VersionControlCodes.Recovery`) tell the renderer which dialog to open after a canceled or partial operation: `remove_index_lock`, `restore_workspace`, `refresh_workspace`, `inspect_workspace`, `abort_merge`, `retry_materialization`, `reconcile_materialization`, `reconcile_index`, `resolve_conflict_session`, `refresh_conflict_session`, `remove_clone_target`.

Helpers on `OperationResultDto` (`map`, `tryValue`, `tryFailure`, `isCanceled`, `recoveryCode`) cover the common checks.

## 4. Common calls

Refresh the sidebar state:

```fsharp
promise {
    let request () = VersionControlApiClient.request (VersionControlApiClient.newOperationId ())
    let! status = VersionControlApiClient.getStatus (request ())
    let! refs = VersionControlApiClient.listRefs (request ())
    let! settings = VersionControlApiClient.getStoragePolicySettings (request ())

    match status, refs, settings with
    | Ok(OperationResultDto.Succeeded status), Ok(OperationResultDto.Succeeded refs), Ok(OperationResultDto.Succeeded settings) ->
        Browser.Dom.console.log ($"Ref: {status.Value.CurrentRef.Name}")
        Browser.Dom.console.log ($"Changes: {status.Value.Changes.Length}")
        Browser.Dom.console.log ($"Refs: {refs.Value.Length}")
        Browser.Dom.console.log ($"Threshold: {settings.Value.AutoPolicyThresholdMb}")
    | _ -> Browser.Dom.console.warn ("Could not refresh the version control state.")
}
```

The status carries `WorkspaceVersion`, an opaque optimistic concurrency token. Mutations (`createRevision`, `restorePaths`, `update`, `publish`, `switchRef`) send it back as `ExpectedWorkspaceVersion`. A stale token fails with `precondition_failed` and `StateChanged = false`, and the workflow refreshes and retries once.

Primary save in the sidebar: `createRevision` with the exact selected paths, refresh, then either `publish` directly (no target or no tracking ref yet) or `previewUpdate`, a confirmation when the preview predicts a conflict session or data loss, `update`, and `publish`. A `publish` that fails with `publish_target_missing` creates the project on the DataHub through `IGitLabApi.createProject`, binds the workspace with `bindWorkspace` and publishes again. A `target_unreachable` failure is an outage and never triggers provisioning.

## 5. Main process structure

`src/Electron/src/Main/VersionControl`:

- `DataHubStrategies.fs`: the credential strategy (token of the account matching the target host, the active account when no host is known, `None` when nobody is signed in) and the identity strategy (commit name and email of that account). Strategies are cheap and side effect free.
- `WorkspaceBindingStore.fs`: the persisted `WorkspaceBinding` per vault root in the app settings, with tolerant decoding.
- `ProviderComposition.fs`: factories, catalog, provider ids, location parsing (`https://` and `ssh://` locations belong to Git, `lakefs://` to lakeFS), the stale lock paths, and `dataHubRevisionPolicy`, the DataHub ruleset as the library's revision policy handed to both factories.
- `VersionControlRuntime.fs`: the process wide runtime (catalog, binding store, resolver).
- `Mappings.fs`: library types to DTOs and back, plus path, ref and revision validation.
- `WorkspaceSessionHost.fs`: sessions, the operation registry, and the per-session settings (threshold and download preference) pushed into the provider when a session opens.
- `VersionControlSettings.fs`: the settings record, its defaults (1 MiB, no download) and the 1 to 100 MiB bound.

`src/Electron/src/Main/IPC/IVersionControlApi.fs` wraps every call: `withSession` registers the operation before the session is opened and sends the started event, `withMutatingSession` also marks the vault busy and refreshes the file tree when the result reports a change, `withService` returns `service_unavailable` when the provider has no such optional service.

## 6. Validation and security rules

Repository paths sent by the renderer are validated in `Mappings.tryRepositoryPath` before any provider call and must be repository relative. Empty values, absolute paths, traversal segments and null characters fail with a `Validation` failure naming the path.

Ref names and revisions are validated the same way. Provider refs are opaque: the sidebar works with names, and requests carry the ref the provider handed out in `listRefs`.

Repository locations are parsed in `ProviderComposition.tryCreateLocation`. Only full `https://`, `ssh://` and `lakefs://` locations are accepted. The Git provider rejects `file://`, `ext::`, `fd::`, protocol overrides such as `-c protocol...`, and SCP style SSH URLs such as `git@git.nfdi4plants.org:group/project.git`. Use `ssh://git@git.nfdi4plants.org/group/project.git` instead.

Credentials are injected per command by the library and are never persisted to repository config. Git runs with `GIT_TERMINAL_PROMPT=0`.

Keep validation in the main process even when the renderer already validates. Renderer validation is for UX, main process validation is the trust boundary.

## 7. Authentication

The Git factory receives a `GitCredentialStrategy` and a `GitIdentityStrategy` built over the DataHub accounts (`DataHubStrategies.fs`). A `RevisionIdentityRequest` names the target host and an optional connection profile. The account is selected by profile when one is present, otherwise by host with the active account preferred, otherwise the active account when no host is known. When no account matches, the strategy returns `None` and the library fails the operation with `identity_missing` or an `Authentication` failure. Tokens are read from `AuthService` at call time, so signing in or switching accounts needs no re-registration.

## 8. Large objects and Git LFS

The library exposes large objects through the object materialization and storage policy services. For Git that is Git LFS.

The threshold for automatic large-object storage and the preference to download large objects are settings of the Electron app, not of the repository. Every session starts from the defaults (1 MiB, no download). The main process keeps the current values in memory per open session and pushes them into the provider when the session opens, so for Git the library keys `versioncontrolservice.lfs.autotrackthresholdmb` and `versioncontrolservice.lfs.materializelargeobjects` in the local repository config only mirror the app values. The sidebar reads them through `getStoragePolicySettings` and changes them through `setStoragePolicySettings`; a change lasts for the open session and is gone when the ARC is opened again. The main process refuses a threshold below 1 MiB or above 100 MiB with `invalid_lfs_threshold`, the code the library uses for the same refusal. lakeFS has no storage policy service, so the values are held by the main process only and the download preference feeds the clone request. Repositories created by earlier Swate versions may still carry `swate.lfs.autotrackthresholdmb` and `swate.lfs.downloadlargefiles` in their local config. Swate does not read them any more.

Renderer state starts with `DownloadLargeFiles = false` until the settings are loaded.

Manual marking follows the DataHub ruleset in `src/Shared/GitLfsRules.fs`:

- `isa.*.xlsx` metadata files must never be tracked with Git LFS. They cannot be marked manually, and a save never turns them into pointers: the composition root hands every provider factory one `RevisionPolicyStrategy` built from the ruleset (`Inline` for metadata files, `LargeObject` for files below a `dataset` folder and for files above 25 MB, `Automatic` otherwise). The Git provider applies it inside its revision transaction and writes the attribute rules it needs; the lakeFS provider accepts the strategy and ignores it. A metadata file that still holds a Git LFS pointer is refused with `inline_content_not_materialized` and the recovery `retry_materialization` until it is downloaded.
- Files below a `dataset` folder must stay tracked and cannot be unmarked.
- Files larger than 25 MB must stay tracked and cannot be unmarked.

The file tree context menu disables blocked toggles, `GitLfsHelper` checks the rules again before calling `setPathStoragePolicy`, and the main process reads the file size itself and rejects a blocked request with `storage_policy_blocked`.

File actions of the explorer: "Download LFS file" calls `materializeObject`, "Free local LFS copy" calls `dematerializeObject`. Both need a clean file. "Clean LFS Cache" (`pruneStorage`) and "Reduce LFS Storage" (`deduplicateStorage`) need a clean working tree. Deduplication can fail on file systems without copy on write support, which is expected and shown to the user.

Clone and update hydrate large objects when `MaterializeAllObjects` (clone) or the materialize setting (update) is on. A cancel during hydration keeps the update and reports a partial result with `retry_materialization`. The sidebar keeps the pulled state and offers the download again.

## 9. Refs and the update workflow

`listRefs` returns local and remote refs with their kind. Switching to a remote ref goes through `preflightSwitchRef` and `switchRef` with the opaque provider ref. `createRef` creates and switches to a new local ref.

`previewUpdate` fetches the target and reports `WouldCreateConflictSession` and `HasDataLossRisk`. The sidebar continues directly when both are false and asks first otherwise. A preview that fails with category `Unsupported` opens the indeterminate confirmation.

`update` reconciles with the target. A conflict returns `PartiallySucceeded` with `conflicts_detected` and the recovery `resolve_conflict_session`, and the status carries the `ActiveConflictSession`. A canceled update returns `Failed` with category `Canceled` and one of the recovery codes `remove_index_lock`, `restore_workspace`, `refresh_workspace`, `inspect_workspace` or `abort_merge`, and the sidebar opens the matching dialog.

`publish` pushes the current ref. It returns `NoOp` when the target is up to date.

## 10. Diff and conflict resolution

The diff page loads `getBaseContent`, `getWordDiff` and the current file content from the vault. The conflict page loads the combined preview of the conflicted item and carries the conflict handle and the workspace version the preview was taken with. Confirming calls `resolveConflict` with the resolved content, then `getStatus`, then `finalizeConflict` when no items remain. A stale handle fails with `refresh_conflict_session` and the page reloads. Abandoning calls `cancelConflict`.

## 11. Busy, progress and cancellation

Mutations run under the vault busy flag so the file watcher does not merge Swate's own writes: `bindWorkspace`, `createRef`, `switchRef`, `createRevision`, `restorePaths`, `update`, `publish`, `resolveConflict`, `finalizeConflict`, `cancelConflict`, `materializeObject`, `dematerializeObject`, `setStoragePolicySettings`, `setPathStoragePolicy`, `pruneStorage`, `deduplicateStorage` and `clearStaleLock`. Nested busy scopes are counted per window, and the flag drops when the outermost scope ends. Read calls, `previewUpdate`, `refreshSynchronization`, the provisioning calls and the dependency calls do not take the flag.

Progress arrives through `versionControlProgress` with float `Completed` and `Total` counters. Clone reports progress to the window that requested it.

Every operation can be canceled with `cancelOperation` and the key from `versionControlOperationStarted`. Operations without a session (clone, initialize, dependency checks) register with an empty session id, which acts as a wildcard on both sides. Cancellation kills the underlying process. The library then restores a clean state where it can and reports what is left through the recovery code.

A stale `.git/index.lock` left by a killed process is removed by `clearStaleLock`, which refuses with `lock_removal_refused` while another operation of the session runs. After clearing, an open merge is abandoned through the normal path.

## 12. Extending the functionality

When adding an operation:

1. Add or reuse DTOs in `VersionControlTypes.fs`. No Git or lakeFS type may appear in a DTO.
2. Add the function to `IVersionControlApi` in `IPCTypes.fs` with a request DTO that carries `OperationId`.
3. Implement it in `src/Electron/src/Main/IPC/IVersionControlApi.fs` through `withSession` or `withMutatingSession`, and map through `Mappings.fs`.
4. Add the wrapper in `Renderer/VersionControlApiClient.fs`.
5. Wire it into `GitWorkflow.GitDependencies` when the sidebar state needs it.
6. Add tests in `tests/Electron.Core` (mappings, session host or IPC over a real repository) and `tests/Electron.Renderer` (workflow).
7. Update this guide.

Provider specific behavior belongs in the library or in the composition root. Do not branch on a provider id anywhere else.

## 13. Requirements and troubleshooting

The library requires Git 2.38 or newer and Git LFS 3.7 or newer. `checkDependencies` reports every component with `Installed`, `Compatible` and a remediation text. The sidebar shows the remediation when a component is missing or too old. Only the Git LFS configuration component can be installed through `installDependency`, which the sidebar offers when an operation fails with category `DependencyMissing`.

`Authentication` or `Authorization` failures on update or publish:

- Confirm an account is signed in for the target host and its token is valid.
- Confirm the remote URL is `https://` or full form `ssh://`.

Clone fails although the repository is public:

- When a token is available, clone runs authenticated and does not retry without it.
- The target must be missing or an empty directory (`target_not_empty` otherwise).

`workspace_unmanaged` after opening a folder:

- The folder is not under version control. The sidebar offers to initialize a repository.

`workspace_ambiguous`:

- More than one provider claims the folder. Remove the state of the provider that is not wanted, or bind the workspace explicitly.

Path rejected:

- Use repository relative paths with `/`. Do not pass absolute paths, `.` or `..` segments, empty values or null characters.
