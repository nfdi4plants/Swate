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
        | ArcFiles.DataMap(Some parentInfo, dataMap) ->
            match parentInfo.Parent with
            | DataMapParent.Assay -> dataMap.ToCreateContractForAssay(parentInfo.ParentId)
            | DataMapParent.Study -> dataMap.ToCreateContractForStudy(parentInfo.ParentId)
            | DataMapParent.Run -> dataMap.ToCreateContractForRun(parentInfo.ParentId)
            | DataMapParent.Workflow -> dataMap.ToCreateContractForWorkflow(parentInfo.ParentId)
            |> Array.singleton
        | unsupportedArcFile -> failwithf "Cannot create ARC file contracts for %A." unsupportedArcFile
