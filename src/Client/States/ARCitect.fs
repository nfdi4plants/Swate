module Model.ARCitect

open ARCtrl.ISA

type Msg =
    | Init
    | Error of exn
    | AssayToARCitect of ArcAssay
    | StudyToARCitect of ArcStudy
    | InvestigationToARCitect of ArcInvestigation
    | TriggerSwateClose

type IEventHandler = {
    Error: exn -> unit
    AssayToSwate            : {| ArcAssayJsonString: string |} -> unit
    StudyToSwate            : {| ArcStudyJsonString: string |} -> unit
    InvestigationToSwate    : {| ArcInvestigationJsonString: string |} -> unit
}