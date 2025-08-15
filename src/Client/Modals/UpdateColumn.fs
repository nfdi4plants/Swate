namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Swate.Components.Shared

open ARCtrl
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
type private FunctionPage =
    | Create
    | Update

open Swate.Components

module private Components =

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

    let Tab (targetPage: FunctionPage, currentPage, setPage) =
        Html.button [
            prop.className [
                "swt:tab"
                if targetPage = currentPage then
                    "swt:tab-active"
            ]
            prop.onClick (fun _ -> setPage targetPage)
            prop.children [ Html.a [ prop.text (targetPage.ToString()) ] ]
        ]

    let TabNavigation (currentPage, setPage) =
        Html.div [
            prop.className "swt:tabs swt:tabs-bordered swt:grow"
            prop.children [
                Tab(FunctionPage.Create, currentPage, setPage)
                Tab(FunctionPage.Update, currentPage, setPage)
            ]
        ]

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

type UpdateColumn =

    [<ReactComponent>]
    static member private CreateForm(cellValues: string[], setPreview) =
        let baseStr, setBaseStr = React.useState ("")
        let suffix, setSuffix = React.useState (false)

        let updateCells (baseStr: string) (suffix: bool) =
            cellValues
            |> Array.mapi (fun i c ->
                match suffix with
                | true -> baseStr + string (i + 1)
                | false -> baseStr
            )
            |> setPreview

        React.fragment [
            Html.div [
                prop.className "swt:fieldset"
                prop.children [
                    Html.legend [ prop.className "swt:fieldset-legend"; prop.text "Base" ]
                    //Daisy.input [
                    Html.input [
                        prop.className "swt:input swt:input-xs swt:sm:input-sm swt:md:input-md"
                        prop.autoFocus true
                        prop.valueOrDefault baseStr
                        prop.onChange (fun (ev: Browser.Types.Event) ->
                            let target = ev.target :?> Browser.Types.HTMLInputElement
                            let value = target.value
                            setBaseStr value
                            updateCells value suffix
                        )
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
                                    updateCells baseStr b
                                )
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private UpdateForm(cellValues: string[], setPreview, regex: string, setRegex: string -> unit) =
        let replacement, setReplacement = React.useState ("")

        let updateCells (replacement: string) (regex: string) =

            if regex <> "" then
                try
                    let regex = Regex(regex)

                    cellValues
                    |> Array.mapi (fun i c ->
                        let m = regex.Match(c)

                        match m.Success with
                        | true ->

                            let replaced =
                                let front = c.[.. m.Index - 1]
                                let tail = c.[m.Index + m.Length ..]
                                front + replacement + tail

                            replaced
                        | false -> c
                    )
                    |> setPreview
                with _ ->
                    ()
            else
                ()

        Html.div [
            prop.className "swt:flex gap-2"
            prop.children [
                Html.div [
                    prop.className "swt:fieldset"
                    prop.children [
                        Html.legend [ prop.className "swt:fieldset-legend"; prop.text "Regex" ]
                        Html.input [
                            prop.autoFocus true
                            prop.className "swt:input swt:input-xs swt:sm:input-sm swt:md:input-md"
                            prop.valueOrDefault regex
                            prop.onChange (fun (v: string) ->
                                setRegex v
                                updateCells replacement v
                            )
                        ]
                        Html.legend [ prop.className "swt:fieldset-legend"; prop.text "Replacement" ]
                        Html.input [
                            prop.className "swt:input swt:input-xs swt:sm:input-sm swt:md:input-md"
                            prop.valueOrDefault replacement
                            prop.onChange (fun (ev: Browser.Types.Event) ->
                                let target = ev.target :?> Browser.Types.HTMLInputElement
                                let value = target.value
                                setReplacement value
                                updateCells value regex
                            )
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(index: int, column: CompositeColumn, dispatch) =

        let isOpen, setIsOpen = React.useState (true)

        let rmv = Util.RMV_MODAL dispatch

        let getCellStrings () =
            column.Cells |> Array.map (fun c -> c.ToString())

        let preview, setPreview = React.useState (getCellStrings)

        let initPage =
            if preview.Length = 0 || preview |> String.concat "" = "" then
                FunctionPage.Create
            else
                FunctionPage.Update

        let currentPage, setPage = React.useState (initPage)
        /// This state is only used for update logic
        let regex, setRegex = React.useState ("")

        let setPage =
            fun p ->
                if p <> FunctionPage.Update then
                    setRegex ""

                setPage p

        let submit =
            fun () ->
                preview
                |> Array.map (fun x -> CompositeCell.FreeText x)
                |> fun x -> CompositeColumn.create (column.Header, x)
                |> fun x -> Spreadsheet.SetColumn(index, x)
                |> SpreadsheetMsg
                |> dispatch

        let modalActivity =
            Html.div [
                Components.TabNavigation(currentPage, setPage)
                match currentPage with
                | FunctionPage.Create -> UpdateColumn.CreateForm(getCellStrings (), setPreview)
                | FunctionPage.Update -> UpdateColumn.UpdateForm(getCellStrings (), setPreview, regex, setRegex)
            ]

        let content = Components.PreviewTable(column, preview, regex)

        let footer =
            Html.div [
                prop.className "swt:justify-end swt:flex swt:gap-2"
                prop.style [ style.marginLeft length.auto ]
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-outline"
                        prop.text "Cancel"
                        prop.onClick rmv
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.style [ style.marginLeft length.auto ]
                        prop.text "Submit"
                        prop.onClick (fun e ->
                            submit ()
                            rmv e
                        )
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.p "Update Column",
            content,
            modalActions = modalActivity,
            footer = footer,
            className = "swt:max-h-screen swt:max-w-4xl swt:flex"
        )
