namespace Dag

open Shared.OfficeInteropTypes

type HtmlString = string

type Model = {
    Loading : bool
    DagHtml : HtmlString option
} with
    static member init() = {
        Loading = false
        DagHtml = None
    }

type Msg =
//Client
| UpdateLoading of bool
//
| ParseTablesOfficeInteropRequest
| ParseTablesDagServerRequest of (string * BuildingBlock []) []
| ParseTablesDagServerResponse of dagHtml:string