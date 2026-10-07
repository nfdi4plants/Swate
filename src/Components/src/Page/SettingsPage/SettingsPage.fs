namespace Swate.Components.PageComponents.SettingsPage

open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Swate.Components
open Swate.Components.Primitive.LayoutComponents
open Swate.Components.Composite.ThemeSelector
open Swate.Components.Composite.ThemeSelector.Context
open Swate.Components.Composite.TermSearch

module SettingsPageDefaults =
    [<Literal>]
    let AutoCreateNotesFolderLocalStorageKey = "swate-settings-auto-create-notes-folder"

[<Erase; Mangle(false)>]
type SettingsPage =

    [<ReactComponent>]
    static member private SettingColumnElement
        (title: string, settingElement: ReactElement, ?description: ReactElement)
        =
        Html.div [
            prop.className "swt:grid swt:grid-cols-1 swt:md:grid-cols-2 swt:gap-2 swt:py-2"
            prop.children [
                Html.p [ prop.className "swt:text-xl not-prose"; prop.text title ]
                Html.div [
                    prop.className "not-prose"
                    prop.children [ settingElement ]
                ]
                if description.IsSome then
                    Html.div [
                        prop.className "swt:text-sm swt:text-base-content/70 swt:md:col-span-2 swt:prose"
                        prop.children description.Value
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private AutoCreateNotesFolderSetting(?onEnabled: unit -> unit) =
        let onEnabled = defaultArg onEnabled ignore

        let autoCreateNotesFolder, setAutoCreateNotesFolder =
            React.useLocalStorage (SettingsPageDefaults.AutoCreateNotesFolderLocalStorageKey, true)

        SettingsPage.SettingColumnElement(
            "Automatically create notes folder",
            Html.input [
                prop.className [
                    if autoCreateNotesFolder then
                        "swt:toggle-primary"
                    "swt:toggle"
                ]
                prop.type'.checkbox
                prop.isChecked autoCreateNotesFolder
                prop.onChange (fun (isEnabled: bool) ->
                    setAutoCreateNotesFolder isEnabled

                    if isEnabled then
                        onEnabled ()
                )
            ],
            description =
                Html.p [
                    prop.className "swt:mt-1 swt:text-sm swt:text-base-content/70"
                    prop.text
                        "Automatically creates an optional /notes folder when an ARC is opened or created. Disable this here if you do not want automatic notes scaffolding."
                ]
        )

    [<ReactComponent>]
    static member private UIScalingSetting(uiScaling: int, onUIScaling: int -> unit) =
        let scalingDraft, setScalingDraft = React.useState uiScaling

        React.useEffect ((fun () -> setScalingDraft uiScaling), [| box uiScaling |])

        SettingsPage.SettingColumnElement(
            "UI Scaling",
            Html.div [
                prop.className "swt:join"
                prop.children [
                    Html.label [
                        prop.className "swt:join-item swt:input"
                        prop.children [
                            Html.input [
                                prop.className "swt:w-16"
                                prop.onChange setScalingDraft
                                prop.type'.number
                                prop.value scalingDraft
                                prop.step 5
                            ]
                            Html.text "%"
                        ]
                    ]
                    Html.div [
                        prop.text "Update"
                        prop.className "swt:join-item swt:btn"
                        prop.onClick (fun _ -> onUIScaling scalingDraft)
                    ]
                ]
            ],
            description =
                Html.p [
                    prop.className "swt:mt-1 swt:text-sm swt:text-base-content/70"
                    prop.text "Allows adjusting the scaling of the user interface. 100% represents the default size."
                ]
        )

    [<ReactComponent>]
    static member private General
        (?onAutoCreateNotesFolderEnabled: unit -> unit, ?uiScaling: int, ?onUIScaling: int -> unit)
        =
        let onAutoCreateNotesFolderEnabled =
            defaultArg onAutoCreateNotesFolderEnabled ignore

        LayoutComponents.BoxedField(
            "General",
            content = [
                SettingsPage.SettingColumnElement(
                    "Theme",
                    ThemeSelector.ThemeSelector(),
                    description =
                        Html.p [
                            prop.className "swt:mt-1 swt:text-sm swt:text-base-content/70"
                            prop.text
                                "Select the theme for the application. The 'Auto' option will use the system's theme settings."
                        ]
                )

                SettingsPage.AutoCreateNotesFolderSetting(onEnabled = onAutoCreateNotesFolderEnabled)

                match uiScaling, onUIScaling with
                | Some uiScaling, Some onUIScaling -> SettingsPage.UIScalingSetting(uiScaling, onUIScaling)
                | _, _ -> Html.none
            ]
        )

    [<ReactComponent>]
    static member private SearchConfig() =
        LayoutComponents.BoxedField(
            "Term Search Configuration",
            content = [
                TermSearchConfigSetter.TermSearchConfigSetter(fun props ->
                    SettingsPage.SettingColumnElement(
                        props.title,
                        props.settingElement,
                        description = props.description
                    )
                )
            ]
        )

    [<ReactComponent>]
    static member SettingsPage
        (?onAutoCreateNotesFolderEnabled: unit -> unit, ?uiScaling: int, ?onUIScaling: int -> unit)
        =
        let onAutoCreateNotesFolderEnabled =
            defaultArg onAutoCreateNotesFolderEnabled ignore

        LayoutComponents.Section [
            SettingsPage.General(
                onAutoCreateNotesFolderEnabled = onAutoCreateNotesFolderEnabled,
                ?uiScaling = uiScaling,
                ?onUIScaling = onUIScaling
            )

            SettingsPage.SearchConfig()

        ]

    [<ReactComponent(true)>]
    static member Entry(?onAutoCreateNotesFolderEnabled: unit -> unit) =
        let onAutoCreateNotesFolderEnabled =
            defaultArg onAutoCreateNotesFolderEnabled ignore

        ThemeProvider.ThemeProvider(
            TermSearchConfigProvider.TIBQueryProvider(
                SettingsPage.SettingsPage(onAutoCreateNotesFolderEnabled = onAutoCreateNotesFolderEnabled)
            )
        )
