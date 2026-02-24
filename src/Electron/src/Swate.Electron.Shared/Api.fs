module Api

open Swate.Components.Shared
open Swate.Electron.Shared.IPCTypes

open Fable.Core
open Fable.Core.JsInterop
open Fable.Remoting.Client
open Fable.Electron.Remoting.Renderer

let arcVaultApi = Remoting.init |> Remoting.buildClient<IArcVaultsApi>
