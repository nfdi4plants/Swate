module Swate.Components.Shared.Cwl.WorkflowLayout

type LayoutNode = {
    Id: string
    Width: float
    Height: float
}

type LayoutEdge = { SourceId: string; TargetId: string }

type LayoutDirection =
    | LeftToRight
    | TopToBottom

type LayoutResult = Map<string, float * float>

type IWorkflowLayoutEngine =
    abstract member Name: string

    abstract member Layout:
        nodes: seq<LayoutNode> -> edges: seq<LayoutEdge> -> direction: LayoutDirection -> LayoutResult
