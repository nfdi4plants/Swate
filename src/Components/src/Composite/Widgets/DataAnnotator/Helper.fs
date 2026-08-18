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

let private isSomeNonEmptyString = Option.exists (String.IsNullOrWhiteSpace >> not)

let private findLastNonEmptyDataCellIndex (cells: ResizeArray<CompositeCell>) =
    let mutable lastNonEmptyIndex = -1

    for index in 0 .. cells.Count - 1 do
        if
            cells.[index].GetContentSwate()
            |> Array.exists (String.IsNullOrWhiteSpace >> not)
        then
            lastNonEmptyIndex <- index

    lastNonEmptyIndex

let private findLastNonEmptyDataContextIndex (dataMap: Datamap) =
    let mutable lastNonEmptyIndex = -1

    for index in 0 .. dataMap.DataContexts.Count - 1 do
        let dataContext = dataMap.DataContexts.[index]

        if
            isSomeNonEmptyString dataContext.FilePath
            || isSomeNonEmptyString dataContext.Selector
            || isSomeNonEmptyString dataContext.Format
            || isSomeNonEmptyString dataContext.SelectorFormat
        then
            lastNonEmptyIndex <- index

    lastNonEmptyIndex

let private setDataContextFields (fileName: string) (fileType: string) (selector: string) (data: Data) =
    data.FilePath <- Some fileName
    data.Selector <- Some selector
    data.Format <- Some fileType
    data.SelectorFormat <- Some URLs.Data.SelectorFormat.csv
    data

let applyToTable (table: ArcTable) (input: AnnotationInput) =
    match input.Target with
    | AnnotationTarget.DataMap _ -> Error "DataMap target cannot be applied to a table destination."
    | AnnotationTarget.Table(targetColumn, writeMode) ->
        let headerResult =
            match targetColumn, writeMode with
            | TargetColumn.Autodetect, WriteMode.Append ->
                Error "Append mode requires selecting Input or Output explicitly."
            | TargetColumn.Autodetect, WriteMode.Replace -> tryGetTargetHeader table targetColumn
            | TargetColumn.Input, _ -> Ok(CompositeHeader.Input IOType.Data)
            | TargetColumn.Output, _ -> Ok(CompositeHeader.Output IOType.Data)

        match headerResult with
        | Error errorMessage -> Error errorMessage
        | Ok header ->
            try
                let existingColumn =
                    match targetColumn with
                    | TargetColumn.Input -> table.TryGetInputColumn()
                    | TargetColumn.Output -> table.TryGetOutputColumn()
                    | TargetColumn.Autodetect -> None

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

                                Data()
                                |> setDataContextFields input.FileName input.FileType input.Selectors.[selectorIndex]
                                |> CompositeCell.createData
                            else
                                match writeMode, existingColumn with
                                | WriteMode.Append, Some column when rowIndex < column.Cells.Count ->
                                    column.Cells.[rowIndex]
                                | _ -> CompositeCell.createData (Data())
                    |]
                    |> ResizeArray

                table.AddColumn(header, values, forceReplace = true)
                Ok input.Selectors.Length
            with exceptionValue ->
                Error exceptionValue.Message

let applyToDataMap (dataMap: Datamap) (input: AnnotationInput) =
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
                    let dataContext = dataMap.DataContexts.[index]
                    dataContext.FilePath <- None
                    dataContext.Selector <- None
                    dataContext.Format <- None
                    dataContext.SelectorFormat <- None

            for selectorOffset in 0 .. input.Selectors.Length - 1 do
                let targetIndex = startIndex + selectorOffset
                let selector = input.Selectors.[selectorOffset]

                dataMap.DataContexts.[targetIndex]
                |> setDataContextFields input.FileName input.FileType selector
                |> ignore

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
