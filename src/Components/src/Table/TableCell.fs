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
                "select-none h-full w-full border border-base-200 snap-center items-center flex"
                if className.IsSome then
                    className.Value
            ]
            if props.IsSome then
                yield! props.Value
            if style.IsSome then
                prop.style style.Value
            prop.children [ content ]
        ]

    static member DefaultClasses = "select-none h-full w-full border border-base-200 snap-center"

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

    static member StickyHeader(index: int, ?debug: bool, ?defaultHeight) =
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
                Html.div [ prop.className "text-lg"; prop.children [ Html.text $"Column {index}" ] ]
            ]
        ]

    // [<ReactMemoComponent>]
    // static member private CompositeCellRender
    //     (
    //         rowIndex,
    //         columnIndex,
    //         cell: CompositeCell,
    //         setCell: CompositeCell -> unit,
    //         cellState,
    //         setActiveCellIndex,
    //         ?className,
    //         ?debug
    //     ) =
    //     let basicInput =
    //         fun (onChange: string -> unit, value: string) ->
    //             Html.input [
    //                 prop.className "rounded-none w-full h-full bg-base-100 text-base-content px-2 py-1"
    //                 prop.onChange onChange
    //                 prop.valueOrDefault value
    //                 prop.autoFocus true
    //                 prop.onKeyDown(fun e ->
    //                     match e.code with
    //                     | kbdEventCode.enter ->
    //                         e.preventDefault()
    //                         setActiveCellIndex (None)
    //                     | kbdEventCode.escape ->
    //                         e.preventDefault()
    //                         setActiveCellIndex (None)
    //                     | anyElse ->
    //                         ()
    //                         // e.preventDefault()
    //                         // setActiveCellIndex (Some {| y = rowIndex; x = columnIndex |})
    //                 )
    //                 prop.onBlur(fun e ->
    //                     setActiveCellIndex (None)
    //                 )
    //             ]

    //     let eleRef = React.useElementRef ()

    //     React.useListener.onClickAway (
    //         eleRef,
    //         fun _ ->
    //             if cellState.IsActive then
    //                 setActiveCellIndex (None)
    //     )

    //     React.useMemo (
    //         (fun () ->
    //             Html.div [
    //                 prop.className "w-full h-full grow flex overflow-visible"
    //                 prop.title (cell.ToString())
    //                 prop.onClick (fun e ->
    //                     if not cellState.IsActive && e.detail >= 2 then
    //                         e.stopPropagation()
    //                         setActiveCellIndex (Some {| y = rowIndex; x = columnIndex |}))
    //                 prop.children [
    //                     match cellState.IsActive with
    //                     | false ->
    //                         Html.div [
    //                             prop.ref eleRef
    //                             prop.className "text-lg h-full w-full flex items-center px-2 py-1 truncate"
    //                             prop.text (cell.ToString())
    //                         ]
    //                     | true ->
    //                         Html.div [
    //                             prop.className "overflow-visible"
    //                             prop.ref eleRef
    //                             prop.children [
    //                                 match cell with
    //                                 | CompositeCell.FreeText text ->
    //                                     basicInput (
    //                                         (fun (e: string) -> setCell (CompositeCell.createFreeText e)),
    //                                         text
    //                                     )
    //                                 | CompositeCell.Term term ->
    //                                     TermSearch.TermSearch(
    //                                         onTermSelect =
    //                                             (fun term ->
    //                                                 let term = Option.defaultValue (Term()) term

    //                                                 setCell (
    //                                                     CompositeCell.createTermFromString (
    //                                                         ?name = term.name,
    //                                                         ?tsr = term.source,
    //                                                         ?tan = term.id
    //                                                     )
    //                                                 )),
    //                                         term =
    //                                             (Term(
    //                                                 ?name = term.Name,
    //                                                 ?source = term.TermSourceREF,
    //                                                 ?id = term.TermAccessionNumber
    //                                              )
    //                                              |> Some),
    //                                         classNames = (TermSearchStyle(!^"rounded-none w-full grow h-full px-2 py-1 truncate"))
    //                                     )
    //                                 | CompositeCell.Unitized(v, term) ->
    //                                     basicInput (
    //                                         (fun (e: string) -> setCell (CompositeCell.createUnitized (e, term))),
    //                                         $"{v} {term.NameText}"
    //                                     )
    //                                 | CompositeCell.Data data ->
    //                                     basicInput (
    //                                         (fun (e: string) ->
    //                                             setCell (
    //                                                 CompositeCell.createDataFromString(
    //                                                     e,
    //                                                     ?format = data.Format,
    //                                                     ?selectorFormat = data.SelectorFormat
    //                                                 )
    //                                             )),
    //                                         data.NameText
    //                                     )
    //                             ]
    //                         ]

    //                 ]
    //             ]),
    //         [| box cell; box cellState |]
    //     )

    // static member CompositeCell
    //     (
    //         rowIndex: int,
    //         columnIndex: int,
    //         cell: CompositeCell,
    //         cellState: TableCellState,
    //         setCell: CompositeCell -> unit,
    //         setActiveCellIndex,
    //         ?className: string,
    //         ?debug: bool
    //     ) =
    //     let className =
    //         [
    //             if not cellState.IsActive then
    //                 if cellState.IsSelected then
    //                     "bg-primary text-primary-content"
    //                 else
    //                     "hover:bg-base-300 text-base-content"
    //             else
    //                 "border border-primary"
    //             if cellState.IsOrigin then
    //                 "border-2 border-base-content"
    //         ]
    //         |> String.concat " "

    //     TableCell.BaseCell(
    //         rowIndex,
    //         columnIndex,
    //         content =
    //             TableCell.CompositeCellRender(
    //                 rowIndex,
    //                 columnIndex,
    //                 cell,
    //                 setCell,
    //                 cellState,
    //                 setActiveCellIndex,
    //                 ?debug = debug
    //             ),
    //         className = className,
    //         ?debug = debug
    //     )