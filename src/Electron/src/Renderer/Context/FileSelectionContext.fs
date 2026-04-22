module Renderer.Context.FileSelectionContext

open Feliz
open Swate.Components.Shared

type FileSelectionController = {
    selection: ArcSelection
    setSelection: ArcSelection -> unit
    updateSelection: (ArcSelection -> ArcSelection) -> unit
}

let DefaultFileSelectionController = {
    selection = ArcSelection.empty
    setSelection = ignore
    updateSelection = ignore
}

let FileSelectionCtx = React.createContext<FileSelectionController> DefaultFileSelectionController

[<Hook>]
let useFileSelectionCtx () = React.useContext FileSelectionCtx
