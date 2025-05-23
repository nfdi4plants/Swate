namespace Modals

open System
open ARCtrl
open Feliz
open Feliz.DaisyUI
open Swate.Components
open Swate.Components.Shared
open Messages
open Model
open Fable.Core
open Browser.Types

type State = {
    NextHeaderType: CompositeHeaderDiscriminate
    NextIOType: IOType option
} with

    static member init(current) = {
        NextHeaderType = current
        NextIOType = None
    }

type CompositeHeaderModal =

    static member SelectHeaderTypeOption(headerType: CompositeHeaderDiscriminate) =
        let txt = headerType.ToString()
        Html.option [ prop.value txt; prop.text txt ]

    static member SelectHeaderType(state, setState) =
        Html.select [
            prop.className "swt:select swt:join-item"
            prop.value (state.NextHeaderType.ToString())
            prop.onChange (fun (e: string) ->
                {
                    state with
                        NextHeaderType = CompositeHeaderDiscriminate.fromString e
                }
                |> setState)
            prop.children [
                // -- term columns --
                Html.optgroup [
                    prop.label "Term Columns"
                    prop.children [
                        CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Characteristic
                        CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Component
                        CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Factor
                        CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Parameter
                    ]
                ]
                // -- io columns --
                Html.optgroup [
                    prop.label "IO Columns"
                    prop.children [
                        CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Input
                        CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Output
                    ]
                ]
                // -- single columns --
                CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Date
                CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Performer
                CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolDescription
                CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolREF
                CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolType
                CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolUri
                CompositeHeaderModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolVersion
            ]
        ]

    static member SelectIOTypeOption(ioType: IOType) =
        let txt = ioType.ToString()
        Html.option [ prop.value txt; prop.text txt ]

    static member SelectIOType(state, setState) =
        Html.select [
            prop.className "swt:select swt:join-item"
            prop.value (state.NextIOType |> Option.defaultValue IOType.Sample |> _.ToString())
            prop.onChange (fun (e: string) ->
                {
                    state with
                        NextIOType = Some(IOType.ofString e)
                }
                |> setState)
            prop.children [
                CompositeHeaderModal.SelectIOTypeOption IOType.Source
                CompositeHeaderModal.SelectIOTypeOption IOType.Sample
                CompositeHeaderModal.SelectIOTypeOption IOType.Material
                CompositeHeaderModal.SelectIOTypeOption IOType.Data
            ]
        ]

    static member Preview(column: CompositeColumn) =
        let parsedStrList =
            ARCtrl.Spreadsheet.CompositeColumn.toStringCellColumns column |> List.transpose

        let headers, body =
            if column.Cells.Length >= 2 then
                parsedStrList.[0], parsedStrList.[1..]
            else
                parsedStrList.[0], []

        Html.div [
            prop.className "swt:overflow-x-auto swt:grow"
            prop.children [
                //Daisy.table [
                //    table.sm
                //    prop.children [
                //        Html.thead [
                //            Html.tr [
                //                for header in headers do
                //                    Html.th [ prop.className "swt:truncate swt:max-w-16"; prop.text header; prop.title header ]
                //            ]
                //        ]
                //        Html.tbody [
                //            for row in body do
                //                Html.tr [
                //                    for cell in row do
                //                        Html.td [ prop.className "swt:truncate swt:max-w-16"; prop.text cell; prop.title cell ]
                //                ]
                //        ]
                //    ]
                //]
                Html.table [
                    prop.className "swt:table swt:table-sm"
                    prop.children [
                        Html.thead [
                            Html.tr [
                                for header in headers do
                                    Html.th [
                                        prop.className "swt:truncate swt:max-w-16"
                                        prop.text header
                                        prop.title header
                                    ]
                            ]
                        ]
                        Html.tbody [
                            for row in body do
                                Html.tr [
                                    for cell in row do
                                        Html.td [
                                            prop.className "swt:truncate swt:max-w-16"
                                            prop.text cell
                                            prop.title cell
                                        ]
                                ]
                        ]
                    ]
                ]
            ]
        ]

    static member cellsToTermCells(column: CompositeColumn) = [|
        for c in column.Cells do
            if c.isUnitized || c.isTerm then c else c.ToTermCell()
    |]

    static member cellsToFreeText(column) = [|
        for c in column.Cells do
            if c.isFreeText then c else c.ToFreeTextCell()
    |]

    static member cellsToDataOrFreeText(column) = [|
        for c in column.Cells do
            if c.isFreeText || c.isData then c else c.ToDataCell()
    |]

    static member updateColumn(column: CompositeColumn, state) =
        let header = column.Header

        match state.NextHeaderType, state.NextIOType with
        // -- term columns --
        | CompositeHeaderDiscriminate.Characteristic, _ ->
            CompositeColumn.create (
                CompositeHeader.Characteristic(header.ToTerm()),
                CompositeHeaderModal.cellsToTermCells (column)
            )
        | CompositeHeaderDiscriminate.Parameter, _ ->
            CompositeColumn.create (
                CompositeHeader.Parameter(header.ToTerm()),
                CompositeHeaderModal.cellsToTermCells (column)
            )
        | CompositeHeaderDiscriminate.Component, _ ->
            CompositeColumn.create (
                CompositeHeader.Component(header.ToTerm()),
                CompositeHeaderModal.cellsToTermCells (column)
            )
        | CompositeHeaderDiscriminate.Factor, _ ->
            CompositeColumn.create (
                CompositeHeader.Factor(header.ToTerm()),
                CompositeHeaderModal.cellsToTermCells (column)
            )
        // -- input columns --
        | CompositeHeaderDiscriminate.Input, Some IOType.Data ->
            CompositeColumn.create (
                CompositeHeader.Input IOType.Data,
                CompositeHeaderModal.cellsToDataOrFreeText (column)
            )
        | CompositeHeaderDiscriminate.Input, Some io ->
            CompositeColumn.create (CompositeHeader.Input io, CompositeHeaderModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.Input, None ->
            CompositeColumn.create (CompositeHeader.Input IOType.Sample, CompositeHeaderModal.cellsToFreeText (column))
        // -- output columns --
        | CompositeHeaderDiscriminate.Output, Some IOType.Data ->
            CompositeColumn.create (
                CompositeHeader.Output IOType.Data,
                CompositeHeaderModal.cellsToDataOrFreeText (column)
            )
        | CompositeHeaderDiscriminate.Output, Some io ->
            CompositeColumn.create (CompositeHeader.Output io, CompositeHeaderModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.Output, None ->
            CompositeColumn.create (CompositeHeader.Output IOType.Sample, CompositeHeaderModal.cellsToFreeText (column))
        // -- single columns --
        | CompositeHeaderDiscriminate.ProtocolREF, _ ->
            CompositeColumn.create (CompositeHeader.ProtocolREF, CompositeHeaderModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.Date, _ ->
            CompositeColumn.create (CompositeHeader.Date, CompositeHeaderModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.Performer, _ ->
            CompositeColumn.create (CompositeHeader.Performer, CompositeHeaderModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.ProtocolDescription, _ ->
            CompositeColumn.create (CompositeHeader.ProtocolDescription, CompositeHeaderModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.ProtocolType, _ ->
            CompositeColumn.create (CompositeHeader.ProtocolType, CompositeHeaderModal.cellsToTermCells (column))
        | CompositeHeaderDiscriminate.ProtocolUri, _ ->
            CompositeColumn.create (CompositeHeader.ProtocolUri, CompositeHeaderModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.ProtocolVersion, _ ->
            CompositeColumn.create (CompositeHeader.ProtocolVersion, CompositeHeaderModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.Comment, _ (*-> failwith "Comment header type is not yet implemented"*)
        | CompositeHeaderDiscriminate.Freetext, _ ->
            CompositeColumn.create (
                CompositeHeader.FreeText(header.ToString()),
                CompositeHeaderModal.cellsToFreeText (column)
            )
    //failwith "Freetext header type is not yet implemented"

    static member header = Html.p "Update Column"

    static member modalActivity(state, setState) =
        Html.div [
            prop.children [
                Html.div [
                    prop.className "swt:join"
                    prop.children [
                        CompositeHeaderModal.SelectHeaderType(state, setState)
                        match state.NextHeaderType with
                        | CompositeHeaderDiscriminate.Output
                        | CompositeHeaderDiscriminate.Input -> CompositeHeaderModal.SelectIOType(state, setState)
                        | _ -> Html.none
                    ]
                ]
            ]
        ]

    static member placeHolderTermCell =
        CompositeCell.createTermFromString ("Name", "Term-Source-Reference", "Term-Accession-Number")

    static member placeHolderUnitCell =
        CompositeCell.createUnitizedFromString ("Value", "Name", "Term-Source-Reference", "Term-Accession-Number")

    static member placeHolderDataCell =
        CompositeCell.createDataFromString ("Value", "Format", "SelectorFormat")

    static member content(column0, state) =
        let previewColumn =
            let cells = Array.takeSafe 10 column0.Cells
            //Replace empty cells with placeholder data to represent meaningfull information in the Preview
            cells
            |> Array.iteri (fun i cell ->
                let content = cell.GetContent()

                if (Array.filter (fun item -> item = "") content).Length > 0 then
                    match cell with
                    | cell when cell.isTerm -> cells.[i] <- CompositeHeaderModal.placeHolderTermCell
                    | cell when cell.isUnitized -> cells.[i] <- CompositeHeaderModal.placeHolderUnitCell
                    | cell when cell.isData -> cells.[i] <- CompositeHeaderModal.placeHolderTermCell
                    | _ -> cells.[i] <- CompositeHeaderModal.placeHolderTermCell)

            CompositeHeaderModal.updateColumn ({ column0 with Cells = cells }, state)

        React.fragment [
            Html.label [ prop.text "Preview:" ]
            Html.div [
                prop.style [ style.maxHeight (length.perc 85); style.overflow.hidden; style.display.flex ]
                prop.children [ CompositeHeaderModal.Preview(previewColumn) ]
            ]
        ]

    static member footer(columnIndex, column0, state, rmv, dispatch) =
        let submit (e) =
            let nxtCol = CompositeHeaderModal.updateColumn (column0, state)
            Spreadsheet.SetColumn(columnIndex, nxtCol) |> SpreadsheetMsg |> dispatch
            rmv (e)

        Html.div [
            prop.className "swt:justify-end swt:flex gap-2"
            prop.style [ style.marginLeft length.auto ]
            prop.children [
                //Daisy.button.button [ prop.onClick rmv; button.outline; prop.text "Cancel" ]
                Html.button [
                    prop.className "swt:btn swt:btn-outline"
                    prop.text "Cancel"
                    prop.onClick rmv
                ]
                //Daisy.button.button [ button.primary; prop.text "Submit"; prop.onClick submit ]
                Html.button [
                    prop.className "swt:btn swt:btn-primary"
                    prop.text "Submit"
                    prop.onClick submit
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(columnIndex: int, model: Model, dispatch, ?rmv) =

        let rmv = defaultArg rmv (Util.RMV_MODAL dispatch)
        let column0 = model.SpreadsheetModel.ActiveTable.GetColumn columnIndex
        let state, setState = React.useState (State.init column0.Header.AsDiscriminate)

        Swate.Components.BaseModal.BaseModal(
            rmv,
            header = Html.p "Update Column",
            modalClassInfo = "lg:max-w-[600px]",
            modalActions = CompositeHeaderModal.modalActivity (state, setState),
            content = CompositeHeaderModal.content (column0, state),
            footer = CompositeHeaderModal.footer (columnIndex, column0, state, rmv, dispatch)
        )