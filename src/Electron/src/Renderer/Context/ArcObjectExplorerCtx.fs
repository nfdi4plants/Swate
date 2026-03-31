module Renderer.Context.ArcObjectExplorerCtx

open ARCtrl
open Feliz
open Swate.Components


type ArcObjectExplorerState = {
    Nodes: ArcExplorerNode list
    SelectedExplorerItemId: string option
    SelectedKindIndices: Set<int>
    ArcFileState: ArcFiles option
    PageState: PageState option
    StatusMessage: string option
} with

    static member init() = {
        Nodes = []
        SelectedExplorerItemId = None
        SelectedKindIndices = Swate.Components.ARCObjectWidget.DefaultKindFilterIndices()
        ArcFileState = None
        PageState = None
        StatusMessage = None
    }

type ArcObjectExplorerController = {
    state: ArcObjectExplorerState
    setState: (ArcObjectExplorerState -> ArcObjectExplorerState) -> unit
    setNodes: ArcExplorerNode list -> unit
    setSelectedExplorerItemId: string option -> unit
    setSelectedKindIndices: Set<int> -> unit
    setArcFileState: ArcFiles option -> unit
    setPreviewState: PageState option -> unit
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
                                PageState = Some(PageState.ErrorPage exn.Message)
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
                        setState (fun currentState -> { currentState with PageState = previewState })
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
