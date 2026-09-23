module Main.Bindings.PromiseRace

open Fable.Core

/// Resolves with the first promise in the race to settle.
[<Emit("Promise.race($0)")>]
let race (promises: Fable.Core.JS.Promise<'T>[]) : Fable.Core.JS.Promise<'T> = jsNative
