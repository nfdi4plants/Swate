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
    static member private General(?onAutoCreateNotesFolderEnabled: unit -> unit) =
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
    static member SettingsPage(?onAutoCreateNotesFolderEnabled: unit -> unit) =
        let onAutoCreateNotesFolderEnabled =
            defaultArg onAutoCreateNotesFolderEnabled ignore

        LayoutComponents.Section [
            SettingsPage.General(onAutoCreateNotesFolderEnabled = onAutoCreateNotesFolderEnabled)

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
