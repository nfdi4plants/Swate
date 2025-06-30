namespace Swate.Components

open ARCtrl
open Feliz
open Swate.Components.Shared

type State = {
    NextHeaderType: CompositeHeaderDiscriminate
    NextIOType: IOType option
} with

    static member init(current) = {
        NextHeaderType = current
        NextIOType = None
    }

type EditColumnModal =

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
                        EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Characteristic
                        EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Component
                        EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Factor
                        EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Parameter
                    ]
                ]
                // -- io columns --
                Html.optgroup [
                    prop.label "IO Columns"
                    prop.children [
                        EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Input
                        EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Output
                    ]
                ]
                // -- single columns --
                EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Date
                EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.Performer
                EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolDescription
                EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolREF
                EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolType
                EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolUri
                EditColumnModal.SelectHeaderTypeOption CompositeHeaderDiscriminate.ProtocolVersion
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
                EditColumnModal.SelectIOTypeOption IOType.Source
                EditColumnModal.SelectIOTypeOption IOType.Sample
                EditColumnModal.SelectIOTypeOption IOType.Material
                EditColumnModal.SelectIOTypeOption IOType.Data
            ]
        ]

    static member Preview(column: CompositeColumn) =
        let parsedStrList =
            ARCtrl.Spreadsheet.CompositeColumn.toStringCellColumns column |> List.transpose
        printfn "parsedStrList: %s" (parsedStrList.ToString())
        let headers, body =
            if column.Cells.Length >= 2 then
                parsedStrList.[0], parsedStrList.[1..]
            else
                parsedStrList.[0], []

        Html.div [
            prop.className "swt:overflow-x-auto swt:grow"
            prop.children [
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
                EditColumnModal.cellsToTermCells (column)
            )
        | CompositeHeaderDiscriminate.Parameter, _ ->
            CompositeColumn.create (
                CompositeHeader.Parameter(header.ToTerm()),
                EditColumnModal.cellsToTermCells (column)
            )
        | CompositeHeaderDiscriminate.Component, _ ->
            CompositeColumn.create (
                CompositeHeader.Component(header.ToTerm()),
                EditColumnModal.cellsToTermCells (column)
            )
        | CompositeHeaderDiscriminate.Factor, _ ->
            CompositeColumn.create (
                CompositeHeader.Factor(header.ToTerm()),
                EditColumnModal.cellsToTermCells (column)
            )
        // -- input columns --
        | CompositeHeaderDiscriminate.Input, Some IOType.Data ->
            CompositeColumn.create (
                CompositeHeader.Input IOType.Data,
                EditColumnModal.cellsToDataOrFreeText (column)
            )
        | CompositeHeaderDiscriminate.Input, Some io ->
            CompositeColumn.create (CompositeHeader.Input io, EditColumnModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.Input, None ->
            CompositeColumn.create (CompositeHeader.Input IOType.Sample, EditColumnModal.cellsToFreeText (column))
        // -- output columns --
        | CompositeHeaderDiscriminate.Output, Some IOType.Data ->
            CompositeColumn.create (
                CompositeHeader.Output IOType.Data,
                EditColumnModal.cellsToDataOrFreeText (column)
            )
        | CompositeHeaderDiscriminate.Output, Some io ->
            CompositeColumn.create (CompositeHeader.Output io, EditColumnModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.Output, None ->
            CompositeColumn.create (CompositeHeader.Output IOType.Sample, EditColumnModal.cellsToFreeText (column))
        // -- single columns --
        | CompositeHeaderDiscriminate.ProtocolREF, _ ->
            CompositeColumn.create (CompositeHeader.ProtocolREF, EditColumnModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.Date, _ ->
            CompositeColumn.create (CompositeHeader.Date, EditColumnModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.Performer, _ ->
            CompositeColumn.create (CompositeHeader.Performer, EditColumnModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.ProtocolDescription, _ ->
            CompositeColumn.create (CompositeHeader.ProtocolDescription, EditColumnModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.ProtocolType, _ ->
            CompositeColumn.create (CompositeHeader.ProtocolType, EditColumnModal.cellsToTermCells (column))
        | CompositeHeaderDiscriminate.ProtocolUri, _ ->
            CompositeColumn.create (CompositeHeader.ProtocolUri, EditColumnModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.ProtocolVersion, _ ->
            CompositeColumn.create (CompositeHeader.ProtocolVersion, EditColumnModal.cellsToFreeText (column))
        | CompositeHeaderDiscriminate.Comment, _ (*-> failwith "Comment header type is not yet implemented"*)
        | CompositeHeaderDiscriminate.Freetext, _ ->
            CompositeColumn.create (
                CompositeHeader.FreeText(header.ToString()),
                EditColumnModal.cellsToFreeText (column)
            )
    //failwith "Freetext header type is not yet implemented"

    static member modalActivity(state, setState) =
        Html.div [
            prop.children [
                Html.div [
                    prop.className "swt:join"
                    prop.children [
                        EditColumnModal.SelectHeaderType(state, setState)
                        match state.NextHeaderType with
                        | CompositeHeaderDiscriminate.Output
                        | CompositeHeaderDiscriminate.Input -> EditColumnModal.SelectIOType(state, setState)
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
            let cells = Array.takeSafe 15 column0.Cells
            //Replace empty cells with placeholder data to represent meaningfull information in the Preview
            cells
            |> Array.iteri (fun i cell ->
                let content = cell.GetContent()

                if (Array.filter (fun item -> item = "") content).Length > 0 then
                    match cell with
                    | cell when cell.isTerm -> cells.[i] <- EditColumnModal.placeHolderTermCell
                    | cell when cell.isUnitized -> cells.[i] <- EditColumnModal.placeHolderUnitCell
                    | cell when cell.isData -> cells.[i] <- EditColumnModal.placeHolderTermCell
                    | _ -> cells.[i] <- EditColumnModal.placeHolderTermCell)

            EditColumnModal.updateColumn ({ column0 with Cells = cells }, state)

        React.fragment [
            Html.label [ prop.text "Preview:" ]
            Html.div [
                prop.style [ style.maxHeight (length.perc 85); style.overflow.hidden; style.display.flex ]
                prop.children [ EditColumnModal.Preview(previewColumn) ]
            ]
        ]

    static member footer(column0, state, setColumn, rmv) =
        let submit () =
            let updatedColumn = EditColumnModal.updateColumn (column0, state)
            setColumn updatedColumn
            rmv ()

        Html.div [
            prop.className "swt:justify-end swt:flex swt:gap-2"
            prop.style [ style.marginLeft length.auto ]
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-outline"
                    prop.text "Cancel"
                    prop.onClick (fun _ -> rmv ())
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-primary"
                    prop.style [ style.marginLeft length.auto ]
                    prop.text "Submit"
                    prop.onClick (fun _ -> submit ())
                ]
            ]
        ]

    [<ReactComponent>]
    static member EditColumnModal(columnIndex: int, table: ArcTable, setColumn, rmv) =

        let column = table.GetColumn columnIndex
        let state, setState = React.useState (State.init column.Header.AsDiscriminate)

        Html.div [
            prop.className "swt:flex swt:flex-col swt:h-full swt:gap-4 swt:min-h-[500px]"
            prop.children [
                Html.div [
                    prop.className "swt:border-b swt:pb-2 swt:mb-2"
                    prop.children [
                        EditColumnModal.modalActivity (state, setState)
                    ]
                ]
                Html.div [
                    prop.className "swt:flex-grow swt:overflow-y-auto swt:h-[200px]"
                    prop.children [
                        EditColumnModal.content (column, state)
                    ]
                ]
                Html.div [
                    prop.className "swt:border-t swt:pt-2 swt:mt-2"
                    prop.children [
                        EditColumnModal.footer (column, state, setColumn, rmv)
                    ]
                ]
            ]
        ]
