namespace Main.ARCtrlExtensions

open ARCtrl
open ARCtrl.Contract
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper


[<AutoOpen>]
module DataMapExtensions =

    type DataMap with

        member this.ToCreateContract(parentInfo: DatamapParentInfo) : Contract =
            match parentInfo.Parent with
            | DataMapParent.Assay -> this.ToCreateContractForAssay(parentInfo.ParentId)
            | DataMapParent.Study -> this.ToCreateContractForStudy(parentInfo.ParentId)
            | DataMapParent.Run -> this.ToCreateContractForRun(parentInfo.ParentId)
            | DataMapParent.Workflow -> this.ToCreateContractForWorkflow(parentInfo.ParentId)

        member this.ToDeleteContract(parentInfo: DatamapParentInfo) : Contract =
            match parentInfo.Parent with
            | DataMapParent.Assay -> this.ToDeleteContractForAssay(parentInfo.ParentId)
            | DataMapParent.Study -> this.ToDeleteContractForStudy(parentInfo.ParentId)
            | DataMapParent.Run -> this.ToDeleteContractForRun(parentInfo.ParentId)
            | DataMapParent.Workflow -> this.ToDeleteContractForWorkflow(parentInfo.ParentId)

    type ARC with

        /// Deletes a DataMap through its parent-specific ARCtrl contract and updates the in-memory ARC after success.
        member this.TryDeleteDataMapAsync(arcPath: string, parentInfo: DatamapParentInfo) = promise {
            match this.TryGetDataMap parentInfo with
            | None ->
                let parentDescription = DatamapParentInfo.describeParent parentInfo

                return
                    Error(
                        exn
                            $"The {parentDescription} does not have a DataMap to delete. Refresh the File Explorer and try again."
                    )
            | Some dataMap ->
                let deleteContract = dataMap.ToDeleteContract(parentInfo)

                match! fullFillContractBatchAsync arcPath [| deleteContract |] with
                | Error errors ->
                    return
                        Error(
                            exn $"The DataMap could not be deleted from disk. {PathHelpers.formatContractErrors errors}"
                        )
                | Ok _ ->
                    this.TrySetDataMap(parentInfo, None) |> ignore
                    this.UpdateFileSystem()
                    return Ok()
        }
