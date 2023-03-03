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
    Level.level [Level.Level.IsMobile] [
        Level.left [] [
            str "Darkmode"
        ]
        Level.right [ Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
            Switch.switch [
                Switch.Id "DarkModeSwitch"
                Switch.Checked model.SiteStyleState.IsDarkMode
                Switch.IsOutlined
                Switch.Color IsPrimary
                Switch.OnChange (fun _ ->
                    let isCurrentlyDarkMode = model.SiteStyleState.IsDarkMode
                    Browser.Dom.document.cookie <-
                        let expire = System.DateTime.Now.AddYears 100
                        $"{Cookies.IsDarkMode.toCookieString}={(not isCurrentlyDarkMode).ToString()}; expires={expire.ToUniversalTime()}; path=/"
                    let nextColor = if isCurrentlyDarkMode then ExcelColors.colorfullMode else ExcelColors.darkMode
                    UpdateColorMode nextColor |> StyleChange |> dispatch
                )
            ] []
        ]
    ]

let toggleRgbModeElement (model:Model) dispatch =
    Level.level [Level.Level.IsMobile] [
        Level.left [] [
            str "RGB"
        ]
        Level.right [ Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
            Switch.switch [
                let isActive = model.SiteStyleState.ColorMode = ExcelColors.transparentMode
                Switch.Id "RgbModeSwitch"
                Switch.Checked isActive
                Switch.IsOutlined
                Switch.Color IsPrimary
                Switch.OnChange (fun _ ->
                    if model.SiteStyleState.ColorMode.Name.StartsWith "Dark" && model.SiteStyleState.ColorMode.Name.EndsWith "_rgb" then
                        let nextColor =
                            if isActive then
                                let b = Browser.Dom.document.body
                                b.classList.remove("niceBkgrnd")
                                ExcelColors.darkMode
                            else
                                let b = Browser.Dom.document.body
                                b.classList.add("niceBkgrnd")
                                ExcelColors.transparentMode
                        UpdateColorMode nextColor |> StyleChange |> dispatch
                )
            ] []
        ]
    ]

let customXmlSettings (model:Model) dispatch =
    Level.level [Level.Level.IsMobile] [
        Level.left [] [
            str "Custom Xml"
        ]
        Level.right [ Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
            Button.a [
                Button.Color IsInfo
                Button.IsOutlined
                Button.OnClick (fun e -> UpdatePageState (Some Routing.Route.SettingsXml) |> dispatch ) 
            ] [
                str "Advanced Settings"
            ]
        ]
    ]

let swateExperts (model:Model) dispatch =
    Level.level [Level.Level.IsMobile] [
        Level.left [] [
            str "Swate.Experts"
        ]
        Level.right [ Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
            Button.a [
                Button.Color IsInfo
                Button.IsOutlined
                Button.OnClick (fun _ -> Msg.Batch [UpdateIsExpert true; UpdatePageState (Some Routing.Route.JsonExport)] |> dispatch ) 
            ] [
                str "Swate.Experts"
            ]
        ]
    ]

let swateCore (model:Model) dispatch =
    Level.level [Level.Level.IsMobile] [
        Level.left [] [
            str "Swate.Core"
        ]
        Level.right [ Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
            Button.a [
                Button.Color IsInfo
                Button.IsOutlined
                Button.OnClick (fun _ -> Msg.Batch [UpdateIsExpert false; UpdatePageState (Some Routing.Route.BuildingBlock)] |> dispatch ) 
            ] [
                str "Swate.Core"
            ]
        ]
    ]
    

let settingsViewComponent (model:Model) dispatch =
    div [
        //Style [MaxWidth "500px"]
    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Swate Settings"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Customize Swate"]
        toggleDarkModeElement model dispatch

        if model.SiteStyleState.ColorMode.Name.StartsWith "Dark" && model.SiteStyleState.ColorMode.Name.EndsWith "_rgb" then
            toggleRgbModeElement model dispatch

        //Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Advanced Settings"]
        //customXmlSettings model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Advanced Settings"]
        if model.PageState.IsExpert then 
            swateCore model dispatch
        else
            swateExperts model dispatch
    ]