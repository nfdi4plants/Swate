module Renderer.Components.MainWindowContent

open System
open ARCtrl
open Fable.Core
open Feliz
open MainElement
open Swate.Components
open Swate.Components.NoteTypes
open Swate.Components.Landing
open Swate.Components.Notes.Editor
open Swate.Electron.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper

type ContentActions = {
    SetArcFileState: ArcFiles option -> unit
    SetPreviewData: PageState option -> unit
    SetSelectedTreeItemPath: string option -> unit
}

type private ArcTargetProps = {
    Path: string
    AppState: AppState
    ArcFileState: ArcFiles option
    PageState: PageState option
    WorkspaceFileTree: FileEntry list
    Actions: ContentActions
}


module MainWindowContentHelper =

    let saveArcFileWithPreview (arcFile: ArcFiles) : JS.Promise<Result<PageState, string>> = promise {
        match ArcFileSaveMapping.tryCreateSaveRequest arcFile with
        | None -> return Error "Saving this file type is not supported in Electron yet."
        | Some request ->
            let! saveResult = Api.ipcArcVaultApi.saveArcFile (unbox null) request

            match saveResult with
            | Ok previewData -> return Ok previewData
            | Error exn -> return Error exn.Message
    }

    let saveArcFile (arcFile: ArcFiles) : JS.Promise<Result<unit, string>> = promise {
        let! saveResult = saveArcFileWithPreview arcFile
        return saveResult |> Result.map ignore
    }

    let onSaveClick arcFileState setPreviewData _ =
        match arcFileState with
        | None -> ()
        | Some arcFile ->
            promise {
                let! result = saveArcFileWithPreview arcFile

                match result with
                | Ok updatedPreview -> setPreviewData (Some updatedPreview)
                | Error errorMsg -> setPreviewData (Some(PageState.Error $"Save failed: {errorMsg}"))
            }
            |> Promise.start

    let createFromLanding
        landingUiState
        setLandingUiState
        setLandingDraft
        appState
        setSelectedTreeItemPath
        setPreviewData
        (payload: SubmitPayload)
        =
        let finishSuccess (previewData: PageState) =
            payload.ArcFile
            |> ARCHelper.tryGetArcFilePath appState
            |> setSelectedTreeItemPath

            setPreviewData (Some previewData)
            setLandingDraft LandingDraft.init
            setLandingUiState LandingUiState.init

        promise {
            setLandingUiState {
                landingUiState with
                    IsSubmitting = true
                    Error = None
            }

            let! saveResult = saveArcFileWithPreview payload.ArcFile

            match saveResult with
            | Result.Error message ->
                setLandingUiState {
                    landingUiState with
                        IsSubmitting = false
                        Error = Some message
                }
            | Ok previewData ->
                match payload.ProtocolIntent with
                | None -> finishSuccess previewData
                | Some protocolIntent ->
                    let request: FileIOTypes.WriteFileRequest = {
                        RelativePath = protocolIntent.RelativePath
                        Content = protocolIntent.Content
                    }

                    let! writeResult = Api.ipcArcVaultApi.writeFile (unbox null) request

                    match writeResult with
                    | Ok() -> finishSuccess previewData
                    | Result.Error exn ->
                        setLandingUiState {
                            landingUiState with
                                IsSubmitting = false
                                Error = Some $"Saved ARC metadata but failed to write protocol file: {exn.Message}"
                        }
        }
        |> Promise.start

[<ReactComponent>]
let private CreateARCPreview
    (arcFile: ArcFiles)
    (setArcFileState: ArcFiles option -> unit)
    (activeView: PreviewActiveView)
    (setActiveView: PreviewActiveView -> unit)
    =

    let setArcFile arcFile = setArcFileState (Some arcFile)

    let canRenderDataMapView =
        match arcFile with
        | ArcFiles.Assay assay -> assay.DataMap.IsSome
        | ArcFiles.Study(study, _) -> study.DataMap.IsSome
        | ArcFiles.Run run -> run.DataMap.IsSome
        | ArcFiles.DataMap _ -> true
        | _ -> false

    React.useEffect (
        (fun () ->
            let tables = arcFile.Tables()

            let nextActiveView =
                match activeView with
                | PreviewActiveView.Table tableIndex when tableIndex >= 0 && tableIndex < tables.Count -> activeView
                | PreviewActiveView.DataMap when canRenderDataMapView -> activeView
                | PreviewActiveView.Metadata -> activeView
                | _ ->
                    if tables.Count > 0 then
                        PreviewActiveView.Table 0
                    else
                        PreviewActiveView.Metadata

            if nextActiveView <> activeView then
                setActiveView nextActiveView
        ),
        [| box arcFile; box activeView |]
    )

    Html.div [
        prop.className "swt:flex swt:flex-col swt:h-full"
        prop.children [|
            Html.div [
                prop.className "swt:flex-1 swt:overflow-x-hidden swt:overflow-y-auto"
                prop.children [ CreateTableView activeView arcFile setArcFile ]
            ]
            CreateAddRowsFooter arcFile activeView setArcFile
            CreateARCitectFooter arcFile activeView setActiveView setArcFile
        |]
    ]

[<ReactComponent>]
let private ArcFilePreviewTarget (arcFileState: ArcFiles option, actions: ContentActions) =

    let activeView, setActiveView = React.useState PreviewActiveView.Metadata

    let activeTableIndex =
        match activeView with
        | PreviewActiveView.Table tableIndex -> Some tableIndex
        | _ -> None

    Html.div [
        prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
        prop.children [
            Html.div [
                prop.className "swt:flex-none"
                prop.children [
                    MainElement.CreateARCitectNavbar
                        arcFileState
                        activeView
                        activeTableIndex
                        actions.SetArcFileState
                        (MainWindowContentHelper.onSaveClick arcFileState actions.SetPreviewData)
                ]
            ]
            Html.div [
                prop.className "swt:flex-1 swt:overflow-y-auto swt:flex swt:flex-col swt:min-w-0"
                prop.children [
                    match arcFileState with
                    | Some arcFile -> CreateARCPreview arcFile actions.SetArcFileState activeView setActiveView
                    | None ->
                        Html.div [
                            prop.className "swt:p-4 swt:text-error"
                            prop.text "Failed to parse ArcFile data"
                        ]
                ]
            ]
        ]
    ]

[<ReactComponent>]
let private TextPreviewTarget (content: string) =
    Html.div [
        prop.className "swt:size-full swt:p-4 swt:overflow-auto swt:bg-base-100"
        prop.children [|
            Html.pre [
                prop.className "swt:text-sm swt:font-mono"
                prop.text content
            ]
        |]
    ]

[<ReactComponent>]
let private UnknownPreviewTarget () =
    Html.div [
        prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
        prop.children [| Html.h1 "Unknown file type" |]
    ]

[<ReactComponent>]
let ErrorPreviewTarget (errMsg: string) =
    Html.div [
        prop.className "swt:size-full swt:flex swt:justify-center swt:items-center swt:flex-col swt:gap-2"
        prop.children [|
            Html.h2 [
                prop.className "swt:text-error swt:font-bold"
                prop.text "Preview Error"
            ]
            Html.span [
                prop.className "swt:text-base-content swt:opacity-70"
                prop.text errMsg
            ]
        |]
    ]

[<ReactComponent>]
let private LandingDraftTarget (appState: AppState, actions: ContentActions) =

    let landingDraft, setLandingDraft = React.useState LandingDraft.init
    let landingUiState, setLandingUiState = React.useState LandingUiState.init

    Landing.Wizard(
        landingDraft,
        setLandingDraft,
        landingUiState,
        setLandingUiState,
        MainWindowContentHelper.createFromLanding
            landingUiState
            setLandingUiState
            setLandingDraft
            appState
            actions.SetSelectedTreeItemPath
            actions.SetPreviewData
    )

[<ReactComponent>]
let private NotesDraftTarget (appState: AppState, workspaceFileTree: FileEntry list, actions: ContentActions) =

    let notesDraft, setNotesDraft = React.useState NotesDraft.init
    let notesUiState, setNotesUiState = React.useState NotesUiState.init

    let availableNotesTargets =
        React.useMemo (
            (fun _ -> NoteSearchPage.createAvailableNotesTargets workspaceFileTree),
            [| box workspaceFileTree |]
        )

    Notes.Wizard(
        notesDraft,
        setNotesDraft,
        notesUiState,
        setNotesUiState,
        NoteSearchPage.createFromNotes
            notesUiState
            setNotesUiState
            setNotesDraft
            appState
            actions.SetSelectedTreeItemPath
            actions.SetPreviewData,
        availableNotesTargets
    )

[<ReactComponent>]
let private NotesSearchTarget (appState: AppState, workspaceFileTree: FileEntry list, actions: ContentActions) =
    NoteSearchPage.CreateNotesSearchPage(
        appState,
        workspaceFileTree,
        actions.SetSelectedTreeItemPath,
        actions.SetPreviewData
    )

[<ReactComponent>]
let private EmptySelectionTarget (path: string) =
    Html.h1 [
        prop.text path
        prop.className
            "swt:text-xl swt:uppercase swt:inline-block swt:text-transparent swt:bg-clip-text swt:bg-linear-to-r swt:from-primary swt:to-secondary"
    ]

[<ReactComponent>]
let private ArcPageTarget (props: ArcTargetProps) =
    match props.PageState with
    | Some data ->
        match data with
        | PageState.ArcFileData _ -> ArcFilePreviewTarget(props.ArcFileState, props.Actions)
        | PageState.Text content -> TextPreviewTarget content
        | PageState.Unknown -> UnknownPreviewTarget()
        | PageState.Error errMsg -> ErrorPreviewTarget errMsg
        | PageState.LandingDraft -> LandingDraftTarget(props.AppState, props.Actions)
        | PageState.NotesDraft -> NotesDraftTarget(props.AppState, props.WorkspaceFileTree, props.Actions)
        | PageState.NotesSearch -> NotesSearchTarget(props.AppState, props.WorkspaceFileTree, props.Actions)
    | None -> EmptySelectionTarget props.Path

[<ReactComponent>]
let private InitTarget () =

    Html.div [
        prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
        prop.children [
            Html.div [
                prop.className "swt:flex-1 swt:flex swt:justify-center swt:items-center"
                prop.children [ InitState.InitState() ]
            ]
        ]
    ]

[<ReactComponent>]
let private ArcTarget (path: string, arcFileState, pageState, actions: ContentActions) =

    let workspaceCtx =
        React.useContext Renderer.Context.WorkspaceStateCtx.WorkspaceStateCtx

    let arcProps: ArcTargetProps = {
        Path = path
        AppState = AppState.ARC path
        ArcFileState = arcFileState
        PageState = pageState
        WorkspaceFileTree = workspaceCtx.state.FileTree
        Actions = actions
    }

    Html.div [
        prop.id "arc-page-target"
        prop.className "swt:size-full swt:flex swt:justify-center"
        prop.children [ Html.div "TESTING!"; ArcPageTarget arcProps ]
    ]

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main (appState: AppState, arcFileState, pageState, actions: ContentActions) =
    match appState with
    | AppState.Init -> InitTarget()
    | AppState.ARC path -> ArcTarget(path, arcFileState, pageState, actions)