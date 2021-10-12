namespace JSONExporter

open Shared
open Shared.OfficeInteropTypes

type Model = {
    TableJSONExportType             : JSONExportType
    WorkbookJSONExportType          : JSONExportType
    Loading                         : bool
    ShowTableExportTypeDropdown     : bool
    ShowWorkbookExportTypeDropdown  : bool
    CurrentExportType               : JSONExportType option
} with
    static member init() = {
        TableJSONExportType             = Assay
        WorkbookJSONExportType          = Assay
        Loading                         = false
        ShowTableExportTypeDropdown     = false
        ShowWorkbookExportTypeDropdown  = false
        CurrentExportType               = None
    }

type Msg =
// Style
| UpdateLoading                         of bool
| UpdateShowTableExportTypeDropdown     of bool
| UpdateShowWorkbookExportTypeDropdown  of bool
| UpdateTableJSONExportType             of JSONExportType
| UpdateWorkbookJSONExportType          of JSONExportType
//
| ParseTableOfficeInteropRequest
/// parse active annotation table to building blocks
| ParseTableServerRequest           of worksheetName:string * BuildingBlock []
| ParseTableServerResponse          of string
/// Parse all annotation tables to buildingblocks
| ParseTablesOfficeInteropRequest
| ParseTablesServerRequest          of (string * BuildingBlock []) []
