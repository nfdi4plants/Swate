module Swate.Components.JsBindings.ReactHooks

open Fable.Core

/// React.useLayoutEffect for an effect with a cleanup. In Feliz 3.1.1 the `unit -> (unit -> unit)` overload
/// carries ImportMember, so React receives the uncurried lambda and runs the cleanup right after the setup.
/// The IDisposable overload hands React an object. This binding calls React directly.
///
/// The setup returns the cleanup function boxed, or `box ()` when there is none.
[<Import("useLayoutEffect", "react")>]
let useLayoutEffectWithCleanup (setup: unit -> obj, dependencies: obj[]) : unit = jsNative
