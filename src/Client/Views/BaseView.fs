module BaseView

open Fable.React
open Fable.React.Props
open Fulma
open ExcelColors
open Model
open Messages

open CustomComponents

let createNavigationTab (pageLink: Routing.Page) (model:Model) (dispatch:Msg-> unit) =
    let isActive = (model.PageState.CurrentPage = pageLink)
    Tabs.tab [Tabs.Tab.IsActive isActive] [
        a [ Href (Routing.Page.toPath pageLink)
            Style [
                if isActive then
                    BorderColor model.SiteStyleState.ColorMode.Accent
                    BackgroundColor model.SiteStyleState.ColorMode.BodyBackground
                    Color model.SiteStyleState.ColorMode.Accent
                else
                    BorderBottomColor model.SiteStyleState.ColorMode.Accent
            ]
        ] [
            Text.span [] [str (Routing.Page.toString pageLink)]
        ]
    ]

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
                    ]
                ]
            ] [
                createNavigationTab Routing.Page.AddBuildingBlock   model dispatch
                createNavigationTab Routing.Page.TermSearch         model dispatch
                createNavigationTab Routing.Page.FilePicker         model dispatch
                createNavigationTab Routing.Page.ActivityLog        model dispatch
            ]
            br []

            if (not model.ExcelState.HasAnnotationTable) then
                CustomComponents.AnnotationTableMissingWarning.annotationTableMissingWarningComponent model dispatch

            yield! bodyChildren

            br []

            Footer.footer [ Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
                Content.content [
                    Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Left)]
                    Content.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode] 
                ]
                    footerChildren
                
            ] 
        ]
    ]