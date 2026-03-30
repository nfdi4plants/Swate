module Renderer.App

open Elmish
open Feliz
open Feliz.UseElmish
open Fable.Electron.Remoting.Renderer
open Fable.Core

open Swate.Components
open Swate.Electron.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Types
open Browser.Dom
open Renderer.Components

type private Model = {
    AppState: ArcRootPath
    PageState: PageState option
    LeftSidebarIsOpen: bool
    LeftSidebarTarget: LeftSidebarPage
    ExplorerMode: ExplorerMode
} with

    static member Empty = {
        AppState = None
        PageState = None
        LeftSidebarIsOpen = false
        LeftSidebarTarget = LeftSidebarPage.FileExplorer
        ExplorerMode = ExplorerMode.NormalFileTree
    }

type private Msg =
    | SetArcRootPath of ArcRootPath
    | PageStateChanged of PageState option
    | ToggleLeftSidebarTarget of LeftSidebarPage
    | SetLeftSidebarIsOpen of bool
    | SetExplorerMode of ExplorerMode

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
        LeftSidebarIsOpen = false
        LeftSidebarTarget = LeftSidebarPage.FileExplorer
        ExplorerMode = ExplorerMode.NormalFileTree
    },
    createGetOpenPathCmd ()

let private msgName (msg: Msg) =
    match msg with
    | SetArcRootPath _ -> "SetArcRootPath"
    | PageStateChanged _ -> "PageStateChanged"
    | ToggleLeftSidebarTarget _ -> "ToggleLeftSidebarTarget"
    | SetLeftSidebarIsOpen _ -> "SetLeftSidebarIsOpen"
    | SetExplorerMode _ -> "SetExplorerMode"

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
    | ToggleLeftSidebarTarget target ->
        let nextModel =
            if model.LeftSidebarTarget = target then
                {
                    model with
                        LeftSidebarIsOpen = not model.LeftSidebarIsOpen
                }
            else
                {
                    model with
                        LeftSidebarIsOpen = true
                        LeftSidebarTarget = target
                }

        nextModel, Cmd.none
    | SetLeftSidebarIsOpen isOpen ->
        {
            model with
                LeftSidebarIsOpen = isOpen
        },
        Cmd.none
    | SetExplorerMode explorerMode ->
        {
            model with
                ExplorerMode = explorerMode
        },
        Cmd.none

[<ReactComponent>]
let private LeftActionButtons (leftSidebarTarget: LeftSidebarPage, toggleTarget) =

    // let leftSidebarStateCtx =
    //     React.useContext Swate.Components.LayoutContext.LeftSidebarContext

    React.Fragment [
        Layout.LayoutBtn(
            iconClassName = "swt:fluent--home-24-regular",
            tooltip = "Home",
            isActive = (leftSidebarTarget = LeftSidebarPage.FileExplorer),
            onClick = fun () -> toggleTarget (LeftSidebarPage.FileExplorer)
        )
    ]

[<ReactComponent>]
let private RightSidebarSync (explorerMode: ExplorerMode) =
    let rightSidebarCtx = React.useContext Swate.Components.LayoutContext.RightSidebarContext

    React.useEffect (
        (fun () ->
            match explorerMode with
            | ExplorerMode.ArcObjectTree when not rightSidebarCtx.state -> rightSidebarCtx.setState true
            | _ -> ()),
        [| box explorerMode |]
    )

    Html.none

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
    let children = Renderer.Components.MainContent.Main.Main(model.AppState, model.ExplorerMode)

    let toggleLeftSidebarTarget =
        React.useCallback ((fun target -> dispatch (ToggleLeftSidebarTarget target)), [||])

    let setExplorerMode =
        React.useCallback ((fun explorerMode -> dispatch (SetExplorerMode explorerMode)), [||])

    let rightSidebar =
        match model.AppState, model.ExplorerMode with
        | Some _, ExplorerMode.ArcObjectTree -> Some(Renderer.Components.RightSidebar.ArcObjectDetailsSidebar.Main())
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
                                    RightSidebarSync(model.ExplorerMode)
                                    children
                                    CloseWindowController.CloseWindowController.Subscription()
                                |],
                            navbar =
                                Renderer.Components.Navbar.Main(
                                    showRightSidebarToggle = (model.AppState.IsSome && model.ExplorerMode = ExplorerMode.ArcObjectTree)
                                ),
                            leftSidebar = Renderer.Components.LeftSidebar.Main.Main(model.ExplorerMode, setExplorerMode),
                            ?rightSidebar = rightSidebar,
                            leftActions = LeftActionButtons(model.LeftSidebarTarget, toggleLeftSidebarTarget),
                            leftSidebarState = {
                                isOpen = model.LeftSidebarIsOpen
                                setIsOpen = fun isOpen -> dispatch (SetLeftSidebarIsOpen isOpen)
                                sidebarType = model.LeftSidebarTarget
                                setSidebarType = fun target -> dispatch (ToggleLeftSidebarTarget target)
                            }
                        )
                    )
                )
            )
        )
    )
