module Renderer.GitApiClient

open Fable.Core
open Swate.Electron.Shared.GitTypes

let private gitApi = Api.ipcGitApi

let private getGitStatusRaw () : JS.Promise<Result<GitStatusDto, exn>> =
    gitApi.getGitStatus (unbox null)

let private getGitBranchesRaw () : JS.Promise<Result<GitBranchRefDto[], exn>> =
    gitApi.getGitBranches (unbox null)

let private getGitLfsSettingsRaw () : JS.Promise<Result<GitLfsSettingsDto, exn>> =
    gitApi.getGitLfsSettings (unbox null)
let private getGitDiffViewDataRaw
    (requestedPath: string)
    : JS.Promise<Result<GitPageLoadResultDto<GitDiffViewDataDto>, exn>> =
    gitApi.getGitDiffViewData (unbox null) requestedPath

let private getGitMergeConflictViewDataRaw
    (requestedPath: string)
    : JS.Promise<Result<GitPageLoadResultDto<GitMergeConflictViewDataDto>, exn>> =
    gitApi.getGitMergeConflictViewData (unbox null) requestedPath

let private installGitLfsRaw () : JS.Promise<Result<GitOperationResult, exn>> =
    gitApi.installGitLfs (unbox null)

let private gitFetchRaw (request: GitRemoteOperationRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    gitApi.gitFetch (unbox null) request

let private gitPullRaw (request: GitRemoteOperationRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    gitApi.gitPull (unbox null) request

let private gitPushRaw (request: GitRemoteOperationRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    gitApi.gitPush (unbox null) request

let private createBranchRaw (request: GitCreateBranchRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    gitApi.createBranch (unbox null) request

let private checkoutBranchRaw (request: GitCheckoutBranchRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    gitApi.checkoutBranch (unbox null) request

let private gitStagePathsRaw (request: GitPathspecRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    gitApi.gitStagePaths (unbox null) request

let private gitUnstagePathsRaw (request: GitPathspecRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    gitApi.gitUnstagePaths (unbox null) request

let private gitCommitRaw (request: GitCommitRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    gitApi.gitCommit (unbox null) request

let private setGitLfsSettingsRaw (settings: GitLfsSettingsDto) : JS.Promise<Result<GitOperationResult, exn>> =
    gitApi.setGitLfsSettings (unbox null) settings

let private confirmGitMergeResolutionRaw
    (request: GitConfirmMergeResolutionRequest)
    : JS.Promise<Result<GitConfirmMergeResolutionResult, exn>> =
    gitApi.confirmGitMergeResolution (unbox null) request

let private mapExnResult (result: Result<'T, exn>) =
    result |> Result.mapError _.Message

let getGitStatus () = promise {
    let! result = getGitStatusRaw ()
    return mapExnResult result
}

let getGitBranches () = promise {
    let! result = getGitBranchesRaw ()
    return mapExnResult result
}

let getGitLfsSettings () = promise {
    let! result = getGitLfsSettingsRaw ()
    return mapExnResult result
}

let getGitDiffViewData (requestedPath: string) = promise {
    let! result = getGitDiffViewDataRaw requestedPath
    return mapExnResult result
}

let getGitMergeConflictViewData (requestedPath: string) = promise {
    let! result = getGitMergeConflictViewDataRaw requestedPath
    return mapExnResult result
}

let installGitLfs () = promise {
    let! result = installGitLfsRaw ()
    return mapExnResult result
}

let gitFetch (request: GitRemoteOperationRequest) = promise {
    let! result = gitFetchRaw request
    return mapExnResult result
}

let gitPull (request: GitRemoteOperationRequest) = promise {
    let! result = gitPullRaw request
    return mapExnResult result
}

let gitPush (request: GitRemoteOperationRequest) = promise {
    let! result = gitPushRaw request
    return mapExnResult result
}

let createBranch (request: GitCreateBranchRequest) = promise {
    let! result = createBranchRaw request
    return mapExnResult result
}

let checkoutBranch (request: GitCheckoutBranchRequest) = promise {
    let! result = checkoutBranchRaw request
    return mapExnResult result
}

let gitStagePaths (request: GitPathspecRequest) = promise {
    let! result = gitStagePathsRaw request
    return mapExnResult result
}

let gitUnstagePaths (request: GitPathspecRequest) = promise {
    let! result = gitUnstagePathsRaw request
    return mapExnResult result
}

let gitCommit (request: GitCommitRequest) = promise {
    let! result = gitCommitRaw request
    return mapExnResult result
}

let setGitLfsSettings (settings: GitLfsSettingsDto) = promise {
    let! result = setGitLfsSettingsRaw settings
    return mapExnResult result
}

let confirmGitMergeResolution (request: GitConfirmMergeResolutionRequest) = promise {
    let! result = confirmGitMergeResolutionRaw request
    return mapExnResult result
}
