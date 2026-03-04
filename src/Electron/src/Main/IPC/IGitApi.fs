module Main.IPC.IGitApi

open System
open Swate.Electron.Shared.IPCTypes
open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Fable.Electron.Remoting.Main
open Main
open Main.Git

let private tryGetVaultAndArcPath (event: IpcMainEvent) =
    let windowId = windowIdFromIpcEvent event

    match ARC_VAULTS.TryGetVault(windowId) with
    | None -> Error(exn $"The ARC for window id {windowId} should exist")
    | Some vault ->
        match vault.path with
        | Some arcPath -> Ok(vault, arcPath)
        | None -> Error(exn "ARC is not loaded.")

let private withBusyWriting (vault: ArcVault) (operation: unit -> JS.Promise<Result<'T, exn>>) : JS.Promise<Result<'T, exn>> =
    promise {
        vault.isBusyWriting <- true

        try
            return! operation ()
        finally
            vault.isBusyWriting <- false
    }

let private toSharedGitFailureKind (kind: GitService.GitFailureKind) =
    match kind with
    | GitService.GitFailureKind.Unauthorized -> GitFailureKind.Unauthorized
    | GitService.GitFailureKind.Forbidden -> GitFailureKind.Forbidden
    | GitService.GitFailureKind.Network -> GitFailureKind.Network
    | GitService.GitFailureKind.Timeout -> GitFailureKind.Timeout
    | GitService.GitFailureKind.Canceled -> GitFailureKind.Canceled
    | GitService.GitFailureKind.Unknown -> GitFailureKind.Unknown

let private toGitOperationResult
    (successMessage: 'T -> string option)
    (successPath: ('T -> string option) option)
    (result: GitService.GitResult<'T>)
    : Result<GitOperationResult, exn> =
    match result with
    | Ok payload ->
        let path =
            successPath
            |> Option.bind (fun projectPath -> projectPath payload)

        Ok {
            Success = true
            Message = successMessage payload
            FailureKind = None
            Path = path
        }
    | Error failure ->
        Ok {
            Success = false
            Message = Some failure.Message
            FailureKind = Some(toSharedGitFailureKind failure.Kind)
            Path = None
        }

let private toStatusDto (status: GitService.GitStatusDto) : GitStatusDto = {
    Current = status.Current
    Tracking = status.Tracking
    Ahead = status.Ahead
    Behind = status.Behind
    IsClean = status.IsClean
    Files =
        status.Files
        |> Array.map (fun file -> {
            Path = file.Path
            Index = file.Index
            WorkingDir = file.WorkingDir
            OriginalPath = file.OriginalPath
        })
}

let private toDiffSummaryDto (diff: GitService.GitDiffSummaryDto) : GitDiffSummaryDto = {
    Changed = diff.Changed
    Insertions = diff.Insertions
    Deletions = diff.Deletions
}

let private createGitProgressReporter (vault: ArcVault) : GitService.GitProgressCallback =
    let rendererApi =
        Remoting.init
        |> Remoting.withWindow vault.window
        |> Remoting.buildClient<IMainUpdateRendererApi>

    fun progressEvent ->
        rendererApi.gitProgressUpdate {
            Method = Some progressEvent.method
            Stage = Some progressEvent.stage
            Progress = Some progressEvent.progress
            Processed = Some progressEvent.processed
            Total = Some progressEvent.total
        }

let api: IGitApi = {
    getGitStatus =
        fun (event: IpcMainEvent) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (_, arcPath) ->
                let! result = GitService.getStatus arcPath

                match result with
                | Ok statusDto -> return Ok(toStatusDto statusDto)
                | Error failure ->
                    return Error(exn $"git status failed ({failure.Kind}): {failure.Message}")
        }
    getGitDiffSummary =
        fun (event: IpcMainEvent) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (_, arcPath) ->
                let! result = GitService.getDiffSummary arcPath

                match result with
                | Ok diffDto -> return Ok(toDiffSummaryDto diffDto)
                | Error failure ->
                    return Error(exn $"git diff summary failed ({failure.Kind}): {failure.Message}")
        }
    gitFetch =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                let! result = GitService.fetch arcPath request.Remote request.Branch (Some progressReporter)
                return toGitOperationResult (fun () -> Some "Fetch completed.") None result
        }
    gitPull =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.pull arcPath request.Remote request.Branch (Some progressReporter)
                        return toGitOperationResult (fun () -> Some "Pull completed.") None result
                    })
        }
    gitPush =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                let! result = GitService.push arcPath request.Remote request.Branch (Some progressReporter)
                return toGitOperationResult (fun () -> Some "Push completed.") None result
        }
    gitInitRepository =
        fun (_event: IpcMainEvent) (targetPath: string) -> promise {
            // Init provisioning is path-driven only; no vault/window context is required.
            let! result = GitProvisioningService.initRepository targetPath

            return
                toGitOperationResult
                    (fun _ -> Some "Repository initialized.")
                    (Some(fun normalizedPath -> Some normalizedPath))
                    result
        }
    gitCloneRepository =
        fun (event: IpcMainEvent) (request: GitCloneRepositoryRequest) -> promise {
            let progressReporter =
                ARC_VAULTS.TryGetVault(windowIdFromIpcEvent event)
                |> Option.map createGitProgressReporter

            let! result =
                GitProvisioningService.cloneRepository
                    request.RemoteUrl
                    request.TargetPath
                    request.Branch
                    progressReporter

            return toGitOperationResult (fun _ -> Some "Clone completed.") (Some(fun normalizedPath -> Some normalizedPath)) result
        }
    gitStagePaths =
        fun (event: IpcMainEvent) (request: GitPathspecRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.stagePaths arcPath request.Pathspecs
                        return toGitOperationResult (fun () -> Some "Files staged.") None result
                    })
        }
    gitUnstagePaths =
        fun (event: IpcMainEvent) (request: GitPathspecRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.unstagePaths arcPath request.Pathspecs
                        return toGitOperationResult (fun () -> Some "Files unstaged.") None result
                    })
        }
    gitCommit =
        fun (event: IpcMainEvent) (request: GitCommitRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.commit arcPath request.Message
                        return toGitOperationResult (fun commitHash -> Some $"Commit completed ({commitHash}).") None result
                    })
        }
    createBranch =
        fun (event: IpcMainEvent) (request: GitCreateBranchRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.createBranch arcPath request.Name request.StartPoint
                        return toGitOperationResult (fun () -> Some $"Branch '{request.Name}' created.") None result
                    })
        }
    checkoutBranch =
        fun (event: IpcMainEvent) (request: GitCheckoutBranchRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok (vault, arcPath) ->
                return!
                    withBusyWriting vault (fun () -> promise {
                        let! result = GitService.checkoutBranch arcPath request.Name
                        return toGitOperationResult (fun () -> Some $"Checked out branch '{request.Name}'.") None result
                    })
        }
}

