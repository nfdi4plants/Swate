module Renderer.Components.Helper.GitLfsHelper

open System
open Fable.Core
open Swate.Electron.Shared.GitTypes

let runToggleLfsMark (relativePath: string) (markAsLfs: bool) : JS.Promise<Result<unit, string>> = promise {
    let request: GitLfsRequest = {
        RequestId = Guid.NewGuid().ToString()
        RepoPath = ""
        Command =
            if markAsLfs then
                GitLfsCommand.Track
            else
                GitLfsCommand.Untrack
        FilePath = Some relativePath
        TimeoutMs = Some 10000
    }

    let! result = Api.ipcArcVaultApi.runGitLfs request

    return
        match result with
        | Ok _ -> Ok()
        | Error exn -> Error exn.Message
}

let runFreeLocalLfsCopy (relativePath: string) : JS.Promise<Result<unit, string>> = promise {
    let request: GitLfsFileRequest = { Path = relativePath }

    let! result = Renderer.GitApiClient.gitLfsFreeLocalCopy request

    return
        match result with
        | Ok operation when operation.Success -> Ok()
        | Ok operation -> Error(operation.Message |> Option.defaultValue "Git LFS cleanup failed.")
        | Error message -> Error message
}

let runDownloadLfsFile (relativePath: string) = promise {

    let request: GitLfsFileRequest = { Path = relativePath }

    let! result = Renderer.GitApiClient.gitLfsDownloadFile request

    return
        match result with
        | Ok operation when operation.Success -> Ok()
        | Ok operation -> Error(operation.Message |> Option.defaultValue "Git LFS download failed.")
        | Error message -> Error message
}
