namespace Swate.Components

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components.Shared
open Swate.Components.AnnotationTable
open Swate.Components.Widgets.Context


[<Erase; Mangle(false)>]
type FilePickerWidget =

    static member private WidgetContainerClass =
        "swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:min-w-80 swt:max-w-[95vw]"

    static member private reindexPathEntries(entries: (int * string) list) =
        entries |> List.mapi (fun index (_, path) -> index + 1, path)

    static member private disabledState(message: string) =
        Html.div [
            prop.className FilePickerWidget.WidgetContainerClass
            prop.children [
                Html.h3 [ prop.className "swt:font-bold"; prop.text "File Picker" ]
                Html.span [
                    prop.className "swt:text-xs swt:opacity-70"
                    prop.text message
                ]
            ]
        ]

    [<ReactComponent>]
    static member private Table
        (pathEntries: seq<int * string>, movePath: int -> string -> float -> unit, removePath: (int * string) -> unit)
        =
        Html.table [
            prop.className "swt:table swt:table-sm swt:table-zebra swt:table-fixed swt:min-w-full"
            prop.children [
                Html.tbody [
                    for id, path in pathEntries do
                        Html.tr [
                            prop.key $"{id}_{path}"
                            prop.children [
                                Html.td [ prop.className "swt:w-12 swt:font-mono"; prop.text id ]
                                Html.td [
                                    prop.className "swt:max-w-[28rem] swt:truncate"
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
                                                    prop.onClick (fun _ -> movePath id path -1.5)
                                                    prop.children [ Icons.ArrowUp() ]
                                                ]
                                                Html.button [
                                                    prop.className "swt:btn swt:btn-xs swt:join-item"
                                                    prop.onClick (fun _ -> movePath id path 1.5)
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
                                            prop.onClick (fun _ -> removePath (id, path))
                                            prop.children [ Icons.Delete() ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private ActionButtons
        (
            clearPaths: unit -> unit,
            insertPaths: unit -> unit,
            canInsert: bool,
            widgetCtx: WidgetControllerContext
        ) =
        Html.div [
            prop.className "swt:flex swt:gap-2"
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-outline"
                    prop.text "Cancel"
                    prop.onClick (fun _ ->
                        clearPaths ()
                        widgetCtx.closeWidget WidgetType.FilePicker
                    )
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
    static member Main
        (
            arcFile: ArcFiles,
            activeTableIndex: int option,
            setArcFile: ArcFiles -> unit,
            services: FilePickerWidgetServices
        ) =

        let pathEntries, setPathEntries =
            React.useStateWithUpdater (List.empty<int * string>)

        let isPicking, setIsPicking = React.useState false
        let statusMessage, setStatusMessage = React.useState (None: string option)
        let widgetCtx = useWidgetControllerCtx ()

        let annotationCtx = AnnotationTableContext.useAnnotationTableCtx ()

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

        let disabledMessage =
            match arcFile.TryGetActiveTable(activeTableIndex) with
            | Some _ -> None
            | None -> Some "Select a table tab first to use the file picker."

        let canInsert = pathEntries.Length > 0 && disabledMessage.IsNone

        let appendPickedPaths (paths: string[]) =
            if paths.Length > 0 then
                setPathEntries (fun current ->
                    let existingPaths = current |> List.map snd |> Set.ofList

                    let newItems =
                        paths
                        |> Array.toList
                        |> List.filter (fun path -> existingPaths.Contains path |> not)
                        |> List.map (fun path -> 0, path)

                    FilePickerWidget.reindexPathEntries (current @ newItems)
                )

        let pickPaths () =
            promise {
                setIsPicking true
                setStatusMessage None

                try
                    let! result = services.pickPaths ()

                    match result with
                    | Ok paths ->
                        appendPickedPaths paths

                        if paths.Length = 0 then
                            setStatusMessage (Some "No paths selected.")
                    | Error message when message <> "Cancelled" ->
                        setStatusMessage (Some $"Failed to pick paths: {message}")
                    | Error _ -> ()
                finally
                    setIsPicking false
            }
            |> Promise.start

        let clearPaths () =
            setPathEntries (fun _ -> [])
            setStatusMessage None

        let sortAscending () =
            setPathEntries (fun current -> current |> List.sortBy snd |> FilePickerWidget.reindexPathEntries)

        let sortDescending () =
            setPathEntries (fun current -> current |> List.sortByDescending snd |> FilePickerWidget.reindexPathEntries)

        let movePath (id: int) (path: string) (delta: float) =
            setPathEntries (fun current ->
                current
                |> List.map (fun (currentIndex, currentPath) ->
                    if (id, path) = (currentIndex, currentPath) then
                        (float currentIndex + delta, currentPath)
                    else
                        (float currentIndex, currentPath)
                )
                |> List.sortBy fst
                |> List.mapi (fun index (_, value) -> index + 1, value)
            )

        let removePath (id: int, path: string) =
            setPathEntries (fun current -> current |> List.except [ id, path ] |> FilePickerWidget.reindexPathEntries)

        let insertPaths () =

            match arcFile.TryGetActiveTable(activeTableIndex), selectedCells with
            | Some(_, table), Some selection ->
                let columnIndex = selection.xStart
                let mutable rowIndex = selection.yStart

                let cellsToInsert = [|
                    for _, path in pathEntries do
                        match table.TryGetCellAt(columnIndex, rowIndex) with
                        | Some cell ->
                            let nextCell = cell.UpdateMainField path
                            let coordinate: CellCoordinate = {| x = columnIndex; y = rowIndex |}
                            coordinate, nextCell
                            rowIndex <- rowIndex + 1
                        | None -> ()
                |]

                if cellsToInsert.Length = 0 then
                    setStatusMessage (Some "Could not write into the selected column.")
                else
                    table.SetCellsAt cellsToInsert
                    setArcFile (WidgetArcFile.refreshRef arcFile)
                    setPathEntries (fun _ -> [])
                    setStatusMessage None
                    widgetCtx.closeWidget WidgetType.FilePicker
            | _ -> setStatusMessage (Some "Select a target cell in the table first.")

        match disabledMessage with
        | Some message -> FilePickerWidget.disabledState message
        | None ->
            Html.div [
                prop.className FilePickerWidget.WidgetContainerClass
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:items-end swt:gap-2"
                        prop.children [
                            Html.h3 [
                                prop.className "swt:text-xl swt:font-bold"
                                prop.text "File Picker"
                            ]
                            Html.span [
                                prop.className "swt:text-xs swt:opacity-70 swt:ml-auto"
                                prop.textf "%d entries" pathEntries.Length
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:flex swt:flex-wrap swt:gap-2"
                        prop.children [
                            Html.button [
                                prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                prop.disabled isPicking
                                prop.text "Pick Files"
                                prop.onClick (fun _ -> pickPaths ())
                            ]
                            Html.button [
                                prop.className "swt:btn swt:btn-sm swt:btn-outline swt:ml-auto"
                                prop.disabled pathEntries.IsEmpty
                                prop.text "Clear"
                                prop.onClick (fun _ -> clearPaths ())
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:text-xs swt:opacity-70"
                        prop.text (
                            match selectedCells with
                            | Some selection ->
                                $"Insert starts at column {selection.xStart + 1}, row {selection.yStart + 1}."
                            | None -> "Select a target cell in the table to enable insertion."
                        )
                    ]
                    if statusMessage.IsSome then
                        Html.div [
                            prop.className "swt:alert swt:alert-warning swt:text-xs"
                            prop.children [ Html.text statusMessage.Value ]
                        ]
                    if pathEntries.IsEmpty |> not then
                        React.Fragment [
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
                            Html.div [
                                prop.className
                                    "swt:overflow-y-auto swt:overflow-x-auto swt:max-h-[45vh] swt:border swt:border-base-300 swt:rounded-box"
                                prop.children [
                                    FilePickerWidget.Table(pathEntries, movePath, removePath)
                                ]
                            ]
                        ]
                    FilePickerWidget.ActionButtons(clearPaths, insertPaths, canInsert, widgetCtx)
                ]
            ]
