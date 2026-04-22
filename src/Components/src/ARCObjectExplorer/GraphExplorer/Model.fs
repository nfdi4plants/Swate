module Swate.Components.ARCObjectExplorer.GraphExplorer.Model

open Fable.Core

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
    additionalProperty: PropertyValue []
}

type DataKinds = {
    Files : Data []
    FragmentSelector : Data []
}

type Material = {
    id: string
    type': string
    additionalType: string option
    name: string
    additionalProperty: PropertyValue []
}

type MaterialKinds = {
    Sources : Material []
    Samples : Material []
}

type ProcessType = {
    Materials : MaterialKinds []
    Data : DataKinds []
}

type LabProcess = {
    id: string option
    type': string
    additionalType: string option
    name: string
    inputs: ProcessType [] //objects
    outputs: ProcessType [] //results
    Materials: Material []
    Data: Data []
    executesProtocol: string //Id of parent protocol
    parameterValue: PropertyValue []
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
    parameters: FormalParameter []
    description: string option
    intendedUse: (DefinedTerm * string) option
    processes: LabProcess []
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
    about: LabProtocol []
    hasPart: Dataset [] //Sub dataset or data files
    additionalProperty: PropertyValue []
}

type DatasetKinds = {
    Studies: Dataset []
    Assays: Dataset []
    Workflows: Dataset []
    Runs: Dataset []
}

type ARCGraph = {
    path: string
    Datasets: DatasetKinds
}

type ARCObjects =
    | Arc of ARCGraph []
    | Datasets of Dataset []
    | Protocols of LabProtocol []
    | FormalParameters of FormalParameter []
    | Processes of LabProcess []

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

type GraphNodeMeta = {
    Tag: GraphNodeTag option
    KindLabel: string
    RoleLabel: string
    Description: string option
    Rows: (string * string) list
    CaseExamples: (string * string) list
}
