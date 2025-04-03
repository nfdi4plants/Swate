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
            ?style,
            ?props,
            ?debug: bool
        ) =
        let debug = defaultArg debug false

        Html.div [
            prop.key $"{rowIndex}-{columnIndex}"
            if debug then
                prop.testId $"cell-{rowIndex}-{columnIndex}"
            prop.className [
                "select-none h-full w-full border border-base-200 snap-center truncate"
                if className.IsSome then
                    className.Value
            ]
            if props.IsSome then
                yield! props.Value
            if style.IsSome then
                prop.style style.Value
            prop.children [ content ]
        ]

    static member StickyIndexColumn(index: int, ?debug: bool, ?defaultWidth) =
        let defaultWidth = defaultArg defaultWidth Constants.Table.DefaultColumnWidth

        TableCell.BaseCell(
            rowIndex = index,
            columnIndex = 0,
            style = [ style.position.sticky; style.width defaultWidth; style.left defaultWidth ],
            className = "bg-base-300 flex text-base-content items-center justify-center",
            content =
                Html.div [
                    prop.className "text-lg"
                    prop.children [
                        if index = 0 then
                            Html.text "Header"
                        else
                            Html.text $"Row {index}"
                    ]
                ],
            ?debug = debug
        )

    static member StickyHeader(index: int, ?debug: bool, ?defaultHeight) =
        let defaultHeight = defaultArg defaultHeight Constants.Table.DefaultRowHeight

        TableCell.BaseCell(
            rowIndex = 0,
            columnIndex = index,
            props = [
                prop.style [ style.position.sticky; style.height defaultHeight; style.top defaultHeight ]
            ],
            className = "bg-neutral text-neutral-content flex items-center px-2 py-1",
            content = Html.div [ prop.className "text-lg"; prop.children [ Html.text $"Column {index}" ] ],
            ?debug = debug
        )

    [<ReactMemoComponent>]
    static member private CompositeCellRender
        (
            rowIndex,
            columnIndex,
            cell: CompositeCell,
            setCell: CompositeCell -> unit,
            cellState,
            setActiveCellIndex,
            ?className,
            ?debug
        ) =
        let basicInput =
            fun (onChange: string -> unit, value: string) ->
                Html.input [
                    prop.className "rounded-none w-full h-full bg-base-100 text-base-content px-2 py-1"
                    prop.onChange onChange
                    prop.valueOrDefault value
                    prop.autoFocus true
                ]

        let eleRef = React.useElementRef ()

        React.useListener.onClickAway (
            eleRef,
            fun _ ->
                if cellState.IsActive then
                    setActiveCellIndex (None)
        )

        React.useMemo (
            (fun () ->
                Html.div [
                    prop.className "w-full h-full grow flex"
                    prop.title (cell.ToString())
                    prop.onClick (fun e ->
                        if not cellState.IsActive && e.detail >= 2 then
                            console.log ("Double click on cell")
                            setActiveCellIndex (Some {| y = rowIndex; x = columnIndex |}))
                    prop.children [
                        match cellState.IsActive with
                        | false ->
                            Html.div [
                                prop.ref eleRef
                                prop.className "text-lg h-full w-full flex items-center px-2 py-1"
                                prop.text (cell.ToString())
                            ]
                        | true ->
                            Html.div [
                                prop.ref eleRef
                                prop.children [
                                    match cell with
                                    | CompositeCell.FreeText text ->
                                        basicInput (
                                            (fun (e: string) -> setCell (CompositeCell.createFreeText e)),
                                            text
                                        )
                                    | CompositeCell.Term term ->
                                        TermSearch.TermSearch(
                                            onTermSelect =
                                                (fun term ->
                                                    let term = Option.defaultValue (Term()) term

                                                    setCell (
                                                        CompositeCell.createTermFromString (
                                                            ?name = term.name,
                                                            ?tsr = term.source,
                                                            ?tan = term.id
                                                        )
                                                    )),
                                            term =
                                                (Term(
                                                    ?name = term.Name,
                                                    ?source = term.TermSourceREF,
                                                    ?id = term.TermAccessionNumber
                                                 )
                                                 |> Some)
                                        )
                                    | CompositeCell.Unitized(v, term) ->
                                        basicInput (
                                            (fun (e: string) -> setCell (CompositeCell.createUnitized (e, term))),
                                            $"{v} {term.NameText}"
                                        )
                                    | CompositeCell.Data data ->
                                        basicInput (
                                            (fun (e: string) ->
                                                setCell (
                                                    CompositeCell.createDataFromString(
                                                        e,
                                                        ?format = data.Format,
                                                        ?selectorFormat = data.SelectorFormat
                                                    )
                                                )),
                                            data.NameText
                                        )
                                ]
                            ]

                    ]
                ]),
            [| box cell; box cellState |]
        )

    static member CompositeCell
        (
            rowIndex: int,
            columnIndex: int,
            cell: CompositeCell,
            cellState: TableCellState,
            setCell: CompositeCell -> unit,
            setActiveCellIndex,
            ?className: string,
            ?debug: bool
        ) =
        let className =
            [
                if not cellState.IsActive then
                    if cellState.IsSelected then
                        "bg-primary text-primary-content"
                    else
                        "hover:bg-base-300 text-base-content"
                else
                    "border border-primary"
                if cellState.IsOrigin then
                    "border-2 border-base-content"
            ]
            |> String.concat " "

        TableCell.BaseCell(
            rowIndex,
            columnIndex,
            content =
                TableCell.CompositeCellRender(
                    rowIndex,
                    columnIndex,
                    cell,
                    setCell,
                    cellState,
                    setActiveCellIndex,
                    ?debug = debug
                ),
            className = className,
            ?debug = debug
        )