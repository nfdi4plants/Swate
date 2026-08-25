namespace Swate.Components.Page.CwlEditor

open System
open Fable.Core
open Feliz
open Swate.Components.Page.CwlEditor.Types
open Swate.Components.Shared.Cwl.Adapters.ArCtrlEncode
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.Documents.Workflow

[<AutoOpen>]
module private WorkflowStepsHelpers =

    let runKindToValue runKind =
        match runKind with
        | ExternalRunKind -> "string"
        | CommandLineToolRunKind -> "command-line-tool"
        | InlineWorkflowRunKind -> "workflow"
        | ExpressionToolRunKind -> "expression-tool"
        | OperationRunKind -> "operation"

    let runKindFromValue value =
        match value with
        | "command-line-tool" -> CommandLineToolRunKind
        | "workflow" -> InlineWorkflowRunKind
        | "expression-tool" -> ExpressionToolRunKind
        | "operation" -> OperationRunKind
        | _ -> ExternalRunKind

    let eventTargetValue (ev: Browser.Types.FocusEvent) =
        let target = ev.target :?> Browser.Types.HTMLInputElement
        if isNull target then "" else target.value

    let tryEncodeWorkflowStepRunYaml (step: WorkflowStepModel) =
        match step.Run with
        | ExternalRun _ -> None
        | InlineCommandLineTool tool ->
            CommandLineToolDoc tool
            |> toProcessingUnit
            |> ARCtrl.CWL.Encode.encodeProcessingUnit
            |> Some
        | InlineWorkflow workflow ->
            WorkflowDoc workflow
            |> toProcessingUnit
            |> ARCtrl.CWL.Encode.encodeProcessingUnit
            |> Some
        | InlineExpressionTool tool ->
            ExpressionToolDoc tool
            |> toProcessingUnit
            |> ARCtrl.CWL.Encode.encodeProcessingUnit
            |> Some
        | InlineOperation operation ->
            OperationDoc operation
            |> toProcessingUnit
            |> ARCtrl.CWL.Encode.encodeProcessingUnit
            |> Some

[<Erase; Mangle(false)>]
type WorkflowSteps =

    [<ReactComponent>]
    static member WorkflowSteps
        (
            workflow: WorkflowModel,
            activeStepId: StepId option,
            activeStepInputId: StepInputId option,
            activeStepOutputId: StepOutputId option,
            setActiveStepId: StepId option -> unit,
            setActiveStepInputId: StepInputId option -> unit,
            setActiveStepOutputId: StepOutputId option -> unit,
            onWorkflowChanged: WorkflowModel -> unit,
            onPreviewYaml: string -> unit,
            setInfoMessage: string option -> unit,
            setErrorMessage: string option -> unit
        ) : ReactElement =
        let host =
            Context.useCwlEditorHostCtx ()
            |> Option.defaultWith (fun () -> failwith "WorkflowSteps requires a CwlEditorHost context.")

        let activeStep =
            activeStepId
            |> Option.bind (fun stepId -> workflow.Steps |> List.tryFind (fun step -> step.Id = stepId))
            |> Option.orElseWith (fun () -> workflow.Steps |> List.tryHead)

        let activeStepId = activeStep |> Option.map (fun step -> step.Id)

        let updateWorkflow nextWorkflow = onWorkflowChanged nextWorkflow

        let activeStepIndex =
            activeStepId
            |> Option.bind (fun stepId -> workflow.Steps |> List.tryFindIndex (fun step -> step.Id = stepId))
            |> Option.defaultValue 0

        let stepDetails =
            match activeStep with
            | Some step ->
                let activeStepInput =
                    activeStepInputId
                    |> Option.bind (fun inputId -> step.Inputs |> List.tryFind (fun input -> input.Id = inputId))
                    |> Option.orElseWith (fun () -> step.Inputs |> List.tryHead)

                let activeStepOutput =
                    activeStepOutputId
                    |> Option.bind (fun outputId -> step.Outputs |> List.tryFind (fun output -> output.Id = outputId))
                    |> Option.orElseWith (fun () -> step.Outputs |> List.tryHead)

                let activeStepInputIndex =
                    activeStepInput
                    |> Option.bind (fun input -> step.Inputs |> List.tryFindIndex (fun item -> item.Id = input.Id))
                    |> Option.defaultValue 0

                let activeStepOutputIndex =
                    activeStepOutput
                    |> Option.bind (fun output -> step.Outputs |> List.tryFindIndex (fun item -> item.Id = output.Id))
                    |> Option.defaultValue 0

                let externalRunPath = tryGetWorkflowStepExternalRunAbsolutePath step

                let saveStepRunToPath (targetPath: string) =
                    match tryEncodeWorkflowStepRunYaml step with
                    | None -> setErrorMessage (Some "Only resolved inline step runs can be exported.")
                    | Some runYaml ->
                        setErrorMessage None

                        promise {
                            try
                                let! result = host.saveCwlFile targetPath runYaml

                                if result.Success then
                                    setInfoMessage (Some(sprintf "Saved step run to %s" result.FilePath))
                                else
                                    let errorText = result.Error |> Option.defaultValue "unknown error"
                                    setErrorMessage (Some(sprintf "Step save failed: %s" errorText))
                            with err ->
                                setErrorMessage (
                                    Some(
                                        sprintf
                                            "Step save failed: %s"
                                            (if isNull err then "unknown error" else string err)
                                    )
                                )
                        }
                        |> Promise.start

                let saveStepRunAsCopy () =
                    match host.pickSavePath with
                    | None -> ()
                    | Some pickSavePath ->
                        promise {
                            try
                                let! dialogResult = pickSavePath ()

                                if dialogResult.Canceled then
                                    ()
                                else
                                    match dialogResult.FilePath with
                                    | Some targetPath when String.IsNullOrWhiteSpace targetPath |> not ->
                                        saveStepRunToPath targetPath
                                    | _ -> ()
                            with err ->
                                setErrorMessage (
                                    Some(
                                        sprintf
                                            "Save copy dialog failed: %s"
                                            (if isNull err then "unknown error" else string err)
                                    )
                                )
                        }
                        |> Promise.start

                let previewStepRun () =
                    match tryEncodeWorkflowStepRunYaml step with
                    | Some yaml ->
                        setErrorMessage None
                        onPreviewYaml yaml
                    | None -> setErrorMessage (Some "Step run preview is only available for resolved inline content.")

                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.h4 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text (sprintf "Step %s details" step.Name)
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Step id" ]
                                Html.input [
                                    prop.testId (sprintf "cwl-workflow-step-id-%d" activeStepIndex)
                                    prop.key (sprintf "step-id-%O" step.Id)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue step.Name
                                    prop.onBlur (fun ev ->
                                        updateWorkflow (renameStep step.Id (eventTargetValue ev) workflow)
                                    )
                                ]
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Run kind" ]
                                Html.select [
                                    prop.testId (sprintf "cwl-workflow-step-run-kind-%d" activeStepIndex)
                                    prop.className "swt:select swt:select-sm swt:w-full"
                                    prop.value (stepRunKind step |> runKindToValue)
                                    prop.onChange (fun value ->
                                        updateWorkflow (setStepRunKind step.Id (runKindFromValue value) workflow)
                                    )
                                    prop.children [
                                        Html.option [
                                            prop.value "string"
                                            prop.text "String reference (.cwl path)"
                                        ]
                                        Html.option [
                                            prop.value "command-line-tool"
                                            prop.text "Inline CommandLineTool"
                                        ]
                                        Html.option [ prop.value "workflow"; prop.text "Inline Workflow" ]
                                        Html.option [
                                            prop.value "expression-tool"
                                            prop.text "Inline ExpressionTool"
                                        ]
                                        Html.option [ prop.value "operation"; prop.text "Inline Operation" ]
                                    ]
                                ]
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Run target" ]
                                Html.input [
                                    prop.testId (sprintf "cwl-workflow-step-run-%d" activeStepIndex)
                                    prop.key (
                                        sprintf "step-run-%d-%s" activeStepIndex (stepRunKind step |> runKindToValue)
                                    )
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue (stepRunDisplay step)
                                    prop.placeholder "tool.cwl"
                                    prop.disabled (not (isStepRunEditable step))
                                    prop.onBlur (fun ev ->
                                        updateWorkflow (setStepRunTarget step.Id (eventTargetValue ev) workflow)
                                    )
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "swt:flex swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.testId (sprintf "cwl-workflow-step-preview-run-%d" activeStepIndex)
                                    prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                    prop.text "Preview run"
                                    prop.onClick (fun _ -> previewStepRun ())
                                ]
                                Html.button [
                                    prop.testId (sprintf "cwl-workflow-step-save-run-%d" activeStepIndex)
                                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                    prop.text "Save run"
                                    prop.disabled externalRunPath.IsNone
                                    prop.onClick (fun _ ->
                                        match externalRunPath with
                                        | Some targetPath -> saveStepRunToPath targetPath
                                        | None -> ()
                                    )
                                ]
                                match host.pickSavePath with
                                | Some _ ->
                                    Html.button [
                                        prop.testId (sprintf "cwl-workflow-step-save-run-as-copy-%d" activeStepIndex)
                                        prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                        prop.text "Save run as copy"
                                        prop.onClick (fun _ -> saveStepRunAsCopy ())
                                    ]
                                | None -> Html.none
                            ]
                        ]
                        match externalRunPath with
                        | Some path ->
                            Html.p [
                                prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                                prop.text (sprintf "External run file: %s" path)
                            ]
                        | None -> Html.none
                        if not (isStepRunEditable step) then
                            Html.div [
                                prop.className "swt:alert"
                                prop.text
                                    "Inline step runs are read-only in this phase. Canvas/run-type editing follows in Phase 3."
                            ]
                        Html.div [
                            prop.className "swt:card swt:bg-base-200 swt:p-4"
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                                    prop.children [
                                        Html.h4 [
                                            prop.className "swt:font-semibold swt:text-base-content"
                                            prop.text "Step inputs"
                                        ]
                                        Html.div [
                                            prop.className "swt:flex swt:gap-2"
                                            prop.children [
                                                Html.button [
                                                    prop.testId (
                                                        sprintf "cwl-workflow-step-input-add-%d" activeStepIndex
                                                    )
                                                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                                    prop.text "Add"
                                                    prop.onClick (fun _ ->
                                                        let nextWorkflow, nextInputId = addStepInput step.Id workflow
                                                        updateWorkflow nextWorkflow
                                                        setActiveStepInputId (Some nextInputId)
                                                    )
                                                ]
                                                Html.button [
                                                    prop.testId (
                                                        sprintf "cwl-workflow-step-input-remove-%d" activeStepIndex
                                                    )
                                                    prop.className "swt:btn swt:btn-sm swt:btn-error"
                                                    prop.text "Remove"
                                                    prop.disabled activeStepInput.IsNone
                                                    prop.onClick (fun _ ->
                                                        activeStepInput
                                                        |> Option.iter (fun input ->
                                                            updateWorkflow (removeStepInput step.Id input.Id workflow)
                                                            setActiveStepInputId None
                                                        )
                                                    )
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                                Html.ul [
                                    prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                                    prop.children [
                                        for index, input in step.Inputs |> List.indexed do
                                            let sourceText = stepInputSourceText input

                                            let labelText =
                                                if sourceText = "" then
                                                    input.Name
                                                else
                                                    sprintf "%s <- %s" input.Name sourceText

                                            Html.li [
                                                prop.testId (
                                                    sprintf "cwl-workflow-step-input-item-%d-%d" activeStepIndex index
                                                )
                                                prop.key (sprintf "%O" input.Id)
                                                prop.className [
                                                    if
                                                        activeStepInput |> Option.map (fun item -> item.Id) = Some
                                                            input.Id
                                                    then
                                                        "swt:menu-active"
                                                ]
                                                prop.onClick (fun _ -> setActiveStepInputId (Some input.Id))
                                                prop.text labelText
                                            ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "swt:flex swt:gap-2"
                                    prop.children [
                                        Html.button [
                                            prop.testId (sprintf "cwl-workflow-step-input-move-up-%d" activeStepIndex)
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Move up"
                                            prop.disabled (activeStepInput.IsNone || activeStepInputIndex = 0)
                                            prop.onClick (fun _ ->
                                                activeStepInput
                                                |> Option.iter (fun input ->
                                                    updateWorkflow (moveStepInputUp step.Id input.Id workflow)
                                                )
                                            )
                                        ]
                                        Html.button [
                                            prop.testId (sprintf "cwl-workflow-step-input-move-down-%d" activeStepIndex)
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Move down"
                                            prop.disabled (
                                                activeStepInput.IsNone || activeStepInputIndex = step.Inputs.Length - 1
                                            )
                                            prop.onClick (fun _ ->
                                                activeStepInput
                                                |> Option.iter (fun input ->
                                                    updateWorkflow (moveStepInputDown step.Id input.Id workflow)
                                                )
                                            )
                                        ]
                                    ]
                                ]
                                match activeStepInput with
                                | Some input ->
                                    Html.div [
                                        prop.className "swt:flex swt:flex-col swt:gap-2"
                                        prop.children [
                                            Html.label [
                                                prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                                                prop.children [
                                                    Html.span [ prop.text "Input id" ]
                                                    Html.input [
                                                        prop.testId (
                                                            sprintf
                                                                "cwl-workflow-step-input-id-%d-%d"
                                                                activeStepIndex
                                                                activeStepInputIndex
                                                        )
                                                        prop.key (sprintf "step-input-id-%O" input.Id)
                                                        prop.className "swt:input swt:input-sm swt:w-full"
                                                        prop.defaultValue input.Name
                                                        prop.onBlur (fun ev ->
                                                            updateWorkflow (
                                                                renameStepInput
                                                                    step.Id
                                                                    input.Id
                                                                    (eventTargetValue ev)
                                                                    workflow
                                                            )
                                                        )
                                                    ]
                                                ]
                                            ]
                                            Html.label [
                                                prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                                                prop.children [
                                                    Html.span [ prop.text "Source (comma separated)" ]
                                                    Html.input [
                                                        prop.testId (
                                                            sprintf
                                                                "cwl-workflow-step-input-source-%d-%d"
                                                                activeStepIndex
                                                                activeStepInputIndex
                                                        )
                                                        prop.key (sprintf "step-input-source-%O" input.Id)
                                                        prop.className "swt:input swt:input-sm swt:w-full"
                                                        prop.defaultValue (stepInputSourceText input)
                                                        prop.placeholder "workflow_input, previous_step/out"
                                                        prop.onBlur (fun ev ->
                                                            updateWorkflow (
                                                                setStepInputSourceText
                                                                    step.Id
                                                                    input.Id
                                                                    (eventTargetValue ev)
                                                                    workflow
                                                            )
                                                        )
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                | None ->
                                    Html.p [
                                        prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                                        prop.text "Select a step input to edit details."
                                    ]
                            ]
                        ]
                        Html.div [
                            prop.className "swt:card swt:bg-base-200 swt:p-4"
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                                    prop.children [
                                        Html.h4 [
                                            prop.className "swt:font-semibold swt:text-base-content"
                                            prop.text "Step outputs"
                                        ]
                                        Html.div [
                                            prop.className "swt:flex swt:gap-2"
                                            prop.children [
                                                Html.button [
                                                    prop.testId (
                                                        sprintf "cwl-workflow-step-output-add-%d" activeStepIndex
                                                    )
                                                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                                    prop.text "Add"
                                                    prop.onClick (fun _ ->
                                                        let nextWorkflow, nextOutputId = addStepOutput step.Id workflow
                                                        updateWorkflow nextWorkflow
                                                        setActiveStepOutputId (Some nextOutputId)
                                                    )
                                                ]
                                                Html.button [
                                                    prop.testId (
                                                        sprintf "cwl-workflow-step-output-remove-%d" activeStepIndex
                                                    )
                                                    prop.className "swt:btn swt:btn-sm swt:btn-error"
                                                    prop.text "Remove"
                                                    prop.disabled activeStepOutput.IsNone
                                                    prop.onClick (fun _ ->
                                                        activeStepOutput
                                                        |> Option.iter (fun output ->
                                                            updateWorkflow (
                                                                removeStepOutput step.Id output.Id workflow
                                                            )

                                                            setActiveStepOutputId None
                                                        )
                                                    )
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                                Html.ul [
                                    prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                                    prop.children [
                                        for index, output in step.Outputs |> List.indexed do
                                            Html.li [
                                                prop.testId (
                                                    sprintf "cwl-workflow-step-output-item-%d-%d" activeStepIndex index
                                                )
                                                prop.key (sprintf "%O" output.Id)
                                                prop.className [
                                                    if
                                                        activeStepOutput |> Option.map (fun item -> item.Id) = Some
                                                            output.Id
                                                    then
                                                        "swt:menu-active"
                                                ]
                                                prop.onClick (fun _ -> setActiveStepOutputId (Some output.Id))
                                                prop.text output.Name
                                            ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "swt:flex swt:gap-2"
                                    prop.children [
                                        Html.button [
                                            prop.testId (sprintf "cwl-workflow-step-output-move-up-%d" activeStepIndex)
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Move up"
                                            prop.disabled (activeStepOutput.IsNone || activeStepOutputIndex = 0)
                                            prop.onClick (fun _ ->
                                                activeStepOutput
                                                |> Option.iter (fun output ->
                                                    updateWorkflow (moveStepOutputUp step.Id output.Id workflow)
                                                )
                                            )
                                        ]
                                        Html.button [
                                            prop.testId (
                                                sprintf "cwl-workflow-step-output-move-down-%d" activeStepIndex
                                            )
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Move down"
                                            prop.disabled (
                                                activeStepOutput.IsNone
                                                || activeStepOutputIndex = step.Outputs.Length - 1
                                            )
                                            prop.onClick (fun _ ->
                                                activeStepOutput
                                                |> Option.iter (fun output ->
                                                    updateWorkflow (moveStepOutputDown step.Id output.Id workflow)
                                                )
                                            )
                                        ]
                                    ]
                                ]
                                match activeStepOutput with
                                | Some output ->
                                    Html.label [
                                        prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                                        prop.children [
                                            Html.span [ prop.text "Output id" ]
                                            Html.input [
                                                prop.testId (
                                                    sprintf
                                                        "cwl-workflow-step-output-id-%d-%d"
                                                        activeStepIndex
                                                        activeStepOutputIndex
                                                )
                                                prop.key (sprintf "step-output-id-%O" output.Id)
                                                prop.className "swt:input swt:input-sm swt:w-full"
                                                prop.defaultValue output.Name
                                                prop.onBlur (fun ev ->
                                                    updateWorkflow (
                                                        renameStepOutput
                                                            step.Id
                                                            output.Id
                                                            (eventTargetValue ev)
                                                            workflow
                                                    )
                                                )
                                            ]
                                        ]
                                    ]
                                | None ->
                                    Html.p [
                                        prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                                        prop.text "Select a step output to edit details."
                                    ]
                            ]
                        ]
                    ]
                ]
            | None ->
                Html.p [
                    prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                    prop.text "Select a step to edit details."
                ]

        Html.section [
            prop.testId "cwl-workflow-steps"
            prop.className "swt:card swt:bg-base-200 swt:p-4"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                    prop.children [
                        Html.h3 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text "Workflow Steps"
                        ]
                        Html.div [
                            prop.className "swt:flex swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.testId "cwl-workflow-step-add"
                                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                    prop.text "Add"
                                    prop.onClick (fun _ ->
                                        let nextWorkflow, nextStepId = addStep workflow
                                        updateWorkflow nextWorkflow
                                        setActiveStepId (Some nextStepId)
                                        setActiveStepInputId None
                                        setActiveStepOutputId None
                                    )
                                ]
                                Html.button [
                                    prop.testId "cwl-workflow-step-remove"
                                    prop.className "swt:btn swt:btn-sm swt:btn-error"
                                    prop.text "Remove"
                                    prop.disabled activeStepId.IsNone
                                    prop.onClick (fun _ ->
                                        activeStepId
                                        |> Option.iter (fun stepId ->
                                            updateWorkflow (removeStep stepId workflow)
                                            setActiveStepId None
                                            setActiveStepInputId None
                                            setActiveStepOutputId None
                                        )
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
                Html.ul [
                    prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                    prop.children [
                        for index, step in workflow.Steps |> List.indexed do
                            Html.li [
                                prop.testId (sprintf "cwl-workflow-step-item-%d" index)
                                prop.key (sprintf "%O" step.Id)
                                prop.className [
                                    if activeStepId = Some step.Id then
                                        "swt:menu-active"
                                ]
                                prop.onClick (fun _ ->
                                    setActiveStepId (Some step.Id)
                                    setActiveStepInputId None
                                    setActiveStepOutputId None
                                )
                                prop.text (sprintf "%s -> %s" step.Name (stepRunDisplay step))
                            ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.testId "cwl-workflow-step-move-up"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Move up"
                            prop.disabled activeStepId.IsNone
                            prop.onClick (fun _ ->
                                activeStepId
                                |> Option.iter (fun stepId -> updateWorkflow (moveStepUp stepId workflow))
                            )
                        ]
                        Html.button [
                            prop.testId "cwl-workflow-step-move-down"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Move down"
                            prop.disabled activeStepId.IsNone
                            prop.onClick (fun _ ->
                                activeStepId
                                |> Option.iter (fun stepId -> updateWorkflow (moveStepDown stepId workflow))
                            )
                        ]
                    ]
                ]
                stepDetails
            ]
        ]
