namespace Swate.Components.Page.CwlEditor

open Browser.Dom
open Fable.Core
open Feliz
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.Adapters.ArCtrlDecode
open Swate.Components.Shared.Cwl.Adapters.ArCtrlEncode
open Swate.Components.Shared.Cwl.Adapters.ValidationAdapter
open Swate.Components.Shared.Cwl.CommandLineToolMutations
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.EditorControllerLogic
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.ExpressionToolMutations
open Swate.Components.Shared.Cwl.HostTypes
open Swate.Components.Shared.Cwl.State.Actions
open Swate.Components.Shared.Cwl.State.EffectRunner
open Swate.Components.Shared.Cwl.State.Init
open Swate.Components.Shared.Cwl.State.Reducer
open Swate.Components.Shared.Cwl.State.Selectors
open Swate.Components.Shared.Cwl.State.Types
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

    let currentVersionNumber (state: AppState) =
        let (Revision value) = currentRevision state
        value

    let currentCwlVersion (document: Swate.Components.Shared.Cwl.Documents.Types.EditorDocument) =
        match document with
        | Swate.Components.Shared.Cwl.Documents.Types.CommandLineToolDoc model -> model.CwlVersion
        | Swate.Components.Shared.Cwl.Documents.Types.WorkflowDoc model -> model.CwlVersion
        | Swate.Components.Shared.Cwl.Documents.Types.ExpressionToolDoc model -> model.CwlVersion
        | Swate.Components.Shared.Cwl.Documents.Types.OperationDoc model -> model.CwlVersion

    let initialAppState (initialFile: LoadCwlResponse option) =
        match initialFile with
        | Some fileResult ->
            match tryCreateLoadedState fileResult with
            | Ok loadedState ->
                {
                    emptyState with
                        Document = Some(fromProcessingUnit loadedState.ProcessingUnit)
                        Meta =
                            Some {
                                DocumentId = newDocumentId ()
                                Revision = Revision 0
                                SavedRevision = Revision 0
                                FilePath = loadedState.FilePath
                            }
                        SessionId = 1
                },
                None
            | Error message -> emptyState, Some message
        | None -> emptyState, None

module private CwlEditorPorts =

    let liveTimerPort: TimerPort = {
        SetTimeout = fun delay callback -> window.setTimeout ((fun _ -> callback ()), delay)
        ClearTimeout = fun handle -> window.clearTimeout handle
    }

    let canceledDialog () = promise { return { Canceled = true; FilePath = None } }

[<Erase; Mangle(false)>]
type CwlEditor =

    [<ReactComponent>]
    static member private Editor
        (initialFile: LoadCwlResponse option, onDirtyChange: (bool -> unit) option)
        : ReactElement =
        let initialState, initialLoadError = initialAppState initialFile

        let state, dispatch =
            React.useReducer ((fun currentState action -> update action currentState |> fst), initialState)

        let selectedInputIndex, setSelectedInputIndex = React.useState<int option> None
        let selectedOutputIndex, setSelectedOutputIndex = React.useState<int option> None
        let selectedStepIndex, setSelectedStepIndex = React.useState<int option> None

        let host =
            Context.useCwlEditorHostCtx ()
            |> Option.defaultWith (fun () -> failwith "CwlEditor requires a CwlEditorHost context.")

        let isDirty = isDirty state

        let resetEditorSelection =
            React.useCallback (
                (fun () ->
                    setSelectedInputIndex None
                    setSelectedOutputIndex None
                    setSelectedStepIndex None
                ),
                [||]
            )

        React.useEffect (
            (fun () ->
                resetEditorSelection ()
                fun () -> ()
            ),
            [| box state.SessionId |]
        )

        React.useEffect (
            (fun () ->
                match onDirtyChange with
                | Some callback -> callback isDirty
                | None -> ()

                fun () -> ()
            ),
            [| box isDirty |]
        )

        let hostApi: CwlHostApi = {
            ShowOpenDialog =
                fun () ->
                    match host.pickOpenFile with
                    | Some pickOpenFile -> pickOpenFile ()
                    | None -> CwlEditorPorts.canceledDialog ()
            ShowSaveDialog =
                fun () ->
                    match host.pickSavePath with
                    | Some pickSavePath -> pickSavePath ()
                    | None ->
                        match currentFilePath state with
                        | Some filePath -> promise {
                            return {
                                Canceled = false
                                FilePath = Some filePath
                            }
                          }
                        | None -> CwlEditorPorts.canceledDialog ()
            LoadCwlFile = host.loadCwlFile
            SaveCwlFile = host.saveCwlFile
        }

        let ports =
            React.useMemo (
                (fun () -> {
                    HostApi = hostApi
                    Timers = CwlEditorPorts.liveTimerPort
                }),
                [| box host; box (currentFilePath state) |]
            )

        React.useEffect (
            (fun () ->
                state.PendingEffects |> List.iter (run ports dispatch)
                fun () -> ()
            ),
            [| box state.PendingEffects |]
        )

        let previewOverlay =
            match state.Overlay with
            | PreviewYaml yaml ->
                Html.div [
                    prop.className
                        "swt:fixed swt:inset-0 swt:bg-black/50 swt:z-50 swt:flex swt:items-center swt:justify-center"
                    prop.onClick (fun _ -> dispatch PreviewClosed)
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
                                            prop.onClick (fun _ -> dispatch PreviewClosed)
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
            | _ -> Html.none

        let discardOverlay =
            match state.Overlay with
            | ConfirmDiscard ->
                Html.div [
                    prop.className
                        "swt:fixed swt:inset-0 swt:bg-black/50 swt:z-50 swt:flex swt:items-center swt:justify-center"
                    prop.onClick (fun _ -> dispatch DiscardCancelled)
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
                                            prop.onClick (fun _ -> dispatch DiscardCancelled)
                                        ]
                                        Html.button [
                                            prop.testId "cwl-discard-confirm"
                                            prop.className "swt:btn swt:btn-sm swt:btn-error"
                                            prop.text "Discard"
                                            prop.onClick (fun _ -> dispatch DiscardConfirmed)
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            | _ -> Html.none

        let processingUnit = state.Document |> Option.map toProcessingUnit

        let commitMutation (mutate: unit -> unit) =
            match processingUnit with
            | Some currentProcessingUnit ->
                mutate ()
                currentProcessingUnit |> fromProcessingUnit |> DocumentUpdated |> dispatch
            | None -> ()

        let saveCurrent () =
            match processingUnit, state.Document with
            | Some currentProcessingUnit, Some document ->
                dispatch (ErrorNotificationSet None)
                dispatch (InfoNotificationSet None)

                let legacyState = {
                    ProcessingUnit = currentProcessingUnit
                    Version = currentVersionNumber state
                    FilePath = currentFilePath state
                    IsDirty = isDirty
                    CwlVersion = currentCwlVersion document
                }

                match ensureCanSave legacyState with
                | Ok() ->
                    match host.pickSavePath, currentFilePath state with
                    | None, None -> dispatch (ErrorNotificationSet(Some "Cannot save: no file path is available."))
                    | _ -> dispatch SaveRequested
                | Error message -> dispatch (ErrorNotificationSet(Some message))
            | _ -> ()

        let wrapEditorView (editorView: ReactElement) =
            Html.div [
                prop.key (string state.SessionId)
                prop.children [ editorView; previewOverlay; discardOverlay ]
            ]

        match initialLoadError with
        | Some message ->
            Html.div [
                prop.testId "cwl-editor-initial-load-error"
                prop.className "swt:alert swt:alert-error"
                prop.text message
            ]
        | None ->
            match state.Document, processingUnit with
            | None, _ ->
                StartScreen.StartScreen(
                    currentVersionNumber state,
                    state.Notifications.ErrorMessage,
                    state.Async.IsLoading,
                    (fun () -> dispatch (ErrorNotificationSet None)),
                    (fun kind ->
                        resetEditorSelection ()

                        CwlEditorPorts.liveTimerPort.SetTimeout 0 (fun () -> dispatch (CreateNewRequested kind))
                        |> ignore
                    ),
                    (fun () -> dispatch LoadExistingRequested)
                )
            | Some document, Some processingUnit ->
                let validationResult = validateDocument Live document
                let kindLabel = currentKindLabel state |> Option.defaultValue "Unknown"
                let fileLabel = currentFilePath state |> Option.defaultValue "unsaved.cwl"
                let version = currentVersionNumber state
                let stateCwlVersion = currentCwlVersion document

                match processingUnit with
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
                        version,
                        kindLabel,
                        fileLabel,
                        isDirty,
                        state.Async.IsSaving,
                        state.Notifications.ErrorMessage,
                        state.Notifications.InfoMessage,
                        stateCwlVersion,
                        intentText tool.Intent,
                        baseCommandValue,
                        tool,
                        inputs,
                        outputs,
                        activeInputIndex,
                        activeOutputIndex,
                        tool.Requirements,
                        tool.Hints,
                        validationResult,
                        commitMutation,
                        setSelectedInputIndex,
                        setSelectedOutputIndex,
                        (fun () -> dispatch PreviewRequested),
                        saveCurrent,
                        (fun () -> dispatch LeaveEditorRequested),
                        (fun nextVersion ->
                            commitMutation (fun () -> setProcessingUnitVersion nextVersion processingUnit)
                        ),
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
                        version,
                        state.SessionId,
                        kindLabel,
                        fileLabel,
                        currentFilePath state,
                        isDirty,
                        state.Async.IsSaving,
                        state.Notifications.ErrorMessage,
                        state.Notifications.InfoMessage,
                        stateCwlVersion,
                        intentText workflow.Intent,
                        workflow,
                        inputs,
                        outputs,
                        activeInputIndex,
                        activeOutputIndex,
                        activeStepIndex,
                        workflow.Requirements,
                        workflow.Hints,
                        validationResult,
                        commitMutation,
                        setSelectedInputIndex,
                        setSelectedOutputIndex,
                        setSelectedStepIndex,
                        (fun () -> dispatch PreviewRequested),
                        saveCurrent,
                        (fun () -> dispatch LeaveEditorRequested),
                        (fun nextVersion ->
                            commitMutation (fun () -> setProcessingUnitVersion nextVersion processingUnit)
                        ),
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
                        (fun yaml -> dispatch (PreviewOpened yaml)),
                        (fun message -> dispatch (InfoNotificationSet message)),
                        (fun message -> dispatch (ErrorNotificationSet message))
                    )
                    |> wrapEditorView

                | CWLProcessingUnit.ExpressionTool tool ->
                    let inputs = CWLExpressionToolDescription.getInputsOrEmpty tool
                    let outputs = tool.Outputs
                    let activeInputIndex = clampIndex selectedInputIndex inputs.Count
                    let activeOutputIndex = clampIndex selectedOutputIndex outputs.Count

                    ExpressionToolEditor.ExpressionToolEditor(
                        version,
                        kindLabel,
                        fileLabel,
                        isDirty,
                        state.Async.IsSaving,
                        state.Notifications.ErrorMessage,
                        state.Notifications.InfoMessage,
                        stateCwlVersion,
                        intentText tool.Intent,
                        tool.Expression,
                        tool,
                        inputs,
                        outputs,
                        activeInputIndex,
                        activeOutputIndex,
                        tool.Requirements,
                        tool.Hints,
                        validationResult,
                        commitMutation,
                        setSelectedInputIndex,
                        setSelectedOutputIndex,
                        (fun () -> dispatch PreviewRequested),
                        saveCurrent,
                        (fun () -> dispatch LeaveEditorRequested),
                        (fun nextVersion ->
                            commitMutation (fun () -> setProcessingUnitVersion nextVersion processingUnit)
                        ),
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
                                                                prop.onClick (fun _ -> dispatch PreviewRequested)
                                                            ]
                                                            Html.button [
                                                                prop.testId "cwl-operation-save"
                                                                prop.className "swt:btn swt:btn-sm swt:btn-primary"
                                                                prop.text "Save"
                                                                prop.onClick (fun _ -> saveCurrent ())
                                                            ]
                                                            Html.button [
                                                                prop.testId "cwl-operation-back"
                                                                prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                                                prop.text "Back"
                                                                prop.onClick (fun _ -> dispatch LeaveEditorRequested)
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
                            discardOverlay
                        ]
                    ]
            | Some _, None -> Html.none

    [<ReactComponent(true)>]
    static member CwlEditor
        (?initialFile: LoadCwlResponse, ?host: Types.CwlEditorHost, ?onDirtyChange: bool -> unit)
        : ReactElement =
        let editor = CwlEditor.Editor(initialFile, onDirtyChange)

        match host with
        | Some providedHost -> Context.CwlEditorHostCtx.Provider(Some providedHost, editor)
        | None -> editor
