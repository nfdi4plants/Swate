module Model.ARCitect

open ARCtrl

type Msg =
    | Init
    | Error of exn
    | RequestPaths of selectDirectories: bool
    | AssayToARCitect of ArcAssay
    | StudyToARCitect of ArcStudy
    | InvestigationToARCitect of ArcInvestigation

type IEventHandler = {
    Error: exn -> unit
    AssayToSwate            : {| ArcAssayJsonString: string |} -> unit
    StudyToSwate            : {| ArcStudyJsonString: string |} -> unit
    InvestigationToSwate    : {| ArcInvestigationJsonString: string |} -> unit
    PathsToSwate            : {| paths: string [] |} -> unit
}