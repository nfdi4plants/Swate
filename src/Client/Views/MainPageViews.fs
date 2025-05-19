namespace View

open Feliz
open Feliz.DaisyUI
open Messages

module private MainPageUtil =
    [<Literal>]
    let DrawerId = "MainPageDrawerId"


open MainPageUtil

type MainPageView =

    static member DrawerSideContentItem(model: Model.Model, route: Routing.MainPage, onclick) =
        let isActive = model.PageState.MainPage = route

        Html.li [
            prop.onClick onclick
            prop.children [
                Html.a [
                    prop.className [
                        if isActive then
                            "active"
                    ]
                    prop.text route.AsStringRdbl
                ]
            ]
        ]

    static member DrawerSideContent(model: Model.Model, dispatch) =
        Html.div [
            prop.className "bg-base-200 text-base-content min-h-full w-80 p-4"
            prop.children [
                Daisy.button.button [
                    prop.role "navigation"
                    prop.ariaLabel "Back to spreadsheet view"
                    button.link
                    button.sm
                    prop.onClick (fun _ ->
                        UpdateModel {
                            model with
                                Model.PageState.MainPage = Routing.MainPage.Default
                        }
                        |> dispatch)
                    prop.children [ Html.i [ prop.className "fa-solid fa-arrow-left" ]; Html.span "Back" ]
                ]
                Html.ul [
                    prop.className "menu gap-y-1"
                    prop.children [
                        MainPageView.DrawerSideContentItem(
                            model,
                            Routing.MainPage.Settings,
                            fun _ ->
                                UpdateModel {
                                    model with
                                        Model.PageState.MainPage = Routing.MainPage.Settings
                                }
                                |> dispatch
                        )
                        MainPageView.DrawerSideContentItem(
                            model,
                            Routing.MainPage.About,
                            fun _ ->
                                UpdateModel {
                                    model with
                                        Model.PageState.MainPage = Routing.MainPage.About
                                }
                                |> dispatch
                        )
                        MainPageView.DrawerSideContentItem(
                            model,
                            Routing.MainPage.PrivacyPolicy,
                            fun _ ->
                                UpdateModel {
                                    model with
                                        Model.PageState.MainPage = Routing.MainPage.PrivacyPolicy
                                }
                                |> dispatch
                        )
                    ]
                ]
            ]
        ]

    static member Navbar(model: Model.Model, dispatch) =
        Components.BaseNavbar.Glow [
            Html.label [
                prop.className "btn btn-square btn-ghost lg:hidden"
                prop.htmlFor DrawerId
                prop.children [
                    Svg.svg [
                        svg.xmlns "http://www.w3.org/2000/svg"
                        svg.className "size-5"
                        svg.fill "none"
                        svg.viewBox (0, 0, 24, 24)
                        svg.stroke "currentColor"
                        svg.children [
                            Svg.path [
                                svg.strokeLineCap "round"
                                svg.strokeLineJoin "round"
                                svg.strokeWidth 2
                                svg.d "M4 6h16M4 12h16M4 18h7"
                            ]
                        ]
                    ]
                ]
            ]

            Html.div [
                prop.ariaLabel "logo"
                prop.onClick (fun _ ->
                    UpdateModel {
                        model with
                            Model.PageState.MainPage = Routing.MainPage.Default
                    }
                    |> dispatch)
                prop.className "cursor-pointer"
                prop.children [
                    Html.img [
                        prop.style [ style.maxHeight (length.perc 100); style.width 100 ]
                        prop.src @"assets/Swate_logo_for_excel.svg"
                    ]
                ]
            ]
        ]

    static member MainContent(model: Model.Model, dispatch) =
        match model.PageState.MainPage with
        | Routing.MainPage.Settings -> Pages.Settings.Main(model, dispatch)
        | Routing.MainPage.About -> Pages.About.Main
        | Routing.MainPage.PrivacyPolicy -> Pages.PrivacyPolicy.Main()
        | _ ->
            Html.div [
                prop.className "flex flex-col items-center"
                prop.children [ Html.h1 [ prop.text "404"; prop.className "text-4xl" ] ]
            ]

    static member Main(model: Model.Model, dispatch) =
        Daisy.drawer [
            prop.className "md:drawer-open"
            prop.children [
                Html.input [ prop.id DrawerId; prop.type'.checkbox; prop.className "drawer-toggle" ]
                Daisy.drawerContent [
                    prop.className "flex flex-col items-center overflow-y-auto"
                    prop.children [
                        MainPageView.Navbar(model, dispatch)
                        MainPageView.MainContent(model, dispatch)
                    ]
                ]
                Daisy.drawerSide [
                    prop.className "z-10"
                    prop.children [
                        Daisy.drawerOverlay [ prop.htmlFor DrawerId; prop.ariaLabel "Close sidebar" ]
                        MainPageView.DrawerSideContent(model, dispatch)
                    ]
                ]
            ]

        ]