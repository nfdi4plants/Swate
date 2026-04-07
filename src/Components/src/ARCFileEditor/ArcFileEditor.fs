namespace Swate.Components

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components.Shared


type ArcFileEditorHeaderProps = {
    arcFile: ArcFiles
    activeView: ActiveView
}

type private AddRowsTarget =
    | Table of ArcTable
    | DataMap of DataMap

type private AddRowsFooterViewProps = {
    rowsToAdd: int
    minRowsToAdd: int
    onRowsToAddChange: int -> unit
    onAddRows: unit -> unit
    onAddRowsAndReset: unit -> unit
}

[<Erase; Mangle(false)>]
type ArcFileEditor =

    static member private NewTablePrefix = "NewTable"

    static member private createNewTableName(tables: ResizeArray<ArcTable>) =
        let existingNames = tables |> Seq.map _.Name

        let rec loop index =
            let name = ArcFileEditor.NewTablePrefix + string index

            if Seq.contains name existingNames then
                loop (index + 1)
            else
                name

        loop 0

    static member private tryGetAddRowsTarget(activeView: ActiveView, arcFileState: ArcFiles) =
        match activeView with
        | ActiveView.Table tableIndex ->
            arcFileState.TryGetActiveTable(Some tableIndex)
            |> Option.map (snd >> AddRowsTarget.Table)
        | ActiveView.DataMap -> arcFileState.TryGetDataMap() |> Option.map AddRowsTarget.DataMap
        | ActiveView.Metadata -> None

    [<ReactComponent>]
    static member private AddRowsFooterView(props: AddRowsFooterViewProps) =
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
                            prop.min props.minRowsToAdd
                            prop.value props.rowsToAdd
                            prop.onChange props.onRowsToAddChange
                            prop.onKeyDown (key.enter, fun _ -> props.onAddRows ())
                            prop.style [ style.width 100 ]
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-outline swt:join-item"
                            prop.onClick (fun _ -> props.onAddRowsAndReset ())
                            prop.children [ Icons.Plus() ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private TableView(table: ArcTable, setTableInArcFile: ArcTable -> unit) =

        Html.div [
            prop.className "swt:w-screen swt:pb-4"
            prop.children [ AnnotationTable.AnnotationTable(table, setTableInArcFile) ]
        ]

    [<ReactComponent>]
    static member private ArcFileView(
        activeView: ActiveView,
        arcFileState: ArcFiles,
        setArcFileState: ArcFiles -> unit)
        =
        match activeView with
        | ActiveView.Metadata -> ArcFileMetadata.View(arcFileState, setArcFileState)
        | ActiveView.Table index ->
            let tables = arcFileState.Tables()

            if index < tables.Count then
                let setTable(nextTable: ArcTable) =
                    tables.[index] <- nextTable
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                ArcFileEditor.TableView(tables.[index], setTable)
            else
                Html.div [
                    prop.className "swt:p-4 swt:text-error"
                    prop.text "Table not found"
                ]
        | ActiveView.DataMap ->
            match arcFileState with
            | ArcFiles.Assay assay when assay.DataMap.IsSome ->
                let setDatamap(nextDatamap: DataMap) =
                    assay.DataMap <- Some nextDatamap
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                DataMapTable.DataMapTable(assay.DataMap.Value, setDatamap)
            | ArcFiles.Study(study, assays) when study.DataMap.IsSome ->
                let setDatamap(nextDatamap: DataMap) =
                    study.DataMap <- Some nextDatamap
                    setArcFileState (WidgetArcFile.refreshRef (ArcFiles.Study(study, assays)))

                DataMapTable.DataMapTable(study.DataMap.Value, setDatamap)
            | ArcFiles.Run run when run.DataMap.IsSome ->
                let setDatamap(nextDatamap: DataMap) =
                    run.DataMap <- Some nextDatamap
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                DataMapTable.DataMapTable(run.DataMap.Value, setDatamap)
            | ArcFiles.Workflow workflow when workflow.DataMap.IsSome ->
                let setDatamap(nextDatamap: DataMap) =
                    workflow.DataMap <- Some nextDatamap
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                DataMapTable.DataMapTable(workflow.DataMap.Value, setDatamap)
            | ArcFiles.DataMap(parent, datamap) ->
                let setDatamap(nextDatamap: DataMap) =
                    setArcFileState (ArcFiles.DataMap(parent, nextDatamap))

                DataMapTable.DataMapTable(datamap, setDatamap)
            | _ ->
                Html.div [
                    prop.className "swt:p-4 swt:text-error"
                    prop.text "No DataMap available"
                ]

    [<ReactComponent>]
    static member private AddRowsFooter(
        activeView: ActiveView,
        arcFileState: ArcFiles,
        setArcFileState: ArcFiles -> unit)
        =
        let minRowsToAdd = 1
        let rowsToAdd, setRowsToAdd = React.useState minRowsToAdd

        let clampRowsToAdd rows = max minRowsToAdd rows
        let tryGetAddRowsTarget() = ArcFileEditor.tryGetAddRowsTarget(activeView, arcFileState)
        let canAddRows = tryGetAddRowsTarget() |> Option.isSome

        let addRowsWithCount rowCount =
            match tryGetAddRowsTarget() with
            | Some(AddRowsTarget.Table table) ->
                table.AddRowsEmpty rowCount
                setArcFileState (WidgetArcFile.refreshRef arcFileState)
            | Some(AddRowsTarget.DataMap dataMap) ->
                dataMap.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                setArcFileState (WidgetArcFile.refreshRef arcFileState)
            | None -> ()

        let addRows() =
            rowsToAdd
            |> clampRowsToAdd
            |> addRowsWithCount

        let addRowsAndReset() =
            let rowCount = clampRowsToAdd rowsToAdd
            setRowsToAdd minRowsToAdd
            addRowsWithCount rowCount

        if canAddRows then
            ArcFileEditor.AddRowsFooterView {
                rowsToAdd = rowsToAdd
                minRowsToAdd = minRowsToAdd
                onRowsToAddChange = clampRowsToAdd >> setRowsToAdd
                onAddRows = addRows
                onAddRowsAndReset = addRowsAndReset
            }
        else
            Html.none

    [<ReactComponent>]
    static member private Footer(
        arcFile: ArcFiles,
        activeView: ActiveView,
        setActiveView: ActiveView -> unit,
        setArcFile: ArcFiles -> unit)
        =
        let tables = arcFile.Tables()
        let canAddTable = arcFile.CanCreateTables()

        let addNewTable() =
            if canAddTable then
                let nextName = ArcFileEditor.createNewTableName tables
                let nextTable = ArcTable.init nextName

                tables.Add nextTable
                setArcFile (WidgetArcFile.refreshRef arcFile)
                setActiveView (ActiveView.Table(tables.Count - 1))

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
                        if activeView = ActiveView.Metadata then
                            "swt:btn-primary swt:border-2 swt:!border-white"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView ActiveView.Metadata)
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
                            if activeView = ActiveView.Table index then
                                "swt:btn-primary swt:border-2 swt:!border-white"
                            else
                                "swt:btn-ghost"
                        ]
                        prop.onClick (fun _ -> setActiveView (ActiveView.Table index))
                        prop.children [
                            tabIcon "swt:fluent--table-24-regular"
                            Html.span [ prop.text table.Name ]
                        ]
                    ]
                if arcFile.CanRenderDataMapView() then
                    Html.button [
                        prop.className [
                            footerTabBaseClasses
                            if activeView = ActiveView.DataMap then
                                "swt:btn-primary swt:border-2 swt:!border-white"
                            else
                                "swt:btn-ghost"
                        ]
                        prop.onClick (fun _ -> setActiveView ActiveView.DataMap)
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
        let activeView, setActiveView = React.useState ActiveView.Metadata

        React.useEffect (
            (fun () ->
                let nextActiveView = ActiveView.Forward(arcFile, activeView)

                setActiveView nextActiveView
            ),
            [|
                box (arcFile.Tables().Count)
                box (arcFile.CanRenderDataMapView())
                box (arcFile.HasMetadata())
            |]
        )

        let headerProps = {
            arcFile = arcFile
            activeView = activeView
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
                                    prop.children [ ArcFileEditor.ArcFileView(activeView, arcFile, setArcFile) ]
                                ]
                                ArcFileEditor.AddRowsFooter(activeView, arcFile, setArcFile)
                                ArcFileEditor.Footer(arcFile, activeView, setActiveView, setArcFile)
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Entry() =

        let startArcFile = ArcFiles.Assay(ArcAssay.init("Test"))

        let arcFile, setArcFile = React.useState(startArcFile)
        ArcFileEditor.Main(arcFile, setArcFile)
