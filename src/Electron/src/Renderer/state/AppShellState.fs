module Renderer.state.AppShellState

open System.Collections.Generic
open Feliz
open Fable.Electron.Remoting.Renderer
open Browser.Dom

open ARCtrl
open ARCtrl.Json

open Swate.Components
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Swate.Components.Landing

open Renderer.components.MainElement
open Renderer.components.FileExplorer

type AppShellState = {
    recentARCs: SelectorTypes.ARCPointer[]
    setRecentARCs: SelectorTypes.ARCPointer[] -> unit
    appState: AppState
    setAppState: AppState -> unit
    arcFileState: ArcFiles option
    setArcFileState: ArcFiles option -> unit
    activeView: PreviewActiveView
    setActiveView: PreviewActiveView -> unit
    previewData: PreviewData option
    setPreviewData: PreviewData option -> unit
    fileExplorer: ReactElement option
    landingDraftActive: bool
    setLandingDraftActive: bool -> unit
    selectedTreeItemPath: string option
    setSelectedTreeItemPath: string option -> unit
}

let private parseArcFileFromJson (fileType: ArcFilesDiscriminate) (json: string) : ArcFiles option =
    match ArcFileSaveMapping.tryParseArcFile fileType json with
    | Ok arcFile -> Some arcFile
    | Microsoft.FSharp.Core.Error e ->
        console.error ("Failed to parse ArcFile JSON: " + e.Message)
        None

let private tryCreateSyncRequest (arcFile: ArcFiles) : SaveArcFileRequest option =
    match arcFile with
    | ArcFiles.Assay assay ->
        Some {
            FileType = ArcFilesDiscriminate.Assay
            Json = assay.ToJsonString()
        }
    | ArcFiles.Investigation investigation ->
        Some {
            FileType = ArcFilesDiscriminate.Investigation
            Json = investigation.ToJsonString()
        }
    | ArcFiles.Run run ->
        Some {
            FileType = ArcFilesDiscriminate.Run
            Json = run.ToJsonString()
        }
    | ArcFiles.Study(study, _) ->
        Some {
            FileType = ArcFilesDiscriminate.Study
            Json = study.ToJsonString()
        }
    | ArcFiles.Workflow workflow ->
        Some {
            FileType = ArcFilesDiscriminate.Workflow
            Json = workflow.ToJsonString()
        }
    | ArcFiles.Template template ->
        Some {
            FileType = ArcFilesDiscriminate.Template
            Json = template.toJsonString()
        }
    | ArcFiles.DataMap _ ->
        Some {
            FileType = ArcFilesDiscriminate.DataMap
            Json = ""
        }

let useAppShellState () : AppShellState =

    let (recentARCs: SelectorTypes.ARCPointer[]), setRecentARCs = React.useState [||]
    let appState, setAppState = React.useState AppState.Init
    let (arcFileState: ArcFiles option), setArcFileState = React.useState None
    let activeView, setActiveView = React.useState PreviewActiveView.Metadata
    let (previewData: PreviewData option), setPreviewData = React.useState None

    let landingDraftActive, setLandingDraftActive = React.useState false
    let (selectedTreeItemPath: string option), setSelectedTreeItemPath = React.useState None
    let (fileTree: Dictionary<string, FileEntry>), setFileTree = React.useState (Dictionary<string, FileEntry>())

    React.useEffect (
        (fun () ->
            match previewData with
            | Some (ArcFileData(fileType, json)) ->
                match parseArcFileFromJson fileType json with
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

    React.useLayoutEffectOnce (fun _ ->
        Api.getOpenPath()
        |> Promise.map (fun pathOption ->
            match pathOption with
            | Some path ->
                Landing.ResetLandingDraft(setPreviewData, setSelectedTreeItemPath)
                AppState.ARC path |> setAppState
            | None ->
                setLandingDraftActive false
                setSelectedTreeItemPath None
                setAppState AppState.Init
        )
        |> Promise.start
    )

    React.useEffect (
        (fun () ->
            match arcFileState with
            | Some arcFile ->
                match tryCreateSyncRequest arcFile with
                | Some request -> Api.syncARC request |> Promise.start
                | None -> ()
            | None -> ()
        ),
        [| box arcFileState |]
    )

    let ipcHandler: IMainUpdateRendererApi = {
        pathChange =
            fun pathOption ->
                console.log ("[Swate] CHANGE PATH!")

                match pathOption with
                | Some path ->
                    Landing.ResetLandingDraft(setPreviewData, setSelectedTreeItemPath)
                    AppState.ARC path |> setAppState
                | None ->
                    setLandingDraftActive false
                    setSelectedTreeItemPath None
                    setAppState AppState.Init
        recentARCsUpdate =
            fun arcs ->
                console.log ("[Swate] CHANGE RECENTARCS!")
                setRecentARCs arcs
        fileTreeUpdate =
            fun updatedFileTree ->
                console.log ("[Swate] FILETREE Create!")
                setFileTree updatedFileTree
    }

    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    let fileExplorer =
        React.useMemo (
            (fun _ ->
                let entries = ResizeArray(fileTree.Values).ToArray()

                let fileTreeRoot =
                    if entries.Length > 0 then
                        Some(getFileTree entries)
                    else
                        None

                createFileTree fileTreeRoot selectedTreeItemPath setSelectedTreeItemPath setPreviewData
            ),
            [| box fileTree; box selectedTreeItemPath |]
        )

    {
        recentARCs = recentARCs
        setRecentARCs = setRecentARCs
        appState = appState
        setAppState = setAppState
        arcFileState = arcFileState
        setArcFileState = setArcFileState
        activeView = activeView
        setActiveView = setActiveView
        previewData = previewData
        setPreviewData = setPreviewData
        fileExplorer = fileExplorer
        landingDraftActive = landingDraftActive
        setLandingDraftActive = setLandingDraftActive
        selectedTreeItemPath = selectedTreeItemPath
        setSelectedTreeItemPath = setSelectedTreeItemPath
    }
