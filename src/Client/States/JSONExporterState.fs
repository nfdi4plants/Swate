namespace JSONExporter

open Shared.OfficeInteropTypes

type JSONExportMode =
| ActiveTable

type Model = {
    JSONExportMode : JSONExportMode
    Loading: bool
    Default: obj
} with
    static member init() = {
        JSONExportMode = ActiveTable
        Loading = false
        Default = ""
    }

type Msg =
// Style
| UpdateLoading                     of bool
//
| ParseTableOfficeInteropRequest
| ParseTableServerRequest           of BuildingBlock []
| ParseTableServerResponse          of Result<string,exn>