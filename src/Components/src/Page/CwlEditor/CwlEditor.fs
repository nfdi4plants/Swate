namespace Swate.Components.Page.CwlEditor

open System
open Browser.Dom
open Fable.Core
open Feliz
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.Adapters.ArCtrlDecode
open Swate.Components.Shared.Cwl.Adapters.ArCtrlEncode
open Swate.Components.Shared.Cwl.Adapters.ValidationAdapter
open Swate.Components.Shared.Cwl.CommandLineToolMutations
open Swate.Components.Shared.Cwl.Documents.Common
open Swate.Components.Shared.Cwl.Documents.ExpressionTool
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.CwlService
open Swate.Components.Shared.Cwl.EditorTypes
open Swate.Components.Shared.Cwl.ExpressionToolMutations
open Swate.Components.Shared.Cwl.Features.InputsFeature
open Swate.Components.Shared.Cwl.Features.OutputsFeature
open Swate.Components.Shared.Cwl.HostTypes
open Swate.Components.Shared.Cwl.State.Actions
open Swate.Components.Shared.Cwl.State.EffectRunner
open Swate.Components.Shared.Cwl.State.Init
open Swate.Components.Shared.Cwl.State.Reducer
open Swate.Components.Shared.Cwl.State.Selectors
open Swate.Components.Shared.Cwl.State.Types
open Swate.Components.Shared.Cwl.Validation.ValidationContext
open Swate.Components.Shared.Cwl.Validation.ValidationEngine

module InputsFeature = Swate.Components.Shared.Cwl.Features.InputsFeature
module OutputsFeature = Swate.Components.Shared.Cwl.Features.OutputsFeature
module RequirementsFeature = Swate.Components.Shared.Cwl.Features.RequirementsFeature
module CommandLineToolDocument = Swate.Components.Shared.Cwl.Documents.CommandLineTool

[<AutoOpen>]
module private CwlEditorHelpers =

    let clampIndex (selectedIndex: int option) (count: int) =
        match selectedIndex with
        | Some index when index >= 0 && index < count -> Some index
        | _ when count > 0 -> Some 0
        | _ -> None

    let activeIndexById selectedId items getId =
        match selectedId with
        | Some id ->
            items
            |> List.tryFindIndex (fun item -> getId item = id)
            |> Option.orElseWith (fun () -> clampIndex None (List.length items))
        | None -> clampIndex None (List.length items)

    let idAtIndex selectedIndex items getId =
        selectedIndex
        |> Option.bind (fun index -> items |> List.tryItem index |> Option.map getId)

    let lastInputId document =
        match document with
        | CommandLineToolDoc model -> model.Inputs
        | WorkflowDoc model -> model.Inputs
        | ExpressionToolDoc model -> model.Inputs
        | OperationDoc model -> model.Inputs
        |> List.tryLast
        |> Option.map (fun input -> input.Id)

    let lastOutputId document =
        match document with
        | CommandLineToolDoc model -> model.Outputs
        | WorkflowDoc model -> model.Outputs
        | ExpressionToolDoc model -> model.Outputs
        | OperationDoc model -> model.Outputs
        |> List.tryLast
        |> Option.map (fun output -> output.Id)

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

    let private formatInitialLoadError (message: string) =
        let prefix = "Failed to decode CWL:"

        if String.IsNullOrWhiteSpace message then
            "Failed to decode CWL."
        elif message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
            message
        else
            sprintf "%s %s" prefix message

    let private tryCreateInitialEditorState (fileResult: LoadCwlResponse) =
        if not fileResult.Success then
            let errorText = fileResult.Error |> Option.defaultValue "unknown error"
            Result.Error(sprintf "Load failed: %s" errorText)
        else
            match fileResult.Yaml with
            | Some yaml ->
                tryLoadToEditorWithResolved yaml fileResult.ResolvedYaml fileResult.FilePath
                |> Result.mapError formatInitialLoadError
            | None -> Result.Error "Load failed: missing YAML payload."

    let formatBlockedSaveMessage (validation: Swate.Components.Shared.Cwl.Validation.ValidationTypes.ValidationResult) =
        let ruleIdText (Swate.Components.Shared.Cwl.Validation.ValidationTypes.RuleId value) = value

        let firstDetail =
            validation.Errors
            |> List.tryHead
            |> Option.map (fun issue ->
                sprintf " First issue (%s at %s): %s" (ruleIdText issue.RuleId) issue.Path issue.Message
            )
            |> Option.defaultValue ""

        sprintf "Save blocked: %d validation error(s).%s" validation.Errors.Length firstDetail

    let initialAppState (initialFile: LoadCwlResponse option) =
        match initialFile with
        | Some fileResult ->
            match tryCreateInitialEditorState fileResult with
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

        let host =
            Context.useCwlEditorHostCtx ()
            |> Option.defaultWith (fun () -> failwith "CwlEditor requires a CwlEditorHost context.")

        let isDirty = isDirty state

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

        let updateDocument nextDocument = dispatch (DocumentUpdated nextDocument)

        let updateCurrentDocument updater =
            state.Document |> Option.map updater |> Option.iter updateDocument

        let addInputAndSelect () =
            state.Document
            |> Option.iter (fun document ->
                let nextDocument = InputsFeature.addInput document
                dispatch (DocumentUpdated nextDocument)

                dispatch (
                    SelectionChanged {
                        state.Selection with
                            ActiveInputId = lastInputId nextDocument
                    }
                )
            )

        let addOutputAndSelect () =
            state.Document
            |> Option.iter (fun document ->
                let nextDocument = OutputsFeature.addOutput document
                dispatch (DocumentUpdated nextDocument)

                dispatch (
                    SelectionChanged {
                        state.Selection with
                            ActiveOutputId = lastOutputId nextDocument
                    }
                )
            )

        let saveCurrent () =
            match processingUnit, state.Document with
            | Some _, Some document ->
                dispatch (ErrorNotificationSet None)
                dispatch (InfoNotificationSet None)

                let validation = validateDocument OnSave document

                if validation.IsValid then
                    match host.pickSavePath, currentFilePath state with
                    | None, None -> dispatch (ErrorNotificationSet(Some "Cannot save: no file path is available."))
                    | _ -> dispatch SaveRequested
                else
                    dispatch (ErrorNotificationSet(Some(formatBlockedSaveMessage validation)))
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
                    let model =
                        match document with
                        | CommandLineToolDoc model -> model
                        | _ -> failwith "Expected CommandLineToolDoc"

                    let baseCommandValue =
                        tool.BaseCommand
                        |> Option.bind (fun commands -> if commands.Count > 0 then Some commands.[0] else None)
                        |> Option.defaultValue ""

                    let activeInputIndex =
                        activeIndexById state.Selection.ActiveInputId model.Inputs (fun input -> input.Id)

                    let activeOutputIndex =
                        activeIndexById state.Selection.ActiveOutputId model.Outputs (fun output -> output.Id)

                    let setActiveInputIndex selectedIndex =
                        dispatch (
                            SelectionChanged {
                                state.Selection with
                                    ActiveInputId = idAtIndex selectedIndex model.Inputs (fun input -> input.Id)
                            }
                        )

                    let setActiveOutputIndex selectedIndex =
                        dispatch (
                            SelectionChanged {
                                state.Selection with
                                    ActiveOutputId = idAtIndex selectedIndex model.Outputs (fun output -> output.Id)
                            }
                        )

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
                        model.Inputs,
                        model.Outputs,
                        activeInputIndex,
                        activeOutputIndex,
                        model.Requirements,
                        model.Hints,
                        validationResult,
                        setActiveInputIndex,
                        setActiveOutputIndex,
                        (fun () -> dispatch PreviewRequested),
                        saveCurrent,
                        (fun () -> dispatch LeaveEditorRequested),
                        (fun nextVersion ->
                            commitMutation (fun () -> setProcessingUnitVersion nextVersion processingUnit)
                        ),
                        (fun value -> commitMutation (fun () -> tool.Intent <- parseIntentText value)),
                        (fun command ->
                            updateCurrentDocument (fun currentDocument ->
                                match currentDocument with
                                | CommandLineToolDoc currentModel ->
                                    CommandLineToolDoc(CommandLineToolDocument.setBaseCommand command currentModel)
                                | _ -> currentDocument
                            )
                        ),
                        (fun inputId name -> updateCurrentDocument (InputsFeature.renameInput inputId name)),
                        (fun inputId cwlType -> updateCurrentDocument (InputsFeature.setInputType inputId cwlType)),
                        (fun inputId prefix -> updateCurrentDocument (InputsFeature.setInputPrefix inputId prefix)),
                        (fun inputId position ->
                            updateCurrentDocument (InputsFeature.setInputPosition inputId position)
                        ),
                        (fun inputId isOptional ->
                            updateCurrentDocument (InputsFeature.setInputOptional inputId isOptional)
                        ),
                        addInputAndSelect,
                        (fun inputId -> updateCurrentDocument (InputsFeature.removeInput inputId)),
                        (fun inputId -> updateCurrentDocument (InputsFeature.moveInputUp inputId)),
                        (fun inputId -> updateCurrentDocument (InputsFeature.moveInputDown inputId)),
                        (fun outputId name -> updateCurrentDocument (OutputsFeature.renameOutput outputId name)),
                        (fun outputId cwlType -> updateCurrentDocument (OutputsFeature.setOutputType outputId cwlType)),
                        (fun outputId glob -> updateCurrentDocument (OutputsFeature.setOutputGlob outputId glob)),
                        addOutputAndSelect,
                        (fun outputId -> updateCurrentDocument (OutputsFeature.removeOutput outputId)),
                        (fun outputId -> updateCurrentDocument (OutputsFeature.moveOutputUp outputId)),
                        (fun outputId -> updateCurrentDocument (OutputsFeature.moveOutputDown outputId)),
                        (fun key isChecked requirementNodeId ->
                            match requirementNodeId with
                            | Some nodeId ->
                                updateCurrentDocument (
                                    RequirementsFeature.setRequirementEnabledWithId nodeId key isChecked
                                )
                            | None -> updateCurrentDocument (RequirementsFeature.setRequirementEnabled key isChecked)
                        ),
                        (fun key isChecked requirementNodeId ->
                            match requirementNodeId with
                            | Some nodeId ->
                                updateCurrentDocument (RequirementsFeature.setHintEnabledWithId nodeId key isChecked)
                            | None -> updateCurrentDocument (RequirementsFeature.setHintEnabled key isChecked)
                        ),
                        (fun requirementNodeId field value ->
                            updateCurrentDocument (
                                RequirementsFeature.setRequirementField requirementNodeId field value
                            )
                        ),
                        (fun requirementNodeId field value ->
                            updateCurrentDocument (RequirementsFeature.setHintField requirementNodeId field value)
                        )
                    )
                    |> wrapEditorView

                | CWLProcessingUnit.Workflow workflow ->
                    let model =
                        match document with
                        | WorkflowDoc model -> model
                        | _ -> failwith "Expected WorkflowDoc"

                    let activeInputIndex =
                        activeIndexById state.Selection.ActiveInputId model.Inputs (fun input -> input.Id)

                    let activeOutputIndex =
                        activeIndexById state.Selection.ActiveOutputId model.Outputs (fun output -> output.Id)

                    let activeStepIndex =
                        activeIndexById state.Selection.ActiveStepId model.Steps (fun step -> step.Id)

                    let setActiveInputIndex selectedIndex =
                        dispatch (
                            SelectionChanged {
                                state.Selection with
                                    ActiveInputId = idAtIndex selectedIndex model.Inputs (fun input -> input.Id)
                            }
                        )

                    let setActiveOutputIndex selectedIndex =
                        dispatch (
                            SelectionChanged {
                                state.Selection with
                                    ActiveOutputId = idAtIndex selectedIndex model.Outputs (fun output -> output.Id)
                            }
                        )

                    let setActiveStepIndex selectedIndex =
                        dispatch (
                            SelectionChanged {
                                state.Selection with
                                    ActiveStepId = idAtIndex selectedIndex model.Steps (fun step -> step.Id)
                                    ActiveStepInputId = None
                                    ActiveStepOutputId = None
                            }
                        )

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
                        model.Inputs,
                        model.Outputs,
                        activeInputIndex,
                        activeOutputIndex,
                        activeStepIndex,
                        model.Requirements,
                        model.Hints,
                        validationResult,
                        commitMutation,
                        setActiveInputIndex,
                        setActiveOutputIndex,
                        setActiveStepIndex,
                        (fun () -> dispatch PreviewRequested),
                        saveCurrent,
                        (fun () -> dispatch LeaveEditorRequested),
                        (fun nextVersion ->
                            commitMutation (fun () -> setProcessingUnitVersion nextVersion processingUnit)
                        ),
                        (fun value -> commitMutation (fun () -> workflow.Intent <- parseIntentText value)),
                        (fun inputId name -> updateCurrentDocument (InputsFeature.renameInput inputId name)),
                        (fun inputId cwlType -> updateCurrentDocument (InputsFeature.setInputType inputId cwlType)),
                        (fun inputId prefix -> updateCurrentDocument (InputsFeature.setInputPrefix inputId prefix)),
                        (fun inputId position ->
                            updateCurrentDocument (InputsFeature.setInputPosition inputId position)
                        ),
                        (fun inputId isOptional ->
                            updateCurrentDocument (InputsFeature.setInputOptional inputId isOptional)
                        ),
                        addInputAndSelect,
                        (fun inputId -> updateCurrentDocument (InputsFeature.removeInput inputId)),
                        (fun inputId -> updateCurrentDocument (InputsFeature.moveInputUp inputId)),
                        (fun inputId -> updateCurrentDocument (InputsFeature.moveInputDown inputId)),
                        (fun outputId name -> updateCurrentDocument (OutputsFeature.renameOutput outputId name)),
                        (fun outputId cwlType -> updateCurrentDocument (OutputsFeature.setOutputType outputId cwlType)),
                        (fun outputId glob -> updateCurrentDocument (OutputsFeature.setOutputGlob outputId glob)),
                        addOutputAndSelect,
                        (fun outputId -> updateCurrentDocument (OutputsFeature.removeOutput outputId)),
                        (fun outputId -> updateCurrentDocument (OutputsFeature.moveOutputUp outputId)),
                        (fun outputId -> updateCurrentDocument (OutputsFeature.moveOutputDown outputId)),
                        (fun key isChecked requirementNodeId ->
                            match requirementNodeId with
                            | Some nodeId ->
                                updateCurrentDocument (
                                    RequirementsFeature.setRequirementEnabledWithId nodeId key isChecked
                                )
                            | None -> updateCurrentDocument (RequirementsFeature.setRequirementEnabled key isChecked)
                        ),
                        (fun key isChecked requirementNodeId ->
                            match requirementNodeId with
                            | Some nodeId ->
                                updateCurrentDocument (RequirementsFeature.setHintEnabledWithId nodeId key isChecked)
                            | None -> updateCurrentDocument (RequirementsFeature.setHintEnabled key isChecked)
                        ),
                        (fun requirementNodeId field value ->
                            updateCurrentDocument (
                                RequirementsFeature.setRequirementField requirementNodeId field value
                            )
                        ),
                        (fun requirementNodeId field value ->
                            updateCurrentDocument (RequirementsFeature.setHintField requirementNodeId field value)
                        ),
                        (fun yaml -> dispatch (PreviewOpened yaml)),
                        (fun message -> dispatch (InfoNotificationSet message)),
                        (fun message -> dispatch (ErrorNotificationSet message))
                    )
                    |> wrapEditorView

                | CWLProcessingUnit.ExpressionTool tool ->
                    let model =
                        match document with
                        | ExpressionToolDoc model -> model
                        | _ -> failwith "Expected ExpressionToolDoc"

                    let activeInputIndex =
                        activeIndexById state.Selection.ActiveInputId model.Inputs (fun input -> input.Id)

                    let activeOutputIndex =
                        activeIndexById state.Selection.ActiveOutputId model.Outputs (fun output -> output.Id)

                    let setActiveInputIndex selectedIndex =
                        dispatch (
                            SelectionChanged {
                                state.Selection with
                                    ActiveInputId = idAtIndex selectedIndex model.Inputs (fun input -> input.Id)
                            }
                        )

                    let setActiveOutputIndex selectedIndex =
                        dispatch (
                            SelectionChanged {
                                state.Selection with
                                    ActiveOutputId = idAtIndex selectedIndex model.Outputs (fun output -> output.Id)
                            }
                        )

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
                        model.Inputs,
                        model.Outputs,
                        activeInputIndex,
                        activeOutputIndex,
                        model.Requirements,
                        model.Hints,
                        validationResult,
                        setActiveInputIndex,
                        setActiveOutputIndex,
                        (fun () -> dispatch PreviewRequested),
                        saveCurrent,
                        (fun () -> dispatch LeaveEditorRequested),
                        (fun nextVersion ->
                            commitMutation (fun () -> setProcessingUnitVersion nextVersion processingUnit)
                        ),
                        (fun value -> commitMutation (fun () -> tool.Intent <- parseIntentText value)),
                        (fun expression -> updateCurrentDocument (setExpression expression)),
                        (fun inputId name -> updateCurrentDocument (InputsFeature.renameInput inputId name)),
                        (fun inputId cwlType -> updateCurrentDocument (InputsFeature.setInputType inputId cwlType)),
                        (fun inputId prefix -> updateCurrentDocument (InputsFeature.setInputPrefix inputId prefix)),
                        (fun inputId position ->
                            updateCurrentDocument (InputsFeature.setInputPosition inputId position)
                        ),
                        (fun inputId isOptional ->
                            updateCurrentDocument (InputsFeature.setInputOptional inputId isOptional)
                        ),
                        addInputAndSelect,
                        (fun inputId -> updateCurrentDocument (InputsFeature.removeInput inputId)),
                        (fun inputId -> updateCurrentDocument (InputsFeature.moveInputUp inputId)),
                        (fun inputId -> updateCurrentDocument (InputsFeature.moveInputDown inputId)),
                        (fun outputId name -> updateCurrentDocument (OutputsFeature.renameOutput outputId name)),
                        (fun outputId cwlType -> updateCurrentDocument (OutputsFeature.setOutputType outputId cwlType)),
                        (fun outputId glob -> updateCurrentDocument (OutputsFeature.setOutputGlob outputId glob)),
                        addOutputAndSelect,
                        (fun outputId -> updateCurrentDocument (OutputsFeature.removeOutput outputId)),
                        (fun outputId -> updateCurrentDocument (OutputsFeature.moveOutputUp outputId)),
                        (fun outputId -> updateCurrentDocument (OutputsFeature.moveOutputDown outputId)),
                        (fun key isChecked requirementNodeId ->
                            match requirementNodeId with
                            | Some nodeId ->
                                updateCurrentDocument (
                                    RequirementsFeature.setRequirementEnabledWithId nodeId key isChecked
                                )
                            | None -> updateCurrentDocument (RequirementsFeature.setRequirementEnabled key isChecked)
                        ),
                        (fun key isChecked requirementNodeId ->
                            match requirementNodeId with
                            | Some nodeId ->
                                updateCurrentDocument (RequirementsFeature.setHintEnabledWithId nodeId key isChecked)
                            | None -> updateCurrentDocument (RequirementsFeature.setHintEnabled key isChecked)
                        ),
                        (fun requirementNodeId field value ->
                            updateCurrentDocument (
                                RequirementsFeature.setRequirementField requirementNodeId field value
                            )
                        ),
                        (fun requirementNodeId field value ->
                            updateCurrentDocument (RequirementsFeature.setHintField requirementNodeId field value)
                        )
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
