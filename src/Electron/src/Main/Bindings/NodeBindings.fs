module Main.Bindings.Node

open System
open Fable.Core
open Fable.Electron
open Fable.Core.JsInterop

let childProcessDynamic: obj = importAll "node:child_process"

[<Import("cpus", "node:os")>]
let cpus () : obj[] = jsNative

type RawIpcMain =
    /// Registers a raw IPC listener for payloads that are not exposed through the typed remoting bridge.
    abstract on: channel: string * listener: Action<IpcMainEvent, obj> -> unit

[<Import("ipcMain", "electron")>]
let ipcMain: RawIpcMain = jsNative

[<AllowNullLiteral>]
type NodeError =
    abstract member code: string

let tryGetErrorCode (error: exn) : string option =
    try
        let code = (unbox<NodeError> error).code

        if String.IsNullOrWhiteSpace code then None else Some code
    with _ ->
        None

[<Emit("process.platform")>]
let processPlatform () : string = jsNative

[<Emit("$0.length")>]
let bufferLength (buffer: obj) : int = jsNative

[<Emit("$0[$1]")>]
let bufferByteAt (buffer: obj) (index: int) : int = jsNative

[<Emit("Buffer.concat($0)")>]
let bufferConcat (buffers: obj[]) : obj = jsNative

[<Emit("$0.subarray($1, $2)")>]
let bufferSubarray (buffer: obj) (startIndex: int) (endIndex: int) : obj = jsNative

[<Emit("$0.toString('utf8')")>]
let bufferToUtf8String (buffer: obj) : string = jsNative
