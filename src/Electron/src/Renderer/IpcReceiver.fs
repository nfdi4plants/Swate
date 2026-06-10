module Renderer.IpcReceiver

open Fable.Electron.Remoting.Renderer
open Feliz

let inline subscribeProxyReceiver<'T> (handler: 'T) : unit -> unit =
    Remoting.createIpc () |> Remoting.buildProxyReceiverDisposable handler

[<Hook>]
let inline useProxyReceiver<'T> (createHandler: unit -> 'T, dependencies: obj[]) : unit =
    React.useEffect ((fun () -> createHandler () |> subscribeProxyReceiver<'T>), dependencies)
