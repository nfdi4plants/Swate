module Renderer.App

open Browser.Dom
open Elmish
open Feliz
open Feliz.UseElmish
open Renderer.Components
open Renderer.Types
open Swate.Components
open Swate.Components.ErrorModal
open Swate.Electron.Shared

type private Model = {
    AppState: ArcRootPath
    PageState: PageState option
    DetailsSidebarIsOpen: bool
    LeftSidebarTarget: LeftSidebarPage
}
with
    static member Empty = {
        AppState = None
        PageState = None
        DetailsSidebarIsOpen = false
        LeftSidebarTarget = LeftSidebarPage.FileExplorer
    }

type private Msg =
    | SetArcRootPath of ArcRootPath
    | PageStateChanged of PageState option
    | SetDetailsSidebarIsOpen of bool
    | SetLeftSidebarTarget of LeftSidebarPage

let private createGetOpenPathCmd () : Cmd<Msg> =
    Cmd.OfPromise.either
        (fun () -> Api.ipcArcVaultApi.getOpenPath (unbox null))
        ()
        SetArcRootPath
        (fun _ -> SetArcRootPath None)

let private init () : Model * Cmd<Msg> =
    Model.Empty, createGetOpenPathCmd ()

let private msgName =
    function
    | SetArcRootPath _ -> "SetArcRootPath"
    | PageStateChanged _ -> "PageStateChanged"
    | SetDetailsSidebarIsOpen _ -> "SetDetailsSidebarIsOpen"
    | SetLeftSidebarTarget _ -> "SetLeftSidebarTarget"

let private traceUpdateMsg (msg: Msg) =
    console.log ($"[Renderer.App Elmish] {msgName msg}")

let private update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    traceUpdateMsg msg

    match msg with
    | SetArcRootPath appState ->
        let nextModel =
            match appState with
            | Some path ->
                {
                    model with
                        AppState = Some path
                }
            | None -> Model.Empty

        nextModel, Cmd.none
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

[<ReactComponent>]
let private LeftActionButtons (leftSidebarTarget: LeftSidebarPage, setLeftSidebarTarget) =
    let leftSidebarCtx = React.useContext Swate.Components.LayoutContext.LeftSidebarContext

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

    React.useEffectOnce (fun () ->
        Renderer.MainUpdateRendererBridge.subscribePathChange (fun pathOption ->
            console.log ("[Swate] CHANGE PATH!")
            dispatch (SetArcRootPath pathOption)
        ))

    let children =
        Renderer.Components.MainContent.Main.Main(model.AppState, model.PageState, model.LeftSidebarTarget)

    let setLeftSidebarTarget =
        React.useCallback ((fun leftSidebarTarget -> dispatch (SetLeftSidebarTarget leftSidebarTarget)), [||])

    let detailsSidebar =
        match model.AppState, model.LeftSidebarTarget with
        | Some _, LeftSidebarPage.ArcObjectExplorer -> Some(Renderer.Components.DetailsSidebar.ArcObjectDetailsSidebar.Main())
        | _ -> None

    let showDetailsSidebarToggle =
        model.AppState.IsSome && model.LeftSidebarTarget = LeftSidebarPage.ArcObjectExplorer

    Context.AppStateContext.AppStateCtx.Provider(
        appCtx,
        Renderer.Context.FileStateContext.FileStateCtxProvider(
            Renderer.Context.ArcObjectExplorerContext.ArcObjectExplorerCtxProvider(
                Renderer.Context.PageStateContext.PageStateCtx.Provider(
                    pageCtx,
                    ErrorModalProvider.ErrorModalProvider(
                        Renderer.Context.AuthStateContext.Provider(
                            Renderer.Context.GitStateContext.GitStateCtxProvider(
                                AnnotationTable.AnnotationTableContextProvider.AnnotationTableContextProvider.AnnotationTableContextProvider(
                                    Layout.Main(
                                        children =
                                            React.Fragment [|
                                                children
                                                CloseWindowController.CloseWindowController.Subscription()
                                            |],
                                        navbar = Renderer.Components.Navbar.Main(showDetailsSidebarToggle = showDetailsSidebarToggle),
                                        leftSidebar = Renderer.Components.LeftSidebar.Main.Main(model.LeftSidebarTarget),
                                        ?rightSidebar = detailsSidebar,
                                        leftActions = LeftActionButtons(model.LeftSidebarTarget, setLeftSidebarTarget),
                                        rightSidebarState = {
                                            isOpen = model.DetailsSidebarIsOpen
                                            setIsOpen = fun isOpen -> dispatch (SetDetailsSidebarIsOpen isOpen)
                                            sidebarType = model.LeftSidebarTarget
                                            setSidebarType = fun leftSidebarTarget -> dispatch (SetLeftSidebarTarget leftSidebarTarget)
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
