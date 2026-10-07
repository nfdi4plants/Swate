module Renderer.Components.MainContent.SettingsPageTarget

open Feliz
open Renderer.Components.Helper.ArcVaultHelper
open Swate.Components.Primitive.ErrorModal.Context

[<ReactComponent(true)>]
let SettingsPage () =
    let errorModal = useErrorModalCtx ()
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()

    let onEnsureNotesError =
        createErrorModalCallback errorModal.enqueue "Could not create notes folder" appStateCtx

    let onAutoCreateNotesFolderEnabled () =
        ensureNotesFolder onEnsureNotesError |> Promise.start


    let uiScaling, setUiScaling = React.useState 100

    React.useLayoutEffectOnce (fun () ->
        promise {
            let! scale = Api.ipcUiSettingsApi.getUiScale ()
            int scale |> setUiScaling

        }
        |> Promise.start
    )

    let setUIScaling =
        fun (newValue: int) ->
            promise {
                let newValuePercentile = float newValue / 100.

                match! Api.ipcUiSettingsApi.setUiScale newValuePercentile with
                | Ok() -> setUiScaling newValue
                | Error e -> createErrorModalCallback errorModal.enqueue "Error" None e.Message
            }
            |> Promise.start

    Html.div [
        prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-y-auto"
        prop.testId "main-content-settings-page"
        prop.children [
            Swate.Components.PageComponents.SettingsPage.SettingsPage.SettingsPage(
                onAutoCreateNotesFolderEnabled = onAutoCreateNotesFolderEnabled,
                uiScaling = uiScaling,
                onUIScaling = setUIScaling
            )
        ]
    ]
