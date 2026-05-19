namespace Main.ArcMerge

open ARCtrl
open ARCtrl.Contract
open CrossAsync
open Swate.Components.Shared

[<AutoOpen>]
module ArcAddExtensions =

    let private formatContractErrors (errors: string[]) =
        errors |> Array.map string |> String.concat "\n"

    let private failIfEntityExists (entityKind: string) (identifier: string) (exists: bool) =
        if exists then
            failwith $"ARC already contains {entityKind} with identifier '{identifier}'."

    type ARC with

        member this.GetAssayAddContracts(assay: ArcAssay, ?includeUpdateContractsFlag: bool) =
            failIfEntityExists "assay" assay.Identifier (this.ContainsAssay assay.Identifier)
            this.AddAssay(assay)
            this.UpdateFileSystem()
            match includeUpdateContractsFlag with
            | Some true -> this.GetUpdateContracts(skipUpdateFS = true)
            | _ -> assay.ToCreateContract(true)

        member this.GetStudyAddContracts(study: ArcStudy, ?includeUpdateContractsFlag: bool) =
            failIfEntityExists "study" study.Identifier (this.ContainsStudy study.Identifier)
            this.AddStudy(study)
            this.UpdateFileSystem()
            match includeUpdateContractsFlag with
            | Some true -> this.GetUpdateContracts(skipUpdateFS = true)
            | _ -> study.ToCreateContract(true)

        member this.GetRunAddContracts(run: ArcRun, ?includeUpdateContractsFlag: bool) =
            failIfEntityExists "run" run.Identifier (this.ContainsRun run.Identifier)
            this.AddRun(run)
            this.UpdateFileSystem()
            match includeUpdateContractsFlag with
            | Some true -> this.GetUpdateContracts(skipUpdateFS = true)
            | _ -> run.ToCreateContract(true)

        member this.GetWorkflowAddContracts(workflow: ArcWorkflow, ?includeUpdateContractsFlag: bool) =
            failIfEntityExists "workflow" workflow.Identifier (this.ContainsWorkflow workflow.Identifier)
            this.AddWorkflow(workflow)
            this.UpdateFileSystem()
            match includeUpdateContractsFlag with
            | Some true -> this.GetUpdateContracts(skipUpdateFS = true)
            | _ -> workflow.ToCreateContract(true)

        member this.GetAddContracts(arcFile: ArcFiles, ?includeUpdateContractsFlag: bool) =
            match arcFile with
            | ArcFiles.Assay assay -> this.GetAssayAddContracts(assay, ?includeUpdateContractsFlag = includeUpdateContractsFlag)
            | ArcFiles.Study(study, _) -> this.GetStudyAddContracts(study, ?includeUpdateContractsFlag = includeUpdateContractsFlag)
            | ArcFiles.Run run -> this.GetRunAddContracts(run, ?includeUpdateContractsFlag = includeUpdateContractsFlag)
            | ArcFiles.Workflow workflow -> this.GetWorkflowAddContracts(workflow, ?includeUpdateContractsFlag = includeUpdateContractsFlag)
            | ArcFiles.Investigation _ -> failwith "Adding investigation files is not supported."
            | ArcFiles.DataMap _ -> failwith "Adding datamap files is not supported."
            | ArcFiles.Template _ -> failwith "Adding template files is not supported."
            
        member this.TryAddArcFileAsync(arcPath: string, arcFile: ArcFiles, ?includeUpdateContractsFlag: bool) =
            crossAsync {
                try
                    let contracts = this.GetAddContracts(arcFile, ?includeUpdateContractsFlag = includeUpdateContractsFlag)
                    return! fullFillContractBatchAsync arcPath contracts
                with error ->
                    return Error [| error.Message |]
            }

        member this.AddArcFileAsync(arcPath: string, arcFile: ArcFiles) =
            crossAsync {
                let! result = this.TryAddArcFileAsync(arcPath, arcFile)

                match result with
                | Ok _ -> ()
                | Error errors ->
                    failwithf "Could not add ARC file, failed with the following errors %s" (formatContractErrors errors)
            }
