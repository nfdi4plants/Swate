module Api

open Swate.Electron.Shared.IPCTypes
open Fable.Electron.Remoting.Renderer

let startUpApi = Remoting.init |> Remoting.buildClient<IStartUpApi>

let arcIOApi = Remoting.init |> Remoting.buildClient<IARCIOApi>