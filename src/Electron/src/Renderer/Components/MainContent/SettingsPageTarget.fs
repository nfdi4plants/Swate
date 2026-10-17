module Renderer.Components.MainContent.SettingsPageTarget

open System
open Feliz
open Renderer.Components.Helper.ArcVaultHelper
open ARCitISA.Components.Page.GitSidebarTypes
open ARCitISA.Components.Primitive.ErrorModal.Context
open ARCitISA.Electron.Shared.IPCTypes
open ARCitISA.Electron.Shared.IPCTypes.MainToRendererIpc

let private toGitDefaults (values: UserDefaultValuesDto) : SettingsGitDefaults = {
    LfsAutoTrackThresholdMb = values.LfsAutoTrackThresholdMb
    DownloadLargeFiles = values.DownloadLargeFiles
    DiffIndexingLimitMb = values.DiffIndexingLimitMb
    UploadRightAway = values.UploadRightAway
}

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

    // The built-in values stand in until the main process answers. The page then never falls back to the
    // browser storage of the old auto-create setting.
    let userDefaults =
        Renderer.MainSyncedState.useMainSyncedState {
            initial = {
                Effective = Renderer.Context.GitWorkflow.builtInUserDefaults
                BuiltIn = Renderer.Context.GitWorkflow.builtInUserDefaults
            }
            load = fun () -> Api.ipcUserDefaultsApi.getUserDefaults ()
            subscribe =
                fun update ->
                    Renderer.IpcReceiver.subscribeProxyReceiver<IUserDefaultsRendererApi> {
                        userDefaultsChanged = update
                    }
            onError =
                fun error ->
                    createErrorModalCallback errorModal.enqueue "Could not load user defaults" None error.Message
            dependencies = [||]
        }

    let submitUserDefaults (values: UserDefaultValuesDto) =
        promise {
            match! Api.ipcUserDefaultsApi.setUserDefaults values with
            | Ok _ -> ()
            | Error message -> createErrorModalCallback errorModal.enqueue "Could not save user defaults" None message
        }
        |> Promise.start

    let effectiveDefaults = userDefaults.state.Effective

    let submitGitDefaults (gitDefaults: SettingsGitDefaults) =
        submitUserDefaults {
            effectiveDefaults with
                LfsAutoTrackThresholdMb = gitDefaults.LfsAutoTrackThresholdMb
                DownloadLargeFiles = gitDefaults.DownloadLargeFiles
                DiffIndexingLimitMb = gitDefaults.DiffIndexingLimitMb
                UploadRightAway = gitDefaults.UploadRightAway
        }

    let submitAutoCreateNotesFolder (isEnabled: bool) =
        submitUserDefaults {
            effectiveDefaults with
                AutoCreateNotesFolder = isEnabled
        }

    Html.div [
        prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-y-auto"
        prop.testId "main-content-settings-page"
        prop.children [
            ARCitISA.Components.PageComponents.SettingsPage.SettingsPage.SettingsPage(
                onAutoCreateNotesFolderEnabled = onAutoCreateNotesFolderEnabled,
                uiScaling = uiScaling.state,
                onUIScaling = submitUIScaling,
                gitDefaults = toGitDefaults effectiveDefaults,
                gitBuiltInDefaults = toGitDefaults userDefaults.state.BuiltIn,
                onGitDefaultsSave = submitGitDefaults,
                autoCreateNotesFolder = effectiveDefaults.AutoCreateNotesFolder,
                onAutoCreateNotesFolderChange = submitAutoCreateNotesFolder
            )
        ]
    ]
