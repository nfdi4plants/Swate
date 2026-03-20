module Renderer.Components.WidgetRegistry

open System
open Feliz
open Swate.Components
open Swate.Components.FileExplorerTypes
open ARCtrl
open Swate.Electron.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper

let private asOptionalText (value: string option) =
    value
    |> Option.bind (fun text ->
        if String.IsNullOrWhiteSpace text then
            None
        else
            Some text)

let private summariseStrings (values: seq<string>) =
    values
    |> Seq.filter (fun value -> String.IsNullOrWhiteSpace value |> not)
    |> Seq.distinct
    |> Seq.toList
    |> function
        | [] -> None
        | [ single ] -> Some single
        | [ first; second ] -> Some $"{first}; {second}"
        | first :: second :: rest -> Some $"{first}; {second}; +{rest.Length} more"

let private personDisplayName (person: Person) =
    [ person.FirstName; person.MidInitials; person.LastName ]
    |> List.choose id
    |> List.filter (fun value -> String.IsNullOrWhiteSpace value |> not)
    |> function
        | [] -> person.ORCID |> asOptionalText |> Option.defaultValue "Unnamed person"
        | parts -> String.concat " " parts

let private ontologyLabel (annotation: OntologyAnnotation option) =
    annotation |> Option.map _.NameText |> asOptionalText

let private nodeKindLabel =
    function
    | ArcExplorerNodeKind.Arc -> "ARC"
    | ArcExplorerNodeKind.Group -> "Group"
    | ArcExplorerNodeKind.Study -> "Study"
    | ArcExplorerNodeKind.Assay -> "Assay"
    | ArcExplorerNodeKind.Workflow -> "Workflow"
    | ArcExplorerNodeKind.Run -> "Run"
    | ArcExplorerNodeKind.DataMap -> "DataMap"
    | ArcExplorerNodeKind.Note -> "Note"
    | ArcExplorerNodeKind.Sample -> "Sample"

let private nodeHasMetadata =
    function
    | ArcExplorerNodeKind.Arc
    | ArcExplorerNodeKind.Study
    | ArcExplorerNodeKind.Assay
    | ArcExplorerNodeKind.Workflow
    | ArcExplorerNodeKind.Run -> true
    | ArcExplorerNodeKind.Group
    | ArcExplorerNodeKind.DataMap
    | ArcExplorerNodeKind.Note
    | ArcExplorerNodeKind.Sample -> false

let private usesDetailedMetadataForm =
    function
    | ArcExplorerNodeKind.Study
    | ArcExplorerNodeKind.Assay
    | ArcExplorerNodeKind.Workflow
    | ArcExplorerNodeKind.Run -> true
    | _ -> false

let private arcFileMatchesMetadataNodeKind
    (selectedNodeKind: ArcExplorerNodeKind)
    (arcFile: ArcFiles)
    =
    match selectedNodeKind, arcFile with
    | ArcExplorerNodeKind.Arc, ArcFiles.Investigation _
    | ArcExplorerNodeKind.Study, ArcFiles.Study _
    | ArcExplorerNodeKind.Assay, ArcFiles.Assay _
    | ArcExplorerNodeKind.Workflow, ArcFiles.Workflow _
    | ArcExplorerNodeKind.Run, ArcFiles.Run _ -> true
    | _ -> false

let private textValue (value: string) =
    Html.span [
        prop.className "swt:text-sm swt:whitespace-pre-wrap swt:break-words"
        prop.text value
    ]

let private codeValue (value: string) =
    Html.code [
        prop.className "swt:text-xs swt:font-mono swt:break-all"
        prop.text value
    ]

let private textRow label value = label, textValue value

let private codeRow label value = label, codeValue value

let private optionalTextRow label value =
    value |> Option.map (fun text -> textRow label text)

let private dataMapSummaryRows (dataMap: DataMap) =
    let headers =
        seq {
            for index in 0 .. dataMap.ColumnCount - 1 do
                yield dataMap.GetHeader(index).ToString()
        }
        |> summariseStrings

    [
        textRow "Data Contexts" (string dataMap.DataContexts.Count)
        textRow "Columns" (string dataMap.ColumnCount)
        yield! headers |> Option.map (fun value -> textRow "Headers" value) |> Option.toList
    ]

let private metadataRows (arcFile: ArcFiles) =
    match arcFile with
    | ArcFiles.Investigation investigation ->
        [
            textRow "Identifier" investigation.Identifier
            yield! optionalTextRow "Title" (asOptionalText investigation.Title) |> Option.toList
            yield! optionalTextRow "Description" (asOptionalText investigation.Description) |> Option.toList
            yield! optionalTextRow "Submission Date" (asOptionalText investigation.SubmissionDate) |> Option.toList
            yield!
                optionalTextRow "Public Release" (asOptionalText investigation.PublicReleaseDate)
                |> Option.toList
            yield!
                summariseStrings (investigation.Contacts |> Seq.map personDisplayName)
                |> Option.map (fun value -> textRow "Contacts" value)
                |> Option.toList
            yield! Some(textRow "Publications" (string investigation.Publications.Count)) |> Option.toList
            yield!
                Some(textRow "Ontology Sources" (string investigation.OntologySourceReferences.Count))
                |> Option.toList
            yield! Some(textRow "Comments" (string investigation.Comments.Count)) |> Option.toList
        ]
    | ArcFiles.Study(study, _) ->
        [
            textRow "Identifier" study.Identifier
            yield! optionalTextRow "Title" (asOptionalText study.Title) |> Option.toList
            yield! optionalTextRow "Description" (asOptionalText study.Description) |> Option.toList
            yield! Some(textRow "Tables" (string study.TableCount)) |> Option.toList
            yield! optionalTextRow "Submission Date" (asOptionalText study.SubmissionDate) |> Option.toList
            yield! optionalTextRow "Public Release" (asOptionalText study.PublicReleaseDate) |> Option.toList
            yield!
                summariseStrings (study.StudyDesignDescriptors |> Seq.map _.NameText)
                |> Option.map (fun value -> textRow "Design" value)
                |> Option.toList
            yield!
                summariseStrings (study.Contacts |> Seq.map personDisplayName)
                |> Option.map (fun value -> textRow "Contacts" value)
                |> Option.toList
            yield! Some(textRow "Publications" (string study.Publications.Count)) |> Option.toList
            yield! Some(textRow "Comments" (string study.Comments.Count)) |> Option.toList
        ]
    | ArcFiles.Assay assay ->
        [
            textRow "Identifier" assay.Identifier
            yield! optionalTextRow "Title" (asOptionalText assay.Title) |> Option.toList
            yield! optionalTextRow "Description" (asOptionalText assay.Description) |> Option.toList
            yield! Some(textRow "Tables" (string assay.TableCount)) |> Option.toList
            yield! ontologyLabel assay.MeasurementType |> Option.map (fun value -> textRow "Measurement" value) |> Option.toList
            yield! ontologyLabel assay.TechnologyType |> Option.map (fun value -> textRow "Technology" value) |> Option.toList
            yield! ontologyLabel assay.TechnologyPlatform |> Option.map (fun value -> textRow "Platform" value) |> Option.toList
            yield!
                summariseStrings (assay.Performers |> Seq.map personDisplayName)
                |> Option.map (fun value -> textRow "Performers" value)
                |> Option.toList
            yield! Some(textRow "Comments" (string assay.Comments.Count)) |> Option.toList
        ]
    | ArcFiles.Workflow workflow ->
        [
            textRow "Identifier" workflow.Identifier
            yield! optionalTextRow "Title" (asOptionalText workflow.Title) |> Option.toList
            yield! optionalTextRow "Description" (asOptionalText workflow.Description) |> Option.toList
            yield! optionalTextRow "Version" (asOptionalText workflow.Version) |> Option.toList
            yield! ontologyLabel workflow.WorkflowType |> Option.map (fun value -> textRow "Type" value) |> Option.toList
            yield! optionalTextRow "URI" (asOptionalText workflow.URI) |> Option.toList
            yield!
                summariseStrings workflow.SubWorkflowIdentifiers
                |> Option.map (fun value -> textRow "Subworkflows" value)
                |> Option.toList
            yield!
                summariseStrings (workflow.Contacts |> Seq.map personDisplayName)
                |> Option.map (fun value -> textRow "Contacts" value)
                |> Option.toList
            yield! Some(textRow "Comments" (string workflow.Comments.Count)) |> Option.toList
        ]
    | ArcFiles.Run run ->
        [
            textRow "Identifier" run.Identifier
            yield! optionalTextRow "Title" (asOptionalText run.Title) |> Option.toList
            yield! optionalTextRow "Description" (asOptionalText run.Description) |> Option.toList
            yield! Some(textRow "Tables" (string run.TableCount)) |> Option.toList
            yield! ontologyLabel run.MeasurementType |> Option.map (fun value -> textRow "Measurement" value) |> Option.toList
            yield! ontologyLabel run.TechnologyType |> Option.map (fun value -> textRow "Technology" value) |> Option.toList
            yield! ontologyLabel run.TechnologyPlatform |> Option.map (fun value -> textRow "Platform" value) |> Option.toList
            yield!
                summariseStrings run.WorkflowIdentifiers
                |> Option.map (fun value -> textRow "Workflows" value)
                |> Option.toList
            yield!
                summariseStrings (run.Performers |> Seq.map personDisplayName)
                |> Option.map (fun value -> textRow "Performers" value)
                |> Option.toList
            yield! Some(textRow "Comments" (string run.Comments.Count)) |> Option.toList
        ]
    | ArcFiles.DataMap(_, dataMap) -> dataMapSummaryRows dataMap
    | ArcFiles.Template template ->
        [
            textRow "Name" template.Name
            yield! optionalTextRow "Description" (asOptionalText (Some template.Description)) |> Option.toList
            yield! optionalTextRow "Version" (asOptionalText (Some template.Version)) |> Option.toList
            yield!
                optionalTextRow "Last Updated" (Some(template.LastUpdated.ToString("yyyy-MM-dd HH:mm")))
                |> Option.toList
            yield! Some(textRow "Organisation" (template.Organisation.ToString())) |> Option.toList
            yield!
                summariseStrings (template.Tags |> Seq.map _.NameText)
                |> Option.map (fun value -> textRow "Tags" value)
                |> Option.toList
            yield!
                summariseStrings (template.Authors |> Seq.map personDisplayName)
                |> Option.map (fun value -> textRow "Authors" value)
                |> Option.toList
        ]

let private currentPreviewRowsForNode (selectedNode: ArcExplorerNode) (arcFile: ArcFiles) =
    match selectedNode.kind with
    | ArcExplorerNodeKind.DataMap -> WidgetArcFile.tryGetDataMap arcFile |> Option.map dataMapSummaryRows
    | _ when arcFile.Tables().Count > 0 ->
        let tableNames = arcFile.Tables() |> Seq.map _.Name |> summariseStrings

        Some [
            textRow "Tables" (string (arcFile.Tables().Count))
            yield! tableNames |> Option.map (fun value -> textRow "Names" value) |> Option.toList
        ]
    | _ -> WidgetArcFile.tryGetDataMap arcFile |> Option.map dataMapSummaryRows

let private noMetadataMessage =
    function
    | ArcExplorerNodeKind.Sample -> "Sample nodes are derived from table content and do not currently expose standalone metadata."
    | ArcExplorerNodeKind.Group -> "Select a concrete ARC object to inspect its metadata."
    | _ -> "No additional metadata is available for this selection."

let private filterArcExplorerTreeByKinds (visibleKinds: Set<string>) (nodes: ArcExplorerNode list) =
    let rec loop (node: ArcExplorerNode) =
        let filteredChildren = node.children |> List.choose loop
        let hasVisibleChildren = filteredChildren |> List.isEmpty |> not
        let kindLabel = nodeKindLabel node.kind
        let isVisibleKind = visibleKinds.Contains kindLabel

        match node.kind with
        | ArcExplorerNodeKind.Arc ->
            Some { node with children = filteredChildren }
        | ArcExplorerNodeKind.Group ->
            if hasVisibleChildren then
                Some { node with children = filteredChildren }
            else
                None
        | _ ->
            if isVisibleKind || hasVisibleChildren then
                Some { node with children = filteredChildren }
            else
                None

    nodes |> List.choose loop

let private flattenFileItems(items: FileItem list) =
    let rec loop (items: FileItem list) =
        items
        |> List.collect (fun item ->
            item :: (item.Children |> Option.defaultValue [] |> loop))

    loop items

let private flattenArcExplorerNodesWithParent(nodes: ArcExplorerNode list) =
    let rec loop (parent: ArcExplorerNode option) (nodes: ArcExplorerNode list) =
        nodes
        |> List.collect (fun node -> (node, parent) :: loop (Some node) node.children)

    loop None nodes

let private searchableArcExplorerItems (nodes: ArcExplorerNode list) (items: FileItem list) =
    let itemsById =
        items
        |> flattenFileItems
        |> List.map (fun item -> item.Id, item)
        |> Map.ofList

    nodes
    |> flattenArcExplorerNodesWithParent
    |> List.choose (fun (node, parent) ->
        if
            not node.isSelectable
            || node.kind = ArcExplorerNodeKind.Arc
            || node.kind = ArcExplorerNodeKind.Group
        then
            None
        else
            itemsById
            |> Map.tryFind node.id
            |> Option.map (fun item ->
                let parentPart =
                    parent
                    |> Option.map (fun parentNode -> $"Parent: {parentNode.name}")
                    |> Option.toList

                let subtitleParts = [
                    nodeKindLabel node.kind
                    if node.isReference then "Reference" else "Canonical"
                    yield! parentPart
                    yield! node.path |> Option.toList
                ]

                node.name, Some(String.concat " | " subtitleParts), item))
    |> List.sortBy (fun (name, _, _) -> name.ToLowerInvariant())
    |> List.toArray

[<ReactComponent>]
let private ARCObjectSection(title: string, children: ReactElement list) =
    Html.div [
        prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
        prop.children [
            Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-3"; prop.text title ]
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-3"
                prop.children children
            ]
        ]
    ]

[<ReactComponent>]
let private ARCObjectPropertyTable(rows: (string * ReactElement) list) =
    Html.dl [
        prop.className "swt:grid swt:grid-cols-1 swt:gap-y-3"
        prop.children [
            for label, value in rows do
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.dt [
                            prop.className "swt:text-xs swt:font-semibold swt:uppercase swt:tracking-wide swt:opacity-60"
                            prop.text label
                        ]
                        Html.dd [
                            prop.className "swt:min-w-0"
                            prop.children [ value ]
                        ]
                    ]
                ]
        ]
    ]

[<ReactComponent>]
let private ARCObjectSelectionSection(selectedNode: ArcExplorerNode) =
    let rows = [
        textRow "Kind" (nodeKindLabel selectedNode.kind)
        textRow "Role" (if selectedNode.isReference then "Reference" else "Canonical")
        if selectedNode.path.IsSome then
            codeRow "Path" selectedNode.path.Value
        else
            textRow "Path" "Virtual"
    ]

    ARCObjectSection(
        "Selection",
        [
            Html.h4 [ prop.className "swt:text-base swt:font-semibold swt:break-words"; prop.text selectedNode.name ]
            ARCObjectPropertyTable rows
        ]
    )

[<ReactComponent>]
let private ARCObjectStatusSection(title: string, message: string) =
    ARCObjectSection(
        title,
        [
            Html.p [
                prop.className "swt:text-sm swt:opacity-80"
                prop.text message
            ]
        ]
    )

[<ReactComponent>]
let private ARCObjectErrorSection(title: string, message: string) =
    ARCObjectSection(
        title,
        [
            Html.p [
                prop.className "swt:text-sm swt:text-error"
                prop.text message
            ]
        ]
    )

[<ReactComponent>]
let private ARCObjectNoteContentSection(content: string) =
    ARCObjectSection(
        "Note Content",
        [
            if String.IsNullOrWhiteSpace content then
                Html.p [
                    prop.className "swt:text-sm swt:opacity-80"
                    prop.text "This note is empty."
                ]
            else
                Html.pre [
                    prop.className
                        "swt:min-w-0 swt:max-w-none swt:overflow-auto swt:whitespace-pre-wrap swt:break-words swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-4 swt:text-sm"
                    prop.text content
                ]
        ]
    )

[<ReactComponent>]
let private ARCObjectDetailedMetadataContent
    (arcFile: ArcFiles)
    (setArcFileState: ArcFiles option -> unit)
    =
    let setArcFile arcFile = setArcFileState (Some arcFile)

    Html.div [
        prop.className "swt:min-w-0"
        prop.children [
            match arcFile with
            | ArcFiles.Study(study, assays) ->
                Renderer.MetadataForms.StudyMetadata(study, fun updated -> setArcFile (ArcFiles.Study(updated, assays)))
            | ArcFiles.Assay assay ->
                Renderer.MetadataForms.AssayMetadata(assay, fun updated -> setArcFile (ArcFiles.Assay updated))
            | ArcFiles.Workflow workflow ->
                Renderer.MetadataForms.WorkflowMetadata(workflow, fun updated -> setArcFile (ArcFiles.Workflow updated))
            | ArcFiles.Run run ->
                Renderer.MetadataForms.RunMetadata(run, fun updated -> setArcFile (ArcFiles.Run updated))
            | _ -> Html.none
        ]
    ]

[<ReactComponent>]
let private ARCObjectDetailsContent
    (selectedNode: ArcExplorerNode option)
    (pageState: PageState option)
    (arcFileState: ArcFiles option)
    (setArcFileState: ArcFiles option -> unit)
    =
    match selectedNode with
    | None ->
        Html.div [
            prop.className
                "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:rounded-lg swt:border swt:border-dashed swt:border-base-300 swt:bg-base-200/40 swt:p-6"
            prop.children [
                Html.p [
                    prop.className "swt:text-sm swt:text-center swt:opacity-70"
                    prop.text "Select an ARC object to inspect its details."
                ]
            ]
        ]
    | Some selectedNode ->
        let metadataArcFile =
            arcFileState
            |> Option.filter (arcFileMatchesMetadataNodeKind selectedNode.kind)

        let currentPreviewRows =
            arcFileState |> Option.bind (currentPreviewRowsForNode selectedNode)

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-3 swt:h-full"
            prop.children [
                ARCObjectSelectionSection(selectedNode)

                match selectedNode.kind with
                | ArcExplorerNodeKind.Note ->
                    match pageState with
                    | Some(PageState.Text content) -> ARCObjectNoteContentSection(content)
                    | Some(PageState.Error message) -> ARCObjectErrorSection("Note Content", message)
                    | _ -> ARCObjectStatusSection("Note Content", "Loading note content...")
                | kind when kind = ArcExplorerNodeKind.DataMap ->
                    match pageState with
                    | Some(PageState.Error message) -> ARCObjectErrorSection("Current Preview", message)
                    | _ ->
                        match currentPreviewRows with
                        | Some rows -> ARCObjectSection("Current Preview", [ ARCObjectPropertyTable rows ])
                        | None -> ARCObjectStatusSection("Current Preview", "No DataMap preview is loaded.")
                | kind when nodeHasMetadata kind ->
                    match pageState with
                    | Some(PageState.Error message) -> ARCObjectErrorSection("Metadata", message)
                    | _ ->
                        match metadataArcFile with
                        | Some arcFile when usesDetailedMetadataForm selectedNode.kind ->
                            ARCObjectDetailedMetadataContent arcFile setArcFileState
                        | Some arcFile ->
                            ARCObjectSection("Metadata", [ ARCObjectPropertyTable (metadataRows arcFile) ])
                        | None -> ARCObjectStatusSection("Metadata", "Loading metadata...")
                | _ ->
                    match currentPreviewRows with
                    | Some rows -> ARCObjectSection("Current Preview", [ ARCObjectPropertyTable rows ])
                    | None -> ARCObjectStatusSection("Current Preview", noMetadataMessage selectedNode.kind)
            ]
        ]

let private filePickerServices: FilePickerWidgetServices = {
    pickPaths =
        fun () ->
            promise {
                let! result = Api.ipcArcVaultApi.pickPaths (unbox null)
                return result |> Result.mapError (fun error -> error.Message)
            }
}

let private dataAnnotatorServices: DataAnnotatorWidgetServices = {
    pickPaths =
        fun () ->
            promise {
                let! result = Api.ipcArcVaultApi.pickPaths (unbox null)
                return result |> Result.mapError (fun error -> error.Message)
            }
    loadTextFile =
        fun path ->
            promise {
                let! result = Api.ipcArcVaultApi.openFile (unbox null) path

                return
                    match result with
                    | Error error -> Error error.Message
                    | Ok(PageState.Text content) -> Ok content
                    | Ok _ -> Error "Selected file could not be loaded as plain text. Only csv/tsv/txt are supported."
            }
}

let private templateServices: TemplateWidgetServices = {
    loadTemplates =
        fun () ->
            async {
                try
                    let! templatesJson = Api.templateApi.getTemplates()

                    let templates =
                        templatesJson
                        |> ARCtrl.Json.Templates.fromJsonString
                        |> Array.ofSeq

                    return Ok templates
                with error ->
                    return Error error.Message
            }
}

let BuildingBlockWidget
    (arcFileState: ArcFiles option)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.BuildingBlock,
    {|
        prefix = "ADD_BUILDINGBLOCK"
        content =
            Swate.Components.BuildingBlockWidget.Main(
                arcFileState,
                activeTableIndex,
                setArcFileState
            )
    |}

let TemplateWidget
    (arcFileState: ArcFiles option)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    (importType: TableJoinOptions)
    (setImportType: TableJoinOptions -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.Template,
    {|
        prefix = "ADD_TEMPLATE"
        content =
            Swate.Components.TemplateWidget.Main(
                arcFileState,
                activeTableIndex,
                setArcFileState,
                importType,
                setImportType,
                templateServices
            )
    |}

let FilePickerWidget
    (arcFileState: ArcFiles option)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.FilePicker,
        {|
            prefix = "FILEPICKER"
            content =
                Swate.Components.FilePickerWidget.Main(
                    arcFileState,
                    activeTableIndex,
                    setArcFileState,
                    filePickerServices
                )
        |}

let DataAnnotatorWidget
    (arcFileState: ArcFiles option)
    (activeView: WidgetHostView)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.DataAnnotator,
    {|
        prefix = "DATAANNOTATOR"
        content =
            Swate.Components.DataAnnotatorWidget.Main(
                arcFileState,
                activeView,
                activeTableIndex,
                setArcFileState,
                dataAnnotatorServices
            )
    |}

[<ReactComponent>]
let private ARCObjectWidgetContent
    (arcFileState: ArcFiles option)
    (pageState: PageState option)
    (setArcFileState: ArcFiles option -> unit)
    (setSelectedExplorerItemId: string option -> unit)
    (setSelectedTreeItemPath: string option -> unit)
    (setPageState: PageState option -> unit)
    =
    let appCtx = React.useContext Renderer.Context.AppStateCtx.AppStateCtx

    let workspaceCtx =
        React.useContext Renderer.Context.WorkspaceStateCtx.WorkspaceStateCtx

    let rootRepoPath =
        match appCtx.state with
        | AppState.ARC arcPath -> Some arcPath
        | AppState.Init -> None

    let selectedKindIndices, setSelectedKindIndices =
        React.useState (Swate.Components.ARCObjectWidget.DefaultKindFilterIndices())

    let visibleKinds =
        Swate.Components.ARCObjectWidget.SelectedKindLabels(selectedKindIndices)

    let filteredExplorerTree =
        filterArcExplorerTreeByKinds visibleKinds workspaceCtx.state.ArcExplorerTree

    let treePane =
        Renderer.Components.ArcExplorer.createArcExplorer
            rootRepoPath
            filteredExplorerTree
            workspaceCtx.state.SelectedExplorerItemId
            workspaceCtx.state.SelectedTreeItemPath
            setSelectedExplorerItemId
            setSelectedTreeItemPath
            setPageState

    let explorerItems =
        Renderer.Components.ArcExplorer.toFileItems filteredExplorerTree

    let searchItems =
        searchableArcExplorerItems filteredExplorerTree explorerItems

    let selectedItemId =
        Renderer.Components.ArcExplorer.getSelectedItemId
            filteredExplorerTree
            workspaceCtx.state.SelectedExplorerItemId
            workspaceCtx.state.SelectedTreeItemPath

    let selectedNode =
        selectedItemId
        |> Option.bind (fun nodeId ->
            Renderer.Components.ArcExplorer.tryFindNodeById nodeId filteredExplorerTree)

    let handleExplorerSelection =
        Renderer.Components.ArcExplorer.createOpenPreviewHandler
            setSelectedExplorerItemId
            setSelectedTreeItemPath
            setPageState

    let selectedTitle =
        selectedNode
        |> Option.map _.name
        |> Option.defaultValue "No visible selection"

    let selectedSubtitle =
        selectedNode
        |> Option.map (fun node ->
            let role = if node.isReference then "Reference" else "Canonical"
            $"{nodeKindLabel node.kind} | {role}")
        |> Option.defaultValue "Selection"

    let searchAction =
        Swate.Components.ARCObjectWidget.SearchAction(
            searchItems,
            (fun (name, _, _) -> name),
            (fun (_, _, item) -> handleExplorerSelection item),
            itemSubtitle = (fun (_, subtitle, _) -> subtitle)
        )

    let navbar =
        Swate.Components.ARCObjectWidget.Navbar(
            selectedTitle,
            selectedSubtitle,
            selectedKindIndices,
            setSelectedKindIndices,
            rightActions = searchAction
        )

    let explorerPane =
        Swate.Components.ARCObjectWidget.ExplorerContent(
            explorerItems,
            ?selectedItemId = selectedItemId,
            onItemClick = handleExplorerSelection
        )

    let detailsPane = ARCObjectDetailsContent selectedNode pageState arcFileState setArcFileState

    match treePane with
    | Some treePane ->
        Swate.Components.ARCObjectWidget.Main(
            navbar = navbar,
            treePane = treePane,
            explorerPane = explorerPane,
            detailsPane = detailsPane
        )
    | None ->
        Swate.Components.ARCObjectWidget.Main(
            navbar = navbar,
            explorerPane = explorerPane,
            detailsPane = detailsPane
        )

//let ARCObjectWidget
//    (arcFileState: ArcFiles option)
//    (pageState: PageState option)
//    (setArcFileState: ArcFiles option -> unit)
//    (setSelectedExplorerItemId: string option -> unit)
//    (setSelectedTreeItemPath: string option -> unit)
//    (setPageState: PageState option -> unit)
//    : WidgetType * WidgetDefinition =
//    WidgetType.ARCObject,
//    {|
//        prefix = "ARC_OBJECT"
//        content =
//            ARCObjectWidgetContent
//                arcFileState
//                pageState
//                setArcFileState
//                setSelectedExplorerItemId
//                setSelectedTreeItemPath
//                setPageState
//    |}

let createWidgets
    (arcFileState: ArcFiles option)
    (pageState: PageState option)
    (activeView: WidgetHostView)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    (importType: TableJoinOptions)
    (setImportType: TableJoinOptions -> unit)
    (setSelectedExplorerItemId: string option -> unit)
    (setSelectedTreeItemPath: string option -> unit)
    (setPageState: PageState option -> unit)
    : Map<WidgetType, WidgetDefinition> =
    [
        BuildingBlockWidget arcFileState activeTableIndex setArcFileState
        TemplateWidget arcFileState activeTableIndex setArcFileState importType setImportType
        FilePickerWidget arcFileState activeTableIndex setArcFileState
        DataAnnotatorWidget arcFileState activeView activeTableIndex setArcFileState
        //ARCObjectWidget arcFileState pageState setArcFileState setSelectedExplorerItemId setSelectedTreeItemPath setPageState
    ]
    |> Map.ofList

let private widgetRequiresTable =
    function
    | WidgetType.ARCObject -> false
    | _ -> true

[<ReactComponent>]
let NavbarButtons(widgetTypes: WidgetType list, hasSelectedTable: bool) =
    let context = WidgetContext.useWidgetController ()

    let widgetInfo (widgetType: WidgetType) =
        match widgetType with
        | WidgetType.BuildingBlock -> "Add Building Block", Icons.BuildingBlock()
        | WidgetType.Template -> "Add Template", Icons.Templates()
        | WidgetType.FilePicker -> "File Picker", Icons.FilePicker()
        | WidgetType.DataAnnotator -> "Data Annotator", Icons.DataAnnotator()
        | WidgetType.ARCObject -> "ARC Object", Icons.Docs()
        | WidgetType.Playground -> "Playground", Icons.Templates()

    let controlButton (widgetType: WidgetType) =
        let isActive = context.isActive widgetType
        let label, icon = widgetInfo widgetType
        let isDisabled = widgetRequiresTable widgetType && not hasSelectedTable
        let tooltip =
            if isDisabled then
                "Select a table to open widgets"
            elif isActive then
                $"Close {label}"
            else
                $"Open {label}"

        QuickAccessButton.QuickAccessButton(
            tooltip,
            icon,
            (fun _ -> context.toggleWidget widgetType),
            isDisabled = isDisabled,
            classes = (if isActive then "swt:!text-primary" else "")
        )

    Html.div [
        prop.className "swt:flex swt:flex-col swt:gap-3 swt:items-center swt:justify-center"
        prop.children [
            Html.div [
                prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:justify-center"
                prop.children [
                    for widgetType in widgetTypes do
                        controlButton widgetType
                ]
            ]
        ]
    ]

let widgetTypes = [
    WidgetType.BuildingBlock
    WidgetType.Template
    WidgetType.FilePicker
    WidgetType.DataAnnotator
    //WidgetType.ARCObject
]

[<ReactComponent>]
let NavbarButtonsForAllWidgets widgets children =

    Widget.WidgetController(widgets, children = children)
