module Api

open Swate.Components.Shared
open Swate.Electron.Shared.IPCTypes

open Fable.Core
open Fable.Remoting.Client
open Fable.Electron.Remoting.Renderer

let ipcGitApi = Remoting.createIpc () |> Remoting.buildProxySender<IGitApi>
let ipcGitLabApi = Remoting.createIpc () |> Remoting.buildProxySender<IGitLabApi>

let ipcArcVaultApi =
    Remoting.createIpc () |> Remoting.buildProxySender<IArcVaultsApi>

let ipcAuthApi = Remoting.createIpc () |> Remoting.buildProxySender<IAuthApi>
