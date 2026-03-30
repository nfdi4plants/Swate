module Renderer.Context.ArcObjectExplorerCtx

open ARCtrl
open Feliz
open Swate.Components
open Fable.Electron.Remoting.Renderer

type ArcObjectExplorerState = {
    Nodes: ArcExplorerNode list
    SelectedExplorerItemId: string option
    SelectedKindIndices: Set<int>
    ArcFileState: ArcFiles option
    PreviewState: ArcObjectPreviewState
    StatusMessage: string option
} with

    static member init() = {
        Nodes = []
        SelectedExplorerItemId = None
        SelectedKindIndices = Swate.Components.ARCObjectWidget.DefaultKindFilterIndices()
        ArcFileState = None
        PreviewState = ArcObjectPreviewState.NoneLoaded
        StatusMessage = None
    }

type ArcObjectExplorerController = {
    state: ArcObjectExplorerState
    setState: (ArcObjectExplorerState -> ArcObjectExplorerState) -> unit
    setNodes: ArcExplorerNode list -> unit
    setSelectedExplorerItemId: string option -> unit
    setSelectedKindIndices: Set<int> -> unit
    setArcFileState: ArcFiles option -> unit
    setPreviewState: ArcObjectPreviewState -> unit
    setStatusMessage: string option -> unit
}

let ArcObjectExplorerCtx =
    React.createContext<ArcObjectExplorerController> (
        {
            state = ArcObjectExplorerState.init ()
            setState = ignore
            setNodes = ignore
            setSelectedExplorerItemId = ignore
            setSelectedKindIndices = ignore
            setArcFileState = ignore
            setPreviewState = ignore
            setStatusMessage = ignore
        }
    )

[<Hook>]
let useArcObjectExplorer () = React.useContext ArcObjectExplorerCtx

let rec private containsNodeId (nodeId: string) (nodes: ArcExplorerNode list) =
    nodes
    |> List.exists (fun node -> node.id = nodeId || containsNodeId nodeId node.children)

[<ReactComponent>]
let ArcObjectExplorerCtxProvider (children: ReactElement) =
    let appStateCtx = Renderer.Context.AppStateCtx.useAppState ()
    let fileStateCtx = Renderer.Context.FileStateCtx.useFileState ()
    let state, setState = React.useStateWithUpdater (ArcObjectExplorerState.init ())

    React.useEffect (
        (fun () ->
            match appStateCtx.state with
            | None -> setState (fun _ -> ArcObjectExplorerState.init ())
            | Some _ ->
                promise {
                    let! result = Api.ipcArcVaultApi.getArcObjectTree (unbox null)

                    match result with
                    | Ok nodes ->
                        setState (fun currentState ->
                            let selectedExplorerItemId =
                                currentState.SelectedExplorerItemId
                                |> Option.filter (fun nodeId -> containsNodeId nodeId nodes)

                            {
                                currentState with
                                    Nodes = nodes
                                    SelectedExplorerItemId = selectedExplorerItemId
                                    StatusMessage = None
                            })
                    | Error exn ->
                        setState (fun currentState -> {
                            currentState with
                                Nodes = []
                                SelectedExplorerItemId = None
                                ArcFileState = None
                                PreviewState = ArcObjectPreviewState.Error exn.Message
                                StatusMessage = Some $"Could not load ARC object explorer: {exn.Message}"
                        })
                }
                |> Promise.start),
        [| box appStateCtx.state; box fileStateCtx.state.FileTree |]
    )

    let ctx =
        React.useMemo (
            (fun _ -> {
                state = state
                setState = setState
                setNodes = fun nodes -> setState (fun currentState -> { currentState with Nodes = nodes })
                setSelectedExplorerItemId =
                    fun selectedExplorerItemId ->
                        setState (fun currentState -> {
                            currentState with
                                SelectedExplorerItemId = selectedExplorerItemId
                        })
                setSelectedKindIndices =
                    fun selectedKindIndices ->
                        setState (fun currentState -> {
                            currentState with
                                SelectedKindIndices = selectedKindIndices
                        })
                setArcFileState =
                    fun arcFileState ->
                        setState (fun currentState -> { currentState with ArcFileState = arcFileState })
                setPreviewState =
                    fun previewState ->
                        setState (fun currentState -> { currentState with PreviewState = previewState })
                setStatusMessage =
                    fun statusMessage ->
                        setState (fun currentState -> {
                            currentState with
                                StatusMessage = statusMessage
                        })
            }),
            [| box state |]
        )

    ArcObjectExplorerCtx.Provider(ctx, children)
