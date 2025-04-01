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

open Swate.Components.ReactHelper

open Browser.Dom

type Settings =

    [<ReactComponent>]
    static member ThemeToggle () =

        let (theme, handleSetTheme) = React.useLocalStorage("theme", "light")
        let animatedMoon =
            """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
            <g fill="currentColor" fill-opacity="0"><path d="M15.22 6.03l2.53-1.94L14.56 4L13.5 1l-1.06 3l-3.19.09l2.53 1.94l-.91 3.06l2.63-1.81l2.63 1.81z">
            <animate id="lineMdMoonRisingLoop0" fill="freeze" attributeName="fill-opacity" begin="0.7s;lineMdMoonRisingLoop0.begin+6s" dur="0.4s" values="0;1"/>
            <animate fill="freeze" attributeName="fill-opacity" begin="lineMdMoonRisingLoop0.begin+2.2s" dur="0.4s" values="1;0"/></path><path d="M13.61 5.25L15.25 4l-2.06-.05L12.5 2l-.69 1.95L9.75 4l1.64 1.25l-.59 1.98l1.7-1.17l1.7 1.17z"><animate fill="freeze" attributeName="fill-opacity" begin="lineMdMoonRisingLoop0.begin+3s" dur="0.4s" values="0;1"/>
            <animate fill="freeze" attributeName="fill-opacity" begin="lineMdMoonRisingLoop0.begin+5.2s" dur="0.4s" values="1;0"/></path><path d="M19.61 12.25L21.25 11l-2.06-.05L18.5 9l-.69 1.95l-2.06.05l1.64 1.25l-.59 1.98l1.7-1.17l1.7 1.17z">
            <animate fill="freeze" attributeName="fill-opacity" begin="lineMdMoonRisingLoop0.begin+0.4s" dur="0.4s" values="0;1"/><animate fill="freeze" attributeName="fill-opacity" begin="lineMdMoonRisingLoop0.begin+2.8s" dur="0.4s" values="1;0"/></path><path d="M20.828 9.731l1.876-1.439l-2.366-.067L19.552 6l-.786 2.225l-2.366.067l1.876 1.439L17.601 12l1.951-1.342L21.503 12z">
            <animate fill="freeze" attributeName="fill-opacity" begin="lineMdMoonRisingLoop0.begin+3.4s" dur="0.4s" values="0;1"/><animate fill="freeze" attributeName="fill-opacity" begin="lineMdMoonRisingLoop0.begin+5.6s" dur="0.4s" values="1;0"/></path></g><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" d="M7 6 C7 12.08 11.92 17 18 17 C18.53 17 19.05 16.96 19.56 16.89 C17.95 19.36 15.17 21 12 21 C7.03 21 3 16.97 3 12 C3 8.83 4.64 6.05 7.11 4.44 C7.04 4.95 7 5.47 7 6 Z" transform="translate(0 22)" stroke-width="1"><animateMotion fill="freeze" calcMode="linear" dur="0.6s" path="M0 0v-22"/>
            </path></svg>"""

        let animatedSun =
            """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
    <rect width="24" height="24" fill="none" />
    <g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2">
        <circle cx="12" cy="32" r="6">
            <animate fill="freeze" attributeName="cy" dur="0.6s" values="32;12" />
        </circle>
        <g>
            <path stroke-dasharray="2" stroke-dashoffset="2" d="M12 19v1M19 12h1M12 5v-1M5 12h-1">
                <animate fill="freeze" attributeName="d" begin="0.7s" dur="0.2s" values="M12 19v1M19 12h1M12 5v-1M5 12h-1;M12 21v1M21 12h1M12 3v-1M3 12h-1" />
                <animate fill="freeze" attributeName="stroke-dashoffset" begin="0.7s" dur="0.2s" values="2;0" />
            </path>
            <path stroke-dasharray="2" stroke-dashoffset="2" d="M17 17l0.5 0.5M17 7l0.5 -0.5M7 7l-0.5 -0.5M7 17l-0.5 0.5">
                <animate fill="freeze" attributeName="d" begin="0.9s" dur="0.2s" values="M17 17l0.5 0.5M17 7l0.5 -0.5M7 7l-0.5 -0.5M7 17l-0.5 0.5;M18.5 18.5l0.5 0.5M18.5 5.5l0.5 -0.5M5.5 5.5l-0.5 -0.5M5.5 18.5l-0.5 0.5" />
                <animate fill="freeze" attributeName="stroke-dashoffset" begin="0.9s" dur="0.2s" values="2;0" />
            </path>
            <animateTransform attributeName="transform" dur="30s" repeatCount="indefinite" type="rotate" values="0 12 12;360 12 12" />
        </g>
    </g>
</svg>"""

        Html.label [
            prop.className "grid lg:col-span-2 grid-cols-subgrid cursor-pointer not-prose"
            prop.children [
                Html.p [
                    prop.className "text-xl py-2"
                    prop.text "Theme"
                ]
                Html.button [
                    prop.className "btn btn-block btn-primary"
                    //prop.text (if theme = "light" then dark else light)
                    prop.children [
                        Html.div [
                            prop.dangerouslySetInnerHTML (
                                match theme.ToLower() with
                                | "light"   -> animatedSun
                                | _         -> animatedMoon
                            )
                        ]
                    ]
                    prop.onClick (fun _ -> 
                        let newTheme = if theme = "light" then "dark" else "light"
                        handleSetTheme newTheme  // Save to localStorage
                        document.documentElement.setAttribute("data-theme", theme)
                    )
                ]
            ]
        ]

    static member ToggleAutosaveConfig(model, dispatch) =
        Html.label [
            prop.className "grid lg:col-span-2 grid-cols-subgrid cursor-pointer not-prose"
            prop.children [
                Html.p [
                    prop.className "select-none text-xl"
                    prop.text "Autosave"
                ]
                Html.div [
                    prop.className "flex items-center pl-10"
                    prop.children [
                        Daisy.toggle [
                            prop.className "ml-14"
                            prop.isChecked model.PersistentStorageState.Autosave
                            toggle.primary
                            prop.onChange (fun (b: bool) ->
                                PersistentStorage.UpdateAutosave b |> PersistentStorageMsg |> dispatch
                            )
                        ]
                    ]
                ]
                Html.p [
                    prop.className "text-sm text-gray-500"
                    prop.text "When you deactivate autosave, your local history will be deleted."
                ]
            ]
        ]

    static member General(model, dispatch) =
        Components.Forms.Generic.BoxedField("General",
            content = [
                Html.div [
                    prop.className "grid grid-cols-1 gap-4 lg:grid-cols-2"
                    prop.children [
                        Settings.ThemeToggle()
                        Settings.ToggleAutosaveConfig(model, dispatch)
                    ]
                ]
            ]
        )

    static member SearchConfig (model, dispatch) =
        Components.Forms.Generic.BoxedField("Term Search Configuration",
            content = [
                Settings.SearchConfig.Main(model, dispatch)
            ]
        )

    static member ActivityLog model =
        Components.Forms.Generic.BoxedField("Activity Log", "Display all recorded activities of this session.",
            content = [
                Html.div [
                    prop.className "overflow-y-auto max-h-[600px]"
                    prop.children [
                        ActivityLog.Main(model)
                    ]
                ]
            ]
        )

    static member Main(model: Model.Model, dispatch) =
        Components.Forms.Generic.Section [
            Settings.General(model, dispatch)

            Settings.SearchConfig (model, dispatch)

            Settings.ActivityLog(model)

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