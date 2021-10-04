module SettingsView

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
open Fable.Core.JS
open Fable.Core.JsInterop

open Model
open Messages
open Browser.Types
open Fulma.Extensions.Wikiki

let toggleDarkModeElement (model:Model) dispatch =
    Level.level [Level.Level.IsMobile][
        Level.left [][
            str "Darkmode"
        ]
        Level.right [ Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
            Switch.switch [
                Switch.Id "DarkModeSwitch"
                Switch.Checked model.SiteStyleState.IsDarkMode
                Switch.IsOutlined
                Switch.Color IsPrimary
                Switch.OnChange (fun _ ->
                    Browser.Dom.document.cookie <-
                        let isDarkMode b =
                            let expire = System.DateTime.Now.AddYears 100
                            sprintf "%s=%b; expires=%A; path=/" Cookies.IsDarkMode.toCookieString b expire
                        not model.SiteStyleState.IsDarkMode |> isDarkMode
                    ToggleColorMode |> StyleChange |> dispatch
                )
            ] []
        ]
    ]

let customXmlSettings (model:Model) dispatch =
    Level.level [Level.Level.IsMobile][
        Level.left [][
            str "Custom Xml"
        ]
        Level.right [ Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
            Button.a [
                Button.Color IsInfo
                Button.IsOutlined
                Button.OnClick (fun e -> UpdatePageState (Some Routing.Route.SettingsXml) |> dispatch ) 
            ][
                str "Advanced Settings"
            ]
        ]
    ]

let dataStewardsSettings (model:Model) dispatch =
    Level.level [Level.Level.IsMobile][
        Level.left [][
            str "Data Stewards"
        ]
        Level.right [ Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
            Button.a [
                Button.Color IsInfo
                Button.IsOutlined
                Button.OnClick (fun e -> UpdatePageState (Some Routing.Route.SettingsDataStewards) |> dispatch ) 
            ][
                str "Advanced Settings"
            ]
        ]
    ]

let settingsViewComponent (model:Model) dispatch =
    div [
        //Style [MaxWidth "500px"]
    ][
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Swate Settings"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][str "Customize Swate"]
        toggleDarkModeElement model dispatch


        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][str "Advanced Settings"]
        customXmlSettings model dispatch
        dataStewardsSettings model dispatch
    ]