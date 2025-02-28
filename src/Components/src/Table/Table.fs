namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI


[<Mangle(false); Erase>]
type TermSearch =

    [<ReactComponent(true)>]
    static member Table() =
        Html.div [
            prop.text "12"
        ]
