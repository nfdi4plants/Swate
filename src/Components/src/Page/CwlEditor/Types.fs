module Swate.Components.Page.CwlEditor.Types

open Swate.Components.Shared.Cwl.HostTypes

/// Host-provided services. All file access is injected; the component itself never touches Electron/fs.
type CwlEditorHost = {
    /// Show an open-file picker. None => user cancelled. (Standalone/story hosts only; Electron host passes None-returning stub because files arrive via initialFile.)
    pickOpenFile: (unit -> Fable.Core.JS.Promise<DialogResult>) option
    /// Load a CWL file's content (raw + optionally run-resolved YAML).
    loadCwlFile: string -> Fable.Core.JS.Promise<LoadCwlResponse>
    /// Show a save-path picker. None member => save-as UI is hidden and saves go to the current FilePath.
    pickSavePath: (unit -> Fable.Core.JS.Promise<DialogResult>) option
    /// Write YAML to a path.
    saveCwlFile: string -> string -> Fable.Core.JS.Promise<SaveCwlResponse>
}
