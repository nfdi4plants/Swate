namespace JSONExporter

open Shared
open Shared.OfficeInteropTypes

type JSONExportMode =
| ActiveTable

type Model = {
    JSONExportMode              : JSONExportMode
    JSONExportType              : JSONExportType
    Loading                     : bool
    ShowExportTypeDropdown      : bool
    ActiveWorksheetName         : string
    ExportJsonString            : string
} with
    static member init() = {
        JSONExportMode          = ActiveTable
        JSONExportType          = Assay
        Loading                 = false
        ShowExportTypeDropdown  = false
        ActiveWorksheetName     = ""
        ExportJsonString        = ""
    }

type Msg =
// Style
| UpdateLoading                     of bool
| UpdateShowExportTypeDropdown      of bool
| UpdateJSONExportType              of JSONExportType
| UpdateJSONExportMode              of JSONExportMode
//
| ParseTableOfficeInteropRequest
/// parse active annotation table to building blocks
| ParseTableServerRequest           of worksheetName:string * BuildingBlock []
| ParseTableServerResponse          of string
/// Parse all annotation tables to buildingblocks
| ParseTablesOfficeInteropRequest
| ParseTablesServerRequest          of (string * BuildingBlock []) []
