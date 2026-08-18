module Swate.Components.JsBindings.XyFlow

open System
open Fable.Core

type HandleProps = {|
    ``type``: string
    position: string
    id: string
    style: obj option
|}

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
    onNodeClick: Func<obj, obj, unit>
    onNodeDragStop: Func<obj, obj, unit>
    onConnect: Func<obj, unit>
    onEdgesDelete: Func<obj, unit>
    onEdgeContextMenu: Func<obj, obj, unit>
    onPaneClick: Func<obj, unit>
    deleteKeyCode: string
|}

[<ImportMember("@xyflow/react")>]
let ReactFlow: obj = jsNative

[<ImportMember("@xyflow/react")>]
let Handle: obj = jsNative

[<ImportMember("@xyflow/react")>]
let MiniMap: obj = jsNative

[<ImportMember("@xyflow/react")>]
let Controls: obj = jsNative

[<ImportMember("@xyflow/react")>]
let Background: obj = jsNative
