module CWLBuilder.Electron.Renderer.State.Effects

open System

type AppEffect =
    | FocusMainWindow of string
    | ShowOpenDialog
    | ShowSaveDialog
    | LoadCwlFile of Guid * string
    | SaveCwlFile of Guid * string * string
