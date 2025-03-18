module Modals

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

type private Term =

    static member header = Html.p "Term"

    static member content(model, term: Swate.Components.Term option, setTerm) =
        [
            Html.div [
                prop.children [
                    Html.label [
                        prop.text "Term:"
                    ]
                    TermSearch.TermSearch(
                        setTerm,
                        term=term,
                        classNames = Swate.Components.TermSearchStyle(U2.Case1 "border-current join-item"),
                        advancedSearch = U2.Case2 true,
                        showDetails = true,
                        disableDefaultSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                        disableDefaultAllChildrenSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                        disableDefaultParentSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                        termSearchQueries = model.PersistentStorageState.TIBQueries.TermSearch,
                        parentSearchQueries = model.PersistentStorageState.TIBQueries.ParentSearch,
                        allChildrenSearchQueries = model.PersistentStorageState.TIBQueries.AllChildrenSearch
                    )
                ]
            ]
            Html.div [
                Html.label [
                    prop.text "Term-Source-Reference:"
                ]
                Html.p [
                    prop.className "border border-gray-300 rounded px-3 py-2 min-h-[42px]"
                    prop.readOnly true
                    prop.text (if term.IsSome && term.Value.source.IsSome then term.Value.source.Value else "")
                ]
            ]
            Html.div [ 
                Html.label [
                    prop.text "Term-Accession-Number:"
                ]
                Html.p [
                    prop.className "border border-gray-300 rounded px-3 py-2 min-h-[42px]"
                    prop.readOnly true
                    prop.text (if term.IsSome && term.Value.id.IsSome then term.Value.id.Value else "")
                ]
            ]
        ]

type private Unit =

    static member header = Html.p "Unit"

    static member content(model, value: string, setValue: string -> unit, term: Swate.Components.Term option, setTerm) =
        let displayUnit = (value.ToString().Length > 0) && term.Value.name.IsSome
        [
            Html.div [
                Html.label [
                    prop.text "Value:"
                    ]
                Html.div [
                    prop.className "border border-gray-300 rounded px-3 py-2 min-h-[42px] flex items-center"
                    prop.children [
                        Html.input [
                            prop.className "flex-1 outline-none border-none bg-transparent"
                            prop.valueOrDefault value
                            prop.autoFocus true
                            prop.onChange (fun input -> setValue input)
                        ]
                        if displayUnit then
                            Html.span [
                                prop.className "text-gray-500 whitespace-nowrap pl-1 "
                                prop.text term.Value.name.Value
                            ]
                    ]
                ]
            ]
            Html.div [
                prop.children [
                    Html.label [
                        prop.text "Unit:"
                    ]
                    TermSearch.TermSearch(
                        setTerm,
                        term=term,
                        classNames = Swate.Components.TermSearchStyle(U2.Case1 "border-current join-item"),
                        advancedSearch = U2.Case2 true,
                        showDetails = true,
                        disableDefaultSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                        disableDefaultAllChildrenSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                        disableDefaultParentSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                        termSearchQueries = model.PersistentStorageState.TIBQueries.TermSearch,
                        parentSearchQueries = model.PersistentStorageState.TIBQueries.ParentSearch,
                        allChildrenSearchQueries = model.PersistentStorageState.TIBQueries.AllChildrenSearch
                    )
                ]
            ]
            Html.div [
                Html.label [
                    prop.text "Term-Source-Reference:"
                ]
                Html.p [
                    prop.className "border border-gray-300 rounded px-3 py-2 min-h-[42px]"
                    prop.readOnly true
                    prop.text (if term.IsSome && term.Value.source.IsSome then term.Value.source.Value else "")
                ]
            ]
            Html.div [ 
                Html.label [
                    prop.text "Term-Accession-Number:"
                ]
                Html.p [
                    prop.className "border border-gray-300 rounded px-3 py-2 min-h-[42px]"
                    prop.readOnly true
                    prop.text (if term.IsSome && term.Value.id.IsSome then term.Value.id.Value else "")
                ]
            ]
        ]

type CompositeCollumnModal =

    static member onKeyDown (index: int*int, dispatch) =
        Model.ModalState.TableModals.TableCellIndex index
        |> Model.ModalState.ModalTypes.TableModal
        |> Some
        |> Messages.UpdateModal
        |> dispatch

    static member modalActivity((potCell: CompositeCell option), modalActivity, setModalActivity, transFormCell, (rmv: MouseEvent -> unit)) =
        Html.div [
            Daisy.cardActions [
                Daisy.button.button [
                    button.primary
                    prop.className "fa-solid fa-cog"
                    prop.style [style.marginLeft length.auto]
                    prop.onClick(fun _ ->
                        setModalActivity modalActivity
                    )
                ]
            ]
            Daisy.button.button [
                button.outline
                button.wide
                prop.style [style.marginLeft length.auto]
                match potCell with
                | Some cell when cell.isTerm ->
                    prop.text "As Unit"
                | Some cell when cell.isUnitized ->
                    prop.text "As Term"
                prop.onClick(fun e ->
                    setModalActivity modalActivity
                    transFormCell ()
                    rmv e
                )
            ]
        ]

    static member footer(submitOnClick, rmv: MouseEvent -> unit) =
        Html.div [
            prop.style [style.marginLeft length.auto]
            prop.children [
                Daisy.cardActions [
                    Daisy.button.button [
                        button.outline
                        prop.text "Cancel"
                        prop.onClick(fun e ->
                            rmv e
                        )
                    ]
                    Daisy.button.button [
                        button.primary
                        prop.text "Submit"
                        prop.onClick(fun e ->
                            submitOnClick()
                            rmv e
                        )
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main (ci: int, ri: int, model: Model.Model, dispatch: Messages.Msg -> unit, (rmv: MouseEvent -> unit)) =
        let index = (ci, ri)
        let potCell = model.SpreadsheetModel.ActiveTable.TryGetCellAt(index)
        let isUnitOrTermCell = Modals.ContextMenus.Util.isUnitOrTermCell potCell
        let potUnitValue, potTerm =
            if isUnitOrTermCell then
                let cell = potCell.Value
                if cell.isTerm then
                    (None, Some (cell.AsTerm.ToTerm()))
                else
                    let value, oa = cell.AsUnitized
                    (Some value, Some (oa.ToTerm()))

            else (None, None)

        let cellHeader = model.SpreadsheetModel.ActiveTable.Headers.[ci]
        let termState, setTermState = React.useState(potTerm)
        let newValue, setValue = React.useState(if potUnitValue.IsSome then potUnitValue.Value else "")
        let showModalActivity, setShowModalActivity = React.useState(false)
        
        let submitTermUnit =
            fun _ ->
                if termState.IsSome then
                    let term = termState.Value
                    let name = defaultArg term.name ""
                    let tsr = defaultArg term.source ""
                    let tan = defaultArg term.id ""
                    let nextCell =
                        if potUnitValue.IsSome then
                            CompositeCell.createUnitizedFromString(newValue, name, tsr, tan)
                        else
                            CompositeCell.createTermFromString(name, tsr, tan)
                    Spreadsheet.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        let transFormCell =
            fun _ ->
                if potCell.IsSome && (potCell.Value.isTerm || potCell.Value.isUnitized) then
                    let term = termState.Value
                    let name = defaultArg term.name ""
                    let tsr = defaultArg term.source ""
                    let tan = defaultArg term.id ""
                    let nextCell =
                        if potCell.Value.isTerm then
                            CompositeCell.createUnitizedFromString(newValue, name, tsr, tan)
                        else
                            CompositeCell.createTermFromString(name, tsr, tan)
                    Spreadsheet.UpdateCell (index, nextCell) |> Messages.SpreadsheetMsg |> dispatch
                elif potCell.IsSome && cellHeader.IsDataColumn then
                    let nextCell = if potCell.Value.isFreeText then potCell.Value.ToDataCell() else potCell.Value.ToFreeTextCell()
                    Spreadsheet.UpdateCell (index, nextCell) |> Messages.SpreadsheetMsg |> dispatch

        match potCell with
        | Some term when term.isTerm ->
            BaseModal.BaseModal(
                rmv = rmv,
                header = Term.header,
                modalClassInfo = "relative overflow-visible",
                modalActivity = CompositeCollumnModal.modalActivity(potCell, showModalActivity, setShowModalActivity, transFormCell, rmv),
                content = Term.content(model, termState, setTermState),
                contentClassInfo = "",
                footer = CompositeCollumnModal.footer(submitTermUnit, rmv))
        | Some unit when unit.isUnitized ->
            BaseModal.BaseModal(
                rmv = rmv,
                header = Unit.header,
                modalClassInfo = "relative overflow-visible",
                modalActivity = CompositeCollumnModal.modalActivity(potCell, showModalActivity, setShowModalActivity, transFormCell, rmv),
                content = Unit.content(model, newValue, setValue, termState, setTermState),
                contentClassInfo = "",
                footer = CompositeCollumnModal.footer(submitTermUnit, rmv))
        | _ -> Html.div []
