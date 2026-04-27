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

type private Model = {
    AppState: ArcRootPath
    PageState: PageState option
    LeftSidebarTarget: LeftSidebarPage
}
with
    static member Empty = {
        AppState = None
        PageState = None
        LeftSidebarTarget = LeftSidebarPage.FileExplorer
    }

type private Msg =
    | SetArcRootPath of ArcRootPath
    | PageStateChanged of PageState option
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
        },
        Cmd.none

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
        Renderer.Components.MainContent.Main.Main(model.AppState, model.PageState)

    let setLeftSidebarTarget =
        React.useCallback ((fun leftSidebarTarget -> dispatch (SetLeftSidebarTarget leftSidebarTarget)), [||])

    Context.AppStateContext.AppStateCtx.Provider(
        appCtx,
        Renderer.Context.FileStateContext.FileStateCtxProvider(
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
                                    navbar = Renderer.Components.Navbar.Main(),
                                    leftSidebar = Renderer.Components.LeftSidebar.Main.Main(model.LeftSidebarTarget),
                                    leftActions = LeftActionButtons(model.LeftSidebarTarget, setLeftSidebarTarget)
                                )
                            )
                        )
                    )
                )
            )
        )
    )
