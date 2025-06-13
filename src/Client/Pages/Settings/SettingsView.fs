namespace Pages

open Fable
open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop

open Model
open Messages

open Feliz
open Feliz.DaisyUI

open Swate.Components.ReactHelper

open Browser.Dom

open Fable
open Feliz
open Messages

open Feliz.DaisyUI

module Settings =

    type private Catalogues = {
        FoundOnTIB: Set<string>
        Selected: Set<string>
    } with

        /// Found on TIB but not selected
        member this.UnusedCatalogues = Set.difference this.FoundOnTIB this.Selected

        /// Selected but not found on TIB
        member this.DisconnectedCatalogues = Set.difference this.Selected this.FoundOnTIB

        member this.AllCatalogues = Set.union this.FoundOnTIB this.Selected

    type SearchConfig =


        /// Element with label and select adding support for search through a single TIB catalogue.
        static member private TIBSearchCatalogueElement
            (chosen: string, catalogues: Catalogues, select: string -> unit, rmv: unit -> unit)
            =
            let disconnected = catalogues.DisconnectedCatalogues.Contains chosen

            let disconnectedMsg =
                "This catalogue is not found on TIB. Get in contact with the support team and remove it for now!"

            Html.div [
                prop.className "swt:flex swt:flex-row swt:gap-2 swt:items-end"
                prop.children [
                    Html.label [
                        prop.className "swt:form-control swt:w-full swt:max-w-xs"
                        prop.children [
                            Html.div [
                                prop.className "swt:label"
                                prop.children [
                                    Html.span [ prop.className "swt:label-text"; prop.text "Choose your catalogue" ]
                                ]
                            ]
                            Html.select [
                                prop.value chosen
                                prop.onChange (fun e -> select (e))
                                prop.className "swt:select"
                                prop.children [
                                    for catalogue in catalogues.AllCatalogues do
                                        let disabled =
                                            catalogues.DisconnectedCatalogues.Contains catalogue
                                            || catalogues.Selected.Contains catalogue

                                        let disconnected = catalogues.DisconnectedCatalogues.Contains catalogue

                                        Html.option [
                                            prop.disabled (disconnected || disabled)
                                            prop.value catalogue
                                            prop.text catalogue
                                            if disconnected then
                                                prop.title disconnectedMsg
                                            prop.className [
                                                if disabled || disconnected then
                                                    "swt:bg-base-300 swt:cursor-not-allowed"
                                                if disconnected then
                                                    "swt:text-error"
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-error swt:btn-square"
                        prop.onClick (fun _ -> rmv ())
                        prop.children [ Html.i [ prop.className "fa-solid fa-trash-can" ] ]
                    ]
                    if disconnected then
                        Html.div [
                            prop.className "swt:tooltip"
                            prop.custom ("data-tip", disconnectedMsg)
                            prop.children [
                                Html.div [
                                    prop.className "swt:relative"
                                    prop.children [
                                        Html.div [
                                            prop.className
                                                "swt:absolute swt:top-0 swt:right-0 swt:left-0 swt:bottom-0 swt:animate-ping swt:bg-yellow-400"
                                        ]
                                        Html.button [
                                            prop.className
                                                "swt:btn swt:btn-warning swt:btn-active swt:btn-square swt:no-animation"
                                            prop.children [
                                                Html.i [ prop.className "fa-solid fa-triangle-exclamation" ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]

        [<ReactComponent>]
        static member TIBSearch(model: Model.Model, dispatch: Messages.Msg -> unit) =
            let selectedCatalogues = model.PersistentStorageState.TIBSearchCatalogues
            let catalogues, setCatalogues = React.useState ([||])
            let loading, setLoading = React.useState (true)

            React.useEffectOnce (fun _ -> // get all currently supported catalogues
                promise {
                    let! catalogues = Swate.Components.Api.TIBApi.getCollections ()
                    setCatalogues catalogues.content
                    setLoading false
                }
                |> ignore
            )

            let catalogues: Catalogues =
                React.useMemo (
                    (fun () -> {
                        FoundOnTIB = catalogues |> Set.ofArray
                        Selected = selectedCatalogues
                    }),
                    [| catalogues; selectedCatalogues |]
                )

            Html.div [
                Html.div [
                    prop.className "swt:flex swt:flex-row swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            prop.disabled (catalogues.UnusedCatalogues.Count = 0)
                            prop.onClick (fun _ ->
                                if catalogues.UnusedCatalogues.Count <> 0 then
                                    Messages.PersistentStorage.AddTIBSearchCatalogue
                                        catalogues.UnusedCatalogues.MinimumElement
                                    |> PersistentStorageMsg
                                    |> dispatch
                            )
                            prop.children [ Html.i [ prop.className "fa-solid fa-plus" ]; Html.text "Add" ]
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-error swt:btn-sm"
                            prop.disabled (selectedCatalogues.Count = 0)
                            prop.onClick (fun _ ->
                                Messages.PersistentStorage.SetTIBSearchCatalogues Set.empty
                                |> PersistentStorageMsg
                                |> dispatch
                            )
                            prop.children [ Html.i [ prop.className "fa-solid fa-trash-can" ]; Html.text "Clear" ]
                        ]
                    ]
                ]
                if loading then
                    Html.div [
                        prop.className "swt:flex swt:justify-center"
                        prop.children [ Html.i [ prop.className "fa-solid fa-spinner fa-spin" ] ]
                    ]
                elif catalogues.AllCatalogues.Count = 0 then
                    Html.div [
                        prop.className "swt:flex swt:justify-center"
                        prop.children [ Html.p [ prop.text "No catalogues found." ] ]
                    ]
                else
                    for catalogue in selectedCatalogues do
                        let rmv =
                            fun () ->
                                Messages.PersistentStorage.RemoveTIBSearchCatalogue catalogue
                                |> PersistentStorageMsg
                                |> dispatch

                        let setter =
                            fun (s: string) ->
                                (selectedCatalogues |> Set.remove catalogue |> Set.add s)
                                |> PersistentStorage.SetTIBSearchCatalogues
                                |> PersistentStorageMsg
                                |> dispatch

                        SearchConfig.TIBSearchCatalogueElement(catalogue, catalogues, setter, rmv)
            ]

type Settings =

    static member SettingColumnElement(title: string, settingElement: ReactElement, ?description: ReactElement) =
        Html.div [
            prop.className "swt:grid swt:grid-cols-1 swt:md:grid-cols-2 swt:gap-2 swt:py-2"
            prop.children [
                Html.p [ prop.className "swt:text-xl not-prose"; prop.text title ]
                Html.div [ prop.className "not-prose"; prop.children [ settingElement ] ]
                if description.IsSome then
                    Html.div [
                        prop.className "swt:text-sm swt:text-gray-500 swt:md:col-span-2 swt:prose"
                        prop.children description.Value
                    ]
            ]
        ]

    static member SettingColumnElement(title: string, settingElement: ReactElement, ?description: string) =
        let description = description |> Option.map (fun d -> Html.p d)
        Settings.SettingColumnElement(title, settingElement, ?description = description)

    static member SettingColumnElement(title: string, settingElement: ReactElement) =
        Settings.SettingColumnElement(title, settingElement, ?description = unbox<ReactElement option> None)


    [<ReactComponent>]
    static member ThemeToggle() =

        let themeCtx = React.useContext ReactContext.ThemeCtx

        let iconRef = React.useElementRef ()

        let browser =
            """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
	<rect width="24" height="24" fill="none" />
	<path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8h16M4 6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2zm4-2v4" />
</svg>"""

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

        let planti =
            """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
	<rect width="24" height="24" fill="none" />
	<path fill="currentColor" d="M23 4.1V2.3l-1.8-.2c-.1 0-.7-.1-1.7-.1c-4.1 0-7.1 1.2-8.8 3.3C9.4 4.5 7.6 4 5.5 4c-1 0-1.7.1-1.7.1l-1.9.3l.1 1.7c.1 3 1.6 8.7 6.8 8.7H9v3.4c-3.8.5-7 1.8-7 1.8v2h20v-2s-3.2-1.3-7-1.8V15c6.3-.1 8-7.2 8-10.9M12 18h-1v-5.6S10.8 9 8 9c0 0 1.5.8 1.9 3.7c-.4.1-.8.1-1.1.1C4.2 12.8 4 6.1 4 6.1S4.6 6 5.5 6c1.9 0 5 .4 5.9 3.1C11.9 4.6 17 4 19.5 4c.9 0 1.5.1 1.5.1s0 9-6.3 9H14c0-2 2-5 2-5c-3 1-3 4.9-3 4.9v5z" />
</svg>"""

        let viola =
            """<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">
	<rect width="32" height="32" fill="none" />
	<path fill="currentColor" d="M16 1C7.716 1 1 7.716 1 16s6.716 15 15 15s15-6.716 15-15S24.284 1 16 1m1.5 2.086L3.086 17.5a13 13 0 0 1-.072-2.1L15.4 3.015a13 13 0 0 1 2.1.072m2.338.49q.81.25 1.572.6L4.176 21.41a13 13 0 0 1-.6-1.572zM5.19 23.224L23.224 5.19q.645.433 1.234.938l-18.33 18.33q-.505-.588-.938-1.234m2.352 2.648l18.33-18.33q.506.588.938 1.234L8.776 26.81q-.646-.432-1.234-.938m3.048 1.952L27.824 10.59q.35.761.6 1.572L12.162 28.424a13 13 0 0 1-1.572-.6m3.91 1.09L28.914 14.5a13 13 0 0 1 .072 2.1L16.6 28.985a13 13 0 0 1-2.1-.072m5.561-.56l8.292-8.293a13.03 13.03 0 0 1-8.292 8.292M3.647 11.938a13.03 13.03 0 0 1 8.292-8.292z" />
</svg>"""

        React.useLayoutEffect (
            (fun () ->
                let icon =
                    match themeCtx.data with
                    | Swate.Components.Types.Theme.Sunrise -> animatedSun
                    | Swate.Components.Types.Finster -> animatedMoon
                    | Swate.Components.Types.Planti -> planti
                    | Swate.Components.Types.Viola -> viola
                    | Swate.Components.Types.Auto -> browser
                    | _ -> "missing"

                iconRef.current?innerHTML <- icon
                ()
            ),
            [| box themeCtx.data |]
        )

        let mkOption (theme: Swate.Components.Types.Theme) =
            let txt = Swate.Components.Types.Theme.toString theme
            Html.option [ prop.value txt; prop.text txt ]

        Settings.SettingColumnElement(
            "Theme",
            Html.label [
                prop.className "swt:select"
                prop.children [
                    Html.label [ prop.className "swt:label"; prop.ref iconRef ]
                    Html.select [
                        prop.defaultValue (Swate.Components.Types.Theme.toString themeCtx.data)
                        prop.onChange (fun (e: string) -> themeCtx.setData (Swate.Components.Types.Theme.fromString e))
                        prop.children [
                            mkOption Swate.Components.Types.Theme.Sunrise
                            mkOption Swate.Components.Types.Theme.Finster
                            mkOption Swate.Components.Types.Theme.Planti
                            mkOption Swate.Components.Types.Theme.Viola
                            mkOption Swate.Components.Types.Theme.Auto
                        ]
                    ]
                ]
            ]
        )

    static member ToggleAutosaveConfig(model, dispatch) =
        Settings.SettingColumnElement(
            "Autosave",
            Html.input [
                prop.className "swt:toggle swt:toggle-primary"
                prop.isChecked model.PersistentStorageState.Autosave
                prop.type'.checkbox
                prop.onChange (fun (b: bool) ->
                    Messages.PersistentStorage.UpdateAutosave b |> PersistentStorageMsg |> dispatch
                )
            ],
            "When you deactivate autosave, your local history will be deleted."
        )

    static member General(model, dispatch) =
        Components.Forms.Generic.BoxedField(
            "General",
            content = [ Settings.ThemeToggle(); Settings.ToggleAutosaveConfig(model, dispatch) ]
        )

    [<ReactComponent>]
    static member SwateDefaultSearch(model, dispatch) =
        Settings.SettingColumnElement(
            "Swate Default Search",
            Html.input [
                prop.className "swt:toggle swt:toggle-primary"
                prop.isChecked model.PersistentStorageState.SwateDefaultSearch
                prop.type'.checkbox

                prop.onChange (fun (b: bool) ->
                    Messages.PersistentStorage.UpdateSwateDefaultSearch b
                    |> PersistentStorageMsg
                    |> dispatch
                )
            ],
            "When you deactivate this, the default search will not be used."
        )

    [<ReactComponent>]
    static member TIBSearchComponent(model, dispatch) =
        Settings.SettingColumnElement(
            "TIB Search",
            Settings.SearchConfig.TIBSearch(model, dispatch),
            React.fragment [
                Html.p [
                    prop.text
                        "Adds support for high performance TIB term search. Choose a catalogue of terms to search through."
                ]
                Html.p [ prop.text "Selecting multiple catalogues may impact search performance." ]
            ]
        )

    static member SearchConfig(model, dispatch) =
        Components.Forms.Generic.BoxedField(
            "Term Search Configuration",
            content = [
                Settings.SwateDefaultSearch(model, dispatch)

                Settings.TIBSearchComponent(model, dispatch)
            ]
        )

    static member ActivityLog model =
        Components.Forms.Generic.BoxedField(
            "Activity Log",
            "Display all recorded activities of this session.",
            content = [
                Html.div [
                    prop.className "swt:overflow-y-auto swt:max-h-[600px]"
                    prop.children [ ActivityLog.Main(model) ]
                ]
            ]
        )

    static member Main(model: Model.Model, dispatch) =
        Components.Forms.Generic.Section [
            Settings.General(model, dispatch)

            Settings.SearchConfig(model, dispatch)

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