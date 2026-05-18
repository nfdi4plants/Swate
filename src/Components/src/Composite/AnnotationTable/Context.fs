module Swate.Components.Composite.AnnotationTable.Context

open Feliz
open Swate.Components
open Swate.Components
open Swate.Components.Primitive


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
     
