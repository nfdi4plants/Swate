module Renderer.Context.ArcObjectExplorerContext

open Feliz
open Swate.Components
open Swate.Components.ARCObjectExplorer
open Swate.Components.Shared

type ArcObjectExplorerState = {
    Nodes: ArcExplorerNode list
    SelectedKindIndices: Set<int>
    ArcFileState: ArcFiles option
    PageState: PageState option
    PendingArcFileSave: ArcFiles option
    StatusMessage: string option
} with

    static member init() = {
        Nodes = []
        SelectedKindIndices =
            KindFilter.defaultSelectedIndices(KindFilter.arcObjectExplorerOptions)
        ArcFileState = None
        PageState = None
        PendingArcFileSave = None
        StatusMessage = None
    }

type ArcObjectExplorerController = {
    state: ArcObjectExplorerState
    setState: (ArcObjectExplorerState -> ArcObjectExplorerState) -> unit
    setNodes: ArcExplorerNode list -> unit
    setSelectedKindIndices: Set<int> -> unit
    setArcFileState: ArcFiles option -> unit
    setPreviewState: PageState option -> unit
    setPendingArcFileSave: ArcFiles option -> unit
    setStatusMessage: string option -> unit
}

let ArcObjectExplorerCtx =
    React.createContext<ArcObjectExplorerController> (
        {
            state = ArcObjectExplorerState.init ()
            setState = ignore
            setNodes = ignore
            setSelectedKindIndices = ignore
            setArcFileState = ignore
            setPreviewState = ignore
            setPendingArcFileSave = ignore
            setStatusMessage = ignore
        }
    )

[<Hook>]
let useArcObjectExplorerCtx () = React.useContext ArcObjectExplorerCtx

module private Helper =

    let rec containsNodeId (nodeId: string) (nodes: ArcExplorerNode list) =
        nodes
        |> List.exists (fun node -> node.id = nodeId || containsNodeId nodeId node.children)

open Helper

[<ReactComponent>]
let ArcObjectExplorerCtxProvider (children: ReactElement) =
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
    let state, setState = React.useStateWithUpdater (ArcObjectExplorerState.init ())

    React.useEffect (
        (fun () ->
            let mutable isCurrent = true

            match appStateCtx with
            | None -> setState (fun _ -> ArcObjectExplorerState.init ())
            | Some _ ->
                promise {
                    let! result = Api.ipcArcVaultApi.getArcObjectTree ()

                    if isCurrent then
                        match result with
                        | Ok nodes ->
                            fileStateCtx.updateSelection (fun currentSelection ->
                                let nextSelection =
                                    match currentSelection.ExplorerNodeId with
                                    | Some nodeId when containsNodeId nodeId nodes -> currentSelection
                                    | Some _ -> ArcSelection.clearExplorerNode currentSelection
                                    | None -> currentSelection

                                nextSelection)

                            setState (fun currentState ->
                                {
                                    currentState with
                                        Nodes = nodes
                                        StatusMessage = None
                                })
                        | Error exn ->
                            fileStateCtx.updateSelection (fun currentSelection ->
                                let nextSelection =
                                    ArcSelection.clearExplorerNode currentSelection

                                nextSelection)

                            setState (fun currentState -> {
                                currentState with
                                    Nodes = []
                                    ArcFileState = None
                                    PageState = Some(PageState.ErrorPage exn.Message)
                                    PendingArcFileSave = None
                                    StatusMessage = Some $"Could not load ARC object explorer: {exn.Message}"
                            })
                }
                |> Promise.start

            Some
                { new System.IDisposable with
                    member _.Dispose() = isCurrent <- false
                }),
        [| box appStateCtx; box fileStateCtx.state.FileTree |]
    )

    let ctx =
        React.useMemo (
            (fun _ -> {
                state = state
                setState = setState
                setNodes = fun nodes -> setState (fun currentState -> { currentState with Nodes = nodes })
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
                setPendingArcFileSave =
                    fun pendingArcFileSave ->
                        setState (fun currentState -> {
                            currentState with
                                PendingArcFileSave = pendingArcFileSave
                        })
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

