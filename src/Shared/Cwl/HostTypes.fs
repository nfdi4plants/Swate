/// IPC channel definitions and shared type contracts between Main and Renderer.
/// All IPC communication goes through typed APIs; raw channel strings are
/// defined here as the single source of truth.
module Swate.Components.Shared.Cwl.HostTypes

/// Channel names for Electron IPC communication.
module Channels =
    /// Request to load a CWL file from disk (renderer -> main).
    [<Literal>]
    let LoadCwlFile = "cwl:load-file"

    /// Request to save a CWL document to disk (renderer -> main).
    [<Literal>]
    let SaveCwlFile = "cwl:save-file"

    /// Request to show file-open dialog (renderer -> main).
    [<Literal>]
    let ShowOpenDialog = "dialog:open"

    /// Request to show file-save dialog (renderer -> main).
    [<Literal>]
    let ShowSaveDialog = "dialog:save"

    /// Request main window focus (renderer -> main).
    [<Literal>]
    let FocusMainWindow = "window:focus"

/// DTOs sent over IPC channels. These are plain records that Fable can
/// serialize/deserialize to JSON for Electron IPC.
module DTOs =

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
