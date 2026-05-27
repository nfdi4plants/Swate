module Renderer.Components.MainContent.SettingsPageTarget

open Feliz

[<ReactComponent(true)>]
let SettingsPage () =

    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()

    Html.div [
        prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-y-auto"
        prop.testId "main-content-settings-page"
        prop.children [
            Swate.Components.PageComponents.SettingsPage.SettingsPage.SettingsPage()
        ]
    ]