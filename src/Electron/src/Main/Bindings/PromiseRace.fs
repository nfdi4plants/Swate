module Main.Bindings.PromiseRace

open Fable.Core

/// Settles the same way as the first promise to settle.
[<Emit("Promise.race($0)")>]
let race (promises: Fable.Core.JS.Promise<'T>[]) : Fable.Core.JS.Promise<'T> = jsNative
