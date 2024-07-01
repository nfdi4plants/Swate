module Modals.BuildingBlockDetailsModal

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Shared
open TermTypes
open Feliz
open Feliz.Bulma


let private getBuildingBlockHeader (terms:TermSearchable []) =
    terms |> Array.tryFind (fun x -> x.RowIndices = [|0|])

let private getBodyRows (terms:TermSearchable []) =
    terms |> Array.filter (fun x -> x.RowIndices <> [|0|])

/// used to parse rowIndices into subsequent windows e.g. "1..3, 5..8"
let private windowRowIndices (rowIndices:int [])=
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
let private rowIndicesToReadable (rowIndices:int []) =
    if rowIndices.Length > 1 then
        windowRowIndices rowIndices
    elif rowIndices = [|0|] then
        "Header"
    else
        $"{rowIndices.[0]}"

let private infoIcon (txt:string) =
    if txt = "" then
        Html.text "No defintion found"
    else
        Html.span [
            prop.style [style.color NFDIColors.Yellow.Base]
            prop.className "has-tooltip-right has-tooltip-multiline"
            prop.custom("data-tooltip", txt)
            prop.children [
                Bulma.icon [
                    Html.i [prop.className "fa-solid fa-circle-info"]
                ]
            ]
        ]

[<Literal>]
let private userSpecificTermMsg = "This Term was not found in the database."

/// Parses TermSearchable to table row only for HEADERS. Addresses found search results and free text input.
let private searchResultTermToTableHeaderElement (term:TermSearchable option) =
    match term with
    | Some isEmpty when isEmpty.Term.Name = "" && isEmpty.Term.TermAccession = "" ->
        Html.tr [
            Html.th "-"
            Html.th "-"
            Html.th [prop.style [style.textAlign.center]; prop.text "-"]
            Html.th (rowIndicesToReadable isEmpty.RowIndices)
        ]
    | Some hasResult when hasResult.SearchResultTerm.IsSome ->
        Html.tr [
            Html.th hasResult.SearchResultTerm.Value.Name
            Html.th [prop.style [style.textAlign.center]; prop.children [infoIcon hasResult.SearchResultTerm.Value.Description]]
            Html.th hasResult.SearchResultTerm.Value.Accession
            Html.th (rowIndicesToReadable hasResult.RowIndices)
        ]
    | Some hasNoResult when hasNoResult.SearchResultTerm.IsNone ->
        Html.tr [
            Html.th [prop.style [style.color NFDIColors.Red.Lighter20]; prop.text hasNoResult.Term.Name]
            Html.th [prop.style [style.textAlign.center]; prop.children [infoIcon userSpecificTermMsg]]
            Html.th hasNoResult.Term.TermAccession
            Html.th (rowIndicesToReadable hasNoResult.RowIndices)
        ]
    | None ->
        Html.tr [
            Html.th "-"
            Html.th "-"
            Html.th "-"
            Html.th "Header"
        ]
    | anythingElse -> failwith $"""Swate encountered an error when trying to parse {anythingElse} to search results."""


/// Parses TermSearchable to table row. Addresses found search results and free text input.
let private searchResultTermToTableElement (term:TermSearchable) =
    match term with
    | isEmpty when term.Term.Name = "" && term.Term.TermAccession = "" ->
        Html.tr [
            Html.td "-"
            Html.td [prop.style [style.textAlign.center]; prop.text "-"]
            Html.td "-"
            Html.td (rowIndicesToReadable isEmpty.RowIndices)
        ]
    | hasResult when term.SearchResultTerm.IsSome ->
        Html.tr [
            Html.td hasResult.SearchResultTerm.Value.Name
            Html.td [prop.style [style.textAlign.center]; prop.children [infoIcon hasResult.SearchResultTerm.Value.Description]]
            Html.td hasResult.SearchResultTerm.Value.Accession
            Html.td (rowIndicesToReadable hasResult.RowIndices)
        ]
    | hasNoResult when term.SearchResultTerm.IsNone ->
        Html.tr [
            Html.td [prop.style [style.color NFDIColors.Red.Lighter20]; prop.text hasNoResult.Term.Name]
            Html.td [prop.style [style.textAlign.center]; prop.children [infoIcon userSpecificTermMsg]]
            Html.td hasNoResult.Term.TermAccession
            Html.td (rowIndicesToReadable hasNoResult.RowIndices)
        ]
    | anythingElse -> failwith $"""Swate encountered an error when trying to parse {anythingElse} to search results."""

/// This element is used if the TermSearchable types for the selected building block do not contain a unit.
let private tableElement (terms:TermSearchable []) =
    let rowHeader = getBuildingBlockHeader terms
    let bodyRows = getBodyRows terms
    Bulma.table [
        Bulma.table.isFullWidth
        Bulma.table.isStriped
        prop.children [
            Html.thead [
                Html.tr [
                    Html.th [prop.className "toExcelColor"; prop.text "Name"]
                    Html.th [prop.className "toExcelColor"; prop.style [ style.textAlign.center]; prop.text "Desc."]
                    Html.th [prop.className "toExcelColor"; prop.text "TAN"]
                    Html.th [prop.className "toExcelColor"; prop.text "Row"]
                ]
            ]
            Html.thead [
                searchResultTermToTableHeaderElement rowHeader
            ]
            Html.tbody [
                for term in bodyRows do 
                    yield
                        searchResultTermToTableElement term
            ]
        ]
    ]

let buildingBlockDetailModal (model:BuildingBlockDetailsState, dispatch) (rmv: _ -> unit) =
    let closeMsg = fun e ->
        rmv e
        UpdateBuildingBlockValues [||] |> BuildingBlockDetails |> dispatch

    let baseArr = model.BuildingBlockValues |> Array.sortBy (fun x -> x.RowIndices |> Array.min)

    Bulma.modal [
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground [
                prop.onClick closeMsg
            ]
            Bulma.notification [
                prop.style [style.width(length.percent 90); style.maxHeight (length.percent 80)]
                prop.children [
                    Bulma.delete [prop.onClick closeMsg]
                    tableElement baseArr
                ]
            ]
        ]
    ]