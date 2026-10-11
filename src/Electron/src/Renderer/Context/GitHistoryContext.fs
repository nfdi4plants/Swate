module Renderer.Context.GitHistoryContext

open Fable.Core
open Feliz
open Renderer.Types
open Swate.Components.Page.GitHistory.Types
open Swate.Electron.Shared.DTOs.GitHistoryDto

type GitHistoryDiffState = {
    Request: GitHistoryDiffRequest
    Loading: bool
    Error: string option
}

/// Rapid selections keep the original return page; workspace diffs have already closed.
let returnPageForHistory (previous: PageState option) (current: PageState option) =
    match current with
    | Some(PageState.GitHistoryDiffPage _) -> previous
    | Some(PageState.GitDiffPage diff) when diff.Committed.IsSome -> previous
    | Some(PageState.GitDiffPage _) -> None
    | other -> other

let isHistorySelectionShown (request: GitHistoryDiffRequest) (current: PageState option) =
    match current with
    | Some(PageState.GitHistoryDiffPage(revision, path)) -> revision = request.Revision && path = request.Path
    | Some(PageState.GitDiffPage diff) ->
        diff.Committed
        |> Option.exists (fun committed -> committed.Revision = request.Revision && committed.Path = request.Path)
    | _ -> false

type GitHistoryState = {
    Root: string option
    Page: GitHistoryPageDto option
    Changes: GitHistoryCommitChanges[]
    ExpandedRevisions: string[]
    SelectedRevision: string option
    SelectedPath: string option
    Initialized: bool
    Stale: bool
    Loading: bool
    Error: string option
    Diff: GitHistoryDiffState option
}

let private emptyState root = {
    Root = root
    Page = None
    Changes = [||]
    ExpandedRevisions = [||]
    SelectedRevision = None
    SelectedPath = None
    Initialized = false
    Stale = false
    Loading = false
    Error = None
    Diff = None
}

type GitHistoryController = {
    state: GitHistoryState
    ensureLoaded: unit -> unit
    refresh: unit -> unit
    loadMore: unit -> unit
    toggleCommit: string -> unit
    selectFile: GitHistoryCommit -> GitHistoryFileChange -> unit
    retryDiff: unit -> unit
    closeDiff: unit -> unit
    scrollTop: unit -> float
    saveScroll: float -> unit
}

let GitHistoryCtx =
    React.createContext<GitHistoryController> {
        state = emptyState None
        ensureLoaded = ignore
        refresh = ignore
        loadMore = ignore
        toggleCommit = ignore
        selectFile = fun _ _ -> ()
        retryDiff = ignore
        closeDiff = ignore
        scrollTop = fun () -> 0.0
        saveScroll = ignore
    }

[<Hook>]
let useGitHistoryCtx () = React.useContext GitHistoryCtx

/// History has its own state and IPC calls; it never dispatches Git workflow writes.
[<ReactComponent>]
let GitHistoryCtxProvider (children: ReactElement) =
    let root = AppStateContext.useAppStateCtx ()
    let page = PageStateContext.usePageStateCtx ()
    let git = GitStateContext.useGitStateCtx ()
    let state, setState = React.useState (emptyState root)
    let latest = React.useRef state
    latest.current <- state
    let liveRoot = React.useRef root
    liveRoot.current <- root
    let historyRequestId = React.useRef 0
    let historyGeneration = React.useRef 0
    let diffRequestId = React.useRef 0
    let scrollPosition = React.useRef 0.0
    let previousMainView = React.useRef<PageState option> None
    let livePage = React.useRef page.state
    livePage.current <- page.state

    let observedGitStatus =
        React.useRef (git.state.Status.CurrentBranch, git.state.WorkspaceVersion)

    let publish next =
        latest.current <- next
        setState next

    let synchronizeRoot () =
        if latest.current.Root <> liveRoot.current then
            historyRequestId.current <- historyRequestId.current + 1
            historyGeneration.current <- historyGeneration.current + 1
            diffRequestId.current <- diffRequestId.current + 1
            scrollPosition.current <- 0.0
            previousMainView.current <- None
            publish (emptyState liveRoot.current)

    let loadHistory reset =
        synchronizeRoot ()
        let before = latest.current
        let nextSkip = before.Page |> Option.bind _.NextSkip

        if before.Root.IsSome && not before.Loading && (reset || nextSkip.IsSome) then
            historyRequestId.current <- historyRequestId.current + 1
            let requestId = historyRequestId.current
            let requestedRoot = before.Root

            let request: GitHistoryRequest = {
                HeadRevision =
                    if reset then
                        None
                    else
                        before.Page |> Option.bind _.HeadRevision
                Skip = if reset then 0 else nextSkip |> Option.defaultValue 0
                PageSize = 30
            }

            if reset then
                scrollPosition.current <- 0.0
                historyGeneration.current <- historyGeneration.current + 1

            publish {
                before with
                    Initialized = true
                    Stale = false
                    Loading = true
                    Error = None
                    Changes = if reset then [||] else before.Changes
                    ExpandedRevisions = if reset then [||] else before.ExpandedRevisions
            }

            promise {
                let! result =
                    Api.ipcGitHistoryApi.listHistory request
                    |> Promise.catch (fun error -> Error error.Message)

                if liveRoot.current = requestedRoot && historyRequestId.current = requestId then
                    match result with
                    | Error message ->
                        publish {
                            latest.current with
                                Loading = false
                                Error = Some message
                        }
                    | Ok loaded ->
                        let loaded =
                            if reset then
                                loaded
                            else
                                let previous =
                                    latest.current.Page |> Option.map _.Commits |> Option.defaultValue [||]

                                {
                                    loaded with
                                        BranchName = latest.current.Page |> Option.bind _.BranchName
                                        Commits = Array.append previous loaded.Commits |> Array.distinctBy _.Revision
                                }

                        publish {
                            latest.current with
                                Page = Some loaded
                                Loading = false
                                Error = None
                        }
            }
            |> Promise.start

    let ensureLoaded () =
        synchronizeRoot ()

        if not latest.current.Initialized || latest.current.Stale then
            loadHistory true

    let toggleCommit revision =
        let before = latest.current

        let existing =
            before.Changes |> Array.tryFind (fun entry -> entry.Revision = revision)

        let expanded = before.ExpandedRevisions |> Array.contains revision
        // Clicking an expanded failed entry retries its file list.
        let retry = existing |> Option.exists (fun entry -> entry.Error.IsSome)

        if expanded && not retry then
            publish {
                before with
                    ExpandedRevisions = before.ExpandedRevisions |> Array.filter ((<>) revision)
            }
        else
            let expansions =
                Array.append before.ExpandedRevisions [| revision |] |> Array.distinct

            if existing |> Option.exists (fun entry -> entry.Loading || entry.Error.IsNone) then
                publish {
                    before with
                        ExpandedRevisions = expansions
                }
            else
                let generation = historyGeneration.current
                let requestedRoot = before.Root

                let pending: GitHistoryCommitChanges = {
                    Revision = revision
                    Files = [||]
                    Loading = true
                    Error = None
                }

                publish {
                    before with
                        ExpandedRevisions = expansions
                        Changes =
                            Array.append (before.Changes |> Array.filter (fun entry -> entry.Revision <> revision)) [|
                                pending
                            |]
                }

                promise {
                    let! result =
                        Api.ipcGitHistoryApi.listChanges { Revision = revision }
                        |> Promise.catch (fun error -> Error error.Message)

                    if liveRoot.current = requestedRoot && historyGeneration.current = generation then
                        let loaded =
                            match result with
                            | Ok files -> {
                                pending with
                                    Files = files
                                    Loading = false
                              }
                            | Error message -> {
                                pending with
                                    Loading = false
                                    Error = Some message
                              }

                        publish {
                            latest.current with
                                Changes =
                                    latest.current.Changes
                                    |> Array.map (fun entry -> if entry.Revision = revision then loaded else entry)
                        }
                }
                |> Promise.start

    let openDiff (request: GitHistoryDiffRequest) =
        synchronizeRoot ()
        previousMainView.current <- returnPageForHistory previousMainView.current livePage.current

        diffRequestId.current <- diffRequestId.current + 1
        let requestId = diffRequestId.current
        let requestedRoot = liveRoot.current

        publish {
            latest.current with
                SelectedRevision = Some request.Revision
                SelectedPath = Some request.Path
                Diff =
                    Some {
                        Request = request
                        Loading = true
                        Error = None
                    }
        }

        let pendingPage = Some(PageState.GitHistoryDiffPage(request.Revision, request.Path))
        livePage.current <- pendingPage
        page.setState pendingPage

        let stillSelected () =
            liveRoot.current = requestedRoot
            && diffRequestId.current = requestId
            && isHistorySelectionShown request livePage.current

        let settle error =
            if stillSelected () then
                publish {
                    latest.current with
                        Diff =
                            Some {
                                Request = request
                                Loading = false
                                Error = error
                            }
                }

        promise {
            let! result =
                Api.ipcGitHistoryApi.openDiff request
                |> Promise.catch (fun error -> Error error.Message)

            if stillSelected () then
                match result with
                | Error message -> settle (Some message)
                | Ok diff ->
                    // The shared workflow owns the worker session and publishes GitDiffPage.
                    let! opened = git.selectCommittedDiff requestedRoot diff

                    match opened with
                    | Ok() -> settle None
                    | Error message -> settle (Some message)
        }
        |> Promise.start

    React.useEffect ((fun () -> synchronizeRoot ()), [| box root |])

    React.useEffect (
        (fun () ->
            match latest.current.Diff with
            | Some diff when not (isHistorySelectionShown diff.Request livePage.current) ->
                diffRequestId.current <- diffRequestId.current + 1
                previousMainView.current <- None
                publish { latest.current with Diff = None }
            | _ -> ()
        ),
        [| box page.state |]
    )

    // Existing status refreshes notify history that its current-branch snapshot needs reloading.
    // Immutable file diffs stay pinned to their selected revisions.
    React.useEffect (
        (fun () ->
            let currentStatus = git.state.Status.CurrentBranch, git.state.WorkspaceVersion
            let changed = observedGitStatus.current <> currentStatus
            observedGitStatus.current <- currentStatus

            if changed && latest.current.Initialized && latest.current.Root = liveRoot.current then
                publish { latest.current with Stale = true }
        ),
        [|
            box git.state.Status.CurrentBranch
            box git.state.WorkspaceVersion
        |]
    )

    React.useEffectOnce (fun () ->
        FsReact.createDisposable (fun () ->
            historyRequestId.current <- historyRequestId.current + 1
            historyGeneration.current <- historyGeneration.current + 1
            diffRequestId.current <- diffRequestId.current + 1
        )
    )

    GitHistoryCtx.Provider(
        {
            state = state
            ensureLoaded = ensureLoaded
            refresh = fun () -> loadHistory true
            loadMore = fun () -> loadHistory false
            toggleCommit = toggleCommit
            selectFile =
                fun commit file ->
                    openDiff {
                        Revision = commit.Revision
                        Path = file.Path
                    }
            retryDiff = fun () -> latest.current.Diff |> Option.iter (fun diff -> openDiff diff.Request)
            closeDiff =
                fun () ->
                    diffRequestId.current <- diffRequestId.current + 1
                    publish { latest.current with Diff = None }
                    let returnPage = previousMainView.current
                    previousMainView.current <- None
                    livePage.current <- returnPage
                    page.setState returnPage
            scrollTop = fun () -> scrollPosition.current
            saveScroll = fun position -> scrollPosition.current <- position
        },
        children
    )
