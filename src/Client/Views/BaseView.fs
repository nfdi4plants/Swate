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
    let isActive = (model.PageState.CurrentPage = pageLink)
    Tabs.tab [Tabs.Tab.IsActive isActive;] [
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
                /// does not work for me in excel, and in excel online i have a fixed width that always shows the icon
                //let mediaQuery = window.matchMedia("(min-width:575px)")
                //if mediaQuery.matches then
                //    str (pageLink |> Routing.Route.toString)
                //else
                //    pageLink |> Routing.Route.toIcon
                span [Class "hideUnder575px"][str (pageLink |> Routing.Route.toString)]
                span [Class "hideOver575px"][pageLink |> Routing.Route.toIcon]
            ]

        ]
    ]

let footerContentStatic (model:Model) dispatch =
    div [][
        str "Swate Release Version "
        a [Href "https://github.com/nfdi4plants/Swate/releases"][str model.PersistentStorageState.AppVersion]
    ]

open System.Text.RegularExpressions

/// The base react component for all views in the app. contains the navbar and takes body and footer components to create the full view.
let baseViewComponent (model: Model) (dispatch: Msg -> unit) (bodyChildren: ReactElement list) (footerChildren: ReactElement list) =
    div [   Style [MinHeight "100vh"; BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text;]
    ] [
        Navbar.navbarComponent model dispatch
        Container.container [
            Container.IsFluid
        ] [
            br []
            Tabs.tabs[
                Tabs.IsCentered; Tabs.IsFullWidth; Tabs.IsBoxed
                Tabs.Props [
                    Style [
                        BackgroundColor model.SiteStyleState.ColorMode.BodyBackground
                        OverflowX OverflowOptions.Hidden
                    ]
                ]
            ] [
                createNavigationTab Routing.Route.AddBuildingBlock   model dispatch
                createNavigationTab Routing.Route.TermSearch         model dispatch
                createNavigationTab Routing.Route.Validation         model dispatch
                createNavigationTab Routing.Route.FilePicker         model dispatch
                createNavigationTab Routing.Route.Info               model dispatch
            ]

            if (not model.ExcelState.HasAnnotationTable) then
                CustomComponents.AnnotationTableMissingWarning.annotationTableMissingWarningComponent model dispatch

            yield! bodyChildren

            br []

            Footer.footer [ Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
                Content.content [
                    Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Left)]
                    Content.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode] 
                ] [
                    yield! footerChildren
                    Button.button [Button.OnClick (fun e -> TryExcel |> ExcelInterop |> dispatch )] [str "Try Excel"]
                ]
            ]
        ]

        div [Style [Position PositionOptions.Fixed; Bottom "0"; Width "100%"; TextAlign TextAlignOptions.Center; Color "grey"]][
            footerContentStatic model dispatch
        ]
    ]