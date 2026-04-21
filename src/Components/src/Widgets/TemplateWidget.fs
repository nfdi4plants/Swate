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


module private TemplateWidgetHelper =

    [<Literal>]
    let WidgetContainerClass =
        "swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:w-[64rem] swt:max-w-[95vw] swt:h-[70vh] swt:max-h-[80vh]"

    type BrowserProps = {
        Templates: Template[]
        IsLoading: bool
        /// This field is used to define if template import is valid for any given state
        DisabledMessage: string option
        SelectedTemplateIds: Set<System.Guid>
        RefreshTemplates: unit -> unit
        OpenImportDialog: unit -> unit
        ToggleTemplateSelection: System.Guid -> unit
    } with

        member this.IsDisabled = this.DisabledMessage.IsSome

    type ImportModalConfirmPayload = {
        ImportType: TableJoinOptions
        SelectedTemplatesForImport: (Template * TemplateTypes.TemplateImportAction)[]
        DeselectedTemplateColumns: Set<System.Guid * int>
    }

open TemplateWidgetHelper

[<Erase; Mangle(false)>]
type TemplateWidget =


    [<ReactComponent>]
    static member DisabledMessage(message: string) =
        Html.span [
            prop.className "swt:text-xs swt:opacity-70"
            prop.text message
        ]

    [<ReactComponent>]
    static member LoadingSpinner() =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:items-center swt:gap-2 swt:py-10"
            prop.children [
                Html.div [ prop.className "swt:loading" ]
                Html.span [ prop.text "Loading templates..." ]
            ]
        ]

    [<ReactComponent>]
    static member private Browser(props: BrowserProps) =

        TemplateFilter.TemplateFilterProvider(
            React.Fragment [
                TemplateToolbar.TemplateToolbar(
                    props.SelectedTemplateIds.Count,
                    props.IsLoading,
                    props.RefreshTemplates,
                    props.OpenImportDialog
                )
                TemplateFilter.TemplateFilter(props.Templates, templateSearchClassName = "swt:grow")
                TemplateFilter.FilteredTemplateRenderer(fun filteredTemplates ->
                    Html.div [
                        prop.className "swt:flex-1 swt:min-h-0 swt:overflow-y-auto"
                        prop.children [
                            match props with
                            | { IsLoading = true } -> TemplateWidget.LoadingSpinner()
                            | { DisabledMessage = Some message } -> TemplateWidget.DisabledMessage message
                            | _ ->
                                TemplateRows.TemplateRows(
                                    filteredTemplates,
                                    props.SelectedTemplateIds,
                                    props.ToggleTemplateSelection
                                )
                        ]
                    ]
                )
            ]
        )


    [<ReactComponent>]
    static member private ImportModal
        (
            isOpen,
            importType: TableJoinOptions,
            selectedTemplates: Template[],
            setIsOpen,
            setImportType,
            submitImport: ImportModalConfirmPayload -> unit
        ) =

        let templateImportDecisions, setTemplateImportDecisions =
            React.useStateWithUpdater (Map.empty<System.Guid, TemplateTypes.TemplateImportAction>)

        let deselectedTemplateColumns, setDeselectedTemplateColumns =
            React.useStateWithUpdater (Set.empty<System.Guid * int>)

        let selectedTemplateIds =
            React.useMemo (
                (fun () -> selectedTemplates |> Seq.map (fun template -> template.Id) |> Set.ofSeq),
                [| box selectedTemplates |]
            )

        React.useEffect (
            (fun () ->
                setTemplateImportDecisions (fun decisions ->
                    decisions
                    |> Map.filter (fun templateId _ -> selectedTemplateIds.Contains templateId)
                )
                |> ignore

                setDeselectedTemplateColumns (fun deselected ->
                    deselected
                    |> Set.filter (fun (templateId, _) -> selectedTemplateIds.Contains templateId)
                )
                |> ignore
            ),
            [| box selectedTemplateIds |]
        )

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
            TemplateActions.selectedTemplatesForImport selectedTemplates selectedTemplateIds templateImportDecisions

        let canConfirmImport = selectedTemplatesForImport.Length > 0

        let previewCallbacks: TemplateTypes.TemplatePreviewCallbacks = {
            GetTemplateImportAction = getTemplateImportAction
            SetTemplateImportAction = setTemplateImportAction
            IsColumnSelected = isTemplateColumnSelected
            ToggleColumnSelection = toggleTemplateColumnSelection
            SelectAllTemplateColumns = selectAllTemplateColumns
            UnselectAllTemplateColumns = unselectAllTemplateColumns
        }

        let confirmImport () =
            submitImport {
                ImportType = importType
                SelectedTemplatesForImport = selectedTemplatesForImport
                DeselectedTemplateColumns = deselectedTemplateColumns
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
                            prop.onClick (fun _ -> confirmImport ())
                        ]
                    ]
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

        let toggleTemplateSelection (templateId: System.Guid) =
            setSelectedTemplateIds (fun current -> TemplateActions.toggleTemplateSelection templateId current)

        let importTemplates (payload: ImportModalConfirmPayload) =
            match canAppend with
            | false -> false
            | true ->
                if payload.SelectedTemplatesForImport.Length > 0 then
                    let importTables =
                        ResizeArray(
                            payload.SelectedTemplatesForImport
                            |> Seq.map (fun (template, _) -> template.Table)
                        )

                    let selectedTemplateIndexById =
                        TemplateActions.selectedTemplateIndexById payload.SelectedTemplatesForImport

                    let deselectedColumns =
                        TemplateActions.deselectedColumnsForImport
                            payload.DeselectedTemplateColumns
                            selectedTemplateIndexById

                    let importConfig =
                        TemplateActions.buildSelectiveImportConfig
                            payload.ImportType
                            (TemplateActions.importTablesConfig payload.SelectedTemplatesForImport)
                            deselectedColumns

                    let nextArcFileState =
                        Helper.updateTables importTables importConfig (tryGetActiveTableIndex arcFile) (Some arcFile)

                    setArcFile (WidgetArcFile.refreshRef nextArcFileState)
                    setSelectedTemplateIds (fun _ -> Set.empty<System.Guid>)
                    true
                else
                    false

        let openImportDialog () =
            if not showImportDialog then
                setShowImportDialog true

        let confirmImport (payload: ImportModalConfirmPayload) =
            let didImport = importTemplates payload
            setShowImportDialog false

            if didImport then
                widgetCtx.closeWidget WidgetType.Template

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
                prop.className WidgetContainerClass
                prop.children [
                    TemplateWidget.Browser(
                        {
                            Templates = templates
                            IsLoading = isLoading
                            DisabledMessage = disabledMessage
                            SelectedTemplateIds = selectedTemplateIds
                            RefreshTemplates = refreshTemplates
                            OpenImportDialog = openImportDialog
                            ToggleTemplateSelection = toggleTemplateSelection
                        }
                    )
                ]
            ]
        ]

    [<ReactComponent>]
    static member Entry
        (arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit, services: TemplateWidgetServices) =

        TemplateCacheProvider.TemplateCacheProvider(
            (fun () -> services.loadTemplates () |> Async.StartAsPromise),
            TemplateWidget.TemplateWidget(arcFile, activeTableIndex, setArcFile)
        )