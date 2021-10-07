namespace JSONExporter

open Shared.OfficeInteropTypes

type JSONExportMode =
| ActiveTable

type Model = {
    JSONExportMode : JSONExportMode
    Default: obj
} with
    static member init() = {
        JSONExportMode = ActiveTable
        Default = ""
    }

type Msg =
| ParseTableOfficeInteropRequest
| ParseTableServerRequest           of BuildingBlock []
| ParseTableServerResponse          of Result<string,exn>