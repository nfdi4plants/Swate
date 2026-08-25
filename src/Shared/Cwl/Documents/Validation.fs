module Swate.Components.Shared.Cwl.Documents.Validation

open Swate.Components.Shared.Cwl.Validation.ValidationContext
open Swate.Components.Shared.Cwl.Adapters.ValidationAdapter
open Swate.Components.Shared.Cwl.Documents.Types

let validateForLiveMode (document: EditorDocument) = validateDocument Live document
