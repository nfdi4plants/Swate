module Swate.Components.Template.Helper

open System
open ARCtrl
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