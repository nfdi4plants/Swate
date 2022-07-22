module CustomComponents.BuildingBlockDetailsModal

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open ExcelColors
open Model
open Messages
open Shared
open TermTypes


let getBuildingBlockHeader (terms:TermSearchable []) =
    terms |> Array.tryFind (fun x -> x.RowIndices = [|0|])

let getBodyRows (terms:TermSearchable []) =
    terms |> Array.filter (fun x -> x.RowIndices <> [|0|])

/// used to parse rowIndices into subsequent windows e.g. "1..3, 5..8"
let windowRowIndices (rowIndices:int [])=
    let splitArrToContinous (l: int []) =
        l 
        |> Array.indexed 
        |> Array.groupBy (fun (i,x) -> i-x)
        |> Array.map (fun (key,valArr) ->
            valArr |> Array.map snd
        )
    let separatedIndices = splitArrToContinous rowIndices
    let sprintedRowIndices =
        [ for contRowIndices in separatedIndices do
            let isMultiple = contRowIndices.Length > 1
            let sprinted =
                if isMultiple then
                    let min = Array.min contRowIndices
                    let max = Array.max contRowIndices
                    sprintf "%i-%i" min max
                else
                    $"{contRowIndices.[0]}"
            yield sprinted
        ]
    sprintedRowIndices |> String.concat ", "

/// parses rowIndices to a nicely formatted sting
let rowIndicesToReadable (rowIndices:int []) =
    if rowIndices.Length > 1 then
        windowRowIndices rowIndices
    elif rowIndices = [|0|] then
        "Header"
    else
        $"{rowIndices.[0]}"

let infoIcon (txt:string) =
    span [
        Style [Color NFDIColors.Yellow.Base; (*OverflowY OverflowOptions.Visible*)]
        Class ("has-tooltip-right has-tooltip-multiline")
        Props.Custom ("data-tooltip", txt)
    ] [
        Fa.i [
            Fa.Solid.InfoCircle
        ] []
    ]

[<Literal>]
let userSpecificTermMsg = "This Term was not found in the database."

/// Parses TermSearchable to table row only for HEADERS. Addresses found search results and free text input.
let searchResultTermToTableHeaderElement (term:TermSearchable option) =
    match term with
    | Some isEmpty when isEmpty.Term.Name = "" && isEmpty.Term.TermAccession = "" ->
        tr [] [
            th [] [str "-"]
            th [] [str "-"]
            th [Style [TextAlign TextAlignOptions.Center]] [str "-"]
            th [] [str (rowIndicesToReadable isEmpty.RowIndices)]
        ]
    | Some hasResult when hasResult.SearchResultTerm.IsSome ->
        tr [ ] [
            th [] [str hasResult.SearchResultTerm.Value.Name]
            th [ Style [TextAlign TextAlignOptions.Center] ] [infoIcon hasResult.SearchResultTerm.Value.Description]
            th [] [str hasResult.SearchResultTerm.Value.Accession]
            th [] [str (rowIndicesToReadable hasResult.RowIndices)]
        ]
    | Some hasNoResult when hasNoResult.SearchResultTerm.IsNone ->
        tr [ ] [
            th [ Style [Color NFDIColors.Red.Lighter20] ] [str hasNoResult.Term.Name]
            th [ Style [TextAlign TextAlignOptions.Center] ] [infoIcon userSpecificTermMsg]
            th [] [str hasNoResult.Term.TermAccession]
            th [] [str (rowIndicesToReadable hasNoResult.RowIndices)]
        ]
    | None ->
        tr [ ] [
            th [] [str "-"]
            th [] [str "-"]
            th [] [str "-"]
            th [] [str "Header"]
        ]
    | anythingElse -> failwith $"""Swate encountered an error when trying to parse {anythingElse} to search results."""


/// Parses TermSearchable to table row. Addresses found search results and free text input.
let searchResultTermToTableElement (term:TermSearchable) =
    match term with
    | isEmpty when term.Term.Name = "" && term.Term.TermAccession = "" ->
        tr [] [
            td [] [str "-"]
            td [ Style [TextAlign TextAlignOptions.Center] ] [str "-"]
            td [] [str "-"]
            td [] [str (rowIndicesToReadable isEmpty.RowIndices)]
        ]
    | hasResult when term.SearchResultTerm.IsSome ->
        tr [ ] [
            td [] [str hasResult.SearchResultTerm.Value.Name]
            td [ Style [TextAlign TextAlignOptions.Center] ] [infoIcon hasResult.SearchResultTerm.Value.Description]
            td [] [str hasResult.SearchResultTerm.Value.Accession]
            td [] [str (rowIndicesToReadable hasResult.RowIndices)]
        ]
    | hasNoResult when term.SearchResultTerm.IsNone ->
        tr [ ] [
            td [ Style [Color NFDIColors.Red.Lighter20] ] [str hasNoResult.Term.Name]
            td [ Style [TextAlign TextAlignOptions.Center] ] [infoIcon userSpecificTermMsg]
            td [] [str hasNoResult.Term.TermAccession]
            td [] [str (rowIndicesToReadable hasNoResult.RowIndices)]
        ]
    | anythingElse -> failwith $"""Swate encountered an error when trying to parse {anythingElse} to search results."""

/// This element is used if the TermSearchable types for the selected building block do not contain a unit.
let tableElement (terms:TermSearchable []) =
    let rowHeader = getBuildingBlockHeader terms
    let bodyRows = getBodyRows terms
    Table.table [
        Table.IsFullWidth
        Table.IsStriped
    ] [
        thead [] [
            tr [] [
                th [Class "toExcelColor"] [str "Name"]
                th [Class "toExcelColor"; Style [TextAlign TextAlignOptions.Center] ] [str "Desc."]
                th [Class "toExcelColor"] [str "TAN"]
                th [Class "toExcelColor"] [str "Row"]
            ]
        ]
        thead [] [
            searchResultTermToTableHeaderElement rowHeader
        ]
        tbody [] [
            for term in bodyRows do 
                yield
                    searchResultTermToTableElement term
        ]
    ]

let buildingBlockDetailModal (model:Model) dispatch =
    let closeMsg = (fun e -> ToggleShowDetails |> BuildingBlockDetails |> dispatch)

    let baseArr = model.BuildingBlockDetailsState.BuildingBlockValues |> Array.sortBy (fun x -> x.RowIndices |> Array.min)

    Modal.modal [ Modal.IsActive true ] [
        Modal.background [
            Props [ OnClick closeMsg ]
        ] [ ]
        Notification.notification [
            Notification.Props [Style [Width "90%"; MaxHeight "80%"]]
        ] [
            Notification.delete [Props [OnClick closeMsg]] []
            tableElement baseArr
        ]
    ]