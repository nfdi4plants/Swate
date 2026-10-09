module Renderer.Components.MainContent.SettingsPageTarget

open System
open Feliz
open Renderer.Components.Helper.ArcVaultHelper
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

[<ReactComponent(true)>]
let SettingsPage () =
    let errorModal = useErrorModalCtx ()
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()

    let onEnsureNotesError =
        createErrorModalCallback errorModal.enqueue "Could not create notes folder" appStateCtx

    let onAutoCreateNotesFolderEnabled () =
        ensureNotesFolder onEnsureNotesError |> Promise.start


    let scaleToPercentage (scale: float) = scale * 100.0 |> Math.Round |> int

    let uiScaling =
        Renderer.MainSyncedState.useMainSyncedState {
            initial = 100
            load =
                fun () -> promise {
                    let! scale = Api.ipcUiSettingsApi.getUiScale ()
                    return scaleToPercentage scale
                }
            subscribe =
                fun update ->
                    Renderer.IpcReceiver.subscribeProxyReceiver<IUiSettingsRendererApi> {
                        uiScaleChanged = scaleToPercentage >> update
                    }
            onError =
                fun error -> createErrorModalCallback errorModal.enqueue "Could not load UI scale" None error.Message
            dependencies = [||]
        }

    let submitUIScaling (newValue: int) =
        promise {
            let newValuePercentile = float newValue / 100.

            match! Api.ipcUiSettingsApi.setUiScale newValuePercentile with
            | Ok() -> ()
            | Error e -> createErrorModalCallback errorModal.enqueue "Error" None e.Message
        }
        |> Promise.start

    Html.div [
        prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-y-auto"
        prop.testId "main-content-settings-page"
        prop.children [
            Swate.Components.PageComponents.SettingsPage.SettingsPage.SettingsPage(
                onAutoCreateNotesFolderEnabled = onAutoCreateNotesFolderEnabled,
                uiScaling = uiScaling.state,
                onUIScaling = submitUIScaling
            )
        ]
    ]
