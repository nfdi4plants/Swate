namespace Modals

open Feliz
open Feliz.DaisyUI
open Messages
open Swate.Components.Shared
open Spreadsheet
open Model
open Swate

open ARCtrl

type private State =
    {
        NextHeaderType: CompositeHeaderDiscriminate
        NextIOType: IOType option
    } with
        static member init(current) = {
            NextHeaderType = current
            NextIOType = None
        }

module private EditColumnComponents =

    let BackButton cancel =
        Daisy.button.button [
            prop.onClick cancel
            button.outline
            prop.text "Back"
        ]

    let SubmitButton(submit) =
        Daisy.button.button [
            button.primary
            prop.text "Submit"
            prop.onClick submit
        ]

    let SelectHeaderTypeOption(headerType: CompositeHeaderDiscriminate) =
        let txt = headerType.ToString()
        Html.option [
            prop.value txt
            prop.text txt
        ]

    let SelectHeaderType(state, setState) =
        Html.select [
            prop.className "select select-bordered join-item"
            prop.value (state.NextHeaderType.ToString())
            prop.onChange (fun (e: string) -> {state with NextHeaderType = CompositeHeaderDiscriminate.fromString e} |> setState )
            prop.children [
                // -- term columns --
                Html.optgroup [
                    prop.label "Term Columns"
                    prop.children [
                        SelectHeaderTypeOption CompositeHeaderDiscriminate.Characteristic
                        SelectHeaderTypeOption CompositeHeaderDiscriminate.Component
                        SelectHeaderTypeOption CompositeHeaderDiscriminate.Factor
                        SelectHeaderTypeOption CompositeHeaderDiscriminate.Parameter
                    ]
                ]
                // -- io columns --
                Html.optgroup [
                    prop.label "IO Columns"
                    prop.children [
                        SelectHeaderTypeOption CompositeHeaderDiscriminate.Input
                        SelectHeaderTypeOption CompositeHeaderDiscriminate.Output
                    ]
                ]
                // -- single columns --
                SelectHeaderTypeOption CompositeHeaderDiscriminate.Date
                SelectHeaderTypeOption CompositeHeaderDiscriminate.Performer
                SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolDescription
                SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolREF
                SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolType
                SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolUri
                SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolVersion
            ]
        ]

    let SelectIOTypeOption(ioType: IOType) =
        let txt = ioType.ToString()
        Html.option [
            prop.value txt
            prop.text txt
        ]

    let SelectIOType(state, setState) =
        Html.select [
            prop.className "select select-bordered join-item"
            prop.value (state.NextIOType |> Option.defaultValue IOType.Sample |> _.ToString())
            prop.onChange (fun (e: string) -> {state with NextIOType = Some (IOType.ofString e)} |> setState )
            prop.children [
                SelectIOTypeOption IOType.Source
                SelectIOTypeOption IOType.Sample
                SelectIOTypeOption IOType.Material
                SelectIOTypeOption IOType.Data
            ]
        ]

    let Preview(column: CompositeColumn) =
        let parsedStrList = ARCtrl.Spreadsheet.CompositeColumn.toStringCellColumns column |> List.transpose
        let headers, body =
            if column.Cells.Length >= 2 then
                parsedStrList.[0], parsedStrList.[1..]
            else
                parsedStrList.[0], []
        Html.div [
            prop.className "overflow-x-auto grow"
            prop.children [
                Daisy.table [
                    table.sm
                    prop.children [
                        Html.thead [
                            Html.tr [
                                for header in headers do
                                    Html.th [
                                        prop.className "truncate max-w-16"
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
                                            prop.className "truncate max-w-16"
                                            prop.text cell
                                            prop.title cell
                                        ]
                                ]
                        ]
                    ]
                ]
            ]
        ]

open EditColumnComponents

type EditColumn =

    [<ReactComponent>]
    static member Main (columnIndex: int, model: Model, dispatch) =
        let column0 = model.SpreadsheetModel.ActiveTable.GetColumn columnIndex
        let state, setState = React.useState(State.init column0.Header.AsDiscriminate)
        let rmv = Util.RMV_MODAL dispatch
        let cellsToTermCells(column:CompositeColumn) =
            [|for c in column.Cells do if c.isUnitized || c.isTerm then c else c.ToTermCell()|]
        let cellsToFreeText(column) =
            [|for c in column.Cells do if c.isFreeText then c else c.ToFreeTextCell()|]
        let cellsToDataOrFreeText(column) =
            [|for c in column.Cells do if c.isFreeText || c.isData then c else c.ToDataCell()|]
        let updateColumn (column: CompositeColumn) =
            let header = column0.Header
            match state.NextHeaderType, state.NextIOType with
            // -- term columns --
            | CompositeHeaderDiscriminate.Characteristic, _  ->
                CompositeColumn.create(CompositeHeader.Characteristic (header.ToTerm()), cellsToTermCells(column))
            | CompositeHeaderDiscriminate.Parameter, _ ->
                CompositeColumn.create(CompositeHeader.Parameter (header.ToTerm()), cellsToTermCells(column))
            | CompositeHeaderDiscriminate.Component, _ ->
                CompositeColumn.create(CompositeHeader.Component (header.ToTerm()), cellsToTermCells(column))
            | CompositeHeaderDiscriminate.Factor, _ ->
                CompositeColumn.create(CompositeHeader.Factor (header.ToTerm()), cellsToTermCells(column))
            // -- input columns --
            | CompositeHeaderDiscriminate.Input, Some IOType.Data ->
                CompositeColumn.create(CompositeHeader.Input IOType.Data, cellsToDataOrFreeText(column))
            | CompositeHeaderDiscriminate.Input, Some io ->
                CompositeColumn.create(CompositeHeader.Input io, cellsToFreeText(column))
            | CompositeHeaderDiscriminate.Input, None ->
                CompositeColumn.create(CompositeHeader.Input IOType.Sample, cellsToFreeText(column))
            // -- output columns --
            | CompositeHeaderDiscriminate.Output, Some IOType.Data ->
                CompositeColumn.create(CompositeHeader.Output IOType.Data, cellsToDataOrFreeText(column))
            | CompositeHeaderDiscriminate.Output, Some io ->
                CompositeColumn.create(CompositeHeader.Output io, cellsToFreeText(column))
            | CompositeHeaderDiscriminate.Output, None ->
                CompositeColumn.create(CompositeHeader.Output IOType.Sample, cellsToFreeText(column))
            // -- single columns --
            | CompositeHeaderDiscriminate.ProtocolREF, _ ->
                CompositeColumn.create(CompositeHeader.ProtocolREF, cellsToFreeText(column))
            | CompositeHeaderDiscriminate.Date, _ ->
                CompositeColumn.create(CompositeHeader.Date, cellsToFreeText(column))
            | CompositeHeaderDiscriminate.Performer, _ ->
                CompositeColumn.create(CompositeHeader.Performer, cellsToFreeText(column))
            | CompositeHeaderDiscriminate.ProtocolDescription, _ ->
                CompositeColumn.create(CompositeHeader.ProtocolDescription, cellsToFreeText(column))
            | CompositeHeaderDiscriminate.ProtocolType, _ ->
                CompositeColumn.create(CompositeHeader.ProtocolType, cellsToTermCells(column))
            | CompositeHeaderDiscriminate.ProtocolUri, _ ->
                CompositeColumn.create(CompositeHeader.ProtocolUri, cellsToFreeText(column))
            | CompositeHeaderDiscriminate.ProtocolVersion, _ ->
                CompositeColumn.create(CompositeHeader.ProtocolVersion, cellsToFreeText(column))
            | CompositeHeaderDiscriminate.Comment, _ -> failwith "Comment header type is not yet implemented"
            | CompositeHeaderDiscriminate.Freetext, _ -> failwith "Freetext header type is not yet implemented"
        let submit (e) =
            let nxtCol = updateColumn column0
            Spreadsheet.SetColumn (columnIndex, nxtCol) |> SpreadsheetMsg |> dispatch
            rmv(e)
        let previewColumn =
            let cells = Array.takeSafe 10 column0.Cells
            updateColumn {column0 with Cells = cells}

        Daisy.modal.div [
            modal.open'
            prop.children [
                Daisy.modalBackdrop [ prop.onClick rmv ]
                Daisy.modalBox.div [
                    prop.className "lg:max-w-[600px]"
                    prop.style [style.maxHeight(length.percent 70)]
                    prop.children [
                        Daisy.cardBody [
                            Daisy.cardTitle [
                                prop.className "flex flex-row justify-between"
                                prop.children [
                                    Html.span "Update Column"
                                    Components.Components.DeleteButton(props=[prop.onClick rmv])
                                ]
                            ]
                            Html.div [
                                prop.className "join"
                                prop.children [
                                    SelectHeaderType(state, setState)
                                    match state.NextHeaderType with
                                    | CompositeHeaderDiscriminate.Output | CompositeHeaderDiscriminate.Input ->
                                        SelectIOType(state, setState)
                                    | _ -> Html.none
                                ]
                            ]
                            Html.div [
                                prop.style [style.maxHeight (length.perc 85); style.overflow.hidden; style.display.flex]
                                prop.children [
                                    Preview(previewColumn)
                                ]
                            ]
                            Daisy.cardActions [
                                prop.className "justify-end"
                                prop.children [
                                    BackButton rmv
                                    SubmitButton submit
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]