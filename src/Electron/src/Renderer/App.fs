module Renderer.App

open Elmish
open Feliz
open Feliz.UseElmish
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

type SyncRequestKey = string * ArcFilesDiscriminate * string

type Model = {
    AppState: AppState
    ArcFileState: ArcFiles option
    PageState: PageState option
    WorkspaceState: Renderer.Context.WorkspaceStateCtx.WorkspaceState
    PendingSyncRequestKey: SyncRequestKey option
    LastSyncedRequestKey: SyncRequestKey option
}

type Msg =
    | SetAppState of AppState
    | SetArcFileState of ArcFiles option
    | SetWorkspaceState of Renderer.Context.WorkspaceStateCtx.WorkspaceState
    | UpdateWorkspaceState of
        (Renderer.Context.WorkspaceStateCtx.WorkspaceState -> Renderer.Context.WorkspaceStateCtx.WorkspaceState)
    | SetPendingSyncRequestKey of SyncRequestKey option
    | SetLastSyncedRequestKey of SyncRequestKey option
    | OpenPathResolved of string option
    | PathChanged of string option
    | RecentArcsUpdated of SelectorTypes.ARCPointer[]
    | AuthAccountsUpdated
    | FileTreeUpdated of System.Collections.Generic.Dictionary<string, FileEntry>
    | SelectTreeItemPath of string option
    | OpenPreviewRequested of string
    | OpenPreviewResponded of Result<PageState, exn>
    | PageStateChanged of PageState option
    | ArcFileParsed of ArcFiles option
    | OpenLandingPageRequested
    | CreateNewNoteRequested
    | OpenNotesSearchPageRequested
    | SaveBeforeCloseRequested
    | SyncArcRequested
    | SyncArcDebounced of SyncRequestKey
    | SyncArcResponded of SyncRequestKey * Result<unit, exn>
    | GitProgressUpdated

let private tryCreateSyncRequest (model: Model) : (SyncRequestKey * SaveArcFileRequest) option =
    match model.AppState, model.ArcFileState with
    | AppState.ARC arcPath, Some arcFile ->
        match ArcFileSaveMapping.tryCreateSaveRequest arcFile with
        | Some request -> Some((arcPath, request.FileType, request.Json), request)
        | None -> None
    | _ -> None

let private createDebounceCmd (requestKey: SyncRequestKey) : Cmd<Msg> =
    Cmd.OfAsync.perform
        (fun key -> async {
            do! Async.Sleep 250
            return key
        })
        requestKey
        SyncArcDebounced

let private createSyncArcCmd (requestKey: SyncRequestKey) (request: SaveArcFileRequest) : Cmd<Msg> =
    Cmd.OfPromise.either
        (fun request -> Api.ipcArcVaultApi.syncARC (unbox null) request)
        request
        (fun result -> SyncArcResponded(requestKey, result))
        (fun ex -> SyncArcResponded(requestKey, Error ex))

let private createGetOpenPathCmd () : Cmd<Msg> =
    Cmd.OfPromise.either
        (fun () -> Api.ipcArcVaultApi.getOpenPath (unbox null))
        ()
        OpenPathResolved
        (fun _ -> OpenPathResolved None)

let private init () : Model * Cmd<Msg> =
    {
        AppState = AppState.Init
        ArcFileState = None
        PageState = None
        WorkspaceState = Renderer.Context.WorkspaceStateCtx.WorkspaceState.init ()
        PendingSyncRequestKey = None
        LastSyncedRequestKey = None
    },
    createGetOpenPathCmd ()

let private msgName (msg: Msg) =
    match msg with
    | SetAppState _ -> "SetAppState"
    | SetArcFileState _ -> "SetArcFileState"
    | SetWorkspaceState _ -> "SetWorkspaceState"
    | UpdateWorkspaceState _ -> "UpdateWorkspaceState"
    | SetPendingSyncRequestKey _ -> "SetPendingSyncRequestKey"
    | SetLastSyncedRequestKey _ -> "SetLastSyncedRequestKey"
    | OpenPathResolved _ -> "OpenPathResolved"
    | PathChanged _ -> "PathChanged"
    | RecentArcsUpdated _ -> "RecentArcsUpdated"
    | AuthAccountsUpdated -> "AuthAccountsUpdated"
    | FileTreeUpdated _ -> "FileTreeUpdated"
    | SelectTreeItemPath _ -> "SelectTreeItemPath"
    | OpenPreviewRequested _ -> "OpenPreviewRequested"
    | OpenPreviewResponded _ -> "OpenPreviewResponded"
    | PageStateChanged _ -> "PageStateChanged"
    | ArcFileParsed _ -> "ArcFileParsed"
    | OpenLandingPageRequested -> "OpenLandingPageRequested"
    | CreateNewNoteRequested -> "CreateNewNoteRequested"
    | OpenNotesSearchPageRequested -> "OpenNotesSearchPageRequested"
    | SaveBeforeCloseRequested -> "SaveBeforeCloseRequested"
    | SyncArcRequested -> "SyncArcRequested"
    | SyncArcDebounced _ -> "SyncArcDebounced"
    | SyncArcResponded _ -> "SyncArcResponded"
    | GitProgressUpdated -> "GitProgressUpdated"

let private traceUpdateMsg (msg: Msg) =
    console.log ($"[Renderer.App Elmish] {msgName msg}")

let private update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    traceUpdateMsg msg

    match msg with
    | SetAppState appState -> { model with AppState = appState }, Cmd.none
    | SetArcFileState arcFileState ->
        {
            model with
                ArcFileState = arcFileState
        },
        Cmd.none
    | SetWorkspaceState workspaceState ->
        {
            model with
                WorkspaceState = workspaceState
        },
        Cmd.none
    | UpdateWorkspaceState updateWorkspaceState ->
        {
            model with
                WorkspaceState = updateWorkspaceState model.WorkspaceState
        },
        Cmd.none
    | SetPendingSyncRequestKey requestKey ->
        {
            model with
                PendingSyncRequestKey = requestKey
        },
        Cmd.none
    | SetLastSyncedRequestKey requestKey ->
        {
            model with
                LastSyncedRequestKey = requestKey
        },
        Cmd.none
    | OpenPathResolved pathOption
    | PathChanged pathOption ->
        match pathOption with
        | Some p ->
            {
                model with
                    AppState = AppState.ARC p
                    ArcFileState = None
                    PageState = Some PageState.LandingDraft
                    PendingSyncRequestKey = None
                    LastSyncedRequestKey = None
                    WorkspaceState = {
                        model.WorkspaceState with
                            SelectedTreeItemPath = None
                    }
            },
            Cmd.none
        | None ->
            {
                model with
                    AppState = AppState.Init
                    ArcFileState = None
                    PageState = None
                    PendingSyncRequestKey = None
                    LastSyncedRequestKey = None
                    WorkspaceState = {
                        model.WorkspaceState with
                            SelectedTreeItemPath = None
                    }
            },
            Cmd.none
    | RecentArcsUpdated arcs ->
        {
            model with
                WorkspaceState = {
                    model.WorkspaceState with
                        RecentARCs = arcs
                }
        },
        Cmd.none
    | AuthAccountsUpdated -> model, Cmd.none
    | FileTreeUpdated fileTree ->
        let immutableFileTree =
            fileTree.Values
            |> Seq.toArray
            |> Array.sortBy (fun entry -> entry.path)
            |> Array.toList

        {
            model with
                WorkspaceState = {
                    model.WorkspaceState with
                        FileTree = immutableFileTree
                }
        },
        Cmd.none
    | SelectTreeItemPath path ->
        {
            model with
                WorkspaceState = {
                    model.WorkspaceState with
                        SelectedTreeItemPath = path
                }
        },
        Cmd.none
    | OpenPreviewRequested path ->
        {
            model with
                WorkspaceState = {
                    model.WorkspaceState with
                        SelectedTreeItemPath = Some path
                }
        },
        Cmd.none
    | OpenPreviewResponded result ->
        match result with
        | Ok pageState ->
            {
                model with
                    PageState = Some pageState
            },
            Cmd.none
        | Error e ->
            {
                model with
                    PageState = Some(PageState.Error e.Message)
            },
            Cmd.none
    | PageStateChanged pageState ->
        match pageState with
        | Some(PageState.ArcFileData(fileType, json)) ->
            { model with PageState = pageState }, Cmd.ofMsg (ArcFileParsed(parseArcFileFromJson fileType json))
        | _ -> { model with PageState = pageState }, Cmd.none
    | ArcFileParsed arcFile -> { model with ArcFileState = arcFile }, Cmd.ofMsg SyncArcRequested
    | OpenLandingPageRequested ->
        {
            model with
                ArcFileState = None
                PageState = Some PageState.LandingDraft
                WorkspaceState = {
                    model.WorkspaceState with
                        SelectedTreeItemPath = None
                }
        },
        Cmd.none
    | CreateNewNoteRequested ->
        {
            model with
                ArcFileState = None
                PageState = Some PageState.NotesDraft
                WorkspaceState = {
                    model.WorkspaceState with
                        SelectedTreeItemPath = None
                }
        },
        Cmd.none
    | OpenNotesSearchPageRequested ->
        {
            model with
                ArcFileState = None
                PageState = Some PageState.NotesSearch
                WorkspaceState = {
                    model.WorkspaceState with
                        SelectedTreeItemPath = None
                }
        },
        Cmd.none
    | SaveBeforeCloseRequested -> model, Cmd.none
    | SyncArcRequested ->
        match tryCreateSyncRequest model with
        | Some(requestKey, _) when model.PendingSyncRequestKey = Some requestKey -> model, Cmd.none
        | Some(requestKey, _) ->
            {
                model with
                    PendingSyncRequestKey = Some requestKey
            },
            createDebounceCmd requestKey
        | None ->
            {
                model with
                    PendingSyncRequestKey = None
                    LastSyncedRequestKey = None
            },
            Cmd.none
    | SyncArcDebounced requestKey ->
        if
            model.PendingSyncRequestKey <> Some requestKey
            || model.LastSyncedRequestKey = Some requestKey
        then
            model, Cmd.none
        else
            match tryCreateSyncRequest model with
            | Some(currentRequestKey, request) when currentRequestKey = requestKey ->
                model, createSyncArcCmd requestKey request
            | _ -> model, Cmd.none
    | SyncArcResponded(requestKey, result) ->
        if model.PendingSyncRequestKey <> Some requestKey then
            model, Cmd.none
        else
            match result with
            | Ok() ->
                {
                    model with
                        PendingSyncRequestKey = None
                        LastSyncedRequestKey = Some requestKey
                },
                Cmd.none
            | Error _ ->
                {
                    model with
                        PendingSyncRequestKey = None
                },
                Cmd.none
    | GitProgressUpdated -> model, Cmd.none

[<ReactComponent>]
let LeftActionButtons () =
    React.Fragment [

    ]

[<ReactComponent>]
let Main () =

    let model, dispatch = React.useElmish (init, update, [||])

    let setAppState (appState: AppState) = dispatch (SetAppState appState)

    let setArcFileState (arcFileState: ArcFiles option) = dispatch (SetArcFileState arcFileState)

    let setPageState (pageState: PageState option) = dispatch (PageStateChanged pageState)

    let setWorkspaceState updater = dispatch (UpdateWorkspaceState updater)

    let appCtx: StateContext<AppState> =
        React.useMemo (
            (fun _ -> {
                state = model.AppState
                setState = setAppState
            }),
            [| box model.AppState |]
        )

    let workspaceCtx: StateContext<Renderer.Context.WorkspaceStateCtx.WorkspaceState> =
        React.useMemo (
            (fun _ -> {
                state = model.WorkspaceState
                setState = fun nextState -> dispatch (SetWorkspaceState nextState)
            }),
            [| box model.WorkspaceState |]
        )

    let setSelectedTreeItemPath (path: string option) =
        setWorkspaceState (fun state -> {
            state with
                SelectedTreeItemPath = path
        })

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
                    Renderer.Components.FileExplorer.createFileTree fileTree workspaceCtx.state.SelectedTreeItemPath {
                        Renderer.Components.FileExplorer.FileExplorerActions.SetSelectedTreeItemPath =
                            setSelectedTreeItemPath
                        SetPageState = setPageState
                    }
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
                dispatch (PathChanged pathOption)
        recentARCsUpdate = RecentArcsUpdated >> dispatch
        authAccountsUpdate = fun _ -> dispatch AuthAccountsUpdated
        fileTreeUpdate =
            fun fileExplorer ->
                console.log ("[Swate] FILETREE Create!")
                dispatch (FileTreeUpdated fileExplorer)
        gitProgressUpdate =
            fun progress ->
                console.log ($"[Swate] Git progress {progress.Method} {progress.Stage} {progress.Progress}")
                dispatch GitProgressUpdated
    }


    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    ///Main content module
    let children =
        React.useMemo (
            (fun _ ->
                MainWindowContent.Main(
                    model.AppState,
                    model.ArcFileState,
                    model.PageState,
                    {
                        SetArcFileState = setArcFileState
                        SetPreviewData = setPageState
                        SetSelectedTreeItemPath = setSelectedTreeItemPath
                    }
                )
            ),
            [|
                box model.AppState
                box model.PageState
                box model.ArcFileState
            |]
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
                            fun _ -> dispatch OpenLandingPageRequested
                        )
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--document-24-regular swt:size-5",
                            "Create Note",
                            fun _ -> dispatch CreateNewNoteRequested
                        )
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--search-24-regular swt:size-5",
                            "Note Search",
                            fun _ -> dispatch OpenNotesSearchPageRequested
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
        dispatch SaveBeforeCloseRequested

        match model.ArcFileState with
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
                         | Some fe -> leftSidebar model.AppState fe
                         | None -> None),
                    leftActions = React.Fragment [| Layout.LeftSidebarToggleBtn() |]
                )
            )
        )
    )