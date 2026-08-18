namespace Swate.Components.Page.CwlEditor

open Fable.Core
open Feliz
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.CwlDefaults
open Swate.Components.Shared.Cwl.Validation.ValidationTypes
open Swate.Components.Shared.Cwl.WorkflowMutations

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
            inputs: ResizeArray<CWLInput>,
            outputs: ResizeArray<CWLOutput>,
            activeInputIndex: int option,
            activeOutputIndex: int option,
            activeStepIndex: int option,
            requirements: ResizeArray<Requirement> option,
            hints: ResizeArray<HintEntry> option,
            validationResult: ValidationResult,
            commitMutation: (unit -> unit) -> unit,
            setActiveInputIndex: int option -> unit,
            setActiveOutputIndex: int option -> unit,
            setActiveStepIndex: int option -> unit,
            onPreview: unit -> unit,
            onSave: unit -> unit,
            onBackToStart: unit -> unit,
            onSetVersion: string -> unit,
            onSetIntent: string -> unit,
            onSetRequirementEnabled: string -> bool -> unit,
            onSetHintEnabled: string -> bool -> unit,
            onSetRequirementField: string -> string -> string -> unit,
            onSetHintField: string -> string -> string -> unit,
            onPreviewYamlText: string -> unit,
            setInfoMessage: string option -> unit,
            setErrorMessage: string option -> unit
        ) : ReactElement =
        let focusedRequirement, setFocusedRequirement =
            React.useState<RequirementFocus option> (None)

        let clearFocusedRequirement () = setFocusedRequirement None

        let setEnabled bucket key isEnabled =
            match bucket with
            | RequirementBucket -> onSetRequirementEnabled key isEnabled
            | HintBucket -> onSetHintEnabled key isEnabled

        let setField bucket key fieldKey value =
            match bucket with
            | RequirementBucket -> onSetRequirementField key fieldKey value
            | HintBucket -> onSetHintField key fieldKey value

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
                                RequirementPicker.RequirementSidebarPanel(
                                    version,
                                    {
                                        Requirements = requirements
                                        Hints = hints
                                        Focused = focusedRequirement
                                        OnFocus = setFocusedRequirement
                                        OnSetEnabled = setEnabled
                                        OnSetField = setField
                                    }
                                )
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
                                    commitMutation,
                                    (fun () -> addWorkflowInput workflow),
                                    onInteract = clearFocusedRequirement
                                )
                                RequirementPicker.RequirementMainPanel(
                                    version,
                                    {
                                        Requirements = requirements
                                        Hints = hints
                                        Focused = focusedRequirement
                                        OnFocus = setFocusedRequirement
                                        OnSetEnabled = setEnabled
                                    }
                                )
                                OutputsEditor.OutputsEditor(
                                    version,
                                    outputs,
                                    activeOutputIndex,
                                    setActiveOutputIndex,
                                    commitMutation,
                                    (fun () -> addWorkflowOutput workflow),
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
                                    version,
                                    workflow,
                                    workflowFilePath,
                                    activeStepIndex,
                                    setActiveStepIndex,
                                    commitMutation,
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
