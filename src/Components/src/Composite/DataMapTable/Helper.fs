module ARCitISA.Components.Composite.DataMapTable.Helper

open ARCtrl
open Browser.Dom
open Fable.Core
open ARCitISA.Components
open ARCitISA.Components.Shared
open ARCitISA.Components.Composite.DataMapTable.Types
open ARCitISA.Components.Composite.DataMapTable.ClipboardTarget
open ARCitISA.Components.Composite.Table.Types

let private captureCells
    (dataMap: DataMap)
    (coordinates: seq<CellCoordinate>)
    : ClipboardContract.Types.ClipboardContent =
    let coordinates =
        coordinates
        |> Seq.filter (fun coordinate -> coordinate.x > 0 && coordinate.y > 0)
        |> Seq.toArray

    let cells =
        ARCitISA.Components.ClipboardContract.Contract.Capture.matrix
            (fun coordinate -> dataMap.GetCell(coordinate.x - 1, coordinate.y - 1))
            coordinates

    {
        PlainText = ARCitISA.Components.ClipboardContract.Contract.Capture.toPlainText cells
        Cells = Some cells
    }

let copyCells (dataMap: DataMap) (coordinates: seq<CellCoordinate>) =
    let content = captureCells dataMap coordinates
    ARCitISA.Components.ClipboardCodec.write content.PlainText content.Cells

let updateDataMap (dataMap: DataMap) (setDataMap: DataMap -> unit) (update: DataMap -> unit) =
    // Always mutate a copy so memoized views compare against the unchanged current value.
    let nextDataMap = DataMapCopyWorkaround.copy dataMap
    update nextDataMap
    setDataMap nextDataMap

let cutCells (dataMap: DataMap) (coordinates: CellCoordinate[]) (setDataMap: DataMap -> unit) =
    ARCitISA.Components.ClipboardContract.Actions.cut
        (captureCells dataMap coordinates)
        (fun () -> updateDataMap dataMap setDataMap _.ClearCells(coordinates))

let pasteFromClipboard
    (dataMap: DataMap)
    (coordinate: CellCoordinate)
    (selection: CellCoordinate[])
    (setDataMap: DataMap -> unit)
    =
    promise {
        let! content = ARCitISA.Components.ClipboardCodec.read ()

        updateDataMap
            dataMap
            setDataMap
            (fun nextDataMap ->
                match content.Cells with
                | Some cells -> nextDataMap.PasteStructuredCells(coordinate, selection, cells)
                | None -> nextDataMap.PasteTabText(coordinate, selection, content.PlainText)
            )
    }
