namespace Swate.Components.PageComponents.SettingsPage

open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Swate.Components
open Swate.Components.Primitive.LayoutComponents
open Swate.Components.Composite.ThemeSelector
open Swate.Components.Composite.ThemeSelector.Context
open Swate.Components.Composite.TermSearch

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
    static member private General() =
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
    static member SettingsPage() =
        LayoutComponents.Section [
            SettingsPage.General()

            SettingsPage.SearchConfig()

        ]

    [<ReactComponent>]
    static member Entry() =
        ThemeProvider.ThemeProvider(
            TermSearchConfigProvider.TIBQueryProvider(SettingsPage.SettingsPage())
        )

