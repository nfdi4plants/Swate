namespace MainComponents

open ARCtrl
open Feliz
open Feliz.DaisyUI
open Fable.Core
open Swate.Components

module private CellStylesHelper =

    [<Literal>]
    let Height = 38

open CellStylesHelper

/// Styling here is shared between datamap and annotationTable view

type CellStyles =

    static member cellStyle(adjusted: string list) =
        prop.className [
            "swt:min-w-48 swt:max-w-xl"
            "swt:overflow-visible swt:border swt:border-solid swt:border-base-content swt:cursor-pointer swt:p-0"
            adjusted |> String.concat " "
        ]

    static member private cellInnerContainerStyle(adjusted: string list) =
        prop.className [
            "swt:flex swt:justify-between swt:flex-row swt:flex-nowrap swt:size-full swt:items-center swt:*:truncate swt:*:min-w-0 swt:px-2 swt:py-1 swt:max-w-xl"
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
                    prop.style [ style.fontSize (length.em 1) ]
                    prop.children [
                        if isExtended then
                            Icons.AnglesBackward()
                        else
                            Icons.AnglesForward()
                    ]
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
                Html.span [ prop.className "swt:grow"; prop.text displayValue ]
                if hasValidOA then
                    Html.i [
                        prop.className [ "swt:ml-auto swt:text-primary"; "swt:size-4" ]
                        prop.children [
                            Icons.Check()
                        ]
                    ]
            ]
        ]

    /// <summary>
    /// rowIndex < 0 equals header
    /// </summary>
    /// <param name="rowIndex"></param>
    static member RowLabel(rowIndex: int) =
        Html.th [
            prop.className "swt:items-center swt:text-center swt:w-min swt:px-2 swt:py-1"
            prop.style [ style.resize.none ]
            prop.children [ Html.b (if rowIndex < 0 then "" else $"{rowIndex + 1}") ]
        ]