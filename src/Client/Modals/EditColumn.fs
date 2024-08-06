module Modals.EditColumn

open Feliz
open Feliz.Bulma
open ExcelColors
open Messages
open Shared
open TermTypes
open Spreadsheet
open Model

open ARCtrl

type private State =
    {
        NextHeaderType: BuildingBlock.HeaderCellType option
        NextIOType: IOType option
    } with
        static member init() = {
            NextHeaderType = None
            NextIOType = None
        }

module private EditColumnComponents =

    let BackButton cancel =
        Bulma.button.button [
            prop.onClick cancel
            color.isWarning
            prop.text "Back"
        ]

    let SubmitButton(submit) =
        Bulma.button.button [
            color.isSuccess
            prop.text "Submit"
            prop.onClick submit
        ]

    let SelectHeaderTypeOption(headerType: BuildingBlock.HeaderCellType) =
        let txt = headerType.ToString()
        Html.option [
            prop.value txt
            prop.text txt
        ]

    let SelectHeaderType(state, setState) =
        Bulma.select [
            prop.onChange (fun (e: string) -> {state with NextHeaderType = Some (BuildingBlock.HeaderCellType.fromString e)} |> setState )
            prop.children [
                // -- term columns --
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.Characteristic
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.Component
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.Factor
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.Parameter
                // -- io columns --
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.Input
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.Output
                // -- single columns --
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.Date
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.Performer
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.ProtocolDescription
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.ProtocolREF
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.ProtocolType
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.ProtocolUri
                SelectHeaderTypeOption BuildingBlock.HeaderCellType.ProtocolVersion
            ]
        ]

    let SelectIOTypeOption(ioType: IOType) =
        let txt = ioType.ToString()
        Html.option [
            prop.value txt
            prop.text txt
        ]

    let SelectIOType(state, setState) =
        Bulma.select [
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
        Bulma.tableContainer [
            prop.style [style.overflowY.auto; style.flexGrow 1]
            prop.children [
                Bulma.table [
                    table.isFullWidth
                    prop.children [
                        Html.thead [
                            Html.tr [
                                for header in headers do
                                    Html.th header 
                            ]
                        ]
                        Html.tbody [
                            for row in body do
                                Html.tr [
                                    for cell in row do
                                        Html.td cell
                                ]
                        ]
                    ]
                ]
            ]
        ]




open EditColumnComponents

[<ReactComponent>]
let Main (columnIndex: int) (model: Model) (dispatch) (rmv: _ -> unit) =
    let column0 = model.SpreadsheetModel.ActiveTable.GetColumn columnIndex
    let state, setState = React.useState(State.init)
    let cellsToTermCells(column:CompositeColumn) =
        [|for c in column.Cells do if c.isUnitized || c.isTerm then c else c.ToTermCell()|]
    let cellsToFreeText(column) =
        [|for c in column.Cells do if c.isFreeText then c else c.ToFreeTextCell()|]
    let cellsToDataOrFreeText(column) =
        [|for c in column.Cells do if c.isFreeText || c.isData then c else c.ToDataCell()|]
    let updateColumn (column: CompositeColumn) =
        let header = column0.Header
        match state.NextHeaderType, state.NextIOType with
        | None, _ -> column
        // -- term columns --
        | Some BuildingBlock.HeaderCellType.Characteristic, _  ->
            CompositeColumn.create(CompositeHeader.Characteristic (header.ToTerm()), cellsToTermCells(column))
        | Some BuildingBlock.HeaderCellType.Parameter, _ ->
            CompositeColumn.create(CompositeHeader.Parameter (header.ToTerm()), cellsToTermCells(column))
        | Some BuildingBlock.HeaderCellType.Component, _ ->
            CompositeColumn.create(CompositeHeader.Component (header.ToTerm()), cellsToTermCells(column))
        | Some BuildingBlock.HeaderCellType.Factor, _ ->
            CompositeColumn.create(CompositeHeader.Factor (header.ToTerm()), cellsToTermCells(column))
        // -- input columns --
        | Some BuildingBlock.HeaderCellType.Input, Some IOType.Data ->
            CompositeColumn.create(CompositeHeader.Input IOType.Data, cellsToDataOrFreeText(column))
        | Some BuildingBlock.HeaderCellType.Input, Some io ->
            CompositeColumn.create(CompositeHeader.Input io, cellsToFreeText(column))
        | Some BuildingBlock.HeaderCellType.Input, None ->
            CompositeColumn.create(CompositeHeader.Input IOType.Sample, cellsToFreeText(column))
        // -- output columns --
        | Some BuildingBlock.HeaderCellType.Output, Some IOType.Data ->
            CompositeColumn.create(CompositeHeader.Output IOType.Data, cellsToDataOrFreeText(column))
        | Some BuildingBlock.HeaderCellType.Output, Some io ->
            CompositeColumn.create(CompositeHeader.Output io, cellsToFreeText(column))
        | Some BuildingBlock.HeaderCellType.Output, None ->
            CompositeColumn.create(CompositeHeader.Output IOType.Sample, cellsToFreeText(column))
        // -- single columns --
        | Some BuildingBlock.HeaderCellType.ProtocolREF, _ ->
            CompositeColumn.create(CompositeHeader.ProtocolREF, cellsToFreeText(column))
        | Some BuildingBlock.HeaderCellType.Date, _ ->
            CompositeColumn.create(CompositeHeader.Date, cellsToFreeText(column))
        | Some BuildingBlock.HeaderCellType.Performer, _ ->
            CompositeColumn.create(CompositeHeader.Performer, cellsToFreeText(column))
        | Some BuildingBlock.HeaderCellType.ProtocolDescription, _ ->
            CompositeColumn.create(CompositeHeader.ProtocolDescription, cellsToFreeText(column))
        | Some BuildingBlock.HeaderCellType.ProtocolType, _ ->
            CompositeColumn.create(CompositeHeader.ProtocolType, cellsToTermCells(column))
        | Some BuildingBlock.HeaderCellType.ProtocolUri, _ ->
            CompositeColumn.create(CompositeHeader.ProtocolUri, cellsToFreeText(column))
        | Some BuildingBlock.HeaderCellType.ProtocolVersion, _ ->
            CompositeColumn.create(CompositeHeader.ProtocolVersion, cellsToFreeText(column))
    let submit (e) =
        let nxtCol = updateColumn column0
        Spreadsheet.SetColumn (columnIndex, nxtCol) |> SpreadsheetMsg |> dispatch
        rmv(e)
    let previewColumn =
        let cells = Array.takeSafe 10 column0.Cells
        updateColumn {column0 with Cells = cells}

    Bulma.modal [
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground [ prop.onClick rmv ]
            Bulma.modalCard [
                prop.style [style.maxHeight(length.percent 70)]
                prop.children [
                    Bulma.modalCardHead [
                        Bulma.modalCardTitle "Update Column"
                        Bulma.delete [ prop.onClick rmv ]
                    ]
                    Bulma.modalCardBody [
                        Bulma.field.div [
                            Bulma.buttons [
                                prop.children [
                                    SelectHeaderType(state, setState)
                                    match state.NextHeaderType with
                                    | Some BuildingBlock.HeaderCellType.Output | Some BuildingBlock.HeaderCellType.Input ->
                                        SelectIOType(state, setState)
                                    | _ -> Html.none
                                ]
                            ]
                        ]
                        Bulma.field.div [
                            prop.style [style.maxHeight (length.perc 85); style.overflow.hidden; style.display.flex]
                            prop.children [
                                Preview(previewColumn)
                            ]
                        ]
                    ]
                    Bulma.modalCardFoot [
                        prop.className "flex grow justify-between"
                        prop.children [
                            BackButton rmv
                            SubmitButton submit
                        ]
                    ]
                ]
            ]
        ]
    ]