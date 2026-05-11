namespace Swate.Components.Widgets

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Shared
open Swate.Components.AnnotationTable
open Swate.Components.AnnotationTable.Context
open Swate.Components.Widgets.Context

module private FilePickerWidgetHelper =

    [<Literal>]
    let RemoveDropId = "__file_picker_remove_target__"

    let appendPickedPaths (setPaths: (string[] -> string[]) -> unit) =
        fun (paths: string[]) ->
            if paths.Length > 0 then
                setPaths (fun existingPaths ->

                    let currentSet = Set existingPaths
                    let newPathsSet = Set paths
                    let combinedSet = Set.union currentSet newPathsSet

                    combinedSet |> Set.toArray
                )

    let clearPaths (setPaths: (string[] -> string[]) -> unit) = fun () -> setPaths (fun _ -> [||])

    let sortAscending (setPaths: (string[] -> string[]) -> unit) =
        fun () -> setPaths (fun current -> current |> Array.sortBy id)

    let sortDescending (setPaths: (string[] -> string[]) -> unit) =
        fun () -> setPaths (fun current -> current |> Array.sortByDescending id)

    let movePath (setPaths: (string[] -> string[]) -> unit) =
        fun (currentIndex: int) (newIndex: int) ->
            setPaths (fun current ->
                if
                    currentIndex < 0
                    || currentIndex >= current.Length
                    || newIndex < 0
                    || newIndex >= current.Length
                then
                    current
                else
                    let pathToMove = current.[currentIndex]
                    let pathsWithout = current |> Array.removeAt currentIndex
                    pathsWithout |> Array.insertAt newIndex pathToMove
            )

    let removePath (setPaths: (string[] -> string[]) -> unit) =
        fun (index: int) -> setPaths (fun current -> current |> Array.removeAt index)

    let movePathById (setPaths: (string[] -> string[]) -> unit) =
        fun (activeId: string) (overId: string) ->
            setPaths (fun current ->
                let oldIndex = current |> Array.tryFindIndex ((=) activeId)
                let newIndex = current |> Array.tryFindIndex ((=) overId)

                match oldIndex, newIndex with
                | Some oldIndex, Some newIndex when oldIndex <> newIndex ->
                    DndKit.arrayMove (ResizeArray current, oldIndex, newIndex) |> Seq.toArray
                | _ -> current
            )

    let removePathById (setPaths: (string[] -> string[]) -> unit) =
        fun (pathId: string) -> setPaths (fun current -> current |> Array.filter (fun path -> path <> pathId))

    let insertPaths
        (arcFile: ArcFiles)
        setArcFile
        (activeTableIndex: int option)
        (paths: string[])
        (selectedCells: option<CellCoordinateRange>)
        =
        fun () ->

            match arcFile.TryGetActiveTable(activeTableIndex), selectedCells with
            | Some(_, table), Some selection ->
                let columnIndex = selection.xStart
                let mutable rowIndex = selection.yStart

                let cellsToInsert = [|
                    for path in paths do
                        match table.TryGetCellAt(columnIndex, rowIndex) with
                        | Some cell ->
                            let nextCell = cell.UpdateMainField path
                            let coordinate: CellCoordinate = {| x = columnIndex; y = rowIndex |}
                            coordinate, nextCell
                            rowIndex <- rowIndex + 1
                        | None -> ()
                |]

                if cellsToInsert.Length = 0 then
                    ()
                else
                    table.SetCellsAt cellsToInsert
                    setArcFile (ArcFiles.refreshRef arcFile)
            | _ -> ()


[<Erase; Mangle(false)>]
type FilePickerWidget =

    [<ReactComponent>]
    static member DisabledStateMessage(message: string) =
        Html.div [
            Html.h3 [ prop.className "swt:font-bold"; prop.text "File Picker" ]
            Html.span [
                prop.className "swt:text-xs swt:opacity-70"
                prop.text message
            ]
        ]

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member private SortableTableRow
        (index: int, path: string, movePath: int -> int -> unit, removePath: int -> unit)
        =
        let sortable = DndKit.useSortable ({| id = path |})

        let style = [
            style.custom ("transform", DndKit.CSS.Transform.toString sortable.transform)
            style.custom ("transition", sortable.transition)
        ]

        Html.tr [
            prop.key path
            prop.id path
            prop.ref sortable.setNodeRef
            prop.style style
            prop.children [
                Html.td [
                    prop.className "swt:w-10"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:cursor-grab"
                            prop.type'.button
                            yield! prop.spread sortable.attributes
                            yield! prop.spread sortable.listeners
                            prop.children [ Icons.ArrowUpDown() ]
                        ]
                    ]
                ]
                Html.td [ prop.className "swt:w-12 swt:font-mono"; prop.text index ]
                Html.td [
                    prop.className "swt:max-w-md swt:truncate"
                    prop.title path
                    prop.text path
                ]
                Html.td [
                    prop.className "swt:w-28"
                    prop.children [
                        Html.div [
                            prop.className "swt:join"
                            prop.children [
                                Html.button [
                                    prop.className "swt:btn swt:btn-xs swt:join-item"
                                    prop.type'.button
                                    prop.onClick (fun _ -> movePath index (index - 1))
                                    prop.children [ Icons.ArrowUp() ]
                                ]
                                Html.button [
                                    prop.className "swt:btn swt:btn-xs swt:join-item"
                                    prop.onClick (fun _ -> movePath index (index + 1))
                                    prop.children [ Icons.ArrowDown() ]
                                ]
                            ]
                        ]
                    ]
                ]
                Html.td [
                    prop.className "swt:w-20 swt:text-right"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-xs swt:btn-error swt:btn-outline"
                            prop.onClick (fun _ -> removePath index)
                            prop.children [ Icons.Delete() ]
                        ]
                    ]
                ]
            ]
        ]

    /// TODO: Virtualize paths
    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member Table(paths: string[], setPaths: (string[] -> string[]) -> unit) =

        let movePath =
            React.useCallback (
                (fun current next -> FilePickerWidgetHelper.movePath setPaths current next),
                [| box setPaths |]
            )

        let removePath =
            React.useCallback ((fun id -> FilePickerWidgetHelper.removePath setPaths id), [| box setPaths |])

        Html.div [
            prop.className
                "swt:overflow-y-auto swt:overflow-x-auto swt:max-h-[45vh] swt:border swt:border-base-300 swt:rounded-box"
            prop.children [
                Html.table [
                    prop.className "swt:table swt:table-xs swt:table-zebra swt:table-fixed swt:min-w-full"
                    prop.children [
                        Html.tbody [
                            for index in 0 .. paths.Length - 1 do
                                let path = paths.[index]

                                FilePickerWidget.SortableTableRow(index, path, movePath, removePath)
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private RemoveDropZone(isDragging: bool) =
        let droppable =
            DndKit.useDroppable (
                {|
                    id = FilePickerWidgetHelper.RemoveDropId
                |}
            )

        Html.button [
            prop.ref droppable.setNodeRef
            prop.type'.button
            prop.disabled (not isDragging)
            prop.className [ "swt:btn swt:btn-sm swt:btn-error"; "swt:btn-dash" ]
            prop.children [ Icons.Delete(); Html.span "Drop row here to remove" ]
        ]

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member SortPathsButtons(setPaths: (string[] -> string[]) -> unit) =

        let sortAscending = FilePickerWidgetHelper.sortAscending setPaths

        let sortDescending = FilePickerWidgetHelper.sortDescending setPaths

        Html.div [
            prop.className "swt:join"
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-sm swt:join-item"
                    prop.onClick (fun _ -> sortAscending ())
                    prop.children [ Icons.ArrowDownAZ() ]
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-sm swt:join-item"
                    prop.onClick (fun _ -> sortDescending ())
                    prop.children [ Icons.ArrowDownZA() ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private FilePathViewer(paths: string[], setPaths: (string[] -> string[]) -> unit) =

        let movePathById =
            React.useCallback (
                (fun current next -> FilePickerWidgetHelper.movePathById setPaths current next),
                [| box setPaths |]
            )

        let removePathById =
            React.useCallback ((fun id -> FilePickerWidgetHelper.removePathById setPaths id), [| box setPaths |])

        let isDragging, setIsDragging = React.useState false

        let pointerSensor =
            DndKit.useSensor (
                DndKit.PointerSensor,
                {|
                    activationConstraint = {| distance = 6 |}
                |}
            )

        let sensors = DndKit.useSensors [| pointerSensor |]

        let handleDragStart (_: DndKit.IDndKitEvent) = setIsDragging true

        let handleDragCancel (_: DndKit.IDndKitEvent) = setIsDragging false

        let handleDragEnd (event: DndKit.IDndKitEvent) =
            setIsDragging false

            let active = event.active
            let over = event.over

            if isNull over |> not then
                let activeId = string active.id
                let overId = string over.id

                if overId = FilePickerWidgetHelper.RemoveDropId then
                    removePathById activeId
                elif activeId <> overId then
                    movePathById activeId overId

        let itemIds = React.useMemo ((fun () -> ResizeArray paths), [| box paths |])

        DndKit.DndContext(
            sensors = sensors,
            onDragStart = handleDragStart,
            onDragCancel = handleDragCancel,
            onDragEnd = handleDragEnd,
            collisionDetection = DndKit.pointerWithin,
            children =
                DndKit.SortableContext(
                    items = itemIds,
                    strategy = DndKit.verticalListSortingStrategy,
                    children =
                        React.Fragment [
                            Html.div [
                                prop.className "swt:flex swt:flex-row swt:items-center swt:justify-between"
                                prop.children [
                                    FilePickerWidget.SortPathsButtons(setPaths)
                                    FilePickerWidget.RemoveDropZone(isDragging)
                                ]
                            ]

                            FilePickerWidget.Table(paths, setPaths)
                        ]
                )
        )

    [<ReactComponent>]
    static member private ActionButtons
        (pickPaths, clearPaths: unit -> unit, insertPaths: unit -> unit, canInsert: bool)
        =
        Html.div [
            prop.className "swt:flex swt:gap-2 swt:w-full"
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-outline"
                    prop.text "Cancel"
                    prop.onClick (fun _ -> clearPaths ())
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-neutral"
                    prop.text "Pick more files"
                    prop.onClick (fun _ -> pickPaths ())
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-primary swt:ml-auto"
                    prop.disabled (not canInsert)
                    prop.text "Insert file names"
                    prop.onClick (fun _ -> insertPaths ())
                ]
            ]
        ]

    [<ReactComponent>]
    static member PickFilePathsButtons(onPickPaths: unit -> unit) =
        Html.div [
            prop.className "swt:flex swt:flex-wrap swt:gap-2"
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-sm swt:btn-primary swt:w-full"
                    prop.text "Pick Files"
                    prop.onClick (fun _ -> onPickPaths ())
                ]
            ]
        ]

    [<ReactComponent(true)>]
    static member Main
        (
            arcFile: ArcFiles,
            activeTableIndex: int option,
            setArcFile: ArcFiles -> unit,
            onPickPaths: unit -> JS.Promise<string[]>
        ) =

        let paths, setPaths = React.useStateWithUpdater ([||]: string[])
        let isLoading, setIsLoading = React.useState false

        let annotationCtx = useAnnotationTableStateCtx ()

        let selectedCells =

            match arcFile.TryGetActiveTable(activeTableIndex) with
            | Some(_, table) ->
                annotationCtx.state
                |> Map.tryFind table.Name
                |> Option.bind (fun tableCtx -> tableCtx.SelectedCells)
                |> Option.map (fun selectedRange -> {|
                    xStart = selectedRange.xStart - 1
                    xEnd = selectedRange.xEnd - 1
                    yStart = selectedRange.yStart - 1
                    yEnd = selectedRange.yEnd - 1
                |})
                |> unbox<CellCoordinateRange option>
            | None -> None

        let hasActiveTableView = arcFile.TryGetActiveTable(activeTableIndex).IsSome

        let hasPaths = paths.Length > 0

        let pickPaths () =
            promise {
                setIsLoading true

                try
                    let! paths = onPickPaths ()
                    FilePickerWidgetHelper.appendPickedPaths setPaths paths
                finally
                    setIsLoading false
            }
            |> Promise.start

        let clearPaths = FilePickerWidgetHelper.clearPaths setPaths

        let insertPaths =
            FilePickerWidgetHelper.insertPaths arcFile setArcFile activeTableIndex paths selectedCells

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-sm"
            prop.children [
                if isLoading then
                    Components.LoadingSpinner("Loading Paths...", DaisyuiSize.LG)
                else if hasPaths then
                    let canInsert = hasActiveTableView && selectedCells.IsSome && hasPaths

                    FilePickerWidget.FilePathViewer(paths, setPaths)

                    FilePickerWidget.ActionButtons(pickPaths, clearPaths, insertPaths, canInsert)
                else
                    Html.span [
                        prop.className "swt:text-sm swt:opacity-70 swt:p-4 swt:text-center"
                        prop.text
                            "No file paths selected. Click the button below to pick files and insert their paths into your table."
                    ]

                    FilePickerWidget.PickFilePathsButtons(pickPaths)
            ]
        ]