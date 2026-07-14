namespace Swate.Components.JsBindings

open Browser.Types
open Fable.Core

[<AllowNullLiteral; Global>]
type ResizeObserver(callback: unit -> unit) =

    member _.observe(_target: HTMLElement) : unit = jsNative

    member _.disconnect() : unit = jsNative
