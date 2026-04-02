namespace Swate.Components.Shared

open Fable.Core

[<StringEnum>]
type ArcExplorerNodeKind =
    | Arc
    | Group
    | Study
    | Assay
    | Workflow
    | Run
    | Table
    | DataMap
    | Note
    | Sample

[<RequireQualifiedAccess>]
module ArcExplorerNodeKind =

    let label =
        function
        | ArcExplorerNodeKind.Arc -> "ARC"
        | ArcExplorerNodeKind.Group -> "Group"
        | ArcExplorerNodeKind.Study -> "Study"
        | ArcExplorerNodeKind.Assay -> "Assay"
        | ArcExplorerNodeKind.Workflow -> "Workflow"
        | ArcExplorerNodeKind.Run -> "Run"
        | ArcExplorerNodeKind.Table -> "Table"
        | ArcExplorerNodeKind.DataMap -> "DataMap"
        | ArcExplorerNodeKind.Note -> "Note"
        | ArcExplorerNodeKind.Sample -> "Sample"

[<RequireQualifiedAccess>]
type ArcExplorerNodePreviewTarget =
    | Default
    | Table of int

type ArcExplorerSampleSummary = {
    Characteristics: string list
    Factors: string list
    DerivesFrom: string list
    SourceTables: string list
    Studies: string list
    Assays: string list
}

type ArcExplorerNodeLink = {
    targetId: string
    name: string
    kind: ArcExplorerNodeKind
    subtitle: string option
    path: string option
}

type ArcExplorerNode = {
    id: string
    name: string
    kind: ArcExplorerNodeKind
    path: string option
    previewTarget: ArcExplorerNodePreviewTarget
    isSelectable: bool
    isReference: bool
    sampleSummary: ArcExplorerSampleSummary option
    relatedSamples: ArcExplorerNodeLink list
    isLfs: bool option
    children: ArcExplorerNode list
} with

    static member create
        (
            id: string,
            name: string,
            kind: ArcExplorerNodeKind,
            ?path: string option,
            ?previewTarget: ArcExplorerNodePreviewTarget,
            ?isSelectable: bool,
            ?isReference: bool,
            ?sampleSummary: ArcExplorerSampleSummary option,
            ?relatedSamples: ArcExplorerNodeLink list,
            ?isLfs: bool option,
            ?children: ArcExplorerNode list
        ) =
        {
            id = id
            name = name
            kind = kind
            path = defaultArg path None
            previewTarget = defaultArg previewTarget ArcExplorerNodePreviewTarget.Default
            isSelectable = defaultArg isSelectable true
            isReference = defaultArg isReference false
            sampleSummary = defaultArg sampleSummary None
            relatedSamples = defaultArg relatedSamples []
            isLfs = defaultArg isLfs None
            children = defaultArg children []
        }
