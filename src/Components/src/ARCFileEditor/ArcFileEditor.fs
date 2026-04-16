namespace Swate.Components.ArcFileEditor

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Widgets.Context
open Swate.Components.Shared
open Swate.Components.ArcFileEditor.Types

module private EntryHelpers =

    let templateServices: TemplateWidgetServices = {
        loadTemplates = fun () -> async { return Ok [||] }
    }

module private WidgetNavbar =

    let widgetTypes = [
        WidgetType.BuildingBlock
        WidgetType.Template
        WidgetType.FilePicker
        WidgetType.DataAnnotator
    ]

    let private widgetInfo (widgetType: WidgetType) =
        match widgetType with
        | WidgetType.BuildingBlock -> "Add Building Block", Icons.BuildingBlock()
        | WidgetType.Template -> "Add Template", Icons.Templates()
        | WidgetType.FilePicker -> "File Picker", Icons.FilePicker()
        | WidgetType.DataAnnotator -> "Data Annotator", Icons.DataAnnotator()
        | WidgetType.Playground -> "Playground", Icons.Templates()

    [<ReactComponent>]
    let Buttons(isEnabled: bool) =
        let context = useWidgetController ()

        let controlButton (widgetType: WidgetType) =
            let isActive = context.isActive widgetType
            let label, icon = widgetInfo widgetType

            let tooltip =
                if not isEnabled then "Select a table to open widgets"
                elif isActive then $"Close {label}"
                else $"Open {label}"

            QuickAccessButton.QuickAccessButton(
                tooltip,
                icon,
                (fun _ -> context.toggleWidget widgetType),
                isDisabled = (not isEnabled),
                classes = (if isActive then "swt:!text-primary" else "")
            )

        Html.div [
            prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:justify-center"
            prop.children [
                for widgetType in widgetTypes do
                    controlButton widgetType
            ]
        ]

    let createWidgets
        (arcFileState: ArcFiles)
        (activeView: WidgetHostView)
        (activeTableIndex: int option)
        (setArcFileState: ArcFiles -> unit)
        (templateImportType: TableJoinOptions)
        (setTemplateImportType: TableJoinOptions -> unit)
        (templateServices: TemplateWidgetServices)
        (widgetServices: ArcFileEditorWidgetServices)
        : Map<WidgetType, WidgetDefinition> =
        [
            WidgetType.BuildingBlock,
            {|
                prefix = "ADD_BUILDINGBLOCK"
                content = BuildingBlockWidget.Main(arcFileState, activeTableIndex, setArcFileState)
            |}
            WidgetType.Template,
            {|
                prefix = "ADD_TEMPLATE"
                content =
                    TemplateWidget.Main(
                        arcFileState,
                        activeTableIndex,
                        setArcFileState,
                        templateImportType,
                        setTemplateImportType,
                        templateServices
                    )
            |}
            WidgetType.FilePicker,
            {|
                prefix = "FILEPICKER"
                content =
                    FilePickerWidget.Main(
                        arcFileState,
                        activeTableIndex,
                        setArcFileState,
                        widgetServices.filePickerServices
                    )
            |}
            WidgetType.DataAnnotator,
            {|
                prefix = "DATAANNOTATOR"
                content =
                    DataAnnotatorWidget.Main(
                        arcFileState,
                        activeView,
                        activeTableIndex,
                        setArcFileState,
                        widgetServices.dataAnnotatorServices
                    )
            |}
        ]
        |> Map.ofList

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
    static member private TableView(table: ArcTable, setTableInArcFile: ArcTable -> unit) =

        Html.div [
            prop.className "swt:w-full swt:min-w-0 swt:pb-4"
            prop.children [
                Swate.Components.AnnotationTable.AnnotationTable.AnnotationTable(table, setTableInArcFile)
            ]
        ]

    [<ReactComponent>]
    static member private ArcFileContentView
        (
            activeView: ActiveView,
            arcFileState: ArcFiles,
            setArcFileState: ArcFiles -> unit,
            templateServices: TemplateWidgetServices
        ) =
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
                | 0 -> EmptyTableView.Main.EmptyTableView(arcFileState, setArcFileState, Some index, templateServices)
                | _ -> Main.TableView(table, setTable)
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
            templateServices: TemplateWidgetServices,
            ?header: (ArcFileEditorHeaderProps -> ReactElement),
            ?widgetServices: ArcFileEditorWidgetServices
        ) =
        let activeView, setActiveView = React.useState ActiveView.Metadata

        let templateImportType, setTemplateImportType =
            React.useState TableJoinOptions.Headers

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
        let widgetHostView = activeView.ToWidgetHostView()

        let hasSelectedTable = arcFile.TryGetActiveTable(activeTableIndex) |> Option.isSome

        let navbar =
            match widgetServices, header with
            | Some _, _ ->
                Html.div [
                    prop.className "swt:flex-none"
                    prop.children [
                        Navbar.Main(
                            left = WidgetNavbar.Buttons(hasSelectedTable),
                            right =
                                match header with
                                | Some renderHeader -> renderHeader headerProps
                                | None -> Html.none
                        )
                    ]
                ]
            | None, Some renderHeader ->
                Html.div [
                    prop.className "swt:flex-none"
                    prop.children [ renderHeader headerProps ]
                ]
            | None, None -> Html.none

        let editorContent =
            Html.div [
                prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
                prop.children [
                    navbar
                    Html.div [
                        prop.className "swt:flex-1 swt:overflow-y-auto swt:flex swt:flex-col swt:min-w-0"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:flex-col swt:h-full"
                                prop.children [
                                    Html.div [
                                        prop.className "swt:flex-1 swt:overflow-x-hidden swt:overflow-y-auto"
                                        prop.children [
                                            Main.ArcFileContentView(activeView, arcFile, setArcFile, templateServices)
                                        ]
                                    ]
                                    Main.AddRowsFooter(activeView, arcFile, setArcFile)
                                    ArcFileEditor.ArcFileFooterTabs.Main(arcFile, activeView, setActiveView, setArcFile)
                                ]
                            ]
                        ]
                    ]
                ]
            ]

        match widgetServices with
        | Some widgetServices ->
            let widgets =
                WidgetNavbar.createWidgets
                    arcFile
                    widgetHostView
                    activeTableIndex
                    setArcFile
                    templateImportType
                    setTemplateImportType
                    templateServices
                    widgetServices

            Widget.WidgetController(widgets, closeAllWhen = not hasSelectedTable, children = [ editorContent ])
        | None -> editorContent

    [<ReactComponent>]
    static member Entry() =

        let startArcFile = ArcFiles.Assay(ArcAssay.init ("Test"))

        for i in 0..10 do
            startArcFile.Tables().Add(ArcTable.init (sprintf "Table %i" i))

        let arcFile, setArcFile = React.useState (startArcFile)
        Main.ArcFileEditor(arcFile, setArcFile, EntryHelpers.templateServices)