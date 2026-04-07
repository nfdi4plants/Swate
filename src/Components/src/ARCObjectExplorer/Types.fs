namespace Swate.Components

open Fable.Core
open Swate.Components.Shared


//type ARCExplorerServices = {
//    openView: string -> JS.Promise<Result<unit, string>>
//    setStatusMessage: string option -> unit
//    runToggleLfsMark: string -> string -> bool -> JS.Promise<Result<unit, string>>
//}

//[<RequireQualifiedAccess>]
//type PageState =
//    | ArcFilePage of ArcFiles
//    | TextPage of string
//    | UnknownPage
//    | LandingDraftPage
//    | NotesDraftPage
//    | NotesSearchPage
//    | ErrorPage of string

//type ArcObjectExplorerProps = {
//    rootRepoPath: string option
//    nodes: ArcExplorerNode list
//    selection: ArcSelection
//    arcFileState: ArcFiles option
//    previewState: PageState option
//    setArcFileState: ArcFiles option -> unit
//    setSelection: ArcSelection -> unit
//    services: ARCExplorerServices
//}

//type NoteTarget =
//    | Root
//    | Study of string
//    | Assay of string
//    | Workflow of string
//    | Run of string

//type NoteEntry = {
//    Name: string
//    RelativePath: string
//    Path: string
//    Target: NoteTarget
//    IsLfs: bool option
//}
