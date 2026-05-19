module Swate.Components.Composite.Widgets.DataAnnotator.Types

open System
open ARCtrl

[<RequireQualifiedAccess>]
type TargetColumn =
    | Input
    | Output
    | Autodetect

    static member fromString(value: string) =
        match value.ToLowerInvariant() with
        | "input" -> TargetColumn.Input
        | "output" -> TargetColumn.Output
        | _ -> TargetColumn.Autodetect

[<RequireQualifiedAccess>]
type WriteMode =
    | Replace
    | Append

    static member fromString(value: string) =
        match value.ToLowerInvariant() with
        | "append" -> WriteMode.Append
        | _ -> WriteMode.Replace

[<RequireQualifiedAccess>]
type AnnotationTarget =
    | Table of targetColumn: TargetColumn * writeMode: WriteMode
    | DataMap of writeMode: WriteMode

[<RequireQualifiedAccess>]
type AnnotationDestination =
    | Table of ArcTable
    | DataMap of DataMap

type AnnotationInput = {
    Selectors: string[]
    FileName: string
    FileType: string
    Target: AnnotationTarget
}

[<RequireQualifiedAccess>]
type DataTarget =
    | Cell of columnIndex: int * rowIndex: int
    | Row of int
    | Column of int

    member this.ToFragmentSelectorString(hasHeader: bool) =
        let rowOffset = if hasHeader then 2 else 1

        match this with
        | DataTarget.Row rowIndex -> sprintf "row=%i" (rowIndex + rowOffset)
        | DataTarget.Column columnIndex -> sprintf "col=%i" (columnIndex + 1)
        | DataTarget.Cell(columnIndex, rowIndex) -> sprintf "cell=%i,%i" (rowIndex + rowOffset) (columnIndex + 1)

    member this.ToReactKey() =
        match this with
        | DataTarget.Row rowIndex -> sprintf "row-%i" rowIndex
        | DataTarget.Column columnIndex -> sprintf "col-%i" columnIndex
        | DataTarget.Cell(columnIndex, rowIndex) -> sprintf "cell-%i-%i" rowIndex columnIndex

type DataFile = {
    DataFileName: string
    DataFileType: string
    DataContent: string
    DataSize: float
} with

    static member create(dataFileName, dataFileType, dataContent, dataSize) = {
        DataFileName = dataFileName
        DataFileType = dataFileType
        DataContent = dataContent
        DataSize = dataSize
    }

    member this.ExpectedSeparator =
        if this.DataFileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) then
            ","
        elif this.DataFileName.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) then
            "\t"
        elif this.DataFileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) then
            "\t"
        else
            ","

type ParsedDataFile = {
    HeaderRow: string[] option
    BodyRows: string[][]
} with

    static member fromFileBySeparator (separator: string) (file: DataFile) =
        let splitRow (value: string) (separator: string) =
            if separator.Length = 1 then
                value.Split separator.[0]
            else
                value.Split([| separator |], StringSplitOptions.None)

        let sanitizedSeparator =
            match separator with
            | "\\t" -> "\t"
            | "\\n" -> "\n"
            | "\\f" -> "\f"
            | "\\r" -> "\r"
            | "\\r\\n" -> "\r\n"
            | "\\v" -> "\v"
            | _ -> separator

        let rows = file.DataContent.Split([| '\n' |], StringSplitOptions.RemoveEmptyEntries)

        let splitRows =
            rows
            |> Array.map (fun row -> row.TrimEnd '\r' |> fun value -> splitRow value sanitizedSeparator)

        if splitRows.Length > 1 then
            {
                HeaderRow = Some splitRows.[0]
                BodyRows = splitRows.[1..]
            }
        else
            {
                HeaderRow = None
                BodyRows = splitRows
            }

    member this.ToggleHeader() =
        match this.HeaderRow with
        | Some header -> {
            this with
                HeaderRow = None
                BodyRows = Array.insertAt 0 header this.BodyRows
          }
        | None when this.BodyRows.Length > 1 -> {
            this with
                HeaderRow = Some this.BodyRows.[0]
                BodyRows = this.BodyRows.[1..]
          }
        | _ -> this