module Main.Bindings.Node

open Fable.Core
open Fable.Core.JsInterop

let childProcessDynamic: obj = importAll "node:child_process"

[<Emit("$0.execFile($1, $2, $3, $4)")>]
let execFile
    (childProcess: obj)
    (file: string)
    (arguments: string[])
    (options: obj)
    (callback: obj -> obj -> obj -> unit)
    : obj =
    jsNative

[<Emit("$0 && typeof $0.code === 'number' ? $0.code : -1")>]
let execFileErrorExitCode (error: obj) : int = jsNative

[<Emit("$0 && $0.message ? $0.message : String($0)")>]
let execFileErrorMessage (error: obj) : string = jsNative

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
