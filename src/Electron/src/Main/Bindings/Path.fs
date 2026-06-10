module Main.Bindings.Path

open Fable.Core

[<Import("join", "path")>]
let join ([<ParamSeq>] paths: string[]) : string = jsNative

[<Import("resolve", "path")>]
let resolve ([<ParamSeq>] paths: string[]) : string = jsNative

[<Import("relative", "path")>]
let relative (fromPath: string) (toPath: string) : string = jsNative

[<Import("isAbsolute", "path")>]
let isAbsolute (path: string) : bool = jsNative

[<Import("dirname", "path")>]
let dirname (path: string) : string = jsNative

[<Import("basename", "path")>]
let basename (path: string) : string = jsNative

[<Import("extname", "path")>]
let extname (path: string) : string = jsNative
