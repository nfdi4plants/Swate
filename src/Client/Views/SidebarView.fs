module SidebarView

open Fable.React
open Fable.React.Props
open Fulma
open ExcelColors
open Model
open Messages
open Browser
open Browser.MediaQueryList
open Browser.MediaQueryListExtensions

open CustomComponents
open Fable.Core.JsInterop
open Feliz

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
    Tabs.tab [Tabs.Tab.IsActive isActive] [
        a [ //Href (Routing.Route.toRouteUrl pageLink)
            Style [
                if isActive then
                    BorderColor model.SiteStyleState.ColorMode.Accent
                    BackgroundColor model.SiteStyleState.ColorMode.BodyBackground
                    Color model.SiteStyleState.ColorMode.Accent
                    BorderBottomColor model.SiteStyleState.ColorMode.BodyBackground
                else
                    BorderBottomColor model.SiteStyleState.ColorMode.Accent
            ]
            OnClick (fun e -> UpdatePageState (Some pageLink) |> dispatch)
        ] [
            Text.span [] [
                match sidebarsize with
                | Mini | MobileMini -> 
                    span [] [pageLink |> Routing.Route.toIcon]
                | _ -> 
                    span [] [str pageLink.toStringRdbl]
            ]

        ]
    ]

let private tabRow (model:Model) (tabs: seq<ReactElement>) =
    Tabs.tabs [
        Tabs.IsCentered; Tabs.IsFullWidth; Tabs.IsBoxed
        Tabs.Props [
            Style [
                BackgroundColor model.SiteStyleState.ColorMode.BodyBackground
                CSSProp.Custom ("overflow","visible")
                PaddingTop "1rem"
            ]
        ]
    ] [
        yield! tabs
    ]

let private tabs (model:Model) dispatch (sidebarsize: Model.WindowSize) =
    let isIEBrowser : bool = Browser.Dom.window.document?documentMode 
    tabRow model [
        if model.PersistentStorageState.PageEntry = Routing.SwateEntry.Core then
            createNavigationTab Routing.Route.BuildingBlock         model dispatch sidebarsize
            createNavigationTab Routing.Route.TermSearch            model dispatch sidebarsize
            createNavigationTab Routing.Route.Protocol              model dispatch sidebarsize
            createNavigationTab Routing.Route.FilePicker            model dispatch sidebarsize
            if not isIEBrowser then
                // docsrc attribute not supported in iframe in IE
                createNavigationTab Routing.Route.Dag               model dispatch sidebarsize
            createNavigationTab Routing.Route.Info                  model dispatch sidebarsize
        else
            createNavigationTab Routing.Route.JsonExport            model dispatch sidebarsize
            createNavigationTab Routing.Route.TemplateMetadata      model dispatch sidebarsize
            createNavigationTab Routing.Route.Validation            model dispatch sidebarsize
            createNavigationTab Routing.Route.Info                  model dispatch sidebarsize
    ]


let private footer (model:Model) =
    div [Style [Color "grey"; BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Position PositionOptions.Sticky; Width "inherit"; Bottom "0"; TextAlign TextAlignOptions.Center ]] [
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
                || model.AddBuildingBlockState.ShowUnitTermSuggestions
                || model.AddBuildingBlockState.ShowUnit2TermSuggestions
                || model.AddBuildingBlockState.ShowBuildingBlockTermSuggestions
            then
                TopLevelMsg.CloseSuggestions |> TopLevelMsg |> dispatch
            if model.AddBuildingBlockState.ShowBuildingBlockSelection then
                BuildingBlockMsg BuildingBlock.ToggleSelectionDropdown |> dispatch
        )
        Style [
            BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text;
            Display DisplayOptions.Flex
            FlexGrow "1"
            FlexDirection "column"
            Position PositionOptions.Relative
        ]
    ] children


module private Content =
    let main (model:Model) (dispatch: Msg -> unit) =
        match model.PageState.CurrentPage with
        | Routing.Route.BuildingBlock ->
            BuildingBlock.addBuildingBlockComponent model dispatch

        | Routing.Route.TermSearch ->
            TermSearch.termSearchComponent model dispatch

        | Routing.Route.Validation ->
            Validation.validationComponent model dispatch

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


        | Routing.Route.SettingsXml ->
            SettingsXml.settingsXmlViewComponent model dispatch

        | Routing.Route.Dag ->
            Dag.Core.mainElement model dispatch

        | Routing.Route.Info ->
            InfoView.infoComponent model dispatch


        | Routing.Route.NotFound ->
            NotFoundView.notFoundComponent model dispatch

    let footer (model:Model) (dispatch: Msg -> unit) =
        let c =
            match model.PageState.CurrentPage with
            | Routing.Route.BuildingBlock ->
                 BuildingBlock.addBuildingBlockFooterComponent model dispatch
                 |> List.singleton
            | _ ->
                []
        if List.isEmpty c |> not then
            Footer.footer [ Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
                Content.content [
                    Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Left)]
                    Content.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode] 
                ] [
                    yield! c
                ]
            ]
        else
            Html.div []
        

/// The base react component for the sidebar view in the app. contains the navbar and takes body and footer components to create the full view.
[<ReactComponent>]
let SidebarView (model: Model) (dispatch: Msg -> unit) =
    let ctx = React.useContext(Context.SpreadsheetDataCtx)
    let state, setState = React.useState(SidebarStyle.init)
    viewContainer model dispatch state setState [
        Navbar.NavbarComponent model dispatch state.Size

        Container.container [ Container.IsFluid ] [
            tabs model dispatch state.Size

            Button.button [
                Button.OnClick(fun _ -> printfn "%A" ctx.State)
            ] [
                str "Test"
            ]

            str <| state.Size.ToString()

            if (not model.ExcelState.HasAnnotationTable) then
                CustomComponents.AnnotationTableMissingWarning.annotationTableMissingWarningComponent model dispatch

            Content.main model dispatch

            Content.footer model dispatch
        ]
        footer model
    ]