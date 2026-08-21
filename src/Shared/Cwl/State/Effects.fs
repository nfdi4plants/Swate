module Swate.Components.Shared.Cwl.State.Effects

open System
open Swate.Components.Shared.Cwl.Documents.Common

type AppEffect =
    | FocusMainWindow of string
    | ShowOpenDialog of Guid
    | ShowSaveDialog of Guid * Revision
    | LoadCwlFile of Guid * string
    | SaveCwlFile of Guid * Revision * string * string
