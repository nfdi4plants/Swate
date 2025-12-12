module Preload

open Swate.Electron.Shared
open Fable.Electron.Remoting.Preload
open Swate.Electron.Shared.IPCTypes

Remoting.init |> Remoting.buildTwoWayBridge<IPCTypes.IArcVaultsApi>

Remoting.init
|> Remoting.buildBridge<IMainUpdateRendererApi>