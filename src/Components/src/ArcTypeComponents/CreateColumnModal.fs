namespace Swate.Components

open System.Text.RegularExpressions
open Feliz
open Swate.Components.Shared
open ARCtrl


module ComponentHelper =

    open System

    let calculateRegex (regex: string) (input: string) =
        try
            let regex = Regex(regex)
            let m = regex.Match(input)

            match m.Success with
            | true -> m.Index, m.Length
            | false -> 0, 0
        with _ ->
            0, 0

    let split (start: int) (length: int) (str: string) =
        let s0, s1 = str |> Seq.toList |> List.splitAt (start)
        let s1, s2 = s1 |> Seq.toList |> List.splitAt (length)
        String.Join("", s0), String.Join("", s1), String.Join("", s2)

    let PreviewRow (index: int, cell0: string, cell: string, markedIndices: int * int) =
        Html.tr [
            Html.td index
            Html.td [
                let s0, marked, s2 = split (fst markedIndices) (snd markedIndices) cell0
                Html.span s0
                Html.mark [ prop.className "swt:bg-info swt:text-info-content"; prop.text marked ]
                Html.span s2
            ]
            Html.td (cell)
        ]

    let PreviewTable (column: CompositeColumn, cellValues: string[], regex) =
        React.fragment [
            Html.label [ prop.className "swt:label"; prop.text "Preview" ]
            Html.div [
                prop.className "swt:overflow-x-auto swt:grow"
                prop.children [
                    Html.table [
                        prop.className "swt:table"
                        prop.children [
                            Html.thead [ Html.tr [ Html.th ""; Html.th "Before"; Html.th "After" ] ]
                            Html.tbody [
                                let previewCount = 5
                                let preview = Array.takeSafe previewCount cellValues

                                for i in 0 .. (preview.Length - 1) do
                                    let cell0 = column.Cells.[i].ToString()
                                    let cell = preview.[i]
                                    let regexMarkedIndex = calculateRegex regex cell0
                                    PreviewRow(i, cell0, cell, regexMarkedIndex)
                            ]
                        ]
                    ]
                ]
            ]
        ]

type CreateColumnModal =

    [<ReactComponent>]
    static member private CreateForm(cellValues: string[], setPreview) =
        let baseStr, setBaseStr = React.useState ("")
        let suffix, setSuffix = React.useState (false)

        let updateCells (baseStr: string) (suffix: bool) =
            cellValues
            |> Array.mapi (fun i c ->
                match suffix with
                | true -> baseStr + string (i + 1)
                | false -> baseStr)
            |> setPreview

        React.fragment [
            Html.div [
                prop.className "swt:fieldset"
                prop.children [
                    Html.legend [ prop.className "swt:fieldset-legend"; prop.text "Base" ]
                    Html.input [
                        prop.className "swt:input swt:input-xs swt:sm:input-sm swt:md:input-md"
                        prop.autoFocus true
                        prop.valueOrDefault baseStr
                        prop.onChange (fun (ev: Browser.Types.Event) ->
                            let target = ev.target :?> Browser.Types.HTMLInputElement
                            let value = target.value
                            setBaseStr value
                            updateCells value suffix)
                    ]
                    Html.label [
                        prop.className "swt:label swt:cursor-pointer"
                        prop.children [
                            Html.span "Add number suffix"
                            Html.input [
                                prop.type'.checkbox
                                prop.className "swt:checkbox"
                                prop.isChecked suffix
                                prop.onChange (fun (b: bool) ->
                                    setSuffix b
                                    updateCells baseStr b)
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member CreateColumnModal(columnIndex: int, arcTable:ArcTable, setColumn, rmv: unit -> unit) =

        let column = arcTable.GetColumn(columnIndex)

        let getCellStrings () =
            column.Cells |> Array.map (fun c -> c.ToString())

        let preview, setPreview = React.useState (getCellStrings)

        /// This state is only used for update logic
        let regex, setRegex = React.useState ("")

        let submit =
            fun () ->
                preview
                |> Array.map (fun x -> CompositeCell.FreeText x)
                |> fun x -> CompositeColumn.create (column.Header, x)
                |> fun column -> setColumn column

        let content = ComponentHelper.PreviewTable(column, preview, regex)

        let footer =
            Html.div [
                prop.className "swt:justify-end swt:flex swt:gap-2"
                prop.style [ style.marginLeft length.auto ]
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-outline"
                        prop.text "Cancel"
                        prop.onClick (fun _ -> rmv())
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.style [ style.marginLeft length.auto ]
                        prop.text "Submit"
                        prop.onClick (fun _ ->
                            submit ()
                            rmv())
                    ]
                ]
            ]

        Html.div [
            prop.className "swt:flex swt:flex-col swt:h-full swt:gap-4"
            prop.children [
                Html.div [
                    prop.className "swt:border-b swt:pb-2 swt:mb-2"
                    prop.children [
                        CreateColumnModal.CreateForm(getCellStrings (), setPreview)
                    ]
                ]
                Html.div [
                    prop.className "swt:flex-grow swt:overflow-y-auto swt:h-[200px]"
                    prop.children [
                        content
                    ]
                ]
                Html.div [
                    prop.className "swt:border-t swt:pt-2 swt:mt-2"
                    prop.children [
                        footer
                    ]
                ]
            ]
        ]
