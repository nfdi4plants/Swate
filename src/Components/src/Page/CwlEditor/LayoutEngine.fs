module Swate.Components.Page.CwlEditor.LayoutEngine

open Fable.Core.JsInterop
open Swate.Components.JsBindings
open Swate.Components.Shared.Cwl.WorkflowLayout

let private toRankDirection direction =
    match direction with
    | LeftToRight -> "LR"
    | TopToBottom -> "TB"

type DagreLayoutEngine() =
    interface IWorkflowLayoutEngine with
        member _.Name = "dagre"

        member _.Layout (nodes: seq<LayoutNode>) (edges: seq<LayoutEdge>) (direction: LayoutDirection) =
            let graph =
                Dagre.createGraph {|
                    multigraph = false
                    compound = false
                |}

            graph.setGraph {|
                rankdir = toRankDirection direction
                nodesep = 60
                ranksep = 110
                marginx = 24
                marginy = 24
            |}
            |> ignore

            graph.setDefaultEdgeLabel (fun () -> createObj []) |> ignore

            for node in nodes do
                graph.setNode (
                    node.Id,
                    {|
                        width = node.Width
                        height = node.Height
                    |}
                )
                |> ignore

            for edge in edges do
                graph.setEdge (edge.SourceId, edge.TargetId) |> ignore

            Dagre.layout graph

            nodes
            |> Seq.choose (fun node ->
                let nodeObj = graph.node (node.Id)

                if isNullOrUndefined nodeObj then
                    None
                else
                    let x = nodeObj.x - (node.Width / 2.0)
                    let y = nodeObj.y - (node.Height / 2.0)
                    Some(node.Id, (x, y))
            )
            |> Map.ofSeq

let dagreLayoutEngine: IWorkflowLayoutEngine =
    DagreLayoutEngine() :> IWorkflowLayoutEngine
