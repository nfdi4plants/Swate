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

    static member content(model, term: Swate.Components.Term option, setTerm, ?value: string, ?setValue: string -> unit) =
        [
            if value.IsSome && setValue.IsSome then
                let value = value.Value
                let setValue = setValue.Value
                let displayUnit = (value.ToString().Length > 0) && term.Value.name.IsSome
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
                        allChildrenSearchQueries = model.PersistentStorageState.TIBQueries.AllChildrenSearch,
                        autoFocus = not value.IsSome
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

type private Freetext =

    static member header = Html.p "Free Text"

    static member content(value: string, setValue: string -> unit) =

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
                ]
            ]
        ]

type private Data =

    static member header = Html.p "Data"

    static member content(
        value: string, setValue: string -> unit,
        selector: string, setSelector: string -> unit,
        format: string, setFormat: string -> unit,
        selectorFormat: string, setSelectorFormat: string -> unit) =
        let displaySelector = value.Length > 0 && selector.Length > 0
        [
            Html.label [
                prop.text "Name:"
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
                    if displaySelector then
                        Html.span [
                            prop.className "text-gray-500 whitespace-nowrap pl-1 "
                            prop.text $"#{selector}"
                        ]
                ]
            ]
            Html.label [
                prop.text "Selector:"
                ]
            Html.div [
                prop.className "border border-gray-300 rounded px-3 py-2 min-h-[42px] flex items-center"
                prop.children [
                    Html.input [
                        prop.className "flex-1 outline-none border-none bg-transparent"
                        prop.valueOrDefault selector
                        prop.onChange (fun input -> setSelector input)
                    ]
                ]
            ]
            Html.label [
                prop.text "Format:"
                ]
            Html.div [
                prop.className "border border-gray-300 rounded px-3 py-2 min-h-[42px] flex items-center"
                prop.children [
                    Html.input [
                        prop.className "flex-1 outline-none border-none bg-transparent"
                        prop.valueOrDefault format
                        prop.onChange (fun input -> setFormat input)
                    ]
                ]
            ]
            Html.label [
                prop.text "Selector Format:"
                ]
            Html.div [
                prop.className "border border-gray-300 rounded px-3 py-2 min-h-[42px] flex items-center"
                prop.children [
                    Html.input [
                        prop.className "flex-1 outline-none border-none bg-transparent"
                        prop.valueOrDefault selectorFormat
                        prop.onChange (fun input -> setSelectorFormat input)
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

    static member modalActivity(potCell: CompositeCell option, modalActivity, setModalActivity, transFormCell, rmv: MouseEvent -> unit, ?isButtonActive) =
        let isButtonActive = defaultArg isButtonActive true
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
                if isButtonActive then
                    button.outline
                else
                    button.disabled
                button.wide
                prop.style [style.marginLeft length.auto]
                match potCell with
                | Some cell when cell.isTerm ->
                    prop.text "As Unit"
                | Some cell when cell.isUnitized ->
                    prop.text "As Term"
                | Some cell when cell.isFreeText ->
                    prop.text "As Data"
                | Some cell when cell.isData ->
                    prop.text "As Free Text"
                | _ -> failwith "Not supported"
                prop.onClick(fun e ->
                    setModalActivity modalActivity
                    transFormCell ()
                    rmv e
                )
            ]
        ]

    static member footer(submitOnClick, rmv: MouseEvent -> unit) =

        let cancelButtonRef = React.useRef<HTMLButtonElement option>(None)
        let submitButtonRef = React.useRef<HTMLButtonElement option>(None)

        let handleKeyDown (buttonRef: IRefValue<HTMLButtonElement option>) targetButton (e: Event) =
            let keyboardEvent = e :?> Browser.Types.KeyboardEvent
            if keyboardEvent.code = targetButton then
                match buttonRef.current with
                | Some button -> button.click()
                | None -> ()

        React.useEffect(fun () ->
            Browser.Dom.document.addEventListener("keydown", handleKeyDown cancelButtonRef Swate.Components.kbdEventCode.escape)
            React.createDisposable(fun () ->
                Browser.Dom.document.removeEventListener("keydown", handleKeyDown cancelButtonRef Swate.Components.kbdEventCode.escape))
        , [||])

        React.useEffect(fun () ->
            Browser.Dom.document.addEventListener("keydown", handleKeyDown submitButtonRef Swate.Components.kbdEventCode.enter)
            React.createDisposable(fun () ->
                Browser.Dom.document.removeEventListener("keydown", handleKeyDown submitButtonRef Swate.Components.kbdEventCode.enter))
        , [||])

        Html.div [
            prop.style [style.marginLeft length.auto]
            prop.children [
                Daisy.cardActions [
                    Daisy.button.button [
                        prop.ref cancelButtonRef
                        button.outline
                        prop.text "Cancel"
                        prop.onClick(fun e ->
                            rmv e
                        )
                    ]
                    Daisy.button.button [
                        prop.ref submitButtonRef
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
        let value, potTerm =
            if isUnitOrTermCell then
                let cell = potCell.Value
                if cell.isTerm then
                    (None, Some (cell.AsTerm.ToTerm()))
                else
                    let value, oa = cell.AsUnitized
                    (Some value, Some (oa.ToTerm()))

            elif potCell.IsSome then
                if potCell.Value.isFreeText then
                    (Some potCell.Value.AsFreeText, None)
                else
                    let result = potCell.Value.AsData
                    (result.FilePath, None)
            else
                (None, None)
        let cellHeader = model.SpreadsheetModel.ActiveTable.Headers.[ci]
        let termState, setTermState = React.useState(potTerm)
        let newValue, setValue = React.useState(if value.IsSome then value.Value else "")
        let showModalActivity, setShowModalActivity = React.useState(false)
        
        let updateTermUnit =
            fun _ ->
                if termState.IsSome then
                    let term = termState.Value
                    let name = defaultArg term.name ""
                    let tsr = defaultArg term.source ""
                    let tan = defaultArg term.id ""
                    let nextCell =
                        if value.IsSome then
                            CompositeCell.createUnitizedFromString(newValue, name, tsr, tan)
                        else
                            CompositeCell.createTermFromString(name, tsr, tan)
                    Spreadsheet.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        let transFormCell =
            fun _ ->
                if isUnitOrTermCell then
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
                    let nextCell =
                        if potCell.Value.isFreeText then
                            CompositeCell.createDataFromString(newValue, "", "")
                        else
                            potCell.Value.ToFreeTextCell()
                    Spreadsheet.UpdateCell (index, nextCell) |> Messages.SpreadsheetMsg |> dispatch

        match potCell with
        | Some cell when cell.isTerm ->
            BaseModal.BaseModal(
                rmv = rmv,
                header = Term.header,
                modalClassInfo = "relative overflow-visible",
                modalActivity = CompositeCollumnModal.modalActivity(potCell, showModalActivity, setShowModalActivity, transFormCell, rmv),
                content = Term.content(model, termState, setTermState),
                contentClassInfo = "",
                footer = CompositeCollumnModal.footer(updateTermUnit, rmv))
        | Some cell when cell.isUnitized ->
            BaseModal.BaseModal(
                rmv = rmv,
                header = Unit.header,
                modalClassInfo = "relative overflow-visible",
                modalActivity = CompositeCollumnModal.modalActivity(potCell, showModalActivity, setShowModalActivity, transFormCell, rmv),
                content = Term.content(model, termState, setTermState, newValue, setValue),
                contentClassInfo = "",
                footer = CompositeCollumnModal.footer(updateTermUnit, rmv))
        | Some cell when cell.isFreeText ->
            let nextCell = CompositeCell.createFreeText(newValue)
            let updateFreetext = fun () -> Spreadsheet.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
            BaseModal.BaseModal(
                rmv = rmv,
                header = Freetext.header,
                modalClassInfo = "relative overflow-visible",
                modalActivity = CompositeCollumnModal.modalActivity(potCell, showModalActivity, setShowModalActivity, transFormCell, rmv, cellHeader.IsDataColumn),
                content = [Freetext.content(newValue, setValue)],
                contentClassInfo = "",
                footer = CompositeCollumnModal.footer(updateFreetext, rmv))
        | Some cell when cell.isData ->
            let data = cell.AsData
            let selector = defaultArg data.Selector ""
            let format = defaultArg data.Format ""
            let selectorFormat = defaultArg data.SelectorFormat ""
            let selector, setSelector = React.useState(selector)
            let format, setFormat = React.useState(format)
            let selectorFormat, setSelectorFormat = React.useState(selectorFormat)
            let nextCell = CompositeCell.createDataFromString($"{newValue}#{selector}", format, selectorFormat)
            let updateData = fun () -> Spreadsheet.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
            BaseModal.BaseModal(
                rmv = rmv,
                header = Data.header,
                modalClassInfo = "relative overflow-visible",
                modalActivity = CompositeCollumnModal.modalActivity(potCell, showModalActivity, setShowModalActivity, transFormCell, rmv, cellHeader.IsDataColumn),
                content = Data.content(newValue, setValue, selector, setSelector, format, setFormat, selectorFormat, setSelectorFormat),
                contentClassInfo = "",
                footer = CompositeCollumnModal.footer(updateData, rmv))
        | _ -> Html.div []
