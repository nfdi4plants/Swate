module Main.Settings.AppVersion

open Main
open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron.Main
open Swate.Components.Api.GitHubReleases
open Swate.Components.Util.SemVer
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open ARCtrl.Helper.SemVer

let private settingsFileName = "app-version.json"

let mutable private state: AppVersionState = {
    CurrentVersion = ""
    UpdateVersion = None
    ChangelogVersion = None
}

let getState () = state

/// Called once after app.whenReady, before creating any windows.
let initialize () =
    let current = Swate.Electron.Shared.ApplicationVersion.current

    let previous =
        try
            SettingsStore.tryReadSettingsFile settingsFileName
            |> Option.bind (fun content ->
                let value = JS.JSON.parse content

                if jsTypeof value = "string" then
                    Some(unbox<string> value)
                else
                    None
            )
        with _ ->
            None

    state <- {
        CurrentVersion = current
        UpdateVersion = None
        // First installation establishes a baseline; only an upgrade opens the changelog.
        ChangelogVersion =
            previous
            |> Option.filter (fun old -> SemVer.isOlderVersion old current)
            |> Option.map (fun _ -> current)
    }

    match SettingsStore.tryWriteSettingsFileAtomic settingsFileName (JS.JSON.stringify current) with
    | Ok() -> ()
    | Error message -> Browser.Dom.console.warn message

let checkForUpdate () = promise {
    try
        let! releases = loadAll swateReleaseRepository

        state <- {
            state with
                UpdateVersion =
                    tryFindUpdate state.CurrentVersion releases
                    |> Option.map (fun release -> release.tag_name)
        }

        for window in BrowserWindow.getAllWindows () do
            Main.WindowSend.send<IAppVersionRendererApi> window (fun api -> api.appVersionChanged state)
    with error ->
        // Offline startup or GitHub rate limits must never prevent the application from opening.
        Browser.Dom.console.warn ("Could not check for Swate updates", error.Message)
}
