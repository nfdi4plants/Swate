namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared

open ARCtrl
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
type private FunctionPage =
| Create
| Update

open Components

module private Components =

    open System

    let calculateRegex (regex:string) (input: string) =
        try
            let regex = Regex(regex)
            let m = regex.Match(input)
            match m.Success with
            | true -> m.Index, m.Length
            | false -> 0,0
        with
            | _ -> 0,0


    let split (start: int) (length: int) (str: string) =
        let s0, s1 =
            str |> Seq.toList |> List.splitAt (start)
        let s1, s2 =
            s1 |> Seq.toList |> List.splitAt (length)
        String.Join("", s0), String.Join("", s1), String.Join("", s2)

    let Tab(targetPage: FunctionPage, currentPage, setPage) =
        Daisy.tab [
            if targetPage = currentPage then tab.active
            prop.onClick (fun _ -> setPage targetPage)
            prop.children [
                Html.a [
                    prop.text (targetPage.ToString())
                ]
            ]
        ]

    let TabNavigation(currentPage, setPage) =
        Daisy.tabs [
            prop.className "grow"
            tabs.bordered
            prop.children [
                Html.ul [
                    Tab(FunctionPage.Create, currentPage, setPage)
                    Tab(FunctionPage.Update, currentPage, setPage)
                ]
            ]
        ]

    let PreviewRow(index:int,cell0: string, cell: string, markedIndices: int*int) =
        Html.tr [
            Html.td index
            Html.td [
                let s0,marked,s2 = split (fst markedIndices) (snd markedIndices) cell0
                Html.span s0
                Html.span [
                    prop.className "has-background-info"
                    prop.text marked
                ]
                Html.span s2
            ]
            Html.td (cell)
        ]

    let PreviewTable(column: CompositeColumn, cellValues: string [], regex) =
        Html.div [
            Daisy.label [
                Daisy.labelText "Preview"
            ]
            Html.div [
                prop.className "overflow-x-auto grow"
                prop.children [
                    Daisy.table [
                        Html.thead [
                            Html.tr [Html.th "";Html.th "Before"; Html.th "After"]
                        ]
                        Html.tbody [
                            let previewCount = 5
                            let preview = Array.takeSafe previewCount cellValues
                            for i in 0 .. (preview.Length-1) do
                                let cell0 = column.Cells.[i].ToString()
                                let cell = preview.[i]
                                let regexMarkedIndex = calculateRegex regex cell0
                                PreviewRow(i,cell0,cell,regexMarkedIndex)
                        ]
                    ]
                ]
            ]
        ]

type UpdateColumn =

    [<ReactComponent>]
    static member private CreateForm(cellValues: string [], setPreview) =
        let baseStr, setBaseStr = React.useState("")
        let suffix, setSuffix = React.useState(false)
        let updateCells (baseStr: string) (suffix:bool) =
            cellValues
            |> Array.mapi (fun i c ->
                match suffix with
                | true -> baseStr + string (i+1)
                | false -> baseStr
            )
            |> setPreview
        React.fragment [
            Daisy.label [
                Daisy.labelText "BAse"
            ]
            Daisy.input [
                prop.autoFocus true
                prop.valueOrDefault baseStr
                prop.onChange(fun s ->
                    setBaseStr s
                    updateCells s suffix
                )
            ]
            Html.label [
                prop.className "is-flex is-align-items-center checkbox"
                prop.style [style.gap (length.rem 0.5)]
                prop.children [
                    Html.input [
                        prop.type' "checkbox"
                        prop.isChecked suffix
                        prop.onChange(fun e ->
                            setSuffix e
                            updateCells baseStr e
                        )
                    ]
                    Html.p [prop.text "Add number suffix"; prop.className "text-sm"]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private UpdateForm(cellValues: string [], setPreview, regex: string, setRegex: string -> unit) =
        let replacement, setReplacement = React.useState("")
        let updateCells (replacement: string) (regex: string) =
            if regex <> "" then
                try
                    let regex = Regex(regex)
                    cellValues
                    |> Array.mapi (fun i c ->
                        let m = regex.Match(c)
                        match m.Success with
                        | true ->
                            let replaced = c.Replace(m.Value, replacement)
                            replaced
                        | false ->
                            c
                    )
                    |> setPreview
                with
                    | _ -> ()
            else
                ()
        Html.div [
            prop.className "is-flex is-flex-direction-row"
            prop.style [style.gap (length.rem 1)]
            prop.children [
                Daisy.label [
                    Daisy.labelText "Regex"
                ]
                Daisy.input [
                    prop.autoFocus true
                    prop.valueOrDefault regex
                    prop.onChange (fun s ->
                        setRegex s;
                        updateCells replacement s
                    )
                ]
                Daisy.label [
                    Daisy.labelText "Replacement"
                ]
                Daisy.input [
                    prop.valueOrDefault replacement
                    prop.onChange (fun s ->
                        setReplacement s;
                        updateCells s regex
                    )
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(index: int, column: CompositeColumn, dispatch) (rmv: _ -> unit) =
        let getCellStrings() = column.Cells |> Array.map (fun c -> c.ToString())
        let preview, setPreview = React.useState(getCellStrings)
        let initPage = if preview.Length = 0 || preview |> String.concat "" = "" then FunctionPage.Create else FunctionPage.Update
        let currentPage, setPage = React.useState(initPage)
        /// This state is only used for update logic
        let regex, setRegex = React.useState("")
        let setPage = fun p ->
            if p <> FunctionPage.Update then
                setRegex ""
            setPage p
        let submit = fun () ->
            preview
            |> Array.map (fun x -> CompositeCell.FreeText x)
            |> fun x -> CompositeColumn.create(column.Header, x)
            |> fun x -> Spreadsheet.SetColumn (index, x)
            |> SpreadsheetMsg
            |> dispatch
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop [ prop.onClick rmv ]
                Daisy.card [
                    prop.style [style.maxHeight(length.percent 70); style.overflowY.hidden]
                    prop.children [
                        Daisy.cardBody [
                            Daisy.cardActions [
                                prop.className "justify-end"
                                prop.children [
                                    Components.DeleteButton(props=[prop.onClick rmv])
                                ]
                            ]
                            Daisy.cardTitle "Update Column"
                            Components.TabNavigation(currentPage, setPage)
                            match currentPage with
                            | FunctionPage.Create -> UpdateColumn.CreateForm(getCellStrings(), setPreview)
                            | FunctionPage.Update -> UpdateColumn.UpdateForm(getCellStrings(), setPreview, regex, setRegex)
                            Components.PreviewTable(column, preview, regex)
                            Daisy.cardActions [
                                Daisy.button.button [
                                    button.info
                                    prop.style [style.marginLeft length.auto]
                                    prop.text "Submit"
                                    prop.onClick(fun e ->
                                        submit()
                                        rmv e
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
