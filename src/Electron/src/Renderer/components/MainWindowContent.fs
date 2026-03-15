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


let private normalizePath (path: string) = path.Replace("\\", "/")

let private tryResolveArcRelativePath (appState: AppState) (relativePath: string) =
    match appState with
    | AppState.ARC arcPath ->
        let root = normalizePath arcPath |> fun path -> path.TrimEnd('/')
        let normalizedRelative = normalizePath relativePath |> fun path -> path.TrimStart('/')

        if String.IsNullOrWhiteSpace normalizedRelative then
            None
        else
            Some $"{root}/{normalizedRelative}"
    | AppState.Init -> None

let private tryGetExistingTargetRef (path: string) =
    let segments = (normalizePath path).Split('/', StringSplitOptions.RemoveEmptyEntries)

    let tryResolveTarget (folderName: string) (kind: NotesTargetKind) =
        match
            segments
            |> Array.tryFindIndex (fun segment -> String.Equals(segment, folderName, StringComparison.OrdinalIgnoreCase))
        with
        | Some index when index + 1 < segments.Length ->
            let name = segments.[index + 1].Trim()

            if String.IsNullOrWhiteSpace name then
                None
            else
                Some {
                    Name = name
                    Kind = kind
                }
        | _ -> None

    match tryResolveTarget "studies" NotesTargetKind.Study with
    | Some target -> Some target
    | None -> tryResolveTarget "assays" NotesTargetKind.Assay

let private createAvailableNotesTargets (fileEntries: FileEntry list) =
    fileEntries
    |> Seq.choose (fun entry -> tryGetExistingTargetRef entry.path)
    |> Seq.distinctBy (fun target -> target.Kind, target.Name)
    |> Seq.sortBy (fun target ->
        let kindOrder =
            match target.Kind with
            | NotesTargetKind.Study -> 0
            | NotesTargetKind.Assay -> 1

        kindOrder, target.Name.ToLowerInvariant ()
    )
    |> ResizeArray

[<ReactComponent>]
let CreateNotesSearchPage
    (
        appState: AppState,
        fileTree: FileEntry list,
        setSelectedTreeItemPath: string option -> unit,
        setPreviewData: PageState option -> unit
    ) =

    let notes, setNotes = React.useState ([]: NoteSearch list)
    let isLoading, setIsLoading = React.useState true
    let error, setError = React.useState (None: string option)

    React.useEffect (
        (fun () ->
            let mutable isDisposed = false

            setIsLoading true
            setError None

            promise {
                let! result = Api.ipcArcVaultApi.readNotes (unbox null)

                if not isDisposed then
                    match result with
                    | Ok nextNotes ->
                        setNotes (nextNotes |> Array.toList)
                        setIsLoading false
                    | Result.Error exn ->
                        setNotes []
                        setError (Some $"Failed to load notes: {exn.Message}")
                        setIsLoading false
            }
            |> Promise.start

            fun () -> isDisposed <- true
        ),
        [| box appState; box fileTree |]
    )

    let openNote (relativePath: string) =
        match tryResolveArcRelativePath appState relativePath with
        | None ->
            setPreviewData (Some(PageState.Error $"Could not resolve note path '{relativePath}'."))
        | Some absolutePath ->
            promise {
                setSelectedTreeItemPath (Some absolutePath)

                let! result = Api.ipcArcVaultApi.openFile (unbox null) absolutePath

                match result with
                | Ok previewData -> setPreviewData (Some previewData)
                | Result.Error exn ->
                    setPreviewData (Some(PageState.Error $"Could not open note: {exn.Message}"))
            }
            |> Promise.start

    SearchComponent.Main(notes, isLoading, error, openNote)

module MainWindowContentHelper =

    open Fable.Core

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

    let createFromNotes
        (notesUiState: NotesUiState)
        (setNotesUiState: NotesUiState -> unit)
        (setNotesDraft: NotesDraft -> unit)
        appState
        setSelectedTreeItemPath
        setPreviewData
        (payload: NotesSubmitPayload)
        =
        promise {
            setNotesUiState {
                notesUiState with
                    IsSubmitting = true
                    Error = None
            }

            let request: WriteFileRequest = {
                RelativePath = payload.Intent.RelativePath
                Content = payload.Intent.Content
            }

            let! writeResult = Api.ipcArcVaultApi.writeFile (unbox null) request

            match writeResult with
            | Result.Error exn ->
                setNotesUiState {
                    notesUiState with
                        IsSubmitting = false
                        Error = Some $"Failed to write note: {exn.Message}"
                }
            | Ok() ->
                let previewPath = tryResolveArcRelativePath appState payload.Intent.RelativePath

                previewPath |> setSelectedTreeItemPath
                setNotesDraft NotesDraft.init
                setNotesUiState NotesUiState.init

                match previewPath with
                | Some absolutePath ->
                    let! previewResult = Api.ipcArcVaultApi.openFile (unbox null) absolutePath

                    match previewResult with
                    | Ok previewData -> setPreviewData (Some previewData)
                    | Result.Error _ -> setPreviewData (Some(PageState.Text payload.Intent.Content))
                | None -> setPreviewData (Some(PageState.Text payload.Intent.Content))
        }
        |> Promise.start

[<ReactComponent>]
let CreateARCPreview
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

let ComputeARCContent
    previewData
    arcFileState
    setArcFileState
    activeView
    setActiveView
    landingDraft
    setLandingDraft
    landingUiState
    setLandingUiState
    notesDraft
    setNotesDraft
    notesUiState
    setNotesUiState
    availableNotesTargets
    appState
    workspaceFileTree
    setSelectedTreeItemPath
    setPreviewData
    (path: string)
    =

    match previewData with
    | Some data ->
        match data with
        | PageState.ArcFileData _ ->
            match arcFileState with
            | Some arcFile -> CreateARCPreview arcFile setArcFileState activeView setActiveView
            | None ->
                Html.div [
                    prop.className "swt:p-4 swt:text-error"
                    prop.text "Failed to parse ArcFile data"
                ]
        | PageState.Text content ->
            Html.div [
                prop.className "swt:size-full swt:p-4 swt:overflow-auto swt:bg-base-100"
                prop.children [|
                    Html.pre [
                        prop.className "swt:text-sm swt:font-mono"
                        prop.text content
                    ]
                |]
            ]
        | PageState.Unknown ->
            Html.div [
                prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
                prop.children [| Html.h1 "Unknown file type" |]
            ]
        | PageState.Error errMsg ->
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
        | PageState.LandingDraft ->
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
                    setSelectedTreeItemPath
                    setPreviewData
            )
        | PageState.NotesDraft ->
            Notes.Wizard(
                notesDraft,
                setNotesDraft,
                notesUiState,
                setNotesUiState,
                MainWindowContentHelper.createFromNotes
                    notesUiState
                    setNotesUiState
                    setNotesDraft
                    appState
                    setSelectedTreeItemPath
                    setPreviewData,
                availableNotesTargets
            )
        | PageState.NotesSearch ->
            CreateNotesSearchPage(appState, workspaceFileTree, setSelectedTreeItemPath, setPreviewData)
    | None ->
        Html.h1 [
            prop.text path
            prop.className
                "swt:text-xl swt:uppercase swt:inline-block swt:text-transparent swt:bg-clip-text swt:bg-linear-to-r swt:from-primary swt:to-secondary"
        ]

[<ReactComponent>]
let Content
    (
        appState: AppState,
        setArcFileState,
        arcFileState,
        pageState,
        setPreviewData,
        setSelectedTreeItemPath
    ) =

    let landingCtx = React.useContext Renderer.Context.LandingStateCtx.LandingStateCtx

    let notesCtx = React.useContext Renderer.Context.NotesStateCtx.NotesStateCtx

    let workspaceCtx =
        React.useContext Renderer.Context.WorkspaceStateCtx.WorkspaceStateCtx

    let activeView, setActiveView = React.useState PreviewActiveView.Metadata

    let availableNotesTargets =
        React.useMemo (
            (fun _ -> createAvailableNotesTargets workspaceCtx.state.FileTree),
            [| box workspaceCtx.state.FileTree |]
        )

    match appState with
    | AppState.Init ->
        Html.div [
            prop.className "swt:drawer swt:md:drawer-open swt:size-full swt:flex swt:justify-center swt:items-center"
            prop.children [
                Html.div [
                    prop.className "swt:flex-1 swt:flex swt:justify-center swt:items-center"
                    prop.children [ InitState.InitState() ]
                ]
            ]
        ]
    | AppState.ARC path ->

        let activeTableIndex =
            match activeView with
            | PreviewActiveView.Table tableIndex -> Some tableIndex
            | _ -> None

        Html.div [
            prop.className "swt:drawer swt:md:drawer-open swt:size-full swt:flex"
            prop.children [
                Html.div [
                    prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex-none"
                            prop.children [
                                MainElement.CreateARCitectNavbar
                                    arcFileState
                                    activeView
                                    setActiveView
                                    activeTableIndex
                                    setArcFileState
                                    (MainWindowContentHelper.onSaveClick arcFileState setPreviewData)
                            ]
                        ]
                        Html.div [
                            prop.className "swt:flex-1 swt:overflow-y-auto swt:flex swt:flex-col swt:min-w-0"
                            prop.children [
                                ComputeARCContent
                                    pageState
                                    arcFileState
                                    setArcFileState
                                    activeView
                                    setActiveView
                                    landingCtx.state.Draft
                                    (fun draft -> landingCtx.setState { landingCtx.state with Draft = draft })
                                    landingCtx.state.UiState
                                    (fun uiState ->
                                        landingCtx.setState {
                                            landingCtx.state with
                                                UiState = uiState
                                        }
                                    )
                                    notesCtx.state.Draft
                                    (fun draft -> notesCtx.setState { notesCtx.state with Draft = draft })
                                    notesCtx.state.UiState
                                    (fun uiState ->
                                        notesCtx.setState {
                                            notesCtx.state with
                                                UiState = uiState
                                        }
                                    )
                                    availableNotesTargets
                                    appState
                                    workspaceCtx.state.FileTree
                                    setSelectedTreeItemPath
                                    setPreviewData
                                    path
                            ]
                        ]
                    ]
                ]
            ]
        ]
