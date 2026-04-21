namespace Swate.Components.ArcFileEditor

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.ArcFileEditor.Types

type private AddRowsFooterViewProps = {
    rowsToAdd: int
    minRowsToAdd: int
    onRowsToAddChange: int -> unit
    onAddRows: unit -> unit
    onAddRowsAndReset: unit -> unit
}

[<Erase; Mangle(false)>]
type Main =

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
    static member private ArcFileContentView
        (activeView: ActiveView, arcFileState: ArcFiles, setArcFileState: ArcFiles -> unit)
        =
        match activeView with
        | ActiveView.Metadata -> ArcFileMetadata.View(arcFileState, setArcFileState)
        | ActiveView.Table index ->
            let tables = arcFileState.Tables()

            let activeTable =
                if index < tables.Count && index >= 0 then
                    Some tables.[index]
                else
                    None

            match activeTable with
            | Some table ->
                let setTable (nextTable: ArcTable) =
                    tables.[index] <- nextTable
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                match table.ColumnCount with
                | 0 -> EmptyTableView.Main.EmptyTableView(arcFileState, setArcFileState, Some index)
                | _ -> AnnotationTable.AnnotationTable(table, setTable)
            | None ->
                Html.div [
                    prop.className "swt:p-4 swt:text-error"
                    prop.text "Table not found"
                ]
        | ActiveView.DataMap ->
            match arcFileState with
            | ArcFiles.Assay assay when assay.DataMap.IsSome ->
                let setDatamap (nextDatamap: DataMap) =
                    assay.DataMap <- Some nextDatamap
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                DataMapTable.DataMapTable(assay.DataMap.Value, setDatamap)
            | ArcFiles.Study(study, assays) when study.DataMap.IsSome ->
                let setDatamap (nextDatamap: DataMap) =
                    study.DataMap <- Some nextDatamap
                    setArcFileState (WidgetArcFile.refreshRef (ArcFiles.Study(study, assays)))

                DataMapTable.DataMapTable(study.DataMap.Value, setDatamap)
            | ArcFiles.Run run when run.DataMap.IsSome ->
                let setDatamap (nextDatamap: DataMap) =
                    run.DataMap <- Some nextDatamap
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                DataMapTable.DataMapTable(run.DataMap.Value, setDatamap)
            | ArcFiles.Workflow workflow when workflow.DataMap.IsSome ->
                let setDatamap (nextDatamap: DataMap) =
                    workflow.DataMap <- Some nextDatamap
                    setArcFileState (WidgetArcFile.refreshRef arcFileState)

                DataMapTable.DataMapTable(workflow.DataMap.Value, setDatamap)
            | ArcFiles.DataMap(parent, datamap) ->
                let setDatamap (nextDatamap: DataMap) =
                    setArcFileState (ArcFiles.DataMap(parent, nextDatamap))

                DataMapTable.DataMapTable(datamap, setDatamap)
            | _ ->
                Html.div [
                    prop.className "swt:p-4 swt:text-error"
                    prop.text "No DataMap available"
                ]

    [<ReactComponent>]
    static member private AddRowsFooter
        (activeView: ActiveView, arcFileState: ArcFiles, setArcFileState: ArcFiles -> unit)
        =
        let minRowsToAdd = 1
        let rowsToAdd, setRowsToAdd = React.useState minRowsToAdd

        let clampRowsToAdd rows = max minRowsToAdd rows

        let tryGetAddRowsTarget () =
            Helper.tryGetAddRowsTarget (activeView, arcFileState)

        let hasColumns =
            activeView.TryGetActiveTable(arcFileState)
            |> Option.map (fun t -> t.Columns.Count > 0)
            |> Option.defaultValue false

        let canAddRows = tryGetAddRowsTarget () |> Option.isSome && hasColumns

        let addRowsWithCount rowCount =
            match tryGetAddRowsTarget () with
            | Some(AddRowsTarget.Table table) ->
                table.AddRowsEmpty rowCount
                setArcFileState (WidgetArcFile.refreshRef arcFileState)
            | Some(AddRowsTarget.DataMap dataMap) ->
                dataMap.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                setArcFileState (WidgetArcFile.refreshRef arcFileState)
            | None -> ()

        let addRows () =
            rowsToAdd |> clampRowsToAdd |> addRowsWithCount

        let addRowsAndReset () =
            let rowCount = clampRowsToAdd rowsToAdd
            setRowsToAdd minRowsToAdd
            addRowsWithCount rowCount

        if canAddRows then
            Main.AddRowsFooterView {
                rowsToAdd = rowsToAdd
                minRowsToAdd = minRowsToAdd
                onRowsToAddChange = clampRowsToAdd >> setRowsToAdd
                onAddRows = addRows
                onAddRowsAndReset = addRowsAndReset
            }
        else
            Html.none

    [<ReactComponent>]
    static member ArcFileEditor
        (
            arcFile: ArcFiles,
            setArcFile: ArcFiles -> unit,
            ?trailingNavbarElements: ArcFileEditorHeaderProps -> ReactElement,
            ?startingActiveView: ActiveView
        ) =
        let activeView, setActiveView =
            React.useState (startingActiveView |> Option.defaultValue ActiveView.Metadata)

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

        let activeTableIndex = activeView.TryTableIndex

        let hasSelectedTable = arcFile.TryGetActiveTable(activeTableIndex) |> Option.isSome

        let navbar =
            Html.div [
                prop.className "swt:shrink-0 swt:border-b swt:border-base-300"
                prop.children [
                    Navbar.Main(
                        left = Swate.Components.ArcFileEditor.Widgets.Main.WidgetToggleBtns(),
                        right =
                            match trailingNavbarElements with
                            | Some renderTrailingNavbarElements -> renderTrailingNavbarElements headerProps
                            | None -> Html.none
                    )
                ]
            ]


        let buildingBlockWidget =
            BuildingBlockWidget.Main(arcFile, activeTableIndex, setArcFile)

        let templateWidget =
            Swate.Components.Widgets.TemplateWidget.TemplateWidget(arcFile, activeTableIndex, setArcFile)

        AnnotationTableContextProvider.AnnotationTableContextProvider(
            Swate.Components.ArcFileEditor.Widgets.Main.Widgets(
                Html.div [
                    prop.className "swt:grow swt:flex swt:flex-col swt:overflow-hidden"
                    prop.children [
                        navbar
                        Html.div [
                            prop.className "swt:grow swt:flex swt:flex-col swt:overflow-hidden"
                            prop.children [ Main.ArcFileContentView(activeView, arcFile, setArcFile) ]
                        ]
                        Main.AddRowsFooter(activeView, arcFile, setArcFile)
                        ArcFileEditor.ArcFileFooterTabs.Main(arcFile, activeView, setActiveView, setArcFile)
                    ]
                ],
                buildingBlockWidget,
                templateWidget
            )
        )

    [<ReactComponent>]
    static member Entry() =

        let startArcFile = ArcFiles.Assay(ArcAssay.init ("Test"))

        let fullerTable = ArcTable("Fuller Table")

        fullerTable.AddColumn(CompositeHeader.Input IOType.Source)
        fullerTable.AddColumn(CompositeHeader.Output IOType.Sample)
        fullerTable.AddColumn(CompositeHeader.ProtocolREF)
        fullerTable.AddColumn(CompositeHeader.ProtocolDescription)
        fullerTable.AddColumn(CompositeHeader.ProtocolType)
        fullerTable.AddColumn(CompositeHeader.ProtocolUri)
        fullerTable.AddColumn(CompositeHeader.ProtocolVersion)

        fullerTable.AddColumn(
            CompositeHeader.Component(OntologyAnnotation("Component Name", "Component Accession", "Component Source"))
        )

        fullerTable.AddRowsEmpty 5

        for i in 0..10 do
            if i = 0 then
                startArcFile.Tables().Add(fullerTable)
            else
                startArcFile.Tables().Add(ArcTable.init (sprintf "Table %i" i))

        let arcFile, setArcFile = React.useState (startArcFile)

        let loadTemplates =
            fun () ->
                promise {
                    let! json = Api.SwateApi.SwateTemplateApi.getTemplates () |> Async.StartAsPromise
                    return Ok(ARCtrl.Json.Templates.fromJsonString json)
                }
                |> Promise.catch (fun error ->
                    // Handle error, e.g., log it or show a notification
                    Error(sprintf "Error loading templates: %s" error.Message)
                )

        Template.TemplateCacheProvider.TemplateCacheProvider(
            loadTemplates,
            Main.ArcFileEditor(arcFile, setArcFile, startingActiveView = ActiveView.Table 0)
        )