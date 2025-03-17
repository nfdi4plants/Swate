module Modals

open ARCtrl
open Feliz
open Feliz.DaisyUI
open Swate.Components
open Swate.Components.Shared
open Messages
open Model
open Fable.Core

type private Term =

    static member header = Html.p "Term"

    static member modalActivity (potCell: CompositeCell option) setModal transFormCell onClick rmv =
        Html.div [
            Daisy.cardActions [
                Daisy.button.button [
                    button.primary
                    prop.className "fa-solid fa-cog"
                    prop.style [style.marginLeft length.auto]
                    prop.onClick(fun _ ->
                        onClick
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
                    setModal
                    transFormCell
                    rmv e
                )
            ]
        ]

    static member content (term: Swate.Components.Term option) =
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

    static member fooder index (term: Swate.Components.Term option) rmv dispatch =
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
                            if term.IsSome then
                                let term = term.Value
                                let name = defaultArg term.name ""
                                let tsr = defaultArg term.source ""
                                let tan = defaultArg term.id ""
                                Spreadsheet.UpdateCell (index, CompositeCell.createTermFromString(name, tsr, tan)) |> SpreadsheetMsg |> dispatch
                            rmv e
                        )
                    ]
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

    [<ReactComponent>]
    static member Main (ci: int, ri: int, model: Model.Model, dispatch: Messages.Msg -> unit, rmv) =

        let index = (ci, ri)
        let potCell = model.SpreadsheetModel.ActiveTable.TryGetCellAt(index)
        let potTerm =
            if potCell.IsSome then
                let cell = potCell.Value
                if cell.isTerm then Some (cell.AsTerm.ToTerm()) else None
            else None
        let cellHeader = model.SpreadsheetModel.ActiveTable.Headers.[ci]
        let (term, setTerm) = React.useState(potTerm)
        let showModalActivity, setShowModalActivity = React.useState(false)
        let isUnitOrTermCell = Modals.ContextMenus.Util.isUnitOrTermCell potCell
        let transFormCell =
            fun _ ->
                if potCell.IsSome && (potCell.Value.isTerm || potCell.Value.isUnitized) then
                    let nextCell = if potCell.Value.isTerm then potCell.Value.ToUnitizedCell() else potCell.Value.ToTermCell()
                    Spreadsheet.UpdateCell (index, nextCell) |> Messages.SpreadsheetMsg |> dispatch
                elif potCell.IsSome && cellHeader.IsDataColumn then
                    let nextCell = if potCell.Value.isFreeText then potCell.Value.ToDataCell() else potCell.Value.ToFreeTextCell()
                    Spreadsheet.UpdateCell (index, nextCell) |> Messages.SpreadsheetMsg |> dispatch

        let header =
            match potCell with
            | Some cell when cell.isTerm ->
                Html.p "Term"
            | Some cell when cell.isUnitized ->
                Html.p "Unit"
        let modalActivity =
            if isUnitOrTermCell then
                Html.div [
                    Daisy.cardActions [
                        Daisy.button.button [
                            button.primary
                            prop.className "fa-solid fa-cog"
                            prop.style [style.marginLeft length.auto]
                            prop.onClick(fun _ ->
                                setShowModalActivity (not showModalActivity)
                            )
                        ]
                    ]
                    if showModalActivity then
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
                                setShowModalActivity (not showModalActivity)
                                transFormCell ()
                                rmv e
                            )
                        ]
                ]
            else
                Html.div []
        let content =
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
        let footer =
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
                                if term.IsSome then
                                    let term = term.Value
                                    let name = defaultArg term.name ""
                                    let tsr = defaultArg term.source ""
                                    let tan = defaultArg term.id ""
                                    Spreadsheet.UpdateCell (index, CompositeCell.createTermFromString(name, tsr, tan)) |> SpreadsheetMsg |> dispatch
                                rmv e
                            )
                        ]
                    ]
                ]
            ]

        BaseModal.BaseModal(
            rmv = rmv,
            header = header,
            modalClassInfo = "relative overflow-visible",
            modalActivity = modalActivity,
            content = content,
            contentClassInfo = "",
            footer = footer)
