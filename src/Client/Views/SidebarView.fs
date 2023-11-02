module SidebarView

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Browser
open Browser.MediaQueryList
open Browser.MediaQueryListExtensions

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
            //style.custom ("overflow","visible")
            style.paddingTop(length.rem 1)
        ]
        tabs
        |> prop.children
    ]

let private tabs (model:Model) dispatch (sidebarsize: Model.WindowSize) =
    let isIEBrowser : bool = Browser.Dom.window.document?documentMode 
    tabRow model [
        if not model.PageState.IsExpert then
            createNavigationTab Routing.Route.BuildingBlock         model dispatch sidebarsize
            createNavigationTab Routing.Route.TermSearch            model dispatch sidebarsize
            createNavigationTab Routing.Route.Protocol              model dispatch sidebarsize
            createNavigationTab Routing.Route.FilePicker            model dispatch sidebarsize
            //if not isIEBrowser then
                // docsrc attribute not supported in iframe in IE
                //createNavigationTab Routing.Route.Dag               model dispatch sidebarsize
            createNavigationTab Routing.Route.Info                  model dispatch sidebarsize
        else
            createNavigationTab Routing.Route.JsonExport            model dispatch sidebarsize
            createNavigationTab Routing.Route.TemplateMetadata      model dispatch sidebarsize
            //createNavigationTab Routing.Route.Validation            model dispatch sidebarsize
            createNavigationTab Routing.Route.Info                  model dispatch sidebarsize
    ]


let private footer (model:Model) =
    div [Style [Color "grey"; Position PositionOptions.Sticky; Width "inherit"; Bottom "0"; TextAlign TextAlignOptions.Center ]] [
        div [] [
            str "Swate Release Version "
            a [Href "https://github.com/nfdi4plants/Swate/releases"] [str model.PersistentStorageState.AppVersion]
        ]
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
                state with
                    Size = Model.WindowSize.ofWidth width
            }
            //printfn "[FIRE OBSERVER]"
            setState nextState
        )


let private viewContainer (model: Model) (dispatch: Msg -> unit) (state: SidebarStyle) (setState: SidebarStyle -> unit) (children: ReactElement list) =

    div [
        Id Sidebar_Id
        OnLoad(fun e ->
            let ele = Browser.Dom.document.getElementById(Sidebar_Id)
            ResizeObserver.observer(state, setState).observe(ele)
        )
        OnClick (fun e ->
            if model.TermSearchState.ShowSuggestions
                || model.AddBuildingBlockState.ShowUnit2TermSuggestions
            then
                TopLevelMsg.CloseSuggestions |> TopLevelMsg |> dispatch
        )
        Style [
            Display DisplayOptions.Flex
            FlexGrow "1"
            FlexDirection "column"
            Position PositionOptions.Relative
            MaxWidth "100%"
        ]
    ] children


module private Content =
    let main (model:Model) (dispatch: Msg -> unit) =
        match model.PageState.CurrentPage with
        | Routing.Route.BuildingBlock ->
            BuildingBlock.Core.addBuildingBlockComponent model dispatch

        | Routing.Route.TermSearch ->
            TermSearch.termSearchComponent model dispatch

        | Routing.Route.FilePicker ->
            FilePicker.filePickerComponent model dispatch

        | Routing.Route.Protocol ->
            Protocol.Core.fileUploadViewComponent model dispatch

        | Routing.Route.JsonExport ->
            JsonExporter.Core.jsonExporterMainElement model dispatch

        | Routing.Route.TemplateMetadata ->
            TemplateMetadata.Core.newNameMainElement model dispatch

        | Routing.Route.ProtocolSearch ->
            Protocol.Search.protocolSearchView model dispatch

        | Routing.Route.ActivityLog ->
            ActivityLog.activityLogComponent model dispatch

        | Routing.Route.Settings ->
            SettingsView.settingsViewComponent model dispatch

        //| Routing.Route.SettingsXml ->
        //    SettingsXml.settingsXmlViewComponent model dispatch

        | Routing.Route.Dag ->
            Dag.Core.mainElement model dispatch

        | Routing.Route.Info ->
            InfoView.infoComponent model dispatch

        | Routing.Route.NotFound ->
            NotFoundView.notFoundComponent model dispatch
        
/// The base react component for the sidebar view in the app. contains the navbar and takes body and footer components to create the full view.
[<ReactComponent>]
let SidebarView (model: Model) (dispatch: Msg -> unit) =
    let state, setState = React.useState(SidebarStyle.init)
    viewContainer model dispatch state setState [
        SidebarComponents.Navbar.NavbarComponent model dispatch state.Size

        Bulma.container [
            Bulma.container.isFluid
            prop.children [
                tabs model dispatch state.Size

                //str <| state.Size.ToString()

                //Button.button [
                //    Button.OnClick (fun _ ->
                //        //Spreadsheet.Controller.deleteRow 2 model.SpreadsheetModel
                //        //()
                //        //Spreadsheet.DeleteColumn 1 |> SpreadsheetMsg |> dispatch
                //        ()
                //    )
                //] [ str "Test button" ]

                match model.PersistentStorageState.Host, not model.ExcelState.HasAnnotationTable with
                | Swatehost.Excel _, true ->
                    SidebarComponents.AnnotationTableMissingWarning.annotationTableMissingWarningComponent model dispatch
                | _ -> ()

                Content.main model dispatch
            ]
        ]
        footer model
    ]