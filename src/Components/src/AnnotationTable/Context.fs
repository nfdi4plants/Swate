namespace Swate.Components.AnnotationTable

open Feliz
open Swate.Components.Types
open Fable.Core

type AnnotationTableContext = {
    SelectedCells: CellCoordinateRange option
} with

    static member init(?selectedCells) = { SelectedCells = selectedCells }

    static member AnnotationTableStateCtx =
        React.createContext<StateContext<Map<string, AnnotationTableContext>>> (
            {
                state = Map.empty
                setState = fun _ -> ()
            }
        )

    [<Hook>]
    static member useAnnotationTableCtx () = React.useContext AnnotationTableContext.AnnotationTableStateCtx

[<Erase; Mangle(false)>]
type AnnotationTableContextProvider =

    [<ReactComponent(true)>]
    static member Init(children: ReactElement) =

        let (data: Map<string, AnnotationTableContext>), setData =
            React.useState (Map.empty)

        AnnotationTableContext.AnnotationTableStateCtx.Provider({ state = data; setState = setData }, [ children ])
