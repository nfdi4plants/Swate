namespace Cytoscape

open Shared
open Shared.OfficeInteropTypes
open JS

type Model = {
    ShowModal: bool
    CyObject: Types.ICytoscape option
} with
    static member init() = {
        ShowModal = true
        CyObject = None
    }

type Msg =
//Client
| UpdateCyObject of Types.ICytoscape option
| UpdateShowModal of bool