namespace Main.ARCtrlExtensions

open ARCtrl.Contract
open Swate.Components.Shared

[<RequireQualifiedAccess>]
module ArcFileCreateContracts =

    let createContracts (withFolder: bool) (arcFile: ArcFiles) : Contract[] =
        match arcFile with
        | ArcFiles.Assay assay -> assay.ToCreateContract(withFolder)
        | ArcFiles.Study(study, _) -> study.ToCreateContract(withFolder)
        | ArcFiles.Workflow workflow -> workflow.ToCreateContract(withFolder)
        | ArcFiles.Run run -> run.ToCreateContract(withFolder)
        | unsupportedArcFile -> failwithf "Cannot create ARC file contracts for %A." unsupportedArcFile
