module Renderer.App

open Feliz
open Fable.Core
open Fable.Electron.Remoting.Renderer

open Swate.Components
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes

open Browser.Dom

open ARCtrl
open ARCtrl.Json

open Renderer.components
open components.MainElement
open components.ExperimentLanding


let ParseArcFileFromJson (fileType: ArcFilesDiscriminate) (json: string) : ArcFiles option =
    match ArcFileSaveMapping.tryParseArcFile fileType json with
    | Ok arcFile -> Some arcFile
    | Error e ->
        console.error ("Failed to parse ArcFile JSON: " + e.Message)
        None

[<ReactComponent>]
let Main () =

    let tableMutationTick, setTableMutationTick = React.useStateWithUpdater 0
    let recentARCs, setRecentARCs = React.useState [||]
    let fileExplorer, setFileExplorer = React.useState None
    let didSelectFile, setDidSelectFile = React.useState false
    let appState, setAppState = React.useState (AppState.Init)
    let (arcFileState: ArcFiles option), setArcFileState = React.useState None
    let activeView, setActiveView = React.useState PreviewActiveView.Metadata
    let (previewError: string option), setPreviewError = React.useState (None)
    let (previewData: PreviewData option), setPreviewData = React.useState (None)

    let (files: System.Collections.Generic.Dictionary<string, FileEntry>), setFiles =
        React.useState (System.Collections.Generic.Dictionary<string, FileEntry>())

    let (selectedTreeItemPath: string option), setSelectedTreeItemPath =
        React.useState (None)

    let landingDraft, setLandingDraft = React.useState LandingDraft.init
    let landingUiState, setLandingUiState = React.useState LandingUiState.init
    let landingDraftActive, setLandingDraftActive = React.useState false
    let showLandingDraft, setShowLandingDraft = React.useState false

    let resetLandingDraft () =
        setLandingDraft LandingDraft.init
        setLandingUiState LandingUiState.init
        setLandingDraftActive true
        setShowLandingDraft true
        setPreviewData None
        setPreviewError None
        setSelectedTreeItemPath None
        setDidSelectFile false
        setArcFileState None

    React.useEffect (
        (fun () ->
            match previewData with
            | Some(ArcFileData(fileType, json)) ->
                match ParseArcFileFromJson fileType json with
                | Some arcFile ->
                    match arcFileState with
                    | None -> setArcFileState (Some arcFile)
                    | Some existing when existing.getIdentifier () <> arcFile.getIdentifier () ->
                        setArcFileState (Some arcFile)
                    | _ -> ()
                | None -> ()
            | _ -> ()
        ),
        [| box previewData |]
    )

    // Used on initializing
    React.useLayoutEffectOnce (fun _ ->
        Api.getOpenPath()
        |> Promise.map (fun pathOption ->
            match pathOption with
            | Some p ->
                resetLandingDraft ()
                AppState.ARC p |> setAppState
            | None ->
                setLandingDraftActive false
                setShowLandingDraft false
                setSelectedTreeItemPath None
                setAppState AppState.Init
        )
        |> Promise.start
    )

    React.useEffect (
        (fun _ ->
            let ra = ResizeArray(files.Values)
            let fileEntries = ra.ToArray()

            let fileTree =
                if fileEntries.Length > 0 then
                    Some(FileExplorer.getFileTree fileEntries)
                else
                    None

            if arcFileState.IsSome then
                let fileType: SaveArcFileRequest =
                    match arcFileState.Value with
                    | ArcFiles.Assay a -> 
                        {
                            FileType = ArcFilesDiscriminate.Assay
                            Json = a.ToJsonString()
                        }
                    | ArcFiles.Investigation i ->
                        {
                            FileType = ArcFilesDiscriminate.Investigation
                            Json = i.ToJsonString()
                        }
                    | ArcFiles.Run r ->
                        {
                            FileType = ArcFilesDiscriminate.Run
                            Json = r.ToJsonString()
                        }
                    | ArcFiles.Study (s, _) ->
                        {
                            FileType = ArcFilesDiscriminate.Study
                            Json = s.ToJsonString()
                        }
                    | ArcFiles.Workflow w ->
                        {
                            FileType = ArcFilesDiscriminate.Workflow
                            Json = w.ToJsonString()
                        }
                    | _ ->
                        {
                            FileType = ArcFilesDiscriminate.Template
                            Json = ""
                        }
                Api.syncARC fileType |> Promise.start

            FileExplorer.createFileTree
                fileTree
                selectedTreeItemPath
                setSelectedTreeItemPath
                setShowLandingDraft
                setPreviewData
                setPreviewError
                setDidSelectFile
            |> setFileExplorer
            |> ignore
        ),
        [| box files; box selectedTreeItemPath |]
    )

    let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
        pathChange =
            fun pathOption ->
                console.log ("[Swate] CHANGE PATH!")

                match pathOption with
                | Some p ->
                    resetLandingDraft ()
                    AppState.ARC p |> setAppState
                | None ->
                    setLandingDraftActive false
                    setShowLandingDraft false
                    setSelectedTreeItemPath None
                    setAppState AppState.Init
        recentARCsUpdate =
            fun arcs ->
                console.log ("[Swate] CHANGE RECENTARCS!")
                setRecentARCs arcs
        fileTreeUpdate =
            fun fileExplorer ->
                console.log ("[Swate] FILETREE Create!")
                setFiles fileExplorer
    }

    let recentARCElements =
        recentARCs
        |> Array.map (fun arcPointer -> Selector.SelectorItem(arcPointer, Selector.onARCClick))

    let selector =
        Selector.Main(
            recentARCElements,
            Selector.actionbar appState,
            onOpenSelector = Selector.onOpenSelector appState setRecentARCs
        )

    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    let onTableMutated () =
        setTableMutationTick (fun latest -> latest + 1)

    ///Main content module
    let children = Html.div "placeholder"

    let navbar = Navbar.Main(selector)

    context.AppStateCtx.AppStateCtx.Provider(
        {
            state = appState
            setState = setAppState
        },
        Layout.Main(
            children = children,
            navbar = navbar,
            ?leftSidebar =
                (let sidebarContent =
                    match fileExplorer with
                    | Some fe -> fe
                    | None -> Html.span [ prop.className "swt:opacity-50"; prop.text "No files" ]

                 Some(
                     Html.div [
                         prop.className "swt:p-4"
                         prop.children [|
                             match appState with
                             | AppState.ARC _ ->
                                 Html.button [
                                     prop.className "swt:btn swt:btn-sm swt:btn-outline swt:mb-2 swt:w-full"
                                     prop.text "Landing Page"
                                     prop.onClick (fun _ ->
                                         setPreviewError None

                                         if landingDraftActive then
                                             setShowLandingDraft true
                                         else
                                             resetLandingDraft ()
                                     )
                                 ]
                             | _ -> Html.none
                             Html.h2 [ prop.text "ARC-Tree" ]
                             sidebarContent
                         |]
                     ]
                 )),
            leftActions = React.Fragment [| Layout.LeftSidebarToggleBtn() |]
        )
    )