module Main.IPC.IAppVersionApi

open Fable.Electron.Main
open Swate.Components.Api.GitHubReleases
open Swate.Electron.Shared.IPCTypes
open Main.Settings.AppVersion

let api: IAppVersionApi = {
    getAppVersion = fun () -> promise { return getState () }
    openUpdate =
        fun () -> promise {
            let state = getState ()

            match state.UpdateVersion with
            | Some tag ->
                try
                    do! shell.openExternal (releaseUrl swateReleaseRepository tag)
                    return Ok()
                with error ->
                    return Error error
            | None -> return Ok()
        }
}
