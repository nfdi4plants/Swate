module IPC

open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.JS

let inline invoke(ipcServiceName:string, arguments: 'a) : Promise<'b> = Browser.Dom.window?ipc?invoke(ipcServiceName, arguments)

let invokeType<'b>(ipcServiceName:string, arguments) : Promise<'b> = Browser.Dom.window?ipc?invoke(ipcServiceName, arguments)

let inline on(ipcServiceName:string, dataHandler: obj -> unit) : unit = Browser.Dom.window?ipc?on(ipcServiceName, dataHandler)

let inline onType<'b>(ipcServiceName:string, dataHandler: 'b -> unit) : unit =
    Browser.Dom.console.log($"Registered handler for '{ipcServiceName}'")
    Browser.Dom.window?ipc?on(ipcServiceName, dataHandler)