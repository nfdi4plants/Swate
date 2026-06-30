module Renderer.GitApiClient

open Fable.Core
open Swate.Electron.Shared.GitTypes

let private gitApi = Api.ipcGitApi

let private mapExnResult (result: Result<'T, exn>) = result |> Result.mapError _.Message

let private callIpc (rawCall: unit -> JS.Promise<Result<'T, exn>>) : JS.Promise<Result<'T, string>> = promise {
    let! result = rawCall ()
    return mapExnResult result
}

let private callIpcWith (arg: 'A) (rawCall: 'A -> JS.Promise<Result<'T, exn>>) : JS.Promise<Result<'T, string>> = promise {
    let! result = rawCall arg
    return mapExnResult result
}

let checkGitVersions () =
    callIpc (fun () -> gitApi.checkGitVersions ())

let getGitStatus () =
    callIpc (fun () -> gitApi.getGitStatus ())

let getGitBranches () =
    callIpc (fun () -> gitApi.getGitBranches ())

let getOriginRepositoryWebUrl () =
    callIpc (fun () -> gitApi.getOriginRepositoryWebUrl ())

let getGitLfsSettings () =
    callIpc (fun () -> gitApi.getGitLfsSettings ())

let getGitDiffViewData (requestedPath: string) =
    callIpcWith requestedPath (gitApi.getGitDiffViewData)

let getGitMergeConflictViewData (requestedPath: string) =
    callIpcWith requestedPath (gitApi.getGitMergeConflictViewData)

let installGitLfs () =
    callIpc (fun () -> gitApi.installGitLfs ())

let gitFetch (request: GitRemoteOperationRequest) = callIpcWith request (gitApi.gitFetch)

let gitPull (request: GitRemoteOperationRequest) = callIpcWith request (gitApi.gitPull)

let previewGitPull (request: GitRemoteOperationRequest) =
    callIpcWith request (gitApi.previewGitPull)

let gitPush (request: GitRemoteOperationRequest) = callIpcWith request (gitApi.gitPush)

let gitCancelPush () =
    callIpc (fun () -> gitApi.gitCancelPush ())

let gitInitRepository (targetPath: string) = promise {
    let! result = gitApi.gitInitRepository targetPath

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
    callIpcWith request (gitApi.gitAddRemote)

let gitCloneRepository (request: GitCloneRepositoryRequest) =
    callIpcWith request (gitApi.gitCloneRepository)

let createBranch (request: GitCreateBranchRequest) =
    callIpcWith request (gitApi.createBranch)

let checkoutBranch (request: GitCheckoutBranchRequest) =
    callIpcWith request (gitApi.checkoutBranch)

let gitStagePaths (request: GitPathspecRequest) =
    callIpcWith request (gitApi.gitStagePaths)

let gitUnstagePaths (request: GitPathspecRequest) =
    callIpcWith request (gitApi.gitUnstagePaths)

let gitDiscardPaths (request: GitPathspecRequest) =
    callIpcWith request (gitApi.gitDiscardPaths)

let gitCommit (request: GitCommitRequest) = callIpcWith request (gitApi.gitCommit)

let setGitLfsSettings (settings: GitLfsSettingsDto) =
    callIpcWith settings (gitApi.setGitLfsSettings)

let confirmGitMergeResolution (request: GitConfirmMergeResolutionRequest) =
    callIpcWith request (gitApi.confirmGitMergeResolution)

let gitLfsPrune () =
    callIpc (fun () -> gitApi.gitLfsPrune ())

let gitLfsDedup () =
    callIpc (fun () -> gitApi.gitLfsDedup ())

let gitLfsFreeLocalCopy (request: GitLfsFileRequest) =
    callIpcWith request (gitApi.gitLfsFreeLocalCopy)

let gitLfsDownloadFile (request: GitLfsFileRequest) =
    callIpcWith request (gitApi.gitLfsDownloadFile)
