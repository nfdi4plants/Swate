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
    match tryGetTargetHeader table input.TargetColumn with
    | Error errorMessage -> Error errorMessage
    | Ok header ->
        try
            if table.ColumnCount > 0 && input.Selectors.Length > table.RowCount then
                table.AddRowsEmpty(input.Selectors.Length - table.RowCount)

            let targetRowCount = System.Math.Max(table.RowCount, input.Selectors.Length)

            let values =
                [|
                    for rowIndex in 0 .. targetRowCount - 1 do
                        if rowIndex < input.Selectors.Length then
                            mkDataCell input.FileName input.FileType input.Selectors.[rowIndex]
                        else
                            mkEmptyDataCell ()
                |]
                |> ResizeArray

            table.AddColumn(header, values, forceReplace = true)
            Ok input.Selectors.Length
        with exceptionValue ->
            Error exceptionValue.Message

let applyToDataMap (dataMap: DataMap) (input: AnnotationInput) =
    try
        if input.Selectors.Length > dataMap.DataContexts.Count then
            let toAdd =
                Array.init (input.Selectors.Length - dataMap.DataContexts.Count) (fun _ -> DataContext())

            dataMap.DataContexts.AddRange toAdd

        for index in 0 .. input.Selectors.Length - 1 do
            let selector = input.Selectors.[index]
            let dataContext = dataMap.DataContexts.[index]
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