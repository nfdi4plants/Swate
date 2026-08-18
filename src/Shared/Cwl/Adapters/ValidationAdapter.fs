module CWLBuilder.Electron.Renderer.Adapters.ValidationAdapter

open CWLBuilder.Validation.ValidationContext
open CWLBuilder.Validation.ValidationEngine
open CWLBuilder.Electron.Renderer.Documents.Types
open CWLBuilder.Electron.Renderer.Adapters.ArCtrlEncode

let validateDocument (mode: ValidationMode) (document: EditorDocument) =
    document
    |> toProcessingUnit
    |> fun processingUnit -> validateProcessingUnit processingUnit mode
