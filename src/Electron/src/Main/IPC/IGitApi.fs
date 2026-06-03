module Main.IPC.IGitApi

open System
open System.Text.RegularExpressions
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Swate.Electron.Shared.GitTypes
open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Fable.Electron.Remoting.Main
open Main
open Main.Git
open Main.Git.GitLfsAdapter

let private versionPattern = Regex(@"(\d+)\.(\d+)(?:\.(\d+))?")

let private isAtLeast (major, minor, patch) (minMajor, minMinor, minPatch) =
    major > minMajor
    || (major = minMajor && minor > minMinor)
    || (major = minMajor && minor = minMinor && patch >= minPatch)

let private hasMinVersion minimum (output: string option) =
    let m = versionPattern.Match(defaultArg output "")

    if m.Success then
        isAtLeast
            (Int32.Parse m.Groups.[1].Value,
             Int32.Parse m.Groups.[2].Value,
             if m.Groups.[3].Success then Int32.Parse m.Groups.[3].Value else 0)
            minimum
    else
        false

let private checkGitVersions () = promise {
    let! gitVersion = tryExecGitText None 5000 [| "--version" |]
    let! gitLfsVersion = tryExecGitText None 5000 [| "lfs"; "--version" |]

    return
        if hasMinVersion (2, 32, 0) gitVersion && hasMinVersion (3, 7, 0) gitLfsVersion then
            Ok()
        else
            Error(exn "Swate requires Git 2.32.0+ and Git LFS 3.7.0+ (`git lfs --version`).")
}

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
        Remoting.createIpc ()
        |> Remoting.withWindow vault.window
        |> Remoting.buildProxySender<IGitProgressRendererApi>

    fun progressEvent ->
        rendererApi.gitProgressUpdate {
            Method = Some progressEvent.method
            Stage = Some progressEvent.stage
            Progress = Some progressEvent.progress
            Processed = Some progressEvent.processed
            Total = Some progressEvent.total
        }

let api (event: IpcMainInvokeEvent) : IGitApi = {
    checkGitVersions = fun () -> checkGitVersions ()
    getGitStatus =
        fun () -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getStatus arcPath

                match result with
                | Ok statusDto -> return Ok statusDto
                | Error failure -> return Error(exn $"git status failed ({failure.Kind}): {failure.Message}")
        }
    getGitBranches =
        fun () -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getBranches arcPath

                match result with
                | Ok branches -> return Ok branches
                | Error failure -> return Error(exn $"git branch list failed ({failure.Kind}): {failure.Message}")
        }
    getGitLfsSettings =
        fun () -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getLfsSettings arcPath

                match result with
                | Ok settings -> return Ok settings
                | Error failure -> return Error(exn $"git lfs settings failed ({failure.Kind}): {failure.Message}")
        }
    previewGitPull =
        fun (request: GitRemoteOperationRequest) -> promise {
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
        fun () -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getDiffSummary arcPath

                match result with
                | Ok diffDto -> return Ok diffDto
                | Error failure -> return Error(exn $"git diff summary failed ({failure.Kind}): {failure.Message}")
        }
    getGitWordDiff =
        fun (request: GitPathspecRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getWordDiff arcPath request.Pathspecs

                match result with
                | Ok diffText -> return Ok diffText
                | Error failure -> return Error(exn $"git word diff failed ({failure.Kind}): {failure.Message}")
        }
    getGitDiffViewData =
        fun (requestedPath: string) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getDiffViewData arcPath requestedPath
                return toGitPageLoadResultDto requestedPath "git diff view" result
        }
    getGitMergeConflictViewData =
        fun (requestedPath: string) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getMergeConflictViewData arcPath requestedPath
                return toGitPageLoadResultDto requestedPath "git merge conflict view" result
        }
    installGitLfs =
        fun () -> promise {
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
        fun (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                let! result = GitService.fetch arcPath request.Remote request.Branch (Some progressReporter)
                return toGitOperationResult (fun () -> Some "Fetch completed.") None None result
        }
    gitPull =
        fun (request: GitRemoteOperationRequest) -> promise {
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
        fun (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                let progressReporter = createGitProgressReporter vault

                let! result = GitService.push arcPath request.Remote request.Branch (Some progressReporter)
                return toGitOperationResult (fun () -> Some "Push completed.") None None result
        }
    gitInitRepository =
        fun (targetPath: string) -> promise {
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
        fun (request: GitRemoteConfigRequest) -> promise {
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
        fun (request: GitCloneRepositoryRequest) -> promise {
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
        fun (request: GitPathspecRequest) -> promise {
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
        fun (request: GitPathspecRequest) -> promise {
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
    gitDiscardPaths =
        fun (request: GitPathspecRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.discardPaths arcPath request.Pathspecs
                            if Result.isOk result then
                                do! vault.RefreshFileTree()
                            return toGitOperationResult (fun () -> Some "Files discarded.") None None result
                        })
        }
    gitCommit =
        fun (request: GitCommitRequest) -> promise {
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
        fun (settings: GitLfsSettingsDto) -> promise {
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
    gitLfsPrune =
        fun () -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.pruneLfsCache arcPath
                            return toGitOperationResult (fun _ -> Some "Hidden Git LFS cache cleaned.") None None result
                        })
        }
    gitLfsDedup =
        fun () -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.dedupLfsStorage arcPath
                            return toGitOperationResult (fun _ -> Some "Git LFS deduplication completed.") None None result
                        })
        }
    gitLfsDownloadFile =
        fun (request: GitLfsDownloadFileRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.downloadLfsFile arcPath request.Path
                            if Result.isOk result then
                                do! vault.RefreshFileTree()
                            return toGitOperationResult (fun () -> Some $"Downloaded LFS file '{request.Path}'.") None None result
                        })
        }
    gitLfsFreeLocalCopy =
        fun (request: GitLfsFreeLocalCopyRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
                return!
                    withBusyWriting
                        vault
                        (fun () -> promise {
                            let! result = GitService.freeLocalLfsCopy arcPath request.Path
                            if Result.isOk result then
                                do! vault.RefreshFileTree()
                            return toGitOperationResult (fun () -> Some $"Freed local LFS copy for '{request.Path}'.") None None result
                        })
        }
    createBranch =
        fun (request: GitCreateBranchRequest) -> promise {
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
        fun (request: GitCheckoutBranchRequest) -> promise {
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
        fun (request: GitConfirmMergeResolutionRequest) -> promise {
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
