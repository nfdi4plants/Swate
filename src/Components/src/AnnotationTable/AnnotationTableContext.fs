module Swate.Components.AnnotationTable.AnnotationTableContext

open Feliz
open Swate.Components.Types


type AnnotationTableContext = {
    SelectedCells: CellCoordinateRange option
} with

    static member init(?selectedCells) = { SelectedCells = selectedCells }

let AnnotationTableStateCtx =
    React.createContext<StateContext<Map<string, AnnotationTableContext>>> (
        {
            state = Map.empty
            setState = fun _ -> ()
        }
    )

[<Hook>]
let useAnnotationTableCtx () = React.useContext AnnotationTableStateCtx
     