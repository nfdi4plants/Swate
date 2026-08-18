module Swate.Components.Shared.Cwl.State.Effects

open System

type AppEffect =
    | FocusMainWindow of string
    | ShowOpenDialog
    | ShowSaveDialog
    | LoadCwlFile of Guid * string
    | SaveCwlFile of Guid * string * string
