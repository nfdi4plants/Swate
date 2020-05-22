module CustomComponents.Navbar

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open ExcelColors
open Model
open Messages

let navbarComponent (model : Model) (dispatch : Msg -> unit) =
    Navbar.navbar [Navbar.Props [Props.Role "navigation"; AriaLabel "main navigation" ; ExcelColors.colorElement model.SiteStyleState.ColorMode]] [
        Navbar.Brand.a [] [
            Navbar.Item.a [Navbar.Item.Props [Props.Href "https://csb.bio.uni-kl.de/"]] [
                img [Props.Src "../assets/CSB_Logo.png"]
            ]
            Navbar.burger [ Navbar.Burger.IsActive model.SiteStyleState.BurgerVisible
                            Navbar.Burger.OnClick (fun e -> ToggleBurger |> StyleChange |> dispatch)
                            Navbar.Burger.Props[
                                    Role "button"
                                    AriaLabel "menu"
                                    Props.AriaExpanded false
                            ]
            ] [
                span [AriaHidden true] []
                span [AriaHidden true] []
                span [AriaHidden true] []
            ]
        ]
        Navbar.menu [Navbar.Menu.Props [Id "navbarMenu"; Class (if model.SiteStyleState.BurgerVisible then "navbar-menu is-active" else "navbar-menu") ; ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
            Navbar.Start.div [] [
                Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                    str "How to use"
                ]
            ]
            Navbar.End.div [] [
                Navbar.Item.div [Navbar.Item.Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
                    Switch.switchInline [
                        Switch.Id "DarkModeSwitch"
                        Switch.IsOutlined
                        Switch.Color IsSuccess
                        Switch.OnChange (fun _ -> ToggleColorMode |> StyleChange |> dispatch)
                    ] [str "DarkMode"]
                ]
                Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                    str "Contact"
                ]
            ]
        ]
    ]