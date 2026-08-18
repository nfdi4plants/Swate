module Swate.Components.JsBindings.Dagre

open Fable.Core
open Fable.Core.JsInterop

type GraphOptions = {| multigraph: bool; compound: bool |}

type GraphLayoutOptions = {|
    rankdir: string
    nodesep: int
    ranksep: int
    marginx: int
    marginy: int
|}

type NodeOptions = {| width: float; height: float |}

[<AllowNullLiteral>]
type NodePosition =
    abstract x: float
    abstract y: float

[<AllowNullLiteral>]
type Graph =
    abstract setGraph: GraphLayoutOptions -> Graph
    abstract setDefaultEdgeLabel: (unit -> obj) -> Graph
    abstract setNode: string * NodeOptions -> Graph
    abstract setEdge: string * string -> Graph
    abstract node: string -> NodePosition

type Graphlib =
    abstract Graph: obj

type DagreExports =
    abstract graphlib: Graphlib
    abstract layout: Graph -> unit

[<ImportAll("dagre")>]
let private dagre: DagreExports = jsNative

let createGraph (options: GraphOptions) : Graph =
    createNew dagre.graphlib.Graph (box options) |> unbox

let layout (graph: Graph) : unit = dagre.layout graph
