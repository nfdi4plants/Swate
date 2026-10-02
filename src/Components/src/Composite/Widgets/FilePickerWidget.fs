namespace Swate.Components.Composite.Widgets

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.DataMapTable.ClipboardTarget
open Swate.Components.Composite.SortableList
open Swate.Components.Composite.SortableList.Types
open Swate.Components.Shared
open Swate.Components.Primitive
open Swate.Components.Primitive.Buttons
open Swate.Components.Primitive.LoadingSpinner
open Swate.Components.Composite.AnnotationTable.Context
open Swate.Components.Composite.DataMapTable
open Swate.Components.Composite.DataMapTable.Types

/// This context is designed to be used only internally in this file.
module private FilePickerWidgetContext =

    let SelectedPathsCtx =
        React.createContext<StateUpdaterContext<string list>> (unbox null)

    [<Hook>]
    let useSelectedPathsCtx () = React.useContext SelectedPathsCtx

module private FilePickerWidgetTypes =
    [<RequireQualifiedAccess>]
    type InsertTarget =
        | Table of index: int * selection: CellCoordinateRange
        | DataMap of selection: CellCoordinateRange

open FilePickerWidgetTypes

module private FilePickerWidgetHelper =
    let appendPickedPaths (setPaths: (string[] -> string[]) -> unit) =
        fun (paths: string[]) ->
            if paths.Length > 0 then
                setPaths (fun existingPaths ->

                    Array.append existingPaths paths |> Array.distinct
                )

    let insertPathsIntoSelectedCells (arcFile: ArcFiles) setArcFile (paths: string[]) (target: InsertTarget option) =
        let paths = paths |> Array.map (toArcRootRelativeFilePath arcFile)
        let nextArcFile = ArcFiles.refreshRef arcFile

        match target with
        | Some(InsertTarget.Table(tableIndex, selection)) ->
            let nextTable = nextArcFile.TryGetActiveTable(Some tableIndex).Value |> snd
            let columnIndex = selection.xStart
            let mutable rowIndex = selection.yStart

            // GetCellAt also resolves cells that were never stored, for example the Input and
            // Output cells of a freshly imported template row, which TryGetCellAt reports as missing.
            let cellsToInsert = [|
                for path in paths do
                    if columnIndex < nextTable.ColumnCount && rowIndex < nextTable.RowCount then
                        let cell = nextTable.GetCellAt(columnIndex, rowIndex)
                        let nextCell = cell.UpdateMainField path
                        let coordinate: CellCoordinate = {| x = columnIndex; y = rowIndex |}
                        coordinate, nextCell
                        rowIndex <- rowIndex + 1
            |]

            if cellsToInsert.Length = 0 then
                failwith "No valid cells to insert paths into. Please check the selected range and try again."
            else
                nextTable.SetCellsAt cellsToInsert
                setArcFile nextArcFile
        | Some(InsertTarget.DataMap selection) ->
            match nextArcFile.TryGetDataMap() with
            | Some dataMap ->
                let anchor: CellCoordinate = {|
                    x = selection.xStart
                    y = selection.yStart
                |}

                dataMap.PasteTabText(anchor, [| anchor |], String.concat System.Environment.NewLine paths)

                setArcFile nextArcFile
            | None -> ()
        | None -> ()


[<Erase; Mangle(false)>]
type FilePickerWidget =


    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member private Table(paths: string[], setPaths: (string[] -> string[]) -> unit) =
        let selectedPathsCtx = FilePickerWidgetContext.useSelectedPathsCtx ()

        let items: SortableListItem<unit>[] =
            React.useMemo (
                (fun () ->
                    paths
                    |> Array.map (fun path -> {|
                        id = path
                        label = path
                        data = None
                    |})
                ),
                [| box paths |]
            )

        Html.div [
            prop.className "swt:max-h-[45vh] swt:overflow-auto"
            prop.children [
                SortableList.SortableList(
                    items,
                    (fun nextItems -> setPaths (fun _ -> nextItems |> Array.map _.label)),
                    className = "swt:max-h-[45vh]",
                    rowProps =
                        (fun item -> [
                            prop.className [
                                "swt:cursor-pointer swt:table-auto"
                                if List.contains item.id selectedPathsCtx.state then
                                    "swt:bg-base-300"
                            ]
                            prop.onClick (fun _ ->
                                selectedPathsCtx.setStateUpdater (fun current ->
                                    if List.contains item.id current then
                                        current |> List.filter ((<>) item.id)
                                    else
                                        item.id :: current
                                )
                            )
                        ]),
                    renderRow =
                        (fun row ->
                            RowComponents.DefaultRow(
                                row,
                                label = Html.span [ prop.className "swt:font-mono"; prop.text row.item.label ]
                            )
                        )
                )
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
                        prop.title "Insert file paths into the currently selected table or DataMap cells."
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
                        prop.title "Insert selected file paths into the currently selected table or DataMap cells."
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
        let activeTable = arcFile.TryGetActiveTable(activeTableIndex)

        let insertionTarget: InsertTarget option =
            match activeTable with
            | Some(tableIndex, table) ->
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
                |> Option.map (fun selection -> InsertTarget.Table(tableIndex, selection))
            | None ->
                annotationCtx.state
                |> Map.tryFind DataMapTable.SelectionContextKey
                |> Option.bind _.SelectedCells
                |> unbox<CellCoordinateRange option>
                |> Option.map InsertTarget.DataMap

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

                FilePickerWidgetHelper.insertPathsIntoSelectedCells arcFile setArcFile paths insertionTarget

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
                    LoadingSpinner.LoadingSpinner("Loading Paths...", DaisyuiSize.LG)
                else if hasPaths then
                    let canInsert = hasPaths && insertionTarget.IsSome

                    FilePickerWidgetContext.SelectedPathsCtx.Provider(
                        selectContextState,
                        React.Fragment [
                            FilePickerWidget.SortPathsButtons(setPaths)

                            FilePickerWidget.Table(paths, setPaths)

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
