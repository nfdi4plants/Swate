module Swate.Components.Shared.Cwl.Features.PreviewFeature

open ARCtrl.CWL
open Swate.Components.Shared.Cwl.Adapters.ArCtrlEncode
open Swate.Components.Shared.Cwl.State.Types

let buildPreviewYaml (state: AppState) =
    state.Document
    |> Option.map (fun document -> document |> toProcessingUnit |> Encode.encodeProcessingUnit)
