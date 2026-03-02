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
open components.MainElement


let ParseArcFileFromJson (fileType: ArcFilesDiscriminate) (json: string) : ArcFiles option =
    match ArcFileSaveMapping.tryParseArcFile fileType json with
    | Ok arcFile -> Some arcFile
    | Microsoft.FSharp.Core.Error e ->
        console.error ("Failed to parse ArcFile JSON: " + e.Message)
        None

[<ReactComponent>]
let Main () =

    let recentARCs, setRecentARCs = React.useState [||]
    let fileExplorer, setFileExplorer = React.useState None
    let appState, setAppState = React.useState AppState.Init
    let activeView, setActiveView = React.useState PreviewActiveView.Metadata
    let (arcFileState: ArcFiles option), setArcFileState = React.useState None
    let (previewData: PreviewData option), setPreviewData = React.useState None
    let (selectedTreeItemPath: string option), setSelectedTreeItemPath = React.useState None
    let (fileTree: System.Collections.Generic.Dictionary<string, FileEntry>), setFileTree = React.useState (System.Collections.Generic.Dictionary<string, FileEntry>())

    let showLandingDraft, setShowLandingDraft = React.useState false

    React.useEffect (
        (fun () ->
            match previewData with
            | Some (ArcFileData(fileType, json)) ->
                match ParseArcFileFromJson fileType json with
                | Some arcFile ->
                    match arcFileState with
                    | None ->
                        setArcFileState (Some arcFile)
                    | Some existing when existing.getIdentifier() <> arcFile.getIdentifier() ->
                        setArcFileState (Some arcFile)
                    | _ -> ()
                | None -> ()
            | _ -> ()
        ),
        [| box previewData |]
    )

    ///Used on initializing
    React.useLayoutEffectOnce (fun _ ->
        Api.getOpenPath()
        |> Promise.map (fun pathOption ->
            match pathOption with
            | Some p ->
                AppState.ARC p |> setAppState
            | None ->
                setSelectedTreeItemPath None
                setAppState AppState.Init
        )
        |> Promise.start
    )

    React.useEffect (
        (fun _ ->
            let ra = ResizeArray(fileTree.Values)
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
                    | ArcFiles.Template t ->
                        {
                            FileType = ArcFilesDiscriminate.Template
                            Json = t.toJsonString()
                        }
                    | ArcFiles.DataMap d ->
                        {
                            FileType = ArcFilesDiscriminate.DataMap
                            Json = ""
                        }

                Api.syncARC fileType |> Promise.start

            FileExplorer.createFileTree
                fileTree
                selectedTreeItemPath
                setSelectedTreeItemPath
                setShowLandingDraft
                setPreviewData
                |> setFileExplorer
                |> ignore
        ),
        [| box fileTree; box selectedTreeItemPath |]
    )

    let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
        pathChange =
            fun pathOption ->
                console.log ("[Swate] CHANGE PATH!")

                match pathOption with
                | Some p ->
                    AppState.ARC p |> setAppState
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
                MainWindowContent.content(
                    appState,
                    setAppState,
                    setArcFileState,
                    activeView,
                    setActiveView,
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
                box activeView
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
                                    prop.onClick (fun _ -> setShowLandingDraft (not showLandingDraft)
                                    )
                                ]
                            | _ -> Html.none
                            Html.h2 [
                                prop.text "ARC-Tree"
                            ]
                            sidebarContent
                        |]
                     ]
                 )),
            leftActions = React.Fragment [| Layout.LeftSidebarToggleBtn() |]
        )
    )
