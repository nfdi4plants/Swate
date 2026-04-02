module Renderer.App

open Elmish
open Feliz
open Feliz.UseElmish
open Fable.Electron.Remoting.Renderer

open Swate.Components
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Types
open Browser.Dom
open Renderer.Components


type private Model = {
    AppState: ArcRootPath
    PageState: PageState option
    LeftSidebarIsOpen: bool
    LeftSidebarTarget: LeftSidebarPage
} with

    static member Empty = {
        AppState = None
        PageState = None
        LeftSidebarIsOpen = false
        LeftSidebarTarget = LeftSidebarPage.FileExplorer
    }

type private Msg =
    | SetArcRootPath of ArcRootPath
    | PageStateChanged of PageState option
    | ToggleRightSidebarTarget of LeftSidebarPage
    | SetRightSidebarIsOpen of bool

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
    },
    createGetOpenPathCmd ()

let private msgName (msg: Msg) =
    match msg with
    | SetArcRootPath _ -> "SetArcRootPath"
    | PageStateChanged _ -> "PageStateChanged"
    | SetRightSidebarIsOpen _ -> "SetLeftSidebarIsOpen"
    | ToggleRightSidebarTarget _ -> "ToggleLeftSidebarTarget"

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
    | ToggleRightSidebarTarget target ->
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
    | SetRightSidebarIsOpen isOpen ->
        {
            model with
                LeftSidebarIsOpen = isOpen
        },
        Cmd.none

[<ReactComponent>]
let private LeftActionButtons (leftSidebarTarget: LeftSidebarPage, toggleTarget) =

    let toggleArcObjectExplorer () =
        match leftSidebarTarget with
        | LeftSidebarPage.ArcObjectTree -> toggleTarget LeftSidebarPage.FileExplorer
        | LeftSidebarPage.FileExplorer -> toggleTarget LeftSidebarPage.ArcObjectTree

    React.Fragment [
        Layout.LayoutBtn(
            iconClassName = "swt:fluent--home-24-regular",
            tooltip = "Home",
            isActive = (leftSidebarTarget = LeftSidebarPage.FileExplorer),
            onClick = fun () -> toggleTarget (LeftSidebarPage.FileExplorer)
        )
        Layout.LayoutBtn(
            iconClassName = "swt:fluent--database-24-regular",
            tooltip = "ARC object explorer",
            isActive = (leftSidebarTarget = LeftSidebarPage.ArcObjectTree),
            onClick = toggleArcObjectExplorer
        )
    ]

[<ReactComponent>]
let private LeftSidebarSync (explorerMode: LeftSidebarPage) =
    let leftSidebarCtx = React.useContext Swate.Components.LayoutContext.LeftSidebarContext

    React.useEffect (
        (fun () ->
            match explorerMode with
            | LeftSidebarPage.ArcObjectTree when not leftSidebarCtx.state -> leftSidebarCtx.setState true
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
    let children = Renderer.Components.MainContent.Main.Main(model.AppState, model.LeftSidebarTarget)

    let toggleLeftSidebarTarget =
        React.useCallback ((fun target -> dispatch (ToggleRightSidebarTarget target)), [||])

    let rightSidebar =
        match model.AppState, model.LeftSidebarTarget with
        | Some _, LeftSidebarPage.ArcObjectTree -> Some(Renderer.Components.RightSidebar.ArcObjectDetailsSidebar.Main())
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
                                    LeftSidebarSync(model.LeftSidebarTarget)
                                    children
                                    CloseWindowController.CloseWindowController.Subscription()
                                |],
                            navbar =
                                Renderer.Components.Navbar.Main(
                                    showRightSidebarToggle = (model.AppState.IsSome && model.LeftSidebarTarget = LeftSidebarPage.ArcObjectTree)
                                ),
                            leftSidebar = Renderer.Components.LeftSidebar.Main.Main(model.LeftSidebarTarget),
                            ?rightSidebar = rightSidebar,
                            leftActions = LeftActionButtons(model.LeftSidebarTarget, toggleLeftSidebarTarget),
                            rightSidebarState = {
                                isOpen = model.LeftSidebarIsOpen
                                setIsOpen = fun isOpen -> dispatch (SetRightSidebarIsOpen isOpen)
                                sidebarType = model.LeftSidebarTarget
                                setSidebarType = fun target -> dispatch (ToggleRightSidebarTarget target)
                            }
                        )
                    )
                )
            )
        )
    )
