namespace Main.ArcMerge

open ARCtrl
open ARCtrl.Contract
open CrossAsync
open Swate.Components.Shared

[<AutoOpen>]
module ArcAddExtensions =

    let private failIfEntityExists (entityKind: string) (identifier: string) (exists: bool) =
        if exists then
            failwith $"ARC already contains {entityKind} with identifier '{identifier}'."

    let private getContractsFromWorkingArc
        (workingArc: ARC)
        (arcFileContracts: unit -> Contract[])
        (includeUpdateContractsFlag: bool option)
        =
        workingArc.UpdateFileSystem()

        match includeUpdateContractsFlag with
        | Some true -> workingArc.GetUpdateContracts(skipUpdateFS = true)
        | _ -> arcFileContracts ()

    let private prepareEntityAddContracts
        (sourceArc: ARC)
        (entityKind: string)
        (identifier: string)
        (exists: bool)
        (addToArc: ARC -> unit)
        (arcFileContracts: unit -> Contract[])
        (includeUpdateContractsFlag: bool option)
        =
        failIfEntityExists entityKind identifier exists
        let workingArc = copyArcPreservingStaticHashes sourceArc
        addToArc workingArc

        let contracts =
            getContractsFromWorkingArc workingArc arcFileContracts includeUpdateContractsFlag

        workingArc, contracts

    let private prepareAddContracts (sourceArc: ARC) (arcFile: ArcFiles) (includeUpdateContractsFlag: bool option) =
        match arcFile with
        | ArcFiles.Assay assay ->
            let assayCopy = assay.Copy()

            prepareEntityAddContracts
                sourceArc
                "assay"
                assay.Identifier
                (sourceArc.ContainsAssay assay.Identifier)
                (fun arc -> arc.AddAssay assayCopy)
                (fun () -> assayCopy.ToCreateContract(true))
                includeUpdateContractsFlag
        | ArcFiles.Study(study, _) ->
            let studyCopy = study.Copy()

            prepareEntityAddContracts
                sourceArc
                "study"
                study.Identifier
                (sourceArc.ContainsStudy study.Identifier)
                (fun arc -> arc.AddStudy studyCopy)
                (fun () -> studyCopy.ToCreateContract(true))
                includeUpdateContractsFlag
        | ArcFiles.Run run ->
            let runCopy = run.Copy()

            prepareEntityAddContracts
                sourceArc
                "run"
                run.Identifier
                (sourceArc.ContainsRun run.Identifier)
                (fun arc -> arc.AddRun runCopy)
                (fun () -> runCopy.ToCreateContract(true))
                includeUpdateContractsFlag
        | ArcFiles.Workflow workflow ->
            let workflowCopy = workflow.Copy()

            prepareEntityAddContracts
                sourceArc
                "workflow"
                workflow.Identifier
                (sourceArc.ContainsWorkflow workflow.Identifier)
                (fun arc -> arc.AddWorkflow workflowCopy)
                (fun () -> workflowCopy.ToCreateContract(true))
                includeUpdateContractsFlag
        | ArcFiles.Investigation _ -> failwith "Adding investigation files is not supported."
        | ArcFiles.DataMap _ -> failwith "Adding datamap files is not supported."
        | ArcFiles.Template _ -> failwith "Adding template files is not supported."

    let private commitAddedArcFile (targetArc: ARC) (workingArc: ARC) (arcFile: ArcFiles) =
        match arcFile with
        | ArcFiles.Assay assay ->
            workingArc.TryGetAssay assay.Identifier
            |> Option.iter (fun sourceAssay -> targetArc.SetAssay(assay.Identifier, sourceAssay.Copy()))
        | ArcFiles.Study(study, _) ->
            workingArc.TryGetStudy study.Identifier
            |> Option.iter (fun sourceStudy -> targetArc.SetStudy(study.Identifier, sourceStudy.Copy()))
        | ArcFiles.Run run ->
            workingArc.TryGetRun run.Identifier
            |> Option.iter (fun sourceRun -> targetArc.SetRun(run.Identifier, sourceRun.Copy()))
        | ArcFiles.Workflow workflow ->
            workingArc.TryGetWorkflow workflow.Identifier
            |> Option.iter (fun sourceWorkflow -> targetArc.SetWorkflow(workflow.Identifier, sourceWorkflow.Copy()))
        | _ -> ()

        targetArc.UpdateFileSystem()

    type ARC with

        member this.GetAssayAddContracts(assay: ArcAssay, ?includeUpdateContractsFlag: bool) =
            let _, contracts = prepareAddContracts this (ArcFiles.Assay assay) includeUpdateContractsFlag
            contracts

        member this.GetStudyAddContracts(study: ArcStudy, ?includeUpdateContractsFlag: bool) =
            let _, contracts = prepareAddContracts this (ArcFiles.Study(study, [])) includeUpdateContractsFlag
            contracts

        member this.GetRunAddContracts(run: ArcRun, ?includeUpdateContractsFlag: bool) =
            let _, contracts = prepareAddContracts this (ArcFiles.Run run) includeUpdateContractsFlag
            contracts

        member this.GetWorkflowAddContracts(workflow: ArcWorkflow, ?includeUpdateContractsFlag: bool) =
            let _, contracts = prepareAddContracts this (ArcFiles.Workflow workflow) includeUpdateContractsFlag
            contracts

        member this.GetAddContracts(arcFile: ArcFiles, ?includeUpdateContractsFlag: bool) =
            let _, contracts = prepareAddContracts this arcFile includeUpdateContractsFlag
            contracts
            
        member this.TryAddArcFileAsync(arcPath: string, arcFile: ArcFiles, ?includeUpdateContractsFlag: bool) =
            crossAsync {
                try
                    let workingArc, contracts = prepareAddContracts this arcFile includeUpdateContractsFlag
                    let! result = fullFillContractBatchAsync arcPath contracts

                    match result with
                    | Ok _ -> commitAddedArcFile this workingArc arcFile
                    | Error _ -> ()

                    return result
                with error ->
                    return Error [| error.Message |]
            }

        member this.AddArcFileAsync(arcPath: string, arcFile: ArcFiles) =
            crossAsync {
                let! result = this.TryAddArcFileAsync(arcPath, arcFile)

                match result with
                | Ok _ -> ()
                | Error errors ->
                    failwithf
                        "Could not add ARC file, failed with the following errors %s"
                        (PathHelpers.formatContractErrors errors)
            }
