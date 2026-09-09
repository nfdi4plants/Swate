module Swate.Components.Composite.DataMapTable.Helper

open ARCtrl
open Browser.Dom
open Fable.Core
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Composite.DataMapTable.Types
open Swate.Components.Composite.Table.Types

module Clipboard = Swate.Components.ClipboardCodec

let copyCells (dataMap: DataMap) (coordinates: seq<CellCoordinate>) =
    let coordinates = coordinates |> Seq.toArray
    let plainText = dataMap.SelectedCellsToTabText coordinates
    let cells = dataMap.GetSelectedCells coordinates
    Clipboard.write plainText (Some cells)

let updateDataMap (dataMap: DataMap) (setDataMap: DataMap -> unit) (update: DataMap -> unit) =
    // Always mutate a copy so memoized views compare against the unchanged current value.
    let nextDataMap = copyDataMapPreservingLabelsWorkaround dataMap
    update nextDataMap
    setDataMap nextDataMap

let pasteCells (dataMap: DataMap) (coordinate: CellCoordinate) (setDataMap: DataMap -> unit) = promise {
    let! content = Clipboard.read ()

    updateDataMap
        dataMap
        setDataMap
        (fun nextDataMap ->
            match content.Payload with
            | Some payload -> nextDataMap.PastePayload(coordinate, payload)
            | None -> nextDataMap.PasteTabText(coordinate, content.PlainText)
        )
}
