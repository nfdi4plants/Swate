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
    static member BaseActiveTableHeader(ts: TableCellController, data: string, setData, ?debug: bool) =
        let tempData, setTempData = React.useState (data)
        React.useEffect ((fun _ -> setTempData data), [| box data |])
        TableCell.BaseCell(
            ts.Index.y,
            ts.Index.x,
            Html.input [
                prop.autoFocus true
                prop.className
                    "swt:rounded-none swt:w-full swt:h-full swt:bg-base-100 swt:text-base-content swt:px-2 swt:py-2 swt:outline-hidden"
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
                    "swt:rounded-none swt:w-full swt:h-full swt:bg-base-100 swt:text-base-content swt:px-2 swt:py-2 swt:outline-hidden"
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

    static member CompositeHeaderActiveRender
        (tableCellController: TableCellController, header: CompositeHeader, setHeader: CompositeHeader -> unit, ?debug, ?displayIndicators)
        =

        let handleTerm (oa: OntologyAnnotation) (setHeader: OntologyAnnotation -> unit) =
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
                    setHeader oa

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
                portalTermDropdown = PortalTermDropdown(Browser.Dom.document.body, termDropdownRenderer),
                ?debug = debug,
                ?displayIndicators = displayIndicators
            )

        let handleTerm header =
            match header with
            | CompositeHeader.Component oa ->
                let setHeader = fun x -> setHeader (CompositeHeader.Component x)
                handleTerm oa setHeader
            | CompositeHeader.Characteristic oa ->
                let setHeader = fun x -> setHeader (CompositeHeader.Characteristic x)
                handleTerm oa setHeader
            | CompositeHeader.Factor oa ->
                let setHeader = fun x -> setHeader (CompositeHeader.Factor x)
                handleTerm oa setHeader
            | CompositeHeader.Parameter oa ->
                let setHeader = fun x -> setHeader (CompositeHeader.Parameter x)
                handleTerm oa setHeader
            | _ -> failwith $"Unknown type {header}"

        match header with
        | CompositeHeader.Input io ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.Input io}", (fun _ -> setHeader (CompositeHeader.Input io)), ?debug = debug)
        | CompositeHeader.Output io ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.Output io}", (fun _ -> setHeader (CompositeHeader.Output io)), ?debug = debug)
        | CompositeHeader.Comment txt ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.Comment txt}", (fun _ -> setHeader (CompositeHeader.Comment txt)), ?debug = debug)
        | CompositeHeader.FreeText txt ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.FreeText txt}", (fun _ -> setHeader (CompositeHeader.FreeText txt)), ?debug = debug)
        | CompositeHeader.ProtocolType ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.ProtocolType}", (fun _ -> setHeader (CompositeHeader.ProtocolType)), ?debug = debug)
        | CompositeHeader.ProtocolDescription ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.ProtocolDescription}", (fun _ -> setHeader (CompositeHeader.ProtocolDescription)), ?debug = debug)
        | CompositeHeader.ProtocolUri ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.ProtocolUri}", (fun _ -> setHeader (CompositeHeader.ProtocolUri)), ?debug = debug)
        | CompositeHeader.ProtocolVersion ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.ProtocolVersion}", (fun _ -> setHeader (CompositeHeader.ProtocolVersion)), ?debug = debug)
        | CompositeHeader.ProtocolREF ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.ProtocolREF}", (fun _ -> setHeader (CompositeHeader.ProtocolREF)), ?debug = debug)
        | CompositeHeader.Performer ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.Performer}", (fun _ -> setHeader (CompositeHeader.Performer)), ?debug = debug)
        | CompositeHeader.Date ->
            TableCell.BaseActiveTableHeader(tableCellController, $"{CompositeHeader.Date}", (fun _ -> setHeader (CompositeHeader.Date)), ?debug = debug)
        | _ -> handleTerm header

    static member CompositeCellActiveRender
        (tableCellController: TableCellController, cell: CompositeCell, setCell: CompositeCell -> unit, ?debug, ?displayIndicators)
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
                portalTermDropdown = PortalTermDropdown(Browser.Dom.document.body, termDropdownRenderer),
                ?debug = debug,
                ?displayIndicators = displayIndicators
            )
        | CompositeCell.FreeText txt ->
            TableCell.BaseActiveTableCell(tableCellController, txt, fun t -> setCell (CompositeCell.FreeText t))
        | CompositeCell.Unitized(v, oa) ->
            TableCell.BaseActiveTableCell(tableCellController, v, fun _ -> setCell (CompositeCell.Unitized(v, oa)))
        | CompositeCell.Data d ->
            TableCell.BaseActiveTableCell(
                tableCellController,
                Option.defaultValue "" d.Name,
                (fun t ->
                    d.Name <- t |> Option.whereNot System.String.IsNullOrWhiteSpace
                    setCell (CompositeCell.Data d)),
                ?debug = debug
            )