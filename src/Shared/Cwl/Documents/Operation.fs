module Swate.Components.Shared.Cwl.Documents.Operation

open Swate.Components.Shared.Cwl.Documents.Types

let setIntent (intent: string list) (model: OperationModel) = { model with Intent = intent }
