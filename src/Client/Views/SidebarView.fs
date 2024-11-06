module SidebarView

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Browser

open CustomComponents
open Fable.Core.JsInterop
open Feliz
open Feliz.Bulma

[<Literal>]
let private Sidebar_Id = "SidebarContainer-ID"

type private SidebarStyle = {
    Size        : Model.WindowSize
} with
    static member init() =
        {
            Size = Model.WindowSize.Tablet
        }

let private createNavigationTab (pageLink: Routing.Route) (model:Model) (dispatch:Msg-> unit) (sidebarsize: Model.WindowSize) =
    let isActive = pageLink.isActive(model.PageState.CurrentPage)
    Bulma.tab [
        if isActive then Bulma.tab.isActive
        Html.a [
            prop.className "navigation" // this class does not do anything, but disables <a> styling.
            prop.onClick (fun e -> e.preventDefault(); UpdatePageState (Some pageLink) |> dispatch)
            match sidebarsize with
            | Mini | MobileMini ->
                prop.children (pageLink |> Routing.Route.toIcon)
            | _ ->
                prop.text pageLink.toStringRdbl
        ]
        |> prop.children
    ]

let private tabRow (model:Model) (tabs: seq<ReactElement>) =
    Bulma.tabs [
        Bulma.tabs.isCentered; Bulma.tabs.isFullWidth; Bulma.tabs.isBoxed
        prop.style [
            style.paddingTop(length.rem 1); style.borderBottom (2, borderStyle.solid, NFDIColors.Mint.Base)
        ]
        tabs
        |> prop.children
    ]

let private tabs (model:Model) dispatch (sidebarsize: Model.WindowSize) =
    let isIEBrowser : bool = Browser.Dom.window.document?documentMode
    tabRow model [
        createNavigationTab Routing.Route.BuildingBlock     model dispatch sidebarsize
        createNavigationTab Routing.Route.TermSearch        model dispatch sidebarsize
        createNavigationTab Routing.Route.Protocol          model dispatch sidebarsize
        createNavigationTab Routing.Route.FilePicker        model dispatch sidebarsize
        createNavigationTab Routing.Route.DataAnnotator     model dispatch sidebarsize
        createNavigationTab Routing.Route.JsonExport        model dispatch sidebarsize
    ]

module private ResizeObserver =

    open Fable.Core
    open Fable.Core.JS
    open Fable.Core.JsInterop

    type ResizeObserverEntry = {
        borderBoxSize: obj
        contentBoxSize: obj
        contentRect: obj
        devicePixelContentBoxSize: obj
        target: obj
    }

    //let private propagateMainWindowSize (dispatch: Messages.Msg -> unit) =
    //    let windowWidth = Browser.Dom.window.innerWidth
    //    let sidebar = Model.WindowSize.ofWidth (int sidebarSize)
    //    let mainWindow = Model.WindowSize.ofWidth <| int (windowWidth - sidebarSize)
    //    (mainWindow, sidebar) |> Messages.StyleChangeMsg.UpdateWindowSizes |> Messages.StyleChange |> dispatch

    type ResizeObserver =
        abstract observe: obj -> unit

    type ResizeObserverStatic =
        [<Emit("new $0($1)")>]
        abstract create : (ResizeObserverEntry [] -> unit) -> ResizeObserver

    [<Global("ResizeObserver")>]
    let MyObserver : ResizeObserverStatic = jsNative

    let observer (state: SidebarStyle, setState: SidebarStyle -> unit) =
        MyObserver.create(fun ele ->
            let width = int ele.[0].contentRect?width
            //let size = ele.
            let nextState = {
                Size = Model.WindowSize.ofWidth width
            }
            //printfn "[FIRE OBSERVER]"
            setState nextState
        )



type SidebarView =

    [<ReactComponent>]
    static member private footer (model:Model, dispatch) =
        React.useEffectOnce(fun () ->
            async {
                let! versionResponse = Api.serviceApi.getAppVersion()
                PersistentStorage.UpdateAppVersion versionResponse |> PersistentStorageMsg |> dispatch
            }
            |> Async.StartImmediate
        )
        Html.div [
            prop.className "flex items-center justify-center p-2"
            prop.children [
                Html.div [
                    Html.text "Swate Release Version "
                    Html.a [prop.href "https://github.com/nfdi4plants/Swate/releases"; prop.target.blank; prop.text model.PersistentStorageState.AppVersion]
                    Html.text " Host "
                    Html.a [prop.style [style.cursor.defaultCursor]; prop.text (sprintf "%O" model.PersistentStorageState.Host)]
                ]
            ]
        ]

    static member private content (model:Model) (dispatch: Msg -> unit) =
        Html.div [
            prop.className "grow overflow-y-auto"
            prop.children [
                match model.PageState.CurrentPage with
                | Routing.Route.BuildingBlock | Routing.Route.Home _ ->
                    BuildingBlock.Core.addBuildingBlockComponent model dispatch

                | Routing.Route.TermSearch ->
                    TermSearch.Main (model, dispatch)

                | Routing.Route.FilePicker ->
                    FilePicker.filePickerComponent model dispatch

                | Routing.Route.Protocol ->
                    Protocol.Templates.Main (model, dispatch)

                | Routing.Route.DataAnnotator ->
                    Pages.DataAnnotator.Main(model, dispatch)

                | Routing.Route.JsonExport ->
                    JsonExporter.Core.FileExporter.Main(model, dispatch)

                | Routing.Route.ProtocolSearch ->
                    Protocol.SearchContainer.Main model dispatch

                | Routing.Route.ActivityLog ->
                    ActivityLog.activityLogComponent model dispatch

                | Routing.Route.Settings ->
                    SettingsView.settingsViewComponent model dispatch

                | Routing.Route.Info ->
                    Pages.Info.Main

                | Routing.Route.PrivacyPolicy ->
                    Pages.PrivacyPolicy.Main()

                | Routing.Route.NotFound ->
                    NotFoundView.notFoundComponent model dispatch
            ]
        ]

    /// The base react component for the sidebar view in the app. contains the navbar and takes body and footer components to create the full view.
    [<ReactComponent>]
    static member Main (model: Model, dispatch: Msg -> unit) =
        let state, setState = React.useState(SidebarStyle.init)
        Html.div [
            prop.className "h-full flex flex-col w-full"
            prop.id Sidebar_Id
            prop.onLoad(fun e ->
                let ele = Browser.Dom.document.getElementById(Sidebar_Id)
                ResizeObserver.observer(state, setState).observe(ele)
            )
            prop.children [

                SidebarComponents.Navbar.NavbarComponent model dispatch state.Size
                Html.div [
                    prop.className "pl-4 pr-4 flex flex-col grow"
                    prop.children [
                        tabs model dispatch state.Size

                        match model.PersistentStorageState.Host, model.ExcelState.HasAnnotationTable with
                        | Some Swatehost.Excel, false ->
                            SidebarComponents.AnnotationTableMissingWarning.annotationTableMissingWarningComponent model dispatch
                            Html.none
                        | _ ->
                            Html.none

                        SidebarView.content model dispatch
                        SidebarView.footer (model, dispatch)
                    ]
                ]
            ]
        ]