module Swate.Components.Page.ARCObjectExplorer.GraphExplorer.Model

open Fable.Core
open Swate.Components.Shared

type PropertyValue = {
    id: string
    type': string
    additionalType: string option
    name: string
    value: string option
    unit: string option //URL option
    nameTAN: string option
    valueTAN: string option
    unitTAN: string option
}

type Data = {
    id: string option
    type': string
    additionalType: string option
    path: string
    selector: string option
    selectorFormat: string option //URL option
    encodingFormat: string option
    additionalProperty: PropertyValue[]
}

type DataKinds = {
    Files: Data[]
    FragmentSelector: Data[]
}

type Material = {
    id: string
    type': string
    additionalType: string option
    name: string
    additionalProperty: PropertyValue[]
}

type MaterialKinds = {
    Sources: Material[]
    Samples: Material[]
}

type ProcessType = {
    Materials: MaterialKinds[]
    Data: DataKinds[]
}

type LabProcess = {
    id: string option
    type': string
    additionalType: string option
    name: string
    inputs: ProcessType[] //objects
    outputs: ProcessType[] //results
    Materials: Material[]
    Data: Data[]
    executesProtocol: string //Id of parent protocol
    parameterValue: PropertyValue[]
}

type FormalParameter = {
    id: string
    type': string
    name: string option
    nameTAN: string option
    defaultValue: string option
}

type DefinedTerm = {
    id: string
    type': string
    name: string
    termCode: string option
    inDefinedTermSet: string option //URL option
    additionalProperty: PropertyValue option
}

type LabProtocol = {
    id: string option
    type': string
    additionalType: string option
    name: string option
    parameters: FormalParameter[]
    description: string option
    intendedUse: (DefinedTerm * string) option
    processes: LabProcess[]
    additionalProperty: PropertyValue option
    version: string option
    url: string option //URL
}

[<StringEnum>]
type ARCDatasets =
    | [<CompiledName("Studies")>] Study
    | [<CompiledName("Assays")>] Assay
    | [<CompiledName("Workflows")>] Workflow
    | [<CompiledName("Runs")>] Run

type Dataset = {
    id: string
    type': ARCDatasets
    additionalType: string
    identifier: string
    name: string option
    description: string option
    about: LabProtocol[]
    hasPart: Dataset[] //Sub dataset or data files
    additionalProperty: PropertyValue[]
}

type DatasetKinds = {
    Studies: Dataset[]
    Assays: Dataset[]
    Workflows: Dataset[]
    Runs: Dataset[]
}

type ARCGraph = { path: string; Datasets: DatasetKinds }

type ARCObjects =
    | Arc of ARCGraph[]
    | Datasets of Dataset[]
    | Protocols of LabProtocol[]
    | FormalParameters of FormalParameter[]
    | Processes of LabProcess[]

let sanitizeSegment (value: string) =
    value.Trim().ToLowerInvariant().Replace(" ", "-").Replace("/", "-").Replace("\\", "-").Replace(":", "-")

type GraphProcessEndpointValueType =
    | Material
    | Data

type GraphPropertyValueOwnerTag =
    | Dataset
    | Protocol
    | Process
    | ProcessEndpoint of GraphProcessEndpointValueType

type GraphNodeTag =
    | Dataset
    | Protocol
    | FormalParameter
    | Process
    | ProcessEndpoint of GraphProcessEndpointValueType
    | PropertyValue of GraphPropertyValueOwnerTag

[<StringEnum; RequireQualifiedAccess>]
type GraphSemanticKind =
    | [<CompiledName("Datasets")>] Datasets
    | [<CompiledName("Study")>] Study
    | [<CompiledName("Assay")>] Assay
    | [<CompiledName("Workflow")>] Workflow
    | [<CompiledName("Run")>] Run
    | [<CompiledName("Protocols")>] Protocols
    | [<CompiledName("FormalParameters")>] FormalParameters
    | [<CompiledName("Processes")>] Processes
    | [<CompiledName("Materials")>] Materials
    | [<CompiledName("Data")>] Data

[<RequireQualifiedAccess>]
module GraphSemanticKind =

    let label =
        function
        | GraphSemanticKind.Datasets -> "Datasets"
        | GraphSemanticKind.Study -> "Study"
        | GraphSemanticKind.Assay -> "Assay"
        | GraphSemanticKind.Workflow -> "Workflow"
        | GraphSemanticKind.Run -> "Run"
        | GraphSemanticKind.Protocols -> "Protocols"
        | GraphSemanticKind.FormalParameters -> "FormalParameters"
        | GraphSemanticKind.Processes -> "Processes"
        | GraphSemanticKind.Materials -> "Materials"
        | GraphSemanticKind.Data -> "Data"

    let tryParseLabel =
        function
        | "Datasets" -> Some GraphSemanticKind.Datasets
        | "Study" -> Some GraphSemanticKind.Study
        | "Assay" -> Some GraphSemanticKind.Assay
        | "Workflow" -> Some GraphSemanticKind.Workflow
        | "Run" -> Some GraphSemanticKind.Run
        | "Protocols" -> Some GraphSemanticKind.Protocols
        | "FormalParameters" -> Some GraphSemanticKind.FormalParameters
        | "Processes" -> Some GraphSemanticKind.Processes
        | "Materials" -> Some GraphSemanticKind.Materials
        | "Data" -> Some GraphSemanticKind.Data
        | _ -> None

    let allInFilterOrder = [
        GraphSemanticKind.Datasets
        GraphSemanticKind.Study
        GraphSemanticKind.Assay
        GraphSemanticKind.Workflow
        GraphSemanticKind.Run
        GraphSemanticKind.Protocols
        GraphSemanticKind.FormalParameters
        GraphSemanticKind.Processes
        GraphSemanticKind.Materials
        GraphSemanticKind.Data
    ]

    let datasetParent = GraphSemanticKind.Datasets

    let datasetChildren = [
        GraphSemanticKind.Study
        GraphSemanticKind.Assay
        GraphSemanticKind.Workflow
        GraphSemanticKind.Run
    ]

    let branchPrunableKinds =
        Set.ofList [
            yield! datasetChildren
            GraphSemanticKind.Protocols
            GraphSemanticKind.FormalParameters
            GraphSemanticKind.Processes
            GraphSemanticKind.Materials
            GraphSemanticKind.Data
        ]

[<RequireQualifiedAccess>]
type GraphExplorerNodeKind =
    | Arc
    | Group
    | Study
    | Assay
    | Workflow
    | Run
    | Protocol
    | Process
    | FormalParameter
    | Material
    | Data
    | PropertyValue

[<RequireQualifiedAccess>]
module GraphExplorerNodeKind =

    let label =
        function
        | GraphExplorerNodeKind.Arc -> "ARC"
        | GraphExplorerNodeKind.Group -> "Group"
        | GraphExplorerNodeKind.Study -> "Study"
        | GraphExplorerNodeKind.Assay -> "Assay"
        | GraphExplorerNodeKind.Workflow -> "Workflow"
        | GraphExplorerNodeKind.Run -> "Run"
        | GraphExplorerNodeKind.Protocol -> "Protocol"
        | GraphExplorerNodeKind.Process -> "Process"
        | GraphExplorerNodeKind.FormalParameter -> "FormalParameter"
        | GraphExplorerNodeKind.Material -> "Material"
        | GraphExplorerNodeKind.Data -> "Data"
        | GraphExplorerNodeKind.PropertyValue -> "PropertyValue"

    let toArcExplorerNodeKind =
        function
        | GraphExplorerNodeKind.Arc -> ArcExplorerNodeKind.Arc
        | GraphExplorerNodeKind.Group -> ArcExplorerNodeKind.Group
        | GraphExplorerNodeKind.Study -> ArcExplorerNodeKind.Study
        | GraphExplorerNodeKind.Assay -> ArcExplorerNodeKind.Assay
        | GraphExplorerNodeKind.Workflow -> ArcExplorerNodeKind.Workflow
        | GraphExplorerNodeKind.Run -> ArcExplorerNodeKind.Run
        | GraphExplorerNodeKind.Protocol -> ArcExplorerNodeKind.Workflow
        | GraphExplorerNodeKind.Process -> ArcExplorerNodeKind.Run
        | GraphExplorerNodeKind.FormalParameter -> ArcExplorerNodeKind.Table
        | GraphExplorerNodeKind.Material -> ArcExplorerNodeKind.Sample
        | GraphExplorerNodeKind.Data -> ArcExplorerNodeKind.DataMap
        | GraphExplorerNodeKind.PropertyValue -> ArcExplorerNodeKind.Table

    let ofArcExplorerNodeKind =
        function
        | ArcExplorerNodeKind.Arc -> GraphExplorerNodeKind.Arc
        | ArcExplorerNodeKind.Group -> GraphExplorerNodeKind.Group
        | ArcExplorerNodeKind.Study -> GraphExplorerNodeKind.Study
        | ArcExplorerNodeKind.Assay -> GraphExplorerNodeKind.Assay
        | ArcExplorerNodeKind.Workflow -> GraphExplorerNodeKind.Workflow
        | ArcExplorerNodeKind.Run -> GraphExplorerNodeKind.Run
        | ArcExplorerNodeKind.Table -> GraphExplorerNodeKind.FormalParameter
        | ArcExplorerNodeKind.DataMap -> GraphExplorerNodeKind.Data
        | ArcExplorerNodeKind.Note -> GraphExplorerNodeKind.PropertyValue
        | ArcExplorerNodeKind.Sample -> GraphExplorerNodeKind.Material

type GraphNodeMeta = {
    Tag: GraphNodeTag option
    GraphKind: GraphExplorerNodeKind
    KindLabel: string
    RoleLabel: string
    Description: string option
    Rows: (string * string) list
    CaseExamples: (string * string) list
}
