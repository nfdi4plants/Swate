namespace Swate.Components

open Feliz
open Fable.Core
open ARCtrl
open Fable.Core.JsInterop
open Swate.Components.Shared

module private DataMapTableHelper =
    [<RequireQualifiedAccess>]
    type Modal = Details of CellCoordinate

open DataMapTableHelper

[<Erase; Mangle(false)>]
type DataMapTable =

    [<ReactComponent>]
    static member private ModalDetails
        (index: CellCoordinate, datamap: DataMap, setDatamap, setModal: Modal option -> unit)
        =

        let close = fun () -> setModal None

        let cell = datamap.GetCell(index.x - 1, index.y - 1)

        let tempCell, setTempCell = React.useState (cell)

        let submit =
            fun () ->
                datamap.SetCell(index.x - 1, index.y - 1, tempCell)
                close ()

        let Content =
            match tempCell with
            | CompositeCell.FreeText txt ->
                AnnotationTableModals.InputField.Input(
                    txt,
                    (fun input -> setTempCell (CompositeCell.FreeText input)),
                    label = "Free Text",
                    rmv = close,
                    submit = submit,
                    autofocus = true
                )
            | CompositeCell.Term oa ->
                let parentOa = datamap.GetHeader(index.x - 1).TryGetTerm()

                React.fragment [
                    AnnotationTableModals.InputField.TermCombi(
                        oa |> Term.fromOntologyAnnotation |> Some,
                        (fun (term: Term option) ->
                            term
                            |> Option.map OntologyAnnotation.fromTerm
                            |> Option.map (fun oa -> CompositeCell.Term oa)
                            |> Option.defaultValue CompositeCell.emptyTerm
                            |> setTempCell
                        ),
                        label = "Term name",
                        rmv = close,
                        submit = submit,
                        autofocus = true,
                        ?parentOa = parentOa
                    )
                    AnnotationTableModals.InputField.Input(
                        oa.TermSourceREF |> Option.defaultValue "",
                        (fun input ->
                            let copy = oa.Copy()
                            copy.TermSourceREF <- input |> Option.whereNot System.String.IsNullOrWhiteSpace
                            setTempCell (CompositeCell.Term copy)
                        ),
                        label = "Term source REF",
                        rmv = close,
                        submit = submit
                    )
                    AnnotationTableModals.InputField.Input(
                        oa.TermAccessionNumber |> Option.defaultValue "",
                        (fun input ->
                            let copy = oa.Copy()
                            copy.TermAccessionNumber <- input |> Option.whereNot System.String.IsNullOrWhiteSpace
                            setTempCell (CompositeCell.Term copy)
                        ),
                        label = "Term Accession Number",
                        rmv = close,
                        submit = submit
                    )
                ]
            | CompositeCell.Unitized(v, oa) ->
                let parentOa = datamap.GetHeader(index.x - 1).TryGetTerm()

                React.fragment [
                    AnnotationTableModals.InputField.Input(
                        v,
                        (fun input -> setTempCell (CompositeCell.Unitized(input, oa))),
                        label = "Value",
                        rmv = close,
                        autofocus = true,
                        submit = submit
                    )
                    AnnotationTableModals.InputField.TermCombi(
                        oa |> Term.fromOntologyAnnotation |> Some,
                        (fun (term: Term option) ->
                            term
                            |> Option.map OntologyAnnotation.fromTerm
                            |> Option.map (fun oa -> CompositeCell.Term oa)
                            |> Option.defaultValue CompositeCell.emptyTerm
                            |> setTempCell
                        ),
                        label = "Unit name",
                        rmv = close,
                        submit = submit,
                        ?parentOa = parentOa
                    )
                    AnnotationTableModals.InputField.Input(
                        oa.TermSourceREF |> Option.defaultValue "",
                        (fun input ->
                            let copy = oa.Copy()
                            copy.TermSourceREF <- input |> Option.whereNot System.String.IsNullOrWhiteSpace
                            setTempCell (CompositeCell.Term copy)
                        ),
                        label = "Term source REF",
                        rmv = close,
                        submit = submit
                    )
                    AnnotationTableModals.InputField.Input(
                        oa.TermAccessionNumber |> Option.defaultValue "",
                        (fun input ->
                            let copy = oa.Copy()
                            copy.TermAccessionNumber <- input |> Option.whereNot System.String.IsNullOrWhiteSpace
                            setTempCell (CompositeCell.Term copy)
                        ),
                        label = "Term Accession Number",
                        rmv = close,
                        submit = submit
                    )
                ]
            | CompositeCell.Data data ->
                React.fragment [
                    AnnotationTableModals.InputField.Input(
                        data.FilePath |> Option.defaultValue "",
                        (fun input ->
                            let copy = data.Copy()
                            copy.FilePath <- input |> Option.whereNot System.String.IsNullOrWhiteSpace
                            setTempCell (CompositeCell.Data copy)
                        ),
                        label = "File Path",
                        rmv = close,
                        submit = submit,
                        autofocus = true
                    )
                    AnnotationTableModals.InputField.Input(
                        data.Selector |> Option.defaultValue "",
                        (fun input ->
                            let copy = data.Copy()
                            copy.Selector <- input |> Option.whereNot System.String.IsNullOrWhiteSpace
                            setTempCell (CompositeCell.Data copy)
                        ),
                        label = "Selector",
                        rmv = close,
                        submit = submit,
                        autofocus = true
                    )
                    AnnotationTableModals.InputField.Input(
                        data.Format |> Option.defaultValue "",
                        (fun input ->
                            let copy = data.Copy()
                            copy.Format <- input |> Option.whereNot System.String.IsNullOrWhiteSpace
                            setTempCell (CompositeCell.Data copy)
                        ),
                        label = "File Format",
                        rmv = close,
                        submit = submit,
                        autofocus = true
                    )
                    AnnotationTableModals.InputField.Input(
                        data.SelectorFormat |> Option.defaultValue "",
                        (fun input ->
                            let copy = data.Copy()
                            copy.SelectorFormat <- input |> Option.whereNot System.String.IsNullOrWhiteSpace
                            setTempCell (CompositeCell.Data copy)
                        ),
                        label = "Selector Format",
                        rmv = close,
                        submit = submit,
                        autofocus = true
                    )
                ]


        React.fragment [
            BaseModal.ModalHeader(Html.div "Data Context Details", close)

            BaseModal.ModalContent(Content)

            BaseModal.ModalFooter(
                React.fragment [
                    AnnotationTableModals.FooterButtons.Cancel(close)
                    AnnotationTableModals.FooterButtons.Submit(submit)
                ]
            )
        ]



    [<ReactComponent>]
    static member private Modals(modal: Modal option, setModal: Modal option -> unit, datamap, setDatamap) =

        let isOpen = modal.IsSome
        let setIsOpen = fun (b: bool) -> if b then () else setModal None

        let Content =
            match modal with
            | Some(Modal.Details index) -> DataMapTable.ModalDetails(index, datamap, setDatamap, setModal)
            | None ->
                Html.div [
                    prop.className "swt:alert swt:alert-error swt:font-semibold"
                    prop.text "Unknown pattern. Please report this as a bug. DataMapTable.Modals - no modal selected"
                ]

        BaseModal.BaseModal(isOpen, setIsOpen, children = Content)

    [<ReactComponent>]
    static member private ContextMenu
        (
            datamap: DataMap,
            setDatamap: DataMap -> unit,
            setModal: Modal option -> unit,
            tableRef: IRefValue<TableHandle>,
            containerRef,
            ?debug: bool
        ) =
        let deleteRow =
            fun (index: CellCoordinate) ->
                let isSelected = tableRef.current.SelectHandle.contains index
                let start = index.y - 1

                if not isSelected then
                    datamap.DataContexts.RemoveAt(start)
                else
                    let range = tableRef.current.SelectHandle.getSelectedCellRange().Value
                    let count = range.yEnd - range.yStart + 1
                    datamap.DataContexts.RemoveRange(start, count)

                setDatamap datamap

        ContextMenu.ContextMenu(
            (fun data ->
                let index = data |> unbox<CellCoordinate>

                [
                    ContextMenuItem(
                        text = Html.div "Details",
                        icon = Icons.MagnifyingGlassPlus(),
                        kbdbutton = AnnotationTableContextMenu.ATCMC.KbdHint("D"),
                        onClick = (fun _ -> setModal (Some(Modal.Details index)))
                    )
                    ContextMenuItem(isDivider = true)
                    ContextMenuItem(
                        text = Html.div "Delete Row",
                        icon = Icons.DeleteLeft(),
                        kbdbutton = AnnotationTableContextMenu.ATCMC.KbdHint("DelR"),
                        onClick = (fun x -> deleteRow index)
                    )
                ]
            ),
            ref = containerRef,
            onSpawn =
                (fun e ->
                    let target = e.target :?> Browser.Types.HTMLElement

                    match target.closest ("[data-row][data-column]"), containerRef.current with
                    | Some cell, Some container when container.contains (cell) ->
                        let cell = cell :?> Browser.Types.HTMLElement
                        let row = int cell?dataset?row
                        let col = int cell?dataset?column
                        let indices: CellCoordinate = {| y = row; x = col |}
                        if col > 0 && row > 0 then Some indices else None // disable context menu on index column
                    | _ ->
                        console.log ("No table cell found")
                        None
                ),
            ?debug = debug
        )

    [<ReactComponent(true)>]
    static member DataMapTable(datamap: DataMap, setDatamap: DataMap -> unit, ?height, ?debug: bool) =

        let modal, setModal = React.useState (None: Modal option)
        let tableRef = React.useRef<TableHandle> (unbox null)
        let containerRef = React.useElementRef ()

        let renderCell =
            React.memo (
                (fun (index: CellCoordinate) ->
                    match index with
                    | _ when index.x > 0 && index.y > 0 ->
                        let cell = datamap.GetCell(index.x - 1, index.y - 1)
                        TableCell.CompositeCellInactiveCell(index, cell, ?debug = debug)
                    | _ when index.x > 0 && index.y = 0 ->
                        let header = datamap.GetHeader(index.x - 1).ToString()
                        TableCell.StringInactiveCell(index, header, disableActivation = true, ?debug = debug)
                    | _ ->
                        TableCell.BaseCell(
                            index.y,
                            index.x,
                            Html.text index.y,
                            className =
                                "swt:rounded-0 swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-center swt:cursor-not-allowed swt:w-full swt:h-full swt:bg-base-200"
                        )
                )
            )

        let renderActiveCell =
            React.memo (
                (fun (index: CellCoordinate) ->
                    match index with
                    | _ when index.x > 0 && index.y > 0 ->
                        let cell = datamap.GetCell(index.x - 1, index.y - 1)

                        let setCell =
                            fun newValue ->
                                datamap.SetCell(index.x - 1, index.y - 1, newValue)
                                setDatamap datamap

                        TableCell.CompositeCellActiveCell(index, cell, setCell)

                    | _ when index.x > 0 && index.y = 0 -> Html.div "when index.x > 0 && index.y = 0"
                    | _ -> Html.div "unknown table pattern"
                )
            )


        React.fragment [
            DataMapTable.Modals(modal, setModal, datamap, setDatamap)
            Html.div [
                if debug.IsSome && debug.Value then
                    prop.testId "datamap_table"
                    prop.custom ("data-columncount", datamap.ColumnCount)
                    prop.custom ("data-rowcount", datamap.RowCount)
                prop.className "swt:overflow-auto swt:flex swt:flex-col swt:h-full"
                prop.ref containerRef

                prop.children [
                    DataMapTable.ContextMenu(datamap, setDatamap, setModal, tableRef, containerRef, ?debug = debug)
                    Table.Table(
                        datamap.RowCount + 1,
                        datamap.ColumnCount + 1,
                        renderCell,
                        renderActiveCell,
                        ref = tableRef,
                        ?height = height,
                        onKeydown =
                            (fun (e, selectedCells, activeCell) ->
                                if
                                    e.code = kbdEventCode.f2
                                    || ((e.ctrlKey || e.metaKey)
                                        && e.code = kbdEventCode.enter
                                        && activeCell.IsNone
                                        && selectedCells.count > 0)
                                then
                                    let cell = selectedCells.selectedCellsReducedSet.MinimumElement
                                    setModal (Some(Modal.Details cell))
                                elif e.code = kbdEventCode.delete && selectedCells.count > 0 then
                                    datamap.ClearSelectedCells(tableRef.current.SelectHandle)
                                    datamap.Copy() |> setDatamap
                            )
                    )
                ]
            ]
        ]


    [<ReactComponent>]
    static member Entry() =

        let datamap, setDatamap =
            React.useState (
                ARCtrl.DataMap(
                    ResizeArray [
                        for i in 0..100 do
                            DataContext(
                                name = sprintf "Name %d" i,
                                dataType = DataFile.RawDataFile,
                                format = sprintf "Format %A" i,
                                selectorFormat = sprintf "Selector %A" i,
                                explication = OntologyAnnotation("Explication", "EXP", "EXP:21309813"),
                                unit = OntologyAnnotation("Unit", "UNIT", "UNIT:0000001"),
                                objectType = OntologyAnnotation("ObjectType", "OT", "OT:0000001"),
                                label = sprintf "Label: %d" i,
                                description = sprintf "Description: %d" i,
                                generatedBy = "Kevin F",
                                comments =
                                    ResizeArray(
                                        [
                                            for i in 0..5 do
                                                Comment(sprintf "Comment %d" i, sprintf "Value %d" i)
                                        ]
                                    )
                            )
                    ]
                )
            )

        DataMapTable.DataMapTable(datamap, setDatamap, 400)