namespace Pages

open Fable
open Fable.React
open Fable.React.Props
open Fable.Core.JS
open Fable.Core.JsInterop

open Model
open Messages

open Feliz
open Feliz.DaisyUI
open LocalStorage.Darkmode

type Settings =
    [<ReactComponent>]
    static member ThemeToggle () =
        let state = React.useContext(LocalStorage.Darkmode.themeContext)
        let isDark = state.Theme = Dark
        Html.label [
            prop.className "grid lg:col-span-2 grid-cols-subgrid cursor-pointer not-prose"
            prop.onClick (fun e ->
                e.preventDefault()
                let next = if isDark then Light else Dark
                DataTheme.SET next
                state.SetTheme {state with Theme = next}
            )
            prop.children [
                Html.p [
                    prop.className "select-none text-xl py-2"
                    prop.text "Theme"
                ]
                Html.div [
                    prop.className [
                        "btn btn-block swap swap-rotate"
                        if isDark then "swap-active";
                    ]
                    prop.children [
                        Svg.svg [
                            svg.className "size-6 fill-current swap-off"
                            svg.xmlns "http://www.w3.org/2000/svg"
                            svg.viewBox (0, 0, 24, 24)
                            svg.children [
                                Svg.path [
                                    svg.d "M5.64,17l-.71.71a1,1,0,0,0,0,1.41,1,1,0,0,0,1.41,0l.71-.71A1,1,0,0,0,5.64,17ZM5,12a1,1,0,0,0-1-1H3a1,1,0,0,0,0,2H4A1,1,0,0,0,5,12Zm7-7a1,1,0,0,0,1-1V3a1,1,0,0,0-2,0V4A1,1,0,0,0,12,5ZM5.64,7.05a1,1,0,0,0,.7.29,1,1,0,0,0,.71-.29,1,1,0,0,0,0-1.41l-.71-.71A1,1,0,0,0,4.93,6.34Zm12,.29a1,1,0,0,0,.7-.29l.71-.71a1,1,0,1,0-1.41-1.41L17,5.64a1,1,0,0,0,0,1.41A1,1,0,0,0,17.66,7.34ZM21,11H20a1,1,0,0,0,0,2h1a1,1,0,0,0,0-2Zm-9,8a1,1,0,0,0-1,1v1a1,1,0,0,0,2,0V20A1,1,0,0,0,12,19ZM18.36,17A1,1,0,0,0,17,18.36l.71.71a1,1,0,0,0,1.41,0,1,1,0,0,0,0-1.41ZM12,6.5A5.5,5.5,0,1,0,17.5,12,5.51,5.51,0,0,0,12,6.5Zm0,9A3.5,3.5,0,1,1,15.5,12,3.5,3.5,0,0,1,12,15.5Z"
                                ]
                            ]
                        ]
                        Svg.svg [
                            svg.className "size-6 fill-current swap-on"
                            svg.xmlns "http://www.w3.org/2000/svg"
                            svg.viewBox (0, 0, 24, 24)
                            svg.children [
                                Svg.path [
                                    svg.d "M21.64,13a1,1,0,0,0-1.05-.14,8.05,8.05,0,0,1-3.37.73A8.15,8.15,0,0,1,9.08,5.49a8.59,8.59,0,0,1,.25-2A1,1,0,0,0,8,2.36,10.14,10.14,0,1,0,22,14.05,1,1,0,0,0,21.64,13Zm-9.5,6.69A8.14,8.14,0,0,1,7.08,5.22v.27A10.15,10.15,0,0,0,17.22,15.63a9.79,9.79,0,0,0,2.1-.22A8.11,8.11,0,0,1,12.14,19.73Z"
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    static member Appearance () =
        Components.Forms.Generic.BoxedField("Appearance",
            content = [
                Html.div [
                    prop.className "grid grid-cols-1 gap-4 lg:grid-cols-2"
                    prop.children [
                        Settings.ThemeToggle ()
                    ]
                ]
            ]
        )

    static member ActivityLog(model) =
        Components.Forms.Generic.BoxedField("Activity Log", "Display all recorded activities of this session.",
            content = [
                Html.div [
                    prop.className "overflow-y-auto max-h-600px"
                    prop.children [
                        ActivityLog.Main model
                    ]
                ]
            ]
        )

    static member Main(model: Model.Model, dispatch) =
        Components.Forms.Generic.Section [
            Settings.Appearance ()

            Settings.ActivityLog model
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