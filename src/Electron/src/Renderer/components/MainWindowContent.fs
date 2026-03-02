module Renderer.components.MainWindowContent

open ARCtrl
open Feliz
open Swate.Components.Landing
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open MainElement

let createFromLanding
    landingUiState
    setLandingUiState
    setLandingDraft
    setShowLandingDraft
    setLandingDraftActive
    appState
    setSelectedTreeItemPath
    setPreviewData
    setDidSelectFile
    (payload: SubmitPayload)
    =
    let finishSuccess (previewData: PreviewData) =
        payload.ArcFile
        |> ARCHelper.tryGetArcFilePath appState
        |> setSelectedTreeItemPath

        setPreviewData (Some previewData)
        setDidSelectFile true
        setShowLandingDraft false
        setLandingDraftActive false
        setLandingDraft LandingDraft.init
        setLandingUiState LandingUiState.init

    promise {
        setLandingUiState {
            landingUiState with
                IsSubmitting = true
                Error = None
        }

        let! saveResult = Navbar.saveArcFileWithPreview payload.ArcFile

        match saveResult with
        | Microsoft.FSharp.Core.Error message ->
            setLandingUiState {
                landingUiState with
                    IsSubmitting = false
                    Error = Some message
            }
        | Ok previewData ->
            match payload.ProtocolIntent with
            | None -> finishSuccess previewData
            | Some protocolIntent ->
                let request = {
                    RelativePath = protocolIntent.RelativePath
                    Content = protocolIntent.Content
                }

                let! writeResult = Api.writeFile request

                match writeResult with
                | Ok() -> finishSuccess previewData
                | Microsoft.FSharp.Core.Error exn ->
                    setLandingUiState {
                        landingUiState with
                            IsSubmitting = false
                            Error = Some $"Saved ARC metadata but failed to write protocol file: {exn.Message}"
                    }
    }
    |> Promise.start

[<ReactComponent>]
let createARCPreview
    (arcFile: ArcFiles)
    (setArcFileState: ArcFiles option -> unit)
    (activeView: PreviewActiveView)
    (setActiveView: PreviewActiveView -> unit)
    didSelectFile
    setDidSelectFile
    =

    let setArcFile arcFile = setArcFileState (Some arcFile)

    React.useEffect (
        (fun () ->
            if didSelectFile then
                setArcFile arcFile
                let tables = arcFile.Tables()
                setDidSelectFile false

                if tables.Count > 0 then
                    setActiveView (PreviewActiveView.Table 0)
                else
                    setActiveView PreviewActiveView.Metadata
        ),
        [| box arcFile |]
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

let computeARCContent
    previewData
    arcFileState
    setArcFileState
    activeView
    setActiveView
    didSelectFile
    landingDraft
    setLandingDraft
    landingUiState
    setLandingUiState
    landingDraftActive
    setLandingDraftActive
    showLandingDraft
    setShowLandingDraft
    appState
    setSelectedTreeItemPath
    setPreviewData
    setDidSelectFile
    (path: string)
    =
    if landingDraftActive && showLandingDraft then
        Landing.Wizard(
            landingDraft,
            setLandingDraft,
            landingUiState,
            setLandingUiState,
            createFromLanding
                landingUiState
                setLandingUiState
                setLandingDraft
                setShowLandingDraft
                setLandingDraftActive
                appState
                setSelectedTreeItemPath
                setPreviewData
                setDidSelectFile
        )
    else
        match previewData with
        | Some data ->
            match data with
            | ArcFileData _ ->
                match arcFileState with
                | Some arcFile ->
                    createARCPreview arcFile setArcFileState activeView setActiveView didSelectFile setDidSelectFile
                | None ->
                    Html.div [
                        prop.className "swt:p-4 swt:text-error"
                        prop.text "Failed to parse ArcFile data"
                    ]
            | Text content ->
                Html.div [
                    prop.className "swt:size-full swt:p-4 swt:overflow-auto swt:bg-base-100"
                    prop.children [|
                        Html.pre [
                            prop.className "swt:text-sm swt:font-mono"
                            prop.text content
                        ]
                    |]
                ]
            | Unknown ->
                Html.div [
                    prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
                    prop.children [| Html.h1 "Unknown file type" |]
                ]
            | Error errMsg ->
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
        | None ->
            Html.h1 [
                prop.text path
                prop.className
                    "swt:text-xl swt:uppercase swt:inline-block swt:text-transparent swt:bg-clip-text swt:bg-linear-to-r swt:from-primary swt:to-secondary"
            ]

[<ReactComponent>]
let content
    (
        appState: AppState,
        setAppState,
        setArcFileState,
        activeView,
        setActiveView,
        arcFileState,
        previewData,
        setPreviewData,
        showLandingDraft,
        setShowLandingDraft,
        setSelectedTreeItemPath
    ) =

    let didSelectFile, setDidSelectFile = React.useState false
    let landingDraft, setLandingDraft = React.useState LandingDraft.init
    let landingUiState, setLandingUiState = React.useState LandingUiState.init
    let landingDraftActive, setLandingDraftActive = React.useState false

    let resetLandingDraft () =
        setLandingDraft LandingDraft.init
        setLandingUiState LandingUiState.init
        setLandingDraftActive true
        setShowLandingDraft true
        setSelectedTreeItemPath None
        setDidSelectFile false
        setArcFileState None

    React.useLayoutEffect(
        (fun () ->
            Api.getOpenPath()
            |> Promise.map (fun pathOption ->
                if showLandingDraft then
                    match pathOption with
                    | Some _ ->
                        resetLandingDraft()
                    | None ->
                        setLandingDraftActive false
                        setShowLandingDraft false
                        setSelectedTreeItemPath None
                        setAppState AppState.Init
            )
            |> Promise.start
        ),
        [| box showLandingDraft |]
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
        Html.div [
            prop.className "swt:drawer swt:md:drawer-open swt:size-full swt:flex"
            prop.children [
                Html.div [
                    prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex-none"
                            prop.children [ MainElement.CreateARCitectNavbarList arcFileState (Navbar.onSaveClick arcFileState setPreviewData setDidSelectFile) ]
                        ]
                        Html.div [
                            prop.className "swt:flex-1 swt:overflow-y-auto swt:flex swt:flex-col swt:min-w-0"
                            prop.children [
                                computeARCContent
                                    previewData
                                    arcFileState
                                    setArcFileState
                                    activeView
                                    setActiveView
                                    didSelectFile
                                    landingDraft
                                    setLandingDraft
                                    landingUiState
                                    setLandingUiState
                                    landingDraftActive
                                    setLandingDraftActive
                                    showLandingDraft
                                    setShowLandingDraft
                                    appState
                                    setSelectedTreeItemPath
                                    setPreviewData
                                    setDidSelectFile
                                    path
                            ]
                        ]
                    ]
                ]
            ]
        ]