namespace Swate.Components.Widgets

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components.Shared
open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Template


module TemplateHelper = Swate.Components.Template.Helper
module TemplateTypes = Swate.Components.Template.Types
module TemplateActions = Swate.Components.Template.TemplateActions


[<Erase; Mangle(false)>]
type TemplateWidget =

    static member private WidgetContainerClass =
        "swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:w-[64rem] swt:max-w-[95vw] swt:h-[70vh] swt:max-h-[80vh]"

    [<ReactComponent>]
    static member Main
        (arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit, services: TemplateWidgetServices) =

        let cacheState, setCacheState =
            React.useLocalStorage (TemplateHelper.CacheStorageKey, TemplateTypes.TemplateCacheState.Empty)

        let templateState, setTemplateState =
            React.useState (fun _ ->
                match TemplateHelper.tryReadTemplatesFromCache cacheState with
                | Ok(Some templates) ->
                    templates
                    |> Array.sortBy (fun template -> template.Name)
                    |> TemplateTypes.TemplateLoadState.Loaded
                | _ -> TemplateTypes.TemplateLoadState.Loading
            )

        let importType, setImportType = React.useState TableJoinOptions.Headers
        let latestFetchId = React.useRef (None: System.Guid option)

        let selectedTemplateIds, setSelectedTemplateIds =
            React.useStateWithUpdater (Set.empty<System.Guid>)

        let templateImportDecisions, setTemplateImportDecisions =
            React.useStateWithUpdater (Map.empty<System.Guid, TemplateTypes.TemplateImportAction>)

        let deselectedTemplateColumns, setDeselectedTemplateColumns =
            React.useStateWithUpdater (Set.empty<System.Guid * int>)

        let showImportDialog, setShowImportDialog = React.useState false
        let widgetCtx = WidgetContext.useWidgetController ()

        let tryGetActiveTableIndex (arcFile: ArcFiles) =
            arcFile.TryGetActiveTable(activeTableIndex) |> Option.map fst

        let disabledMessage =
            match tryGetActiveTableIndex arcFile with
            | Some _ -> None
            | None -> Some "Select a table first to import templates."

        let canAppend = tryGetActiveTableIndex arcFile |> Option.isSome

        let syncSelectedTemplateIds (loadedTemplates: Template[]) =
            TemplateActions.syncSelectedTemplateIds
                loadedTemplates
                setSelectedTemplateIds
                setTemplateImportDecisions
                setDeselectedTemplateColumns

        let loadTemplates (forceRefresh: bool) = async {
            let cachedTemplatesResult = TemplateHelper.tryReadTemplatesFromCache cacheState

            let shouldFetchFresh =
                forceRefresh
                || Result.isError cachedTemplatesResult
                || TemplateHelper.shouldFetchFresh false cacheState System.DateTime.UtcNow

            if shouldFetchFresh then
                let hasVisibleTemplates =
                    match templateState with
                    | TemplateTypes.TemplateLoadState.Loaded templates -> templates.Length > 0
                    | _ -> false

                if not hasVisibleTemplates then
                    setTemplateState TemplateTypes.TemplateLoadState.Loading

                let requestId = System.Guid.NewGuid()
                latestFetchId.current <- Some requestId

                let! result = services.loadTemplates ()

                if latestFetchId.current = Some requestId then
                    match result with
                    | Ok templates ->
                        let sortedTemplates = templates |> Array.sortBy (fun template -> template.Name)

                        setCacheState (TemplateHelper.toCacheState sortedTemplates System.DateTime.UtcNow)
                        setTemplateState (TemplateTypes.TemplateLoadState.Loaded sortedTemplates)
                    | Error message -> setTemplateState (TemplateTypes.TemplateLoadState.LoadError message)
            else
                match cachedTemplatesResult with
                | Ok(Some templates) ->
                    templates
                    |> Array.sortBy (fun template -> template.Name)
                    |> TemplateTypes.TemplateLoadState.Loaded
                    |> setTemplateState
                | Ok None -> setTemplateState TemplateTypes.TemplateLoadState.Loading
                | Error message ->
                    setCacheState TemplateTypes.TemplateCacheState.Empty
                    setTemplateState (TemplateTypes.TemplateLoadState.LoadError message)
        }

        React.useEffectOnce (fun _ -> loadTemplates false |> Async.StartImmediate)

        let refreshTemplates () =
            loadTemplates true |> Async.StartImmediate

        React.useEffect (
            (fun () ->
                match templateState with
                | TemplateTypes.TemplateLoadState.Loaded templates -> syncSelectedTemplateIds templates
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
                                state
                                |> Map.add templateId TemplateTypes.TemplateImportAction.AppendToActiveTable
                        )
                        prunedToSelected
                )
            ),
            [| box selectedTemplateIds |]
        )

        let toggleTemplateSelection (templateId: System.Guid) =
            setSelectedTemplateIds (fun current -> TemplateActions.toggleTemplateSelection templateId current)

            TemplateActions.toggleTemplateSelectionState
                templateId
                selectedTemplateIds
                setTemplateImportDecisions
                setDeselectedTemplateColumns

        let isTemplateColumnSelected (templateId: System.Guid) (columnIndex: int) =
            TemplateActions.isTemplateColumnSelected templateId columnIndex deselectedTemplateColumns

        let getTemplateImportAction (templateId: System.Guid) =
            TemplateActions.getTemplateImportAction templateId templateImportDecisions

        let setTemplateImportAction (templateId: System.Guid) (importAction: TemplateTypes.TemplateImportAction) =
            TemplateActions.setTemplateImportAction templateId importAction setTemplateImportDecisions

        let toggleTemplateColumnSelection (templateId: System.Guid) (columnIndex: int) =
            TemplateActions.toggleTemplateColumnSelection templateId columnIndex setDeselectedTemplateColumns

        let selectAllTemplateColumns (templateId: System.Guid) =
            TemplateActions.selectAllTemplateColumns templateId setDeselectedTemplateColumns

        let unselectAllTemplateColumns (template: Template) =
            TemplateActions.unselectAllTemplateColumns template setDeselectedTemplateColumns

        let importTemplates () =
            match canAppend with
            | false -> false
            | true ->
                match templateState with
                | TemplateTypes.TemplateLoadState.Loaded templates ->
                    let selectedTemplatesForImport =
                        TemplateActions.selectedTemplatesForImport templates selectedTemplateIds templateImportDecisions

                    if selectedTemplatesForImport.Length > 0 then
                        let importTables =
                            ResizeArray(selectedTemplatesForImport |> Seq.map (fun (template, _) -> template.Table))

                        let selectedTemplateIndexById =
                            TemplateActions.selectedTemplateIndexById selectedTemplatesForImport

                        let deselectedColumns =
                            TemplateActions.deselectedColumnsForImport
                                deselectedTemplateColumns
                                selectedTemplateIndexById

                        let importConfig =
                            TemplateActions.buildSelectiveImportConfig
                                importType
                                (TemplateActions.importTablesConfig selectedTemplatesForImport)
                                deselectedColumns

                        let nextArcFileState =
                            Helper.updateTables
                                importTables
                                importConfig
                                (tryGetActiveTableIndex arcFile)
                                (Some arcFile)

                        setArcFile (WidgetArcFile.refreshRef nextArcFileState)
                        setSelectedTemplateIds (fun _ -> Set.empty<System.Guid>)
                        setTemplateImportDecisions (fun _ -> Map.empty<System.Guid, TemplateTypes.TemplateImportAction>)
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
        | TemplateTypes.TemplateLoadState.Loading ->
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
        | TemplateTypes.TemplateLoadState.LoadError message ->
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
        | TemplateTypes.TemplateLoadState.Loaded templates ->
            match disabledMessage with
            | Some message -> disabledState message
            | None ->
                let selectedTemplates =
                    templates
                    |> Array.filter (fun template -> selectedTemplateIds.Contains template.Id)

                let selectedTemplatesForImport =
                    selectedTemplates
                    |> Array.filter (fun template ->
                        getTemplateImportAction template.Id
                        <> TemplateTypes.TemplateImportAction.NoImport
                    )

                let canImport = selectedTemplates.Length > 0 && disabledMessage.IsNone

                let canConfirmImport =
                    selectedTemplatesForImport.Length > 0 && disabledMessage.IsNone

                let previewCallbacks: TemplateTypes.TemplatePreviewCallbacks = {
                    GetTemplateImportAction = getTemplateImportAction
                    SetTemplateImportAction = setTemplateImportAction
                    IsColumnSelected = isTemplateColumnSelected
                    ToggleColumnSelection = toggleTemplateColumnSelection
                    SelectAllTemplateColumns = selectAllTemplateColumns
                    UnselectAllTemplateColumns = unselectAllTemplateColumns
                }

                TemplateFilter.TemplateFilterProvider(
                    Html.div [
                        prop.className TemplateWidget.WidgetContainerClass
                        prop.children [
                            TemplateToolbar.TemplateToolbar(
                                selectedTemplates.Length,
                                canImport,
                                refreshTemplates,
                                openImportDialog
                            )
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
                                            for importMode, _, label in Helper.TemplateImportMode.options do
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
                                                    TemplatePreview.TemplatePreview(selectedTemplates, previewCallbacks)
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
                                        TemplateRows.TemplateRows(
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