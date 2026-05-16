[<AutoOpen>]
module Swate.Components.Composite.DataMapTable.Types

open Swate.Components.Composite.Table

[<AutoOpen>]
module ARCtrlExtensions =

    open ARCtrl
    open Swate.Components
    open Helper
    open ArcTableAux

    type DataMap with

        member this.ClearSelectedCells(selectHandle: SelectHandle) =
            match selectHandle.getCount () with
            | c when c <= 100 ->
                let selectedCells = selectHandle.getSelectedCells ()

                selectedCells |> Seq.iter (fun i -> this.Clear(i.x - 1, i.y - 1))
            | c ->
                for col in 0 .. this.ColumnCount - 1 do
                    for row in 0 .. this.RowCount - 1 do
                        if selectHandle.contains ({| x = col + 1; y = row + 1 |}) then
                            this.Clear(col, row)