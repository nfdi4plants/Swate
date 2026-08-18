namespace Swate.Components.Page.CwlEditor

open System
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open ARCtrl.CWL
open Swate.Components.Page.CwlEditor.EditorController
open Swate.Components.Shared.Cwl.CommandLineToolMutations
open Swate.Components.Shared.Cwl.CwlService
open Swate.Components.Shared.Cwl.EditorControllerLogic
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.ExpressionToolMutations
open Swate.Components.Shared.Cwl.HostTypes
open Swate.Components.Shared.Cwl.Validation.ValidationContext
open Swate.Components.Shared.Cwl.Validation.ValidationEngine
open Swate.Components.Shared.Cwl.WorkflowMutations

[<AutoOpen>]
module private CwlEditorHelpers =

    let clampIndex (selectedIndex: int option) (count: int) =
        match selectedIndex with
        | Some index when index >= 0 && index < count -> Some index
        | _ when count > 0 -> Some 0
        | _ -> None

    let eventTargetValue (ev: Browser.Types.FocusEvent) =
        let target = ev.target :?> Browser.Types.HTMLInputElement
        if isNull target then "" else target.value

[<Erase; Mangle(false)>]
type CwlEditor =

    [<ReactComponent>]
    static member private Editor
        (initialFile: LoadCwlResponse option, onDirtyChange: (bool -> unit) option)
        : ReactElement =
        let initialEditorState, initialLoadError =
            match initialFile with
            | Some fileResult ->
                match tryCreateLoadedState fileResult with
                | Ok loadedState -> Some loadedState, None
                | Error message -> None, Some message
            | None -> None, None

        let editorState, setEditorState =
            React.useState<EditorState option> (initialEditorState)

        let editorSessionId, setEditorSessionId = React.useState (0)
        let errorMsg, setErrorMsg = React.useState<string option> (None)
        let infoMsg, setInfoMsg = React.useState<string option> (None)
        let isLoading, setIsLoading = React.useState (false)
        let isSaving, setIsSaving = React.useState (false)
        let selectedInputIndex, setSelectedInputIndex = React.useState<int option> (None)
        let selectedOutputIndex, setSelectedOutputIndex = React.useState<int option> (None)
        let selectedStepIndex, setSelectedStepIndex = React.useState<int option> (None)
        let previewYaml, setPreviewYaml = React.useState<string option> (None)
        let showDiscardPrompt, setShowDiscardPrompt = React.useState (false)
        let latestEditorStateRef = React.useRef (editorState)
        let nextEditorSessionIdRef = React.useRef (0)

        let host =
            Context.useCwlEditorHostCtx ()
            |> Option.defaultWith (fun () -> failwith "CwlEditor requires a CwlEditorHost context.")

        latestEditorStateRef.current <- editorState

        let isDirty =
            editorState
            |> Option.map (fun state -> state.IsDirty)
            |> Option.defaultValue false

        React.useEffect (
            (fun () ->
                match onDirtyChange with
                | Some callback -> callback isDirty
                | None -> ()

                fun () -> ()
            ),
            [| box isDirty |]
        )

        let beginEditorSession () =
            nextEditorSessionIdRef.current <- nextEditorSessionIdRef.current + 1
            setEditorSessionId nextEditorSessionIdRef.current

        let validationResult =
            React.useMemo (
                (fun () ->
                    match editorState with
                    | Some state -> Some(validateProcessingUnit state.ProcessingUnit Live)
                    | None -> None
                ),
                [| box editorState |]
            )

        let resetEditorSelection =
            React.useCallback (
                (fun () ->
                    setSelectedInputIndex None
                    setSelectedOutputIndex None
                    setSelectedStepIndex None
                ),
                [||]
            )

        let tryLeaveEditor =
            React.useCallback (
                (fun (state: EditorState) ->
                    if state.IsDirty then
                        setShowDiscardPrompt true
                    else
                        resetEditorSelection ()
                        setEditorState None
                        setInfoMsg None
                        setErrorMsg None
                        setPreviewYaml None
                ),
                [| box resetEditorSelection |]
            )

        let controllerCallbacks: ControllerCallbacks =
            React.useMemo (
                (fun () -> {
                    ResetEditorSelection = resetEditorSelection
                    SetEditorState =
                        (fun nextState ->
                            let shouldBeginSession =
                                match latestEditorStateRef.current, nextState with
                                | None, Some _ -> true
                                | Some currentState, Some nextEditorState ->
                                    Object.ReferenceEquals(
                                        currentState.ProcessingUnit,
                                        nextEditorState.ProcessingUnit
                                    )
                                    |> not
                                | _ -> false

                            setEditorState nextState

                            if shouldBeginSession then
                                beginEditorSession ()
                        )
                    SetErrorMessage = setErrorMsg
                    SetInfoMessage = setInfoMsg
                    SetIsLoading = setIsLoading
                    SetIsSaving = setIsSaving
                    GetLatestEditorState = (fun () -> latestEditorStateRef.current)
                }),
                [| box resetEditorSelection |]
            )

        let runLoadCwl =
            React.useCallback (
                (fun () -> handleLoadCwl host controllerCallbacks ()),
                [| box host; box controllerCallbacks |]
            )

        let runSaveCwl =
            React.useCallback (
                (fun (state: EditorState) -> handleSaveCwl host controllerCallbacks state),
                [| box host; box controllerCallbacks |]
            )

        let runPreviewCwl =
            React.useCallback (
                (fun () ->
                    match latestEditorStateRef.current with
                    | Some currentState ->
                        let yaml =
                            match currentState.FilePath with
                            | Some filePath -> saveFromEditorForPath currentState filePath
                            | None -> saveFromEditor currentState

                        setPreviewYaml (Some yaml)
                    | None -> ()
                ),
                [||]
            )

        let previewOverlay =
            match previewYaml with
            | Some yaml ->
                Html.div [
                    prop.className
                        "swt:fixed swt:inset-0 swt:bg-black/50 swt:z-50 swt:flex swt:items-center swt:justify-center"
                    prop.onClick (fun _ -> setPreviewYaml None)
                    prop.children [
                        Html.section [
                            prop.className
                                "swt:bg-base-100 swt:rounded-box swt:shadow-xl swt:p-4 swt:max-w-3xl swt:w-full swt:max-h-[90vh] swt:flex swt:flex-col swt:gap-4"
                            prop.onClick (fun e -> e.stopPropagation ())
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                                    prop.children [
                                        Html.h3 [
                                            prop.className "swt:font-semibold swt:text-base-content"
                                            prop.text "CWL Preview"
                                        ]
                                        Html.button [
                                            prop.testId "cwl-preview-close"
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Close"
                                            prop.onClick (fun _ -> setPreviewYaml None)
                                        ]
                                    ]
                                ]
                                Html.pre [
                                    prop.className
                                        "swt:whitespace-pre swt:font-mono swt:text-sm swt:overflow-auto swt:flex-1 swt:min-h-0"
                                    prop.text yaml
                                ]
                            ]
                        ]
                    ]
                ]
            | None -> Html.none

        let discardOverlay =
            if showDiscardPrompt then
                Html.div [
                    prop.className
                        "swt:fixed swt:inset-0 swt:bg-black/50 swt:z-50 swt:flex swt:items-center swt:justify-center"
                    prop.onClick (fun _ -> setShowDiscardPrompt false)
                    prop.children [
                        Html.section [
                            prop.className
                                "swt:bg-base-100 swt:rounded-box swt:shadow-xl swt:p-4 swt:max-w-3xl swt:w-full swt:flex swt:flex-col swt:gap-4"
                            prop.onClick (fun e -> e.stopPropagation ())
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2"
                                    prop.children [
                                        Html.h3 [
                                            prop.className "swt:font-semibold swt:text-base-content"
                                            prop.text "Discard Unsaved Changes?"
                                        ]
                                    ]
                                ]
                                Html.p [
                                    prop.className "swt:text-base-content"
                                    prop.text "You have unsaved changes. Discard them and return to Start?"
                                ]
                                Html.div [
                                    prop.className "swt:flex swt:gap-2"
                                    prop.children [
                                        Html.button [
                                            prop.testId "cwl-discard-cancel"
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Cancel"
                                            prop.onClick (fun _ -> setShowDiscardPrompt false)
                                        ]
                                        Html.button [
                                            prop.testId "cwl-discard-confirm"
                                            prop.className "swt:btn swt:btn-sm swt:btn-error"
                                            prop.text "Discard"
                                            prop.onClick (fun _ ->
                                                setShowDiscardPrompt false
                                                resetEditorSelection ()
                                                setEditorState None
                                                setInfoMsg None
                                                setErrorMsg None
                                                setPreviewYaml None
                                            )
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            else
                Html.none

        let wrapEditorView (editorView: ReactElement) =
            Html.div [
                prop.key (string editorSessionId)
                prop.children [ editorView; previewOverlay; discardOverlay ]
            ]

        let tryTouchCurrentEditor (processingUnit: CWLProcessingUnit) =
            match latestEditorStateRef.current with
            | Some currentState when Object.ReferenceEquals(currentState.ProcessingUnit, processingUnit) ->
                setEditorState (Some(touch currentState))
            | _ -> ()

        let commitMutation (mutate: unit -> unit) =
            mutate ()
            setInfoMsg None

            match editorState with
            | Some state -> tryTouchCurrentEditor state.ProcessingUnit
            | None -> ()

        let setVersion (version: string) =
            match editorState with
            | Some state ->
                setProcessingUnitVersion version state.ProcessingUnit
                setInfoMsg None

                match latestEditorStateRef.current with
                | Some currentState when Object.ReferenceEquals(currentState.ProcessingUnit, state.ProcessingUnit) ->
                    let nextState = touch currentState
                    setEditorState (Some { nextState with CwlVersion = version })
                | _ -> ()
            | None -> ()

        match initialLoadError with
        | Some message ->
            Html.div [
                prop.testId "cwl-editor-initial-load-error"
                prop.className "swt:alert swt:alert-error"
                prop.text message
            ]
        | None ->
            match editorState with
            | None ->
                StartScreen.StartScreen(
                    0,
                    errorMsg,
                    isLoading,
                    (fun () -> setErrorMsg None),
                    (fun kind ->
                        resetEditorSelection ()
                        setPreviewYaml None
                        setErrorMsg None
                        setInfoMsg None
                        let active: obj = document.activeElement

                        if not (isNull active) && not (isNullOrUndefined active?blur) then
                            active?blur ()

                        window.setTimeout ((fun _ -> controllerCallbacks.SetEditorState(Some(createNew kind))), 0)
                        |> ignore
                    ),
                    runLoadCwl
                )
            | Some state ->
                let kindLabel =
                    match state.ProcessingUnit with
                    | CWLProcessingUnit.CommandLineTool _ -> "CommandLineTool"
                    | CWLProcessingUnit.Workflow _ -> "Workflow"
                    | CWLProcessingUnit.ExpressionTool _ -> "ExpressionTool"
                    | CWLProcessingUnit.Operation _ -> "Operation"

                let fileLabel =
                    match state.FilePath with
                    | Some path -> path
                    | None -> "unsaved.cwl"

                let currentValidationResult =
                    validationResult
                    |> Option.defaultWith (fun () -> validateProcessingUnit state.ProcessingUnit Live)

                match state.ProcessingUnit with
                | CWLProcessingUnit.CommandLineTool tool ->
                    let baseCommandValue =
                        tool.BaseCommand
                        |> Option.bind (fun commands -> if commands.Count > 0 then Some commands.[0] else None)
                        |> Option.defaultValue ""

                    let inputs = CWLToolDescription.getInputsOrEmpty tool
                    let outputs = tool.Outputs
                    let activeInputIndex = clampIndex selectedInputIndex inputs.Count
                    let activeOutputIndex = clampIndex selectedOutputIndex outputs.Count

                    CommandLineToolEditor.CommandLineToolEditor(
                        state.Version,
                        kindLabel,
                        fileLabel,
                        state.IsDirty,
                        isSaving,
                        errorMsg,
                        infoMsg,
                        state.CwlVersion,
                        intentText tool.Intent,
                        baseCommandValue,
                        tool,
                        inputs,
                        outputs,
                        activeInputIndex,
                        activeOutputIndex,
                        tool.Requirements,
                        tool.Hints,
                        currentValidationResult,
                        commitMutation,
                        setSelectedInputIndex,
                        setSelectedOutputIndex,
                        runPreviewCwl,
                        (fun () -> runSaveCwl state),
                        (fun () -> tryLeaveEditor state),
                        setVersion,
                        (fun value -> commitMutation (fun () -> tool.Intent <- parseIntentText value)),
                        (fun command -> commitMutation (fun () -> setBaseCommand tool command)),
                        (fun key isChecked -> commitMutation (fun () -> setRequirementEnabled tool key isChecked)),
                        (fun key isChecked -> commitMutation (fun () -> setHintEnabled tool key isChecked)),
                        (fun key field value -> commitMutation (fun () -> setRequirementField tool key field value)),
                        (fun key field value -> commitMutation (fun () -> setHintField tool key field value))
                    )
                    |> wrapEditorView

                | CWLProcessingUnit.Workflow workflow ->
                    let inputs = workflow.Inputs
                    let outputs = workflow.Outputs
                    let steps = workflow.Steps
                    let activeInputIndex = clampIndex selectedInputIndex inputs.Count
                    let activeOutputIndex = clampIndex selectedOutputIndex outputs.Count
                    let activeStepIndex = clampIndex selectedStepIndex steps.Count

                    WorkflowEditor.WorkflowEditor(
                        state.Version,
                        editorSessionId,
                        kindLabel,
                        fileLabel,
                        state.FilePath,
                        state.IsDirty,
                        isSaving,
                        errorMsg,
                        infoMsg,
                        state.CwlVersion,
                        intentText workflow.Intent,
                        workflow,
                        inputs,
                        outputs,
                        activeInputIndex,
                        activeOutputIndex,
                        activeStepIndex,
                        workflow.Requirements,
                        workflow.Hints,
                        currentValidationResult,
                        commitMutation,
                        setSelectedInputIndex,
                        setSelectedOutputIndex,
                        setSelectedStepIndex,
                        runPreviewCwl,
                        (fun () -> runSaveCwl state),
                        (fun () -> tryLeaveEditor state),
                        setVersion,
                        (fun value -> commitMutation (fun () -> workflow.Intent <- parseIntentText value)),
                        (fun key isChecked ->
                            commitMutation (fun () -> setWorkflowRequirementEnabled workflow key isChecked)
                        ),
                        (fun key isChecked -> commitMutation (fun () -> setWorkflowHintEnabled workflow key isChecked)),
                        (fun key field value ->
                            commitMutation (fun () -> setWorkflowRequirementField workflow key field value)
                        ),
                        (fun key field value ->
                            commitMutation (fun () -> setWorkflowHintField workflow key field value)
                        ),
                        (fun yaml -> setPreviewYaml (Some yaml)),
                        setInfoMsg,
                        setErrorMsg
                    )
                    |> wrapEditorView

                | CWLProcessingUnit.ExpressionTool tool ->
                    let inputs = CWLExpressionToolDescription.getInputsOrEmpty tool
                    let outputs = tool.Outputs
                    let activeInputIndex = clampIndex selectedInputIndex inputs.Count
                    let activeOutputIndex = clampIndex selectedOutputIndex outputs.Count

                    ExpressionToolEditor.ExpressionToolEditor(
                        state.Version,
                        kindLabel,
                        fileLabel,
                        state.IsDirty,
                        isSaving,
                        errorMsg,
                        infoMsg,
                        state.CwlVersion,
                        intentText tool.Intent,
                        tool.Expression,
                        tool,
                        inputs,
                        outputs,
                        activeInputIndex,
                        activeOutputIndex,
                        tool.Requirements,
                        tool.Hints,
                        currentValidationResult,
                        commitMutation,
                        setSelectedInputIndex,
                        setSelectedOutputIndex,
                        runPreviewCwl,
                        (fun () -> runSaveCwl state),
                        (fun () -> tryLeaveEditor state),
                        setVersion,
                        (fun value -> commitMutation (fun () -> tool.Intent <- parseIntentText value)),
                        (fun expression -> commitMutation (fun () -> setExpressionText tool expression)),
                        (fun key isChecked ->
                            commitMutation (fun () -> setExpressionRequirementEnabled tool key isChecked)
                        ),
                        (fun key isChecked -> commitMutation (fun () -> setExpressionHintEnabled tool key isChecked)),
                        (fun key field value ->
                            commitMutation (fun () -> setExpressionRequirementField tool key field value)
                        ),
                        (fun key field value -> commitMutation (fun () -> setExpressionHintField tool key field value))
                    )
                    |> wrapEditorView

                | CWLProcessingUnit.Operation operation ->
                    Html.div [
                        prop.key (string editorSessionId)
                        prop.testId "cwl-operation-editor"
                        prop.className "swt:flex swt:flex-col swt:h-full swt:min-h-0"
                        prop.children [
                            Html.main [
                                prop.className
                                    "swt:grid swt:grid-cols-[360px_1fr] swt:gap-4 swt:flex-1 swt:h-full swt:min-h-0 swt:p-4"
                                prop.children [
                                    Html.section [
                                        prop.className "swt:min-h-0"
                                        prop.children [
                                            Html.div [
                                                prop.className "swt:card swt:bg-base-200 swt:p-4"
                                                prop.children [
                                                    Html.h3 [
                                                        prop.className "swt:font-semibold swt:text-base-content"
                                                        prop.text "Operation Editing Is Not Implemented Yet"
                                                    ]
                                                    Html.p [
                                                        prop.className "swt:text-base-content"
                                                        prop.text
                                                            "This CWL document type is supported by ARCtrl beta18 and can be loaded/saved, but the editor UI currently only supports CommandLineTool, Workflow, and ExpressionTool."
                                                    ]
                                                    Html.p [
                                                        prop.className "swt:text-base-content"
                                                        prop.text (
                                                            sprintf "Current cwlVersion: %s" operation.CWLVersion
                                                        )
                                                    ]
                                                    Html.label [
                                                        prop.className
                                                            "swt:label swt:flex-col swt:items-start swt:gap-1"
                                                        prop.children [
                                                            Html.span [
                                                                prop.className "swt:text-sm"
                                                                prop.text "Intent (comma separated)"
                                                            ]
                                                            Html.input [
                                                                prop.testId "cwl-operation-intent"
                                                                prop.className "swt:input swt:input-sm swt:w-full"
                                                                prop.defaultValue (intentText operation.Intent)
                                                                prop.placeholder "service, orchestration"
                                                                prop.onBlur (fun ev ->
                                                                    commitMutation (fun () ->
                                                                        operation.Intent <-
                                                                            parseIntentText (eventTargetValue ev)
                                                                    )
                                                                )
                                                            ]
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "swt:flex swt:gap-2"
                                                        prop.children [
                                                            Html.button [
                                                                prop.testId "cwl-operation-preview"
                                                                prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                                                prop.text "Preview"
                                                                prop.onClick (fun _ -> runPreviewCwl ())
                                                            ]
                                                            Html.button [
                                                                prop.testId "cwl-operation-save"
                                                                prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                                                prop.text "Save"
                                                                prop.onClick (fun _ -> runSaveCwl state)
                                                            ]
                                                            Html.button [
                                                                prop.testId "cwl-operation-back"
                                                                prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                                                prop.text "Back"
                                                                prop.onClick (fun _ -> tryLeaveEditor state)
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                            previewOverlay
                        ]
                    ]

    [<ReactComponent(true)>]
    static member CwlEditor
        (
            ?initialFile: Swate.Components.Shared.Cwl.HostTypes.LoadCwlResponse,
            ?host: Types.CwlEditorHost,
            ?onDirtyChange: bool -> unit
        ) : ReactElement =
        let editor = CwlEditor.Editor(initialFile, onDirtyChange)

        match host with
        | Some providedHost -> Context.CwlEditorHostCtx.Provider(Some providedHost, editor)
        | None -> editor
