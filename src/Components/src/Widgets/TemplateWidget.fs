namespace Swate.Components.Widgets

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Template


module TemplateTypes = Swate.Components.Template.Types
module TemplateActions = Swate.Components.Template.TemplateActions
module TemplateCacheContext = Swate.Components.Template.TemplateCacheContext


[<Erase; Mangle(false)>]
type TemplateWidget =

    static member private WidgetContainerClass =
        "swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:w-[64rem] swt:max-w-[95vw] swt:h-[70vh] swt:max-h-[80vh]"

    [<ReactComponent>]
    static member ImportModal
        (
            isOpen,
            importType: TableJoinOptions,
            selectedTemplates: Template[],
            setIsOpen,
            setImportType,
            submitImport: unit -> unit
        ) =

        let templateImportDecisions, setTemplateImportDecisions =
            React.useStateWithUpdater (Map.empty<System.Guid, TemplateTypes.TemplateImportAction>)

        let deselectedTemplateColumns, setDeselectedTemplateColumns =
            React.useStateWithUpdater (Set.empty<System.Guid * int>)

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

        let selectedTemplatesForImport =
            selectedTemplates
            |> Array.filter (fun template ->
                getTemplateImportAction template.Id
                <> TemplateTypes.TemplateImportAction.NoImport
            )

        let canConfirmImport = selectedTemplatesForImport.Length > 0

        let previewCallbacks: TemplateTypes.TemplatePreviewCallbacks = {
            GetTemplateImportAction = getTemplateImportAction
            SetTemplateImportAction = setTemplateImportAction
            IsColumnSelected = isTemplateColumnSelected
            ToggleColumnSelection = toggleTemplateColumnSelection
            SelectAllTemplateColumns = selectAllTemplateColumns
            UnselectAllTemplateColumns = unselectAllTemplateColumns
        }

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
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
                            let isChecked = importType = importMode

                            Html.label [
                                prop.className "swt:label swt:cursor-pointer swt:justify-start swt:gap-2"
                                prop.children [
                                    Html.input [
                                        prop.type'.radio
                                        prop.name "template-import-mode"
                                        prop.className "swt:radio swt:radio-sm"
                                        prop.isChecked isChecked
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
                            prop.onClick (fun _ -> setIsOpen false)
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-primary swt:ml-auto"
                            prop.disabled (not canConfirmImport)
                            prop.text "Import"
                            prop.onClick (fun _ -> submitImport ())
                        ]
                    ]
                ]
        )

    [<ReactComponent>]
    static member TemplateBrowser(selectedTemplates: Template[], refreshTemplates: unit -> unit) =


        TemplateFilter.TemplateFilterProvider(
            React.Fragment [
                TemplateToolbar.TemplateToolbar(selectedTemplates.Length, isLoading, refreshTemplates, openImportDialog)
                TemplateFilter.TemplateFilter(templates, templateSearchClassName = "swt:grow")
                TemplateFilter.FilteredTemplateRenderer(fun filteredTemplates ->
                    Html.div [
                        prop.className "swt:flex-1 swt:min-h-0 swt:overflow-y-auto"
                        prop.children [
                            TemplateRows.TemplateRows(filteredTemplates, selectedTemplateIds, toggleTemplateSelection)
                        ]
                    ]
                )
            ]
        )



    [<ReactComponent>]
    static member TemplateWidget(arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit) =

        let templateCacheCtx = TemplateCacheContext.useTemplateCacheCtx ()
        let templates = templateCacheCtx.Templates
        let isLoading = templateCacheCtx.IsLoading

        let importType, setImportType = React.useState TableJoinOptions.Headers

        let selectedTemplateIds, setSelectedTemplateIds =
            React.useStateWithUpdater (Set.empty<System.Guid>)

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
            TemplateActions.syncSelectedTemplateIds loadedTemplates setSelectedTemplateIds

        let refreshTemplates () = templateCacheCtx.RefreshTemplates()

        React.useEffect ((fun () -> syncSelectedTemplateIds templates), [| box templates |])

        // React.useEffect (
        //     (fun () ->
        //         setTemplateImportDecisions (fun decisions ->
        //             let prunedToSelected =
        //                 decisions
        //                 |> Map.filter (fun templateId _ -> selectedTemplateIds.Contains templateId)

        //             selectedTemplateIds
        //             |> Set.fold
        //                 (fun state templateId ->
        //                     if Map.containsKey templateId state then
        //                         state
        //                     else
        //                         state
        //                         |> Map.add templateId TemplateTypes.TemplateImportAction.AppendToActiveTable
        //                 )
        //                 prunedToSelected
        //         )
        //     ),
        //     [| box selectedTemplateIds |]
        // )

        let toggleTemplateSelection (templateId: System.Guid) =
            setSelectedTemplateIds (fun current -> TemplateActions.toggleTemplateSelection templateId current)

            TemplateActions.toggleTemplateSelectionState templateId selectedTemplateIds

        let importTemplates () =
            match canAppend with
            | false -> false
            | true ->
                let selectedTemplatesForImport =
                    TemplateActions.selectedTemplatesForImport templates selectedTemplateIds templateImportDecisions

                if selectedTemplatesForImport.Length > 0 then
                    let importTables =
                        ResizeArray(selectedTemplatesForImport |> Seq.map (fun (template, _) -> template.Table))

                    let selectedTemplateIndexById =
                        TemplateActions.selectedTemplateIndexById selectedTemplatesForImport

                    let deselectedColumns =
                        TemplateActions.deselectedColumnsForImport deselectedTemplateColumns selectedTemplateIndexById

                    let importConfig =
                        TemplateActions.buildSelectiveImportConfig
                            importType
                            (TemplateActions.importTablesConfig selectedTemplatesForImport)
                            deselectedColumns

                    let nextArcFileState =
                        Helper.updateTables importTables importConfig (tryGetActiveTableIndex arcFile) (Some arcFile)

                    setArcFile (WidgetArcFile.refreshRef nextArcFileState)
                    setSelectedTemplateIds (fun _ -> Set.empty<System.Guid>)
                    setTemplateImportDecisions (fun _ -> Map.empty<System.Guid, TemplateTypes.TemplateImportAction>)
                    setDeselectedTemplateColumns (fun _ -> Set.empty<System.Guid * int>)
                    true
                else
                    false

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

        if isLoading then
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
        else
            match disabledMessage with
            | Some message -> disabledState message
            | None ->
                let selectedTemplates =
                    templates
                    |> Array.filter (fun template -> selectedTemplateIds.Contains template.Id)

                React.Fragment [
                    TemplateWidget.ImportModal(
                        isOpen = showImportDialog,
                        importType = importType,
                        selectedTemplates = selectedTemplates,
                        setIsOpen = setShowImportDialog,
                        setImportType = setImportType,
                        submitImport = confirmImport
                    )
                    Html.div [
                        prop.className TemplateWidget.WidgetContainerClass
                        prop.children [ TemplateWidget.TemplateBrowser() ]
                    ]
                ]

    [<ReactComponent>]
    static member Entry
        (arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit, services: TemplateWidgetServices) =

        TemplateCacheProvider.TemplateCacheProvider(
            (fun () -> services.loadTemplates () |> Async.StartAsPromise),
            TemplateWidget.TemplateWidget(arcFile, activeTableIndex, setArcFile)
        )