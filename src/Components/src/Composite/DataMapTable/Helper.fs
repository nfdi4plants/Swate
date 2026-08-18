module Swate.Components.Composite.DataMapTable.Helper

open ARCtrl
open Browser.Dom
open Fable.Core
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Composite.DataMapTable.Types
open Swate.Components.Composite.Table.Types

let copyCells (dataMap: Datamap) (coordinates: seq<CellCoordinate>) =
    coordinates |> dataMap.SelectedCellsToTabText |> navigator.clipboard.writeText

let updateDataMap (dataMap: Datamap) (setDataMap: Datamap -> unit) (update: Datamap -> unit) =
    // Always mutate a copy so memoized views compare against the unchanged current value.
    let nextDataMap = dataMap.Copy()
    update nextDataMap
    setDataMap nextDataMap

let pasteCells (dataMap: Datamap) (coordinate: CellCoordinate) (setDataMap: Datamap -> unit) = promise {
    let! clipboardText = navigator.clipboard.readText ()
    updateDataMap dataMap setDataMap _.PasteTabText(coordinate, clipboardText)
}
