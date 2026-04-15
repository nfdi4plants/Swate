namespace Swate.Components

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components.Shared
open Swate.Components.Widgets.Context


type private TemplateLoadState =
    | TemplateLoading
    | TemplatesLoaded of Template[]
    | TemplateLoadError of string

type private TemplateImportAction =
    | ImportAsNewTable
    | AppendToActiveTable
    | NoImport

[<Erase; Mangle(false)>]
type TemplateWidget =

    static member private WidgetContainerClass =
        "swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:w-[64rem] swt:max-w-[95vw] swt:h-[70vh] swt:max-h-[80vh]"

    static member private toFullAuthorName(author: Person) =
        [ author.FirstName; author.MidInitials; author.LastName ]
        |> List.choose id
        |> String.concat " "

    static member private tryTrimmed(value: string) =
        if System.String.IsNullOrWhiteSpace value then
            None
        else
            Some(value.Trim())

    static member private compositeCellPreviewValues(cell: CompositeCell) =
        match cell with
        | CompositeCell.FreeText text -> [| text |]
        | CompositeCell.Term ontologyAnnotation -> [|
            ontologyAnnotation.NameText
            defaultArg ontologyAnnotation.TermSourceREF ""
            defaultArg ontologyAnnotation.TermAccessionNumber ""
          |]
        | CompositeCell.Unitized(value, ontologyAnnotation) -> [|
            value
            ontologyAnnotation.NameText
            defaultArg ontologyAnnotation.TermSourceREF ""
            defaultArg ontologyAnnotation.TermAccessionNumber ""
          |]
        | CompositeCell.Data data -> [|
            defaultArg data.FilePath ""
            data.NameText
            defaultArg data.Selector ""
            defaultArg data.Format ""
            defaultArg data.SelectorFormat ""
          |]
        |> Array.choose TemplateWidget.tryTrimmed

    static member private templateColumnValuePreview (table: ArcTable) (columnIndex: int) =
        seq {
            for rowIndex in 0 .. table.RowCount - 1 do
                let row = table.GetRow(rowIndex, true) |> Array.ofSeq

                if columnIndex < row.Length then
                    yield row.[columnIndex]
        }
        |> Seq.collect TemplateWidget.compositeCellPreviewValues
        |> Seq.distinct
        |> Seq.truncate 3
        |> String.concat " | "
        |> fun preview -> if preview = "" then "No values" else preview

    [<ReactComponent>]
    static member private TemplateRows
        (templates: Template[], selectedTemplateIds: Set<System.Guid>, toggleTemplateSelection: System.Guid -> unit)
        =
        if templates.Length = 0 then
            Html.div [
                prop.className "swt:text-sm swt:opacity-70 swt:text-center"
                prop.text "No templates found."
            ]
        else
            Html.table [
                prop.className "swt:table swt:table-fixed swt:w-full"
                prop.children [
                    Html.thead [
                        Html.tr [
                            Html.th [ prop.className "swt:w-10"; prop.text "" ]
                            Html.th [ prop.className "swt:w-[35%]"; prop.text "Template" ]
                            Html.th [ prop.className "swt:w-[20%]"; prop.text "Organisation" ]
                            Html.th [ prop.className "swt:w-[15%]"; prop.text "Version" ]
                            Html.th [ prop.className "swt:w-[30%]"; prop.text "Authors" ]
                        ]
                    ]
                    Html.tbody [
                        for template in templates do
                            let isSelected = selectedTemplateIds.Contains template.Id

                            let authors =
                                template.Authors
                                |> Seq.map TemplateWidget.toFullAuthorName
                                |> String.concat ", "

                            let toggleThisTemplate _ = toggleTemplateSelection template.Id

                            Html.tr [
                                prop.key (string template.Id)
                                prop.className [
                                    "swt:cursor-pointer hover:swt:bg-base-200"
                                    if isSelected then
                                        "swt:bg-primary/10"
                                ]
                                prop.onClick toggleThisTemplate
                                prop.children [
                                    Html.td [
                                        prop.className "swt:w-10"
                                        prop.children [
                                            Html.input [
                                                prop.className "swt:checkbox"
                                                prop.isChecked isSelected
                                                prop.type'.checkbox
                                                prop.custom ("readOnly", true)
                                                prop.onChange (fun (_: bool) -> toggleTemplateSelection template.Id)
                                                prop.onClick (fun event -> event.stopPropagation ())
                                            ]
                                        ]
                                    ]
                                    Html.td [
                                        prop.className "swt:truncate"
                                        prop.title template.Name
                                        prop.text template.Name
                                    ]
                                    Html.td [
                                        prop.className "swt:truncate"
                                        prop.title (template.Organisation.ToString())
                                        prop.text (template.Organisation.ToString())
                                    ]
                                    Html.td [
                                        prop.className "swt:truncate"
                                        prop.title template.Version
                                        prop.text template.Version
                                    ]
                                    Html.td [
                                        prop.className
                                            "swt:text-xs swt:opacity-70 swt:whitespace-nowrap swt:overflow-hidden swt:text-ellipsis"
                                        prop.title authors
                                        prop.text authors
                                    ]
                                ]
                            ]
                    ]
                ]
            ]

    [<ReactComponent>]
    static member private SelectedTemplatePreviewBoxes
        (
            templates: Template[],
            getTemplateImportAction: System.Guid -> TemplateImportAction,
            setTemplateImportAction: System.Guid -> TemplateImportAction -> unit,
            isColumnSelected: System.Guid -> int -> bool,
            toggleColumnSelection: System.Guid -> int -> unit,
            selectAllTemplateColumns: System.Guid -> unit,
            unselectAllTemplateColumns: Template -> unit
        ) =
        if templates.Length = 0 then
            Html.div [
                prop.className "swt:text-sm swt:opacity-70"
                prop.text "No templates selected."
            ]
        else
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2"
                prop.children [
                    for template in templates do
                        let radioGroupName = $"template-import-action-{template.Id}"
                        let importAction = getTemplateImportAction template.Id
                        let columns = template.Table.Columns |> Array.ofSeq

                        let selectedColumnsCount =
                            columns
                            |> Array.indexed
                            |> Array.sumBy (fun (columnIndex, _) ->
                                if isColumnSelected template.Id columnIndex then 1 else 0
                            )

                        let canEditColumns = importAction <> TemplateImportAction.NoImport

                        let renderImportActionOption (action: TemplateImportAction) (label: string) =
                            Html.label [
                                prop.className "swt:inline-flex swt:items-center swt:gap-2 swt:cursor-pointer"
                                prop.children [
                                    Html.input [
                                        prop.type'.radio
                                        prop.name radioGroupName
                                        prop.className "swt:radio swt:radio-xs"
                                        prop.isChecked (importAction.Equals action)
                                        prop.onChange (fun (_: bool) -> setTemplateImportAction template.Id action)
                                    ]
                                    Html.span [ prop.className "swt:text-xs"; prop.text label ]
                                ]
                            ]

                        Html.div [
                            prop.key (string template.Id)
                            prop.className
                                "swt:border swt:border-base-300 swt:rounded-box swt:p-2 swt:flex swt:flex-col swt:gap-2"
                            prop.children [
                                Html.div [
                                    prop.className "swt:text-sm swt:font-medium swt:truncate"
                                    prop.title template.Name
                                    prop.text template.Name
                                ]
                                Html.div [
                                    prop.className "swt:flex swt:flex-col swt:gap-1"
                                    prop.children [
                                        renderImportActionOption
                                            TemplateImportAction.ImportAsNewTable
                                            "Import (new table)"
                                        renderImportActionOption
                                            TemplateImportAction.AppendToActiveTable
                                            "Append to active table"
                                        renderImportActionOption TemplateImportAction.NoImport "No import"
                                    ]
                                ]
                                Html.details [
                                    prop.className "swt:border swt:border-base-300 swt:rounded-box swt:px-2 swt:py-1"
                                    prop.children [
                                        Html.summary [
                                            prop.className "swt:cursor-pointer swt:text-xs swt:font-medium"
                                            prop.textf "Columns: %d/%d selected" selectedColumnsCount columns.Length
                                        ]
                                        Html.div [
                                            prop.className "swt:flex swt:flex-col swt:gap-2 swt:mt-2"
                                            prop.children [
                                                if columns.Length = 0 then
                                                    Html.div [
                                                        prop.className "swt:text-xs swt:opacity-70"
                                                        prop.text "Template has no columns."
                                                    ]
                                                else
                                                    React.Fragment [
                                                        Html.div [
                                                            prop.className "swt:flex swt:items-center swt:gap-2"
                                                            prop.children [
                                                                Html.button [
                                                                    prop.className
                                                                        "swt:btn swt:btn-ghost swt:btn-xs swt:ml-auto"
                                                                    prop.text "Select all"
                                                                    prop.disabled (not canEditColumns)
                                                                    prop.onClick (fun _ ->
                                                                        selectAllTemplateColumns template.Id
                                                                    )
                                                                ]
                                                                Html.button [
                                                                    prop.className "swt:btn swt:btn-ghost swt:btn-xs"
                                                                    prop.text "Unselect all"
                                                                    prop.disabled (not canEditColumns)
                                                                    prop.onClick (fun _ ->
                                                                        unselectAllTemplateColumns template
                                                                    )
                                                                ]
                                                            ]
                                                        ]
                                                        Html.div [
                                                            prop.className "swt:overflow-x-auto"
                                                            prop.children [
                                                                Html.table [
                                                                    prop.className
                                                                        "swt:table swt:table-xs swt:table-fixed swt:w-max"
                                                                    prop.children [
                                                                        Html.tbody [
                                                                            Html.tr [
                                                                                prop.children [
                                                                                    for columnIndex in
                                                                                        0 .. columns.Length - 1 do
                                                                                        let header =
                                                                                            columns.[columnIndex].Header
                                                                                                .ToString()

                                                                                        Html.td [
                                                                                            prop.key
                                                                                                $"{template.Id}_{columnIndex}_header_card"
                                                                                            prop.className
                                                                                                "swt:w-52 swt:min-w-52 swt:px-2 swt:py-1"
                                                                                            prop.children [
                                                                                                Html.label [
                                                                                                    prop.className
                                                                                                        "swt:flex swt:items-center swt:gap-2 swt:cursor-pointer swt:border swt:border-base-300 swt:rounded-box swt:px-2 swt:py-1"
                                                                                                    prop.children [
                                                                                                        Html.input [
                                                                                                            prop.type'.checkbox
                                                                                                            prop.className
                                                                                                                "swt:checkbox swt:checkbox-xs"
                                                                                                            prop
                                                                                                                .isChecked (
                                                                                                                    isColumnSelected
                                                                                                                        template.Id
                                                                                                                        columnIndex
                                                                                                                )
                                                                                                            prop
                                                                                                                .disabled (
                                                                                                                    not
                                                                                                                        canEditColumns
                                                                                                                )
                                                                                                            prop.onChange (fun
                                                                                                                               (_:
                                                                                                                                   bool) ->
                                                                                                                toggleColumnSelection
                                                                                                                    template.Id
                                                                                                                    columnIndex
                                                                                                            )
                                                                                                        ]
                                                                                                        Html.span [
                                                                                                            prop.className
                                                                                                                "swt:text-xs swt:font-medium swt:truncate"
                                                                                                            prop.title
                                                                                                                header
                                                                                                            prop.text
                                                                                                                header
                                                                                                        ]
                                                                                                    ]
                                                                                                ]
                                                                                            ]
                                                                                        ]
                                                                                ]
                                                                            ]
                                                                            Html.tr [
                                                                                prop.children [
                                                                                    for columnIndex in
                                                                                        0 .. columns.Length - 1 do
                                                                                        let valuePreviewText =
                                                                                            TemplateWidget.templateColumnValuePreview
                                                                                                template.Table
                                                                                                columnIndex

                                                                                        Html.td [
                                                                                            prop.key
                                                                                                $"{template.Id}_{columnIndex}_values_card"
                                                                                            prop.className
                                                                                                "swt:w-52 swt:min-w-52 swt:px-2 swt:pt-0 swt:pb-1"
                                                                                            prop.children [
                                                                                                Html.span [
                                                                                                    prop.className
                                                                                                        "swt:block swt:text-[10px] swt:opacity-70 swt:truncate"
                                                                                                    prop.title
                                                                                                        valuePreviewText
                                                                                                    prop.text
                                                                                                        valuePreviewText
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
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]

    [<ReactComponent>]
    static member Main
        (
            arcFile: ArcFiles,
            activeTableIndex: int option,
            setArcFile: ArcFiles -> unit,
            importType: TableJoinOptions,
            setImportType: TableJoinOptions -> unit,
            services: TemplateWidgetServices
        ) =

        let templateState, setTemplateState = React.useState (fun _ -> TemplateLoading)

        let selectedTemplateIds, setSelectedTemplateIds =
            React.useStateWithUpdater (Set.empty<System.Guid>)

        let templateImportDecisions, setTemplateImportDecisions =
            React.useStateWithUpdater (Map.empty<System.Guid, TemplateImportAction>)

        let deselectedTemplateColumns, setDeselectedTemplateColumns =
            React.useStateWithUpdater (Set.empty<System.Guid * int>)

        let showImportDialog, setShowImportDialog = React.useState false
        let widgetCtx = useWidgetController ()

        let tryGetActiveTableIndex (arcFile: ArcFiles) =
            arcFile.TryGetActiveTable(activeTableIndex) |> Option.map fst

        let disabledMessage =
            match tryGetActiveTableIndex arcFile with
            | Some _ -> None
            | None -> Some "Select a table first to import templates."

        let canAppend = tryGetActiveTableIndex arcFile |> Option.isSome

        let syncSelectedTemplateIds (loadedTemplates: Template[]) =
            let templateIds =
                loadedTemplates |> Seq.map (fun template -> template.Id) |> Set.ofSeq

            setSelectedTemplateIds (fun selected -> selected |> Set.intersect templateIds)

            setTemplateImportDecisions (fun decisions ->
                decisions |> Map.filter (fun templateId _ -> templateIds.Contains templateId)
            )

            setDeselectedTemplateColumns (fun deselected ->
                deselected
                |> Set.filter (fun (templateId, columnIndex) ->
                    loadedTemplates
                    |> Array.tryFind (fun template -> template.Id = templateId)
                    |> Option.exists (fun template -> columnIndex >= 0 && columnIndex < template.Table.Columns.Count)
                )
            )

        React.useEffectOnce (fun _ ->
            async {
                let! result = services.loadTemplates ()

                match result with
                | Ok templates ->
                    templates
                    |> Array.sortBy (fun template -> template.Name)
                    |> TemplatesLoaded
                    |> setTemplateState
                | Error message -> setTemplateState (TemplateLoadError message)
            }
            |> Async.StartImmediate
        )

        React.useEffect (
            (fun () ->
                match templateState with
                | TemplatesLoaded templates -> syncSelectedTemplateIds templates
                | _ -> ()
            ),
            [| box templateState |]
        )

        React.useEffect (
            (fun () ->
                setTemplateImportDecisions (fun decisions ->
                    let prunedToSelected =
                        decisions
                        |> Map.filter (fun templateId _ -> selectedTemplateIds.Contains templateId)

                    selectedTemplateIds
                    |> Set.fold
                        (fun state templateId ->
                            if Map.containsKey templateId state then
                                state
                            else
                                state |> Map.add templateId TemplateImportAction.AppendToActiveTable
                        )
                        prunedToSelected
                )
            ),
            [| box selectedTemplateIds |]
        )

        let toggleTemplateSelection (templateId: System.Guid) =
            let isCurrentlySelected = selectedTemplateIds.Contains templateId

            setSelectedTemplateIds (fun current ->
                if isCurrentlySelected then
                    current.Remove templateId
                else
                    current.Add templateId
            )

            if isCurrentlySelected then
                setTemplateImportDecisions (fun decisions -> decisions.Remove templateId)

                setDeselectedTemplateColumns (fun deselected ->
                    deselected
                    |> Set.filter (fun (candidateTemplateId, _) -> candidateTemplateId <> templateId)
                )
            else
                setTemplateImportDecisions (fun decisions ->
                    decisions.Add(templateId, TemplateImportAction.AppendToActiveTable)
                )

        let isTemplateColumnSelected (templateId: System.Guid) (columnIndex: int) =
            deselectedTemplateColumns.Contains(templateId, columnIndex) |> not

        let getTemplateImportAction (templateId: System.Guid) =
            templateImportDecisions
            |> Map.tryFind templateId
            |> Option.defaultValue TemplateImportAction.AppendToActiveTable

        let setTemplateImportAction (templateId: System.Guid) (importAction: TemplateImportAction) =
            setTemplateImportDecisions (fun decisions -> decisions.Add(templateId, importAction))

        let toggleTemplateColumnSelection (templateId: System.Guid) (columnIndex: int) =
            setDeselectedTemplateColumns (fun deselected ->
                if deselected.Contains(templateId, columnIndex) then
                    deselected.Remove(templateId, columnIndex)
                else
                    deselected.Add(templateId, columnIndex)
            )

        let selectAllTemplateColumns (templateId: System.Guid) =
            setDeselectedTemplateColumns (fun deselected ->
                deselected
                |> Set.filter (fun (candidateTemplateId, _) -> candidateTemplateId <> templateId)
            )

        let unselectAllTemplateColumns (template: Template) =
            let allTemplateColumns =
                template.Table.Columns
                |> Seq.mapi (fun columnIndex _ -> template.Id, columnIndex)
                |> Set.ofSeq

            setDeselectedTemplateColumns (fun deselected -> Set.union deselected allTemplateColumns)

        let importTemplates () =
            match canAppend with
            | false -> false
            | true ->
                match templateState with
                | TemplatesLoaded templates ->
                    let selectedTemplates =
                        templates
                        |> Array.filter (fun template -> selectedTemplateIds.Contains template.Id)

                    let selectedTemplatesForImport =
                        selectedTemplates
                        |> Array.choose (fun template ->
                            match getTemplateImportAction template.Id with
                            | TemplateImportAction.NoImport -> None
                            | nextAction -> Some(template, nextAction)
                        )

                    if selectedTemplatesForImport.Length > 0 then
                        let importTables =
                            ResizeArray(selectedTemplatesForImport |> Seq.map (fun (template, _) -> template.Table))

                        let selectedTemplateIndexById =
                            selectedTemplatesForImport
                            |> Array.mapi (fun tableIndex (template, _) -> template.Id, tableIndex)
                            |> Map.ofArray

                        let deselectedColumns =
                            deselectedTemplateColumns
                            |> Seq.choose (fun (templateId, columnIndex) ->
                                selectedTemplateIndexById
                                |> Map.tryFind templateId
                                |> Option.map (fun tableIndex -> tableIndex, columnIndex)
                            )
                            |> Set.ofSeq

                        let importTablesConfig =
                            selectedTemplatesForImport
                            |> Array.mapi (fun tableIndex (_, action) ->
                                ({
                                    Index = tableIndex
                                    FullImport = (action = TemplateImportAction.ImportAsNewTable)
                                }
                                : WidgetTemplateImport.ImportTable)
                            )
                            |> Array.toList

                        let importConfig: WidgetTemplateImport.SelectiveImportConfig = {
                            WidgetTemplateImport.SelectiveImportConfig.init () with
                                ImportType = importType
                                ImportMetadata = false
                                ImportTables = importTablesConfig
                                DeselectedColumns = deselectedColumns
                        }

                        let nextArcFileState =
                            WidgetTemplateImport.updateTables
                                importTables
                                importConfig
                                (tryGetActiveTableIndex arcFile)
                                (Some arcFile)

                        setArcFile (WidgetArcFile.refreshRef nextArcFileState)
                        setSelectedTemplateIds (fun _ -> Set.empty<System.Guid>)
                        setTemplateImportDecisions (fun _ -> Map.empty<System.Guid, TemplateImportAction>)
                        setDeselectedTemplateColumns (fun _ -> Set.empty<System.Guid * int>)
                        true
                    else
                        false
                | _ -> false

        let openImportDialog () =
            if not showImportDialog then
                setShowImportDialog true

        let confirmImport () =
            let didImport = importTemplates ()
            setShowImportDialog false

            if didImport then
                widgetCtx.closeWidget WidgetType.Template

        let disabledState (message: string) =
            Html.div [
                prop.className TemplateWidget.WidgetContainerClass
                prop.children [
                    Html.h3 [ prop.className "swt:font-bold"; prop.text "Add Template" ]
                    Html.span [
                        prop.className "swt:text-xs swt:opacity-70"
                        prop.text message
                    ]
                ]
            ]

        match templateState with
        | TemplateLoading ->
            Html.div [
                prop.className TemplateWidget.WidgetContainerClass
                prop.children [
                    Html.h3 [ prop.className "swt:font-bold"; prop.text "Add Template" ]
                    Html.div [
                        prop.className "swt:flex swt:justify-center swt:flex-1 swt:items-center"
                        prop.children [ Icons.SpinningSpinner() ]
                    ]
                ]
            ]
        | TemplateLoadError message ->
            Html.div [
                prop.className TemplateWidget.WidgetContainerClass
                prop.children [
                    Html.h3 [ prop.className "swt:font-bold"; prop.text "Add Template" ]
                    Html.span [
                        prop.className "swt:text-xs swt:text-error"
                        prop.text $"Failed to load templates: {message}"
                    ]
                ]
            ]
        | TemplatesLoaded templates ->
            match disabledMessage with
            | Some message -> disabledState message
            | None ->
                let selectedTemplates =
                    templates
                    |> Array.filter (fun template -> selectedTemplateIds.Contains template.Id)

                let selectedTemplatesForImport =
                    selectedTemplates
                    |> Array.filter (fun template ->
                        getTemplateImportAction template.Id <> TemplateImportAction.NoImport
                    )

                let canImport = selectedTemplates.Length > 0 && disabledMessage.IsNone

                let canConfirmImport =
                    selectedTemplatesForImport.Length > 0 && disabledMessage.IsNone

                TemplateFilter.TemplateFilterProvider(
                    Html.div [
                        prop.className TemplateWidget.WidgetContainerClass
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:items-end"
                                prop.children [
                                    Html.h3 [
                                        prop.className "swt:text-xl swt:font-bold"
                                        prop.text "Add Template"
                                    ]
                                    Html.div [
                                        prop.className "swt:text-xs swt:opacity-70 swt:ml-auto"
                                        prop.textf "%d selected" selectedTemplates.Length
                                    ]
                                    Html.button [
                                        prop.className [
                                            "swt:btn swt:btn-sm swt:w-full"
                                            if canImport then "swt:btn-primary" else "swt:btn-disabled"
                                        ]
                                        prop.disabled (not canImport)
                                        prop.onClick (fun _ -> openImportDialog ())
                                        prop.text "Import"
                                    ]
                                ]
                            ]
                            BaseModal.Modal(
                                isOpen = showImportDialog,
                                setIsOpen = setShowImportDialog,
                                header = Html.text "Import templates",
                                description = Html.text "Select an import mode before importing the selected templates.",
                                children =
                                    Html.div [
                                        prop.className "swt:flex swt:flex-col swt:gap-2"
                                        prop.children [
                                            Html.div [
                                                prop.className "swt:text-xs swt:opacity-70"
                                                prop.text "Import mode"
                                            ]
                                            for importMode, _, label in WidgetTemplateImport.TemplateImportMode.options do
                                                Html.label [
                                                    prop.className
                                                        "swt:label swt:cursor-pointer swt:justify-start swt:gap-2"
                                                    prop.children [
                                                        Html.input [
                                                            prop.type'.radio
                                                            prop.name "template-import-mode"
                                                            prop.className "swt:radio swt:radio-sm"
                                                            prop.isChecked (importType.Equals importMode)
                                                            prop.onChange (fun (_: bool) -> setImportType importMode)
                                                        ]
                                                        Html.span [ prop.className "swt:text-sm"; prop.text label ]
                                                    ]
                                                ]
                                            Html.div [ prop.className "swt:divider swt:my-1" ]
                                            Html.div [
                                                prop.className "swt:text-xs swt:opacity-70"
                                                prop.textf "Template preview (%d selected)" selectedTemplates.Length
                                            ]
                                            Html.div [
                                                prop.className "swt:max-h-64 swt:overflow-y-auto"
                                                prop.children [
                                                    TemplateWidget.SelectedTemplatePreviewBoxes(
                                                        selectedTemplates,
                                                        getTemplateImportAction,
                                                        setTemplateImportAction,
                                                        isTemplateColumnSelected,
                                                        toggleTemplateColumnSelection,
                                                        selectAllTemplateColumns,
                                                        unselectAllTemplateColumns
                                                    )
                                                ]
                                            ]
                                        ]
                                    ],
                                footer =
                                    Html.div [
                                        prop.className "swt:flex swt:w-full swt:gap-2"
                                        prop.children [
                                            Html.button [
                                                prop.className "swt:btn swt:btn-outline"
                                                prop.text "Cancel"
                                                prop.onClick (fun _ -> setShowImportDialog false)
                                            ]
                                            Html.button [
                                                prop.className "swt:btn swt:btn-primary swt:ml-auto"
                                                prop.disabled (not canConfirmImport)
                                                prop.text "Import"
                                                prop.onClick (fun _ -> confirmImport ())
                                            ]
                                        ]
                                    ]
                            )
                            TemplateFilter.TemplateFilter(templates, templateSearchClassName = "swt:grow")
                            TemplateFilter.FilteredTemplateRenderer(fun filteredTemplates ->
                                Html.div [
                                    prop.className "swt:flex-1 swt:min-h-0 swt:overflow-y-auto"
                                    prop.children [
                                        TemplateWidget.TemplateRows(
                                            filteredTemplates,
                                            selectedTemplateIds,
                                            toggleTemplateSelection
                                        )
                                    ]
                                ]
                            )
                        ]
                    ]
                )
