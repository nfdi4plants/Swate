/// Thin renderer client over the provider-neutral version control IPC. It turns the
/// transport error into a string and leaves the structured operation result intact.
module Renderer.VersionControlApiClient

open System
open Fable.Core
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.VersionControlTypes

let private api: IVersionControlApi = Api.ipcVersionControlApi

let private call (invoke: unit -> JS.Promise<Result<'T, exn>>) : JS.Promise<Result<'T, string>> = promise {
    let! result = invoke ()
    return result |> Result.mapError _.Message
}

/// A fresh operation id. The renderer picks it so it can cancel the operation before
/// the main process has reported anything back.
let newOperationId () = Guid.NewGuid().ToString()

let private request () : OperationRequestDto = { OperationId = newOperationId () }

let getSessionInfo () =
    call (fun () -> api.getSessionInfo (request ()))

let cloneWorkspace (dto: CloneWorkspaceRequestDto) = call (fun () -> api.cloneWorkspace dto)

let initializeWorkspace (dto: InitializeWorkspaceRequestDto) =
    call (fun () -> api.initializeWorkspace dto)

let bindWorkspace (dto: BindWorkspaceRequestDto) = call (fun () -> api.bindWorkspace dto)

let cancelOperation (key: OperationKeyDto) =
    call (fun () -> api.cancelOperation key)

let checkDependencies () =
    call (fun () -> api.checkDependencies (request ()))

let installDependency (dto: InstallDependencyRequestDto) =
    call (fun () -> api.installDependency dto)

let getStatus () =
    call (fun () -> api.getStatus (request ()))

let listRefs () =
    call (fun () -> api.listRefs (request ()))

let createRef (dto: CreateRefRequestDto) = call (fun () -> api.createRef dto)

let preflightSwitchRef (dto: SwitchRefRequestDto) =
    call (fun () -> api.preflightSwitchRef dto)

let switchRef (dto: SwitchRefRequestDto) = call (fun () -> api.switchRef dto)

let createRevision (dto: CreateRevisionRequestDto) = call (fun () -> api.createRevision dto)

let restorePaths (dto: RestorePathsRequestDto) = call (fun () -> api.restorePaths dto)

let getDiffSummary () =
    call (fun () -> api.getDiffSummary (request ()))

let getTextDiff (dto: ObjectPathRequestDto) = call (fun () -> api.getTextDiff dto)

let getWordDiff (dto: ObjectPathRequestDto) = call (fun () -> api.getWordDiff dto)

let getBaseContent (dto: ObjectPathRequestDto) = call (fun () -> api.getBaseContent dto)

let refreshSynchronization (dto: OperationRequestDto) =
    call (fun () -> api.refreshSynchronization dto)

let previewUpdate (dto: OperationRequestDto) = call (fun () -> api.previewUpdate dto)

let update (dto: UpdateRequestDto) = call (fun () -> api.update dto)

let publish (dto: PublishRequestDto) = call (fun () -> api.publish dto)

let getActiveConflictSession () =
    call (fun () -> api.getActiveConflictSession (request ()))

let resolveConflict (dto: ResolveConflictRequestDto) =
    call (fun () -> api.resolveConflict dto)

let finalizeConflict (dto: FinalizeConflictRequestDto) =
    call (fun () -> api.finalizeConflict dto)

let cancelConflict (dto: CancelConflictRequestDto) = call (fun () -> api.cancelConflict dto)

let listObjects () =
    call (fun () -> api.listObjects (request ()))

let materializeObject (dto: ObjectPathRequestDto) =
    call (fun () -> api.materializeObject dto)

let dematerializeObject (dto: ObjectPathRequestDto) =
    call (fun () -> api.dematerializeObject dto)

let getStoragePolicySettings () =
    call (fun () -> api.getStoragePolicySettings (request ()))

let setStoragePolicySettings (dto: StoragePolicySettingsRequestDto) =
    call (fun () -> api.setStoragePolicySettings dto)

let setPathStoragePolicy (dto: PathStoragePolicyRequestDto) =
    call (fun () -> api.setPathStoragePolicy dto)

let pruneStorage (dto: OperationRequestDto) = call (fun () -> api.pruneStorage dto)

let deduplicateStorage (dto: OperationRequestDto) =
    call (fun () -> api.deduplicateStorage dto)

let getRepositoryWebUrl () =
    call (fun () -> api.getRepositoryWebUrl (request ()))

let clearStaleLock (dto: OperationRequestDto) = call (fun () -> api.clearStaleLock dto)
