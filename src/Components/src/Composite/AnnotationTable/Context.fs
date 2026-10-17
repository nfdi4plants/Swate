module ARCitISA.Components.Composite.AnnotationTable.Context

open Feliz
open ARCitISA.Components
open ARCitISA.Components
open ARCitISA.Components.Primitive


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
let useAnnotationTableStateCtx () =
    React.useContext AnnotationTableStateCtx
