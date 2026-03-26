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

open ARCtrl

open Renderer.Components

type SyncRequestKey = string * ArcFilesDiscriminate * string

type private Model = {
    AppState: ArcRootPath
    PageState: PageState option
}

type private Msg =
    | SetArcRootPath of ArcRootPath
    | PageStateChanged of PageState option
    | GetOpenPathResponse of string option
    | PathChanged of string option

let private createGetOpenPathCmd () : Cmd<Msg> =
    Cmd.OfPromise.either
        (fun () -> Api.ipcArcVaultApi.getOpenPath (unbox null))
        ()
        GetOpenPathResponse
        (fun _ -> GetOpenPathResponse None)

let private init () : Model * Cmd<Msg> =
    { AppState = None; PageState = None }, createGetOpenPathCmd ()

let private msgName (msg: Msg) =
    match msg with
    | SetArcRootPath _ -> "SetAppState"
    | GetOpenPathResponse _ -> "GetOpenPathResponse"
    | PathChanged _ -> "PathChanged"
    | PageStateChanged _ -> "PageStateChanged"

let private traceUpdateMsg (msg: Msg) =
    console.log ($"[Renderer.App Elmish] {msgName msg}")

let private update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    traceUpdateMsg msg

    match msg with
    | SetArcRootPath appState -> { model with AppState = appState }, Cmd.none
    | GetOpenPathResponse pathOption
    | PathChanged pathOption ->
        match pathOption with
        | Some p -> { model with AppState = Some p }, Cmd.none
        | None ->
            {
                model with
                    AppState = None
                    PageState = None
            },
            Cmd.none
    | PageStateChanged pageStateOption ->
        {
            model with
                PageState = pageStateOption
        },
        Cmd.none

[<ReactComponent>]
let private LeftActionButtons () =
    React.Fragment [

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
                dispatch (PathChanged pathOption)
        recentARCsUpdate = ignore
        authAccountsUpdate = ignore
        fileTreeUpdate = ignore
        gitProgressUpdate = ignore
    }


    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    ///Main content module
    let children = Renderer.Components.MainContent.Main.Main(model.AppState)

    Context.AppStateCtx.AppStateCtx.Provider(
        appCtx,
        Renderer.Context.FileStateCtx.FileStateCtxProvider(
            Renderer.Context.PageStateCtx.PageStateCtx.Provider(
                pageCtx,
                AnnotationTableContextProvider.AnnotationTableContextProvider(
                    Layout.Main(
                        children =
                            React.Fragment [|
                                children
                                CloseWindowController.CloseWindowController.Subscription()
                            |],
                        navbar = Renderer.Components.Navbar.Main(),
                        leftSidebar = Renderer.Components.LeftSidebar.Main.Main(),
                        leftActions = React.Fragment [| Layout.LeftSidebarToggleBtn() |]
                    )
                )
            )
        )
    )