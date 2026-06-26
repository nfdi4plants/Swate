module Main.Bindings.Node

open System
open Fable.Core
open Fable.Core.JsInterop

let childProcessDynamic: obj = importAll "node:child_process"

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
