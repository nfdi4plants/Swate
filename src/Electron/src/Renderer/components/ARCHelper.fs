module Renderer.Components.ARCHelper

open Swate.Electron.Shared
open ARCtrl

let tryGetArcFilePath appState (arcFile: ArcFiles) =
    match appState with
    | AppState.ARC arcPath ->
        let root = arcPath.Replace("\\", "/").TrimEnd('/')

        match arcFile with
        | ArcFiles.Investigation _ -> Some $"{root}/isa.investigation.xlsx"
        | ArcFiles.Study(study, _) -> Some $"{root}/studies/{study.Identifier}/isa.study.xlsx"
        | ArcFiles.Assay assay -> Some $"{root}/assays/{assay.Identifier}/isa.assay.xlsx"
        | ArcFiles.Run run -> Some $"{root}/runs/{run.Identifier}/isa.run.xlsx"
        | ArcFiles.Workflow workflow -> Some $"{root}/workflows/{workflow.Identifier}/isa.workflow.xlsx"
        | ArcFiles.DataMap _
        | ArcFiles.Template _ -> None
    | AppState.Init -> None