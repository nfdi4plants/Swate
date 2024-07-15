module SettingsView

open Fable
open Fable.React
open Fable.React.Props
open Fable.Core.JS
open Fable.Core.JsInterop

open Model
open Messages
open Browser.Types

open Feliz
open Feliz.Bulma

let toggleDarkModeElement (model:Model) dispatch =
    Bulma.level [
        Bulma.level.isMobile
        prop.children [
            Bulma.levelLeft "Darkmode" 
            Bulma.levelRight [
                SidebarComponents.DarkmodeButton.Main() |> prop.children
            ]
        ]
    ]

//let toggleRgbModeElement (model:Model) dispatch =
//    Bulma.level [
//        Bulma.level.isMobile
//        prop.children [
//            Bulma.levelLeft "RGB"
//            Bulma.levelRight [
//                Switch.checkbox [
//                    let isActive = model.SiteStyleState.ColorMode = ExcelColors.transparentMode
//                    prop.id "RgbModeSwitch"
//                    prop.isChecked isActive
//                    switch.isOutlined
//                    Bulma.color.isPrimary
//                    prop.onChange (fun (_:bool) ->
//                        if model.SiteStyleState.ColorMode.Name.StartsWith "Dark" && model.SiteStyleState.ColorMode.Name.EndsWith "_rgb" then
//                            let nextColor =
//                                if isActive then
//                                    let b = Browser.Dom.document.body
//                                    b.classList.remove("niceBkgrnd")
//                                    ExcelColors.darkMode
//                                else
//                                    let b = Browser.Dom.document.body
//                                    b.classList.add("niceBkgrnd")
//                                    ExcelColors.transparentMode
//                            UpdateColorMode nextColor |> StyleChange |> dispatch
//                    )
//                ] |> prop.children
//            ]
//        ]
//    ]

//let customXmlSettings (model:Model) dispatch =
//    Bulma.level [
//        Bulma.level.isMobile
//        prop.children [
//            Bulma.levelLeft "Custom Xml"
//        ]
//        Level.right [ Props [ Style [if model.SiteStyleState.IsDarkMode then Color model.SiteStyleState.ColorMode.Text else Color model.SiteStyleState.ColorMode.Fade]]] [
//            Button.a [
//                Button.Color IsInfo
//                Button.IsOutlined
//                Button.OnClick (fun e -> UpdatePageState (Some Routing.Route.SettingsXml) |> dispatch ) 
//            ] [
//                str "Advanced Settings"
//            ]
//        ]
//    ]

let swateExperts (model:Model) dispatch =
    Bulma.level [
        Bulma.level.isMobile
        prop.children [
            Bulma.levelLeft "Swate.Experts"
            Bulma.levelRight [
                Bulma.button.a [
                    Bulma.color.isInfo
                    Bulma.button.isOutlined
                    prop.onClick (fun _ -> Msg.Batch [UpdateIsExpert true; UpdatePageState (Some Routing.Route.JsonExport)] |> dispatch ) 
                    prop.text "Swate.Experts"
                ] |> prop.children
            ]
        ]
    ]

let swateCore (model:Model) dispatch =
    Bulma.level [
        Bulma.level.isMobile
        prop.children [
            Bulma.levelLeft "Swate.Core"
            Bulma.levelRight [
                Bulma.button.a [
                    Bulma.color.isInfo
                    Bulma.button.isOutlined
                    prop.onClick (fun _ -> Msg.Batch [UpdateIsExpert false; UpdatePageState (Some Routing.Route.BuildingBlock)] |> dispatch ) 
                    prop.text "Swate.Core"
                ] |> prop.children
            ]
        ]
    ]
    

let settingsViewComponent (model:Model) dispatch =
    Html.div [
        pageHeader "Swate Settings"

        Bulma.label "Customize Swate"
        toggleDarkModeElement model dispatch

        //if model.SiteStyleState.ColorMode.Name.StartsWith "Dark" && model.SiteStyleState.ColorMode.Name.EndsWith "_rgb" then
        //    toggleRgbModeElement model dispatch

        //Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Advanced Settings"]
        //customXmlSettings model dispatch

        //Bulma.label "Advanced Settings"
        //if model.PageState.IsExpert then 
        //    swateCore model dispatch
        //else
        //    swateExperts model dispatch
    ]