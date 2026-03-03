module Renderer.App

open Feliz
open Fable.Electron.Remoting.Renderer
open Fable.Core

open Swate.Components
open Swate.Components.Landing
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes

open Browser.Dom

open ARCtrl

open Renderer.components


let ParseArcFileFromJson (fileType: ArcFilesDiscriminate) (json: string) : ArcFiles option =
    match ArcFileSaveMapping.tryParseArcFile fileType json with
    | Ok arcFile -> Some arcFile
    | Result.Error e ->
        console.error ("Failed to parse ArcFile JSON: " + e.Message)
        None

[<ReactComponent>]
let Main () =

    let recentARCs, setRecentARCs = React.useState [||]
    let appState, setAppState = React.useState AppState.Init
    let (arcFileState: ArcFiles option), setArcFileState = React.useState None
    let (selectedTreeItemPath: string option), setSelectedTreeItemPath = React.useState None
    let (pageState: PageState option), (setPageState: PageState option -> unit) = React.useState None
    let (fileTree: System.Collections.Generic.Dictionary<string, FileEntry>), setFileTree = React.useState (System.Collections.Generic.Dictionary<string, FileEntry>())
    let landingDraft, setLandingDraft = React.useState LandingDraft.init
    let landingUiState, setLandingUiState = React.useState LandingUiState.init


    let landingStateCtxValue: Renderer.context.LandingStateCtx.LandingStateContext =
        React.useMemo (
            (fun _ -> {
                Draft = landingDraft
                SetDraft = setLandingDraft
                UiState = landingUiState
                SetUiState = setLandingUiState
            }),
            [| box landingDraft; box landingUiState |]
        )

    React.useEffect (
        (fun () ->
            match pageState with
            | Some (PageState.ArcFileData(fileType, json)) ->
                match ParseArcFileFromJson fileType json with
                | Some arcFile -> setArcFileState (Some arcFile)
                | None -> setArcFileState None
            | _ -> setArcFileState None
        ),
        [| box pageState |]
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
                            setPageState
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
                    setArcFileState,
                    arcFileState,
                    pageState,
                    setPageState,
                    setSelectedTreeItemPath)
            ),
            [|
                box appState
                box pageState
                box arcFileState
            |]
        )

    let navbar = Navbar.Main(selector)

    let leftSidebar appState (fe: ReactElement) =
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
                                setLandingDraft LandingDraft.init
                                setLandingUiState LandingUiState.init
                                setSelectedTreeItemPath None
                                setArcFileState None
                                setPageState (Some PageState.LandingDraft)
                            )
                        ]
                    | _ -> Html.none
                    Html.h2 [
                        prop.text "ARC-Tree"
                    ]
                    fe
                |]
            ]
        )

    let saveBeforeClose () : JS.Promise<Result<unit, string>> =
        promise {
            match arcFileState with
            | None -> return Ok()
            | Some arcFile ->
                let! result = Navbar.saveArcFileWithPreview arcFile

                match result with
                | Ok updatedPreview ->
                    setPageState (Some updatedPreview)
                    return Ok()
                | Result.Error errorMsg ->
                    let msg = $"Save failed: {errorMsg}"
                    return Result.Error msg
        }

    context.AppStateCtx.AppStateCtx.Provider(
        {
            state = appState
            setState = setAppState
        },
        Renderer.context.LandingStateCtx.LandingStateCtx.Provider(
            landingStateCtxValue,
            Layout.Main(
                children =
                    React.Fragment [|
                        children
                        CloseWindowController.CloseWindowController.Subscription(saveBeforeClose)
                    |],
                navbar = navbar,
                ?leftSidebar =
                    (
                        match fileExplorer with
                        | Some fe -> leftSidebar appState fe
                        | None -> None
                     ),
                leftActions = React.Fragment [| Layout.LeftSidebarToggleBtn() |]
            )
        )
    )
