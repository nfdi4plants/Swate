/// Styling here is shared between datamap and annotationTable view
module MainComponents.CellStyles

open ARCtrl
open Feliz
open Feliz.DaisyUI
open Fable.Core

[<Literal>]
let Height = 38

let cellStyle (adjusted: string list) =
    prop.className [
        "min-w-48 max-w-xl h-[38px] min-h-[38px] max-h-[38px] overflow-visible border border-solid border-base-content cursor-pointer"
        adjusted |> String.concat " "
    ]

let cellInnerContainerStyle (adjusted: string list) =
    prop.className [
        "flex justify-between size-full items-center truncate px-2 py-1"
        adjusted |> String.concat " "
    ]

let basicValueDisplayCell (v: string) =
    Html.span [
        if v.Length > 60 then
            prop.title v
        prop.className [
            "truncate"
        ]
        prop.text v
    ]

let compositeCellDisplay (oa: OntologyAnnotation) (displayValue: string) =
    let hasValidOA = oa.TermAccessionShort <> ""
    let v = displayValue
    React.fragment [
        Html.span [
            prop.className "grow"
            prop.text v
        ]
        if hasValidOA then
            Html.i [
                prop.className ["ml-auto text-primary"; "fa-solid"; "fa-check"; "size-4"]
            ]
    ]

/// <summary>
/// rowIndex < 0 equals header
/// </summary>
/// <param name="rowIndex"></param>
let RowLabel (rowIndex: int) =
    Html.th [
        //prop.style [style.resize.none; style.border(length.px 1, borderStyle.solid, "darkgrey")]
        //prop.children [
        //    Daisy.button.button [
        //        prop.className "px-2 py-1"
        //        prop.style [style.custom ("border", "unset"); style.borderRadius 0]
        //        button.block
        //        Daisy.button.isStatic
        //        prop.tabIndex -1
        //        prop.text (if rowIndex < 0 then "" else $"{rowIndex+1}")
        //    ]
        //]
        prop.className "border border-solid border-base-content"
        prop.style [style.resize.none;]
        prop.children [
            Html.div [
                prop.style [style.height(length.perc 100);]
                prop.className "flex items-center justify-center px-2 py-1"
                prop.disabled true
                prop.children [
                    Html.b (if rowIndex < 0 then "" else $"{rowIndex+1}")
                ]
            ]
        ]
    ]