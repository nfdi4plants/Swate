/// Host-abstraction data contracts for CWL operations.
module Swate.Components.Shared.Cwl.HostTypes

open Fable.Core

/// Response after loading a CWL file.
type LoadCwlResponse = {
    Success: bool
    Yaml: string option
    ResolvedYaml: string option
    FilePath: string
    Error: string option
}

/// Result of a save operation.
type SaveCwlResponse = {
    Success: bool
    FilePath: string
    Error: string option
}

/// Result of a file dialog (open or save).
type DialogResult = {
    Canceled: bool
    FilePath: string option
}

/// Host operations consumed by the shared CWL effect runner.
type CwlHostApi = {
    ShowOpenDialog: unit -> JS.Promise<DialogResult>
    ShowSaveDialog: unit -> JS.Promise<DialogResult>
    LoadCwlFile: string -> JS.Promise<LoadCwlResponse>
    SaveCwlFile: string -> string -> JS.Promise<SaveCwlResponse>
}
