namespace Renderer.components.Widgets

open Feliz
open Fable.Core
open ARCtrl
open ARCtrl.Json


[<RequireQualifiedAccess>]
type TemplateImportMode =
    | AppendToActiveTable
    | CreateNewTable
    | Skip

type TemplateDataSource =

    static member fetchTemplates
        (setIsLoading: bool -> unit)
        (setTemplates: Template[] -> unit)
        (setStatus: StatusMessage option -> unit)
        =
        promise {
            setIsLoading true

            try
                let! templatesJson =
                    Api.templateApi.getTemplates ()
                    |> Async.StartAsPromise

                let templates =
                    templatesJson
                    |> Templates.fromJsonString
                    |> Seq.toArray
                    |> Array.sortBy _.Name

                setTemplates templates
                setStatus None
            with exn ->
                setStatus (
                    Some {
                        Kind = StatusKind.Error
                        Text = $"Failed to load templates: {exn.Message}"
                    }
                )

            setIsLoading false
        }
        |> Promise.start

    static member ApplyTemplates
        (activeTableData: ActiveTableData)
        (selectedTemplates: Template[])
        (importType: TableJoinOptions)
        (templateImportModes: Map<System.Guid, TemplateImportMode>)
        (deselectedTemplateColumns: Map<System.Guid, Set<int>>)
        : Result<ArcFiles, string>
        =
        try
            let getTemplateImportMode (templateId: System.Guid) =
                templateImportModes
                |> Map.tryFind templateId
                |> Option.defaultValue TemplateImportMode.AppendToActiveTable

            let deselectedColumns =
                selectedTemplates
                |> Array.mapi (fun tableIndex template ->
                    deselectedTemplateColumns
                    |> Map.tryFind template.Id
                    |> Option.defaultValue Set.empty
                    |> Set.toSeq
                    |> Seq.map (fun columnIndex -> (tableIndex, columnIndex))
                )
                |> Seq.concat
                |> Set.ofSeq

            let importTables: global.Types.FileImport.ImportTable list =
                selectedTemplates
                |> Array.mapi (fun index template ->
                    match getTemplateImportMode template.Id with
                    | TemplateImportMode.Skip -> None
                    | TemplateImportMode.AppendToActiveTable ->
                        let importTable: global.Types.FileImport.ImportTable = {
                            Index = index
                            FullImport = false
                        }

                        Some importTable
                    | TemplateImportMode.CreateNewTable ->
                        let importTable: global.Types.FileImport.ImportTable = {
                            Index = index
                            FullImport = true
                        }

                        Some importTable
                )
                |> Array.choose id
                |> Array.toList

            if importTables.IsEmpty then
                Error "No templates selected for import. Set at least one template to append or create new."
            else
                let importConfig =
                    {
                        global.Types.FileImport.SelectiveImportConfig.init () with
                            ImportType = importType
                            ImportTables = importTables
                            DeselectedColumns = deselectedColumns
                    }

                let tables =
                    selectedTemplates
                    |> Array.map _.Table
                    |> ResizeArray

                let updatedArcFile =
                    Update.UpdateUtil.JsonImportHelper.updateTables
                        tables
                        importConfig
                        (Some activeTableData.TableIndex)
                        (Some activeTableData.ArcFile)

                Ok updatedArcFile
        with exn ->
            Error exn.Message

type TemplateWidget =

    static member PreviewColumnValues (column: CompositeColumn) =
        try
            if isNull (box column) || isNull (box column.Cells) then
                [||]
            else
                column.Cells
                |> Seq.map (fun cell -> (string cell).Trim())
                |> Seq.filter (fun value -> not (System.String.IsNullOrWhiteSpace value))
                |> Seq.distinct
                |> Seq.truncate 3
                |> Array.ofSeq
        with _ ->
            [||]

    [<ReactComponent>]
    static member TemplateTable
        (
            templates: Template[],
            selectedTemplateIds: Set<System.Guid>,
            toggleSelectedTemplate: System.Guid -> unit,
            isLoading: bool,
            onRefresh: unit -> unit
        ) =
        let expandedTemplateIds, setExpandedTemplateIds = React.useStateWithUpdater Set.empty<System.Guid>

        let toggleExpandedTemplate (templateId: System.Guid) =
            setExpandedTemplateIds (fun current ->
                if Set.contains templateId current then
                    Set.remove templateId current
                else
                    Set.add templateId current
            )

        let authorString (template: Template) =
            template.Authors
            |> Seq.map (fun author ->
                [ author.FirstName; author.LastName; author.MidInitials ]
                |> List.choose id
                |> String.concat " "
            )
            |> String.concat ", "

        let columnCount = 4

        Html.div [
            prop.className "swt:max-h-72 swt:overflow-y-auto swt:border swt:rounded-md"
            prop.children [
                Html.table [
                    prop.className "swt:table swt:table-pin-cols swt:table-fixed swt:w-full"
                    prop.children [
                        Html.thead [
                            Html.tr [
                                Html.th [
                                    Html.div [
                                        prop.className "swt:flex swt:items-center"
                                        prop.children [
                                            Swate.Components.Icons.Filter("swt:size-3")
                                            Html.div [
                                                prop.className "swt:w-12"
                                                prop.text templates.Length
                                            ]
                                        ]
                                    ]
                                ]
                                Html.th [ prop.className "swt:w-1/2"; prop.text "Template Name" ]
                                Html.th [ prop.className "swt:w-2/5"; prop.text "Details" ]
                                Html.th [
                                    prop.className "swt:w-14"
                                    prop.children [
                                        Html.button [
                                            prop.type'.button
                                            prop.className "swt:btn swt:btn-square swt:btn-sm"
                                            prop.onClick (fun _ -> onRefresh ())
                                            prop.children [ Swate.Components.Icons.ArrowsRotate() ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.tbody [
                            if isLoading then
                                Html.tr [
                                    Html.td [
                                        prop.colSpan columnCount
                                        prop.className "swt:text-center"
                                        prop.children [ Swate.Components.Icons.SpinningSpinner() ]
                                    ]
                                ]
                            elif templates.Length = 0 then
                                Html.tr [
                                    Html.td [
                                        prop.colSpan columnCount
                                        prop.className "swt:text-center swt:opacity-70"
                                        prop.text "No templates match current filters."
                                    ]
                                ]
                            else
                                for template in templates do
                                    let isSelected = Set.contains template.Id selectedTemplateIds
                                    let isExpanded = Set.contains template.Id expandedTemplateIds
                                    let authors = authorString template

                                    React.KeyedFragment(
                                        string template.Id,
                                        [
                                            Html.tr [
                                                prop.className [
                                                    if isSelected then
                                                        "swt:bg-primary/10"
                                                    if isExpanded then
                                                        "swt:!border-transparent"
                                                ]
                                                prop.children [
                                                    Html.td [
                                                        prop.className "swt:cursor-pointer"
                                                        prop.onClick (fun _ -> toggleSelectedTemplate template.Id)
                                                        prop.children [
                                                            Html.input [
                                                                prop.type'.checkbox
                                                                prop.readOnly true
                                                                prop.isChecked isSelected
                                                                prop.className "swt:checkbox"
                                                            ]
                                                        ]
                                                    ]
                                                    Html.td [
                                                        prop.className "swt:cursor-pointer"
                                                        prop.onClick (fun _ -> toggleSelectedTemplate template.Id)
                                                        prop.children [
                                                            Html.div [
                                                                prop.className "swt:line-clamp-2"
                                                                prop.title template.Name
                                                                prop.text template.Name
                                                            ]
                                                        ]
                                                    ]
                                                    Html.td [
                                                        prop.className "swt:cursor-pointer"
                                                        prop.onClick (fun _ -> toggleSelectedTemplate template.Id)
                                                        prop.children [
                                                            Html.div [
                                                                prop.className "swt:flex swt:flex-col swt:text-xs swt:opacity-70"
                                                                prop.children [
                                                                    Html.span (template.Organisation.ToString())
                                                                    Html.span $"v{template.Version}"
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                    Html.td [
                                                        prop.className "swt:w-14 swt:p-0"
                                                        prop.onClick (fun e ->
                                                            e.preventDefault ()
                                                            e.stopPropagation ()
                                                            toggleExpandedTemplate template.Id
                                                        )
                                                        prop.children [
                                                            Html.button [
                                                                prop.type'.button
                                                                prop.className
                                                                    "swt:btn swt:btn-ghost swt:btn-sm swt:w-full swt:h-full swt:min-h-10 swt:rounded-none"
                                                                prop.ariaLabel (
                                                                    if isExpanded then
                                                                        "Collapse template details"
                                                                    else
                                                                        "Expand template details"
                                                                )
                                                                prop.onClick (fun e ->
                                                                    e.preventDefault ()
                                                                    e.stopPropagation ()
                                                                    toggleExpandedTemplate template.Id
                                                                )
                                                                prop.children [
                                                                    if isExpanded then
                                                                        Swate.Components.Icons.Close()
                                                                    else
                                                                        Swate.Components.Icons.ChevronDown()
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                            if isExpanded then
                                                Html.tr [
                                                    prop.className [
                                                        if isSelected then
                                                            "swt:bg-primary/10"
                                                    ]
                                                    prop.children [
                                                        Html.td [
                                                            prop.className "swt:pt-0"
                                                            prop.colSpan columnCount
                                                            prop.children [
                                                                Html.div [
                                                                    prop.className "swt:py-2 swt:text-xs swt:break-words"
                                                                    prop.text (
                                                                        if System.String.IsNullOrWhiteSpace template.Description then
                                                                            "No description available."
                                                                        else
                                                                            template.Description
                                                                    )
                                                                ]
                                                                Html.div [
                                                                    prop.className "swt:flex swt:flex-wrap swt:gap-1 swt:pb-2"
                                                                    prop.children [
                                                                        if not (System.String.IsNullOrWhiteSpace authors) then
                                                                            Html.span [
                                                                                prop.className
                                                                                    "swt:badge swt:badge-sm swt:badge-neutral swt:max-w-full swt:overflow-hidden swt:text-ellipsis"
                                                                                prop.text $"Authors: {authors}"
                                                                            ]
                                                                        for repository in template.EndpointRepositories do
                                                                            Html.span [
                                                                                prop.className
                                                                                    "swt:badge swt:badge-sm swt:badge-accent swt:max-w-full swt:overflow-hidden swt:text-ellipsis"
                                                                                prop.text repository.NameText
                                                                            ]
                                                                        for tag in template.Tags do
                                                                            Html.span [
                                                                                prop.className
                                                                                    "swt:badge swt:badge-sm swt:badge-secondary swt:max-w-full swt:overflow-hidden swt:text-ellipsis"
                                                                                prop.text tag.NameText
                                                                            ]
                                                                    ]
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                        ]
                                    )
                        ]
                    ]
                ]
            ]
        ]

    static member SelectedColumnPreview ((columns: CompositeColumn []), (isColumnSelected: System.Guid -> int -> bool) , toggleColumnSelected, (template: Template), (importMode: TemplateImportMode)) =
        Html.div [
            prop.className "swt:collapse-content swt:overflow-x-auto swt:pt-1"
            prop.children [
                if columns.Length = 0 then
                    Html.div [
                        prop.className "swt:text-sm swt:opacity-70"
                        prop.text "No columns available."
                    ]
                else
                    Html.div [
                        prop.className "swt:flex swt:flex-row swt:gap-2 swt:overflow-x-auto swt:pb-1"
                        prop.children [
                            for columnIndex in 0 .. columns.Length - 1 do
                                let isSelected = isColumnSelected template.Id columnIndex
                                let previewValues = TemplateWidget.PreviewColumnValues columns.[columnIndex]

                                Html.label [
                                    prop.key $"{template.Id}-column-{columnIndex}"
                                    prop.className [
                                        "swt:flex swt:flex-col swt:items-start swt:cursor-pointer swt:gap-1 swt:min-h-16 swt:min-w-56 swt:max-w-64 swt:border swt:rounded-md swt:px-2 swt:py-2"
                                        if not isSelected then
                                            "swt:opacity-70"
                                    ]
                                    prop.children [
                                        Html.div [
                                            prop.className "swt:flex swt:items-center swt:gap-2 swt:w-full"
                                            prop.children [
                                                Html.input [
                                                    prop.type'.checkbox
                                                    prop.className "swt:checkbox swt:checkbox-sm"
                                                    prop.isChecked isSelected
                                                    if importMode = TemplateImportMode.Skip then
                                                        prop.disabled true
                                                    prop.onChange (fun (_: Browser.Types.Event) -> toggleColumnSelected template.Id columnIndex)
                                                ]
                                                Html.span [
                                                    prop.className "swt:text-sm swt:font-medium swt:line-clamp-2"
                                                    prop.title (columns.[columnIndex].Header.ToString())
                                                    prop.text (columns.[columnIndex].Header.ToString())
                                                ]
                                            ]
                                        ]
                                        Html.div [
                                            prop.className "swt:flex swt:flex-wrap swt:gap-1 swt:pl-6 swt:w-full"
                                            prop.children [
                                                if previewValues.Length = 0 then
                                                    Html.span [
                                                        prop.className "swt:text-xs swt:opacity-60"
                                                        prop.text "No values"
                                                    ]
                                                else
                                                    for previewValue in previewValues do
                                                        Html.span [
                                                            prop.key $"{template.Id}-column-{columnIndex}-value-{previewValue}"
                                                            prop.className
                                                                "swt:badge swt:badge-xs swt:badge-outline swt:max-w-44 swt:overflow-hidden swt:text-ellipsis"
                                                            prop.title previewValue
                                                            prop.text previewValue
                                                        ]
                                            ]
                                        ]
                                    ]
                                ]
                        ]
                    ]
            ]
        ]

    static member DropdownContent(isRowExtended: bool, setIsRowExtended, content: ReactElement) =
        Html.div [
            prop.className "swt:collapse swt:border swt:rounded-md"
            prop.children [
                Html.input [
                    prop.type'.checkbox
                    prop.className "swt:min-h-0 swt:h-5"
                    prop.isChecked isRowExtended
                    prop.onChange (fun (_: Browser.Types.Event) -> setIsRowExtended (not isRowExtended))
                ]
                Html.div [
                    prop.className
                        "swt:collapse-title swt:font-bold swt:text-sm swt:min-h-0 swt:h-10 swt:flex swt:items-center swt:p-3"
                    prop.children [
                        Html.div [
                            prop.text (
                                if isRowExtended then
                                    "Preview Selected Columns"
                                else
                                    "Preview Table"
                            )
                        ]
                        Swate.Components.Icons.MagnifyingClass()
                    ]
                ]
                content
            ]
        ]

    [<ReactComponent>]
    static member Main (activeTableData: ActiveTableData option, onTableMutated: unit -> unit) =
        let templates, setTemplates = React.useState [||]
        let selectedTemplateIds, setSelectedTemplateIds = React.useStateWithUpdater Set.empty<System.Guid>
        let templateImportModes, setTemplateImportModes = React.useStateWithUpdater Map.empty<System.Guid, TemplateImportMode>
        let deselectedTemplateColumns, setDeselectedTemplateColumns =
            React.useStateWithUpdater Map.empty<System.Guid, Set<int>>
        let importType, setImportType = React.useState TableJoinOptions.Headers
        let isImportDialogOpen, setIsImportDialogOpen = React.useState false
        let isLoading, setIsLoading = React.useState false
        let isRowExtended, setIsRowExtended = React.useState false
        let status, setStatus = React.useState (None: StatusMessage option)

        let refreshTemplates () =
            TemplateDataSource.fetchTemplates setIsLoading setTemplates setStatus

        React.useEffectOnce (fun _ -> refreshTemplates ())

        let toggleSelectedTemplate (templateId: System.Guid) =
            setSelectedTemplateIds (fun current ->
                if Set.contains templateId current then
                    Set.remove templateId current
                else
                    Set.add templateId current
            )

        React.useEffect (
            (fun () ->
                setTemplateImportModes (fun current ->
                    current
                    |> Map.filter (fun templateId _ -> Set.contains templateId selectedTemplateIds)
                )

                setDeselectedTemplateColumns (fun current ->
                    current
                    |> Map.filter (fun templateId _ -> Set.contains templateId selectedTemplateIds)
                )
            ),
            [| box selectedTemplateIds |]
        )

        let selectedTemplates =
            templates
            |> Array.filter (fun template -> Set.contains template.Id selectedTemplateIds)

        let getTemplateImportMode (templateId: System.Guid) =
            templateImportModes
            |> Map.tryFind templateId
            |> Option.defaultValue TemplateImportMode.AppendToActiveTable

        let setTemplateImportMode (templateId: System.Guid) (mode: TemplateImportMode) =
            setTemplateImportModes (fun current -> current.Add(templateId, mode))

        let isColumnSelected (templateId: System.Guid) (columnIndex: int) =
            deselectedTemplateColumns
            |> Map.tryFind templateId
            |> Option.map (fun deselected -> deselected |> Set.contains columnIndex |> not)
            |> Option.defaultValue true

        let toggleColumnSelected (templateId: System.Guid) (columnIndex: int) =
            setDeselectedTemplateColumns (fun current ->
                let deselectedColumnsForTemplate =
                    current
                    |> Map.tryFind templateId
                    |> Option.defaultValue Set.empty

                let nextDeselectedColumns =
                    if Set.contains columnIndex deselectedColumnsForTemplate then
                        Set.remove columnIndex deselectedColumnsForTemplate
                    else
                        Set.add columnIndex deselectedColumnsForTemplate

                if nextDeselectedColumns.IsEmpty then
                    current.Remove templateId
                else
                    current.Add(templateId, nextDeselectedColumns)
            )

        let templatesToImportCount =
            selectedTemplates
            |> Array.sumBy (fun template ->
                match getTemplateImportMode template.Id with
                | TemplateImportMode.Skip -> 0
                | _ -> 1
            )

        let canOpenImportDialog = selectedTemplates.Length > 0 && not isLoading
        let canSubmitImport = activeTableData.IsSome && templatesToImportCount > 0

        let importTypeRadio (option: TableJoinOptions) (label: string) =
            let isSelected = importType = option

            Html.label [
                prop.className "swt:flex swt:items-center swt:cursor-pointer swt:gap-2 swt:min-h-8"
                prop.children [
                    Html.input [
                        prop.type'.radio
                        prop.className "swt:radio swt:radio-sm"
                        prop.name "template-import-detail"
                        prop.isChecked isSelected
                        prop.onChange (fun (_: Browser.Types.Event) -> setImportType option)
                    ]
                    Html.span [
                        prop.className "swt:text-sm"
                        prop.text label
                    ]
                ]
            ]

        let tryImportTemplates () =
            match activeTableData with
            | None ->
                setStatus (
                    Some {
                        Kind = StatusKind.Error
                        Text = "Open a table before importing templates."
                    }
                )
            | Some tableData ->
                if templatesToImportCount = 0 then
                    setStatus (
                        Some {
                            Kind = StatusKind.Error
                            Text = "No templates selected for import. Set at least one template to append or create new."
                        }
                    )
                else
                    match
                        TemplateDataSource.ApplyTemplates
                            tableData
                            selectedTemplates
                            importType
                            templateImportModes
                            deselectedTemplateColumns
                    with
                    | Error importError ->
                        setStatus (
                            Some {
                                Kind = StatusKind.Error
                                Text = $"Template import failed: {importError}"
                            }
                        )
                    | Ok updatedArcFile ->
                        setIsImportDialogOpen false
                        onTableMutated ()
                        promise {
                            let! syncResult = Renderer.ArcFilePersistence.saveArcFile updatedArcFile

                            match syncResult with
                            | Ok() ->
                                setStatus (
                                    Some {
                                        Kind = StatusKind.Info
                                        Text = $"Imported {selectedTemplates.Length} template(s) into \"{tableData.TableName}\"."
                                    }
                                )
                            | Error syncError ->
                                setStatus (
                                    Some {
                                        Kind = StatusKind.Error
                                        Text = $"Templates imported locally, but save failed: {syncError}"
                                    }
                                )
                        }
                        |> Promise.start

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-3 swt:p-2"
            prop.children [
                if activeTableData.IsNone then
                    Html.div [
                        prop.className "swt:text-sm swt:opacity-70"
                        prop.text "No active table. Open a table to import templates."
                    ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.type'.button
                            prop.className [
                                "swt:btn swt:w-full"
                                if canOpenImportDialog then "swt:btn-primary" else "swt:btn-error"
                            ]
                            if not canOpenImportDialog then
                                prop.disabled true
                            prop.onClick (fun _ -> setIsImportDialogOpen true)
                            prop.text "Import Templates"
                        ]
                    ]
                ]

                Swate.Components.TemplateFilter.TemplateFilterProvider(
                    React.Fragment [
                        Swate.Components.TemplateFilter.TemplateFilter(
                            templates,
                            templateSearchClassName = "swt:grow"
                        )
                        Swate.Components.TemplateFilter.FilteredTemplateRenderer(fun filteredTemplates ->
                            TemplateWidget.TemplateTable(filteredTemplates, selectedTemplateIds, toggleSelectedTemplate, isLoading, refreshTemplates))
                    ]
                )

                Swate.Components.BaseModal.Modal(
                    isOpen = isImportDialogOpen,
                    setIsOpen = setIsImportDialogOpen,
                    header = Html.text "Import Templates",
                    description = Html.text "Configure import detail level and destination per selected template.",
                    children =
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-4"
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:flex-col swt:gap-2"
                                    prop.children [
                                        Html.div [
                                            prop.className "swt:border swt:rounded-md swt:p-3 swt:w-full swt:min-h-28 swt:flex swt:flex-col swt:gap-2"
                                            prop.children [
                                                Html.h3 [
                                                    prop.className "swt:font-semibold swt:gap-2 swt:flex swt:flex-row swt:items-center"
                                                    prop.children [
                                                        Swate.Components.Icons.Cog()
                                                        Html.span "Import Type"
                                                    ]
                                                ]
                                                importTypeRadio TableJoinOptions.Headers "Column Headers"
                                                importTypeRadio TableJoinOptions.WithUnit "With Units"
                                                importTypeRadio TableJoinOptions.WithValues "With Values"
                                            ]
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "swt:flex swt:flex-col swt:gap-2"
                                    prop.children [
                                        if selectedTemplates.Length = 0 then
                                            Html.div [
                                                prop.className "swt:text-sm swt:opacity-70"
                                                prop.text "No templates selected."
                                            ]
                                        else
                                            for template in selectedTemplates do
                                                let importMode = getTemplateImportMode template.Id
                                                let radioName = $"template-mode-{template.Id}"
                                                let columns = template.Table.Columns |> Seq.toArray

                                                let modeRadio (mode: TemplateImportMode) (label: string) =
                                                    let isSelected = importMode = mode

                                                    Html.label [
                                                        prop.className "swt:flex swt:items-center swt:cursor-pointer swt:gap-2 swt:min-h-8"
                                                        prop.children [
                                                            Html.input [
                                                                prop.type'.radio
                                                                prop.className "swt:radio swt:radio-sm"
                                                                prop.name radioName
                                                                prop.isChecked isSelected
                                                                prop.onChange (fun (_: Browser.Types.Event) -> setTemplateImportMode template.Id mode)
                                                            ]
                                                            Html.span [
                                                                prop.className "swt:text-sm"
                                                                prop.text label
                                                            ]
                                                        ]
                                                    ]

                                                Html.div [
                                                    prop.key (string template.Id)
                                                    prop.className "swt:border swt:rounded-md swt:p-3 swt:w-full swt:min-h-28 swt:flex swt:flex-col swt:gap-2"
                                                    prop.children [
                                                        Html.h3 [
                                                            prop.className "swt:font-semibold swt:gap-2 swt:flex swt:flex-row swt:items-center"
                                                            prop.children [
                                                                Swate.Components.Icons.Table()
                                                                Html.span template.Name
                                                            ]
                                                        ]
                                                        Html.div [
                                                            prop.className "swt:flex swt:flex-col swt:gap-1"
                                                            prop.children [
                                                                modeRadio TemplateImportMode.AppendToActiveTable "Append"
                                                                modeRadio TemplateImportMode.CreateNewTable "Create New Table"
                                                                modeRadio TemplateImportMode.Skip "Do Not Import"
                                                            ]
                                                        ]
                                                        Html.div [
                                                            prop.className "swt:flex swt:flex-col swt:gap-2"
                                                            prop.children [
                                                                TemplateWidget.DropdownContent(
                                                                    isRowExtended,
                                                                    setIsRowExtended,
                                                                    TemplateWidget.SelectedColumnPreview(
                                                                        columns,
                                                                        isColumnSelected,
                                                                        toggleColumnSelected,
                                                                        template,
                                                                        importMode
                                                                    )
                                                                )
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                    ]
                                ]
                                if activeTableData.IsNone then
                                    Html.div [
                                        prop.className "swt:alert swt:alert-warning swt:text-warning-content swt:py-2 swt:text-sm"
                                        prop.children [
                                            Html.span "No active table selected. Open a table before submitting import."
                                        ]
                                    ]
                            ]
                        ],
                    footer =
                        Html.div [
                            prop.className "swt:justify-end swt:flex swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.className "swt:btn swt:btn-outline"
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> setIsImportDialogOpen false)
                                ]
                                Html.button [
                                    prop.className "swt:btn swt:btn-primary swt:ml-auto"
                                    prop.disabled (not canSubmitImport)
                                    prop.text "Import"
                                    prop.onClick (fun _ -> tryImportTemplates ())
                                ]
                            ]
                        ]
                )
                match status with
                | Some message -> StatusElement.Create message
                | None -> Html.none
            ]
        ]
