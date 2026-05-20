module Swate.Components.Composite.Widgets.DataAnnotator.Helper

open System
open ARCtrl
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Composite.Widgets.DataAnnotator.Types

let private compareTargets (left: DataTarget) (right: DataTarget) =
    let key =
        function
        | DataTarget.Cell(columnIndex, rowIndex) -> 0, rowIndex, columnIndex
        | DataTarget.Column columnIndex -> 1, 0, columnIndex
        | DataTarget.Row rowIndex -> 2, rowIndex, 0

    compare (key left) (key right)

let selectorsFromTargets (hasHeader: bool) (targets: Set<DataTarget>) =
    targets
    |> Seq.sortWith compareTargets
    |> Seq.map (fun target -> target.ToFragmentSelectorString(hasHeader))
    |> Array.ofSeq

let tryParseDataFile (separator: string) (file: DataFile) =
    try
        let parsed = ParsedDataFile.fromFileBySeparator separator file

        if parsed.BodyRows.Length = 0 then
            Error "Parsed file does not contain any data rows."
        else
            Ok parsed
    with exceptionValue ->
        Error exceptionValue.Message

let tryGetTargetHeader (table: ArcTable) (targetColumn: TargetColumn) =
    match targetColumn with
    | TargetColumn.Input -> Ok(CompositeHeader.Input IOType.Data)
    | TargetColumn.Output -> Ok(CompositeHeader.Output IOType.Data)
    | TargetColumn.Autodetect ->
        match table.TryGetInputColumn(), table.TryGetOutputColumn() with
        | Some _, None
        | None, None -> Ok(CompositeHeader.Output IOType.Data)
        | None, Some _ -> Ok(CompositeHeader.Input IOType.Data)
        | Some _, Some _ -> Error "Both Input and Output columns already exist. Select Input or Output explicitly."

let private targetColumnToHeader (targetColumn: TargetColumn) =
    match targetColumn with
    | TargetColumn.Input -> CompositeHeader.Input IOType.Data
    | TargetColumn.Output -> CompositeHeader.Output IOType.Data
    | TargetColumn.Autodetect -> CompositeHeader.Output IOType.Data

let private tryGetExistingTargetColumn (table: ArcTable) (targetColumn: TargetColumn) =
    match targetColumn with
    | TargetColumn.Input -> table.TryGetInputColumn()
    | TargetColumn.Output -> table.TryGetOutputColumn()
    | TargetColumn.Autodetect -> None

let private isSomeNonEmptyString = Option.exists (String.IsNullOrWhiteSpace >> not)

let private isDataContextNonEmpty (dataContext: DataContext) =
    isSomeNonEmptyString dataContext.FilePath
    || isSomeNonEmptyString dataContext.Selector
    || isSomeNonEmptyString dataContext.Format
    || isSomeNonEmptyString dataContext.SelectorFormat

let private isCompositeCellNonEmpty (cell: CompositeCell) =
    cell.GetContentSwate() |> Array.exists (String.IsNullOrWhiteSpace >> not)

let private findLastNonEmptyDataCellIndex (cells: ResizeArray<CompositeCell>) =
    let mutable lastNonEmptyIndex = -1

    for index in 0 .. cells.Count - 1 do
        if isCompositeCellNonEmpty cells.[index] then
            lastNonEmptyIndex <- index

    lastNonEmptyIndex

let private findLastNonEmptyDataContextIndex (dataMap: DataMap) =
    let mutable lastNonEmptyIndex = -1

    for index in 0 .. dataMap.DataContexts.Count - 1 do
        if isDataContextNonEmpty dataMap.DataContexts.[index] then
            lastNonEmptyIndex <- index

    lastNonEmptyIndex

let private clearDataContextData (dataContext: DataContext) =
    dataContext.FilePath <- None
    dataContext.Selector <- None
    dataContext.Format <- None
    dataContext.SelectorFormat <- None

let private tryGetTableWriteHeader (table: ArcTable) (targetColumn: TargetColumn) (writeMode: WriteMode) =
    match targetColumn, writeMode with
    | TargetColumn.Autodetect, WriteMode.Append -> Error "Append mode requires selecting Input or Output explicitly."
    | TargetColumn.Autodetect, WriteMode.Replace -> tryGetTargetHeader table targetColumn
    | _ -> Ok(targetColumnToHeader targetColumn)

let private mkDataCell (fileName: string) (fileType: string) (selector: string) =
    let data = Data()
    data.FilePath <- Some fileName
    data.Selector <- Some selector
    data.Format <- Some fileType
    data.SelectorFormat <- Some URLs.Data.SelectorFormat.csv
    CompositeCell.createData data

let private mkEmptyDataCell () =
    let data = Data()
    CompositeCell.createData data

let applyToTable (table: ArcTable) (input: AnnotationInput) =
    match input.Target with
    | AnnotationTarget.DataMap _ -> Error "DataMap target cannot be applied to a table destination."
    | AnnotationTarget.Table(targetColumn, writeMode) ->
        match tryGetTableWriteHeader table targetColumn writeMode with
        | Error errorMessage -> Error errorMessage
        | Ok header ->
            try
                let existingColumn = tryGetExistingTargetColumn table targetColumn

                let startRowIndex =
                    match writeMode, existingColumn with
                    | WriteMode.Append, Some column -> findLastNonEmptyDataCellIndex column.Cells + 1
                    | WriteMode.Append, None -> 0
                    | WriteMode.Replace, _ -> 0

                let targetRowCount =
                    System.Math.Max(table.RowCount, startRowIndex + input.Selectors.Length)

                if targetRowCount > table.RowCount && table.ColumnCount > 0 then
                    table.AddRowsEmpty(targetRowCount - table.RowCount)

                let selectorEndExclusive = startRowIndex + input.Selectors.Length

                let values =
                    [|
                        for rowIndex in 0 .. targetRowCount - 1 do
                            if rowIndex >= startRowIndex && rowIndex < selectorEndExclusive then
                                let selectorIndex = rowIndex - startRowIndex
                                mkDataCell input.FileName input.FileType input.Selectors.[selectorIndex]
                            else
                                match writeMode, existingColumn with
                                | WriteMode.Append, Some column when rowIndex < column.Cells.Count ->
                                    column.Cells.[rowIndex]
                                | _ -> mkEmptyDataCell ()
                    |]
                    |> ResizeArray

                table.AddColumn(header, values, forceReplace = true)
                Ok input.Selectors.Length
            with exceptionValue ->
                Error exceptionValue.Message

let applyToDataMap (dataMap: DataMap) (input: AnnotationInput) =
    match input.Target with
    | AnnotationTarget.Table _ -> Error "Table target cannot be applied to a DataMap destination."
    | AnnotationTarget.DataMap writeMode ->
        try
            let startIndex =
                match writeMode with
                | WriteMode.Replace -> 0
                | WriteMode.Append -> findLastNonEmptyDataContextIndex dataMap + 1

            let requiredCount = startIndex + input.Selectors.Length

            if requiredCount > dataMap.DataContexts.Count then
                let toAdd =
                    Array.init (requiredCount - dataMap.DataContexts.Count) (fun _ -> DataContext())

                dataMap.DataContexts.AddRange toAdd

            if writeMode = WriteMode.Replace then
                for index in requiredCount .. dataMap.DataContexts.Count - 1 do
                    clearDataContextData dataMap.DataContexts.[index]

            for selectorOffset in 0 .. input.Selectors.Length - 1 do
                let targetIndex = startIndex + selectorOffset
                let selector = input.Selectors.[selectorOffset]
                let dataContext = dataMap.DataContexts.[targetIndex]
                dataContext.FilePath <- Some input.FileName
                dataContext.Selector <- Some selector
                dataContext.Format <- Some input.FileType
                dataContext.SelectorFormat <- Some URLs.Data.SelectorFormat.csv

            Ok input.Selectors.Length
        with exceptionValue ->
            Error exceptionValue.Message

let DefaultSeparatorOptions: (string * string)[] = [|
    "\\t", "Tab (\\t)"
    ",", "Comma (,)"
    ";", "Semicolon (;)"
    "|", "Pipe (|)"
|]

let fileTypeFromName (fileName: string) =
    if fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) then
        "text/csv"
    elif fileName.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) then
        "text/tab-separated-values"
    elif fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) then
        "text/plain"
    else
        "text/plain"

let separatorToInput (separator: string) =
    match separator with
    | "\t" -> "\\t"
    | "\n" -> "\\n"
    | "\r" -> "\\r"
    | "\r\n" -> "\\r\\n"
    | "\f" -> "\\f"
    | "\v" -> "\\v"
    | _ -> separator

let parseDataFileBySeparator (separator: string) (dataFile: DataFile) =
    match tryParseDataFile separator dataFile with
    | Ok parsed -> Ok parsed
    | Error errorMessage ->
        let fallbackSeparator = dataFile.ExpectedSeparator

        if separator <> fallbackSeparator then
            tryParseDataFile fallbackSeparator dataFile
        else
            Error errorMessage