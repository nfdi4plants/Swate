module Swate.Components.ClipboardContract.Contract

open global.ARCtrl
open Swate.Components
open Swate.Components.ClipboardContract.Types

let LineBreaks = [| "\r\n"; "\n"; "\r" |]

module Capture =

    let matrix (getCell: CellCoordinate -> CompositeCell) (coordinates: seq<CellCoordinate>) =
        let coordinates = coordinates |> Seq.distinct |> Seq.toArray

        if Array.isEmpty coordinates then
            [||]
        else
            coordinates
            |> Array.groupBy _.y
            |> Array.sortBy fst
            |> Array.map (fun (_, row) -> row |> Array.sortBy _.x |> Array.map getCell)

    let toPlainText (cells: CompositeCell[][]) =
        cells
        |> Array.map (Array.map _.ToString() >> String.concat "\t")
        |> String.concat System.Environment.NewLine

module Mapping =

    let private modulo value length =
        let remainder = value % length
        if remainder < 0 then remainder + length else remainder

    let map (source: CompositeCell[][]) (anchor: CellCoordinate) (selection: CellCoordinate[]) =
        if
            Array.isEmpty source
            || source |> Array.exists Array.isEmpty
            || source |> Array.exists (fun row -> row.Length <> source.[0].Length)
        then
            invalidArg (nameof source) "The clipboard source must be a non-empty rectangular matrix."

        let sourceHeight = source.Length
        let sourceWidth = source.[0].Length

        let targets =
            if selection.Length > 1 then
                selection |> Array.distinct
            else
                Array.init
                    sourceHeight
                    (fun rowOffset ->
                        Array.init
                            sourceWidth
                            (fun columnOffset -> {|
                                x = anchor.x + columnOffset
                                y = anchor.y + rowOffset
                            |})
                    )
                |> Array.concat

        targets
        |> Array.sortBy (fun coordinate -> coordinate.y, coordinate.x)
        |> Array.map (fun target -> {
            Source = source.[modulo (target.y - anchor.y) sourceHeight].[modulo (target.x - anchor.x) sourceWidth]
            Target = target
        })
