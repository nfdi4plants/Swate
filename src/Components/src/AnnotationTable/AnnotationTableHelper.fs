namespace Swate.Components

open System
open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Feliz
open Types.AnnotationTableContextMenu

type AnnotationTableHelper =

    //Recalculates the index, then the amount of selected cells is bigger than the amount of copied cells
    static member getIndex(startIndex, length) =
        let rec loop index length =
            if index < length then
                index
            else
                loop (index - length) length

        loop startIndex length

    static member getCopiedCells() = promise {
        let! copiedValue = navigator.clipboard.readText ()

        let rows =
            copiedValue.Split([| System.Environment.NewLine |], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun item -> item.Split('\t') |> Array.map _.Trim())

        return rows
    }

    static member getFittedCells(data: string[][], headers: CompositeHeader[]) =

        let fitColumnsToTarget (row: string[][]) (headers: CompositeHeader[]) =

            let rec loop index result =
                if index >= headers.Length then
                    result |> List.rev |> Array.ofList
                else
                    let header = headers.[index]

                    let newIndex =
                        if index >= row.Length then
                            AnnotationTableHelper.getIndex (index, row.Length)
                        else
                            index

                    match header with
                    | x when x.IsSingleColumn ->
                        let cell = CompositeCell.fromContentValid (row.[newIndex], header)
                        loop (index + 1) (cell :: result)
                    | x when x.IsDataColumn ->
                        let cell = CompositeCell.fromContentValid (row.[newIndex], header)
                        loop (index + 1) (cell :: result)
                    | x when x.IsTermColumn ->
                        let value =
                            if row.[newIndex].Length = 2 then
                                [| row.[newIndex].[0] |]
                            else
                                row.[newIndex]

                        let cell = CompositeCell.fromContentValid (value, header)
                        loop (index + 1) (cell :: result)
                    | anyElse -> failwith $"Error-fitColumnsToTarget: Encountered unsupported case: {anyElse}"

            loop 0 []

        let getHeadersWithLength (headers: CompositeHeader[]) =
            headers
            |> Array.map (fun header ->
                match header with
                | x when x.IsSingleColumn -> x.ToString(), [ 1 ]
                | x when x.IsDataColumn -> x.ToString(), [ 4 ]
                | x when x.IsTermColumn -> x.ToString(), [ 1; 2; 3; 4 ]
                | anyElse -> failwith $"Error-getHeadersWithLength: Encountered unsupported case: {anyElse}"
            )

        let fitHeaders (strings: string[]) (headersSizes: (string * int list)[]) =
            let rec tryFit (cell: string[]) index (headerSizesList: (string * int list) list) =
                let index = if index >= strings.Length then 0 else index

                match headerSizesList with
                | [] -> Some []
                | (name, sizes) :: rest ->
                    sizes
                    |> List.sortDescending
                    |> List.choose (fun size ->
                        let actualSize =
                            if index + size <= cell.Length then size
                            elif index < cell.Length then cell.Length - index
                            else 0

                        if actualSize > 0 then
                            let segment = cell.[index .. index + actualSize - 1]

                            match tryFit cell (index + actualSize) rest with
                            | Some restResult -> Some((name, segment) :: restResult)
                            | None -> None
                        else
                            None
                    )
                    |> List.tryHead

            tryFit strings 0 (Array.toList headersSizes)

        let fittingHeaders = getHeadersWithLength headers

        let fittedRows = data |> Array.map (fun row -> fitHeaders row fittingHeaders)

        let result =
            fittedRows
            |> Array.choose (fun row -> row)
            |> Array.map (fun row -> row |> List.map (fun (_, cells) -> cells) |> Array.ofList)

        result |> Array.map (fun row -> fitColumnsToTarget row headers)

    static member predictPasteBehaviour
        (cellIndex: CellCoordinate, targetTable: ArcTable, selectHandle: SelectHandle, data: string[][])
        =

        //Convert cell coordinates to array
        let cellCoordinates = selectHandle.getSelectedCells () |> Array.ofSeq

        //Get all required headers for cells
        let headers =
            let columnIndices = cellCoordinates |> Array.distinctBy (fun item -> item.x)

            columnIndices
            |> Array.map (fun index -> targetTable.GetColumn(index.x - 1).Header)

        let checkForHeaders (row: string[]) =
            let headers = ARCtrl.CompositeHeader.Cases |> Array.map (fun (_, header) -> header)

            let areHeaders =
                headers
                |> Array.collect (fun header -> row |> Array.map (fun cell -> cell.StartsWith(header)))

            Array.contains true areHeaders

        if checkForHeaders data.[0] then

            let body =
                let rest = data.[1..]
                if rest.Length > 0 then rest else [||]

            let columns = Array.append [| data.[0] |] body |> Array.transpose
            let columnsList = columns |> Seq.toArray |> Array.map (Seq.toArray)

            let compositeColumns =
                ARCtrl.Spreadsheet.ArcTable.composeColumns columnsList |> ResizeArray

            PasteCases.AddColumns {|
                data = compositeColumns
                columnIndex = cellIndex.x
            |}
        else

            //Group all cells based on their row
            let groupedCellCoordinates =
                cellCoordinates
                |> Array.ofSeq
                |> Array.groupBy (fun item -> item.y)
                |> Array.map (fun (_, row) -> row)

            let fittedCells = AnnotationTableHelper.getFittedCells (data, headers)

            let isEmpty =
                Array.isEmpty fittedCells
                || fittedCells
                   |> Array.map (fun row ->
                       Array.isEmpty row
                       || Array.forall (fun (cell: CompositeCell) -> String.IsNullOrWhiteSpace(cell.ToTabStr())) row
                   )
                   |> Array.contains true

            if isEmpty then
                PasteCases.Unknown {| data = data; headers = headers |}
            else
                PasteCases.PasteColumns {|
                    data = fittedCells
                    coordinates = groupedCellCoordinates
                |}

    static member pasteDefault
        (
            pasteColumns:
                {|
                    data: CompositeCell[][]
                    coordinates: CellCoordinate[][]
                |},
            coordinate: CellCoordinate,
            table: ArcTable,
            selectHandle: SelectHandle,
            setTable
        ) =

        let getCorrectTarget
            (currentCell: CompositeCell)
            (table: ArcTable)
            (targetCoordinate: CellCoordinate)
            adaption
            =
            if currentCell.isUnitized then
                let targetCell =
                    table.GetCellAt(targetCoordinate.x - adaption, targetCoordinate.y - adaption)

                let value, unit = currentCell.AsUnitized

                if targetCell.isUnitized && unit.isEmpty () then
                    let _, targetUnit = targetCell.AsUnitized
                    let newTarget = CompositeCell.createUnitized (value, targetUnit)
                    newTarget
                elif targetCell.isTerm && unit.isEmpty () then
                    let targetTerm = targetCell.AsTerm
                    let newTarget = CompositeCell.createUnitized (value, targetTerm)
                    newTarget
                else
                    currentCell
            else
                currentCell

        //Check amount of selected cells
        //When multiple cells are selected a different handling is required
        if selectHandle.getCount () > 1 then

            //Map over all selected cells
            pasteColumns.coordinates
            |> Array.iteri (fun yi row ->
                //Restart row index, when the amount of selected rows is bigger than copied rows
                let yIndex = AnnotationTableHelper.getIndex (yi, pasteColumns.data.Length)

                row
                |> Array.iteri (fun xi coordinate ->
                    //Restart column index, when the amount of selected columns is bigger than copied columns
                    let xIndex = AnnotationTableHelper.getIndex (xi, pasteColumns.data.[0].Length)

                    let currentCell = pasteColumns.data.[yIndex].[xIndex]
                    let newTarget = getCorrectTarget currentCell table coordinate 1
                    table.SetCellAt(coordinate.x - 1, coordinate.y - 1, newTarget)
                )
            )
        else
            let currentCell = pasteColumns.data.[0].[0]
            let newTarget = getCorrectTarget currentCell table coordinate 0
            table.SetCellAt(coordinate.x - 1, coordinate.y - 1, newTarget)

        table.Copy() |> setTable

    static member paste
        (
            pasteCases: PasteCases,
            coordinate: CellCoordinate,
            table: ArcTable,
            setModal: Types.AnnotationTable.ModalTypes option -> unit,
            selectHandle: SelectHandle,
            setTable: ArcTable -> unit
        ) =

        match pasteCases with
        | AddColumns addColumns ->
            setModal (
                AnnotationTable.ModalTypes.PasteCaseUserInput(PasteCases.AddColumns addColumns)
                |> Some
            )
        | PasteColumns pasteColumns ->
            AnnotationTableHelper.pasteDefault (pasteColumns, coordinate, table, selectHandle, setTable)
        | Unknown unknownPasteCase ->
            setModal (
                AnnotationTable.ModalTypes.UnknownPasteCase(PasteCases.Unknown unknownPasteCase)
                |> Some
            )

    static member tryPasteCopiedCells
        (
            cellIndex: CellCoordinate,
            arcTable: ArcTable,
            selectHandle: SelectHandle,
            setModal: AnnotationTable.ModalTypes option -> unit,
            setArcTable: ArcTable -> unit
        ) =
        promise {
            let! data = AnnotationTableHelper.getCopiedCells ()

            try
                let prediction =
                    AnnotationTableHelper.predictPasteBehaviour (cellIndex, arcTable, selectHandle, data)

                AnnotationTableHelper.paste (prediction, cellIndex, arcTable, setModal, selectHandle, setArcTable)
            with exn ->
                setModal (AnnotationTable.ModalTypes.Error(exn.Message) |> Some)
        }