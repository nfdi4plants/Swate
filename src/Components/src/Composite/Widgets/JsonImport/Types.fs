module Swate.Components.Composite.Widgets.JsonImport.Types

open Swate.Components.Shared

type JsonImportFile = {
    FileName: string option
    Content: string
}

type JsonImportRequest = {
    ImportedFile: ArcFiles
    SourceFileName: string option
    JsonFormat: JsonExportFormat
}
