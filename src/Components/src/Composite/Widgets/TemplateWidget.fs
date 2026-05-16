namespace Swate.Components.Widgets

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Template
open Swate.Components.Widgets.Context


module TemplateTypes = Swate.Components.Template.Types
module TemplateActions = Swate.Components.Template.TemplateActions
module TemplateCacheContext = Swate.Components.Template.TemplateCacheContext

[<Erase; Mangle(false)>]
type TemplateWidget =

    [<ReactComponent(true)>]
    static member TemplateWidget
        // 👀 If you rename these variables, ensure that the names are forwarded for lazy loading in `src\Components\src\ARCFileEditor\ArcFileEditor.fs` as well!
        (arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit) =

        let templateCacheCtx = TemplateCacheContext.useTemplateCacheCtx ()
        let templates = templateCacheCtx.Templates
        let isLoading = templateCacheCtx.IsLoading

        let importType, setImportType = React.useState TableJoinOptions.Headers

        let selectedTemplateIds, setSelectedTemplateIds =
            React.useStateWithUpdater (Set.empty<System.Guid>)

        let showImportDialog, setShowImportDialog = React.useState false
        let widgetCtx = useWidgetControllerCtx ()

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

        let importTemplates (payload: TemplateTypes.ImportModalConfirmPayload) =
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

                    setArcFile (ArcFiles.refreshRef nextArcFileState)
                    setSelectedTemplateIds (fun _ -> Set.empty<System.Guid>)
                    true
                else
                    false

        let openImportDialog () =
            if not showImportDialog then
                setShowImportDialog true

        let confirmImport (payload: TemplateTypes.ImportModalConfirmPayload) =
            let didImport = importTemplates payload
            setShowImportDialog false

            if didImport then
                widgetCtx.closeWidget WidgetType.Template

        let selectedTemplates =
            templates
            |> Array.filter (fun template -> selectedTemplateIds.Contains template.Id)

        React.Fragment [
            TemplateImportModal.TemplateImportModal(
                isOpen = showImportDialog,
                importType = importType,
                selectedTemplates = selectedTemplates,
                setIsOpen = setShowImportDialog,
                setImportType = setImportType,
                submitImport = confirmImport
            )
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2 swt:overflow-y-hidden swt:p-2"
                prop.children [
                    TemplateBrowser.TemplateBrowser(
                        templates,
                        isLoading,
                        selectedTemplateIds,
                        refreshTemplates,
                        openImportDialog,
                        toggleTemplateSelection,
                        ?disabledMessage = disabledMessage
                    )
                ]
            ]
        ]

    /// This will be used for tests in Widgets
    [<ReactComponent>]
    static member Entry
        (arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit, services: TemplateWidgetServices) =

        TemplateCacheProvider.TemplateCacheProvider(
            (fun () -> services.loadTemplates () |> Async.StartAsPromise),
            TemplateWidget.TemplateWidget(arcFile, activeTableIndex, setArcFile)
        )