module Api

open Swate.Electron.Shared.IPCTypes
open Fable.Electron.Remoting.Renderer

let arcVaultApi = Remoting.init |> Remoting.buildClient<IArcVaultsApi>
