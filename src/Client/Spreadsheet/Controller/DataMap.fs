module Spreadsheet.Controller.DataMap

open Swate.Components.Shared
open ARCtrl

let updateDatamap (dataMapOpt: DataMap option) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let nextArcFile =
        match state.ArcFile with
        | Some(Assay a) ->
            a.DataMap <- dataMapOpt
            Some(Assay a)
        | Some(Study(s, _)) ->
            s.DataMap <- dataMapOpt
            Some(Study(s, []))
        | _ ->
            console.warn "[WARNING] updateDatamap: No Assay or Study found in ArcFile"
            state.ArcFile

    match dataMapOpt with
    | None when state.ActiveView = Spreadsheet.ActiveView.DataMap -> {
        state with
            ArcFile = nextArcFile
            ActiveView = Spreadsheet.ActiveView.Metadata
      }
    | _ -> { state with ArcFile = nextArcFile }

let updateDataMapDataContextAt (dtx) (index) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let ensureIndexExists (dtm: DataMap) =
        if index >= dtm.DataContexts.Count then
            failwithf
                "DataMap does not contain the an item at index: %i. Only %i items exist."
                index
                dtm.DataContexts.Count

    let nextArcFile =
        match state.ArcFile with
        | Some(Assay a) when a.DataMap.IsSome ->
            ensureIndexExists a.DataMap.Value
            a.DataMap.Value.DataContexts.[index] <- dtx
            Some(Assay a)
        | Some(Study(s, _)) when s.DataMap.IsSome ->
            ensureIndexExists s.DataMap.Value
            s.DataMap.Value.DataContexts.[index] <- dtx
            Some(Study(s, []))
        | _ ->
            console.warn "[WARNING] updateDatamap: No Assay or Study found in ArcFile"
            state.ArcFile

    { state with ArcFile = nextArcFile }

let addRows (n: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    let rows = Array.init n (fun _ -> DataContext())

    if state.HasDataMap() then
        state.DataMapOrDefault.DataContexts.AddRange(rows)

    { state with ArcFile = state.ArcFile }

let deleteRow (n: int) (state: Spreadsheet.Model) : Spreadsheet.Model =
    if state.HasDataMap() then
        state.DataMapOrDefault.DataContexts.RemoveAt n

    { state with ArcFile = state.ArcFile }

let deleteRows (rows: int[]) (state: Spreadsheet.Model) : Spreadsheet.Model =
    if state.HasDataMap() then
        rows
        |> Array.sortDescending
        |> Array.iter (fun n -> state.DataMapOrDefault.DataContexts.RemoveAt n)

    { state with ArcFile = state.ArcFile }

let addDataAnnotation
    (data:
        {|
            fragmentSelectors: string[]
            fileName: string
            fileType: string
            targetColumn: DataAnnotator.TargetColumn
        |})
    (state: Spreadsheet.Model)
    : Spreadsheet.Model =
    let inputLength = data.fragmentSelectors.Length
    // Append rows if needed
    if inputLength > state.DataMapOrDefault.DataContexts.Count then
        let rows =
            Array.init (inputLength - state.DataMapOrDefault.DataContexts.Count) (fun _ -> DataContext())

        state.DataMapOrDefault.DataContexts.AddRange(rows)

    for i in 0 .. inputLength - 1 do
        let selector = data.fragmentSelectors.[i]
        let dtx = state.DataMapOrDefault.DataContexts.[i]
        dtx.FilePath <- Some data.fileName
        dtx.Selector <- Some selector
        dtx.Format <- Some data.fileType
        dtx.SelectorFormat <- Some Swate.Components.Shared.URLs.Data.SelectorFormat.csv

    { state with ArcFile = state.ArcFile }