module Swate.Components.AnnotationTable.AnnotationTableContextProvider

open Feliz
open Fable.Core
open Swate.Components
open Swate.Components.AnnotationTable
open Swate.Components.AnnotationTable.AnnotationTableContext


[<Erase; Mangle(false)>]
type AnnotationTableContextProvider =

    [<ReactComponent(true)>]
    static member Init(children: ReactElement) =

        let (data: Map<string, AnnotationTableContext>), setData =
            React.useState (Map.empty)

        AnnotationTableContext.AnnotationTableStateCtx.Provider({ state = data; setState = setData }, [ children ])
