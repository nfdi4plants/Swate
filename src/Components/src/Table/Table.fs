namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI

module Virtual =

    [<Literal>]
    let ImportPath = "@tanstack/react-virtual"

    [<ImportMember(ImportPath)>]
    type Range = interface end

    [<ImportMember(ImportPath)>]
    type VirtualItem =
        member this.key: string = jsNative
        member this.index: int = jsNative
        member this.start: int = jsNative
        member this.``end``: int = jsNative
        member this.size: int = jsNative

    [<StringEnum(CaseRules.LowerFirst)>]
    type AlignOption =
        | Auto
        | Start
        | Center
        | End

    [<StringEnum(CaseRules.LowerFirst)>]
    type ScrollBehavior =
        | Auto
        | Smooth

    [<ImportMember(ImportPath)>]
    type Virtualizer<'A, 'B> =
        member this.getVirtualItems() : VirtualItem[] = jsNative
        member this.getTotalSize() : int = jsNative

        member this.scrollToIndex
            (
                index: int,
                ?options:
                    {|
                        align: AlignOption option
                        behavior: ScrollBehavior option
                    |}
            ) : unit =
            jsNative

type Virtual =

    [<ImportMember(Virtual.ImportPath)>]
    static member defaultRangeExtractor(range: Virtual.Range) : int[] = jsNative

    [<ImportMember(Virtual.ImportPath)>]
    [<NamedParamsAttribute>]
    static member useVirtualizer
        (
            // required
            count: int,
            getScrollElement: unit -> option<Browser.Types.HTMLElement>,
            estimateSize: int -> int,
            // optional
            ?scrollMargin: float,
            ?scrollPaddingStart: float,
            ?scrollPaddingEnd: float,
            ?overscan: int,
            ?rangeExtractor: Virtual.Range -> int[],
            ?debug: bool,
            ?onChange: (Virtual.Virtualizer<_, _> * bool) -> unit,
            ?horizontal: bool,
            ?paddingStart: int,
            ?paddingEnd: int,
            ?gap: int,
            ?lanes: int
        ) : Virtual.Virtualizer<obj, obj> =
        jsNative

[<Mangle(false); Erase>]
type Table =

    static member DefaultRowHeight = 40
    static member DefaultColumnWidth = 150

    static member Cell(rowIndex: int, columnIndex: int, props, className, content: ReactElement, debug: bool) =
        Html.div [
            prop.key $"{rowIndex}-{columnIndex}"
            if debug then
                prop.testId $"cell-{rowIndex}-{columnIndex}"
            prop.className [
                "h-full w-full py-2 px-1 flex items-center justify-center border border-base-200 snap-center"
                className
            ]
            yield! props
            prop.children [ content ]
        ]

    static member StickyHeader(index: int, debug: bool) =
        Table.Cell(
            rowIndex = 0,
            columnIndex = index,
            props = [
                prop.style [
                    style.position.sticky
                    style.height Table.DefaultRowHeight
                    style.top Table.DefaultRowHeight
                ]
            ],
            className = "bg-neutral text-neutral-content",
            content = Html.div [ prop.className "text-lg"; prop.children [ Html.text $"Column {index}" ] ],
            debug = debug
        )

    static member StickyIndexColumn(index: int, debug: bool) =
        Table.Cell(
            rowIndex = index,
            columnIndex = 0,
            props = [
                prop.style [
                    style.position.sticky
                    style.width Table.DefaultColumnWidth
                    style.left Table.DefaultColumnWidth
                ]
            ],
            className = "bg-base-300 text-base-content",
            content = Html.div [ prop.className "text-lg"; prop.children [ Html.text $"Row {index}" ] ],
            debug = debug
        )

    [<ReactComponent(true)>]
    static member Table
        (
            rowCount: int,
            columnCount: int,
            renderCell: CellCoordinate -> ReactElement,
            ref: IRefValue<TableHandle>,
            ?onSelect: GridSelect.OnSelect,
            ?debug: bool
        ) =
        let debug = defaultArg debug false

        let scrollContainerRef = React.useElementRef ()

        let rowVirtualizer =
            Virtual.useVirtualizer (
                count = rowCount,
                getScrollElement = (fun () -> scrollContainerRef.current),
                estimateSize = (fun _ -> Table.DefaultRowHeight),
                overscan = 2,
                scrollMargin = -Table.DefaultRowHeight,
                scrollPaddingEnd = 1.5 * float Table.DefaultRowHeight,
                rangeExtractor =
                    (fun range ->
                        let next = set [ 0; yield! Virtual.defaultRangeExtractor range ]
                        Set.toArray next)
            )

        let columnVirtualizer =
            Virtual.useVirtualizer (
                count = columnCount,
                getScrollElement = (fun () -> scrollContainerRef.current),
                estimateSize = (fun _ -> Table.DefaultColumnWidth),
                overscan = 2,
                scrollMargin = -Table.DefaultColumnWidth,
                scrollPaddingEnd = 1.5 * float Table.DefaultColumnWidth,
                horizontal = true,
                rangeExtractor =
                    (fun range ->
                        let next = set [ 0; yield! Virtual.defaultRangeExtractor range ]
                        Set.toArray next)
            )

        let scrollTo =
            fun (coordinate: CellCoordinate) ->
                rowVirtualizer.scrollToIndex (coordinate.y)
                columnVirtualizer.scrollToIndex (coordinate.x)

        let onSelect =
            fun cell newCellRange ->
                onSelect |> Option.iter (fun onSelect -> onSelect cell newCellRange)
                scrollTo (cell)

        let GridSelect =
            React.useGridSelect (
                kbdNavContainer = scrollContainerRef,
                rowCount = rowCount,
                columnCount = columnCount,
                minRow = 1,
                minCol = 1,
                onSelect = onSelect
            )

        React.useImperativeHandle (
            ref,
            (fun () ->
                TableHandle(
                    scrollTo = scrollTo,
                    select =
                        SelectHandle(
                            contains = GridSelect.contains,
                            selectAt = GridSelect.selectAt,
                            clear = GridSelect.clear
                        )
                ))
        )

        Html.div [
            prop.ref scrollContainerRef
            prop.tabIndex 0
            prop.className "overflow-auto h-96 w-full border border-primary rounded snap-both snap-proximity"
            if debug then
                prop.testId "virtualized-table"
            prop.children [
                Html.div [
                    prop.style [
                        style.marginTop Table.DefaultRowHeight
                        style.marginLeft Table.DefaultColumnWidth
                        style.height (rowVirtualizer.getTotalSize ())
                        style.width (columnVirtualizer.getTotalSize ())
                        style.position.relative
                    ]
                    prop.className ""
                    prop.children [
                        for virtualRow in rowVirtualizer.getVirtualItems () do
                            React.keyedFragment (
                                virtualRow.index,
                                [
                                    for virtualColumn in columnVirtualizer.getVirtualItems () do
                                        Html.div [
                                            prop.key virtualColumn.key
                                            prop.style [
                                                style.top 0
                                                style.left 0
                                                style.position.absolute
                                                style.custom (
                                                    "transform",
                                                    $"translateX({virtualColumn.start}px) translateY({virtualRow.start}px)"
                                                )
                                                if virtualColumn.index = 0 then
                                                    style.height virtualRow.size
                                                    style.width (length.perc 100)
                                                    style.zIndex 2
                                                    style.pointerEvents.none
                                                elif virtualRow.index = 0 then
                                                    style.height (length.perc 100)
                                                    style.width virtualColumn.size
                                                    style.zIndex 1
                                                    style.pointerEvents.none
                                                else
                                                    style.height virtualRow.size
                                                    style.width virtualColumn.size
                                            ]
                                            prop.onClick (fun e ->
                                                let nextSet =
                                                    Set.singleton (
                                                        {|
                                                            x = virtualColumn.index
                                                            y = virtualRow.index
                                                        |}
                                                    )

                                                if GridSelect.selectedCellsReducedSet = nextSet then
                                                    GridSelect.clear ()
                                                else
                                                    e.preventDefault ()

                                                    GridSelect.selectAt (
                                                        {|
                                                            x = virtualColumn.index
                                                            y = virtualRow.index
                                                        |},
                                                        e.shiftKey
                                                    ))
                                            prop.className
                                                "data-[active=true]:bg-accent data-[active=true]:text-accent-content cursor-pointer data-[is-append-origin=true]:border data-[is-append-origin=true]:border-base-content"
                                            prop.custom (
                                                "data-active",
                                                GridSelect.contains (
                                                    {|
                                                        x = virtualColumn.index
                                                        y = virtualRow.index
                                                    |}
                                                )
                                            )
                                            prop.custom (
                                                "data-is-append-origin",
                                                GridSelect.selectOrigin
                                                |> Option.exists (fun origin ->
                                                    origin = {|
                                                                 x = virtualColumn.index
                                                                 y = virtualRow.index
                                                             |})
                                            )
                                            prop.children [
                                                renderCell {|
                                                    x = virtualColumn.index
                                                    y = virtualRow.index
                                                |}
                                            ]
                                        ]
                                ]
                            )
                    ]
                ]
            ]
        ]

    static member Entry() =
        let TableHandler = React.useRef<TableHandle> (null)

        let render =
            React.memo (
                (fun (coordinate: CellCoordinate) ->
                    if coordinate.x = 0 then
                        Table.StickyIndexColumn(coordinate.y, false)
                    elif coordinate.y = 0 then
                        Table.StickyHeader(coordinate.x, false)
                    else
                        Table.Cell(
                            coordinate.y,
                            coordinate.x,
                            [],
                            "",
                            Html.text $"Row {coordinate.y}, Column {coordinate.x}",
                            false
                        )),
                withKey = (fun (coordinate: CellCoordinate) -> $"{coordinate.x}-{coordinate.y}")
            )

        Html.div [
            prop.className "flex flex-col gap-4"
            prop.children [
                Html.button [
                    prop.className "btn btn-primary"
                    prop.text "scroll to 500, 500"
                    prop.onClick (fun _ ->
                        TableHandler.current.select.selectAt ({| x = 500; y = 500 |}, false)
                        TableHandler.current.scrollTo ({| x = 500; y = 500 |}))
                ]
                Table.Table(rowCount = 1000, columnCount = 1000, renderCell = render, ref = TableHandler)
            ]
        ]