/// Styling here is shared between datamap and annotationTable view
module MainComponents.CellStyles

open ARCtrl
open Feliz
open Feliz.Bulma
open Fable.Core

let cellStyle (specificStyle: IStyleAttribute list) = prop.style [
        style.minWidth 100
        style.height 22
        style.border(length.px 1, borderStyle.solid, "darkgrey")
        yield! specificStyle
    ]

let cellInnerContainerStyle (specificStyle: IStyleAttribute list) = prop.style [
        style.display.flex;
        style.justifyContent.spaceBetween;
        style.height(length.percent 100);
        style.minHeight(35)
        style.width(length.percent 100)
        style.alignItems.center
        yield! specificStyle
    ]

let basicValueDisplayCell (v: string) =
    Html.span [
        prop.style [
            style.flexGrow 1
            style.padding(length.em 0.5,length.em 0.75)
        ]
        prop.text v
    ]

let compositeCellDisplay (oa: OntologyAnnotation) (displayValue: string) =
    let hasValidOA = oa.TermAccessionShort <> ""
    let v = displayValue
    Html.div [
        prop.classes ["is-flex"]
        prop.style [
            style.flexGrow 1
            style.padding(length.em 0.5,length.em 0.75)
        ]
        prop.children [
            Html.span [
                prop.style [
                    style.flexGrow 1
                ]
                prop.text v
            ]
            if hasValidOA then 
                Bulma.icon [Html.i [
                    prop.style [style.custom("marginLeft", "auto")]
                    prop.className ["fa-solid"; "fa-check"]
                ]]
        ]
    ]

/// <summary>
/// rowIndex < 0 equals header
/// </summary>
/// <param name="rowIndex"></param>
let RowLabel (rowIndex: int) = 
    let t : IReactProperty list -> ReactElement = if rowIndex < 0 then Html.th else Html.td 
    t [
        //prop.style [style.resize.none; style.border(length.px 1, borderStyle.solid, "darkgrey")]
        //prop.children [
        //    Bulma.button.button [
        //        prop.className "px-2 py-1"
        //        prop.style [style.custom ("border", "unset"); style.borderRadius 0]
        //        Bulma.button.isFullWidth
        //        Bulma.button.isStatic
        //        prop.tabIndex -1
        //        prop.text (if rowIndex < 0 then "" else $"{rowIndex+1}")
        //    ]
        //]
        prop.style [style.resize.none; style.border(length.px 1, borderStyle.solid, "darkgrey"); style.height(length.perc 100)]
        prop.children [
            Html.div [
                prop.style [style.height(length.perc 100);]
                prop.className "is-flex is-justify-content-center is-align-items-center px-2 is-unselectable my-grey-out"
                prop.disabled true
                prop.children [
                    Html.b (if rowIndex < 0 then "" else $"{rowIndex+1}")
                ]
            ]
        ]
    ]