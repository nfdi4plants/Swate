module Renderer.components.Widgets.DataAnnotatorDataSource

open ARCtrl
open Swate.Components.Shared
open DataAnnotator

type AnnotationInput = {
    Selectors: string[]
    FileName: string
    FileType: string
    TargetColumn: TargetColumn
}

let private compareTargets (a: DataTarget) (b: DataTarget) =
    let key =
        function
        | DataTarget.Cell(ci, ri) -> 0, ri, ci
        | DataTarget.Column ci -> 1, 0, ci
        | DataTarget.Row ri -> 2, ri, 0

    compare (key a) (key b)

let SelectorsFromTargets (hasHeader: bool) (targets: Set<DataTarget>) =
    targets
    |> Seq.sortWith compareTargets
    |> Seq.map (fun target -> target.ToFragmentSelectorString(hasHeader))
    |> Array.ofSeq

let TryParseDataFile (separator: string) (file: DataFile) =
    try
        let parsed = ParsedDataFile.fromFileBySeparator separator file

        if parsed.BodyRows.Length = 0 then
            Error "Parsed file does not contain any data rows."
        else
            Ok parsed
    with exn ->
        Error exn.Message

let TryGetTargetHeader (table: ArcTable) (targetColumn: TargetColumn) =
    match targetColumn with
    | TargetColumn.Input -> Ok(CompositeHeader.Input IOType.Data)
    | TargetColumn.Output -> Ok(CompositeHeader.Output IOType.Data)
    | TargetColumn.Autodetect ->
        match table.TryGetInputColumn(), table.TryGetOutputColumn() with
        | Some _, None
        | None, None -> Ok(CompositeHeader.Output IOType.Data)
        | None, Some _ -> Ok(CompositeHeader.Input IOType.Data)
        | Some _, Some _ ->
            Error "Both Input and Output columns already exist. Select Input or Output explicitly."

let private mkDataCell (fileName: string) (fileType: string) (selector: string) =
    let data = Data()
    data.FilePath <- Some fileName
    data.Selector <- Some selector
    data.Format <- Some fileType
    data.SelectorFormat <- Some URLs.Data.SelectorFormat.csv
    CompositeCell.createData data

let ApplyToTable (table: ArcTable) (input: AnnotationInput) =
    match TryGetTargetHeader table input.TargetColumn with
    | Error err -> Error err
    | Ok header ->
        try
            let values =
                input.Selectors
                |> Array.map (mkDataCell input.FileName input.FileType)
                |> ResizeArray

            table.AddColumn(header, values, forceReplace = true)
            Ok input.Selectors.Length
        with exn ->
            Error exn.Message

let ApplyToDataMap (dataMap: DataMap) (input: AnnotationInput) =
    try
        if input.Selectors.Length > dataMap.DataContexts.Count then
            let toAdd =
                Array.init (input.Selectors.Length - dataMap.DataContexts.Count) (fun _ -> DataContext())

            dataMap.DataContexts.AddRange(toAdd)

        for index in 0 .. input.Selectors.Length - 1 do
            let selector = input.Selectors.[index]
            let dtx = dataMap.DataContexts.[index]
            dtx.FilePath <- Some input.FileName
            dtx.Selector <- Some selector
            dtx.Format <- Some input.FileType
            dtx.SelectorFormat <- Some URLs.Data.SelectorFormat.csv

        Ok input.Selectors.Length
    with exn ->
        Error exn.Message
