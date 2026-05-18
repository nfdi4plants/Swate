namespace Swate.Components.Composite.Widgets

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Shared
open Swate.Components.Primitive
open Swate.Components.Primitive.Buttons
open Swate.Components.Composite.AnnotationTable.Context

/// This context is designed to be used only internally in this file.
module private FilePickerWidgetContext =

    let SelectedPathsCtx =
        React.createContext<StateUpdaterContext<string list>> (unbox null)

    [<Hook>]
    let useSelectedPathsCtx () = React.useContext SelectedPathsCtx

    [<Hook>]
    let useSelectedPathCtx (path: string) =
        let ctx = useSelectedPathsCtx ()
        let isSelected = List.contains path ctx.state

        let toggle =
            React.useCallback (
                (fun () ->
                    ctx.setStateUpdater (fun current ->
                        if isSelected then
                            List.filter ((<>) path) current
                        else
                            path :: current
                    )
                ),
                [| box ctx; box path |]
            )

        {|
            isSelected = isSelected
            toggle = toggle
        |}

module private FilePickerWidgetHelper =
    let appendPickedPaths (setPaths: (string[] -> string[]) -> unit) =
        fun (paths: string[]) ->
            if paths.Length > 0 then
                setPaths (fun existingPaths ->

                    Array.append existingPaths paths |> Array.distinct
                )

    let movePathByIndex (setPaths: (string[] -> string[]) -> unit) =
        fun (oldIndex: int) (newIndex: int) ->
            setPaths (fun current -> DndKit.arrayMove (ResizeArray current, oldIndex, newIndex) |> Seq.toArray)

    let removePathAt (setPaths: (string[] -> string[]) -> unit) =
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

    let insertPathsIntoSelectedTableCells
        (arcFile: ArcFiles)
        setArcFile
        (activeTableIndex: int option)
        (paths: string[])
        (selectedCells: option<CellCoordinateRange>)
        =
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
                failwith "No valid cells to insert paths into. Please check the selected range and try again."
            else
                table.SetCellsAt cellsToInsert
                setArcFile (ArcFiles.refreshRef arcFile)
        | _ -> ()


[<Erase; Mangle(false)>]
type FilePickerWidget =


    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member private SortableTableRow
        (
            index: int,
            path: string,
            movePath: int -> int -> unit,
            removePath: int -> unit,
            ?key: string,
            ?isLastItem: bool
        ) =
        let sortable = DndKit.useSortable ({| id = path |})
        let filerPickerItemCtx = FilePickerWidgetContext.useSelectedPathCtx path

        let style = [
            style.custom ("transform", DndKit.CSS.Transform.toString sortable.transform)
            style.custom ("transition", sortable.transition)
        ]

        Html.tr [
            prop.key path
            prop.ref sortable.setNodeRef
            prop.style style
            prop.onClick (fun _ -> filerPickerItemCtx.toggle ())
            prop.className [
                "swt:cursor-pointer swt:table-auto"
                if filerPickerItemCtx.isSelected then
                    "swt:bg-base-300"
            ]
            prop.children [
                Html.td [
                    prop.className "swt:w-10"
                    prop.children [
                        Html.button [
                            prop.onClick (fun e -> e.stopPropagation ())
                            prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:cursor-grab"
                            prop.type'.button
                            yield! prop.spread sortable.attributes
                            yield! prop.spread sortable.listeners
                            prop.children [ Icons.ArrowUpDown() ]
                        ]
                    ]
                ]
                Html.td [
                    prop.className "swt:max-w-md swt:truncate swt:font-mono"
                    prop.title path
                    prop.text path
                ]
                Html.td [
                    prop.className "swt:w-20"
                    prop.children [
                        Html.div [
                            prop.className "swt:join"
                            prop.children [
                                Html.button [
                                    prop.className "swt:btn swt:btn-xs swt:join-item"
                                    prop.type'.button
                                    prop.disabled ((index = 0))
                                    prop.onClick (fun e ->
                                        e.stopPropagation ()
                                        movePath index (index - 1)
                                    )
                                    prop.children [ Icons.ArrowUp() ]
                                ]
                                Html.button [
                                    prop.className "swt:btn swt:btn-xs swt:join-item"
                                    prop.type'.button
                                    prop.disabled ((isLastItem.IsSome && isLastItem.Value))
                                    prop.onClick (fun e ->
                                        e.stopPropagation ()
                                        movePath index (index + 1)
                                    )
                                    prop.children [ Icons.ArrowDown() ]
                                ]
                            ]
                        ]
                    ]
                ]
                Html.td [
                    prop.className "swt:text-right swt:w-14"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-xs swt:btn-error swt:btn-outline"
                            prop.onClick (fun e ->
                                e.stopPropagation ()
                                removePath index
                            )
                            prop.children [ Icons.Delete() ]
                        ]
                    ]
                ]
            ]
        ]

    /// TODO: Virtualize paths
    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member private Table(paths: string[], setPaths: (string[] -> string[]) -> unit) =

        let movePath =
            React.useCallback (
                (fun current next -> FilePickerWidgetHelper.movePathByIndex setPaths current next),
                [| box setPaths |]
            )

        let removePath =
            React.useCallback ((fun id -> FilePickerWidgetHelper.removePathAt setPaths id), [| box setPaths |])

        Html.div [
            prop.className
                "swt:overflow-y-auto swt:overflow-x-auto swt:max-h-[45vh] swt:border swt:border-base-300 swt:rounded-box"
            prop.children [
                Html.table [
                    prop.className "swt:table swt:table-xs swt:table-fixed swt:min-w-full"
                    prop.children [
                        Html.tbody [
                            for index in 0 .. paths.Length - 1 do
                                let path = paths.[index]
                                let isLast = if index = paths.Length - 1 then Some true else None

                                FilePickerWidget.SortableTableRow(
                                    index,
                                    path,
                                    movePath,
                                    removePath,
                                    key = path,
                                    ?isLastItem = isLast
                                )
                        ]
                    ]
                ]
            ]
        ]

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member private SortPathsButtons(setPaths: (string[] -> string[]) -> unit) =

        let sortAscending = fun () -> setPaths (fun current -> current |> Array.sortBy id)

        let sortDescending =
            fun () -> setPaths (fun current -> current |> Array.sortByDescending id)

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
    static member private VerticalPathDragAndDropContext
        (paths: string[], setPaths: (string[] -> string[]) -> unit, children: ReactElement)
        =

        let movePathById =
            React.useCallback (
                (fun current next -> FilePickerWidgetHelper.movePathById setPaths current next),
                [| box setPaths |]
            )

        let pointerSensor =
            DndKit.useSensor (
                DndKit.PointerSensor,
                {|
                    activationConstraint = {| distance = 6 |}
                |}
            )

        let sensors = DndKit.useSensors [| pointerSensor |]

        let handleDragEnd (event: DndKit.IDndKitEvent) =

            let active = event.active
            let over = event.over

            if isNull over |> not then
                let activeId = string active.id
                let overId = string over.id

                if activeId <> overId then
                    movePathById activeId overId

        let itemIds = React.useMemo ((fun () -> ResizeArray paths), [| box paths |])

        DndKit.DndContext(
            sensors = sensors,
            onDragEnd = handleDragEnd,
            collisionDetection = DndKit.pointerWithin,
            children =
                DndKit.SortableContext(
                    items = itemIds,
                    strategy = DndKit.verticalListSortingStrategy,
                    children = children
                )
        )

    [<ReactComponent>]
    static member private ActionButtons
        (setPaths: (string[] -> string[]) -> unit, pickPaths, insertPaths: bool -> unit, canInsert: bool)
        =
        let selectedPathsCtx = FilePickerWidgetContext.useSelectedPathsCtx ()

        Html.div [
            prop.className "swt:flex swt:gap-2 swt:w-full"
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-outline"
                    prop.text "Pick more files"
                    prop.title "Select more file paths and add them to the list"
                    prop.onClick (fun _ -> pickPaths ())
                ]
                match selectedPathsCtx.state with
                | [] ->
                    Html.button [
                        prop.className "swt:btn swt:btn-neutral"
                        prop.text "Clear"
                        prop.title "Clear all file paths from the list"
                        prop.onClick (fun _ -> setPaths (fun _ -> [||]))
                    ]

                    Html.button [
                        prop.className "swt:btn swt:btn-primary swt:ml-auto"
                        prop.disabled (not canInsert)
                        prop.text "Insert file names"
                        prop.title
                            "Insert file paths into the currently selected table cells. Requires an active table view and selected cells."
                        prop.onClick (fun _ -> insertPaths false)
                    ]
                | _ ->
                    Html.button [
                        prop.className "swt:btn swt:btn-neutral"
                        prop.text "Clear Selected"
                        prop.title "Clear only the currently selected paths from the list"
                        prop.onClick (fun _ ->
                            setPaths (fun current ->
                                let selected = selectedPathsCtx.state
                                current |> Array.filter (fun path -> not (List.contains path selected))
                            )

                            selectedPathsCtx.setStateUpdater (fun _ -> [])
                        )
                    ]

                    Html.button [
                        prop.className "swt:btn swt:btn-primary swt:ml-auto"
                        prop.disabled (not canInsert)
                        prop.title
                            "Insert selected file paths into the currently selected table cells. Requires an active table view and selected cells."
                        prop.text "Insert selected"
                        prop.onClick (fun _ -> insertPaths true)
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private PickFilePathsButtons(onPickPaths: unit -> unit) =
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
        let selectedPaths, setSelectedPaths = React.useStateWithUpdater ([]: string list)
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

        let insertPaths =
            fun (useSelectedPaths: bool) ->
                let paths =
                    if useSelectedPaths then
                        paths |> Array.filter (fun path -> List.contains path selectedPaths)
                    else
                        paths

                FilePickerWidgetHelper.insertPathsIntoSelectedTableCells
                    arcFile
                    setArcFile
                    activeTableIndex
                    paths
                    selectedCells

        let selectContextState =
            React.useMemo (
                (fun () -> {
                    state = selectedPaths
                    setStateUpdater = setSelectedPaths
                }),
                [| selectedPaths |]
            )

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-sm"
            prop.children [
                if isLoading then
                    Buttons.LoadingSpinner("Loading Paths...", DaisyuiSize.LG)
                else if hasPaths then
                    let canInsert = hasActiveTableView && selectedCells.IsSome && hasPaths

                    FilePickerWidgetContext.SelectedPathsCtx.Provider(
                        selectContextState,
                        React.Fragment [
                            FilePickerWidget.SortPathsButtons(setPaths)

                            FilePickerWidget.VerticalPathDragAndDropContext(
                                paths,
                                setPaths,
                                FilePickerWidget.Table(paths, setPaths)
                            )

                            FilePickerWidget.ActionButtons(setPaths, pickPaths, insertPaths, canInsert)
                        ]
                    )
                else
                    Html.span [
                        prop.className "swt:text-sm swt:opacity-70 swt:p-4 swt:text-center"
                        prop.text
                            "No file paths selected. Click the button below to pick files and insert their paths into your table."
                    ]

                    FilePickerWidget.PickFilePathsButtons(pickPaths)
            ]
        ]
