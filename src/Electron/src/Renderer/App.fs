module Renderer.App

open Feliz
open Fable.Electron.Remoting.Renderer
open Fable.Core

open Swate.Components
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

    let notesState, setNotesState =
        React.useState (Renderer.Context.NotesStateCtx.NotesState.init ())

    let workspaceState, setWorkspaceState =
        React.useStateWithUpdater (Renderer.Context.WorkspaceStateCtx.WorkspaceState.init ())

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
                setState = fun nextState -> setWorkspaceState (fun _ -> nextState)
            }),
            [| box workspaceState |]
        )

    let notesCtx: StateContext<Renderer.Context.NotesStateCtx.NotesState> =
        React.useMemo (
            (fun _ -> {
                state = notesState
                setState = setNotesState
            }),
            [| box notesState |]
        )

    let setSelectedTreeItemPath (path: string option) =
        setWorkspaceState (fun state -> {
            state with
                SelectedTreeItemPath = path
        })

    let setRecentARCs (arcs: SelectorTypes.ARCPointer[]) =
        setWorkspaceState (fun state -> {
            state with
                RecentARCs = arcs
        })

    let setFileTree (fileTree: System.Collections.Generic.Dictionary<string, FileEntry>) =
        let immutableFileTree =
            fileTree.Values
            |> Seq.toArray
            |> Array.sortBy (fun entry -> entry.path)
            |> Array.toList

        setWorkspaceState (fun state -> {
            state with
                FileTree = immutableFileTree
        })

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
            | Some p ->
                AppState.ARC p |> setAppState
                Renderer.Navigation.PageActions.openArcStartPage notesCtx landingCtx setArcFileState setSelectedTreeItemPath setPageState
            | None ->
                setAppState AppState.Init
                setSelectedTreeItemPath None
                setPageState None
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
                    Renderer.Navigation.PageActions.openArcStartPage notesCtx landingCtx setArcFileState setSelectedTreeItemPath setPageState
                | None ->
                    setSelectedTreeItemPath None
                    setPageState None
                    setArcFileState None
                    setAppState AppState.Init
        recentARCsUpdate = ignore
        authAccountsUpdate = ignore
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
            (fun _ ->
                MainWindowContent.Content(
                    appState,
                    setArcFileState,
                    arcFileState,
                    pageState,
                    setPageState,
                    setSelectedTreeItemPath
                )),
            [| box appState; box pageState; box arcFileState |]
        )

    let actionBar =
        Html.div [
            prop.className "swt:mb-2 swt:flex swt:justify-center"
            prop.children [
                Actionbar.Main(
                    [|
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--document-bullet-list-24-regular swt:size-5",
                            "Labbook View",
                            fun () -> Renderer.Navigation.PageActions.openLandingPage landingCtx setArcFileState setSelectedTreeItemPath setPageState
                        )
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--document-24-regular swt:size-5",
                            "Create Note",
                            fun () -> Renderer.Navigation.PageActions.createNewNote notesCtx setArcFileState setSelectedTreeItemPath setPageState
                        )
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--search-24-regular swt:size-5",
                            "Note Search",
                            fun () -> Renderer.Navigation.PageActions.openNotesSearchPage setArcFileState setSelectedTreeItemPath setPageState
                        )
                    |],
                    4
                )
            ]
        ]

    let leftSidebar appState (fe: ReactElement) =
        Some(
            Html.div [
                prop.className "swt:p-4"
                prop.children [|
                    match appState with
                    | AppState.ARC _ -> actionBar
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
                Renderer.Context.NotesStateCtx.NotesStateCtx.Provider(
                    notesCtx,
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
    )
