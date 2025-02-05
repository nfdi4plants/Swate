namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop

[<Erase>]
type SwateStyle =
    static member import = importSideEffects "./swateBundleStyle.css"