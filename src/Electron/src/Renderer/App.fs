module Renderer.App


open Elmish
open Feliz
open Feliz.UseElmish
open Renderer.Components
open Renderer.Types
open Swate.Components
open Swate.Components.Layout
open Swate.Components.ErrorModal
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

type private Model = {
    // Current ARC root shared with renderer contexts.
    ArcRootPath: ArcRootPath
    // Bumped for main-process live updates so stale snapshot replies are ignored.
    ArcRootPathLiveUpdateVersion: int
    // Bumped for each snapshot request so older async replies cannot win.
    ArcRootPathRequestVersion: int
    PageState: PageState option
    DetailsSidebarIsOpen: bool
    LeftSidebarTarget: LeftSidebarPage
} with

    static member Empty = {
        ArcRootPath = None
        ArcRootPathLiveUpdateVersion = 0
        ArcRootPathRequestVersion = 0
        PageState = None
        DetailsSidebarIsOpen = false
        LeftSidebarTarget = LeftSidebarPage.FileExplorer
    }

type private Msg =
    | ArcRootPathSnapshotRequested
    | ArcRootPathSnapshotLoaded of requestVersion: int * liveUpdateVersionAtStart: int * Result<ArcRootPath, exn>
    | ArcRootPathChanged of ArcRootPath
    | PageStateChanged of PageState option
    | SetDetailsSidebarIsOpen of bool
    | SetLeftSidebarTarget of LeftSidebarPage

let private init () : Model * Cmd<Msg> =
    Model.Empty, Cmd.none

let private msgName =
    function
    | ArcRootPathSnapshotRequested -> "ArcRootPathSnapshotRequested"
    | ArcRootPathSnapshotLoaded _ -> "ArcRootPathSnapshotLoaded"
    | ArcRootPathChanged _ -> "ArcRootPathChanged"
    | PageStateChanged _ -> "PageStateChanged"
    | SetDetailsSidebarIsOpen _ -> "SetDetailsSidebarIsOpen"
    | SetLeftSidebarTarget _ -> "SetLeftSidebarTarget"

let private traceUpdateMsg (msg: Msg) =
    console.log ($"[Renderer.App Elmish] {msgName msg}")

let private resetForClosedArc (model: Model) = {
    Model.Empty with
        ArcRootPathLiveUpdateVersion = model.ArcRootPathLiveUpdateVersion
        ArcRootPathRequestVersion = model.ArcRootPathRequestVersion
}

let private applyArcRootPath (arcRootPath: ArcRootPath) (model: Model) =
    match arcRootPath with
    | Some _ -> {
        model with
            ArcRootPath = arcRootPath
      }
    | None -> resetForClosedArc model

let private createGetOpenPathCmd requestVersion liveUpdateVersionAtStart =
    Cmd.OfPromise.either
        Api.ipcArcVaultApi.getOpenPath
        ()
        (fun arcRootPath -> ArcRootPathSnapshotLoaded(requestVersion, liveUpdateVersionAtStart, Ok arcRootPath))
        (fun error -> ArcRootPathSnapshotLoaded(requestVersion, liveUpdateVersionAtStart, Error error))

let private update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    traceUpdateMsg msg

    match msg with
    | ArcRootPathSnapshotRequested ->
        let requestVersion = model.ArcRootPathRequestVersion + 1

        {
            model with
                ArcRootPathRequestVersion = requestVersion
        },
        createGetOpenPathCmd requestVersion model.ArcRootPathLiveUpdateVersion
    | ArcRootPathSnapshotLoaded(requestVersion, liveUpdateVersionAtStart, Ok arcRootPath)
        when requestVersion = model.ArcRootPathRequestVersion
             && liveUpdateVersionAtStart = model.ArcRootPathLiveUpdateVersion ->
        model |> applyArcRootPath arcRootPath, Cmd.none
    | ArcRootPathSnapshotLoaded(requestVersion, _, _) when requestVersion = model.ArcRootPathRequestVersion ->
        model, Cmd.none
    | ArcRootPathSnapshotLoaded _ -> model, Cmd.none
    | ArcRootPathChanged appState ->
        let model = {
            model with
                ArcRootPathLiveUpdateVersion = model.ArcRootPathLiveUpdateVersion + 1
        }

        match appState with
        | Some _ -> model |> applyArcRootPath appState, Cmd.none
        | None -> model |> resetForClosedArc, Cmd.none
    | PageStateChanged pageStateOption ->
        {
            model with
                PageState = pageStateOption
        },
        Cmd.none
    | SetLeftSidebarTarget leftSidebarTarget ->
        {
            model with
                LeftSidebarTarget = leftSidebarTarget
                DetailsSidebarIsOpen = (leftSidebarTarget = LeftSidebarPage.ArcObjectExplorer)
        },
        Cmd.none
    | SetDetailsSidebarIsOpen isOpen ->
        {
            model with
                DetailsSidebarIsOpen = isOpen
        },
        Cmd.none

let private subscribe (_model: Model) : Sub<Msg> = [
    [ "appPathChange" ],
    fun dispatch ->
        let dispose =
            Renderer.IpcReceiver.subscribeProxyReceiver<IPathChangeRendererApi> {
                pathChange = ArcRootPathChanged >> dispatch
            }

        dispatch ArcRootPathSnapshotRequested

        { new System.IDisposable with
            member _.Dispose() = dispose ()
        }
]

[<ReactComponent>]
let private LeftActionButtons (leftSidebarTarget: LeftSidebarPage, setLeftSidebarTarget) =
    let leftSidebarCtx = Swate.Components.Layout.LeftSidebarContext.useLeftSidebarCtx ()

    let toggleTarget target =
        if leftSidebarTarget = target then
            leftSidebarCtx.setState (not leftSidebarCtx.state)
        else
            leftSidebarCtx.setState true
            setLeftSidebarTarget target

    React.Fragment [
        Layout.LayoutBtn(
            iconClassName = "swt:fluent--home-24-regular",
            tooltip = "File explorer",
            isActive = (leftSidebarTarget = LeftSidebarPage.FileExplorer),
            onClick = fun () -> toggleTarget LeftSidebarPage.FileExplorer
        )
        Layout.LayoutBtn(
            iconClassName = "swt:fluent--database-24-regular",
            tooltip = "ARC object explorer",
            isActive = (leftSidebarTarget = LeftSidebarPage.ArcObjectExplorer),
            onClick = fun () -> toggleTarget LeftSidebarPage.ArcObjectExplorer
        )
        Layout.LayoutBtn(
            iconClassName = "swt:fluent--branch-fork-24-regular",
            tooltip = "Git",
            isActive = (leftSidebarTarget = LeftSidebarPage.Git),
            onClick = fun () -> toggleTarget LeftSidebarPage.Git
        )
    ]

[<ReactComponent>]
let Main () =
    let model, dispatch = React.useElmish (init, update, subscribe, [||])

    let setPageState (pageState: PageState option) = dispatch (PageStateChanged pageState)

    let pageCtx: StateContext<PageState option> =
        React.useMemo (
            (fun _ -> {
                state = model.PageState
                setState = setPageState
            }),
            [| box model.PageState |]
        )

    let children =
        Renderer.Components.MainContent.Main.Main(model.ArcRootPath, model.PageState, model.LeftSidebarTarget)

    let setLeftSidebarTarget =
        React.useCallback ((fun leftSidebarTarget -> dispatch (SetLeftSidebarTarget leftSidebarTarget)), [||])

    let detailsSidebar =
        match model.ArcRootPath, model.LeftSidebarTarget with
        | Some _, LeftSidebarPage.ArcObjectExplorer -> Some(Renderer.Components.DetailsSidebar.ArcObjectDetailsSidebar.Main())
        | _ -> None

    let showDetailsSidebarToggle =
        model.ArcRootPath.IsSome && model.LeftSidebarTarget = LeftSidebarPage.ArcObjectExplorer

    Context.AppStateContext.AppStateCtx.Provider(
        model.ArcRootPath,
        Renderer.Context.FileStateContext.FileStateCtxProvider(
            (fun () -> Api.ipcArcVaultApi.getFileTree ()),
            Renderer.Context.ArcObjectExplorerContext.ArcObjectExplorerCtxProvider(
                Renderer.Context.PageStateContext.PageStateCtx.Provider(
                    pageCtx,
                    ErrorModalProvider.ErrorModalProvider(
                        Renderer.Context.AuthStateContext.Provider(
                            Renderer.Context.GitStateContext.GitStateCtxProvider(
                                AnnotationTable
                                    .AnnotationTableContextProvider
                                    .AnnotationTableContextProvider(
                                        Layout.Main(
                                            children =
                                                React.Fragment [|
                                                    children
                                                    CloseWindowController.CloseWindowController.Subscription()
                                                |],
                                            navbar =
                                                Renderer.Components.Navbar.Main(
                                                    showDetailsSidebarToggle = showDetailsSidebarToggle
                                                ),
                                            leftSidebar =
                                                Renderer.Components.LeftSidebar.Main.Main(model.LeftSidebarTarget),
                                            ?rightSidebar = detailsSidebar,
                                            leftActions =
                                                LeftActionButtons(model.LeftSidebarTarget, setLeftSidebarTarget),
                                            rightSidebarState = {
                                                isOpen = model.DetailsSidebarIsOpen
                                                setIsOpen = fun isOpen -> dispatch (SetDetailsSidebarIsOpen isOpen)
                                                sidebarType = model.LeftSidebarTarget
                                                setSidebarType =
                                                    fun leftSidebarTarget ->
                                                        dispatch (SetLeftSidebarTarget leftSidebarTarget)
                                            }
                                        )
                                    )
                            )
                        )
                    )
                )
            )
        )
    )
