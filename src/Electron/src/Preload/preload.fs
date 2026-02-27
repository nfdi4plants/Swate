module Preload

open Fable.Electron.Remoting.Preload
open Swate.Electron.Shared.IPCTypes

Remoting.init |> Remoting.buildTwoWayBridge<IArcVaultsApi>

Remoting.init |> Remoting.buildTwoWayBridge<ISaveBeforeQuitApi>

Remoting.init |> Remoting.buildBridge<IMainUpdateRendererApi>

Remoting.init |> Remoting.buildBridge<IArcFileWatcherApi>

Remoting.init |> Remoting.buildBridge<IMainSaveBeforeQuitApi>