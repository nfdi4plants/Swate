namespace Swate.Components.Template

open Fable.Core
open Feliz
open ARCtrl
open Swate.Components
open Swate.Components.Template.Types

[<Erase; Mangle(false)>]
type TemplateImportModal =

    [<ReactComponent>]
    static member TemplateImportModal
        (
            isOpen,
            importType: TableJoinOptions,
            selectedTemplates: Template[],
            setIsOpen,
            setImportType,
            submitImport: ImportModalConfirmPayload -> unit
        ) =

        let templateImportDecisions, setTemplateImportDecisions =
            React.useStateWithUpdater (Map.empty<System.Guid, TemplateImportAction>)

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

        let setTemplateImportAction (templateId: System.Guid) (importAction: TemplateImportAction) =
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

        let previewCallbacks: TemplatePreviewCallbacks = {
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
                                TemplateImportModalPreview.TemplateImportModalPreview(
                                    selectedTemplates,
                                    previewCallbacks
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