namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI


module private TableHelper =

    let (|ActiveTrigger|Default|) (eventCode: string) =
        let lower = eventCode.ToLower()

        if
            (lower.StartsWith("key")
             || lower.StartsWith("digit")
             || lower.StartsWith("numpad"))
            || lower.StartsWith("backspace")
        then
            ActiveTrigger
        else
            Default


    let keyDownController
        (e: Browser.Types.KeyboardEvent)
        (selectedCells: GridSelect.GridSelectHandle)
        (activeCellIndex: CellCoordinate option, setActiveCellIndex)
        onKeydown
        =
        if activeCellIndex.IsNone then
            let nav = selectedCells.selectBy e

            if not nav && selectedCells.count > 0 then
                onKeydown
                |> Option.iter (fun onKeydown -> onKeydown (e, selectedCells, activeCellIndex))

                match e.code with
                | ActiveTrigger ->
                    selectedCells.SelectOrigin
                    |> Option.defaultValue selectedCells.selectedCellsReducedSet.MinimumElement
                    |> Some
                    |> setActiveCellIndex
                | Default -> ()



[<Mangle(false); Erase>]
type Table =

    static member TableCellStyle =
        """data-[selected=true]:text-secondary-content data-[selected=true]:bg-secondary
data-[is-append-origin=true]:border data-[is-append-origin=true]:border-base-content
data-[active=true]:border-2 data-[active=true]:border-primary data-[active=true]:!bg-transparent
cursor-pointer
select-none
p-0"""

    [<ReactComponent(true)>]
    static member Table
        (
            rowCount: int,
            columnCount: int,
            renderCell: TableCellController -> ReactElement,
            renderActiveCell: TableCellController -> ReactElement,
            ref: IRefValue<TableHandle>,
            ?onSelect: GridSelect.OnSelect,
            ?onKeydown: (Browser.Types.KeyboardEvent * GridSelect.GridSelectHandle * CellCoordinate option) -> unit,
            ?enableColumnHeaderSelect: bool,
            ?defaultStyleSelect: bool,
            ?debug: bool
        ) =
        let debug = defaultArg debug false
        let enableColumnHeaderSelect = defaultArg enableColumnHeaderSelect false
        let defaultStyleSelect = defaultArg defaultStyleSelect true

        let scrollContainerRef = React.useElementRef ()

        let rowVirtualizer =
            Virtual.useVirtualizer (
                count = rowCount,
                getScrollElement = (fun () -> scrollContainerRef.current),
                estimateSize = (fun _ -> Constants.Table.DefaultRowHeight),
                overscan = 2,
                gap = 0,
                scrollPaddingStart = 1.5 * float Constants.Table.DefaultRowHeight,
                scrollPaddingEnd = 1.5 * float Constants.Table.DefaultRowHeight,
                rangeExtractor =
                    (fun range ->
                        let next = set [ 0; yield! Virtual.defaultRangeExtractor range ]
                        Set.toArray next)
            )

        let columnVirtualizer =
            Virtual.useVirtualizer (
                count = columnCount,
                getScrollElement = (fun () -> scrollContainerRef.current),
                estimateSize = (fun _ -> Constants.Table.DefaultColumnWidth),
                gap = 0,
                overscan = 2,
                scrollPaddingEnd = 1.5 * float Constants.Table.DefaultColumnWidth,
                horizontal = true,
                rangeExtractor =
                    (fun range ->
                        let next =
                            set [
                                0

                                yield! Virtual.defaultRangeExtractor range
                            ]

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

        let activeCellIndex, setActiveCellIndex =
            React.useState (None: CellCoordinate option)

        let GridSelect =
            React.useGridSelect (
                rowCount = rowCount,
                columnCount = columnCount,
                minRow = (if enableColumnHeaderSelect then 0 else 1),
                minCol = 1,
                onSelect = onSelect
            )

        React.useImperativeHandle (
            ref,
            (fun () ->
                TableHandle(
                    focus = (fun () -> scrollContainerRef.current.Value.focus ()),
                    scrollTo = scrollTo,
                    SelectHandle =
                        SelectHandle(
                            contains = GridSelect.contains,
                            selectAt = GridSelect.selectAt,
                            clear = GridSelect.clear,
                            getSelectedCells =
                                (fun () ->
                                    match GridSelect.selectedCells with
                                    | Some range ->
                                        ResizeArray [|
                                            for x in range.xStart .. range.xEnd do
                                                for y in range.yStart .. range.yEnd do
                                                    yield {| x = x; y = y |}
                                        |]
                                    | None -> ResizeArray()),
                            getSelectedCellRange = (fun () -> GridSelect.selectedCells),
                            getCount = (fun () -> GridSelect.count)
                        )
                )),
            [| GridSelect.selectedCells |]
        )

        let createController (index: CellCoordinate) =
            let isSelected = GridSelect.contains index

            let isOrigin =
                GridSelect.SelectOrigin |> Option.exists (fun origin -> origin = index)

            let isActive = (activeCellIndex = Some index)

            TableCellController.init (
                index,
                isActive,
                isSelected,
                isOrigin,
                onKeyDown =
                    (fun (e: Browser.Types.KeyboardEvent) ->
                        match e.code with
                        | kbdEventCode.enter ->
                            if GridSelect.contains ({| index with y = index.y + 1 |}) then
                                setActiveCellIndex (Some({| index with y = index.y + 1 |}))
                            elif
                                GridSelect.contains (
                                    {|
                                        x = index.x + 1
                                        y = GridSelect.selectedCells.Value.yStart
                                    |}
                                )
                            then
                                setActiveCellIndex (
                                    Some {|
                                        x = index.x + 1
                                        y = GridSelect.selectedCells.Value.yStart
                                    |}
                                )
                            else
                                setActiveCellIndex (None)
                                scrollContainerRef.current.Value.focus ()
                                GridSelect.selectAt (index, false)
                        | kbdEventCode.escape ->
                            setActiveCellIndex (None)
                            scrollContainerRef.current.Value.focus ()
                            GridSelect.clear ()
                        | _ -> ()),
                onBlur = (fun _ -> setActiveCellIndex (None)),
                onClick =
                    (fun e ->
                        if not isActive then
                            if e.detail >= 2 then
                                setActiveCellIndex (Some index)

                                if GridSelect.count = 0 then
                                    GridSelect.selectAt (index, false)
                            else
                                let nextSet = Set.singleton (index)

                                if GridSelect.selectedCellsReducedSet = nextSet then
                                    GridSelect.clear ()
                                else
                                    GridSelect.selectAt (index, e.shiftKey))
            )

        React.fragment [
            Html.div [
                prop.ref scrollContainerRef
                prop.onKeyDown (fun e ->
                    TableHelper.keyDownController e GridSelect (activeCellIndex, setActiveCellIndex) onKeydown)
                prop.tabIndex 0
                prop.className "overflow-auto h-96 w-full border border-primary rounded-sm"
                if debug then
                    prop.testId "virtualized-table"
                prop.children [
                    Html.div [
                        prop.style [
                            style.height (rowVirtualizer.getTotalSize ())
                            style.width (columnVirtualizer.getTotalSize ())
                            style.position.relative
                        ]
                        prop.children [
                            Html.table [
                                prop.className "w-full h-full border-collapse"
                                prop.children [
                                    Html.thead [
                                        prop.key "table-thead"
                                        prop.className "[&>tr>th]:border [&>tr>th]:border-neutral"
                                        prop.children [
                                            Html.tr [
                                                prop.key "header"
                                                prop.className "sticky top-0 left-0 z-10 bg-base-100 text-left"
                                                prop.style [ style.height Constants.Table.DefaultRowHeight ]
                                                prop.children [
                                                    for virtualColumn in columnVirtualizer.getVirtualItems () do
                                                        let controller =
                                                            createController {| x = virtualColumn.index; y = 0 |}

                                                        Html.th [
                                                            prop.ref columnVirtualizer.measureElement
                                                            prop.custom ("data-index", virtualColumn.index)
                                                            prop.key virtualColumn.key
                                                            prop.className [
                                                                if virtualColumn.index <> 0 then
                                                                    "min-w-32"
                                                                else
                                                                    "min-w-min"
                                                                "h-full resize-x overflow-x-auto"
                                                                if defaultStyleSelect then
                                                                    Table.TableCellStyle
                                                            ]
                                                            prop.dataRow 0
                                                            prop.dataColumn virtualColumn.index
                                                            prop.style [
                                                                style.position.absolute
                                                                style.top 0
                                                                style.left 0
                                                                style.custom (
                                                                    "transform",
                                                                    $"translateX({virtualColumn.start}px)"
                                                                )
                                                            ]
                                                            if controller.IsActive then
                                                                prop.custom ("data-active", true)
                                                            if controller.IsSelected then
                                                                prop.custom ("data-selected", true)
                                                            if controller.IsOrigin then
                                                                prop.custom ("data-is-append-origin", true)
                                                            prop.children [
                                                                if virtualColumn.index = 0 then
                                                                    TableCell.BaseCell(
                                                                        controller.Index.y,
                                                                        controller.Index.x,
                                                                        Html.text (
                                                                            rowVirtualizer.getVirtualIndexes ()
                                                                            |> Seq.last
                                                                        ),
                                                                        className =
                                                                            "px-2 py-1 flex items-center cursor-not-allowed w-full h-full min-w-8 bg-base-200 text-transparent"
                                                                    )
                                                                elif controller.IsActive then
                                                                    renderActiveCell controller
                                                                else
                                                                    renderCell controller
                                                            ]
                                                        ]
                                                ]
                                            ]
                                        ]
                                    ]
                                    Html.tbody [
                                        prop.style [ style.marginTop Constants.Table.DefaultRowHeight ]
                                        prop.className "[&>tr>td]:border [&>tr>td]:border-neutral"
                                        prop.children [
                                            for virtualRow in rowVirtualizer.getVirtualItems () do
                                                if virtualRow.index = 0 then
                                                    Html.none // skip header row, is part of thead
                                                else
                                                    Html.tr [
                                                        prop.key virtualRow.key
                                                        prop.style [
                                                            style.top 0
                                                            style.left 0
                                                            style.position.absolute
                                                            style.custom (
                                                                "transform",
                                                                $"translateY({virtualRow.start}px)"
                                                            )
                                                            style.height virtualRow.size
                                                        ]
                                                        prop.className "w-full"
                                                        prop.children [
                                                            for virtualColumn in columnVirtualizer.getVirtualItems () do
                                                                let index = {|
                                                                    x = virtualColumn.index
                                                                    y = virtualRow.index
                                                                |}

                                                                let controller = createController index

                                                                Html.td [
                                                                    prop.key virtualColumn.key
                                                                    prop.dataRow virtualRow.index
                                                                    prop.dataColumn virtualColumn.index
                                                                    prop.className [
                                                                        if defaultStyleSelect then
                                                                            Table.TableCellStyle
                                                                    ]
                                                                    if controller.IsActive then
                                                                        prop.custom ("data-active", true)
                                                                    if controller.IsSelected then
                                                                        prop.custom ("data-selected", true)
                                                                    if controller.IsOrigin then
                                                                        prop.custom ("data-is-append-origin", true)
                                                                    prop.style [
                                                                        if virtualColumn.index = 0 then
                                                                            style.position.sticky
                                                                            style.zIndex 10
                                                                        else
                                                                            style.position.absolute
                                                                        style.width virtualColumn.size
                                                                        style.height virtualRow.size
                                                                        style.top 0
                                                                        style.left 0
                                                                        style.custom (
                                                                            "transform",
                                                                            $"translateX({virtualColumn.start}px)"
                                                                        )
                                                                    ]
                                                                    prop.children [
                                                                        if controller.IsActive then
                                                                            renderActiveCell controller
                                                                        else
                                                                            renderCell controller
                                                                    ]
                                                                ]
                                                        ]
                                                    ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        let TableHandler = React.useRef<TableHandle> (null)
        let rowCount = 1_000
        let columnCount = 1_000

        let data, setData =
            React.useState (
                [|
                    for i in 0 .. rowCount - 1 do
                        [|
                            for j in 0 .. columnCount - 1 do
                                yield $"Row {i}, Column {j}"
                        |]
                |]
            )

        let render =
            React.memo (
                (fun (ts: TableCellController) ->
                    TableCell.BaseCell(
                        ts.Index.y,
                        ts.Index.x,
                        Html.div [
                            prop.key $"{ts.Index.x}-{ts.Index.y}"
                            prop.className "truncate"
                            if ts.Index.x = 0 then
                                prop.text ts.Index.y
                            else
                                prop.text data.[ts.Index.y].[ts.Index.x]
                            prop.onClick (fun e -> ts.onClick e)
                        ]
                    )),
                withKey = (fun (ts: TableCellController) -> $"{ts.Index.x}-{ts.Index.y}")
            )

        let renderActiveCell =
            React.memo (
                (fun (ts: TableCellController) ->
                    TableCell.BaseActiveTableCell(
                        ts,
                        data.[ts.Index.y].[ts.Index.x],
                        (fun newData ->
                            data.[ts.Index.y].[ts.Index.x] <- newData
                            setData data)
                    )),
                withKey = (fun (ts: TableCellController) -> $"{ts.Index.x}-{ts.Index.y}")
            )

        Html.div [
            prop.className "flex flex-col gap-4"
            prop.children [
                Html.button [
                    prop.className "btn btn-primary"
                    prop.text "scroll to 500, 500"
                    prop.onClick (fun _ ->
                        TableHandler.current.SelectHandle.selectAt ({| x = 500; y = 500 |}, false)
                        TableHandler.current.scrollTo ({| x = 500; y = 500 |}))
                ]
                Table.Table(
                    rowCount = rowCount,
                    columnCount = columnCount,
                    renderCell = render,
                    renderActiveCell = renderActiveCell,
                    ref = TableHandler
                )
            ]
        ]