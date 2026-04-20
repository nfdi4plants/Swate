namespace Swate.Components.Shared.DTOs

 
open Swate.Components.Shared


type ARCDatasets =
    | Assay
    | Study
    | Workflow
    | Run

type ARCMaterial =
    | Sources
    | Samples

type ARCData =
    | Files
    | FragmentSelector

type ProcessType =
    | Material of ARCMaterial
    | Data of ARCData

type CurrentProcess =
    | Input of ProcessType
    | Output of ProcessType

type ARCObjects =
    | Arc
    | Datasets of ARCDatasets
    | Protocols
    | FormalParameters
    | Processes of CurrentProcess

type ARCtrlJsonObject = {
    Json: string
    JsonObject: ARCObjects
    JsonType: JsonExportFormat
} with

    static member init(json, jsonObj, jsonType) = {
        Json = json
        JsonObject = jsonObj
        JsonType = jsonType
    }

type JsonDTOs =
    | ARCtrlJsonObject of ARCtrlJsonObject
    | ARCtrlContract


type JsonDTOUnwrapper =

    static member unwrap(jsonDTO: JsonDTOs) =
        match jsonDTO with
        | JsonDTOs.ARCtrlJsonObject dto -> dto
        | JsonDTOs.ARCtrlContract -> failwith "Not implemented yet"
