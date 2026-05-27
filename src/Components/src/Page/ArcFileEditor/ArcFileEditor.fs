namespace Swate.Components.Page.ArcFileEditor

open ARCtrl
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.Primitive
open Swate.Components.Primitive.Navbar
open Swate.Components.Composite.Widgets.Context
open Swate.Components.Composite.DataMapTable
open Swate.Components.Shared
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Composite.AnnotationTable
open Swate.Components.Composite.Widgets.DataAnnotator.Types

type private AddRowsFooterViewProps = {
    rowsToAdd: int
    minRowsToAdd: int
    onRowsToAddChange: int -> unit
    onAddRows: unit -> unit
    onAddRowsAndReset: unit -> unit
}

type private LazyComponents =

    [<ReactLazyComponent>]
    static member LazyDataMap(datamap: DataMap, setDatamap: DataMap -> unit) =
        DataMapTable.DataMapTable(datamap = datamap, setDatamap = setDatamap)

    [<ReactLazyComponent>]
    static member LazyBuildingBlockWidget
        (arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit)
        =
        Swate.Components.Composite.Widgets.BuildingBlockWidget.BuildingBlockWidget.Main(
            arcFile = arcFile,
            activeTableIndex = activeTableIndex,
            setArcFile = setArcFile
        )

    [<ReactLazyComponent>]
    static member LazyTemplateWidget(arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit) =
        Swate.Components.Composite.Widgets.TemplateWidget.TemplateWidget(
            arcFile = arcFile,
            activeTableIndex = activeTableIndex,
            setArcFile = setArcFile
        )

    [<ReactLazyComponent>]
    static member LazyFilePickerWidget
        (
            arcFile: ArcFiles,
            activeTableIndex: int option,
            setArcFile: ArcFiles -> unit,
            onPickPaths: unit -> Fable.Core.JS.Promise<string[]>
        ) =
        Swate.Components.Composite.Widgets.FilePickerWidget.Main(
            arcFile = arcFile,
            activeTableIndex = activeTableIndex,
            setArcFile = setArcFile,
            onPickPaths = onPickPaths
        )

    [<ReactLazyComponent>]
    static member LazyDataAnnotator(destination: AnnotationDestination, setAnnotationInput, onError) =
        Swate.Components.Composite.Widgets.DataAnnotator.DataAnnotator.Main(destination, setAnnotationInput, onError)

    [<ReactLazyComponent>]
    static member LazyArcFileMetadata(arcFile: ArcFiles, setArcFile: ArcFiles -> unit) =
        Swate.Components.Page.Metadata.ArcFileMetadata.ArcFileMetadata(arcFile = arcFile, setArcFile = setArcFile)

[<Erase; Mangle(false)>]
type Main =

    [<ReactComponent>]
    static member private LazyFallback(text: string) =
        Html.div [
            prop.className "swt:flex swt:items-center swt:justify-center swt:p-3 swt:text-sm swt:opacity-70"
            prop.text text
        ]

    static member LazyLoaderWithMessage(lazyComponent: ReactElement, message: string) =
        React.Suspense([ lazyComponent ], fallback = Main.LazyFallback(message))

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
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
                AnnotationTable.AnnotationTable(table, setTableInArcFile)
            ]
        ]

    [<ReactComponent>]
    static member private ArcFileContentView
        (activeView: ActiveView, arcFileState: ArcFiles, setArcFileState: ArcFiles -> unit)
        =
        match activeView with
        | ActiveView.Metadata ->
            Main.LazyLoaderWithMessage(
                LazyComponents.LazyArcFileMetadata(arcFileState, setArcFileState),
                "Loading metadata..."
            )
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
                    setArcFileState (ArcFiles.refreshRef arcFileState)

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
                    setArcFileState (ArcFiles.refreshRef arcFileState)

                Main.LazyLoaderWithMessage(
                    LazyComponents.LazyDataMap(assay.DataMap.Value, setDatamap),
                    "Loading DataMap..."
                )
            | ArcFiles.Study(study, assays) when study.DataMap.IsSome ->
                let setDatamap (nextDatamap: DataMap) =
                    study.DataMap <- Some nextDatamap
                    setArcFileState (ArcFiles.refreshRef (ArcFiles.Study(study, assays)))

                Main.LazyLoaderWithMessage(
                    LazyComponents.LazyDataMap(study.DataMap.Value, setDatamap),
                    "Loading DataMap..."
                )
            | ArcFiles.Run run when run.DataMap.IsSome ->
                let setDatamap (nextDatamap: DataMap) =
                    run.DataMap <- Some nextDatamap
                    setArcFileState (ArcFiles.refreshRef arcFileState)

                Main.LazyLoaderWithMessage(
                    LazyComponents.LazyDataMap(run.DataMap.Value, setDatamap),
                    "Loading DataMap..."
                )
            | ArcFiles.Workflow workflow when workflow.DataMap.IsSome ->
                let setDatamap (nextDatamap: DataMap) =
                    workflow.DataMap <- Some nextDatamap
                    setArcFileState (ArcFiles.refreshRef arcFileState)

                Main.LazyLoaderWithMessage(
                    LazyComponents.LazyDataMap(workflow.DataMap.Value, setDatamap),
                    "Loading DataMap..."
                )
            | ArcFiles.DataMap(parent, datamap) ->
                let setDatamap (nextDatamap: DataMap) =
                    setArcFileState (ArcFiles.DataMap(parent, nextDatamap))

                Main.LazyLoaderWithMessage(LazyComponents.LazyDataMap(datamap, setDatamap), "Loading DataMap...")
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
            |> Option.map (fun t -> t.ColumnCount > 0)
            |> Option.defaultValue false

        let canAddRows = tryGetAddRowsTarget () |> Option.isSome && hasColumns

        let addRowsWithCount rowCount =
            match tryGetAddRowsTarget () with
            | Some(AddRowsTarget.Table table) ->
                table.AddRowsEmpty rowCount
                setArcFileState (ArcFiles.refreshRef arcFileState)
            | Some(AddRowsTarget.DataMap dataMap) ->
                dataMap.DataContexts.AddRange(Array.init rowCount (fun _ -> DataContext()))
                setArcFileState (ArcFiles.refreshRef arcFileState)
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
            pickPaths: unit -> Fable.Core.JS.Promise<string[]>,
            ?trailingNavbarElements: ArcFileEditorHeaderProps -> ReactElement,
            ?startingActiveView: ActiveView,
            ?onError: string -> unit
        ) =

        let onError =
            defaultArg onError (fun errorMsg -> console.error ("Error in ArcFileEditor: " + errorMsg))

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

        let headerProps =
            React.useMemo (
                (fun () -> {
                    arcFile = arcFile
                    activeView = activeView
                }),
                [| box arcFile; box activeView |]
            )

        let activeTableIndex = activeView.TryTableIndex

        let trailingNavbarElement =
            React.useMemo (
                (fun () ->
                    match trailingNavbarElements with
                    | Some renderTrailingNavbarElements -> renderTrailingNavbarElements headerProps
                    | None -> Html.none
                ),
                [| box trailingNavbarElements; box headerProps |]
            )

        let navbar =
            React.useMemo (
                (fun () ->
                    Html.div [
                        prop.className "swt:shrink-0 swt:border-b swt:border-base-300"
                        prop.children [
                            Navbar.Main(
                                left = Swate.Components.Page.ArcFileEditor.Widgets.Main.WidgetToggleBtns(),
                                right = trailingNavbarElement
                            )
                        ]
                    ]
                ),
                [| box trailingNavbarElement |]
            )

        let widgetElements =
            React.useMemo (
                (fun () -> {|
                    buildingBlock =
                        Main.LazyLoaderWithMessage(
                            LazyComponents.LazyBuildingBlockWidget(arcFile, activeTableIndex, setArcFile),
                            "Loading Building Block Widget..."
                        )
                    template =
                        Main.LazyLoaderWithMessage(
                            LazyComponents.LazyTemplateWidget(arcFile, activeTableIndex, setArcFile),
                            "Loading Template Widget..."
                        )
                    filePicker =
                        Main.LazyLoaderWithMessage(
                            LazyComponents.LazyFilePickerWidget(arcFile, activeTableIndex, setArcFile, pickPaths),
                            "Loading File Picker Widget..."
                        )
                    dataAnnotator =
                        match Helper.tryGetDataAnnotatorDestination (activeView, arcFile) with
                        | Ok destination ->
                            Main.LazyLoaderWithMessage(
                                LazyComponents.LazyDataAnnotator(
                                    destination,
                                    Helper.applyDataAnnotatorInputToArcFile (destination, arcFile, setArcFile),
                                    onError = onError
                                ),
                                "Loading Data Annotator Widget..."
                            )
                        | Error message ->
                            Html.div [
                                prop.className "swt:p-3 swt:text-sm swt:opacity-70"
                                prop.text message
                            ]
                |}),
                [|
                    box arcFile
                    box activeView
                    box activeTableIndex
                    box setArcFile
                    pickPaths
                |]
            )

        /// ArcFiles type is too complex for react. Therefore we check hashcode instead and compare that.
        let arcFileHashCode = arcFile.GetHashCode()
        let activeViewHash = activeView.GetHashCode()

        let ArcFileContentViewMemo =
            React.useMemo (
                (fun () -> Main.ArcFileContentView(activeView, arcFile, setArcFile)),
                [| box activeViewHash; box arcFileHashCode |]
            )

        let AddRowsFooterMemo =
            React.useMemo (
                (fun () -> Main.AddRowsFooter(activeView, arcFile, setArcFile)),
                [| box activeViewHash; box arcFileHashCode |]
            )

        let content =
            React.useMemo (
                (fun () ->
                    Html.div [
                        prop.className "swt:grow swt:flex swt:flex-col swt:overflow-hidden"
                        prop.children [
                            navbar
                            Html.div [
                                prop.className "swt:grow swt:flex swt:flex-col swt:overflow-hidden"
                                prop.children [ ArcFileContentViewMemo ]
                            ]
                            AddRowsFooterMemo
                            ArcFileFooterTabs.Main(arcFile, activeView, setActiveView, setArcFile)
                        ]
                    ]
                ),
                [|
                    box navbar
                    box activeViewHash
                    box arcFileHashCode
                    box setArcFile
                    box setActiveView
                |]
            )

        AnnotationTableContextProvider.AnnotationTableContextProvider(
            Swate.Components.Page.ArcFileEditor.Widgets.Main.Widgets(
                content,
                widgetElements.buildingBlock,
                widgetElements.template,
                widgetElements.filePicker,
                widgetElements.dataAnnotator
            )
        )

    [<ReactComponent>]
    static member Entry(?debug: bool) =

        let startAssay =
            React.useMemo (fun _ ->
                let startAssay = ArcAssay.init ("Test")

                let fullerTable = ArcTable("Fuller Table")

                fullerTable.AddColumn(CompositeHeader.Input IOType.Source)
                fullerTable.AddColumn(CompositeHeader.Output IOType.Sample)
                fullerTable.AddColumn(CompositeHeader.ProtocolREF)
                fullerTable.AddColumn(CompositeHeader.ProtocolDescription)
                fullerTable.AddColumn(CompositeHeader.ProtocolType)
                fullerTable.AddColumn(CompositeHeader.ProtocolUri)
                fullerTable.AddColumn(CompositeHeader.ProtocolVersion)

                fullerTable.AddColumn(
                    CompositeHeader.Component(
                        OntologyAnnotation("Component Name", "Component Accession", "Component Source")
                    )
                )

                fullerTable.AddRowsEmpty 5

                for i in 0..2 do
                    if i = 0 then
                        startAssay.AddTable(fullerTable)
                    else
                        startAssay.AddTable(ArcTable.init (sprintf "Table %i" i))

                startAssay.DataMap <- Some(ARCtrl.DataMap.init ())

                startAssay
            )

        let (arcFile: ArcFiles), setArcFile = React.useState (ArcFiles.Assay(startAssay))

        let loadTemplates =
            fun () ->
                match debug with
                | Some true -> promise {
                    let STORY_TEMPLATE_NAME = "Story Import Template"

                    let createImportTemplate =
                        let template = Template.init (STORY_TEMPLATE_NAME)
                        template.Organisation <- Organisation.DataPLANT
                        template.Table.AddColumn(CompositeHeader.Input(IOType.Source))
                        template.Table.AddColumn(CompositeHeader.Output(IOType.Sample))

                        template.Table.AddColumn(
                            CompositeHeader.Component(
                                OntologyAnnotation("My Awesome component", tsr = "COMASS", tan = "COMASS:12345")
                            )
                        )

                        template.Table.AddRowsEmpty(1)
                        template.Version <- "1.0.0"
                        template

                    return Ok([| createImportTemplate |])
                  }
                | _ ->

                    promise {
                        let! json = Api.SwateApi.SwateTemplateApi.getTemplates () |> Async.StartAsPromise
                        return Ok(ARCtrl.Json.Templates.fromJsonString json)
                    }
                    |> Promise.catch (fun error ->
                        // Handle error, e.g., log it or show a notification
                        Error(sprintf "Error loading templates: %s" error.Message)
                    )

        let ColumnCountTestDisplay () =
            let firstTableColumnCount =
                arcFile.TryGetActiveTable(Some 0)
                |> Option.map (fun (_, t) -> string t.ColumnCount)
                |> Option.defaultValue "N/A"

            Html.div [
                prop.className "swt:p-4 swt:text-sm swt:opacity-70"
                prop.testId "arc-file-editor-column-count"
                prop.text ("Column count: " + firstTableColumnCount)
            ]

        let pickPathsMockFn () = promise {
            return [|
                yield "myImage.png"
                yield "README.md"
                for i in 0..40 -> (sprintf "Path %d" i)
            |]
        }

        Swate.Components.Composite.Template.TemplateCacheProvider.TemplateCacheProvider(
            loadTemplates,
            React.Fragment [
                ColumnCountTestDisplay()
                Main.ArcFileEditor(arcFile, setArcFile, pickPathsMockFn, startingActiveView = ActiveView.Table 0)
            ]
        )