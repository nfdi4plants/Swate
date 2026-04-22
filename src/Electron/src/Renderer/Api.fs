module Api

open Swate.Components.Shared
open Swate.Electron.Shared.IPCTypes

open Fable.Core
open Fable.Core.JsInterop
open Fable.Remoting.Client
open Fable.Electron.Remoting.Renderer

let ipcGitApi = Remoting.init |> Remoting.buildClient<IGitApi>
let ipcGitLabApi = Remoting.init |> Remoting.buildClient<IGitLabApi>
let ipcArcVaultApi = Remoting.init |> Remoting.buildClient<IArcVaultsApi>
let ipcAuthApi = Remoting.init |> Remoting.buildClient<IAuthApi>
let ipcRendererBridgeSyncApi = Remoting.init |> Remoting.buildClient<IRendererBridgeSyncApi>
