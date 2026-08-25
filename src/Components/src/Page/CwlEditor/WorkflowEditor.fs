namespace Swate.Components.Page.CwlEditor

open Fable.Core
open Feliz
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.CwlDefaults
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.Validation.ValidationTypes

[<AutoOpen>]
module private WorkflowEditorHelpers =

    let eventTargetValue (ev: Browser.Types.FocusEvent) =
        let target = ev.target :?> Browser.Types.HTMLInputElement
        if isNull target then "" else target.value

[<Erase; Mangle(false)>]
type WorkflowEditor =

    [<ReactComponent>]
    static member WorkflowEditor
        (
            version: int,
            editorSessionId: int,
            kindLabel: string,
            fileLabel: string,
            workflowFilePath: string option,
            isDirty: bool,
            isSaving: bool,
            errorMessage: string option,
            infoMessage: string option,
            stateCwlVersion: string,
            intentValue: string,
            workflow: CWLWorkflowDescription,
            workflowModel: WorkflowModel,
            inputs: InputModel list,
            outputs: OutputModel list,
            activeInputIndex: int option,
            activeOutputIndex: int option,
            activeStepIndex: int option,
            activeStepId: StepId option,
            activeStepInputId: StepInputId option,
            activeStepOutputId: StepOutputId option,
            requirements: RequirementNode list,
            hints: RequirementNode list,
            validationResult: ValidationResult,
            commitMutation: (unit -> unit) -> unit,
            setActiveInputIndex: int option -> unit,
            setActiveOutputIndex: int option -> unit,
            setActiveStepIndex: int option -> unit,
            setActiveStepId: StepId option -> unit,
            setActiveStepInputId: StepInputId option -> unit,
            setActiveStepOutputId: StepOutputId option -> unit,
            onWorkflowChanged: WorkflowModel -> unit,
            onPreview: unit -> unit,
            onSave: unit -> unit,
            onBackToStart: unit -> unit,
            onSetVersion: string -> unit,
            onSetIntent: string -> unit,
            onRenameInput: InputId -> string -> unit,
            onSetInputType: InputId -> string option -> unit,
            onSetInputPrefix: InputId -> string -> unit,
            onSetInputPosition: InputId -> string -> unit,
            onSetInputOptional: InputId -> bool -> unit,
            onAddInput: unit -> unit,
            onRemoveInput: InputId -> unit,
            onMoveInputUp: InputId -> unit,
            onMoveInputDown: InputId -> unit,
            onRenameOutput: OutputId -> string -> unit,
            onSetOutputType: OutputId -> string option -> unit,
            onSetOutputGlob: OutputId -> string -> unit,
            onAddOutput: unit -> unit,
            onRemoveOutput: OutputId -> unit,
            onMoveOutputUp: OutputId -> unit,
            onMoveOutputDown: OutputId -> unit,
            onSetRequirementEnabled: string -> bool -> RequirementNodeId option -> unit,
            onSetHintEnabled: string -> bool -> RequirementNodeId option -> unit,
            onSetRequirementField: RequirementNodeId -> string -> string -> unit,
            onSetHintField: RequirementNodeId -> string -> string -> unit,
            onPreviewYamlText: string -> unit,
            setInfoMessage: string option -> unit,
            setErrorMessage: string option -> unit
        ) : ReactElement =
        let focusedRequirementId, setFocusedRequirementId =
            React.useState<RequirementNodeId option> (None)

        let clearFocusedRequirement () = setFocusedRequirementId None

        let setEnabled bucket key isEnabled requirementNodeId =
            match bucket with
            | RequirementBucket -> onSetRequirementEnabled key isEnabled requirementNodeId
            | HintBucket -> onSetHintEnabled key isEnabled requirementNodeId

        let setField bucket requirementNodeId fieldKey value =
            match bucket with
            | RequirementBucket -> onSetRequirementField requirementNodeId fieldKey value
            | HintBucket -> onSetHintField requirementNodeId fieldKey value

        Html.div [
            prop.testId "cwl-workflow-editor"
            prop.className "swt:flex swt:flex-col swt:h-full swt:min-h-0"
            prop.children [
                Header.Header(version, kindLabel, fileLabel, isDirty, isSaving, onPreview, onSave, onBackToStart)
                Html.main [
                    prop.className
                        "swt:grid swt:grid-cols-[360px_1fr] swt:gap-4 swt:flex-1 swt:h-full swt:min-h-0 swt:p-4"
                    prop.children [
                        Html.aside [
                            prop.className "swt:overflow-y-auto swt:min-h-0 swt:flex swt:flex-col swt:gap-4"
                            prop.children [
                                match errorMessage with
                                | Some message ->
                                    Html.div [
                                        prop.testId "cwl-editor-error"
                                        prop.className "swt:alert swt:alert-error"
                                        prop.text message
                                    ]
                                | None -> Html.none
                                match infoMessage with
                                | Some message ->
                                    Html.div [
                                        prop.testId "cwl-editor-info"
                                        prop.className "swt:alert swt:alert-info"
                                        prop.text message
                                    ]
                                | None -> Html.none
                                Html.section [
                                    prop.testId "cwl-editor-base-properties"
                                    prop.className "swt:card swt:bg-base-200 swt:p-4"
                                    prop.children [
                                        Html.h3 [
                                            prop.className "swt:font-semibold swt:text-base-content"
                                            prop.text "Base Properties"
                                        ]
                                        Html.label [
                                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                                            prop.children [
                                                Html.span [ prop.className "swt:text-sm"; prop.text "CWL Version" ]
                                                Html.select [
                                                    prop.testId "cwl-editor-cwl-version"
                                                    prop.className "swt:select swt:select-sm swt:w-full"
                                                    prop.value stateCwlVersion
                                                    prop.onChange onSetVersion
                                                    prop.children [
                                                        for cwlVersion in SupportedCwlVersions do
                                                            Html.option [
                                                                prop.key cwlVersion
                                                                prop.value cwlVersion
                                                                prop.text cwlVersion
                                                            ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                        Html.label [
                                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                                            prop.children [
                                                Html.span [
                                                    prop.className "swt:text-sm"
                                                    prop.text "Intent (comma separated)"
                                                ]
                                                Html.input [
                                                    prop.testId "cwl-editor-intent"
                                                    prop.className "swt:input swt:input-sm swt:w-full"
                                                    prop.defaultValue intentValue
                                                    prop.placeholder "workflow-orchestration, pipeline"
                                                    prop.onBlur (fun ev -> onSetIntent (eventTargetValue ev))
                                                ]
                                            ]
                                        ]
                                        Html.p [
                                            prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                                            prop.text
                                                "Workflow editing supports both form-based mutation and canvas-based connection editing. Advanced run-type tooling remains pending."
                                        ]
                                    ]
                                ]
                                RequirementPicker.RequirementNodeSidebarPanel {
                                    Version = version
                                    RequirementItems = requirements
                                    HintItems = hints
                                    FocusedId = focusedRequirementId
                                    OnFocus = setFocusedRequirementId
                                    OnSetEnabled = setEnabled
                                    OnSetField = setField
                                }
                                ValidationPanel.ValidationPanel(version, validationResult)
                            ]
                        ]
                        Html.section [
                            prop.className "swt:flex swt:flex-col swt:gap-4 swt:min-h-0 swt:overflow-y-auto"
                            prop.children [
                                InputsEditor.InputsEditor(
                                    version,
                                    inputs,
                                    activeInputIndex,
                                    setActiveInputIndex,
                                    onRenameInput,
                                    onSetInputType,
                                    onSetInputPrefix,
                                    onSetInputPosition,
                                    onSetInputOptional,
                                    onAddInput,
                                    onRemoveInput,
                                    onMoveInputUp,
                                    onMoveInputDown,
                                    onInteract = clearFocusedRequirement
                                )
                                RequirementPicker.RequirementNodeMainPanel {
                                    RequirementItems = requirements
                                    HintItems = hints
                                    FocusedId = focusedRequirementId
                                    OnFocus = setFocusedRequirementId
                                    OnSetEnabled = setEnabled
                                }
                                OutputsEditor.OutputsEditor(
                                    version,
                                    outputs,
                                    activeOutputIndex,
                                    setActiveOutputIndex,
                                    onRenameOutput,
                                    onSetOutputType,
                                    onSetOutputGlob,
                                    onAddOutput,
                                    onRemoveOutput,
                                    onMoveOutputUp,
                                    onMoveOutputDown,
                                    onInteract = clearFocusedRequirement
                                )
                                Html.div [
                                    prop.key (sprintf "workflow-canvas-host:%d" editorSessionId)
                                    prop.children [
                                        WorkflowCanvas.WorkflowCanvas(
                                            version,
                                            editorSessionId,
                                            workflow,
                                            workflowFilePath,
                                            activeStepIndex,
                                            setActiveStepIndex,
                                            (fun () -> setActiveOutputIndex None),
                                            commitMutation
                                        )
                                    ]
                                ]
                                WorkflowSteps.WorkflowSteps(
                                    workflowModel,
                                    activeStepId,
                                    activeStepInputId,
                                    activeStepOutputId,
                                    setActiveStepId,
                                    setActiveStepInputId,
                                    setActiveStepOutputId,
                                    onWorkflowChanged,
                                    onPreviewYamlText,
                                    setInfoMessage,
                                    setErrorMessage
                                )
                            ]
                        ]
                    ]
                ]
            ]
        ]
