module Renderer.components.Widgets.AddFilePickerWidget

open Feliz
open Fable.Core
open ARCtrl
open Swate.Components
open Swate.Electron.Shared

let private widgetContainerClass =
    "swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:min-w-80 swt:max-w-[95vw]"

let private refreshArcFileRef (arcFile: ArcFiles) =
    match arcFile with
    | ArcFiles.Investigation investigation -> ArcFiles.Investigation investigation
    | ArcFiles.Study(study, assays) -> ArcFiles.Study(study, assays)
    | ArcFiles.Assay assay -> ArcFiles.Assay assay
    | ArcFiles.Run run -> ArcFiles.Run run
    | ArcFiles.Workflow workflow -> ArcFiles.Workflow workflow
    | ArcFiles.DataMap(parent, dataMap) -> ArcFiles.DataMap(parent, dataMap)
    | ArcFiles.Template template -> ArcFiles.Template template

let private reindexPathEntries (entries: (int * string) list) =
    entries |> List.mapi (fun i (_, path) -> i + 1, path)

let private disabledState (message: string) =
    Html.div [
        prop.className widgetContainerClass
        prop.children [
            Html.h3 [
                prop.className "swt:font-bold"
                prop.text "File Picker"
            ]
            Html.span [
                prop.className "swt:text-xs swt:opacity-70"
                prop.text message
            ]
        ]
    ]

[<ReactComponent>]
let Main
    (
        arcFileState: ArcFiles option,
        activeTableIndex: int option,
        setArcFileState: ArcFiles option -> unit
    ) =

    let pathEntries, setPathEntries =
        React.useStateWithUpdater (List.empty<int * string>)
    let isPicking, setIsPicking = React.useState false
    let statusMessage, setStatusMessage = React.useState (None: string option)
    let widgetCtx = WidgetContext.useWidgetController ()

    let annotationCtx =
        React.useContext (Contexts.AnnotationTable.AnnotationTableStateCtx)

    let tryGetActiveTable (arcFile: ArcFiles) =
        match activeTableIndex with
        | Some tableIndex when tableIndex >= 0 && tableIndex < arcFile.Tables().Count ->
            Some(tableIndex, arcFile.Tables().[tableIndex])
        | _ -> None

    let selectedCells =
        match arcFileState with
        | Some arcFile ->
            match tryGetActiveTable arcFile with
            | Some (_, table) ->
                annotationCtx.state
                |> Map.tryFind table.Name
                |> Option.bind (fun tableCtx -> tableCtx.SelectedCells)
                |> Option.map (fun x -> {|
                    xStart = x.xStart - 1
                    xEnd = x.xEnd - 1
                    yStart = x.yStart - 1
                    yEnd = x.yEnd - 1
                |})
                |> unbox<Types.CellCoordinateRange option>
            | None -> None
        | None -> None

    let disabledMessage =
        match arcFileState with
        | None -> Some "Open an ARC file first."
        | Some arcFile ->
            match tryGetActiveTable arcFile with
            | Some _ -> None
            | None -> Some "Select a table tab first to use the file picker."

    let canInsert = pathEntries.Length > 0 && selectedCells.IsSome && disabledMessage.IsNone

    let appendPickedPaths (paths: string[]) =
        if paths.Length > 0 then
            setPathEntries (fun current ->
                let existing = current |> List.map snd |> Set.ofList

                let newItems =
                    paths
                    |> Array.toList
                    |> List.filter (fun path -> existing.Contains path |> not)
                    |> List.map (fun path -> 0, path)

                reindexPathEntries (current @ newItems)
            )

    let pickPaths () =
        promise {
            setIsPicking true
            setStatusMessage None

            try
                let! result = Api.pickPaths ()

                match result with
                | Ok paths ->
                    appendPickedPaths paths

                    if paths.Length = 0 then
                        setStatusMessage (Some "No paths selected.")
                | Error e ->
                    if e.Message <> "Cancelled" then
                        setStatusMessage (Some $"Failed to pick paths: {e.Message}")
            finally
                setIsPicking false
        }
        |> Promise.start

    let clearPaths () =
        setPathEntries (fun _ -> [])
        setStatusMessage None

    let sortAscending () =
        setPathEntries (fun current ->
            current |> List.sortBy snd |> reindexPathEntries
        )

    let sortDescending () =
        setPathEntries (fun current ->
            current |> List.sortByDescending snd |> reindexPathEntries
        )

    let movePath (id: int) (path: string) (delta: float) =
        setPathEntries (fun current ->
            current
            |> List.map (fun (iterIndex, iterPath) ->
                if (id, path) = (iterIndex, iterPath) then
                    (float iterIndex + delta, iterPath)
                else
                    (float iterIndex, iterPath)
            )
            |> List.sortBy fst
            |> List.mapi (fun i (_, value) -> i + 1, value)
        )

    let removePath (id: int, path: string) =
        setPathEntries (fun current ->
            current
            |> List.except [ id, path ]
            |> reindexPathEntries
        )

    let insertPaths () =
        match arcFileState with
        | None -> setStatusMessage (Some "Open an ARC file first.")
        | Some arcFile ->
            match tryGetActiveTable arcFile, selectedCells with
            | Some (_, table), Some selection ->
                let columnIndex = selection.xStart
                let mutable rowIndex = selection.yStart

                let cellsToInsert =
                    [|
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
                    table.SetCellsAt(cellsToInsert)
                    setArcFileState (Some (refreshArcFileRef arcFile))
                    setPathEntries (fun _ -> [])
                    setStatusMessage None
                    widgetCtx.closeWidget WidgetType.FilePicker
            | _ ->
                setStatusMessage (Some "Select a target cell in the table first.")

    match disabledMessage with
    | Some msg -> disabledState msg
    | None ->
        Html.div [
            prop.className widgetContainerClass
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
                            prop.disabled (pathEntries.IsEmpty)
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
                                Html.table [
                                    prop.className "swt:table swt:table-sm swt:table-zebra swt:table-fixed swt:min-w-full"
                                    prop.children [
                                        Html.tbody [
                                            for id, path in pathEntries do
                                                Html.tr [
                                                    prop.key $"{id}_{path}"
                                                    prop.children [
                                                        Html.td [
                                                            prop.className "swt:w-12 swt:font-mono"
                                                            prop.text id
                                                        ]
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
                            ]
                        ]
                    ]
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
            ]
        ]
