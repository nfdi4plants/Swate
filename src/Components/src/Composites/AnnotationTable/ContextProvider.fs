namespace Swate.Components.AnnotationTable

open Feliz
open Fable.Core
open Swate.Components
open Swate.Components.Primitives
open Swate.Components.AnnotationTable
open Swate.Components.AnnotationTable.Context


[<Erase; Mangle(false)>]
type AnnotationTableContextProvider =

    [<ReactComponent(true)>]
    static member AnnotationTableContextProvider(children: ReactElement) =

        let (data: Map<string, AnnotationTableContext>), setData =
            React.useState (Map.empty)

        AnnotationTableStateCtx.Provider({ state = data; setState = setData }, [ children ])