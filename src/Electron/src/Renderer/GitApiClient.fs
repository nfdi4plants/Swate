module Renderer.GitApiClient

open Fable.Core
open Swate.Electron.Shared.GitTypes

let private gitApi = Api.ipcGitApi

let private mapExnResult (result: Result<'T, exn>) =
    result |> Result.mapError _.Message

let private callIpc (rawCall: unit -> JS.Promise<Result<'T, exn>>) : JS.Promise<Result<'T, string>> =
    promise {
        let! result = rawCall ()
        return mapExnResult result
    }

let private callIpcWith
    (arg: 'A)
    (rawCall: 'A -> JS.Promise<Result<'T, exn>>)
    : JS.Promise<Result<'T, string>> =
    promise {
        let! result = rawCall arg
        return mapExnResult result
    }

let getGitStatus () =
    callIpc (fun () -> gitApi.getGitStatus (unbox null))

let getGitBranches () =
    callIpc (fun () -> gitApi.getGitBranches (unbox null))

let getGitLfsSettings () =
    callIpc (fun () -> gitApi.getGitLfsSettings (unbox null))

let getGitDiffViewData (requestedPath: string) =
    callIpcWith requestedPath (gitApi.getGitDiffViewData (unbox null))

let getGitMergeConflictViewData (requestedPath: string) =
    callIpcWith requestedPath (gitApi.getGitMergeConflictViewData (unbox null))

let installGitLfs () =
    callIpc (fun () -> gitApi.installGitLfs (unbox null))

let gitFetch (request: GitRemoteOperationRequest) =
    callIpcWith request (gitApi.gitFetch (unbox null))

let gitPull (request: GitRemoteOperationRequest) =
    callIpcWith request (gitApi.gitPull (unbox null))

let previewGitPull (request: GitRemoteOperationRequest) =
    callIpcWith request (gitApi.previewGitPull (unbox null))

let gitPush (request: GitRemoteOperationRequest) =
    callIpcWith request (gitApi.gitPush (unbox null))

let gitInitRepository (targetPath: string) =
    promise {
        let! result = gitApi.gitInitRepository (unbox null) targetPath

        return
            result
            |> mapExnResult
            |> Result.bind (fun operation ->
                if operation.Success then
                    Ok(operation.Path |> Option.defaultValue targetPath)
                else
                    Error(operation.Message |> Option.defaultValue "Git repository initialization failed.")
            )
    }

let gitAddRemote (request: GitRemoteConfigRequest) =
    callIpcWith request (gitApi.gitAddRemote (unbox null))

let gitCloneRepository (request: GitCloneRepositoryRequest) =
    callIpcWith request (gitApi.gitCloneRepository (unbox null))

let createBranch (request: GitCreateBranchRequest) =
    callIpcWith request (gitApi.createBranch (unbox null))

let checkoutBranch (request: GitCheckoutBranchRequest) =
    callIpcWith request (gitApi.checkoutBranch (unbox null))

let gitStagePaths (request: GitPathspecRequest) =
    callIpcWith request (gitApi.gitStagePaths (unbox null))

let gitUnstagePaths (request: GitPathspecRequest) =
    callIpcWith request (gitApi.gitUnstagePaths (unbox null))

let gitCommit (request: GitCommitRequest) =
    callIpcWith request (gitApi.gitCommit (unbox null))

let setGitLfsSettings (settings: GitLfsSettingsDto) =
    callIpcWith settings (gitApi.setGitLfsSettings (unbox null))

let confirmGitMergeResolution (request: GitConfirmMergeResolutionRequest) =
    callIpcWith request (gitApi.confirmGitMergeResolution (unbox null))
