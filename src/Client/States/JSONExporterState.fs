namespace JSONExporter

open Shared
open Shared.OfficeInteropTypes

type Model = {
    /// Use this value to determine on click which export value to use
    CurrentExportType               : JSONExportType option
    //
    TableJSONExportType             : JSONExportType
    WorkbookJSONExportType          : JSONExportType
    XLSXParsingExportType           : JSONExportType
    Loading                         : bool
    ShowTableExportTypeDropdown     : bool
    ShowWorkbookExportTypeDropdown  : bool
    ShowXLSXExportTypeDropdown      : bool
    // XLSX upload with json parsing
    XLSXByteArray                   : byte []
} with
    static member init() = {

        CurrentExportType               = None
        //
        TableJSONExportType             = JSONExportType.Assay
        WorkbookJSONExportType          = JSONExportType.Assay
        XLSXParsingExportType           = JSONExportType.Assay
        Loading                         = false
        ShowTableExportTypeDropdown     = false
        ShowWorkbookExportTypeDropdown  = false
        ShowXLSXExportTypeDropdown     = false

        // XLSX upload with json parsing
        XLSXByteArray                   = Array.empty
    }

type Msg =
// Style
| UpdateLoading                         of bool
| UpdateShowTableExportTypeDropdown     of bool
| UpdateShowWorkbookExportTypeDropdown  of bool
| UpdateShowXLSXExportTypeDropdown      of bool
| CloseAllDropdowns
| UpdateTableJSONExportType             of JSONExportType
| UpdateWorkbookJSONExportType          of JSONExportType
| UpdateXLSXParsingExportType           of JSONExportType
//
| ParseTableOfficeInteropRequest
/// parse active annotation table to building blocks
| ParseTableServerRequest           of worksheetName:string * BuildingBlock []
| ParseTableServerResponse          of string
/// Parse all annotation tables to buildingblocks
| ParseTablesOfficeInteropRequest
| ParseTablesServerRequest          of (string * BuildingBlock []) []
// XLSX upload with json parsing
| StoreXLSXByteArray                of byte []
| ParseXLSXToJsonRequest            of byte []
| ParseXLSXToJsonResponse           of string