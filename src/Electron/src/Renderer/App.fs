module Renderer.App


open Elmish
open Feliz
open Feliz.UseElmish
open Renderer.Components
open Renderer.Types
open Swate.Components
open Swate.Components.Composite.Layout
open Swate.Components.Primitive.ErrorModal
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

type private Model = {
    // Current ARC root shared with renderer contexts.
    ArcRootPath: ArcRootPath
    // Bumped for main-process live updates so stale snapshot replies are ignored.
    ArcRootPathLiveUpdateVersion: int
    // Bumped for each snapshot request so older async replies cannot win.
    ArcRootPathRequestVersion: int
    PageState: PageState option
    LeftSidebarTarget: LeftSidebarPage
} with

    static member Empty = {
        ArcRootPath = None
        ArcRootPathLiveUpdateVersion = 0
        ArcRootPathRequestVersion = 0
        PageState = None
        LeftSidebarTarget = LeftSidebarPage.FileExplorer
    }

type private Msg =
    | ArcRootPathSnapshotRequested
    | ArcRootPathSnapshotLoaded of requestVersion: int * liveUpdateVersionAtStart: int * Result<ArcRootPath, exn>
    | ArcRootPathChanged of ArcRootPath
    | PageStateChanged of PageState option
    | SetLeftSidebarTarget of LeftSidebarPage

let private init () : Model * Cmd<Msg> = Model.Empty, Cmd.none

let private msgName =
    function
    | ArcRootPathSnapshotRequested -> "ArcRootPathSnapshotRequested"
    | ArcRootPathSnapshotLoaded _ -> "ArcRootPathSnapshotLoaded"
    | ArcRootPathChanged _ -> "ArcRootPathChanged"
    | PageStateChanged _ -> "PageStateChanged"
    | SetLeftSidebarTarget _ -> "SetLeftSidebarTarget"

let private traceUpdateMsg (msg: Msg) =
    console.log ($"[Renderer.App Elmish] {msgName msg}")

let private resetForClosedArc (model: Model) = {
    Model.Empty with
        ArcRootPathLiveUpdateVersion = model.ArcRootPathLiveUpdateVersion
        ArcRootPathRequestVersion = model.ArcRootPathRequestVersion
}

let private applyArcRootPath (arcRootPath: ArcRootPath) (model: Model) =
    match arcRootPath with
    | Some _ -> { model with ArcRootPath = arcRootPath }
    | None -> resetForClosedArc model

let private createGetOpenPathCmd requestVersion liveUpdateVersionAtStart =
    Cmd.OfPromise.either
        Api.ipcArcVaultApi.getOpenPath
        ()
        (fun arcRootPath -> ArcRootPathSnapshotLoaded(requestVersion, liveUpdateVersionAtStart, Ok arcRootPath))
        (fun error -> ArcRootPathSnapshotLoaded(requestVersion, liveUpdateVersionAtStart, Error error))

let private update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    traceUpdateMsg msg

    match msg with
    | ArcRootPathSnapshotRequested ->
        let requestVersion = model.ArcRootPathRequestVersion + 1

        {
            model with
                ArcRootPathRequestVersion = requestVersion
        },
        createGetOpenPathCmd requestVersion model.ArcRootPathLiveUpdateVersion
    | ArcRootPathSnapshotLoaded(requestVersion, liveUpdateVersionAtStart, Ok arcRootPath) when
        requestVersion = model.ArcRootPathRequestVersion
        && liveUpdateVersionAtStart = model.ArcRootPathLiveUpdateVersion
        ->
        model |> applyArcRootPath arcRootPath, Cmd.none
    | ArcRootPathSnapshotLoaded(requestVersion, _, _) when requestVersion = model.ArcRootPathRequestVersion ->
        model, Cmd.none
    | ArcRootPathSnapshotLoaded _ -> model, Cmd.none
    | ArcRootPathChanged appState ->
        let model = {
            model with
                ArcRootPathLiveUpdateVersion = model.ArcRootPathLiveUpdateVersion + 1
        }

        match appState with
        | Some _ -> model |> applyArcRootPath appState, Cmd.none
        | None -> model |> resetForClosedArc, Cmd.none
    | PageStateChanged pageStateOption ->
        {
            model with
                PageState = pageStateOption
        },
        Cmd.none
    | SetLeftSidebarTarget leftSidebarTarget ->
        {
            model with
                LeftSidebarTarget = leftSidebarTarget
        },
        Cmd.none

let private subscribe (_model: Model) : Sub<Msg> = [
    [ "appPathChange" ],
    fun dispatch ->
        let dispose =
            Renderer.IpcReceiver.subscribeProxyReceiver<IPathChangeRendererApi> {
                pathChange = ArcRootPathChanged >> dispatch
            }

        dispatch ArcRootPathSnapshotRequested

        { new System.IDisposable with
            member _.Dispose() = dispose ()
        }
]

[<ReactComponent>]
let private LeftActionButtons (leftSidebarTarget: LeftSidebarPage, setLeftSidebarTarget) =
    let leftSidebarCtx = Swate.Components.Composite.Layout.LeftSidebarContext.useLeftSidebarCtx ()

    let toggleTarget target =
        if leftSidebarTarget = target then
            leftSidebarCtx.setState (not leftSidebarCtx.state)
        else
            leftSidebarCtx.setState true
            setLeftSidebarTarget target

    React.Fragment [
        Layout.LayoutBtn(
            iconClassName = "swt:fluent--home-24-regular",
            tooltip = "File explorer",
            isActive = (leftSidebarTarget = LeftSidebarPage.FileExplorer),
            onClick = fun () -> toggleTarget LeftSidebarPage.FileExplorer
        )
        Layout.LayoutBtn(
            iconClassName = "swt:fluent--branch-fork-24-regular",
            tooltip = "Git",
            isActive = (leftSidebarTarget = LeftSidebarPage.Git),
            onClick = fun () -> toggleTarget LeftSidebarPage.Git
        )
    ]

[<ReactComponent>]
let Main () =
    let model, dispatch = React.useElmish (init, update, subscribe, [||])

    let setPageState (pageState: PageState option) = dispatch (PageStateChanged pageState)

    let pageCtx: StateContext<PageState option> =
        React.useMemo (
            (fun _ -> {
                state = model.PageState
                setState = setPageState
            }),
            [| box model.PageState |]
        )

    let children =
        Renderer.Components.MainContent.Main.Main model.ArcRootPath model.PageState

    let setLeftSidebarTarget =
        React.useCallback ((fun leftSidebarTarget -> dispatch (SetLeftSidebarTarget leftSidebarTarget)), [||])

    let isInitializedArcVault = Option.isSome model.ArcRootPath

    let currentArcScopeId =
        model.ArcRootPath
        |> Option.map Swate.Components.Shared.PathHelpers.normalizePath
        |> Option.bind (fun path ->
            if System.String.IsNullOrWhiteSpace path then
                None
            else
                Some path
        )

    Context.AppStateContext.AppStateCtx.Provider(
        model.ArcRootPath,
        Renderer.Context.FileStateContext.FileStateCtxProvider(
            (fun () -> Api.ipcArcVaultApi.getFileTree ()),
            Renderer.Context.PageStateContext.PageStateCtx.Provider(
                pageCtx,
                ErrorModalProvider.ErrorModalProvider(
                    Renderer.Context.AuthStateContext.Provider(
                        Renderer.Context.GitStateContext.GitStateCtxProvider(
                            Swate.Components.Composite.AnnotationTable.AnnotationTableContextProvider.AnnotationTableContextProvider(
                                Layout.Main(
                                    children =
                                        React.Fragment [|
                                            children
                                            CloseWindowController.CloseWindowController.Subscription()
                                        |],
                                    navbar = Renderer.Components.Navbar.Main(),
                                    ?leftSidebar =
                                        (if isInitializedArcVault then
                                             Renderer.Components.LeftSidebar.Main.Main(model.LeftSidebarTarget) |> Some
                                         else
                                             None),
                                    ?leftActions =
                                        (if isInitializedArcVault then
                                             LeftActionButtons(model.LeftSidebarTarget, setLeftSidebarTarget) |> Some
                                         else
                                             None)
                                )
                            )
                        )
                    ),
                    ?scopeId = currentArcScopeId
                )
            )
        )
    )
