module Swate.Components.AnnotationTable.Context

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
let useAnnotationTableStateCtx () = React.useContext AnnotationTableStateCtx
