module ARCitISA.Components.Composite.Widgets.JsonImport.Types

open ARCitISA.Components.Shared

type JsonImportFile = {
    FileName: string option
    Content: string
}

type JsonImportRequest = {
    ImportedFile: ArcFiles
    SourceFileName: string option
    JsonFormat: JsonExportFormat
}
