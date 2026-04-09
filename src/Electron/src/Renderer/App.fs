module Renderer.App

open Elmish
open Feliz
open Feliz.UseElmish
open Fable.Electron.Remoting.Renderer

open Swate.Components
open Swate.Components.Shared
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Types
open Browser.Dom
open Renderer.Components


type private Model = {
    AppState: ArcRootPath
    PageState: PageState option
    DetailsSidebarIsOpen: bool
    WorkspaceMode: WorkspaceMode
} with

    static member Empty = {
        AppState = None
        PageState = None
        DetailsSidebarIsOpen = false
        WorkspaceMode = WorkspaceMode.FileExplorer
    }

type private Msg =
    | SetArcRootPath of ArcRootPath
    | PageStateChanged of PageState option
    | SetDetailsSidebarIsOpen of bool
    | SetWorkspaceMode of WorkspaceMode

let private createGetOpenPathCmd () : Cmd<Msg> =
    Cmd.OfPromise.either
        (fun () -> Api.ipcArcVaultApi.getOpenPath (unbox null))
        ()
        SetArcRootPath
        (fun _ -> SetArcRootPath None)

let private init () : Model * Cmd<Msg> =
    {
        AppState = None
        PageState = None
        DetailsSidebarIsOpen = false
        WorkspaceMode = WorkspaceMode.FileExplorer
    },
    createGetOpenPathCmd ()

let private msgName (msg: Msg) =
    match msg with
    | SetArcRootPath _ -> "SetArcRootPath"
    | PageStateChanged _ -> "PageStateChanged"
    | SetDetailsSidebarIsOpen _ -> "SetDetailsSidebarIsOpen"
    | SetWorkspaceMode _ -> "SetWorkspaceMode"

let private traceUpdateMsg (msg: Msg) =
    console.log ($"[Renderer.App Elmish] {msgName msg}")

let private update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    traceUpdateMsg msg

    match msg with
    | SetArcRootPath appState ->
        let nextModel =
            match appState with
            | Some path -> { model with AppState = Some path }
            | None -> Model.Empty

        nextModel, Cmd.none
    | PageStateChanged pageStateOption ->
        {
            model with
                PageState = pageStateOption
        },
        Cmd.none
    | SetWorkspaceMode workspaceMode ->
        {
            model with
                WorkspaceMode = workspaceMode
                DetailsSidebarIsOpen =
                    match workspaceMode with
                    | WorkspaceMode.ArcObjectExplorer -> true
                    | WorkspaceMode.FileExplorer -> model.DetailsSidebarIsOpen
        },
        Cmd.none
    | SetDetailsSidebarIsOpen isOpen ->
        {
            model with
                DetailsSidebarIsOpen = isOpen
        },
        Cmd.none

[<ReactComponent>]
let private WorkspaceModeButtons (workspaceMode: WorkspaceMode, setWorkspaceMode) =

    let toggleArcObjectExplorer () =
        match workspaceMode with
        | WorkspaceMode.ArcObjectExplorer -> setWorkspaceMode WorkspaceMode.FileExplorer
        | WorkspaceMode.FileExplorer -> setWorkspaceMode WorkspaceMode.ArcObjectExplorer

    React.Fragment [
        Layout.LayoutBtn(
            iconClassName = "swt:fluent--home-24-regular",
            tooltip = "Home",
            isActive = (workspaceMode = WorkspaceMode.FileExplorer),
            onClick = fun () -> setWorkspaceMode WorkspaceMode.FileExplorer
        )
        Layout.LayoutBtn(
            iconClassName = "swt:fluent--database-24-regular",
            tooltip = "ARC object explorer",
            isActive = (workspaceMode = WorkspaceMode.ArcObjectExplorer),
            onClick = toggleArcObjectExplorer
        )
    ]

[<ReactComponent>]
let Main () =

    let model, dispatch = React.useElmish (init, update, [||])

    let setAppState (appState: ArcRootPath) = dispatch (SetArcRootPath appState)

    let setPageState (pageState: PageState option) = dispatch (PageStateChanged pageState)

    let appCtx: StateContext<ArcRootPath> =
        React.useMemo (
            (fun _ -> {
                state = model.AppState
                setState = setAppState
            }),
            [| box model.AppState |]
        )

    let pageCtx: StateContext<PageState option> =
        React.useMemo (
            (fun _ -> {
                state = model.PageState
                setState = setPageState
            }),
            [| box model.PageState |]
        )

    let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
        pathChange =
            fun pathOption ->
                console.log ("[Swate] CHANGE PATH!")
                dispatch (SetArcRootPath pathOption)
        recentARCsUpdate = ignore
        authAccountsUpdate = ignore
        fileTreeUpdate = ignore
        gitProgressUpdate = ignore
    }

    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    ///Main content module
    let children = Renderer.Components.MainContent.Main.Main(model.AppState, model.WorkspaceMode)

    let setWorkspaceMode =
        React.useCallback ((fun workspaceMode -> dispatch (SetWorkspaceMode workspaceMode)), [||])

    let detailsSidebar =
        match model.AppState, model.WorkspaceMode with
        | Some _, WorkspaceMode.ArcObjectExplorer -> Some(Renderer.Components.DetailsSidebar.ArcObjectDetailsSidebar.Main())
        | _ -> None

    Context.AppStateCtx.AppStateCtx.Provider(
        appCtx,
        Renderer.Context.FileStateCtx.FileStateCtxProvider(
            Renderer.Context.ArcObjectExplorerCtx.ArcObjectExplorerCtxProvider(
                Renderer.Context.PageStateCtx.PageStateCtx.Provider(
                    pageCtx,
                    AnnotationTableContextProvider.AnnotationTableContextProvider(
                        Layout.Main(
                            children =
                                React.Fragment [|
                                    children
                                    CloseWindowController.CloseWindowController.Subscription()
                                |],
                            navbar =
                                Renderer.Components.Navbar.Main(
                                    showDetailsSidebarToggle =
                                        (model.AppState.IsSome && model.WorkspaceMode = WorkspaceMode.ArcObjectExplorer)
                                ),
                            leftSidebar = Renderer.Components.LeftSidebar.Main.Main(model.WorkspaceMode),
                            ?rightSidebar = detailsSidebar,
                            leftActions = WorkspaceModeButtons(model.WorkspaceMode, setWorkspaceMode),
                            rightSidebarState = {
                                isOpen = model.DetailsSidebarIsOpen
                                setIsOpen = fun isOpen -> dispatch (SetDetailsSidebarIsOpen isOpen)
                                sidebarType = model.WorkspaceMode
                                setSidebarType = fun workspaceMode -> dispatch (SetWorkspaceMode workspaceMode)
                            }
                        )
                    )
                )
            )
        )
    )
