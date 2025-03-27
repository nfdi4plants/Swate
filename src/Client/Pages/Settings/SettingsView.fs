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
open Browser.Dom

type Settings =

    static member themeChangeModule: obj = importDefault "theme-change"

    [<ReactComponent>]
    static member ThemeToggle () =

        let currentTheme, setTheme = React.useState(document.documentElement.getAttribute("data-theme"))

        let themeChange =
            let themeChange: obj = Settings.themeChangeModule?themeChange
            themeChange :?> (bool -> unit)

        React.useEffectOnce (fun () ->
            themeChange false // Reattach event listeners manually
            None
        )

        let newTheme, icon =
            match currentTheme with
            | "light"   -> "dark", "🌙 Dark Mode"
            | "dark"    -> "light", "☀️ Light Mode"
            | _         -> "dark", "🌙 Dark Mode"

        Html.button [
            prop.className "btn btn-primary"
            prop.custom ("data-set-theme", newTheme) // Theme-change switches between themes
            prop.custom ("data-key", "theme")
            prop.text icon
            prop.onClick (fun _ -> setTheme newTheme) // Manual override to ensure the change
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