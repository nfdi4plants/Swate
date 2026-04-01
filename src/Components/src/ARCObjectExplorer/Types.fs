namespace Swate.Components

open Fable.Core
open ARCtrl

type ARCExplorerServices = {
    openPreview: string -> JS.Promise<Result<unit, string>>
    setStatusMessage: string option -> unit
    runToggleLfsMark: string -> string -> bool -> JS.Promise<Result<unit, string>>
}

[<RequireQualifiedAccess>]
type PageState =
    | ArcFilePage of ArcFiles
    | TextPage of string
    | UnknownPage
    | LandingDraftPage
    | NotesDraftPage
    | NotesSearchPage
    | ErrorPage of string


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

type ArcObjectExplorerProps = {
    rootRepoPath: string option
    nodes: ArcExplorerNode list
    selectedExplorerItemId: string option
    selectedTreeItemPath: string option
    arcFileState: ArcFiles option
    previewState: PageState option
    setArcFileState: ArcFiles option -> unit
    setSelectedExplorerItemId: string option -> unit
    setSelectedTreeItemPath: string option -> unit
    services: ARCExplorerServices
}

type NoteTarget =
    | Root
    | Study of string
    | Assay of string
    | Workflow of string
    | Run of string

type NoteEntry = {
    Name: string
    RelativePath: string
    Path: string
    Target: NoteTarget
    IsLfs: bool option
}
