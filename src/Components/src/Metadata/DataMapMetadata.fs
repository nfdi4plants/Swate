namespace Swate.Components.Metadata

open Fable.Core
open Feliz
open ARCtrl
open Swate.Components.Metadata

[<Erase; Mangle(false)>]
type DataMapMetadata =

    [<ReactComponent(true)>]
    static member DataMapMetadata
        // 👀 If you rename these variables, ensure that the names are forwarded for lazy loading in `src\Components\src\Metadata\ArcFileMetadata.fs` as well!
        (datamap: DataMap)
        =
        Generic.Section [
            Generic.BoxedField("DataMap", description = $"Data Contexts: {datamap.DataContexts.Count}")
        ]