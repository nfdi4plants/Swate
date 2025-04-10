namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI
open ARCtrl


[<Mangle(false); Erase>]
type TableCell =

    static member BaseCell
        (
            rowIndex: int,
            columnIndex: int,
            content: ReactElement,
            ?className: string,
            ?props,
            ?debug: bool,
            ?isStickyHeader: bool
        ) =
        let debug = defaultArg debug false

        Html.div [
            prop.key $"{rowIndex}-{columnIndex}"
            if debug then
                prop.testId $"cell-{rowIndex}-{columnIndex}"
            prop.className [
                TableCell.DefaultClasses
                if className.IsSome then
                    className.Value
            ]
            if props.IsSome then
                yield! props.Value
            if isStickyHeader.IsSome then
                prop.style [ style.position.sticky; style.height Constants.Table.DefaultRowHeight; style.top Constants.Table.DefaultRowHeight ]
            prop.children [ content ]
        ]

    static member DefaultClasses: string = "select-none h-full w-full border border-base-200"

    static member StickyIndexColumn(index: int, ?debug: bool, ?defaultWidth) =
        let defaultWidth = defaultArg defaultWidth Constants.Table.DefaultColumnWidth
        let debug = defaultArg debug false
        let rowIndex = index
        let columnIndex = 0
        Html.div [
            prop.key $"{rowIndex}-{columnIndex}"
            if debug then
                prop.testId $"cell-{rowIndex}-{columnIndex}"
            prop.className [
                TableCell.DefaultClasses
                "bg-base-300 flex text-base-content items-center justify-center"
            ]
            prop.style [ style.position.sticky; style.width defaultWidth; style.left defaultWidth ]
            prop.children [
                Html.div [
                    prop.className "text-lg"
                    prop.children [
                        if index = 0 then
                            Html.text "Header"
                        else
                            Html.text $"Row {index}"
                    ]
                ]
            ]
        ]

    static member StickyHeader(index: int, ?customContent: ReactElement, ?debug: bool, ?defaultHeight) =
        let defaultHeight = defaultArg defaultHeight Constants.Table.DefaultRowHeight
        let debug = defaultArg debug false
        let rowIndex = 0
        let columnIndex = index
        Html.div [
            prop.key $"{rowIndex}-{columnIndex}"
            if debug then
                prop.testId $"cell-{rowIndex}-{columnIndex}"
            prop.className [
                TableCell.DefaultClasses
                "bg-neutral text-neutral-content flex items-center px-2 py-1"
            ]
            prop.style [ style.position.sticky; style.height defaultHeight; style.top defaultHeight ]
            prop.children [
                if customContent.IsSome then
                    customContent.Value
                else
                    Html.div [ prop.className "text-lg"; prop.children [ Html.text $"Column {index}" ] ]
            ]
        ]

    [<ReactComponent>]
    static member BaseActiveTableCell(ts: TableCellController, data: string, setData, ?isStickyHeader: bool) =
        let isStickyHeader = defaultArg isStickyHeader false
        let tempData, setTempData = React.useState(data)
        React.useEffect((fun _ ->
            setTempData data
        ), [| box data |])
        TableCell.BaseCell(
            ts.Index.y,
            ts.Index.x,
            Html.input [
                prop.autoFocus true
                prop.className "rounded-none w-full h-full bg-base-100 text-base-content px-2 py-1 outline-none"
                if isStickyHeader then
                    prop.style [ style.position.sticky; style.height Constants.Table.DefaultRowHeight; style.top Constants.Table.DefaultRowHeight ]
                prop.defaultValue tempData
                prop.onChange (fun (e: string) ->
                    setTempData e
                )
                prop.onKeyDown (fun e ->
                    ts.onKeyDown e
                    match e.code with
                    | kbdEventCode.enter ->
                        setData tempData
                    | _ -> ()
                )
                prop.onBlur (fun e ->
                    ts.onBlur e
                    setData tempData
                )
            ]
        )

    static member CompositeCellActiveRender(tableCellController: TableCellController, cell: CompositeCell, setCell: CompositeCell -> unit) =

        match cell with
        | CompositeCell.Term oa ->
            let term =
                if oa.isEmpty() then
                    None
                else
                    Term.fromOntologyAnnotation oa |> Some
            let setTerm = fun (t: Term option) ->
                let oa =
                    t
                    |> Option.map Term.toOntologyAnnotation
                    |> Option.defaultValue (OntologyAnnotation())
                setCell(CompositeCell.Term oa)
            let termDropdownRenderer =
                fun (client: Browser.Types.ClientRect) (dropdown: ReactElement) ->
                    Html.div [
                        prop.className "absolute z-50"
                        prop.style [
                            style.left (int (client.left + Browser.Dom.window.scrollX - 2.))
                            style.top (int (client.bottom + Browser.Dom.window.scrollY + 5.))
                        ]
                        prop.children [ dropdown ]
                    ]
            TermSearch.TermSearch(
                setTerm,
                term,
                onBlur = (fun _ -> tableCellController.onBlur !!()),
                onKeyDown = (fun e -> tableCellController.onKeyDown e),
                classNames = TermSearchStyle(!^"rounded-none px-1 py-1 w-full h-full bg-base-100 text-base-content"),
                autoFocus = true,
                portalModals = Browser.Dom.document.body,
                portalTermDropdown = PortalTermDropdown(Browser.Dom.document.body, termDropdownRenderer)
            )
        | CompositeCell.FreeText txt ->
            TableCell.BaseActiveTableCell(
                tableCellController,
                txt,
                fun t -> setCell(CompositeCell.FreeText t)
            )
        | CompositeCell.Unitized (v, oa) ->
            TableCell.BaseActiveTableCell(
                tableCellController,
                v,
                fun t -> setCell(CompositeCell.Unitized (v,oa))
            )
        | CompositeCell.Data d ->
            TableCell.BaseActiveTableCell(
                tableCellController,
                Option.defaultValue "" d.Name,
                fun t ->
                    d.Name <- t |> Option.whereNot System.String.IsNullOrWhiteSpace
                    setCell(CompositeCell.Data d)
            )