
#r "nuget: Fable.Core"
#r "nuget: ARCtrl"
#r "nuget: ARCtrl.Spreadsheet"
#r "nuget: FSharp.Data"

open Swate.Components.Shared
open Swate.Components
open Database
open Swate.Components.Shared
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI
open ARCtrl
open ARCtrl.Spreadsheet

let fromTableStr (content: string [], headers: CompositeHeader []) =
    //let content = str.Split('\t') |> Array.map _.Trim()
    let expectedLength, termCount =
        let expectedLength, termCount =
            headers
            |> Array.map (fun header ->
                match header with
                | item when item.IsSingleColumn -> 1, 0
                | item when item.IsDataColumn -> 4, 0
                | item when item.IsTermColumn -> 0, 1
            )
            |> Array.unzip
        expectedLength |> Array.sum,
        termCount |> Array.sum

    let expectedTermLength = expectedLength + (3 * termCount)
    let expectedUnitLength = expectedLength + (4 * termCount)

    printfn "expectedTermLength: %i" expectedTermLength

    let parseRow (row: string []) (headers: CompositeHeader []) =
        let rec loop index result =
            if index >= headers.Length then
                result |> List.rev |> Array.ofList
            else
                let header = headers.[index]
                match header with
                | x when x.IsSingleColumn ->
                    let cell = CompositeCell.fromContentValid([|row.[index]|], header)
                    loop (index + 1) (cell::result)
                | x when x.IsDataColumn ->
                    let content = Array.sub row index 4
                    let cell = CompositeCell.fromContentValid(content, header)
                    loop (index + 4) (cell::result)
                | x when x.IsTermColumn ->
                    let content = Array.sub row index 3
                    let cell = CompositeCell.fromContentValid(content, header)
                    loop (index + 3) (cell::result)
        loop 0 []
            
    //match content.Length with
    //| length when length = expectedTermLength -> ()
    //| length when length = expectedUnitLength -> ()
    //| _ -> ()

    parseRow content headers

let paste ((columnIndex, rowIndex): (int * int), table: ArcTable, selectHandle: SelectHandle, setTable) =
    promise {
        let! copiedValue = navigator.clipboard.readText()
        let rows =
            copiedValue.Split([|System.Environment.NewLine|], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun item ->
                item.Split('\t')
                |> Array.map _.Trim())

        //Check amount of selected cells
        //When multiple cells are selected a different handling is required
        if selectHandle.getCount() > 1 then

            //Convert cell coordinates to array
            let cellCoordinates =
                selectHandle.getSelectedCells()
                |> Array.ofSeq

            //Get allr required headers for cells
            let headers =
                let columnIndices =
                    cellCoordinates
                    |> Array.distinctBy (fun item -> item.x)
                columnIndices
                |> Array.map (fun index -> table.GetColumn(index.x - 1).Header)

            //Recalculates the index, then the amount of selected cells is bigger than the amount of copied cells
            let getIndex startIndex length =
                let rec loop index length =
                    if index < length then
                        index
                    else
                        loop (index - length) length
                loop startIndex length

            //Converts the cells of each row
            let rowCells =
                rows
                |> Array.map (fun row ->
                    CompositeCell.fromTableStr(row, headers))

            //Group all cells based on their row
            let groupedCellCoordinates =
                cellCoordinates
                |> Array.ofSeq
                |> Array.groupBy (fun item -> item.y)

            //Map over all selected cells
            groupedCellCoordinates
            |> Array.iteri (fun yi (_, row) ->
                //Restart row index, when the amount of selected rows is bigger than copied rows
                let yIndex = getIndex yi rowCells.Length
                row
                |> Array.iteri (fun xi coordinate ->
                    //Restart column index, when the amount of selected columns is bigger than copied columns
                    let xIndex = getIndex xi rowCells.[0].Length
                    table.SetCellAt(coordinate.x - 1, coordinate.y - 1, rowCells.[yIndex].[xIndex])
                )
            )
        else
            let selectedHeader = table.GetColumn(columnIndex).Header
            let newCell = CompositeCell.fromTableStr(rows.[0], [|selectedHeader|])
            table.SetCellAt(columnIndex, rowIndex, newCell.[0])
        table.Copy()
        |> setTable
    }
