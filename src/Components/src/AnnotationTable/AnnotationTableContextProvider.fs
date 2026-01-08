namespace Swate.Components

open Feliz
open Fable.Core

[<Erase; Mangle(false)>]
type AnnotationTableContextProvider =

    [<ReactComponent(true)>]
    static member AnnotationTableContextProvider(children: ReactElement) =

        let (data: Map<string, Contexts.AnnotationTable.AnnotationTableContext>), setData =
            React.useState (Map.empty)

        Contexts.AnnotationTable.AnnotationTableStateCtx.Provider({ state = data; setState = setData }, [ children ])