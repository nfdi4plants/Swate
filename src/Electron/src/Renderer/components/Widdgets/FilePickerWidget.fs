namespace Renderer.components.Widgets

open Feliz
open Fable.Core
open ARCtrl
open ARCtrl.Json

open Swate.Components


type InsertStart = {
    ColumnIndex: int
    RowIndex: int
}

type FilePickerState =

    static member Swap (left: int) (right: int) (fileNames: string list) =
        let arr = fileNames |> List.toArray
        let tmp = arr.[left]
        arr.[left] <- arr.[right]
        arr.[right] <- tmp
        arr |> Array.toList

    static member MoveUp (index: int) (fileNames: string list) =
        if index > 0 && index < fileNames.Length then
            FilePickerState.Swap index (index - 1) fileNames
        else
            fileNames

    static member MoveDown (index: int) (fileNames: string list) =
        if index >= 0 && index < fileNames.Length - 1 then
            FilePickerState.Swap index (index + 1) fileNames
        else
            fileNames

    static member RemoveAt (index: int) (fileNames: string list) =
        fileNames
        |> List.mapi (fun currentIndex name ->
            if currentIndex = index then
                None
            else
                Some name
        )
        |> List.choose id

type InsertResult = {
    RequestedCount: int
    InsertedCount: int
}

type FilePickerDataSource =

    static member InsertFileNames (table: ArcTable) (start: InsertStart) (fileNames: string list) : InsertResult =
        let rec loop (rowIndex: int) (remaining: string list) (inserted: int) =
            match remaining with
            | [] -> inserted
            | nextName :: rest ->
                match table.TryGetCellAt(start.ColumnIndex, rowIndex) with
                | Some currentCell ->
                    let nextCell = currentCell.UpdateMainField nextName
                    table.SetCellAt(start.ColumnIndex, rowIndex, nextCell, true)
                    loop (rowIndex + 1) rest (inserted + 1)
                | None -> inserted

        let insertedCount = loop start.RowIndex fileNames 0

        {
            RequestedCount = fileNames.Length
            InsertedCount = insertedCount
        }

    static member InsertDataMapFileNames (dataMap: DataMap) (start: InsertStart) (fileNames: string list) : InsertResult =
        if start.ColumnIndex < 0 || start.ColumnIndex >= dataMap.ColumnCount || start.RowIndex < 0 then
            {
                RequestedCount = fileNames.Length
                InsertedCount = 0
            }
        else
            let rec loop (rowIndex: int) (remaining: string list) (inserted: int) =
                match remaining with
                | [] -> inserted
                | nextName :: rest ->
                    if rowIndex >= dataMap.RowCount then
                        inserted
                    else
                        let currentCell = dataMap.GetCell(start.ColumnIndex, rowIndex)
                        let nextCell = currentCell.UpdateMainField nextName
                        dataMap.SetCell(start.ColumnIndex, rowIndex, nextCell)
                        loop (rowIndex + 1) rest (inserted + 1)

            let insertedCount = loop start.RowIndex fileNames 0

            {
                RequestedCount = fileNames.Length
                InsertedCount = insertedCount
            }

    static member SyncArcVault (arcFile: ArcFiles) : JS.Promise<Result<unit, string>> =
        Renderer.ArcFilePersistence.saveArcFile arcFile

type FilePickerWidget =

    static member CreateFilePickerButtons (fileInputRef: IRefValue<Browser.Types.HTMLElement option>) setFileNames setStatus canInsert tryInsertFileNames =
        Html.div [
            prop.className "swt: p-2"
            prop.children [
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-primary swt:w-full"
                    prop.text "Pick file names"
                    prop.onClick (fun _ ->
                        fileInputRef.current
                        |> Option.iter (fun input -> input.click ())
                    )
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-row swt:justify-center swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.type'.button
                            prop.className
                                "swt:btn swt:btn-neutral swt:btn-outline swt:bg-neutral swt:text-white swt:hover:btn-primary"
                            prop.text "Cancel"
                            prop.onClick (fun _ ->
                                setFileNames []
                                setStatus None
                            )
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className [
                                "swt:btn"
                                if canInsert then "swt:btn-primary" else "swt:btn-error"
                            ]
                            if not canInsert then
                                prop.disabled true
                            prop.text "Insert file names"
                            prop.onClick (fun _ -> tryInsertFileNames ())
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member StatusElement (status: StatusMessage) =
        let classNames =
            match status.Kind with
            | StatusKind.Info -> [ "swt:alert-info"; "swt:text-info-content" ]
            | StatusKind.Warning -> [ "swt:alert-warning"; "swt:text-warning-content" ]
            | StatusKind.Error -> [ "swt:alert-error"; "swt:text-error-content" ]

        Html.div [
            prop.className ([ "swt:alert swt:py-2 swt:text-sm" ] @ classNames)
            prop.children [ Html.span status.Text ]
        ]

    [<ReactComponent>]
    static member Main
        (
            activeTableData: ActiveTableData option,
            activeDataMapData: ActiveDataMapData option,
            onTableMutated: unit -> unit
        ) =
        let fileNames, setFileNames = React.useState List.empty<string>
        let status, setStatus = React.useState (None: StatusMessage option)
        let fileInputRef = React.useElementRef ()

        let annotationTableCtx =
            React.useContext Contexts.AnnotationTable.AnnotationTableStateCtx

        let selectedInsertStart =
            let selectedCells =
                match activeTableData, activeDataMapData with
                | Some tableData, _ ->
                    annotationTableCtx.state
                    |> Map.tryFind tableData.TableName
                    |> Option.bind (fun ctx -> ctx.SelectedCells)
                | None, Some _ ->
                    annotationTableCtx.state
                    |> Map.tryFind DataMapTable.SelectionContextKey
                    |> Option.bind (fun ctx -> ctx.SelectedCells)
                | None, None -> None

            selectedCells
            |> Option.map (fun cells -> {
                ColumnIndex = cells.xStart - 1
                RowIndex = cells.yStart - 1
            })
            |> Option.filter (fun start -> start.ColumnIndex >= 0 && start.RowIndex >= 0)

        let canInsert =
            (activeTableData.IsSome || activeDataMapData.IsSome)
            && selectedInsertStart.IsSome
            && not fileNames.IsEmpty

        let setErrorStatus text =
            setStatus (
                Some {
                    Kind = StatusKind.Error
                    Text = text
                }
            )

        let tryInsertFileNames () =
            match activeTableData, activeDataMapData, selectedInsertStart with
            | None, None, _ ->
                setErrorStatus "Open a table or datamap before inserting file names."
            | _, _, None ->
                setErrorStatus "Select a start cell in the active table/datamap before inserting file names."
            | Some tableData, _, Some start ->
                if fileNames.IsEmpty then
                    setErrorStatus "Select at least one file before inserting."
                else
                    let insertResult =
                        FilePickerDataSource.InsertFileNames tableData.Table start fileNames

                    if insertResult.InsertedCount = 0 then
                        setErrorStatus "No file names were inserted. Check your selected start cell."
                    else
                        onTableMutated ()
                        let isPartialInsert = insertResult.InsertedCount < insertResult.RequestedCount
                        promise {
                            let! syncResult =
                                FilePickerDataSource.SyncArcVault tableData.ArcFile

                            let statusMessage =
                                match isPartialInsert, syncResult with
                                | false, Ok() ->
                                    {
                                        Kind = StatusKind.Info
                                        Text = $"Inserted {insertResult.InsertedCount} file name(s)."
                                    }
                                | true, Ok() ->
                                    {
                                        Kind = StatusKind.Warning
                                        Text =
                                            $"Inserted {insertResult.InsertedCount} of {insertResult.RequestedCount} file name(s). Reached end of table."
                                    }
                                | false, Error syncError ->
                                    {
                                        Kind = StatusKind.Error
                                        Text = $"File names inserted locally, but save failed: {syncError}"
                                    }
                                | true, Error syncError ->
                                    {
                                        Kind = StatusKind.Warning
                                        Text =
                                            $"Inserted {insertResult.InsertedCount} of {insertResult.RequestedCount} file name(s), but save failed: {syncError}"
                                    }
                            setStatus (Some statusMessage)
                        }
                        |> Promise.start

            | None, Some dataMapData, Some start ->
                if fileNames.IsEmpty then
                    setErrorStatus "Select at least one file before inserting."
                else
                    let insertResult =
                        FilePickerDataSource.InsertDataMapFileNames dataMapData.DataMap start fileNames

                    if insertResult.InsertedCount = 0 then
                        setErrorStatus "No file names were inserted. Check your selected start cell."
                    else
                        onTableMutated ()
                        let isPartialInsert = insertResult.InsertedCount < insertResult.RequestedCount
                        promise {
                            let! syncResult =
                                FilePickerDataSource.SyncArcVault dataMapData.ArcFile

                            let statusMessage =
                                match isPartialInsert, syncResult with
                                | false, Ok() ->
                                    {
                                        Kind = StatusKind.Info
                                        Text = $"Inserted {insertResult.InsertedCount} file name(s)."
                                    }
                                | true, Ok() ->
                                    {
                                        Kind = StatusKind.Warning
                                        Text =
                                            $"Inserted {insertResult.InsertedCount} of {insertResult.RequestedCount} file name(s). Reached end of datamap."
                                    }
                                | false, Error syncError ->
                                    {
                                        Kind = StatusKind.Error
                                        Text = $"File names inserted locally, but save failed: {syncError}"
                                    }
                                | true, Error syncError ->
                                    {
                                        Kind = StatusKind.Warning
                                        Text =
                                            $"Inserted {insertResult.InsertedCount} of {insertResult.RequestedCount} file name(s), but save failed: {syncError}"
                                    }
                            setStatus (Some statusMessage)
                        }
                        |> Promise.start

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-3 swt:p-2"
            prop.children [
                if activeTableData.IsNone && activeDataMapData.IsNone then
                    Html.div [
                        prop.className "swt:text-sm swt:opacity-70"
                        prop.text "No active table/datamap. Open a table or datamap and select a start cell."
                    ]
                Html.input [
                    prop.ref fileInputRef
                    prop.type'.file
                    prop.multiple true
                    prop.style [ style.display.none ]
                    prop.onChange (fun (files: Browser.Types.File list) ->
                        let nextFileNames =
                            files
                            |> List.map (fun file -> file.name)

                        setFileNames nextFileNames
                        setStatus None
                    )
                ]
                if not fileNames.IsEmpty then
                    Html.div [
                        prop.className "swt:join swt:self-start"
                        prop.children [
                            Html.button [
                                prop.type'.button
                                prop.className "swt:btn swt:join-item"
                                prop.title "Sort ascending"
                                prop.onClick (fun _ ->
                                    fileNames
                                    |> List.sort
                                    |> setFileNames
                                )
                                prop.children [ Icons.ArrowDownAZ() ]
                            ]
                            Html.button [
                                prop.type'.button
                                prop.className "swt:btn swt:join-item"
                                prop.title "Sort descending"
                                prop.onClick (fun _ ->
                                    fileNames
                                    |> List.sortDescending
                                    |> setFileNames
                                )
                                prop.children [ Icons.ArrowDownZA() ]
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:overflow-y-auto swt:overflow-x-hidden swt:max-h-72 swt:border swt:rounded-md"
                        prop.children [
                            Html.table [
                                prop.className "swt:table swt:table-zebra swt:table-xs"
                                prop.children [
                                    Html.tbody [
                                        for index, fileName in fileNames |> List.indexed do
                                            Html.tr [
                                                prop.key $"{index}-{fileName}"
                                                prop.children [
                                                    Html.td [ Html.b $"{index + 1}" ]
                                                    Html.td [
                                                        Html.div [
                                                            prop.title fileName
                                                            prop.className "swt:max-w-72 swt:truncate"
                                                            prop.text fileName
                                                        ]
                                                    ]
                                                    Html.td [
                                                        Html.div [
                                                            prop.className "swt:join"
                                                            prop.children [
                                                                Html.button [
                                                                    prop.type'.button
                                                                    prop.className "swt:btn swt:btn-xs swt:join-item"
                                                                    prop.title "Move up"
                                                                    prop.onClick (fun _ ->
                                                                        setFileNames (FilePickerState.MoveUp index fileNames)
                                                                    )
                                                                    prop.children [ Icons.ArrowUp() ]
                                                                ]
                                                                Html.button [
                                                                    prop.type'.button
                                                                    prop.className "swt:btn swt:btn-xs swt:join-item"
                                                                    prop.title "Move down"
                                                                    prop.onClick (fun _ ->
                                                                        setFileNames (FilePickerState.MoveDown index fileNames)
                                                                    )
                                                                    prop.children [ Icons.ArrowDown() ]
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                    Html.td [
                                                        prop.style [ style.textAlign.right ]
                                                        prop.children [
                                                            Html.button [
                                                                prop.type'.button
                                                                prop.className "swt:btn swt:btn-xs swt:btn-error swt:btn-outline"
                                                                prop.title "Remove"
                                                                prop.onClick (fun _ ->
                                                                    setFileNames (FilePickerState.RemoveAt index fileNames)
                                                                )
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
                FilePickerWidget.CreateFilePickerButtons fileInputRef setFileNames setStatus canInsert tryInsertFileNames
                match status with
                | Some message -> FilePickerWidget.StatusElement message
                | None -> Html.none
            ]
        ]
