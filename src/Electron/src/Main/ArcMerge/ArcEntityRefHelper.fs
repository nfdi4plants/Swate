namespace Main.ArcMerge

open ARCtrl
open ARCtrl.Contract
open Swate.Components.Shared

[<AutoOpen>]
module ArcEntityRefExtensions =

    type ArcEntityRef with
        static member fromPath(path: string) : ArcEntityRef =
            match ArcEntityPathRules.tryParseCanonicalArcFileTarget path with
            | Some ArcEntityPathRules.CanonicalArcFileTarget.InvestigationFile -> ArcEntityRef.Investigation
            | Some(ArcEntityPathRules.CanonicalArcFileTarget.EntityFile(zone, identifier)) ->
                match zone with
                | ArcEntityPathRules.AddZone.Assays -> ArcEntityRef.Assay identifier
                | ArcEntityPathRules.AddZone.Studies -> ArcEntityRef.Study identifier
                | ArcEntityPathRules.AddZone.Workflows -> ArcEntityRef.Workflow identifier
                | ArcEntityPathRules.AddZone.Runs -> ArcEntityRef.Run identifier
            | Some(ArcEntityPathRules.CanonicalArcFileTarget.DataMapFile(zone, identifier)) ->
                match zone with
                | ArcEntityPathRules.AddZone.Assays -> ArcEntityRef.AssayDataMap identifier
                | ArcEntityPathRules.AddZone.Studies -> ArcEntityRef.StudyDataMap identifier
                | ArcEntityPathRules.AddZone.Workflows -> ArcEntityRef.WorkflowDataMap identifier
                | ArcEntityPathRules.AddZone.Runs -> ArcEntityRef.RunDataMap identifier
            | None -> ArcEntityRef.Unknown path

module ArcFileDefaults =

    let private createDefaultArcFile (fileType: ArcFilesDiscriminate) (identifier: string) =
        match fileType with
        | ArcFilesDiscriminate.Assay -> ArcFiles.Assay(ArcAssay.init identifier)
        | ArcFilesDiscriminate.Study -> ArcFiles.Study(ArcStudy.init identifier, [])
        | ArcFilesDiscriminate.Run -> ArcFiles.Run(ArcRun.init identifier)
        | ArcFilesDiscriminate.Workflow -> ArcFiles.Workflow(ArcWorkflow.init identifier)
        | unsupportedFileType -> failwithf "Cannot create default ARC entity contracts for %A." unsupportedFileType

    let createDefaultEntityContracts
        (fileType: ArcFilesDiscriminate)
        (withFolder: bool)
        (identifier: string)
        : Contract[] =
        let arcFile = createDefaultArcFile fileType identifier
        arcFile.EnsureDefaultAnnotationTable() |> ignore

        match arcFile with
        | ArcFiles.Assay assay -> assay.ToCreateContract(withFolder)
        | ArcFiles.Study(study, _) -> study.ToCreateContract(withFolder)
        | ArcFiles.Run run -> run.ToCreateContract(withFolder)
        | ArcFiles.Workflow workflow -> workflow.ToCreateContract(withFolder)
        | _ -> failwithf "Cannot create default ARC entity contracts for %A." fileType
