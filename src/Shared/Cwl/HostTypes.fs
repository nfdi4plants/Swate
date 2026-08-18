/// Host-abstraction data contracts for CWL operations.
module Swate.Components.Shared.Cwl.HostTypes

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
