module Main.Bindings.Path

open Fable.Core

[<Import("join", "path")>]
let join: path1: string * path2: string -> string = jsNative