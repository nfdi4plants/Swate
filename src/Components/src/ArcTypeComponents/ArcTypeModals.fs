namespace Swate.Components

open System
open ARCtrl
open Feliz
open Feliz.DaisyUI
open Swate.Components
open Swate.Components.Shared
open Fable.Core
open Browser.Types


module private ArcTypeModalsUtil =
    let inputKeydownHandler =
        fun (e: KeyboardEvent) submit cancel ->
            match e.code with
            | kbdEventCode.enter ->
                if e.ctrlKey || e.metaKey then
                    e.preventDefault ()
                    e.stopPropagation ()
                    submit ()
            | kbdEventCode.escape ->
                e.preventDefault ()
                e.stopPropagation ()
                cancel ()
            | _ -> ()

type InputField =
    static member Input(v: string, setter: string -> unit, label: string, rmv, submit, ?autofocus: bool) =
        let autofocus = defaultArg autofocus false

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:w-full"
            prop.children [
                Html.label [ prop.className "swt:label"; prop.text label ]
                Html.input [
                    prop.className "swt:input swt:w-full"
                    prop.autoFocus autofocus
                    prop.valueOrDefault v
                    prop.onChange (fun (input: string) -> setter input)
                    prop.onKeyDown (fun (e: KeyboardEvent) -> ArcTypeModalsUtil.inputKeydownHandler e submit rmv)
                ]
            ]
        ]

    static member Show(v: string, label: string, rmv) =

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:w-full"
            prop.children [
                Html.label [ prop.className "swt:label"; prop.text label ]
                Html.input [
                    prop.className "swt:input swt:w-full"
                    prop.readOnly true
                    prop.valueOrDefault v
                ]
            ]
        ]

    static member TermCombi
        (
            v: Term option,
            setter: Term option -> unit,
            label: string,
            rmv,
            submit,
            ?autofocus: bool,
            ?parentOa: OntologyAnnotation
        ) =
        let autofocus = defaultArg autofocus false

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.label [ prop.className "swt:label"; prop.text label ]
                TermSearch.TermSearch(
                    setter,
                    term = v,
                    classNames = TermSearchStyle(U2.Case1 "swt:border-current"),
                    advancedSearch = U2.Case2 true,
                    showDetails = true,
                    autoFocus = autofocus,
                    portalModals = Browser.Dom.document.body,
                    onKeyDown = (fun (e: KeyboardEvent) -> ArcTypeModalsUtil.inputKeydownHandler e submit rmv),
                    ?parentId = (parentOa |> Option.map (fun oa -> oa.TermAccessionShort))
                )
            ]
        ]

type FooterButtons =
    static member Cancel(rmv: unit -> unit) =

        //Daisy.button.button [ button.outline; prop.text "Cancel"; prop.onClick (fun e -> rmv ()) ]
        Html.button [
            prop.className "swt:btn swt:btn-outline"
            prop.text "Cancel"
            prop.onClick (fun _ -> rmv ())
        ]

    static member Submit(submitOnClick: unit -> unit) =
        //Daisy.button.button [
        //    button.primary
        //    prop.text "Submit"
        //    prop.className "ml-auto"
        //    prop.onClick (fun e -> submitOnClick ())
        //]
        Html.button [
            prop.className "swt:btn swt:btn-primary swt:ml-auto"
            prop.text "Submit"
            prop.onClick (fun e -> submitOnClick ())
        ]


[<Mangle(false); Erase>]
type CompositeCellModal =

    /// pr is required to make indicators on termsearch not overflow
    /// pl is required to make the input ouline when focused not cut of
    static member BaseModalContentClassOverride =
        "swt:overflow-y-auto swt:overflow-x-hidden swt:space-y-2 swt:pl-1 swt:pr-4 swt:py-1"

    static member TermModal
        (oa: OntologyAnnotation, rmv, ?relevantCompositeHeader: CompositeHeader, ?setOa: OntologyAnnotation -> unit, ?setHeader: CompositeHeader -> unit, ?debug: string)
        =
        let initTerm = Term.fromOntologyAnnotation oa
        let isOpen, setIsOpen = React.useState (true)
        let tempTerm, setTempTerm = React.useState (initTerm)

        let submit =
            fun () ->
                if setOa.IsSome then
                    tempTerm |> Term.toOntologyAnnotation |> setOa.Value
                    rmv ()
                elif setHeader.IsSome then
                    let header =
                        match relevantCompositeHeader.Value with
                        | CompositeHeader.Characteristic _ ->
                            CompositeHeader.Characteristic (Term.toOntologyAnnotation tempTerm)
                        | CompositeHeader.Component _ ->
                            CompositeHeader.Component (Term.toOntologyAnnotation tempTerm)
                        | CompositeHeader.Factor _ ->
                            CompositeHeader.Factor (Term.toOntologyAnnotation tempTerm)
                        | CompositeHeader.Parameter _ ->
                            CompositeHeader.Parameter (Term.toOntologyAnnotation tempTerm)
                        | _ -> failwith $"Unknown CompositeHeader type {relevantCompositeHeader.Value.ToString()}"
                    header
                    |> setHeader.Value
                    rmv ()
                else
                    failwith "At least one set parameter must be set!"

        let parentOa = relevantCompositeHeader |> Option.map (fun h -> h.ToTerm())

        BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.div "Term",
            React.fragment [
                InputField.TermCombi(
                    Some tempTerm,
                    (fun t -> t |> Option.defaultValue (Term()) |> setTempTerm),
                    "Term Name",
                    rmv,
                    submit,
                    autofocus = debug.IsNone,
                    ?parentOa = parentOa
                )
                InputField.Input(
                    (tempTerm.source |> Option.defaultValue ""),
                    (fun input ->
                        tempTerm.source <- Option.whereNot System.String.IsNullOrWhiteSpace input
                        setTempTerm (tempTerm)
                    ),
                    "Source",
                    rmv,
                    submit
                )
                InputField.Input(
                    (tempTerm.id |> Option.defaultValue ""),
                    (fun input ->
                        tempTerm.id <- Option.whereNot System.String.IsNullOrWhiteSpace input
                        setTempTerm (tempTerm)
                    ),
                    "Accession Number",
                    rmv,
                    submit
                )
            ],
            footer = React.fragment [ FooterButtons.Cancel(rmv); FooterButtons.Submit(submit) ],
            //contentClassInfo = CompositeCellModal.BaseModalContentClassOverride,
            ?debug = debug
        )

    static member UnitizedModal
        (
            value: string,
            oa: OntologyAnnotation,
            setUnitized: string -> OntologyAnnotation -> unit,
            rmv,
            ?relevantCompositeHeader: CompositeHeader,
            ?debug
        ) =
        let initTerm = Term.fromOntologyAnnotation oa
        let isOpen, setIsOpen = React.useState (true)
        let tempValue, setTempValue = React.useState (value)
        let tempTerm, setTempTerm = React.useState (initTerm)

        let submit =
            fun () ->
                let oa = tempTerm |> Term.toOntologyAnnotation
                setUnitized tempValue oa
                rmv ()

        let parentOa = relevantCompositeHeader |> Option.map (fun h -> h.ToTerm())

        BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.div "Unitized",
            React.fragment [
                InputField.Input(
                    tempValue,
                    (fun input -> setTempValue input),
                    "Value",
                    rmv,
                    submit,
                    autofocus = debug.IsNone
                )
                InputField.TermCombi(
                    Some tempTerm,
                    (fun t -> t |> Option.defaultValue (Term()) |> setTempTerm),
                    "Term Name",
                    rmv,
                    submit,
                    ?parentOa = parentOa
                )
                InputField.Input(
                    (tempTerm.source |> Option.defaultValue ""),
                    (fun input ->
                        tempTerm.source <- Option.whereNot System.String.IsNullOrWhiteSpace input
                        setTempTerm (tempTerm)
                    ),
                    "Source",
                    rmv,
                    submit
                )
                InputField.Input(
                    (tempTerm.id |> Option.defaultValue ""),
                    (fun input ->
                        tempTerm.id <- Option.whereNot System.String.IsNullOrWhiteSpace input
                        setTempTerm (tempTerm)
                    ),
                    "Accession Number",
                    rmv,
                    submit
                )
            ],
            footer = React.fragment [ FooterButtons.Cancel(rmv); FooterButtons.Submit(submit) ],
            //contentClassInfo = CompositeCellModal.BaseModalContentClassOverride,
            ?debug = debug
        )

    static member private submit ((setValue:('a -> unit) option), (setHeader: (CompositeHeader -> unit) option), value, headerValue, rmv) =
        try
            if setValue.IsSome then
                setValue.Value value
                rmv ()
            elif setHeader.IsSome then
                let header = headerValue
                header
                |> setHeader.Value
                rmv ()
            else
                failwith "At least one set parameter must be given!"
        with _ -> ()

    static member FreeTextModal(value: string, rmv, ?setText: string -> unit, ?setHeader: CompositeHeader -> unit, ?debug) =
        let tempValue, setTempValue = React.useState (value)
        let isOpen, setIsOpen = React.useState (true)

        let submit =
            fun () ->
                let headerValue = CompositeHeader.OfHeaderString tempValue
                CompositeCellModal.submit(setText, setHeader, tempValue, headerValue, rmv)

        BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.div "Freetext",
            React.fragment [
                InputField.Input(
                    tempValue,
                    (fun input -> setTempValue input),
                    "Value",
                    rmv,
                    submit,
                    autofocus = debug.IsNone
                )
            ],
            footer = React.fragment [ FooterButtons.Cancel(rmv); FooterButtons.Submit(submit) ],
            //contentClassInfo = CompositeCellModal.BaseModalContentClassOverride,
            ?debug = debug
        )

    static member DataModal
        (value: ARCtrl.Data, rmv, ?relevantCompositeHeader: CompositeHeader, ?setData: ARCtrl.Data -> unit, ?setHeader: CompositeHeader -> unit, ?debug)
        =
        let tempData, setTempData = React.useState (value)
        let isOpen, setIsOpen = React.useState (true)

        let submit =
            fun () ->
                try
                    let filePath = defaultArg tempData.FilePath ""
                    let headerValue = CompositeHeader.OfHeaderString filePath
                    CompositeCellModal.submit(setData, setHeader, tempData, headerValue, rmv)
                with _ -> ()

        BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.div "Data",
            React.fragment [
                InputField.Input(
                    (tempData.FilePath |> Option.defaultValue ""),
                    (fun input ->
                        tempData.FilePath <- Option.whereNot System.String.IsNullOrWhiteSpace input
                        setTempData tempData
                    ),
                    "File Path",
                    rmv,
                    submit,
                    autofocus = debug.IsNone
                )
                if setData.IsSome then
                    InputField.Input(
                        (tempData.Selector |> Option.defaultValue ""),
                        (fun input ->
                            tempData.Selector <- Option.whereNot System.String.IsNullOrWhiteSpace input
                            setTempData tempData
                        ),
                        "Selector",
                        rmv,
                        submit
                    )
                    InputField.Input(
                        (tempData.SelectorFormat |> Option.defaultValue ""),
                        (fun input ->
                            tempData.SelectorFormat <- Option.whereNot System.String.IsNullOrWhiteSpace input
                            setTempData tempData
                        ),
                        "Selector Format",
                        rmv,
                        submit
                    )
            ],
            footer = React.fragment [ FooterButtons.Cancel(rmv); FooterButtons.Submit(submit) ],
            //contentClassInfo = CompositeCellModal.BaseModalContentClassOverride,
            ?debug = debug
        )

    [<ReactComponent>]
    static member CompositeCellModal
        (
            compositeCell: CompositeCell,
            setCell: CompositeCell -> unit,
            rmv: unit -> unit,
            ?relevantCompositeHeader: CompositeHeader,
            ?debug: bool
        ) =

        match compositeCell with
        | CompositeCell.Term oa ->
            let setOa = fun oa -> setCell (CompositeCell.Term oa)
            let debug =
                if debug.IsSome && debug.Value then
                    Some "Details_Term"
                else
                    None
            CompositeCellModal.TermModal(oa, rmv, ?relevantCompositeHeader = relevantCompositeHeader, setOa = setOa, ?debug = debug)
        | CompositeCell.Unitized(v, oa) ->
            let setUnitized = fun v oa -> setCell (CompositeCell.Unitized(v, oa))
            let debug =
                if debug.IsSome && debug.Value then
                    Some "Details_Unitized"
                else
                    None
            CompositeCellModal.UnitizedModal(
                v,
                oa,
                setUnitized,
                rmv,
                ?relevantCompositeHeader = relevantCompositeHeader,
                ?debug = debug
            )
        | CompositeCell.FreeText text ->
            let setText = fun text -> setCell (CompositeCell.FreeText text)
            let debug =
                if debug.IsSome && debug.Value then
                    Some "Details_FreeText"
                else
                    None
            CompositeCellModal.FreeTextModal(text, rmv, setText = setText, ?debug = debug)
        | CompositeCell.Data data ->
            let setData = fun data -> setCell (CompositeCell.Data data)
            let debug =
                if debug.IsSome && debug.Value then
                    Some "Details_Data"
                else
                    None
            CompositeCellModal.DataModal(data, rmv, setData = setData, ?relevantCompositeHeader = relevantCompositeHeader, ?debug = debug)

    [<ReactComponent>]
    static member CompositeHeaderModal
        (
            header: CompositeHeader,
            setHeader: CompositeHeader -> unit,
            rmv: unit -> unit
        ) =
        match header with
        | compositeHeader when compositeHeader.IsTermColumn ->
            let setOa = fun oa -> setHeader oa
            let oa = header.ToTerm()
            CompositeCellModal.TermModal(oa, rmv, setHeader = setOa, relevantCompositeHeader = header)
        | compositeHeader when compositeHeader.IsDataColumn ->
            let data = Data.create(Name = header.ToString())
            CompositeCellModal.DataModal(data, rmv, relevantCompositeHeader = header, setHeader = setHeader)
        | compositeHeader when compositeHeader.isInput
            || compositeHeader.isOutput
            || compositeHeader.isFreeText ->
            let setText = fun v -> setHeader v
            let text = header.ToString()
            CompositeCellModal.FreeTextModal(text, rmv, setHeader = setText)
        | _ -> failwith $"Unknown type of header {header}"


type ContextMenuModals =
    static member PasteFullColumnsModal
        (
            arcTable: ArcTable,
            setArcTable,
            addColumns: {| data: ResizeArray<CompositeColumn>; columnIndex: int |},
            setModal: AnnotationTable.ModalTypes -> unit,
            tableRef: IRefValue<TableHandle>
        ) =
        let isOpen, setIsOpen = React.useState (true)

        let rmv =
            fun _ ->
                tableRef.current.focus ()
                setModal AnnotationTable.ModalTypes.None

        let compositeColumns = addColumns.data |> Array.ofSeq

        let addColumnsBtn compositeColumns columnIndex =
            Html.button [
                prop.className "swt:btn swt:btn-outline swt:btn-primary"
                prop.text "Confirm"
                prop.onClick (fun _ ->
                    arcTable.AddColumns(compositeColumns, columnIndex, false, false)
                    arcTable.Copy() |> setArcTable
                    rmv ()
                )
            ]

        let rows =
            compositeColumns
            |> Array.map (fun compositeColumn -> compositeColumn.Cells)
            |> Array.transpose

        BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.div "Headers have been detected",
            React.fragment [
                Html.div [
                    Html.text "Preview"
                    Html.div [
                        prop.className "swt:overflow-x-auto"
                        prop.children [
                            Html.table [
                                prop.className "swt:table swt:table-xs"
                                prop.children [
                                    Html.thead [
                                        Html.tr (
                                            compositeColumns
                                            |> Array.map (fun compositeColumn ->
                                                Html.th (compositeColumn.Header.ToString())
                                            )
                                        )
                                    ]
                                    Html.tbody (
                                        rows
                                        |> Array.map (fun compositeColumn ->
                                            Html.tr (
                                                compositeColumn |> Array.map (fun cell -> Html.td (cell.ToString()))
                                            )
                                        )
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
            ],
            footer =
                React.fragment [
                    FooterButtons.Cancel(rmv)
                    addColumnsBtn compositeColumns (addColumns.columnIndex + 1)
                ]
            //contentClassInfo = CompositeCellModal.BaseModalContentClassOverride
        )

    [<ReactComponent>]
    static member MoveColumnModal
        (
            arcTable: ArcTable,
            setArcTable: ArcTable -> unit,
            arcTableIndex: CellCoordinate,
            uiTableIndex: CellCoordinate,
            setModal: AnnotationTable.ModalTypes -> unit,
            tableRef: IRefValue<TableHandle>,
            ?debug: bool
        ) =
        let rmv =
            fun _ ->
                tableRef.current.focus ()
                setModal AnnotationTable.ModalTypes.None

        let isOpen, setIsOpen = React.useState (true)

        let isInSelected = tableRef.current.SelectHandle.contains (uiTableIndex)

        let columnIndices =
            if isInSelected then
                let range = tableRef.current.SelectHandle.getSelectedCellRange().Value
                [| range.xStart - 1 .. range.xEnd - 1 |]
            else
                [| arcTableIndex.x |]

        let Subtable = arcTable.Subtable(columnIndices)

        let tempTable =
            React.useRef (
                let table = arcTable.Copy()
                table.RemoveColumns columnIndices
                table
            )

        /// We try to avoid any out of bounds errors by limiting the range of the index
        let MaxIndex = tempTable.current.ColumnCount - 1

        let selectedIndex, setSelectedIndex = React.useState (0)

        let submit =
            fun _ ->
                let table = tempTable.current.Copy()
                table.Join(Subtable, selectedIndex, TableJoinOptions.WithValues, skipFillMissing = true)
                setArcTable (table)
                rmv ()

        let modalActivity =
            Html.label [
                prop.className "swt:input swt:w-fit"
                prop.children [
                    Html.input [
                        prop.type'.number
                        prop.onChange (fun (input: int) -> setSelectedIndex (input - 1))
                        prop.defaultValue (selectedIndex + 1)
                        prop.min 1
                        prop.max (MaxIndex + 2)
                    ]
                    Html.span [
                        prop.className "swt:label"
                        prop.textf "%i|%i" (selectedIndex + 1) (MaxIndex + 2)
                    ]
                ]
            ]

        let content =
            let mkRow (index: int, value: string option, isInserted) =
                Html.tr [
                    if isInserted then
                        prop.className "swt:bg-success swt:success-content swt:border-success-content"
                    prop.children [
                        Html.td (if value.IsSome then sprintf "%i" (index + 1) else "")
                        Html.td (value |> Option.defaultValue "")
                    ]
                ]

            React.fragment [
                Html.table [
                    prop.className "swt:table"
                    prop.children [
                        Html.thead [ Html.tr [ Html.th "Index"; Html.th "Column" ] ]
                        Html.tbody [
                            for i in 0 .. tempTable.current.ColumnCount do // do columncount instead of columncount - 1 to included append option
                                if i = selectedIndex then
                                    for subIndex in 0 .. Subtable.ColumnCount - 1 do
                                        mkRow (i + subIndex, Subtable.Headers.[subIndex].ToString() |> Some, true)

                                if i < tempTable.current.ColumnCount then
                                    let index = if i < selectedIndex then i else i + Subtable.ColumnCount
                                    mkRow (index, tempTable.current.Headers.[i].ToString() |> Some, false)
                        ]
                    ]
                ]
            ]

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
                    Html.a [
                        prop.className "swt:btn swt:btn-primary"
                        prop.text "Submit"
                        prop.onClick submit
                    ]
                ]
            ]

        let debugString =
            if debug.IsSome && debug.Value then
                Some "Move_Column"
            else
                None

        Swate.Components.BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.p (
                if Subtable.ColumnCount > 1 then
                    "Move Columns"
                else
                    "Move Column"
            ),
            content,
            modalActions = modalActivity,
            footer = footer,
            ?debug = debugString
        )

    [<ReactComponent>]
    static member ErrorModal (
        exn: string,
        setModal: AnnotationTable.ModalTypes -> unit,
        tableRef: IRefValue<TableHandle>
        ) =

        let rmv =
            fun _ ->
                tableRef.current.focus ()
                setModal AnnotationTable.ModalTypes.None

        ErrorBaseModal.ErrorBaseModal(
            (fun _ -> rmv ()),
            exn
        )

    [<ReactComponent>]
    static member UnknownPasteCase (
        data: string [] [],
        headers: CompositeHeader [],
        setModal: AnnotationTable.ModalTypes -> unit,
        tableRef: IRefValue<TableHandle>
        ) =

        let rmv =
            fun _ ->
                tableRef.current.focus ()
                setModal AnnotationTable.ModalTypes.None

        let sHeaders = headers |> Array.map (fun header -> header.ToString())

        let msg =
            [|
                $"We cannot determine the paste case for the data in combination with the selected headers."
                $"data: {data}"
                $"headers: {sHeaders}"
                "Please, create an Issue on Github and provide the data and headers!"
            |]
            |> String.concat System.Environment.NewLine

        ErrorBaseModal.ErrorBaseModal(
            (fun _ -> rmv ()),
            msg
        )