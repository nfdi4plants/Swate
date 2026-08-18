module Swate.Components.Composite.DataMapTable.Helper

open ARCtrl
open Browser.Dom
open Fable.Core
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Composite.DataMapTable.Types
open Swate.Components.Composite.Table.Types

let copyCells (dataMap: DataMap) (coordinates: seq<CellCoordinate>) =
    coordinates |> dataMap.SelectedCellsToTabText |> navigator.clipboard.writeText

let updateDataMap (dataMap: DataMap) (setDataMap: DataMap -> unit) (update: DataMap -> unit) =
    // Always mutate a copy so memoized views compare against the unchanged current value.
    let nextDataMap = dataMap.Copy()
    preserveDataMapLabelsWorkaround dataMap nextDataMap
    update nextDataMap
    setDataMap nextDataMap

let pasteCells (dataMap: DataMap) (coordinate: CellCoordinate) (setDataMap: DataMap -> unit) = promise {
    let! clipboardText = navigator.clipboard.readText ()
    updateDataMap dataMap setDataMap _.PasteTabText(coordinate, clipboardText)
}
