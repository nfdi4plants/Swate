module Renderer.components.MainWindowContent

open Feliz

open Swate.Electron.Shared.IPCTypes

open ARCtrl

open MainElement
open ExperimentLanding

open Feliz
open Fable.Electron.Remoting.Renderer

open Swate.Components
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes

open Browser.Dom

open ARCtrl

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
    (previewError: string option)
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
    setPreviewError
    setDidSelectFile
    (path: string)
    =
    if landingDraftActive && showLandingDraft then
        ExperimentLandingView(
            landingDraft,
            setLandingDraft,
            landingUiState,
            setLandingUiState,
            LandingPage.createFromLanding
                landingUiState
                setLandingUiState
                landingDraft
                setLandingDraft
                setShowLandingDraft
                setLandingDraftActive
                appState
                setSelectedTreeItemPath
                setPreviewData
                setPreviewError
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
        | None ->
            match previewError with
            | Some errMsg ->
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

let content
    (
        appState: AppState,
        setArcFileState,
        activeTableData,
        activeDataMapData,
        onTableMutated,
        activeView,
        setActiveView,
        arcFileState,
        previewData,
        setPreviewData,
        previewError,
        setPreviewError,
        didSelectFile,
        setDidSelectFile,
        landingDraft,
        setLandingDraft,
        landingUiState,
        setLandingUiState,
        landingDraftActive,
        setLandingDraftActive,
        showLandingDraft,
        setShowLandingDraft,
        setSelectedTreeItemPath
    ) =

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
                        // prop.children [ CreateARCitectNavbar activeView addWidget arcFileState (Navbar.onSaveClick arcFileState setPreviewData setPreviewError setDidSelectFile) ]
                        ]
                        Html.div [
                            prop.className "swt:flex-1 swt:overflow-y-auto swt:flex swt:flex-col swt:min-w-0"
                            prop.children [
                                computeARCContent
                                    previewData
                                    (previewError: string option)
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
                                    setPreviewError
                                    setDidSelectFile
                                    path
                            ]
                        ]
                    ]
                ]
            ]
        ]