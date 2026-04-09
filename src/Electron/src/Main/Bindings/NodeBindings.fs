module Main.Bindings.Node

open Fable.Core
open Fable.Core.JsInterop

let childProcessDynamic: obj = importAll "node:child_process"
let fsPromisesDynamic: obj = importAll "fs/promises"
let pathDynamic: obj = importAll "path"

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
