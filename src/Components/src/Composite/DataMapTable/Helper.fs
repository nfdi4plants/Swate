module Swate.Components.Composite.DataMapTable.Helper

open ARCtrl
open Browser.Dom
open Fable.Core
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Composite.DataMapTable.Types
open Swate.Components.Composite.DataMapTable.ClipboardTarget
open Swate.Components.Composite.Table.Types

let copyCells (dataMap: DataMap) (coordinates: seq<CellCoordinate>) =
    let coordinates =
        coordinates
        |> Seq.filter (fun coordinate -> coordinate.x > 0 && coordinate.y > 0)
        |> Seq.toArray

    let cells =
        Swate.Components.ClipboardContract.Contract.Capture.matrix
            (fun coordinate -> dataMap.GetCell(coordinate.x - 1, coordinate.y - 1))
            coordinates

    let plainText =
        Swate.Components.ClipboardContract.Contract.Capture.toPlainText cells

    Swate.Components.ClipboardCodec.write plainText (Some cells)

let updateDataMap (dataMap: DataMap) (setDataMap: DataMap -> unit) (update: DataMap -> unit) =
    // Always mutate a copy so memoized views compare against the unchanged current value.
    let nextDataMap = DataMapCopyWorkaround.copy dataMap
    update nextDataMap
    setDataMap nextDataMap

let pasteFromClipboard
    (dataMap: DataMap)
    (coordinate: CellCoordinate)
    (selection: CellCoordinate[])
    (setDataMap: DataMap -> unit)
    =
    promise {
        let! content = Swate.Components.ClipboardCodec.read ()

        updateDataMap
            dataMap
            setDataMap
            (fun nextDataMap ->
                match content.Cells with
                | Some cells -> nextDataMap.PasteStructuredCells(coordinate, selection, cells)
                | None -> nextDataMap.PasteTabText(coordinate, content.PlainText)
            )
    }
