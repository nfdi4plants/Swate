namespace Main.ARCtrlExtensions

open ARCtrl
open ARCtrl.Contract
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper


[<AutoOpen>]
module DataMapExtensions =

    let private tryGetParentDataMapRemovalContract (arc: ARC) (parentInfo: DatamapParentInfo) =
        match parentInfo.Parent with
        | DataMapParent.Assay ->
            arc.TryGetAssay parentInfo.ParentId
            |> Option.map (fun assay ->
                assay.DataMap <- None
                assay.ToUpdateContract()
            )
        | DataMapParent.Study ->
            arc.TryGetStudy parentInfo.ParentId
            |> Option.map (fun study ->
                study.DataMap <- None
                study.ToUpdateContract()
            )
        | DataMapParent.Run ->
            arc.TryGetRun parentInfo.ParentId
            |> Option.map (fun run ->
                run.DataMap <- None
                run.ToUpdateContract()
            )
        | DataMapParent.Workflow ->
            arc.TryGetWorkflow parentInfo.ParentId
            |> Option.map (fun workflow ->
                workflow.DataMap <- None
                workflow.ToUpdateContract()
            )

    type DataMap with

        member this.ToCreateContract(parentInfo: DatamapParentInfo) : Contract = DataMapContract.create parentInfo this

        member this.ToDeleteContract(parentInfo: DatamapParentInfo) : Contract = DataMapContract.delete parentInfo this

    type ARC with

        /// Deletes a DataMap through its parent-specific ARCtrl contract and updates the in-memory ARC after success.
        member this.TryDeleteDataMapAsync(arcPath: string, parentInfo: DatamapParentInfo) = promise {
            match this.TryGetDataMap parentInfo with
            | None ->
                let parentPath = DatamapParentInfo.toFolderPath parentInfo

                return
                    Error(
                        exn
                            $"Parent '{parentPath}' does not have a DataMap to delete. Refresh the File Explorer and try again."
                    )
            | Some dataMap ->
                let deleteContract = dataMap.ToDeleteContract(parentInfo)

                match! ARC.LoadAsyncSwate arcPath with
                | Error errors ->
                    return
                        Error(
                            exn
                                $"The DataMap could not be deleted because its persisted parent could not be loaded. {PathHelpers.formatContractErrors errors}"
                        )
                | Ok persistedArc ->
                    match tryGetParentDataMapRemovalContract persistedArc parentInfo with
                    | None ->
                        let parentPath = DatamapParentInfo.toFolderPath parentInfo

                        return
                            Error(
                                exn
                                    $"The DataMap could not be deleted because parent '{parentPath}' was not found on disk. Refresh the File Explorer and try again."
                            )
                    | Some parentUpdateContract ->
                        match! fullFillContractBatchAsync arcPath [| parentUpdateContract; deleteContract |] with
                        | Error errors ->
                            return
                                Error(
                                    exn
                                        $"The DataMap could not be deleted from disk. {PathHelpers.formatContractErrors errors}"
                                )
                        | Ok _ ->
                            this.TrySetDataMap(parentInfo, None) |> ignore
                            this.UpdateFileSystem()
                            return Ok()
        }
