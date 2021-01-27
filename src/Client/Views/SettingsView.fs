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
            Switch.switchInline [
                Switch.Id "DarkModeSwitch"
                Switch.IsOutlined
                Switch.Color IsSuccess
                Switch.OnChange (fun _ -> ToggleColorMode |> StyleChange |> dispatch)
            ] [span [Class "nonSelectText"][str "DarkMode"]]
        ]
    ]

let settingsViewComponent (model:Model) dispatch =
    div [

    ][
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Swate Settings"]
        Label.label [][str "Customize Swate"]
        toggleDarkModeElement model dispatch
    ]