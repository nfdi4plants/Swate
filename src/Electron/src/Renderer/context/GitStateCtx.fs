module Renderer.Context.GitStateCtx

open System
open Fable.Core
open Fable.Electron.Remoting.Renderer
open Feliz

open Renderer.Types
open Swate.Components.GitSidebarTypes
open Swate.Electron.Shared.GitTypes
open Swate.Electron.Shared.IPCTypes

type GitState = {
    Status: GitSidebarStatus
    ChangedFiles: GitSidebarChange[]
    BranchOptions: GitSidebarBranchOption[]
    CurrentProgress: GitSidebarProgress option
    SelectedChangePath: string option
    BusyNotice: string option
    ErrorNotice: string option
} with

    static member Empty = {
        Status = {
            CurrentBranch = None
            TrackingBranch = None
            Ahead = 0
            Behind = 0
            IsClean = true
            IsMergeInProgress = false
        }
        ChangedFiles = [||]
        BranchOptions = [||]
        CurrentProgress = None
        SelectedChangePath = None
        BusyNotice = None
        ErrorNotice = None
    }

type GitStateController = {
    state: GitState
    refresh: unit -> JS.Promise<Result<unit, string>>
    fetch: unit -> JS.Promise<Result<unit, string>>
    pull: unit -> JS.Promise<Result<unit, string>>
    push: unit -> JS.Promise<Result<unit, string>>
    sync: unit -> JS.Promise<Result<unit, string>>
    createBranch: GitSidebarCreateBranchRequest -> JS.Promise<Result<unit, string>>
    selectChange: GitSidebarChange -> JS.Promise<Result<unit, string>>
    confirmMergeResolution: GitConfirmMergeResolutionRequest -> JS.Promise<Result<unit, string>>
}

let private ipcGitApiDynamic: obj = unbox Api.ipcGitApi

[<Emit("$0[$1]()")>]
let private invokeGitApiWithoutPayload<'T> (api: obj) (methodName: string) : 'T = jsNative

[<Emit("$0[$1](undefined, $2)")>]
let private invokeGitApiWithPayload<'TArg, 'T> (api: obj) (methodName: string) (arg: 'TArg) : 'T = jsNative

let private getGitStatus () : JS.Promise<Result<GitStatusDto, exn>> =
    invokeGitApiWithoutPayload ipcGitApiDynamic "getGitStatus"

let private getGitBranches () : JS.Promise<Result<GitBranchRefDto[], exn>> =
    invokeGitApiWithoutPayload ipcGitApiDynamic "getGitBranches"

let private getGitDiffViewData (requestedPath: string) : JS.Promise<Result<GitDiffViewDataDto, exn>> =
    invokeGitApiWithPayload ipcGitApiDynamic "getGitDiffViewData" requestedPath

let private getGitMergeConflictViewData (requestedPath: string) : JS.Promise<Result<GitMergeConflictViewDataDto, exn>> =
    invokeGitApiWithPayload ipcGitApiDynamic "getGitMergeConflictViewData" requestedPath

let private gitFetch (request: GitRemoteOperationRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    invokeGitApiWithPayload ipcGitApiDynamic "gitFetch" request

let private gitPull (request: GitRemoteOperationRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    invokeGitApiWithPayload ipcGitApiDynamic "gitPull" request

let private gitPush (request: GitRemoteOperationRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    invokeGitApiWithPayload ipcGitApiDynamic "gitPush" request

let private createBranch (request: GitCreateBranchRequest) : JS.Promise<Result<GitOperationResult, exn>> =
    invokeGitApiWithPayload ipcGitApiDynamic "createBranch" request

let private confirmGitMergeResolution
    (request: GitConfirmMergeResolutionRequest)
    : JS.Promise<Result<GitConfirmMergeResolutionResult, exn>> =
    invokeGitApiWithPayload ipcGitApiDynamic "confirmGitMergeResolution" request

let private unsupportedGitContentToken = "Unsupported git content"

let private isUnsupportedGitContentError (message: string) =
    message.ToLowerInvariant().Contains(unsupportedGitContentToken.ToLowerInvariant())

let private staleMergeConflictTokens =
    [|
        "not currently marked as conflicted"
        "changed on disk since it was opened"
    |]

let private isStaleMergeConflictError (message: string) =
    let normalizedMessage = message.ToLowerInvariant()
    staleMergeConflictTokens |> Array.exists (fun token -> normalizedMessage.Contains(token))

let private mapStatus (status: GitStatusDto) : GitSidebarStatus = {
    CurrentBranch = status.Current
    TrackingBranch = status.Tracking
    Ahead = status.Ahead
    Behind = status.Behind
    IsClean = status.IsClean
    IsMergeInProgress = status.IsMergeInProgress
}

let private mapChanges (status: GitStatusDto) : GitSidebarChange[] =
    let conflictedPaths = status.Conflicted |> Set.ofArray

    status.Files
    |> Array.map (fun file -> {
        Path = file.Path
        OriginalPath = file.OriginalPath
        IndexStatus = file.Index
        WorkingTreeStatus = file.WorkingDir
        IsConflicted = conflictedPaths.Contains file.Path
    })

let private mapBranchKind (kind: Swate.Electron.Shared.GitTypes.GitBranchRefKind) : GitSidebarBranchKind =
    match kind with
    | Swate.Electron.Shared.GitTypes.GitBranchRefKind.Local -> GitSidebarBranchKind.Local
    | Swate.Electron.Shared.GitTypes.GitBranchRefKind.Remote -> GitSidebarBranchKind.Remote

let private mapBranches (branches: GitBranchRefDto[]) : GitSidebarBranchOption[] =
    branches
    |> Array.map (fun branch -> {
        RefName = branch.RefName
        DisplayLabel = branch.DisplayLabel
        Kind = mapBranchKind branch.Kind
        IsCurrent = branch.IsCurrent
        IsTracking = branch.IsTracking
    })

let private mapProgress (progress: GitProgressDto) : GitSidebarProgress = {
    Method = progress.Method
    Stage = progress.Stage
    ProgressPercent = progress.Progress
}

let GitStateCtx =
    React.createContext<GitStateController> (
        {
            state = GitState.Empty
            refresh = fun () -> promise { return Ok() }
            fetch = fun () -> promise { return Ok() }
            pull = fun () -> promise { return Ok() }
            push = fun () -> promise { return Ok() }
            sync = fun () -> promise { return Ok() }
            createBranch = fun _ -> promise { return Ok() }
            selectChange = fun _ -> promise { return Ok() }
            confirmMergeResolution = fun _ -> promise { return Ok() }
        }
    )

[<Hook>]
let useGitState () = React.useContext GitStateCtx

[<ReactComponent>]
let GitStateCtxProvider (children: ReactElement) =

    let appStateCtx = Renderer.Context.AppStateCtx.useAppState ()
    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()
    let gitState, setGitState = React.useStateWithUpdater GitState.Empty

    let applyStatus (status: GitStatusDto) =
        let mappedChanges = mapChanges status

        setGitState (fun current ->
            let nextSelectedPath =
                current.SelectedChangePath
                |> Option.filter (fun selectedPath ->
                    mappedChanges |> Array.exists (fun change -> change.Path = selectedPath)
                )

            {
                current with
                    Status = mapStatus status
                    ChangedFiles = mappedChanges
                    SelectedChangePath = nextSelectedPath
            }
        )

    let applyBranches (branches: GitBranchRefDto[]) =
        setGitState (fun current -> { current with BranchOptions = mapBranches branches })

    let setBusyNotice (busyNotice: string option) =
        setGitState (fun current -> { current with BusyNotice = busyNotice })

    let setErrorNotice (errorNotice: string option) =
        setGitState (fun current -> { current with ErrorNotice = errorNotice })

    let setSelectedChangePath (selectedChangePath: string option) =
        setGitState (fun current -> { current with SelectedChangePath = selectedChangePath })

    let clearTransientNotices () =
        setGitState (fun current -> {
            current with
                BusyNotice = None
                CurrentProgress = None
                ErrorNotice = None
        })

    let showUnsupportedPage path message =
        setSelectedChangePath (Some path)
        setErrorNotice None
        pageStateCtx.setState (Some(PageState.GitUnsupportedPage { Path = path; Reason = Some message }))

    let refreshAll () = promise {
        if appStateCtx.state.IsNone then
            setGitState (fun _ -> GitState.Empty)
            pageStateCtx.setState None
            return Ok()
        else
            let! statusResult = getGitStatus ()
            let! branchResult = getGitBranches ()

            match statusResult, branchResult with
            | Ok status, Ok branches ->
                applyStatus status
                applyBranches branches
                setErrorNotice None
                return Ok()
            | Ok status, Error exn ->
                applyStatus status
                setGitState (fun current -> {
                    current with
                        BranchOptions = [||]
                        ErrorNotice = Some exn.Message
                })
                return Error exn.Message
            | Error exn, _ ->
                setGitState (fun current -> {
                    current with
                        Status = GitState.Empty.Status
                        ChangedFiles = [||]
                        BranchOptions = [||]
                        ErrorNotice = Some exn.Message
                })
                return Error exn.Message
    }

    let openDiffPage path = promise {
        let! result = getGitDiffViewData path

        match result with
        | Ok diffData ->
            setSelectedChangePath (Some path)
            setErrorNotice None
            pageStateCtx.setState (Some(PageState.GitDiffPage diffData))
            return Ok()
        | Error exn when isUnsupportedGitContentError exn.Message ->
            showUnsupportedPage path exn.Message
            return Ok()
        | Error exn ->
            setErrorNotice (Some exn.Message)
            return Error exn.Message
    }

    let openMergeConflictPage path = promise {
        let! result = getGitMergeConflictViewData path

        match result with
        | Ok mergeData ->
            setSelectedChangePath (Some path)
            setErrorNotice None
            pageStateCtx.setState (Some(PageState.GitMergeConflictPage mergeData))
            return Ok()
        | Error exn when isUnsupportedGitContentError exn.Message ->
            showUnsupportedPage path exn.Message
            return Ok()
        | Error exn ->
            setErrorNotice (Some exn.Message)
            return Error exn.Message
    }

    let openFirstConflictIfNeeded (status: GitStatusDto) = promise {
        match status.Conflicted |> Array.tryHead with
        | Some firstConflictPath -> return! openMergeConflictPage firstConflictPath
        | None -> return Ok()
    }

    let completeOperation (busyLabel: string) (result: Result<unit, string>) =
        promise {
            setBusyNotice None
            setGitState (fun current -> { current with CurrentProgress = None })

            match result with
            | Ok () ->
                let! refreshResult = refreshAll ()

                match refreshResult with
                | Ok () -> return Ok()
                | Error message -> return Error message
            | Error message ->
                setErrorNotice (Some message)
                return Error message
        }

    let runWriteOperation
        (busyLabel: string)
        (operation: unit -> JS.Promise<Result<GitOperationResult, exn>>)
        =
        promise {
            setBusyNotice (Some busyLabel)
            setErrorNotice None

            let! result = operation ()

            match result with
            | Error exn ->
                return! completeOperation busyLabel (Error exn.Message)
            | Ok operationResult when operationResult.Success ->
                return! completeOperation busyLabel (Ok())
            | Ok operationResult ->
                let message = operationResult.Message |> Option.defaultValue $"{busyLabel} failed."
                return! completeOperation busyLabel (Error message)
        }

    let refresh () = refreshAll ()

    let fetch () =
        runWriteOperation "Fetching from remote" (fun () -> gitFetch { Remote = None; Branch = None })

    let push () =
        runWriteOperation "Pushing to remote" (fun () -> gitPush { Remote = None; Branch = None })

    let pull () = promise {
        setBusyNotice (Some "Pulling from remote")
        setErrorNotice None

        let! result = gitPull { Remote = None; Branch = None }
        let! refreshResult = refreshAll ()
        setBusyNotice None
        setGitState (fun current -> { current with CurrentProgress = None })

        let! latestStatusResult = getGitStatus ()

        match latestStatusResult with
        | Ok latestStatus when latestStatus.Conflicted.Length > 0 ->
            return! openFirstConflictIfNeeded latestStatus
        | _ ->
            match result, refreshResult with
            | Error exn, _ -> setErrorNotice (Some exn.Message); return Error exn.Message
            | Ok op, _ when not op.Success ->
                let message = op.Message |> Option.defaultValue "Pull failed."
                setErrorNotice (Some message)
                return Error message
            | _, Error message ->
                setErrorNotice (Some message)
                return Error message
            | _ ->
                setErrorNotice None
                return Ok()
    }

    let sync () = promise {
        let! pullResult = pull ()

        match pullResult with
        | Error message -> return Error message
        | Ok () ->
            let! latestStatusResult = getGitStatus ()

            match latestStatusResult with
            | Ok latestStatus when latestStatus.Conflicted.Length > 0 || latestStatus.IsMergeInProgress ->
                return Ok()
            | Ok _ ->
                return! push ()
            | Error exn ->
                setErrorNotice (Some exn.Message)
                return Error exn.Message
    }

    let createBranchFrom (request: GitSidebarCreateBranchRequest) = promise {
        setBusyNotice (Some "Creating branch")
        setErrorNotice None

        let! result =
            createBranch {
                Name = request.BranchName
                StartPoint = request.StartPoint
            }

        setBusyNotice None

        match result with
        | Error exn ->
            setErrorNotice (Some exn.Message)
            return Error exn.Message
        | Ok operationResult when operationResult.Success ->
            let! refreshResult = refreshAll ()
            return refreshResult
        | Ok operationResult ->
            let message = operationResult.Message |> Option.defaultValue "Branch creation failed."
            setErrorNotice (Some message)
            return Error message
    }

    let selectChange (change: GitSidebarChange) =
        if change.IsConflicted then
            openMergeConflictPage change.Path
        else
            openDiffPage change.Path

    let confirmMergeResolution (request: GitConfirmMergeResolutionRequest) = promise {
        setBusyNotice (Some "Confirming merge resolution")
        setErrorNotice None

        let! result = confirmGitMergeResolution request

        setBusyNotice None
        setGitState (fun current -> { current with CurrentProgress = None })

        match result with
        | Error exn ->
            if isStaleMergeConflictError exn.Message then
                let! _ = refreshAll ()
                setSelectedChangePath None
                pageStateCtx.setState None

            setErrorNotice (Some exn.Message)
            return Error exn.Message
        | Ok payload ->
            applyStatus payload.UpdatedStatus

            match payload.NextConflictedPath with
            | Some nextConflictPath ->
                return! openMergeConflictPage nextConflictPath
            | None ->
                setSelectedChangePath None
                pageStateCtx.setState None
                return Ok()
    }

    let ipcHandler: IMainUpdateRendererApi = {
        IMainUpdateRendererApi.empty with
            gitProgressUpdate = fun progress -> setGitState (fun current -> { current with CurrentProgress = Some(mapProgress progress) })
    }

    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    React.useEffect (
        (fun () ->
            if appStateCtx.state.IsSome then
                refreshAll () |> Promise.start
            else
                setGitState (fun _ -> GitState.Empty)
                pageStateCtx.setState None),
        [| box appStateCtx.state |]
    )

    let gitStateController: GitStateController =
        React.useMemo (
            (fun _ -> {
                state = gitState
                refresh = refresh
                fetch = fetch
                pull = pull
                push = push
                sync = sync
                createBranch = createBranchFrom
                selectChange = selectChange
                confirmMergeResolution = confirmMergeResolution
            }),
            [| box gitState |]
        )

    GitStateCtx.Provider(gitStateController, children)
