module Main.IPC.IGitApi

open System
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.GitTypes
open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Fable.Electron.Remoting.Main
open Main
open Main.Git

let private toGitOperationResult
    (successMessage: 'T -> string option)
    (successPath: ('T -> string option) option)
    (successWarning: ('T -> GitService.GitFailure option) option)
    (result: GitService.GitResult<'T>)
    : Result<GitOperationResult, exn> =
    match result with
    | Ok payload ->
        let path = successPath |> Option.bind (fun projectPath -> projectPath payload)
        let warning = successWarning |> Option.bind (fun warningSelector -> warningSelector payload)

        Ok {
            Success = true
            Message = successMessage payload
            FailureKind = None
            WarningMessage = warning |> Option.map _.Message
            WarningKind = warning |> Option.map _.Kind
            Path = path
        }
    | Error failure ->
        Ok {
            Success = false
            Message = Some failure.Message
            FailureKind = Some failure.Kind
            WarningMessage = None
            WarningKind = None
            Path = None
        }

let private toGitPageLoadResultDto
    (requestedPath: string)
    (operationName: string)
    (result: GitService.GitResult<'T>)
    : Result<GitPageLoadResultDto<'T>, exn> =
    match result with
    | Ok payload ->
        Ok(GitPageLoadResultDto.Loaded payload)
    | Error failure ->
        match GitService.tryGetUnsupportedGitContent requestedPath failure with
        | Some unsupported ->
            Ok(GitPageLoadResultDto.Unsupported unsupported)
        | None ->
            Error(exn $"{operationName} failed ({failure.Kind}): {failure.Message}")

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
            | Ok(_, arcPath) ->
                let! result = GitService.getStatus arcPath

                match result with
                | Ok statusDto -> return Ok statusDto
                | Error failure -> return Error(exn $"git status failed ({failure.Kind}): {failure.Message}")
        }
    getGitBranches =
        fun (event: IpcMainEvent) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getBranches arcPath

                match result with
                | Ok branches -> return Ok branches
                | Error failure -> return Error(exn $"git branch list failed ({failure.Kind}): {failure.Message}")
        }
    getGitLfsSettings =
        fun (event: IpcMainEvent) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getLfsSettings arcPath

                match result with
                | Ok settings -> return Ok settings
                | Error failure -> return Error(exn $"git lfs settings failed ({failure.Kind}): {failure.Message}")
        }
    previewGitPull =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault
                let! result = GitService.previewPull arcPath request.Remote request.Branch (Some progressReporter)

                match result with
                | Ok preview -> return Ok preview
                | Error failure -> return Error(exn $"git pull preview failed ({failure.Kind}): {failure.Message}")
        }
    getGitDiffSummary =
        fun (event: IpcMainEvent) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getDiffSummary arcPath

                match result with
                | Ok diffDto -> return Ok diffDto
                | Error failure -> return Error(exn $"git diff summary failed ({failure.Kind}): {failure.Message}")
        }
    getGitWordDiff =
        fun (event: IpcMainEvent) (request: GitPathspecRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getWordDiff arcPath request.Pathspecs

                match result with
                | Ok diffText -> return Ok diffText
                | Error failure -> return Error(exn $"git word diff failed ({failure.Kind}): {failure.Message}")
        }
    getGitDiffViewData =
        fun (event: IpcMainEvent) (requestedPath: string) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getDiffViewData arcPath requestedPath
                return toGitPageLoadResultDto requestedPath "git diff view" result
        }
    getGitMergeConflictViewData =
        fun (event: IpcMainEvent) (requestedPath: string) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getMergeConflictViewData arcPath requestedPath
                return toGitPageLoadResultDto requestedPath "git merge conflict view" result
        }
    installGitLfs =
        fun (_event: IpcMainEvent) -> promise {
            let! result = GitLfsService.installSystem ()

            return
                match result with
                | Ok () ->
                    Ok {
                        Success = true
                        Message = Some "Git LFS installed."
                        FailureKind = None
                        WarningMessage = None
                        WarningKind = None
                        Path = None
                    }
                | Error message ->
                    Ok {
                        Success = false
                        Message = Some message
                        FailureKind = Some GitFailureKind.Unknown
                        WarningMessage = None
                        WarningKind = None
                        Path = None
                    }
        }
    gitFetch =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                let! result = GitService.fetch arcPath request.Remote request.Branch (Some progressReporter)
                return toGitOperationResult (fun () -> Some "Fetch completed.") None None result
        }
    gitPull =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.pull arcPath request.Remote request.Branch (Some progressReporter)
                            do! vault.RefreshFileTree()
                            return
                                toGitOperationResult
                                    (fun _ -> Some "Pull completed.")
                                    None
                                    (Some(fun (payload: GitService.GitPullResult) -> payload.Warning))
                                    result
                        })
        }
    gitPush =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                let! result = GitService.push arcPath request.Remote request.Branch (Some progressReporter)
                return toGitOperationResult (fun () -> Some "Push completed.") None None result
        }
    gitInitRepository =
        fun (_event: IpcMainEvent) (targetPath: string) -> promise {
            // Init provisioning is path-driven only; no vault/window context is required.
            let! result = GitProvisioningService.initRepository targetPath

            return
                toGitOperationResult
                    (fun _ -> Some "Repository initialized.")
                    (Some(fun normalizedPath -> Some normalizedPath))
                    None
                    result
        }
    gitAddRemote =
        fun (event: IpcMainEvent) (request: GitRemoteConfigRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.addRemote arcPath request.RemoteName request.RemoteUrl

                return
                    toGitOperationResult
                        (fun () -> Some $"Remote '{request.RemoteName}' configured.")
                        None
                        None
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
                    request.DownloadLargeFiles
                    progressReporter

            return
                toGitOperationResult
                    (fun _ -> Some "Clone completed.")
                    (Some(fun normalizedPath -> Some normalizedPath))
                    None
                    result
        }
    gitStagePaths =
        fun (event: IpcMainEvent) (request: GitPathspecRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.stagePaths arcPath request.Pathspecs
                            if Result.isOk result then
                                do! vault.RefreshFileTree()
                            return toGitOperationResult (fun () -> Some "Files staged.") None None result
                        })
        }
    gitUnstagePaths =
        fun (event: IpcMainEvent) (request: GitPathspecRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.unstagePaths arcPath request.Pathspecs
                            return toGitOperationResult (fun () -> Some "Files unstaged.") None None result
                        })
        }
    gitCommit =
        fun (event: IpcMainEvent) (request: GitCommitRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.commit arcPath request.Message

                            return
                                toGitOperationResult
                                    (fun commitHash -> Some $"Commit completed ({commitHash}).")
                                    None
                                    None
                                    result
                        })
        }
    setGitLfsSettings =
        fun (event: IpcMainEvent) (settings: GitLfsSettingsDto) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result =
                    GitService.setLfsSettings
                        arcPath
                        {
                            AutoTrackThresholdMb = settings.AutoTrackThresholdMb
                            DownloadLargeFiles = settings.DownloadLargeFiles
                        }

                return toGitOperationResult (fun () -> Some "Git LFS settings updated.") None None result
        }
    createBranch =
        fun (event: IpcMainEvent) (request: GitCreateBranchRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.createBranch arcPath request.Name request.StartPoint
                            do! vault.RefreshFileTree()

                            return
                                toGitOperationResult (fun () -> Some $"Branch '{request.Name}' created.") None None result
                        })
        }
    checkoutBranch =
        fun (event: IpcMainEvent) (request: GitCheckoutBranchRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.checkoutBranch arcPath request
                            do! vault.RefreshFileTree()

                            return
                                toGitOperationResult
                                    (fun () -> Some $"Checked out branch '{request.Name}'.")
                                    None
                                    None
                                    result
                        })
        }
    confirmGitMergeResolution =
        fun (event: IpcMainEvent) (request: GitConfirmMergeResolutionRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result =
                                GitService.confirmMergeResolution
                                    arcPath
                                    request.Path
                                    request.ExpectedConflictContent
                                    request.ResolvedContent
                                    request.AutoCommit

                            match result with
                            | Ok payload ->
                                do! vault.RefreshFileTree()
                                return Ok payload
                            | Error failure ->
                                return Error(exn $"confirm merge resolution failed ({failure.Kind}): {failure.Message}")
                        })
        }
}
