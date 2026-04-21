module Swate.Components.ARCObjectExplorer.GraphExplorer.Model

open Fable.Core

type Data ={
    id: string option
    type': string
    additionalType: string option
    path: string
    selector: string option
    selectorFormat: string option //URL option
    encodingFormat: string option
    name: string option
    additionalProperty: string option
}

type ARCData =
    | Files of Data
    | FragmentSelector of Data

type Material ={
    id: string
    type': string
    name: string
    additionalProperty: string option
}

type ARCMaterial =
    | Sources of Material
    | Samples of Material

type ProcessType =
    | Material of ARCMaterial
    | Data of ARCData

type Process ={
    id: string option
    type': string
    additionalType: string option
    name: string
    object: ProcessType list
    result: ProcessType list
    ExecutesProtocol: string //Id of parent protocol
    parameterValue: string option
}

type CurrentProcess =
    | Input of Process
    | Output of Process

type FormalParameter ={
    id: string option
    type': string
    additionalType: string option
    name: string
    description: string
    intendedUse: string
    additionalProperty: string option
    version: string option
    url: string option //URL
    processes: CurrentProcess list
}

type Protocol ={
    id: string option
    type': string
    additionalType: string option
    name: string
    description: string
    intendedUse: string
    additionalProperty: string option
    version: string option
    url: string option //URL
    processes: CurrentProcess list
    formalParameters: FormalParameter list
}

[<StringEnum>]
type ARCDatasets =
    | Study
    | Assay
    | Workflow
    | Run

type Dataset ={
    id: string
    type': ARCDatasets
    additionalType: string
    identifier: string
    name: string
    description: string
    hasPart: string option //Id of sub dataset or data files
    additionalProperty: string option
    about: Protocol list
}

type ARCGraph = {
    path: string
    Datasets: Dataset list
}

type ARCObjects =
    | Arc of ARCGraph list
    | Datasets of Dataset list
    | Protocols of Protocol list
    | FormalParameters of FormalParameter list
    | Processes of CurrentProcess list

type GraphProcessEndpointValueType =
    | Material
    | Data

type GraphNodeTag =
    | Dataset
    | Protocol
    | FormalParameter
    | Process
    | ProcessEndpoint of GraphProcessEndpointValueType

type GraphNodeMeta = {
    Tag: GraphNodeTag option
    KindLabel: string
    RoleLabel: string
    Description: string option
    Rows: (string * string) list
    CaseExamples: (string * string) list
}
