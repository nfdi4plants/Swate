module Modals.EditColumn

open Feliz
open Feliz.Bulma
open ExcelColors
open Model
open Messages
open Shared
open OfficeInteropTypes
open TermTypes
open Spreadsheet

type private ColumnType =
| Freetext
| Unit
| Term

type private EditState = {
    CurrentType: ColumnType
    NextType: ColumnType
} with
    static member init(columnHeader: HeaderCell) =
        let c_type =
            match columnHeader with
            | unit when columnHeader.HasUnit -> Unit
            | term when columnHeader.Term.IsSome -> Term
            | ft -> Freetext
        {
            CurrentType = c_type
            NextType = c_type
        }

let private updateField state setState =
    Bulma.field.div [
        Bulma.label [prop.text "Column type"]
        Bulma.control.div [
            prop.style [style.display.inlineFlex; style.justifyContent.spaceEvenly;]
            prop.children [
                Html.label [
                    prop.className "radio pl-1 pr-1 nonSelectText"
                    prop.children [
                        Html.input [
                            prop.isChecked (state.NextType = Freetext)
                            prop.type' "radio"; prop.name "c_type"
                            prop.onChange(fun (e:Browser.Types.Event) ->
                                setState {state with NextType = Freetext}
                            )
                        ]
                        Html.text " Freetext"
                    ]
                ]
                Html.label [
                    prop.className "radio pl-1 pr-1 nonSelectText"
                    prop.children [
                        Html.input [
                            prop.isChecked (state.NextType = Term)
                            prop.type' "radio"; prop.name "c_type"
                            prop.onChange(fun (e:Browser.Types.Event) ->
                                setState {state with NextType = Term}
                            )
                        ]
                        Html.text " Term"
                    ]
                ]
                Html.label [
                    prop.className "radio pl-1 pr-1 nonSelectText"
                    prop.children [
                        Html.input [
                            prop.isChecked (state.NextType = Unit)
                            prop.type' "radio"; prop.name "c_type"
                            prop.onChange(fun (e:Browser.Types.Event) ->
                                setState {state with NextType = Unit}
                            )
                        ]
                        Html.span " Unit"
                    ]
                ]
            ]
        ]
    ]

let private previewField (column : (int*SwateCell) []) state =
    Bulma.field.div [
        Bulma.label [prop.text "Preview"]
        Bulma.tableContainer [
            Bulma.table [
                prop.style [style.height (length.percent 50)]
                Bulma.table.isStriped
                prop.children [
                    Html.thead [
                        Html.tr [
                            let header = (snd column.[0]).Header
                            match state.NextType with
                            | Unit ->
                                let uid = if header.Term.IsSome then header.Term.Value.TermAccession else ""
                                Html.th $"{header.DisplayValue}"
                                Html.th "Unit"
                                Html.th $"{ColumnCoreNames.TermAccessionNumber.toString} ({uid})"
                            | Freetext -> 
                                Html.th $"{header.DisplayValue}"
                            | Term ->
                                let uid = if header.Term.IsSome then header.Term.Value.TermAccession else ""
                                Html.th $"{header.DisplayValue}"
                                Html.th $"{ColumnCoreNames.TermAccessionNumber.toString} ({uid})"
                        ]
                    ]
                    Html.tbody [
                        prop.children [
                            for _,cell in Array.skip 1 column |> Array.truncate 9 do
                                let cellUpdated =
                                    match state.NextType with
                                    | Unit -> cell.toUnitCell
                                    | Freetext -> cell.toFreetext
                                    | Term -> cell.toTermCell
                                Html.tr [
                                    match state.NextType with
                                    | Unit ->
                                        Html.td $"{cellUpdated.Unit.Value}"
                                        Html.td $"{cellUpdated.Unit.Unit.Name}"
                                        Html.td $"{cellUpdated.Unit.Unit.TermAccession}"
                                    | Freetext -> 
                                        Html.td $"{cellUpdated.Freetext.Value}"
                                    | Term -> 
                                        Html.td $"{cellUpdated.Term.Term.Name}"
                                        Html.td $"{cellUpdated.Term.Term.TermAccession}"
                                ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let private footer columnIndex rmv state dispatch =
    Bulma.modalCardFoot [
        Bulma.button.a [
            prop.onClick rmv
            Bulma.color.isInfo
            prop.text "Back"
        ]
        Bulma.button.a [
            prop.disabled (state.NextType = state.CurrentType)
            prop.onClick (fun e ->
                let nt = match state.NextType with | Unit -> SwateCell.emptyUnit | Term -> SwateCell.emptyTerm | Freetext -> SwateCell.emptyFreetext
                Spreadsheet.EditColumn (columnIndex, nt) |> SpreadsheetMsg |> dispatch
                rmv e;
            )
            Bulma.color.isSuccess
            prop.text "Update"
        ]
    ]

[<ReactComponent>]
let Main (columnIndex: int) (model: Messages.Model) (dispatch) (rmv: _ -> unit) =
    let column : (int*SwateCell) [] = model.SpreadsheetModel.getColumn(columnIndex)
    let header = column |> Array.sortBy fst |> Array.head |> snd |> fun header -> header.Header
    let state, setState = React.useState(EditState.init(header))
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
                        updateField state setState
                        previewField column state
                    ]
                    footer columnIndex rmv state dispatch
                ]
            ]
        ]
    ]