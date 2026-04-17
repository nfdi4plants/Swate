namespace Swate.Components.Shared.DTOs


open Fable.Core  
open Swate.Components.Shared

[<StringEnum>]
type ARCDatasets =
    | Assay
    | Study
    | Workflow
    | Run

[<StringEnum>]
type ARCMaterial =
    | Sources
    | Samples

[<StringEnum>]
type ARCData =
    | Files
    | FragmentSelector

[<StringEnum>]
type ProcessType =
    | Material of ARCMaterial
    | Data of ARCData

[<StringEnum>]
type CurrentProcess =
    | Input of ProcessType
    | Output of ProcessType

[<StringEnum>]
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

[<StringEnum>]
type JsonDTOs =
    | ARCtrlJsonObject of ARCtrlJsonObject
    | ARCtrlContract


type JsonDTOUnwrapper =

    static member unwrap(jsonDTO: JsonDTOs) =
        match jsonDTO with
        | JsonDTOs.ARCtrlJsonObject dto -> dto
        | JsonDTOs.ARCtrlContract -> failwith "Not implemented yet"
