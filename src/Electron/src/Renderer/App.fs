module Renderer.App

open Feliz
open Fable.Electron.Remoting.Renderer
open Fable.Core

open Swate.Components
open Swate.Components.Landing
open Swate.Electron.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper

open Browser.Dom

open ARCtrl

open Renderer.Components


let parseArcFileFromJson (fileType: ArcFilesDiscriminate) (json: string) : ArcFiles option =
    match ArcFileSaveMapping.tryParseArcFile fileType json with
    | Ok arcFile -> Some arcFile
    | Result.Error e ->
        console.error ("Failed to parse ArcFile JSON: " + e.Message)
        None

[<ReactComponent>]
let LeftActionButtons () =
    React.Fragment [

    ]

[<ReactComponent>]
let Main () =

    let appState, setAppState = React.useState AppState.Init
    let (arcFileState: ArcFiles option), setArcFileState = React.useState None

    let (pageState: PageState option), (setPageState: PageState option -> unit) =
        React.useState None

    let landingState, setLandingState =
        React.useState (Renderer.Context.LandingStateCtx.LandingState.init ())

    let workspaceState, setWorkspaceState =
        React.useState (Renderer.Context.WorkspaceStateCtx.WorkspaceState.init ())

    let syncArcTimeoutRef = React.useRef<int option> None

    let lastSyncedRequestKeyRef =
        React.useRef<(string * ArcFilesDiscriminate * string) option> None

    let appCtx: StateContext<AppState> =
        React.useMemo (
            (fun _ -> {
                state = appState
                setState = setAppState
            }),
            [| box appState |]
        )

    let landingCtx: StateContext<Renderer.Context.LandingStateCtx.LandingState> =
        React.useMemo (
            (fun _ -> {
                state = landingState
                setState = setLandingState
            }),
            [| box landingState |]
        )

    let workspaceCtx: StateContext<Renderer.Context.WorkspaceStateCtx.WorkspaceState> =
        React.useMemo (
            (fun _ -> {
                state = workspaceState
                setState = setWorkspaceState
            }),
            [| box workspaceState |]
        )

    let setSelectedTreeItemPath (path: string option) =
        workspaceCtx.setState {
            workspaceCtx.state with
                SelectedTreeItemPath = path
        }

    let setRecentARCs (arcs: SelectorTypes.ARCPointer[]) =
        workspaceCtx.setState {
            workspaceCtx.state with
                RecentARCs = arcs
        }

    let setFileTree (fileTree: System.Collections.Generic.Dictionary<string, FileEntry>) =
        let immutableFileTree =
            fileTree.Values
            |> Seq.toArray
            |> Array.sortBy (fun entry -> entry.path)
            |> Array.toList

        workspaceCtx.setState {
            workspaceCtx.state with
                FileTree = immutableFileTree
        }

    React.useEffect (
        (fun () ->
            match pageState with
            | Some(PageState.ArcFileData(fileType, json)) ->
                match parseArcFileFromJson fileType json with
                | Some arcFile -> setArcFileState (Some arcFile)
                | None -> setArcFileState None
            | _ -> ()
        ),
        [| box pageState |]
    )

    React.useEffect (
        (fun () ->
            let clearPendingSync () =
                match syncArcTimeoutRef.current with
                | Some timeoutId ->
                    Fable.Core.JS.clearTimeout timeoutId
                    syncArcTimeoutRef.current <- None
                | None -> ()

            clearPendingSync ()

            match appState, arcFileState with
            | AppState.ARC arcPath, Some arcFile ->
                match ArcFileSaveMapping.tryCreateSaveRequest arcFile with
                | Some request ->
                    let requestKey = (arcPath, request.FileType, request.Json)

                    let timeoutId =
                        Fable.Core.JS.setTimeout
                            (fun () ->
                                if lastSyncedRequestKeyRef.current <> Some requestKey then
                                    Api.ipcArcVaultApi.syncARC (unbox null) request |> Promise.start
                                    lastSyncedRequestKeyRef.current <- Some requestKey

                                syncArcTimeoutRef.current <- None
                            )
                            250

                    syncArcTimeoutRef.current <- Some timeoutId
                | None -> lastSyncedRequestKeyRef.current <- None
            | _ -> lastSyncedRequestKeyRef.current <- None

            FsReact.createDisposable (fun () -> clearPendingSync ())
        ),
        [| box appState; box arcFileState |]
    )

    // Used on initializing
    React.useEffectOnce (fun _ ->
        Api.ipcArcVaultApi.getOpenPath (unbox null)
        |> Promise.map (fun pathOption ->
            match pathOption with
            | Some p -> AppState.ARC p |> setAppState
            | None ->
                setAppState AppState.Init
                setSelectedTreeItemPath None
        )
        |> Promise.start
    )

    let fileExplorer =
        React.useMemo (
            (fun _ ->
                let fileEntries = workspaceCtx.state.FileTree |> List.toArray

                let fileTree =
                    if fileEntries.Length > 0 then
                        Some(Renderer.Components.FileExplorer.getFileTree fileEntries)
                    else
                        None

                if fileTree.IsSome then
                    Renderer.Components.FileExplorer.createFileTree
                        fileTree
                        workspaceCtx.state.SelectedTreeItemPath
                        setSelectedTreeItemPath
                        setPageState
                else
                    None
            ),
            [|
                workspaceCtx.state.FileTree
                workspaceCtx.state.SelectedTreeItemPath
            |]
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
        recentARCsUpdate = ignore
        fileTreeUpdate =
            fun fileExplorer ->
                console.log ("[Swate] FILETREE Create!")
                setFileTree fileExplorer
        gitProgressUpdate =
            fun progress -> console.log ($"[Swate] Git progress {progress.Method} {progress.Stage} {progress.Progress}")
    }


    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    ///Main content module
    let children =
        React.useMemo (
            (fun _ -> MainWindowContent.Content(appState, setArcFileState, arcFileState, pageState, setPageState)),
            [| box appState; box pageState; box arcFileState |]
        )

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
                                landingCtx.setState {
                                    landingCtx.state with
                                        Draft = LandingDraft.init
                                        UiState = LandingUiState.init
                                }

                                setSelectedTreeItemPath None
                                setArcFileState None
                                setPageState (Some PageState.LandingDraft)
                            )
                        ]
                    | _ -> Html.none
                    Html.h2 [ prop.text "ARC-Tree" ]
                    fe
                |]
            ]
        )

    let saveBeforeClose () : JS.Promise<Result<unit, string>> = promise {
        match arcFileState with
        | None -> return Ok()
        | Some arcFile ->
            let! result = MainWindowContent.MainWindowContentHelper.saveArcFileWithPreview arcFile

            match result with
            | Ok updatedPreview ->
                setPageState (Some updatedPreview)
                return Ok()
            | Result.Error errorMsg ->
                let msg = $"Save failed: {errorMsg}"
                return Result.Error msg
    }

    Context.AppStateCtx.AppStateCtx.Provider(
        appCtx,
        Renderer.Context.WorkspaceStateCtx.WorkspaceStateCtx.Provider(
            workspaceCtx,
            Renderer.Context.LandingStateCtx.LandingStateCtx.Provider(
                landingCtx,
                AnnotationTableContextProvider.AnnotationTableContextProvider(
                    Layout.Main(
                        children =
                            React.Fragment [|
                                children
                                CloseWindowController.CloseWindowController.Subscription(saveBeforeClose)
                            |],
                        navbar = Renderer.Components.Navbar.Main(),
                        ?leftSidebar =
                            (match fileExplorer with
                             | Some fe -> leftSidebar appState fe
                             | None -> None),
                        leftActions = React.Fragment [| Layout.LeftSidebarToggleBtn() |]
                    )
                )
            )
        )
    )
