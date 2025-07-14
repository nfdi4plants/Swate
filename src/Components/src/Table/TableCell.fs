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
        (rowIndex: int, columnIndex: int, content: ReactElement, ?className: string, ?props, ?debug: bool)
        =
        let debug = defaultArg debug false
        Html.div [
            prop.key $"BaseCell-{rowIndex}-{columnIndex}"
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
    static member BaseActiveTableCell(ts: TableCellController, data: string, setData, ?isStickyHeader: bool, ?debug: bool) =
        let isStickyHeader = defaultArg isStickyHeader false
        let tempData, setTempData = React.useState (data)
        React.useEffect ((fun _ -> setTempData data), [| box data |])

        TableCell.BaseCell(
            ts.Index.y,
            ts.Index.x,
            Html.input [
                prop.autoFocus true
                prop.className
                    "swt:rounded-none swt:w-full swt:h-full swt:bg-base-100 swt:text-base-content swt:px-2 swt:py-1 swt:outline-hidden"
                if isStickyHeader then
                    prop.style [
                        style.position.sticky
                        style.height Constants.Table.DefaultRowHeight
                        style.top Constants.Table.DefaultRowHeight
                    ]
                prop.defaultValue tempData
                prop.onChange (fun (e: string) -> setTempData e)
                prop.onKeyDown (fun e ->
                    ts.onKeyDown e

                    match e.code with
                    | kbdEventCode.enter -> setData tempData
                    | _ -> ())
                prop.onBlur (fun e ->
                    ts.onBlur e
                    setData tempData)
            ],
            ?debug = debug
        )

    static member CompositeCellActiveRender
        (tableCellController: TableCellController, cell: CompositeCell, setCell: CompositeCell -> unit, ?debug)
        =

        match cell with
        | CompositeCell.Term oa ->
            let term =
                if oa.isEmpty () then
                    None
                else
                    Term.fromOntologyAnnotation oa |> Some

            let setTerm =
                fun (t: Term option) ->
                    let oa =
                        t
                        |> Option.map Term.toOntologyAnnotation
                        |> Option.defaultValue (OntologyAnnotation())

                    setCell (CompositeCell.Term oa)

            let termDropdownRenderer =
                fun (client: Browser.Types.ClientRect) (dropdown: ReactElement) ->
                    Html.div [
                        prop.className "swt:absolute swt:z-50"
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
                classNames =
                    TermSearchStyle(
                        !^"swt:rounded-none swt:px-1 swt:py-1 swt:w-full swt:h-full swt:bg-base-100 swt:text-base-content"
                    ),
                autoFocus = true,
                portalModals = Browser.Dom.document.body,
                portalTermDropdown = PortalTermDropdown(Browser.Dom.document.body, termDropdownRenderer)
            )
        | CompositeCell.FreeText txt ->
            TableCell.BaseActiveTableCell(tableCellController, txt, fun t -> setCell (CompositeCell.FreeText t))
        | CompositeCell.Unitized(v, oa) ->
            TableCell.BaseActiveTableCell(tableCellController, v, fun t -> setCell (CompositeCell.Unitized(v, oa)))
        | CompositeCell.Data d ->
            TableCell.BaseActiveTableCell(
                tableCellController,
                Option.defaultValue "" d.Name,
                (fun t ->
                    d.Name <- t |> Option.whereNot System.String.IsNullOrWhiteSpace
                    setCell (CompositeCell.Data d)),
                ?debug = debug
            )