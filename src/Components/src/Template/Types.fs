module Swate.Components.Template.Types

open ARCtrl

[<RequireQualifiedAccess>]
type TemplateLoadState =
    | Loading
    | Loaded of Template[]
    | LoadError of string

[<RequireQualifiedAccess>]
type TemplateImportAction =
    | ImportAsNewTable
    | AppendToActiveTable
    | NoImport

type TemplateCacheState = {
    SchemaVersion: int
    LastFetchedUtcTicks: int64 option
    TemplatesJson: string option
} with

    static member Empty = {
        SchemaVersion = 1
        LastFetchedUtcTicks = None
        TemplatesJson = None
    }

type TemplatePreviewCallbacks = {
    GetTemplateImportAction: System.Guid -> TemplateImportAction
    SetTemplateImportAction: System.Guid -> TemplateImportAction -> unit
    IsColumnSelected: System.Guid -> int -> bool
    ToggleColumnSelection: System.Guid -> int -> unit
    SelectAllTemplateColumns: System.Guid -> unit
    UnselectAllTemplateColumns: Template -> unit
}

type ImportTable = { Index: int; FullImport: bool }

type SelectiveImportConfig = {
    ImportType: ARCtrl.TableJoinOptions
    ImportMetadata: bool
    ImportTables: ImportTable list
    DeselectedColumns: Set<int * int>
    TemplateName: string option
} with

    static member init() = {
        ImportType = ARCtrl.TableJoinOptions.Headers
        ImportMetadata = false
        ImportTables = []
        DeselectedColumns = Set.empty
        TemplateName = None
    }