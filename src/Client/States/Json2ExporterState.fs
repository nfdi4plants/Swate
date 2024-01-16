module Model.JsonExporter

open Shared
open Shared.OfficeInteropTypes

type Model = {
    /// Use this value to determine on click which export value to use
    CurrentExportType               : JsonExportType option
    //
    TableJsonExportType             : JsonExportType
    WorkbookJsonExportType          : JsonExportType
    XLSXParsingExportType           : JsonExportType
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
        TableJsonExportType             = JsonExportType.Assay
        WorkbookJsonExportType          = JsonExportType.Assay
        XLSXParsingExportType           = JsonExportType.Assay
        Loading                         = false
        ShowTableExportTypeDropdown     = false
        ShowWorkbookExportTypeDropdown  = false
        ShowXLSXExportTypeDropdown      = false

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
| UpdateTableJsonExportType             of JsonExportType
| UpdateWorkbookJsonExportType          of JsonExportType
| UpdateXLSXParsingExportType           of JsonExportType
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