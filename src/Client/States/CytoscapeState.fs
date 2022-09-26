namespace Cytoscape

open Shared
open Shared.OfficeInteropTypes
open JS

type Model = {
    TargetAccession: string
    ShowModal: bool
    //CyObject: Types.ICytoscape option
    CyTermTree: TreeTypes.Tree option
} with
    static member init(?accession: string) = {
        TargetAccession = if accession.IsSome then accession.Value else ""
        ShowModal = false
        //CyObject = None
        CyTermTree = None
    }

type Msg =
// Client
//| UpdateCyObject of Types.ICytoscape option
| UpdateShowModal of bool
// Server Interop
| GetTermTree of accession:string
| GetTermTreeResponse of tree:TreeTypes.Tree