module Renderer.Components.MainWindowContent

open ARCtrl
open Feliz
open Swate.Components.Landing
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open MainElement

module MainWindowContentHelper =

    open Fable.Core

    let saveArcFileWithPreview (arcFile: ArcFiles) : JS.Promise<Result<PageState, string>> = promise {
        match ArcFileSaveMapping.tryCreateSaveRequest arcFile with
        | None -> return Error "Saving this file type is not supported in Electron yet."
        | Some request ->
            let! saveResult = Api.saveArcFile request

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

                    let! writeResult = Api.writeFile request

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
    appState
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
    | None ->
        Html.h1 [
            prop.text path
            prop.className
                "swt:text-xl swt:uppercase swt:inline-block swt:text-transparent swt:bg-clip-text swt:bg-linear-to-r swt:from-primary swt:to-secondary"
        ]

[<ReactComponent>]
let Content (appState: AppState, setArcFileState, arcFileState, pageState, setPreviewData) =

    let landingCtx = React.useContext Renderer.Context.LandingStateCtx.LandingStateCtx

    let workspaceCtx =
        React.useContext Renderer.Context.WorkspaceStateCtx.WorkspaceStateCtx

    let activeView, setActiveView = React.useState PreviewActiveView.Metadata

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
        Html.div [
            prop.className "swt:drawer swt:md:drawer-open swt:size-full swt:flex"
            prop.children [
                Html.div [
                    prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex-none"
                            prop.children [
                                MainElement.CreateARCitectNavbarList
                                    arcFileState
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
                                    appState
                                    (fun path ->
                                        workspaceCtx.setState {
                                            workspaceCtx.state with
                                                SelectedTreeItemPath = path
                                        }
                                    )
                                    setPreviewData
                                    path
                            ]
                        ]
                    ]
                ]
            ]
        ]