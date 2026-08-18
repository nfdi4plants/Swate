module Swate.Components.JsBindings.XyFlow

open System
open Fable.Core
open Feliz

type HandleProps = {|
    ``type``: string
    position: string
    id: string
    style: obj option
|}

[<AllowNullLiteral>]
type NodeData =
    abstract label: string
    abstract inputPorts: string array
    abstract outputPorts: string array

[<AllowNullLiteral>]
type Position =
    abstract x: float
    abstract y: float

[<AllowNullLiteral>]
type Node =
    abstract id: string
    abstract position: Position
    abstract data: NodeData

[<AllowNullLiteral>]
type Connection =
    abstract source: string
    abstract sourceHandle: string option
    abstract target: string
    abstract targetHandle: string option

[<AllowNullLiteral>]
type Edge =
    abstract id: string

[<AllowNullLiteral>]
type EdgeContextMenuEvent =
    abstract clientX: float
    abstract clientY: float
    abstract preventDefault: unit -> unit

[<AllowNullLiteral>]
type NodeProps =
    abstract data: NodeData

type FitViewOptions = {| padding: float |}

type ReactFlowProps = {|
    key: string
    id: string
    nodes: obj array
    edges: obj array
    nodeTypes: obj
    fitView: bool
    fitViewOptions: FitViewOptions
    minZoom: float
    maxZoom: float
    nodesDraggable: bool
    nodesConnectable: bool
    elementsSelectable: bool
    onNodeClick: Func<obj, Node, unit>
    onNodeDragStop: Func<obj, Node, unit>
    onConnect: Func<Connection, unit>
    onEdgesDelete: Func<Edge array, unit>
    onEdgeContextMenu: Func<EdgeContextMenuEvent, Edge, unit>
    onPaneClick: Func<obj, unit>
    deleteKeyCode: string
|}

[<Erase>]
type XyFlow =

    [<ReactComponent("ReactFlow", "@xyflow/react")>]
    static member ReactFlow
        (
            key: string,
            id: string,
            nodes: obj array,
            edges: obj array,
            nodeTypes: obj,
            fitView: bool,
            fitViewOptions: FitViewOptions,
            minZoom: float,
            maxZoom: float,
            nodesDraggable: bool,
            nodesConnectable: bool,
            elementsSelectable: bool,
            onNodeClick: Func<obj, Node, unit>,
            onNodeDragStop: Func<obj, Node, unit>,
            onConnect: Func<Connection, unit>,
            onEdgesDelete: Func<Edge array, unit>,
            onEdgeContextMenu: Func<EdgeContextMenuEvent, Edge, unit>,
            onPaneClick: Func<obj, unit>,
            deleteKeyCode: string,
            children: ReactElement
        ) : ReactElement =
        React.Imported()

    [<ReactComponent("Handle", "@xyflow/react")>]
    static member Handle(``type``: string, position: string, id: string, ?style: obj) : ReactElement = React.Imported()

    [<ReactComponent("MiniMap", "@xyflow/react")>]
    static member MiniMap() : ReactElement = React.Imported()

    [<ReactComponent("Controls", "@xyflow/react")>]
    static member Controls() : ReactElement = React.Imported()

    [<ReactComponent("Background", "@xyflow/react")>]
    static member Background() : ReactElement = React.Imported()
