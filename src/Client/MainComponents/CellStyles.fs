namespace MainComponents

open ARCtrl
open Feliz
open Feliz.DaisyUI
open Fable.Core

module private CellStylesHelper =

    [<Literal>]
    let Height = 38

open CellStylesHelper

/// Styling here is shared between datamap and annotationTable view

type CellStyles =

    static member cellStyle(adjusted: string list) =
        prop.className [
            "min-w-48 max-w-xl"
            "h-[38px] min-h-[38px] max-h-[38px] overflow-visible border border-solid border-base-content cursor-pointer"
            adjusted |> String.concat " "
        ]

    static member private cellInnerContainerStyle(adjusted: string list) =
        prop.className [
            "flex justify-between flex-row flex-nowrap size-full items-center *:truncate *:min-w-0 px-2 py-1 max-w-xl"
            adjusted |> String.concat " "
        ]

    static member ExtendHeaderButton(state_extend: Set<int>, columnIndex, setState_extend) =
        let isExtended = state_extend.Contains(columnIndex)

        Html.div [
            prop.style [ style.minWidth 25; style.cursor.pointer ]
            prop.onDoubleClick (fun e ->
                e.stopPropagation ()
                e.preventDefault ()
                ())
            prop.onClick (fun e ->
                e.stopPropagation ()
                e.preventDefault ()

                let nextState =
                    if isExtended then
                        state_extend.Remove(columnIndex)
                    else
                        state_extend.Add(columnIndex)

                setState_extend nextState)
            prop.children [
                Html.i [
                    prop.classes [
                        "fa-sharp"
                        "fa-solid"
                        "fa-angles-up"
                        if isExtended then "fa-rotate-270" else "fa-rotate-90"
                    ]
                    prop.style [ style.fontSize (length.em 1) ]
                ]
            ]
        ]

    static member BasicValueDisplayCell(v: string, extendableButton: ReactElement option) =
        Html.div [
            CellStyles.cellInnerContainerStyle []
            if v.Length > 60 then
                prop.title v
            prop.children [
                Html.span [ prop.text v ]
                if extendableButton.IsSome then
                    extendableButton.Value
            ]
        ]


    static member CompositeCellDisplay(oa: OntologyAnnotation, displayValue: string) =
        let hasValidOA = oa.TermAccessionShort <> ""

        Html.div [
            CellStyles.cellInnerContainerStyle []
            prop.children [
                Html.span [ prop.className "grow"; prop.text displayValue ]
                if hasValidOA then
                    Html.i [ prop.className [ "ml-auto text-primary"; "fa-solid"; "fa-check"; "size-4" ] ]
            ]
        ]

    /// <summary>
    /// rowIndex < 0 equals header
    /// </summary>
    /// <param name="rowIndex"></param>
    static member RowLabel(rowIndex: int) =
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
            prop.style [ style.resize.none ]
            prop.children [
                Html.div [
                    prop.style [ style.height (length.perc 100) ]
                    prop.className "flex items-center justify-center px-2 py-1"
                    prop.disabled true
                    prop.children [ Html.b (if rowIndex < 0 then "" else $"{rowIndex + 1}") ]
                ]
            ]
        ]