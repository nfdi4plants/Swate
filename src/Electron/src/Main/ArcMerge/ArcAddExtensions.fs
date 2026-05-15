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

        member this.GetAssayAddContracts(assay: ArcAssay) =
            failIfEntityExists "assay" assay.Identifier (this.ContainsAssay assay.Identifier)
            this.AddAssay(assay)
            this.UpdateFileSystem()
            this.GetUpdateContracts(skipUpdateFS = true)

        member this.GetStudyAddContracts(study: ArcStudy) =
            failIfEntityExists "study" study.Identifier (this.ContainsStudy study.Identifier)
            this.AddStudy(study)
            this.UpdateFileSystem()
            this.GetUpdateContracts(skipUpdateFS = true)

        member this.GetRunAddContracts(run: ArcRun) =
            failIfEntityExists "run" run.Identifier (this.ContainsRun run.Identifier)
            this.AddRun(run)
            this.UpdateFileSystem()
            this.GetUpdateContracts(skipUpdateFS = true)

        member this.GetWorkflowAddContracts(workflow: ArcWorkflow) =
            failIfEntityExists "workflow" workflow.Identifier (this.ContainsWorkflow workflow.Identifier)
            this.AddWorkflow(workflow)
            this.UpdateFileSystem()
            this.GetUpdateContracts(skipUpdateFS = true)

        member this.GetAddContracts(arcFile: ArcFiles) =
            match arcFile with
            | ArcFiles.Assay assay -> this.GetAssayAddContracts assay
            | ArcFiles.Study(study, _) -> this.GetStudyAddContracts study
            | ArcFiles.Run run -> this.GetRunAddContracts run
            | ArcFiles.Workflow workflow -> this.GetWorkflowAddContracts workflow
            | ArcFiles.Investigation _ -> failwith "Adding investigation files is not supported."
            | ArcFiles.DataMap _ -> failwith "Adding datamap files is not supported."
            | ArcFiles.Template _ -> failwith "Adding template files is not supported."

        member this.TryAddAsync(arcPath: string, arcFile: ArcFiles) =
            crossAsync {
                try
                    let contracts = this.GetAddContracts arcFile
                    return! fullFillContractBatchAsync arcPath contracts
                with error ->
                    return Error [| error.Message |]
            }

        member this.AddAsync(arcPath: string, arcFile: ArcFiles) =
            crossAsync {
                let! result = this.TryAddAsync(arcPath, arcFile)

                match result with
                | Ok _ -> ()
                | Error errors ->
                    failwithf "Could not add ARC file, failed with the following errors %s" (formatContractErrors errors)
            }
