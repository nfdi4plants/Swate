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

let private findLastNonEmptyDataContextIndex (dataMap: DataMap) =
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
