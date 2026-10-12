module Main.Bindings.Menu

open Fable.Core
open Fable.Electron.Main
open Fable.Electron.Types

type MenuClickHandler = MenuItem * BaseWindow option * KeyboardEvent -> unit

/// Electron passes three arguments; the generated binding expects an F# tuple.
[<Emit("(item, window, event) => $0([item, window ?? undefined, event])")>]
let adaptClick (_callback: MenuClickHandler) : MenuClickHandler = jsNative
