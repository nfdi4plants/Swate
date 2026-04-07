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

let private toSharedGitFailureKind (kind: GitService.GitFailureKind) =
    match kind with
    | GitService.GitFailureKind.Unauthorized -> GitFailureKind.Unauthorized
    | GitService.GitFailureKind.Forbidden -> GitFailureKind.Forbidden
    | GitService.GitFailureKind.Network -> GitFailureKind.Network
    | GitService.GitFailureKind.Timeout -> GitFailureKind.Timeout
    | GitService.GitFailureKind.Canceled -> GitFailureKind.Canceled
    | GitService.GitFailureKind.LfsInstallRequired -> GitFailureKind.LfsInstallRequired
    | GitService.GitFailureKind.Unknown -> GitFailureKind.Unknown

let private toGitOperationResult
    (successMessage: 'T -> string option)
    (successPath: ('T -> string option) option)
    (result: GitService.GitResult<'T>)
    : Result<GitOperationResult, exn> =
    match result with
    | Ok payload ->
        let path = successPath |> Option.bind (fun projectPath -> projectPath payload)

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
    Conflicted = status.Conflicted
    IsMergeInProgress = status.IsMergeInProgress
    Files =
        status.Files
        |> Array.map (fun file -> {
            Path = file.Path
            Index = file.Index
            WorkingDir = file.WorkingDir
            OriginalPath = file.OriginalPath
        })
}

let private toBranchKind (kind: GitService.GitBranchRefKind) =
    match kind with
    | GitService.GitBranchRefKind.Local -> GitBranchRefKind.Local
    | GitService.GitBranchRefKind.Remote -> GitBranchRefKind.Remote

let private toBranchDto (branch: GitService.GitBranchRefDto) : GitBranchRefDto = {
    RefName = branch.RefName
    DisplayLabel = branch.DisplayLabel
    Kind = toBranchKind branch.Kind
    IsCurrent = branch.IsCurrent
    IsTracking = branch.IsTracking
}

let private toDiffSummaryDto (diff: GitService.GitDiffSummaryDto) : GitDiffSummaryDto = {
    Changed = diff.Changed
    Insertions = diff.Insertions
    Deletions = diff.Deletions
}

let private toLfsSettingsDto (settings: GitService.GitLfsSettingsDto) : GitLfsSettingsDto = {
    AutoTrackThresholdMb = settings.AutoTrackThresholdMb
    DownloadLargeFiles = settings.DownloadLargeFiles
}

let private toDiffViewDataDto (data: GitService.GitDiffViewDataDto) : GitDiffViewDataDto = {
    Path = data.Path
    PreviousContent = data.PreviousContent
    CurrentContent = data.CurrentContent
    WordDiffText = data.WordDiffText
}

let private toMergeConflictViewDataDto (data: GitService.GitMergeConflictViewDataDto) : GitMergeConflictViewDataDto = {
    Path = data.Path
    MergeConflictContent = data.MergeConflictContent
}

let private toGitPageLoadResultDto
    (requestedPath: string)
    (mapPayload: 'TSource -> 'TTarget)
    (operationName: string)
    (result: GitService.GitResult<'TSource>)
    : Result<GitPageLoadResultDto<'TTarget>, exn> =
    match result with
    | Ok payload ->
        Ok(GitPageLoadResultDto.Loaded(mapPayload payload))
    | Error failure ->
        match GitService.tryGetUnsupportedGitContent requestedPath failure with
        | Some unsupported ->
            Ok(GitPageLoadResultDto.Unsupported unsupported)
        | None ->
            Error(exn $"{operationName} failed ({failure.Kind}): {failure.Message}")

let private toConfirmMergeResolutionResult (result: GitService.GitConfirmMergeResolutionResult) : GitConfirmMergeResolutionResult = {
    UpdatedStatus = toStatusDto result.UpdatedStatus
    RemainingConflictedPaths = result.RemainingConflictedPaths
    NextConflictedPath = result.NextConflictedPath
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
            | Ok(_, arcPath) ->
                let! result = GitService.getStatus arcPath

                match result with
                | Ok statusDto -> return Ok(toStatusDto statusDto)
                | Error failure -> return Error(exn $"git status failed ({failure.Kind}): {failure.Message}")
        }
    getGitBranches =
        fun (event: IpcMainEvent) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getBranches arcPath

                match result with
                | Ok branches -> return Ok(branches |> Array.map toBranchDto)
                | Error failure -> return Error(exn $"git branch list failed ({failure.Kind}): {failure.Message}")
        }
    getGitLfsSettings =
        fun (event: IpcMainEvent) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getLfsSettings arcPath

                match result with
                | Ok settings -> return Ok(toLfsSettingsDto settings)
                | Error failure -> return Error(exn $"git lfs settings failed ({failure.Kind}): {failure.Message}")
        }
    getGitDiffSummary =
        fun (event: IpcMainEvent) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getDiffSummary arcPath

                match result with
                | Ok diffDto -> return Ok(toDiffSummaryDto diffDto)
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
                return toGitPageLoadResultDto requestedPath toDiffViewDataDto "git diff view" result
        }
    getGitMergeConflictViewData =
        fun (event: IpcMainEvent) (requestedPath: string) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(_, arcPath) ->
                let! result = GitService.getMergeConflictViewData arcPath requestedPath
                return toGitPageLoadResultDto requestedPath toMergeConflictViewDataDto "git merge conflict view" result
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
                        Path = None
                    }
                | Error message ->
                    Ok {
                        Success = false
                        Message = Some message
                        FailureKind = Some GitFailureKind.Unknown
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
                return toGitOperationResult (fun () -> Some "Fetch completed.") None result
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
                            return toGitOperationResult (fun () -> Some "Pull completed.") None result
                        })
        }
    gitPush =
        fun (event: IpcMainEvent) (request: GitRemoteOperationRequest) -> promise {
            match tryGetVaultAndArcPath event with
            | Error error -> return Error error
            | Ok(vault, arcPath) ->
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
                    request.DownloadLargeFiles
                    progressReporter

            return
                toGitOperationResult
                    (fun _ -> Some "Clone completed.")
                    (Some(fun normalizedPath -> Some normalizedPath))
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
                            return toGitOperationResult (fun () -> Some "Files staged.") None result
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
                            return toGitOperationResult (fun () -> Some "Files unstaged.") None result
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

                return toGitOperationResult (fun () -> Some "Git LFS threshold updated.") None result
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
                                toGitOperationResult (fun () -> Some $"Branch '{request.Name}' created.") None result
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
                            let! result = GitService.checkoutBranch arcPath request.Name
                            do! vault.RefreshFileTree()

                            return
                                toGitOperationResult
                                    (fun () -> Some $"Checked out branch '{request.Name}'.")
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

                            match result with
                            | Ok payload ->
                                do! vault.RefreshFileTree()
                                return Ok(toConfirmMergeResolutionResult payload)
                            | Error failure ->
                                return Error(exn $"confirm merge resolution failed ({failure.Kind}): {failure.Message}")
                        })
        }
}
