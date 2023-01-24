module BaseView

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

let createNavigationTab (pageLink: Routing.Route) (model:Model) (dispatch:Msg-> unit) =
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
                span [Class "hideUnder775px"] [str pageLink.toStringRdbl]
                span [Class "hideOver775px"] [pageLink |> Routing.Route.toIcon]
            ]

        ]
    ]

let tabRow (model:Model) dispatch (tabs: seq<ReactElement>)=
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

let tabs (model:Model) dispatch =
    let isIEBrowser : bool = Browser.Dom.window.document?documentMode 
    tabRow model dispatch [
        if model.PersistentStorageState.PageEntry = Routing.SwateEntry.Core then
            createNavigationTab Routing.Route.BuildingBlock         model dispatch
            createNavigationTab Routing.Route.TermSearch            model dispatch
            createNavigationTab Routing.Route.Protocol              model dispatch
            createNavigationTab Routing.Route.FilePicker            model dispatch
            if not isIEBrowser then
                // docsrc attribute not supported in iframe in IE
                createNavigationTab Routing.Route.Dag                   model dispatch
            createNavigationTab Routing.Route.Info                  model dispatch
        else
            createNavigationTab Routing.Route.JsonExport            model dispatch
            createNavigationTab Routing.Route.TemplateMetadata      model dispatch
            createNavigationTab Routing.Route.Validation            model dispatch
            createNavigationTab Routing.Route.Info                  model dispatch
    ]


//let sndRowTabs (model:Model) dispatch =
//    tabRow model dispatch [ ]

let footerContentStatic (model:Model) dispatch =
    div [] [
        str "Swate Release Version "
        a [Href "https://github.com/nfdi4plants/Swate/releases"] [str model.PersistentStorageState.AppVersion]
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

    type ResizeObserver =
        abstract observe: obj -> unit

    type ResizeObserverStatic =
        [<Emit("new $0($1)")>]
        abstract create : (ResizeObserverEntry [] -> unit) -> ResizeObserver

    [<Global("ResizeObserver")>]
    let MyObserver : ResizeObserverStatic = jsNative

    let observer = MyObserver.create(fun ele -> Browser.Dom.console.log(ele.[0].contentRect))


let viewContainer (model: Model) (dispatch: Msg -> unit) (children: ReactElement list) =
    let id = "BaseContainer"
    div [
        Id id
        OnLoad(fun e ->
            let ele = Browser.Dom.document.getElementById(id)
            Browser.Dom.console.log("log", ele.offsetWidth)
            ResizeObserver.observer.observe(ele)
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
            BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text; PaddingBottom "2rem"; Display DisplayOptions.Block
        ]
    ] children


/// The base react component for all views in the app. contains the navbar and takes body and footer components to create the full view.
let baseViewMainElement (model: Model) (dispatch: Msg -> unit) (bodyChildren: ReactElement list) (footerChildren: ReactElement list) =
    viewContainer model dispatch [
        Navbar.navbarComponent model dispatch
        //Navbar.quickAccessScalableNavbar model dispatch
        Container.container [
            Container.IsFluid
        ] [
            //tabs model dispatch
            //sndRowTabs model dispatch

            if (not model.ExcelState.HasAnnotationTable) then
                CustomComponents.AnnotationTableMissingWarning.annotationTableMissingWarningComponent model dispatch

            // Error Modal element, not shown when no lastFullEror
            if model.DevState.LastFullError.IsSome then
                CustomComponents.ErrorModal.errorModal model dispatch

            if model.WarningModal.IsSome then
                CustomComponents.WarningModal.warningModal model dispatch

            if model.BuildingBlockDetailsState.ShowDetails then
                CustomComponents.BuildingBlockDetailsModal.buildingBlockDetailModal model dispatch

            if not model.DevState.DisplayLogList.IsEmpty then
                CustomComponents.InteropLoggingModal.interopLoggingModal model dispatch

            if model.CytoscapeModel.ShowModal then
                Cytoscape.View.view model dispatch

            //yield! bodyChildren

            if footerChildren.IsEmpty |> not then
                Footer.footer [ Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
                    Content.content [
                        Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Left)]
                        Content.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode] 
                    ] [
                        yield! footerChildren
                    ]
                ]
        ]

        div [Style [Position PositionOptions.Fixed; Bottom "0"; Width "100%"; TextAlign TextAlignOptions.Center; Color "grey"; BackgroundColor model.SiteStyleState.ColorMode.BodyBackground]] [
            footerContentStatic model dispatch
        ]

    ]