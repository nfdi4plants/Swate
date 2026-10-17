namespace ARCitISA.Components.Composite.AnnotationTable

open Feliz
open Fable.Core
open ARCitISA.Components
open ARCitISA.Components.Primitive
open ARCitISA.Components.Composite.AnnotationTable
open ARCitISA.Components.Composite.AnnotationTable.Context

[<Erase; Mangle(false)>]
type AnnotationTableContextProvider =

    [<ReactComponent(true)>]
    static member AnnotationTableContextProvider(children: ReactElement) =

        let (data: Map<string, AnnotationTableContext>), setData =
            React.useState (Map.empty)

        AnnotationTableStateCtx.Provider({ state = data; setState = setData }, [ children ])
