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
            ?debug: bool
        ) =
        let debug = defaultArg debug false

        Html.div [
            prop.key $"{rowIndex}-{columnIndex}"
            if debug then
                prop.testId $"cell-{rowIndex}-{columnIndex}"
            prop.className [
                if className.IsSome then
                    className.Value
            ]
            if props.IsSome then
                yield! props.Value
            prop.children [ content ]
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