module Main.IPC.AvprApi

open Fable.Core
open Fable.Electron.Main
open Swate.Electron.Shared.IPCTypes
open Swate.Components.Composite.ValidationPackageSelector

/// Proxies the ARC validation package registry (AVPR) listing through Electron main. `net.fetch` goes through
/// Chromium's network stack, so the renderer never has to deal with CORS for the registry. The raw JSON is
/// handed to the renderer, which decodes it with the shared Components decoder.
let api: IAvprApi = {
    getPackages =
        fun () -> promise {
            try
                let! response = net.fetch (U2.Case1 AvprApi.PackagesUrl)

                if response.Ok then
                    let! json = response.text ()
                    return Ok json
                else
                    return Error(exn $"AVPR request failed with HTTP {response.Status}.")
            with error ->
                return Error error
        }
}
