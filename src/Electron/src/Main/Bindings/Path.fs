module Main.Bindings.Path

open Fable.Core
open Fable.Core.JsInterop

[<Import("join", "path")>]
let join ([<ParamSeq>] paths: string[]) = jsNative