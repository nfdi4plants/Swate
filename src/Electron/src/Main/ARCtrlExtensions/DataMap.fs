namespace Main.ARCtrlExtensions

open ARCtrl
open ARCtrl.Contract
open Swate.Components.Shared

[<RequireQualifiedAccess>]
module DataMapContracts =

    let createForParent (parentInfo: DatamapParentInfo) (dataMap: DataMap) : Contract =
        match parentInfo.Parent with
        | DataMapParent.Assay -> dataMap.ToCreateContractForAssay(parentInfo.ParentId)
        | DataMapParent.Study -> dataMap.ToCreateContractForStudy(parentInfo.ParentId)
        | DataMapParent.Run -> dataMap.ToCreateContractForRun(parentInfo.ParentId)
        | DataMapParent.Workflow -> dataMap.ToCreateContractForWorkflow(parentInfo.ParentId)

    let deleteForParent (parentInfo: DatamapParentInfo) (dataMap: DataMap) : Contract =
        match parentInfo.Parent with
        | DataMapParent.Assay -> dataMap.ToDeleteContractForAssay(parentInfo.ParentId)
        | DataMapParent.Study -> dataMap.ToDeleteContractForStudy(parentInfo.ParentId)
        | DataMapParent.Run -> dataMap.ToDeleteContractForRun(parentInfo.ParentId)
        | DataMapParent.Workflow -> dataMap.ToDeleteContractForWorkflow(parentInfo.ParentId)
