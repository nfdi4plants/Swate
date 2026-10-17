namespace ARCitISA.Components

open Fable.Core
open Fable.Core.JsInterop

[<Erase>]
type ARCitISAStyle =
    static member import = importSideEffects "./arcitisa-components.css"
