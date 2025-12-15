namespace Swate.Components

open System
open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Dom
open Browser.Types


[<AutoOpen>]
module TypeHolder =

    type ARCPointer =
        {name: string; path: string; isActive: bool}

        static member create (name: string, path: string, isActive: bool) = { name = name; path = path; isActive = isActive }

    type ButtonInfo =
        { icon: string; toolTip: string; onClick: MouseEvent -> unit }

        static member create (icon: string, toolTip: string, (onClick: MouseEvent -> unit)) =
            { icon = icon; toolTip = toolTip; onClick = onClick }
