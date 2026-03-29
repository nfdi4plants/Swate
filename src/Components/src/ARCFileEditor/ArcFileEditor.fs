namespace Swate.Components

open ARCtrl
open Fable.Core
open Feliz

[<RequireQualifiedAccess>]
type ArcFileEditorView =
    | Metadata
    | Table of int
    | DataMap
    | Error of string option

[<RequireQualifiedAccess>]
module ArcFileEditorView =

    let activeTableIndex =
        function
        | ArcFileEditorView.Table tableIndex -> Some tableIndex
        | _ -> None

    let toWidgetHostView =
        function
        | ArcFileEditorView.Table _ -> WidgetHostView.TableView
        | ArcFileEditorView.DataMap -> WidgetHostView.DataMapView
        | ArcFileEditorView.Metadata -> WidgetHostView.MetadataView
        | ArcFileEditorView.Error _ -> WidgetHostView.PreviewErrorView

type ArcFileEditorHeaderProps = {
    arcFile: ArcFiles
    activeView: ArcFileEditorView
    activeTableIndex: int option
    widgetHostView: WidgetHostView
}

[<Erase; Mangle(false)>]
type ArcFileEditor =

    static member private NewTablePrefix = "NewTable"

    static member private canCreateTables(arcFile: ArcFiles) =
        match arcFile with
        | ArcFiles.Assay _
        | ArcFiles.Study _
        | ArcFiles.Run _ -> true
        | _ -> false

    static member private canRenderDataMapView(arcFile: ArcFiles) =
        WidgetArcFile.tryGetDataMap arcFile |> Option.isSome

    static member private createNewTableName(tables: ResizeArray<ArcTable>) =
        let existingNames = tables |> Seq.map _.Name

        let rec loop index =
            let name = ArcFileEditor.NewTablePrefix + string index

            if Seq.contains name existingNames then
                loop (index + 1)
            else
                name

        loop 0

    [<ReactComponent>]
    static member private TablePreview(table: ArcTable, setTableInArcFile: ArcTable -> unit) =
        let tableState, setTableState = React.useState table

        let setTable(nextTable: ArcTable) =
            setTableState nextTable
            setTableInArcFile nextTable

        React.useEffect (
            (fun () -> setTableState table),
            [| box table |]
        )

        Html.div [
            prop.className "swt:w-screen swt:pb-4"
            prop.children [ AnnotationTable.AnnotationTable(tableState, setTable) ]
        ]

    [<ReactComponent>]
    static member private DataMapPreview(datamap: DataMap, setDatamapInArcFile: DataMap -> unit) =
        DataMapTable.DataMapTable(datamap, setDatamapInArcFile)

    static member private TableView
        (activeView: ArcFileEditorView)
        (arcFileState: ArcFiles)
        (setArcFileState: ArcFiles -> unit)
        =
        match activeView with
        | ArcFileEditorView.Metadata -> ArcFileMetadata.Preview(arcFileState, setArcFileState)
        | ArcFileEditorView.Table index ->
            let tables = arcFileState.Tables()

            if index < tables.Count then
                let setTable(nextTable: ArcTable) =
                    tables.[index] <- nextTable
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                ArcFileEditor.TablePreview(tables.[index], setTable)
            else
                Html.div [
                    prop.className "swt:p-4 swt:text-error"
                    prop.text "Table not found"
                ]
        | ArcFileEditorView.DataMap ->
            match arcFileState with
            | ArcFiles.Assay assay when assay.DataMap.IsSome ->
                let setDatamap(nextDatamap: DataMap) =
                    assay.DataMap <- Some nextDatamap
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                ArcFileEditor.DataMapPreview(assay.DataMap.Value, setDatamap)
            | ArcFiles.Study(study, assays) when study.DataMap.IsSome ->
                let setDatamap(nextDatamap: DataMap) =
                    study.DataMap <- Some nextDatamap
                    setArcFileState (ArcFiles.Study(study, assays))

                ArcFileEditor.DataMapPreview(study.DataMap.Value, setDatamap)
            | ArcFiles.Run run when run.DataMap.IsSome ->
                let setDatamap(nextDatamap: DataMap) =
                    run.DataMap <- Some nextDatamap
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                ArcFileEditor.DataMapPreview(run.DataMap.Value, setDatamap)
            | ArcFiles.DataMap(parent, datamap) ->
                let setDatamap(nextDatamap: DataMap) =
                    setArcFileState (ArcFiles.DataMap(parent, nextDatamap))

                ArcFileEditor.DataMapPreview(datamap, setDatamap)
            | _ ->
                Html.div [
                    prop.className "swt:p-4 swt:text-error"
                    prop.text "No DataMap available"
                ]
        | ArcFileEditorView.Error message ->
            Html.div [
                prop.className "swt:p-4 swt:text-error"
                prop.text $"The following error occured: {message}"
            ]

    [<ReactComponent>]
    static member private AddRowsFooter
        (arcFile: ArcFiles)
        (activeView: ArcFileEditorView)
        (setArcFile: ArcFiles -> unit)
        =
        let tables = arcFile.Tables()
        let minRowsToAdd = 1
        let rowsToAdd, setRowsToAdd = React.useState minRowsToAdd
        let rowInputRef = React.useInputRef ()

        let clampRowsToAdd rows = max minRowsToAdd rows

        let canAddRows =
            match activeView with
            | ArcFileEditorView.Table tableIndex -> tableIndex >= 0 && tableIndex < tables.Count
            | ArcFileEditorView.DataMap -> ArcFileEditor.canRenderDataMapView arcFile
            | ArcFileEditorView.Metadata
            | ArcFileEditorView.Error _ -> false

        let addRows() =
            let rowCount = clampRowsToAdd rowsToAdd

            if canAddRows then
                match activeView, arcFile with
                | ArcFileEditorView.Table tableIndex, _ when tableIndex >= 0 && tableIndex < tables.Count ->
                    tables.[tableIndex].AddRowsEmpty rowCount
                    setArcFile (WidgetArcFile.refreshRef arcFile)
                | ArcFileEditorView.DataMap, ArcFiles.Assay assay when assay.DataMap.IsSome ->
                    assay.DataMap.Value.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                    setArcFile (WidgetArcFile.refreshRef arcFile)
                | ArcFileEditorView.DataMap, ArcFiles.Study(study, _) when study.DataMap.IsSome ->
                    study.DataMap.Value.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                    setArcFile (WidgetArcFile.refreshRef arcFile)
                | ArcFileEditorView.DataMap, ArcFiles.Run run when run.DataMap.IsSome ->
                    run.DataMap.Value.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                    setArcFile (WidgetArcFile.refreshRef arcFile)
                | ArcFileEditorView.DataMap, ArcFiles.DataMap(_, dataMap) ->
                    dataMap.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                    setArcFile (WidgetArcFile.refreshRef arcFile)
                | _ -> ()

        if canAddRows then
            Html.div [
                prop.className
                    "swt:w-full swt:flex swt:justify-center swt:items-center swt:shrink-0 swt:p-2 swt:bg-base-200 swt:border-t swt:border-base-300"
                prop.title "Add Rows"
                prop.children [
                    Html.div [
                        prop.className "swt:join"
                        prop.children [
                            Html.input [
                                prop.className "swt:input swt:join-item swt:border-current"
                                prop.type'.number
                                prop.ref rowInputRef
                                prop.min minRowsToAdd
                                prop.defaultValue minRowsToAdd
                                prop.onChange (fun (value: int) -> setRowsToAdd (clampRowsToAdd value))
                                prop.onKeyDown (key.enter, fun _ -> addRows ())
                                prop.style [ style.width 100 ]
                            ]
                            Html.button [
                                prop.className "swt:btn swt:btn-outline swt:join-item"
                                prop.onClick (fun _ ->
                                    rowInputRef.current.Value.value <- unbox minRowsToAdd
                                    setRowsToAdd minRowsToAdd
                                    addRows ()
                                )
                                prop.children [ Icons.Plus() ]
                            ]
                        ]
                    ]
                ]
            ]
        else
            Html.none

    [<ReactComponent>]
    static member private Footer
        (arcFile: ArcFiles)
        (activeView: ArcFileEditorView)
        (setActiveView: ArcFileEditorView -> unit)
        (setArcFile: ArcFiles -> unit)
        =
        let tables = arcFile.Tables()
        let canAddTable = ArcFileEditor.canCreateTables arcFile

        let addNewTable() =
            if canAddTable then
                let nextName = ArcFileEditor.createNewTableName tables
                let nextTable = ArcTable.init nextName

                tables.Add nextTable
                setArcFile (WidgetArcFile.refreshRef arcFile)
                setActiveView (ArcFileEditorView.Table(tables.Count - 1))

        let footerTabBaseClasses =
            "swt:btn swt:btn-sm swt:border swt:!border-white swt:hover:!border-white swt:rounded-none"

        let tabIcon iconClass =
            Html.i [ prop.className [ "swt:iconify " + iconClass ] ]

        Html.div [
            prop.className "swt:flex swt:gap-0 swt:p-2 swt:bg-base-200 swt:border-t swt:border-base-300 swt:overflow-x-auto"
            prop.children [
                Html.button [
                    prop.className [
                        footerTabBaseClasses
                        if activeView = ArcFileEditorView.Metadata then
                            "swt:btn-primary swt:border-2 swt:!border-white"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView ArcFileEditorView.Metadata)
                    prop.children [
                        tabIcon "swt:fluent--info-24-regular"
                        Html.span [ prop.text "Metadata" ]
                    ]
                ]
                for index = 0 to tables.Count - 1 do
                    let table = tables.[index]

                    Html.button [
                        prop.key (string index)
                        prop.className [
                            footerTabBaseClasses
                            if activeView = ArcFileEditorView.Table index then
                                "swt:btn-primary swt:border-2 swt:!border-white"
                            else
                                "swt:btn-ghost"
                        ]
                        prop.onClick (fun _ -> setActiveView (ArcFileEditorView.Table index))
                        prop.children [
                            tabIcon "swt:fluent--table-24-regular"
                            Html.span [ prop.text table.Name ]
                        ]
                    ]
                if ArcFileEditor.canRenderDataMapView arcFile then
                    Html.button [
                        prop.className [
                            footerTabBaseClasses
                            if activeView = ArcFileEditorView.DataMap then
                                "swt:btn-primary swt:border-2 swt:!border-white"
                            else
                                "swt:btn-ghost"
                        ]
                        prop.onClick (fun _ -> setActiveView ArcFileEditorView.DataMap)
                        prop.children [
                            tabIcon "swt:fluent--database-24-regular"
                            Html.span [ prop.text "DataMap" ]
                        ]
                    ]
                if canAddTable then
                    Html.button [
                        prop.key "new-table-button"
                        prop.title "+"
                        prop.className
                            "swt:btn swt:btn-sm swt:btn-outline swt:items-center swt:border swt:!border-white swt:hover:!border-white swt:rounded-none"
                        prop.onClick (fun _ -> addNewTable ())
                        prop.children [ Html.span [ prop.text "+" ] ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member Main
        (
            arcFile: ArcFiles,
            setArcFile: ArcFiles -> unit,
            ?header: (ArcFileEditorHeaderProps -> ReactElement)
        ) =
        let activeView, setActiveView = React.useState ArcFileEditorView.Metadata

        React.useEffect (
            (fun () ->
                let tables = arcFile.Tables()

                let nextActiveView =
                    match activeView with
                    | ArcFileEditorView.Table tableIndex when tableIndex >= 0 && tableIndex < tables.Count -> activeView
                    | ArcFileEditorView.DataMap when ArcFileEditor.canRenderDataMapView arcFile -> activeView
                    | ArcFileEditorView.Metadata -> activeView
                    | _ ->
                        if tables.Count > 0 then
                            ArcFileEditorView.Table 0
                        else
                            ArcFileEditorView.Metadata

                if nextActiveView <> activeView then
                    setActiveView nextActiveView
            ),
            [| box arcFile; box activeView |]
        )

        let activeTableIndex = ArcFileEditorView.activeTableIndex activeView

        let headerProps = {
            arcFile = arcFile
            activeView = activeView
            activeTableIndex = activeTableIndex
            widgetHostView = ArcFileEditorView.toWidgetHostView activeView
        }

        Html.div [
            prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
            prop.children [
                match header with
                | Some renderHeader ->
                    Html.div [
                        prop.className "swt:flex-none"
                        prop.children [ renderHeader headerProps ]
                    ]
                | None -> Html.none
                Html.div [
                    prop.className "swt:flex-1 swt:overflow-y-auto swt:flex swt:flex-col swt:min-w-0"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:h-full"
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex-1 swt:overflow-x-hidden swt:overflow-y-auto"
                                    prop.children [ ArcFileEditor.TableView activeView arcFile setArcFile ]
                                ]
                                ArcFileEditor.AddRowsFooter arcFile activeView setArcFile
                                ArcFileEditor.Footer arcFile activeView setActiveView setArcFile
                            ]
                        ]
                    ]
                ]
            ]
        ]
