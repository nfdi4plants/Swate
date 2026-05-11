namespace Swate.Components.Widgets

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.AnnotationTable
open Swate.Components.AnnotationTable.Context
open Swate.Components.Widgets.Context

module private FilePickerWidgetHelper =

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

    [<ReactComponent>]
    static member private TableRow(index: int, path: string, movePath: int -> int -> unit, removePath: int -> unit) =
        Html.tr [
            prop.key $"{index}_{path}"
            prop.children [
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

    [<ReactComponent>]
    static member private Table(paths: string[], setPaths: (string[] -> string[]) -> unit) =

        let movePath = FilePickerWidgetHelper.movePath setPaths
        let removePath = FilePickerWidgetHelper.removePath setPaths

        Html.table [
            prop.className "swt:table swt:table-xs swt:table-zebra swt:table-fixed swt:min-w-full"
            prop.children [
                Html.tbody [
                    for index in 0 .. paths.Length - 1 do
                        let path = paths.[index]

                        FilePickerWidget.TableRow(index, path, movePath, removePath)
                ]
            ]
        ]

    [<ReactComponent>]
    static member private ActionButtons(clearPaths: unit -> unit, insertPaths: unit -> unit, canInsert: bool) =
        Html.div [
            prop.className "swt:flex swt:gap-2"
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-outline"
                    prop.text "Cancel"
                    prop.onClick (fun _ -> clearPaths ())
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
    static member SortPathsButtons(sortAscending, sortDescending) =
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
    static member PickFilesButtons(onPickPaths: unit -> unit) =
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

        let sortAscending = FilePickerWidgetHelper.sortAscending setPaths

        let sortDescending = FilePickerWidgetHelper.sortDescending setPaths

        let clearPaths = FilePickerWidgetHelper.clearPaths setPaths

        let insertPaths =
            FilePickerWidgetHelper.insertPaths arcFile setArcFile activeTableIndex paths selectedCells

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                if isLoading then
                    Components.LoadingSpinner("Loading Paths...", DaisyuiSize.LG)
                else if hasPaths then
                    let canInsert = hasActiveTableView && selectedCells.IsSome && hasPaths

                    FilePickerWidget.SortPathsButtons(sortAscending, sortDescending)

                    Html.div [
                        prop.className
                            "swt:overflow-y-auto swt:overflow-x-auto swt:max-h-[45vh] swt:border swt:border-base-300 swt:rounded-box"
                        prop.children [ FilePickerWidget.Table(paths, setPaths) ]
                    ]

                    FilePickerWidget.ActionButtons(clearPaths, insertPaths, canInsert)
                else
                    FilePickerWidget.PickFilesButtons(pickPaths)
            ]
        ]