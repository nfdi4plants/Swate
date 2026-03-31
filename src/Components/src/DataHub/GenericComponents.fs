module Swate.Components.DataHubComponents

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types

open DataHubTypes

type DataHubComponents =

    [<ReactComponent>]
    static member SectionHeading(text: string) =
        Html.h3 [
            prop.className "swt:text-sm swt:font-semibold swt:text-base-content/70 swt:uppercase swt:tracking-wide"
            prop.text text
        ]