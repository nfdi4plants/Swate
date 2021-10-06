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

let createNavigationTab (pageLink: Routing.Route) (model:Model) (dispatch:Msg-> unit) =
    let isActive = model.PageState.CurrentPage = pageLink
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
                span [Class "hideUnder775px"][str pageLink.toStringRdbl]
                span [Class "hideOver775px"][pageLink |> Routing.Route.toIcon]
            ]

        ]
    ]

let tabRow (model:Model) dispatch (tabs: seq<ReactElement>)=
    Tabs.tabs[
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
    tabRow model dispatch [
        if model.PersistentStorageState.PageEntry = Routing.SwateEntry.Core then
            createNavigationTab Routing.Route.BuildingBlock         model dispatch
            createNavigationTab Routing.Route.TermSearch            model dispatch
            createNavigationTab Routing.Route.Protocol              model dispatch
            createNavigationTab Routing.Route.FilePicker            model dispatch
            createNavigationTab Routing.Route.Info                  model dispatch
        else
            createNavigationTab Routing.Route.Validation            model dispatch
            createNavigationTab Routing.Route.XLSXConverter         model dispatch
            createNavigationTab Routing.Route.Info                  model dispatch
    ]


//let sndRowTabs (model:Model) dispatch =
//    tabRow model dispatch [ ]

let footerContentStatic (model:Model) dispatch =
    div [][
        str "Swate Release Version "
        a [Href "https://github.com/nfdi4plants/Swate/releases"][str model.PersistentStorageState.AppVersion]
    ]

open Fable.Core.JsInterop
open Fable.FontAwesome

let viewContainer (model: Model) (dispatch: Msg -> unit) (children: ReactElement list) =
    div [
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
        Style [MinHeight "100vh"; BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text; PaddingBottom "2rem"]
    ] children

/// The base react component for all views in the app. contains the navbar and takes body and footer components to create the full view.
let baseViewMainElement (model: Model) (dispatch: Msg -> unit) (bodyChildren: ReactElement list) (footerChildren: ReactElement list) =
    viewContainer model dispatch [
        Navbar.navbarComponent model dispatch
        Navbar.quickAccessScalableNavbar model dispatch
        Container.container [
            Container.IsFluid
        ] [
            tabs model dispatch
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

            yield! bodyChildren

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

        div [Style [Position PositionOptions.Fixed; Bottom "0"; Width "100%"; TextAlign TextAlignOptions.Center; Color "grey"; BackgroundColor model.SiteStyleState.ColorMode.BodyBackground]][
            footerContentStatic model dispatch
        ]
    ]