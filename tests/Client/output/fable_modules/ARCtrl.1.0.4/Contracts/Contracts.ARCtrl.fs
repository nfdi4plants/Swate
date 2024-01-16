module ARCtrl.Contract.ARC

open ARCtrl.Path

open ARCtrl.Contract

// Assumptions: 
// 1. All ISA .xlsx files MUST have the same names, either "isa.investigation.xlsx", "isa.assay.xlsx" or "isa.study.xlsx"
// 2. All ISA .xlsx files MUST be placed at the correct place. 
//      - isa.investigation.xlsx: MUST be in root
//      - isa.assay.xlsx: MUST be in root/assays/ANY_ASSAY_NAME/
//      - isa.study.xlsx: MUST be in root/studies/ANY_STUDY_NAME/
// 3. We ignore all other .xlsx files.
/// Tries to create READ contract with DTOType = ISA_Assay, ISA_Study or ISA_Investigation from given path relative to ARC root.
let tryISAReadContractFromPath (path: string) = 
    let split = ARCtrl.Path.split path
    match split with
    | InvestigationPath p -> 
        Some <| Contract.createRead(p, DTOType.ISA_Investigation) 
    | AssayPath p ->
        Some <| Contract.createRead(p, DTOType.ISA_Assay) 
    | StudyPath p ->
        Some <| Contract.createRead(p, DTOType.ISA_Study) 
    | anyElse -> 
        None

