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

[<AutoOpen>]
module DataMapExtensions =

    type ARC with

        member this.TryGetDataMapParentArcFile(parentInfo: DatamapParentInfo) =
            match parentInfo.Parent with
            | DataMapParent.Assay -> this.TryGetAssay parentInfo.ParentId |> Option.map ArcFiles.Assay
            | DataMapParent.Study ->
                this.TryGetStudy parentInfo.ParentId
                |> Option.map (fun study -> ArcFiles.Study(study, []))
            | DataMapParent.Run -> this.TryGetRun parentInfo.ParentId |> Option.map ArcFiles.Run
            | DataMapParent.Workflow -> this.TryGetWorkflow parentInfo.ParentId |> Option.map ArcFiles.Workflow
