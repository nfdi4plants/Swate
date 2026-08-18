module Swate.Components.Shared.Cwl.Adapters.ValidationAdapter

open Swate.Components.Shared.Cwl.Validation.ValidationContext
open Swate.Components.Shared.Cwl.Validation.ValidationEngine
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.Adapters.ArCtrlEncode

let validateDocument (mode: ValidationMode) (document: EditorDocument) =
    document
    |> toProcessingUnit
    |> fun processingUnit -> validateProcessingUnit processingUnit mode
