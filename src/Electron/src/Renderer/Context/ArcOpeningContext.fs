module Renderer.Context.ArcOpeningContext

open Feliz

type ArcOpeningController = {
    isOpeningArc: bool
    openArc: unit -> unit
    openArcByPath: string -> unit
}

let ArcOpeningCtx =
    React.createContext<ArcOpeningController> {
        isOpeningArc = false
        openArc = ignore
        openArcByPath = ignore
    }

[<Hook>]
let useArcOpeningCtx () = React.useContext ArcOpeningCtx
