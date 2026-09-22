/// Large-object actions of the file explorer over the provider-neutral storage
/// policy and materialization services. The DataHub ruleset (isa.*.xlsx never in
/// large-object storage, dataset files always) is checked here before the provider is
/// asked, so a refused toggle never reaches the repository.
module Renderer.Components.Helper.GitLfsHelper

open Fable.Core
open Swate.Components.Shared
open Swate.Electron.Shared.VersionControlTypes

let private operationId () =
    Renderer.VersionControlApiClient.newOperationId ()

let private toUnitResult = Renderer.Context.GitWorkflow.toUnitResult

/// The size is not known here, so the size rule is left to the context menu, which
/// has it. The path rules are checked for both directions.
let private tryGetToggleBlockedReason (relativePath: string) (markAsLfs: bool) =
    GitLfsRules.tryGetToggleBlockedReason relativePath None markAsLfs

let runToggleLfsMark (relativePath: string) (markAsLfs: bool) : JS.Promise<Result<unit, string>> = promise {
    match tryGetToggleBlockedReason relativePath markAsLfs with
    | Some reason -> return Error reason
    | None ->
        let! result =
            Renderer.VersionControlApiClient.setPathStoragePolicy {
                OperationId = operationId ()
                Path = PathHelpers.normalizeSeparators relativePath
                UseLargeObjectStorage = markAsLfs
            }

        return toUnitResult result
}

let runFreeLocalLfsCopy (relativePath: string) : JS.Promise<Result<unit, string>> = promise {
    let! result =
        Renderer.VersionControlApiClient.dematerializeObject {
            OperationId = operationId ()
            Path = PathHelpers.normalizeSeparators relativePath
            RefreshTree = None
        }

    return toUnitResult result
}

let runDownloadLfsFile (relativePath: string) : JS.Promise<Result<unit, string>> = promise {
    let! result =
        Renderer.VersionControlApiClient.materializeObject {
            OperationId = operationId ()
            Path = PathHelpers.normalizeSeparators relativePath
            RefreshTree = None
        }

    return toUnitResult result
}
