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
    type Range =
        interface end

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
    type Virtualizer<'A,'B> =
        member this.getVirtualItems() : VirtualItem [] = jsNative
        member this.getTotalSize() : int = jsNative
        member this.scrollToIndex (index: int, ?options: {|align: AlignOption option; behavior: ScrollBehavior option|}) : unit = jsNative

type Virtual =

    [<ImportMember(Virtual.ImportPath)>]
    static member defaultRangeExtractor(range: Virtual.Range) : int [] = jsNative

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
            ?rangeExtractor: Virtual.Range -> int [],
            ?debug: bool,
            ?onChange: (Virtual.Virtualizer<_,_> * bool) -> unit,
            ?horizontal: bool,
            ?paddingStart: int,
            ?paddingEnd: int,
            ?gap: int,
            ?lanes: int
        ) : Virtual.Virtualizer<obj, obj> = jsNative

[<Mangle(false); Erase>]
type Table =


    [<ReactComponent(true)>]
    static member Table(
        rowCount: int,
        columnCount: int,
        renderCell: CellCoordinate -> bool -> bool -> ReactElement,
        ref: IRefValue<TableHandle>,
        ?onSelect: GridSelect.OnSelect,
        ?defaultStyleSelect: bool,
        ?debug: bool
    ) =
        let debug = defaultArg debug false
        let defaultStyleSelect = defaultArg defaultStyleSelect true

        let scrollContainerRef = React.useElementRef()

        let rowVirtualizer = Virtual.useVirtualizer(
            count = rowCount,
            getScrollElement = (fun () -> scrollContainerRef.current),
            estimateSize = (fun _ -> Constants.Table.DefaultRowHeight),
            overscan = 2,
            scrollMargin = -Constants.Table.DefaultRowHeight,
            scrollPaddingEnd = 1.5 * float Constants.Table.DefaultRowHeight,
            rangeExtractor = (fun range ->
                let next = set [
                    0
                    yield! Virtual.defaultRangeExtractor range
                ]
                Set.toArray next
            )
        )

        let columnVirtualizer = Virtual.useVirtualizer(
            count = columnCount,
            getScrollElement = (fun () -> scrollContainerRef.current),
            estimateSize = (fun _ -> Constants.Table.DefaultColumnWidth),
            overscan = 2,
            scrollMargin = -Constants.Table.DefaultColumnWidth,
            scrollPaddingEnd = 1.5 * float Constants.Table.DefaultColumnWidth,
            horizontal = true,
            rangeExtractor = (fun range ->
                let next = set [
                    0
                    yield! Virtual.defaultRangeExtractor range
                ]
                Set.toArray next
            )
        )

        let scrollTo = fun (coordinate: CellCoordinate) ->
            rowVirtualizer.scrollToIndex(coordinate.y)
            columnVirtualizer.scrollToIndex(coordinate.x)

        let onSelect =
            fun cell newCellRange ->
                onSelect |> Option.iter(fun onSelect ->
                    onSelect cell newCellRange
                )
                scrollTo(cell)

        let GridSelect = React.useGridSelect(
            kbdNavContainer = scrollContainerRef,
            rowCount = rowCount,
            columnCount = columnCount,
            minRow = 1,
            minCol = 1,
            onSelect = onSelect
        )

        React.useImperativeHandle(
            ref,
            (fun () ->
                TableHandle(
                    scrollTo = scrollTo,
                    select = SelectHandle(
                        contains = GridSelect.contains,
                        SelectOrigin = GridSelect.SelectOrigin,
                        selectAt = GridSelect.selectAt,
                        clear = GridSelect.clear
                    )
                )
            ),
            [| GridSelect.selectedCells |]
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
                        style.marginTop Constants.Table.DefaultRowHeight
                        style.marginLeft Constants.Table.DefaultColumnWidth
                        style.height (rowVirtualizer.getTotalSize())
                        style.width (columnVirtualizer.getTotalSize())
                        style.position.relative
                    ]
                    prop.className ""
                    prop.children [
                        for virtualRow in rowVirtualizer.getVirtualItems() do
                            React.keyedFragment(virtualRow.index, [
                                for virtualColumn in columnVirtualizer.getVirtualItems() do
                                    let isSelected = GridSelect.contains ({| x = virtualColumn.index; y = virtualRow.index |})
                                    let isOrigin = GridSelect.SelectOrigin |> Option.exists(fun origin -> origin = {| x = virtualColumn.index; y = virtualRow.index |})
                                    Html.div [
                                        prop.key virtualColumn.key
                                        prop.style [
                                            style.top 0
                                            style.left 0
                                            style.position.absolute
                                            style.custom("transform", $"translateX({virtualColumn.start}px) translateY({virtualRow.start}px)" )
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
                                        prop.onClick(fun e ->
                                            let nextSet = Set.singleton ({|x = virtualColumn.index; y = virtualRow.index|})
                                            if GridSelect.selectedCellsReducedSet = nextSet then
                                                GridSelect.clear()
                                            else
                                                GridSelect.selectAt({|x = virtualColumn.index; y = virtualRow.index|}, e.shiftKey)
                                        )
                                        prop.className [
                                            "cursor-pointer"
                                            if defaultStyleSelect then
                                                "data-[selected=true]:bg-accent data-[selected=true]:text-accent-content data-[is-append-origin=true]:border data-[is-append-origin=true]:border-base-content"
                                        ]
                                        prop.custom("data-selected", isSelected)
                                        prop.custom("data-is-append-origin", GridSelect.SelectOrigin |> Option.exists(fun origin -> origin = {| x = virtualColumn.index; y = virtualRow.index|}))
                                        prop.children (
                                            renderCell {| x = virtualColumn.index; y = virtualRow.index |} isSelected isOrigin
                                        )
                                    ]
                            ])
                    ]
                ]
            ]
        ]

    static member Entry() =
        let TableHandler = React.useRef<TableHandle>(null)
        let render =
            React.memo (
                (fun (coordinate: CellCoordinate) ->
                    if coordinate.x = 0 then
                        TableCell.StickyIndexColumn(coordinate.y, false)
                    elif coordinate.y = 0 then
                        TableCell.StickyHeader(coordinate.x, false)
                    else
                        TableCell.BaseCell(
                            coordinate.y,
                            coordinate.x,
                            Html.text $"Row {coordinate.y}, Column {coordinate.x}"
                        )
                ),
                withKey = (fun (coordinate: CellCoordinate) -> $"{coordinate.x}-{coordinate.y}")
            )
        Html.div [
            prop.className "flex flex-col gap-4"
            prop.children [
                Html.button [
                    prop.className "btn btn-primary"
                    prop.text "scroll to 500, 500"
                    prop.onClick (fun _ ->
                        TableHandler.current.select.selectAt({| x = 500; y = 500 |}, false)
                        TableHandler.current.scrollTo({| x = 500; y = 500 |})
                    )
                ]
                Table.Table(
                    rowCount = 1000,
                    columnCount = 1000,
                    renderCell = (fun cc _ _ -> render cc),
                    ref = TableHandler
                )
            ]
        ]
