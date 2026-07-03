namespace Swate.Components.Composite.AnnotationTable

open System
open System.Text.RegularExpressions
open ARCtrl

open Swate.Components
open Types.AnnotationTableContextMenu
open Swate.Components.Composite.Table
open Swate.Components.Composite.Table.Types
open Swate.Components.Composite.AnnotationTable.Types

type AnnotationTableClipboard =

    static member clear(cellIndex: CellCoordinate, table: ArcTable, selectHandle: SelectHandle) : ArcTable =
        let nextTable = table.Copy()

        if selectHandle.contains cellIndex then
            nextTable.ClearSelectedCells(selectHandle)
        else
            nextTable.ClearCell(cellIndex)

        nextTable

    static member copy(cellIndex: CellCoordinate, table: ArcTable, selectHandle: SelectHandle) =
        let result =
            if selectHandle.getCount () > 1 then

                let cellCoordinates =
                    selectHandle.getSelectedCells ()
                    |> Array.ofSeq
                    |> Array.groupBy (fun item -> item.y)

                let headers, cells =
                    cellCoordinates
                    |> Array.map (fun (_, row) ->
                        row
                        |> Array.partition (fun coordinate -> (coordinate.y - 1) < 0)
                        |> fun (headerCoordinates, bodyCoordinates) ->
                            headerCoordinates
                            |> Array.map (fun coordinate -> table.GetColumn(coordinate.x - 1).Header),
                            bodyCoordinates
                            |> Array.map (fun coordinate -> table.GetCellAt(coordinate.x - 1, coordinate.y - 1))
                    )
                    |> Array.unzip

                let headers = headers |> Array.concat

                if headers.Length > 0 then
                    let columns =
                        let body = cells.[1..] |> Array.transpose

                        headers
                        |> Array.mapi (fun index header ->
                            if body.Length - 1 >= index then
                                CompositeColumn.create (header, body.[index] |> ResizeArray)
                            else
                                CompositeColumn.create (header, Array.empty |> ResizeArray)
                        )

                    let tableString =
                        let table = ArcTable.init ("placeholder")
                        table.AddColumns(columns)
                        table.ToStringSeqs()

                    tableString
                    |> Array.map (fun row -> row |> String.concat "\t")
                    |> String.concat Environment.NewLine
                else
                    CompositeCell.ToTableTxt(cells)
            else if cellIndex.y - 1 < 0 then
                let column = table.GetColumn(cellIndex.x - 1)
                let table = ArcTable.init ("placeholder")
                table.AddColumn(column.Header)

                table.ToStringSeqs()
                |> Array.map (fun row -> row |> String.concat "\t")
                |> String.concat Environment.NewLine
            else
                let cell = table.GetCellAt((cellIndex.x - 1, cellIndex.y - 1))
                cell.ToTabStr()

        navigator.clipboard.writeText result

    static member cut(cellIndex: CellCoordinate, table: ArcTable, setTable, selectHandle: SelectHandle) = promise {
        do! AnnotationTableClipboard.copy (cellIndex, table, selectHandle)

        let nextTable =
            AnnotationTableClipboard.clear (cellIndex, table, selectHandle)

        nextTable |> setTable
    }

    static member private getIndex(startIndex, length) =
        let rec loop index length =
            if index < length then
                index
            else
                loop (index - length) length

        loop startIndex length

    static member getCopiedCells() = promise {
        let! copiedValue = navigator.clipboard.readText ()

        let rows =
            copiedValue.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
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
                            AnnotationTableClipboard.getIndex (index, row.Length)
                        else
                            index

                    match header with
                    | x when x.IsSingleColumn || x.IsDataColumn || x.IsTermColumn ->
                        let cell = CompositeCell.fromContentValid (row.[newIndex], header)
                        loop (index + 1) (cell :: result)
                    | anyElse -> failwith $"Error-fitColumnsToTarget: Encountered unsupported case: {anyElse}"

            loop 0 []

        let getHeadersExpectedLengths (headers: CompositeHeader[]) =
            headers
            |> Array.map (fun header ->
                match header with
                | x when x.IsSingleColumn -> x.ToString(), [ 1 ]
                | x when x.IsDataColumn -> x.ToString(), [ 4 ]
                | x when x.IsTermColumn -> x.ToString(), [ 1; 2; 3; 4 ]
                | anyElse -> failwith $"Error-getHeadersWithLength: Encountered unsupported case: {anyElse}"
            )

        let fitHeaders (strings: string[]) (headersSizes: (string * int list)[]) (maxColumns: int) =
            let rec tryFit (cell: string[]) index (remaining: (string * int list) list) (columnsLeft: int) =

                let index =
                    if index >= strings.Length && columnsLeft > 0 then
                        0
                    else
                        index

                match remaining, columnsLeft with
                | _, 0 -> Some []
                | [], _ -> Some []
                | (name, sizes) :: rest, _ ->
                    sizes
                    |> List.sortDescending
                    |> List.choose (fun size ->

                        let maxSize =
                            let cellsLeft = cell.Length - index

                            if cellsLeft > columnsLeft - 1 then
                                cellsLeft - (columnsLeft - 1)
                            else
                                cellsLeft

                        let adjustedSize = min size maxSize

                        let actualSize =
                            if index + adjustedSize <= cell.Length then adjustedSize
                            elif index < cell.Length then cell.Length - index
                            else 0

                        if actualSize > 0 then
                            let segment = cell.[index .. index + actualSize - 1]

                            match tryFit cell (index + actualSize) rest (columnsLeft - 1) with
                            | Some restResult -> Some((name, segment) :: restResult)
                            | None -> None
                        else
                            None
                    )
                    |> List.tryHead

            tryFit strings 0 (Array.toList headersSizes) maxColumns

        let fittingHeaders = getHeadersExpectedLengths headers

        let fittedRows =
            data |> Array.map (fun row -> fitHeaders row fittingHeaders headers.Length)

        let result =
            fittedRows
            |> Array.choose id
            |> Array.map (fun row -> row |> List.map (fun (_, cells) -> cells) |> Array.ofList)

        result |> Array.map (fun row -> fitColumnsToTarget row headers)

    static member checkForHeader(value: string) =

        let splitCamelCase (input: string) =
            Regex.Matches(input, @"([A-Z][a-z]+|[A-Z]+(?![a-z]))")
            |> Seq.cast<Match>
            |> Seq.map (fun m -> m.Value)
            |> String.concat " "

        let headerNames =
            CompositeHeader.Cases |> Array.map (fun (_, item) -> splitCamelCase item)

        match value with
        | header when ARCtrl.Helper.Regex.tryParseCharacteristicColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseComponentColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseFactorColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseInputColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseOutputColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseParameterColumnHeader(header).IsSome -> true
        | header when
            // Header with a short length can lead to a problem here, so it must be at least as long as an expected header.
            let shortestHeaderLength = headerNames |> Array.minBy String.length |> String.length

            headerNames
            |> Array.exists (fun case ->
                not (String.IsNullOrEmpty header)
                && case.StartsWith(header)
                && header.Length >= shortestHeaderLength
            )
            ->
            true
        | _ -> false

    static member predictPasteBehaviour
        (cellIndex: CellCoordinate, targetTable: ArcTable, selectHandle: SelectHandle, data: string[][])
        =

        let cellCoordinates = selectHandle.getSelectedCells () |> Array.ofSeq

        let headers =
            let columnIndices = cellCoordinates |> Array.distinctBy (fun item -> item.x)

            columnIndices
            |> Array.map (fun index -> targetTable.GetColumn(index.x - 1).Header)

        let checkForHeaders (row: string[]) =
            let headers = CompositeHeader.Cases |> Array.map (fun (_, header) -> header)

            let areHeaders =
                headers
                |> Array.collect (fun _ ->
                    row
                    |> Array.map (fun cell -> AnnotationTableClipboard.checkForHeader cell)
                )

            Array.contains true areHeaders

        let groupedCellCoordinates =
            cellCoordinates
            |> Array.ofSeq
            |> Array.groupBy (fun item -> item.y)
            |> Array.map (fun (_, row) -> row)

        if checkForHeaders data.[0] || cellCoordinates.[0].y <= 0 || cellIndex.y <= 0 then

            let headers, body =
                if data.Length > 1 then
                    data.[0], data.[1..]
                else
                    data.[0], [||]

            let columns = Array.append [| headers |] body |> Array.transpose
            let columnsArrays = columns |> Seq.toArray |> Array.map Seq.toArray

            let compositeColumns =
                ARCtrl.Spreadsheet.ArcTable.composeColumns columnsArrays |> ResizeArray

            PasteCases.AddColumns {|
                data = compositeColumns
                coordinate = cellIndex
                coordinates = groupedCellCoordinates
            |}
        else
            let fittedCells =
                AnnotationTableClipboard.getFittedCells (data, headers)
                |> Array.transpose
                |> Array.map ResizeArray

            let columns =
                Array.map2 (fun header cells -> CompositeColumn.create (header, cells)) headers fittedCells
                |> ResizeArray

            let isEmpty =
                Array.isEmpty fittedCells
                || fittedCells
                   |> Array.map (fun row ->
                       Seq.isEmpty row
                       || Seq.forall (fun (cell: CompositeCell) -> String.IsNullOrWhiteSpace(cell.ToTabStr())) row
                   )
                   |> Array.contains true

            if isEmpty then
                PasteCases.Unknown {| data = data; headers = headers |}
            else
                PasteCases.PasteCells {|
                    data = columns
                    coordinates = groupedCellCoordinates
                |}

    static member private getValueOfCompositeHeader(compositeHeader: CompositeHeader) =
        match compositeHeader with
        | CompositeHeader.Component oa -> defaultArg oa.Name (compositeHeader.ToString())
        | CompositeHeader.Characteristic oa -> defaultArg oa.Name (compositeHeader.ToString())
        | CompositeHeader.Factor oa -> defaultArg oa.Name (compositeHeader.ToString())
        | CompositeHeader.Parameter oa -> defaultArg oa.Name (compositeHeader.ToString())
        | CompositeHeader.ProtocolType -> CompositeHeaderDiscriminate.ProtocolType.ToString()
        | CompositeHeader.ProtocolDescription -> CompositeHeaderDiscriminate.ProtocolDescription.ToString()
        | CompositeHeader.ProtocolUri -> CompositeHeaderDiscriminate.ProtocolUri.ToString()
        | CompositeHeader.ProtocolVersion -> CompositeHeaderDiscriminate.ProtocolVersion.ToString()
        | CompositeHeader.ProtocolREF -> CompositeHeaderDiscriminate.ProtocolREF.ToString()
        | CompositeHeader.Performer -> CompositeHeaderDiscriminate.Performer.ToString()
        | CompositeHeader.Date -> CompositeHeaderDiscriminate.Date.ToString()
        | CompositeHeader.Input io -> io.ToString()
        | CompositeHeader.Output io -> io.ToString()
        | CompositeHeader.Comment s -> s
        | CompositeHeader.FreeText s -> s

    static member pasteCells
        (
            pasteColumns:
                {|
                    data: ResizeArray<CompositeColumn>
                    coordinates: CellCoordinate[][]
                |},
            coordinate: CellCoordinate,
            selectHandle: SelectHandle,
            table: ArcTable,
            setTable
        ) =

        let getCorrectTargetForCell
            (currentCell: CompositeCell)
            (table: ArcTable)
            (targetCoordinate: CellCoordinate)
            adaption
            =

            if currentCell.isUnitized then
                let targetCell =
                    table.GetCellAt(targetCoordinate.x - adaption, targetCoordinate.y - adaption)

                let value, unit = currentCell.AsUnitized

                let unitNeedsMetadata =
                    unit.isEmpty()
                    || Option.forall String.IsNullOrWhiteSpace unit.TermSourceREF
                    || Option.forall String.IsNullOrWhiteSpace unit.TermAccessionNumber

                let hasSameUnitName (targetTerm: OntologyAnnotation) =
                    unit.isEmpty()
                    || String.Equals(unit.NameText, targetTerm.NameText, StringComparison.OrdinalIgnoreCase)

                let targetUnit =
                    if targetCell.isUnitized then
                        targetCell.AsUnitized |> snd |> Some
                    elif targetCell.isTerm then
                        targetCell.AsTerm |> Some
                    else
                        None

                match targetUnit with
                | Some targetUnit when unitNeedsMetadata && hasSameUnitName targetUnit ->
                    CompositeCell.createUnitized (value, targetUnit)
                | _ -> currentCell
            else
                currentCell

        let getCorrectTargetForHeader
            (currentHeader: CompositeHeader)
            (table: ArcTable)
            (targetCoordinate: CellCoordinate)
            adaption
            =

            let targetCell =
                table.GetCellAt(targetCoordinate.x - adaption, targetCoordinate.y - adaption)

            if targetCell.isUnitized then
                let _, targetUnit = targetCell.AsUnitized
                let value = AnnotationTableClipboard.getValueOfCompositeHeader currentHeader
                CompositeCell.createUnitized (value, targetUnit)
            elif targetCell.isTerm then
                let targetTerm = defaultArg (currentHeader.TryGetTerm()) targetCell.AsTerm
                CompositeCell.createTerm targetTerm
            elif targetCell.isData then
                let targetInfo = targetCell.AsData
                let targetData = AnnotationTableClipboard.getValueOfCompositeHeader currentHeader

                CompositeCell.createDataFromString (
                    targetData,
                    ?format = targetInfo.Format,
                    ?selectorFormat = targetInfo.SelectorFormat
                )
            else
                let value = AnnotationTableClipboard.getValueOfCompositeHeader currentHeader
                CompositeCell.createFreeText value

        let headerCoordinates, bodyCoordinates =
            if pasteColumns.coordinates.[0].[0].y - 1 < 0 then
                pasteColumns.coordinates.[0], pasteColumns.coordinates.[1..]
            else
                [||], pasteColumns.coordinates

        let pasteWithCoordiantes (coordinates: CellCoordinate[][]) =
            if selectHandle.getCount () > 1 then
                coordinates
                |> Array.iteri (fun yi row ->
                    let yIndex =
                        if (pasteColumns.data.Item 0).Cells.Count = 0 then
                            1
                        else
                            AnnotationTableClipboard.getIndex (yi, (pasteColumns.data.Item 0).Cells.Count)

                    row
                    |> Array.iteri (fun xi coordinate ->
                        let xIndex = AnnotationTableClipboard.getIndex (xi, pasteColumns.data.Count)

                        if coordinate.y - 1 < 0 then
                            table.UpdateHeader(coordinate.x - 1, pasteColumns.data.[xIndex].Header, true)
                        else
                            let newTarget =
                                if pasteColumns.data.[0].Cells.Count = 0 then
                                    let currentHeader = pasteColumns.data.[xIndex].Header
                                    getCorrectTargetForHeader currentHeader table coordinate 1
                                else
                                    let cells = (pasteColumns.data.Item xIndex).Cells
                                    let currentCell = cells.[yIndex]
                                    getCorrectTargetForCell currentCell table coordinate 1

                            table.SetCellAt(coordinate.x - 1, coordinate.y - 1, newTarget)
                    )
                )
            else if coordinate.y - 1 < 0 then
                table.UpdateHeader(coordinate.x - 1, pasteColumns.data.[0].Header, true)
            else
                let newTarget =
                    if pasteColumns.data.[0].Cells.Count = 0 then
                        let currentHeader = pasteColumns.data.[0].Header
                        getCorrectTargetForHeader currentHeader table coordinate 1
                    else
                        let currentCell = pasteColumns.data.[0].Cells.[0]
                        getCorrectTargetForCell currentCell table coordinate 1

                table.SetCellAt(coordinate.x - 1, coordinate.y - 1, newTarget)

        if headerCoordinates.Length > 0 then
            pasteWithCoordiantes [| headerCoordinates |]

        if bodyCoordinates.Length > 0 then
            pasteWithCoordiantes bodyCoordinates

        table.Copy() |> setTable

    static member paste
        (
            pasteCases: PasteCases,
            coordinate: CellCoordinate,
            table: ArcTable,
            selectHandle: SelectHandle,
            setModal: Types.AnnotationTable.ModalTypes option -> unit,
            setTable: ArcTable -> unit
        ) =

        match pasteCases with
        | AddColumns addColumns ->
            setModal (
                AnnotationTable.ModalTypes.PasteCaseUserInput(PasteCases.AddColumns addColumns, selectHandle)
                |> Some
            )
        | PasteCells pasteColumns ->
            AnnotationTableClipboard.pasteCells (pasteColumns, coordinate, selectHandle, table, setTable)
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
            let! data = AnnotationTableClipboard.getCopiedCells ()

            try
                let prediction =
                    AnnotationTableClipboard.predictPasteBehaviour (cellIndex, arcTable, selectHandle, data)

                AnnotationTableClipboard.paste (
                    prediction,
                    cellIndex,
                    arcTable,
                    selectHandle,
                    setModal,
                    setArcTable
                )
            with exn ->
                setModal (AnnotationTable.ModalTypes.Error(exn.Message) |> Some)
        }
