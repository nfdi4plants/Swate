namespace ARCitISA.Components.Page.Metadata

open Fable.Core
open Feliz
open ARCtrl
open ARCitISA.Components
open ARCitISA.Components.Primitive.LayoutComponents

[<Erase; Mangle(false)>]
type DataMapMetadata =

    [<ReactComponent(true)>]
    static member DataMapMetadata
        // 👀 If you rename these variables, ensure that the names are forwarded for lazy loading in `src\Components\src\Metadata\ArcFileMetadata.fs` as well!
        (datamap: DataMap)
        =
        ARCitISA.Components.Primitive.LayoutComponents.LayoutComponents.Section [
            ARCitISA.Components.Primitive.LayoutComponents.LayoutComponents.BoxedField(
                "DataMap",
                description = $"Data Contexts: {datamap.DataContexts.Count}"
            )
        ]
