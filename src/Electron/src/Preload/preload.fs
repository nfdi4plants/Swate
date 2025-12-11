module Preload

open Swate.Electron.Shared
open Fable.Electron.Remoting.Preload

Remoting.init |> Remoting.buildTwoWayBridge<IPCTypes.IStartUpApi>

Remoting.init |> Remoting.buildTwoWayBridge<IPCTypes.IARCIOApi>