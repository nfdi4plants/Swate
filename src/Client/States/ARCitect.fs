module Model.ARCitect

open ARCtrl.ISA

type Msg =
    | Init
    | Error of exn
    | AssayToARCitect of ArcAssay
    | StudyToARCitect of ArcStudy
    | TriggerSwateClose

type IEventHandler = {
    Error: exn -> unit
    AssayToSwate: {| ArcAssayJsonString: string |} -> unit
    StudyToSwate: {| ArcStudyJsonString: string |} -> unit
}