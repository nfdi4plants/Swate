module Renderer.App

open Feliz
open Fable.Electron.Remoting.Renderer

open Swate.Components
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes

open Browser.Dom

open ARCtrl
open ARCtrl.Json

open Renderer.components


let ParseArcFileFromJson (fileType: ArcFilesDiscriminate) (json: string) : ArcFiles option =
    match ArcFileSaveMapping.tryParseArcFile fileType json with
    | Ok arcFile -> Some arcFile
    | Error e ->
        console.error ("Failed to parse ArcFile JSON: " + e.Message)
        None

[<ReactComponent>]
let Main () =

    let recentARCs, setRecentARCs = React.useState [||]
    let appState, setAppState = React.useState AppState.Init
    let showLandingDraft, setShowLandingDraft = React.useState false
    let (arcFileState: ArcFiles option), setArcFileState = React.useState None
    let (selectedTreeItemPath: string option), setSelectedTreeItemPath = React.useState None
    let (previewData: PreviewData option), (setPreviewData: PreviewData option -> unit) = React.useState None
    let (fileTree: System.Collections.Generic.Dictionary<string, FileEntry>), setFileTree = React.useState (System.Collections.Generic.Dictionary<string, FileEntry>())

    React.useEffect (
        (fun () ->
            match previewData with
            | Some (PreviewData.ArcFileData(fileType, json)) ->
                match ParseArcFileFromJson fileType json with
                | Some arcFile -> setArcFileState (Some arcFile)
                | None -> setArcFileState None
            | _ -> setArcFileState None
        ),
        [| box previewData |]
    )

    React.useEffect (
        (fun () ->
            match arcFileState with
            | Some arcFile ->
                match ArcFileSaveMapping.tryCreateSaveRequest arcFile with
                | Some request ->
                    Api.syncARC request |> Promise.start
                | None -> ()
            | None -> ()
        ),
        [| box arcFileState |]
    )

    ///Used on initializing
    React.useEffectOnce (fun _ ->
        Api.getOpenPath()
        |> Promise.map (fun pathOption ->
            match pathOption with
            | Some p ->
                AppState.ARC p |> setAppState
            | None ->
                setAppState AppState.Init
                setSelectedTreeItemPath None
        )
        |> Promise.start
    )

    let fileExplorer =
        React.useMemo (
            (fun _ ->
                let ra = ResizeArray(fileTree.Values)
                let fileEntries = ra.ToArray()

                let fileTree =
                    if fileEntries.Length > 0 then
                        Some(FileExplorer.getFileTree fileEntries)
                    else
                        None

                if fileTree.IsSome then
                    Some (
                        FileExplorer.CreateFileTree
                            fileTree
                            selectedTreeItemPath
                            setSelectedTreeItemPath
                            setShowLandingDraft
                            setPreviewData
                    )
                else
                    None
            ),
            [|  fileTree; selectedTreeItemPath |]
    )

    let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
        pathChange =
            fun pathOption ->
                console.log ("[Swate] CHANGE PATH!")

                match pathOption with
                | Some p ->
                    AppState.ARC p |> setAppState
                    setSelectedTreeItemPath pathOption
                | None ->
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
                setFileTree fileExplorer
    }

    let selector = Selector.Main(recentARCs, Selector.onARCClick, Selector.actionbar appState, onOpenSelector = Selector.onOpenSelector appState setRecentARCs)

    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    ///Main content module
    let children =
        React.useMemo (
            (fun _ ->
                MainWindowContent.Content(
                    appState,
                    setAppState,
                    setArcFileState,
                    arcFileState,
                    previewData,
                    setPreviewData,
                    showLandingDraft,
                    setShowLandingDraft,
                    setSelectedTreeItemPath)
            ),
            [|
                box appState
                box previewData
                box arcFileState
                box showLandingDraft
            |]
        )

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
                (
                    match fileExplorer with
                    | Some fe ->
                        Some(
                            Html.div [
                                prop.className "swt:p-4"
                                prop.children [|
                                    match appState with
                                    | AppState.ARC _ ->
                                        Html.button [
                                            prop.className "swt:btn swt:btn-sm swt:btn-outline swt:mb-2 swt:w-full"
                                            prop.text "Landing Page"
                                            prop.onClick (fun _ -> setShowLandingDraft (not showLandingDraft))
                                        ]
                                    | _ -> Html.none
                                    Html.h2 [
                                        prop.text "ARC-Tree"
                                    ]
                                    fe
                                |]
                            ]
                        )
                    | None -> None
                 ),
            leftActions = React.Fragment [| Layout.LeftSidebarToggleBtn() |]
        )
    )
