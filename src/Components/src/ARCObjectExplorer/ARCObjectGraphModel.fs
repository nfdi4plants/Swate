namespace Swate.Components.ARCObjectExplorer


open Swate.Components.Shared.DTOs

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

type Material ={
    id: string
    type': string
    name: string
    additionalProperty: string option
}

type Process ={
    id: string option
    type': string
    additionalType: string option
    name: string
    object: ProcessType
    result: ProcessType
    ExecutesProtocol: string //Id of parent protocol
    parameterValue: string option
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
}

type FormalParameters ={
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

type Dataset ={
    id: string
    type': string
    additionalType: string
    identifier: string
    name: string
    description: string
    about: CurrentProcess
    hasPart: string option //Id of sub dataset or data files
    additionalProperty: string option
    protocols: Protocol list
}

type ARCS = {
    path: string
    Datasets: ARCDatasets list
}
