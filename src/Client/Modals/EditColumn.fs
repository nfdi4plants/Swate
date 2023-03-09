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
    NextType: ColumnType
    BuildingBlockType: BuildingBlockType option
} with
    static member init(columnHeader: HeaderCell) =
        let b_type, c_type =
            match columnHeader with
            | unit when columnHeader.HasUnit -> Some columnHeader.BuildingBlockType, Unit
            | term when columnHeader.Term.IsSome -> Some columnHeader.BuildingBlockType, Term
            | ft -> Some columnHeader.BuildingBlockType, Freetext
        {
            NextType = c_type
            BuildingBlockType = b_type
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

open Fable.Core.JsInterop

let private buildingBlockField (state: EditState) (setState: EditState -> unit) =
    let options = BuildingBlockType.TermColumns
    Bulma.field.div [
        Bulma.label [prop.text "Select building block type"]
        Bulma.select [
            prop.value (Option.defaultValue BuildingBlockType.Parameter state.BuildingBlockType |> fun x -> x.toString)
            prop.onChange(fun (e: Browser.Types.Event) ->
                let b_type = string e.target?value |> BuildingBlockType.ofString
                let nextState = {state with BuildingBlockType = Some b_type}
                setState nextState
            )
            prop.children [
                for termColumn in options do
                    yield Html.option [
                        prop.value termColumn.toString
                        prop.text termColumn.toString
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
                            let header = (snd column.[0])
                            let headerUpdated =
                                match state.NextType, state.BuildingBlockType with
                                | Unit, Some bb -> header.toUnitHeader(bb)
                                | Unit, None -> header.toUnitHeader()
                                | Term, Some bb -> header.toTermHeader(bb)
                                | Term, None -> header.toTermHeader()
                                | Freetext, Some bb -> header.toFreetextHeader(bb)
                                | Freetext, None -> header.toFreetextHeader()
                            match state.NextType with
                            | Freetext -> 
                                Html.th $"{headerUpdated.DisplayValue}"
                            | Term ->
                                let uid = headerUpdated.Term.Value.TermAccession
                                Html.th $"{headerUpdated.DisplayValue}"
                                Html.th $"{ColumnCoreNames.TermAccessionNumber.toString} ({uid})"
                            | Unit ->
                                let uid = headerUpdated.Term.Value.TermAccession
                                Html.th $"{headerUpdated.DisplayValue}"
                                Html.th $"{ColumnCoreNames.Unit.toString}"
                                Html.th $"{ColumnCoreNames.TermAccessionNumber.toString} ({uid})"
                        ]
                    ]
                    Html.tbody [
                        prop.children [
                            for _,cell in Array.skip 1 column |> Array.truncate 9 do
                                let cellUpdated =
                                    match state.NextType with
                                    | Unit -> cell.toUnitCell()
                                    | Freetext -> cell.toFreetextCell()
                                    | Term -> cell.toTermCell()
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

let private footer columnIndex rmv (lastState: EditState) (state: EditState) dispatch =
    Bulma.modalCardFoot [
        Bulma.button.a [
            prop.onClick rmv
            Bulma.color.isInfo
            prop.text "Back"
        ]
        Bulma.button.a [
            prop.disabled <| (state = lastState)
            prop.onClick (fun e ->
                let nt = match state.NextType with | Unit -> SwateCell.emptyUnit | Term -> SwateCell.emptyTerm | Freetext -> SwateCell.emptyFreetext
                Spreadsheet.EditColumn (columnIndex, nt, state.BuildingBlockType) |> SpreadsheetMsg |> dispatch
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
    let last = EditState.init(header)
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
                        if state.NextType = Unit || state.NextType = Term then
                            buildingBlockField state setState
                        previewField column state
                    ]
                    footer columnIndex rmv last state dispatch
                ]
            ]
        ]
    ]