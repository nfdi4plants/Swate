module CustomComponents.Loading

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome


let loadingComponent =
    Fa.i [
        Fa.Solid.Spinner
        Fa.Pulse
        Fa.Size Fa.Fa4x
    ] []