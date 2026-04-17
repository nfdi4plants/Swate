module Swate.Components.Template.Helper

open System
open ARCtrl
open Swate.Components.Shared
open Swate.Components.Template.Types

[<Literal>]
let CacheStorageKey = "swate.components.template.cache.v1"

let DefaultFetchInterval = TimeSpan.FromHours 1.0

let toFullAuthorName (author: Person) =
    [ author.FirstName; author.MidInitials; author.LastName ]
    |> List.choose id
    |> String.concat " "

let private tryTrimmed (value: string) =
    if String.IsNullOrWhiteSpace value then
        None
    else
        Some(value.Trim())

let compositeCellPreviewValues (cell: CompositeCell) =
    match cell with
    | CompositeCell.FreeText text -> [| text |]
    | CompositeCell.Term ontologyAnnotation -> [|
        ontologyAnnotation.NameText
        defaultArg ontologyAnnotation.TermSourceREF ""
        defaultArg ontologyAnnotation.TermAccessionNumber ""
      |]
    | CompositeCell.Unitized(value, ontologyAnnotation) -> [|
        value
        ontologyAnnotation.NameText
        defaultArg ontologyAnnotation.TermSourceREF ""
        defaultArg ontologyAnnotation.TermAccessionNumber ""
      |]
    | CompositeCell.Data data -> [|
        defaultArg data.FilePath ""
        data.NameText
        defaultArg data.Selector ""
        defaultArg data.Format ""
        defaultArg data.SelectorFormat ""
      |]
    |> Array.choose tryTrimmed

let templateColumnValuePreview (table: ArcTable) (columnIndex: int) =
    seq {
        for rowIndex in 0 .. table.RowCount - 1 do
            let row = table.GetRow(rowIndex, true) |> Array.ofSeq

            if columnIndex < row.Length then
                yield row.[columnIndex]
    }
    |> Seq.collect compositeCellPreviewValues
    |> Seq.distinct
    |> Seq.truncate 3
    |> String.concat " | "
    |> fun preview -> if preview = "" then "No values" else preview

let getLastFetchedUtc (cacheState: TemplateCacheState) =
    cacheState.LastFetchedUtcTicks
    |> Option.map (fun ticks -> DateTime(ticks, DateTimeKind.Utc))

let shouldFetchFresh (forceRefresh: bool) (cacheState: TemplateCacheState) (nowUtc: DateTime) =
    if forceRefresh then
        true
    else
        cacheState
        |> getLastFetchedUtc
        |> Option.map (fun lastFetchedUtc -> nowUtc - lastFetchedUtc > DefaultFetchInterval)
        |> Option.defaultValue true

let tryReadTemplatesFromCache (cacheState: TemplateCacheState) =
    match cacheState.TemplatesJson with
    | Some templatesJson when not (String.IsNullOrWhiteSpace templatesJson) ->
        try
            let parsedTemplates =
                templatesJson |> ARCtrl.Json.Templates.fromJsonString |> Array.ofSeq

            Ok(Some parsedTemplates)
        with error ->
            Error error.Message
    | _ -> Ok None

let toCacheState (templates: Template[]) (nowUtc: DateTime) = {
    SchemaVersion = TemplateCacheState.Empty.SchemaVersion
    LastFetchedUtcTicks = Some nowUtc.Ticks
    TemplatesJson = Some(ARCtrl.Json.Templates.toJsonString 0 templates)
}


[<RequireQualifiedAccess>]
module TemplateImportMode =

    let options: (ARCtrl.TableJoinOptions * string * string)[] = [|
        ARCtrl.TableJoinOptions.Headers, "Headers", "Column Headers"
        ARCtrl.TableJoinOptions.WithUnit, "WithUnit", "With Units"
        ARCtrl.TableJoinOptions.WithValues, "WithValues", "With Values"
    |]

let private getDeselectedTableColumnIndices (deselectedColumns: Set<int * int>) (tableIndex: int) =
    deselectedColumns
    |> Seq.choose (fun (candidateTableIndex, columnIndex) ->
        if candidateTableIndex = tableIndex then
            Some columnIndex
        else
            None
    )
    |> List.ofSeq

let createUpdatedTables
    (arcTables: ResizeArray<ArcTable>)
    (state: SelectiveImportConfig)
    (deselectedColumns: Set<int * int>)
    fullImport
    =
    [
        for importTable in state.ImportTables do
            let fullImport = defaultArg fullImport importTable.FullImport

            if importTable.FullImport = fullImport then
                let deselectedColumnIndices =
                    getDeselectedTableColumnIndices deselectedColumns importTable.Index

                let sourceTable = arcTables.[importTable.Index]
                let appliedTable = ArcTable.init sourceTable.Name

                let finalTable =
                    Table.selectiveTablePrepare appliedTable sourceTable deselectedColumnIndices

                appliedTable.Join(finalTable, joinOptions = state.ImportType)
                appliedTable
    ]
    |> ResizeArray

let updateTables
    (importTables: ResizeArray<ArcTable>)
    (importConfig: SelectiveImportConfig)
    (activeTableIndex: int option)
    (existingOpt: ArcFiles option)
    =
    let deselectedColumns = importConfig.DeselectedColumns

    match existingOpt with
    | Some existing ->
        let existingTables = existing.Tables()

        match activeTableIndex with
        | Some tableIndex when tableIndex >= 0 && tableIndex < existingTables.Count ->
            let activeTable = existingTables.[tableIndex]

            let selectedColumnTables =
                createUpdatedTables importTables importConfig deselectedColumns (Some false)
                |> Array.ofSeq
                |> Array.rev

            let tempTable = activeTable.Copy()

            for table in selectedColumnTables do
                if table.RowCount = 0 then
                    let cells =
                        table.Columns
                        |> Array.ofSeq
                        |> Array.map (fun column ->
                            match column.Header with
                            | CompositeHeader.Factor _
                            | CompositeHeader.Component _
                            | CompositeHeader.Parameter _
                            | CompositeHeader.Characteristic _ ->
                                match importConfig.ImportType with
                                | TableJoinOptions.WithUnit -> CompositeCell.Unitized("", OntologyAnnotation.empty ())
                                | _ -> CompositeCell.Term(OntologyAnnotation.empty ())
                            | _ -> CompositeCell.FreeText ""
                        )
                        |> ResizeArray

                    let rows = Array.create tempTable.RowCount cells
                    table.AddRows(ResizeArray rows)
                elif table.RowCount < tempTable.RowCount then
                    table.AddRowsEmpty(tempTable.RowCount - table.RowCount)

                let preparedTemplate = Table.distinctByHeader tempTable table
                tempTable.Join(preparedTemplate, joinOptions = importConfig.ImportType)

            existingTables.[tableIndex] <- tempTable
        | _ -> ()

        let selectedColumnTables =
            createUpdatedTables importTables importConfig deselectedColumns (Some true)
            |> Array.ofSeq
            |> Array.rev

        selectedColumnTables
        |> Seq.map (fun table ->
            let nextTable = ArcTable.init table.Name
            nextTable.Join(table, joinOptions = importConfig.ImportType)
            nextTable
        )
        |> Seq.rev
        |> Seq.iter existingTables.Add

        existing
    | None -> failwith "Error! Can only append information if metadata sheet exists!"