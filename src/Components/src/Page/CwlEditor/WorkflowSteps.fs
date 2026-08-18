namespace Swate.Components.Page.CwlEditor

open System
open Fable.Core
open Feliz
open ARCtrl.CWL
open Swate.Components.Page.CwlEditor.Types
open Swate.Components.Shared.Cwl.WorkflowMutations

[<AutoOpen>]
module private WorkflowStepsHelpers =

    let runKindToValue runKind =
        match runKind with
        | RunStringKind -> "string"
        | RunCommandLineToolKind -> "command-line-tool"
        | RunWorkflowKind -> "workflow"
        | RunExpressionToolKind -> "expression-tool"
        | RunOperationKind -> "operation"

    let runKindFromValue value =
        match value with
        | "command-line-tool" -> RunCommandLineToolKind
        | "workflow" -> RunWorkflowKind
        | "expression-tool" -> RunExpressionToolKind
        | "operation" -> RunOperationKind
        | _ -> RunStringKind

    let eventTargetValue (ev: Browser.Types.FocusEvent) =
        let target = ev.target :?> Browser.Types.HTMLInputElement
        if isNull target then "" else target.value

[<Erase; Mangle(false)>]
type WorkflowSteps =

    [<ReactComponent>]
    static member WorkflowSteps
        (
            version: int,
            workflow: CWLWorkflowDescription,
            workflowFilePath: string option,
            activeStepIndex: int option,
            setActiveStepIndex: int option -> unit,
            commitMutation: (unit -> unit) -> unit,
            onPreviewYaml: string -> unit,
            setInfoMessage: string option -> unit,
            setErrorMessage: string option -> unit
        ) : ReactElement =
        let host =
            Context.useCwlEditorHostCtx ()
            |> Option.defaultWith (fun () -> failwith "WorkflowSteps requires a CwlEditorHost context.")

        let selectedStepInputIndex, setSelectedStepInputIndex =
            React.useState<int option> (None)

        let selectedStepOutputIndex, setSelectedStepOutputIndex =
            React.useState<int option> (None)

        let steps = workflow.Steps

        let stepDetails =
            match activeStepIndex with
            | Some stepIndex when stepIndex >= 0 && stepIndex < steps.Count ->
                let step = steps.[stepIndex]
                let currentRunKind = stepRunKind step
                let canEditRunTarget = isStepRunEditable step
                let runDetails = tryGetWorkflowStepRunDetails step
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

                let activeStepInputIndex =
                    match selectedStepInputIndex with
                    | Some index when index >= 0 && index < step.In.Count -> Some index
                    | _ when step.In.Count > 0 -> Some 0
                    | _ -> None

                let activeStepOutputIndex =
                    match selectedStepOutputIndex with
                    | Some index when index >= 0 && index < step.Out.Count -> Some index
                    | _ when step.Out.Count > 0 -> Some 0
                    | _ -> None

                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.h4 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text (sprintf "Step %d details" (stepIndex + 1))
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Step id" ]
                                Html.input [
                                    prop.testId (sprintf "cwl-workflow-step-id-%d" stepIndex)
                                    prop.key (sprintf "step-id-%d" stepIndex)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue step.Id
                                    prop.onBlur (fun ev ->
                                        let value = eventTargetValue ev
                                        commitMutation (fun () -> setWorkflowStepIdAt steps stepIndex value)
                                    )
                                ]
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                            prop.children [
                                Html.span [ prop.text "Run kind" ]
                                Html.select [
                                    prop.testId (sprintf "cwl-workflow-step-run-kind-%d" stepIndex)
                                    prop.className "swt:select swt:select-sm swt:w-full"
                                    prop.value (runKindToValue currentRunKind)
                                    prop.onChange (fun runKindValue ->
                                        let nextKind = runKindFromValue runKindValue
                                        commitMutation (fun () -> setWorkflowStepRunKindAt steps stepIndex nextKind)
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
                                    prop.testId (sprintf "cwl-workflow-step-run-%d" stepIndex)
                                    prop.key (sprintf "step-run-%d-%A" stepIndex currentRunKind)
                                    prop.className "swt:input swt:input-sm swt:w-full"
                                    prop.defaultValue (stepRunDisplay step)
                                    prop.placeholder "tool.cwl"
                                    prop.disabled (not canEditRunTarget)
                                    prop.onBlur (fun ev ->
                                        let value = eventTargetValue ev
                                        commitMutation (fun () -> setWorkflowStepRunAt steps stepIndex value)
                                    )
                                ]
                            ]
                        ]
                        match runDetails with
                        | Some details ->
                            Html.div [
                                prop.className "swt:alert"
                                prop.text (
                                    sprintf
                                        "Resolved run type: %s | Inputs: %s | Outputs: %s"
                                        details.KindLabel
                                        (if details.InputIds.Length = 0 then
                                             "(none)"
                                         else
                                             String.concat ", " details.InputIds)
                                        (if details.OutputIds.Length = 0 then
                                             "(none)"
                                         else
                                             String.concat ", " details.OutputIds)
                                )
                            ]
                        | None -> Html.none
                        Html.div [
                            prop.className "swt:flex swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.testId (sprintf "cwl-workflow-step-preview-run-%d" stepIndex)
                                    prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                    prop.text "Preview run"
                                    prop.onClick (fun _ -> previewStepRun ())
                                ]
                                Html.button [
                                    prop.testId (sprintf "cwl-workflow-step-save-run-%d" stepIndex)
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
                                        prop.testId (sprintf "cwl-workflow-step-save-run-as-copy-%d" stepIndex)
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
                        if not canEditRunTarget then
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
                                                    prop.testId (sprintf "cwl-workflow-step-input-add-%d" stepIndex)
                                                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                                    prop.text "Add"
                                                    prop.onClick (fun _ ->
                                                        commitMutation (fun () ->
                                                            let nextIndex = addWorkflowStepInputAt steps stepIndex
                                                            setSelectedStepInputIndex nextIndex
                                                        )
                                                    )
                                                ]
                                                Html.button [
                                                    prop.testId (sprintf "cwl-workflow-step-input-remove-%d" stepIndex)
                                                    prop.className "swt:btn swt:btn-sm swt:btn-error"
                                                    prop.text "Remove"
                                                    prop.disabled activeStepInputIndex.IsNone
                                                    prop.onClick (fun _ ->
                                                        commitMutation (fun () ->
                                                            let nextIndex =
                                                                removeWorkflowStepInputAt
                                                                    steps
                                                                    stepIndex
                                                                    activeStepInputIndex

                                                            setSelectedStepInputIndex nextIndex
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
                                        for index, stepInput in step.In |> Seq.indexed do
                                            let sourceText = stepInputSourceText stepInput

                                            let labelText =
                                                if sourceText = "" then
                                                    stepInput.Id
                                                else
                                                    sprintf "%s <- %s" stepInput.Id sourceText

                                            Html.li [
                                                prop.testId (
                                                    sprintf "cwl-workflow-step-input-item-%d-%d" stepIndex index
                                                )
                                                prop.key stepInput.Id
                                                prop.className [
                                                    if activeStepInputIndex = Some index then
                                                        "swt:menu-active"
                                                ]
                                                prop.onClick (fun _ -> setSelectedStepInputIndex (Some index))
                                                prop.text labelText
                                            ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "swt:flex swt:gap-2"
                                    prop.children [
                                        Html.button [
                                            prop.testId (sprintf "cwl-workflow-step-input-move-up-%d" stepIndex)
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Move up"
                                            prop.disabled (activeStepInputIndex.IsNone || activeStepInputIndex = Some 0)
                                            prop.onClick (fun _ ->
                                                commitMutation (fun () ->
                                                    let nextIndex =
                                                        moveWorkflowStepInputUp steps stepIndex activeStepInputIndex

                                                    setSelectedStepInputIndex nextIndex
                                                )
                                            )
                                        ]
                                        Html.button [
                                            prop.testId (sprintf "cwl-workflow-step-input-move-down-%d" stepIndex)
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Move down"
                                            prop.disabled (
                                                activeStepInputIndex.IsNone
                                                || activeStepInputIndex = Some(step.In.Count - 1)
                                            )
                                            prop.onClick (fun _ ->
                                                commitMutation (fun () ->
                                                    let nextIndex =
                                                        moveWorkflowStepInputDown steps stepIndex activeStepInputIndex

                                                    setSelectedStepInputIndex nextIndex
                                                )
                                            )
                                        ]
                                    ]
                                ]
                                match activeStepInputIndex with
                                | Some inputIndex ->
                                    let stepInput = step.In.[inputIndex]

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
                                                                stepIndex
                                                                inputIndex
                                                        )
                                                        prop.key (sprintf "step-input-id-%d-%d" stepIndex inputIndex)
                                                        prop.className "swt:input swt:input-sm swt:w-full"
                                                        prop.defaultValue stepInput.Id
                                                        prop.onBlur (fun ev ->
                                                            let value = eventTargetValue ev

                                                            commitMutation (fun () ->
                                                                setWorkflowStepInputIdAt
                                                                    steps
                                                                    stepIndex
                                                                    inputIndex
                                                                    value
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
                                                                stepIndex
                                                                inputIndex
                                                        )
                                                        prop.key (
                                                            sprintf "step-input-source-%d-%d" stepIndex inputIndex
                                                        )
                                                        prop.className "swt:input swt:input-sm swt:w-full"
                                                        prop.defaultValue (stepInputSourceText stepInput)
                                                        prop.placeholder "workflow_input, previous_step/out"
                                                        prop.onBlur (fun ev ->
                                                            let value = eventTargetValue ev

                                                            commitMutation (fun () ->
                                                                setWorkflowStepInputSourceAt
                                                                    steps
                                                                    stepIndex
                                                                    inputIndex
                                                                    value
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
                                                    prop.testId (sprintf "cwl-workflow-step-output-add-%d" stepIndex)
                                                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                                    prop.text "Add"
                                                    prop.onClick (fun _ ->
                                                        commitMutation (fun () ->
                                                            let nextIndex = addWorkflowStepOutputAt steps stepIndex
                                                            setSelectedStepOutputIndex nextIndex
                                                        )
                                                    )
                                                ]
                                                Html.button [
                                                    prop.testId (sprintf "cwl-workflow-step-output-remove-%d" stepIndex)
                                                    prop.className "swt:btn swt:btn-sm swt:btn-error"
                                                    prop.text "Remove"
                                                    prop.disabled activeStepOutputIndex.IsNone
                                                    prop.onClick (fun _ ->
                                                        commitMutation (fun () ->
                                                            let nextIndex =
                                                                removeWorkflowStepOutputAt
                                                                    steps
                                                                    stepIndex
                                                                    activeStepOutputIndex

                                                            setSelectedStepOutputIndex nextIndex
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
                                        for index, stepOutput in step.Out |> Seq.indexed do
                                            Html.li [
                                                prop.testId (
                                                    sprintf "cwl-workflow-step-output-item-%d-%d" stepIndex index
                                                )
                                                prop.key (stepOutputId stepOutput)
                                                prop.className [
                                                    if activeStepOutputIndex = Some index then
                                                        "swt:menu-active"
                                                ]
                                                prop.onClick (fun _ -> setSelectedStepOutputIndex (Some index))
                                                prop.text (stepOutputId stepOutput)
                                            ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "swt:flex swt:gap-2"
                                    prop.children [
                                        Html.button [
                                            prop.testId (sprintf "cwl-workflow-step-output-move-up-%d" stepIndex)
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Move up"
                                            prop.disabled (
                                                activeStepOutputIndex.IsNone || activeStepOutputIndex = Some 0
                                            )
                                            prop.onClick (fun _ ->
                                                commitMutation (fun () ->
                                                    let nextIndex =
                                                        moveWorkflowStepOutputUp steps stepIndex activeStepOutputIndex

                                                    setSelectedStepOutputIndex nextIndex
                                                )
                                            )
                                        ]
                                        Html.button [
                                            prop.testId (sprintf "cwl-workflow-step-output-move-down-%d" stepIndex)
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Move down"
                                            prop.disabled (
                                                activeStepOutputIndex.IsNone
                                                || activeStepOutputIndex = Some(step.Out.Count - 1)
                                            )
                                            prop.onClick (fun _ ->
                                                commitMutation (fun () ->
                                                    let nextIndex =
                                                        moveWorkflowStepOutputDown
                                                            steps
                                                            stepIndex
                                                            activeStepOutputIndex

                                                    setSelectedStepOutputIndex nextIndex
                                                )
                                            )
                                        ]
                                    ]
                                ]
                                match activeStepOutputIndex with
                                | Some outputIndex ->
                                    Html.label [
                                        prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                                        prop.children [
                                            Html.span [ prop.text "Output id" ]
                                            Html.input [
                                                prop.testId (
                                                    sprintf "cwl-workflow-step-output-id-%d-%d" stepIndex outputIndex
                                                )
                                                prop.key (sprintf "step-output-id-%d-%d" stepIndex outputIndex)
                                                prop.className "swt:input swt:input-sm swt:w-full"
                                                prop.defaultValue (stepOutputId step.Out.[outputIndex])
                                                prop.onBlur (fun ev ->
                                                    let value = eventTargetValue ev

                                                    commitMutation (fun () ->
                                                        setWorkflowStepOutputIdAt steps stepIndex outputIndex value
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
            | _ ->
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
                                        commitMutation (fun () ->
                                            let nextIndex = addWorkflowStep workflow
                                            setActiveStepIndex (Some nextIndex)
                                            setSelectedStepInputIndex None
                                            setSelectedStepOutputIndex None
                                        )
                                    )
                                ]
                                Html.button [
                                    prop.testId "cwl-workflow-step-remove"
                                    prop.className "swt:btn swt:btn-sm swt:btn-error"
                                    prop.text "Remove"
                                    prop.disabled activeStepIndex.IsNone
                                    prop.onClick (fun _ ->
                                        commitMutation (fun () ->
                                            let nextIndex = removeWorkflowStep activeStepIndex steps
                                            setActiveStepIndex nextIndex
                                            setSelectedStepInputIndex None
                                            setSelectedStepOutputIndex None
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
                        for index, step in steps |> Seq.indexed do
                            Html.li [
                                prop.testId (sprintf "cwl-workflow-step-item-%d" index)
                                prop.key step.Id
                                prop.className [
                                    if activeStepIndex = Some index then
                                        "swt:menu-active"
                                ]
                                prop.onClick (fun _ -> setActiveStepIndex (Some index))
                                prop.text (sprintf "%s -> %s" step.Id (stepRunDisplay step))
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
                            prop.disabled (activeStepIndex.IsNone || activeStepIndex = Some 0)
                            prop.onClick (fun _ ->
                                commitMutation (fun () ->
                                    let nextIndex = moveWorkflowStepUp activeStepIndex steps
                                    setActiveStepIndex nextIndex
                                )
                            )
                        ]
                        Html.button [
                            prop.testId "cwl-workflow-step-move-down"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Move down"
                            prop.disabled (activeStepIndex.IsNone || activeStepIndex = Some(steps.Count - 1))
                            prop.onClick (fun _ ->
                                commitMutation (fun () ->
                                    let nextIndex = moveWorkflowStepDown activeStepIndex steps
                                    setActiveStepIndex nextIndex
                                )
                            )
                        ]
                    ]
                ]
                stepDetails
            ]
        ]
