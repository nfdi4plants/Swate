namespace Main.ArcMerge

open Swate.Components.Shared

[<AutoOpen>]
module ArcEntityRefExtensions =

    type ArcEntityRef with
        static member fromPath(path: string) : ArcEntityRef =
            match ArcDeletePathRules.tryParseCanonicalArcFileTarget path with
            | Some ArcDeletePathRules.CanonicalArcFileTarget.InvestigationFile -> ArcEntityRef.Investigation
            | Some(ArcDeletePathRules.CanonicalArcFileTarget.EntityFile(zone, identifier)) ->
                match zone with
                | ArcDeletePathRules.AddZone.Assays -> ArcEntityRef.Assay identifier
                | ArcDeletePathRules.AddZone.Studies -> ArcEntityRef.Study identifier
                | ArcDeletePathRules.AddZone.Workflows -> ArcEntityRef.Workflow identifier
                | ArcDeletePathRules.AddZone.Runs -> ArcEntityRef.Run identifier
            | Some(ArcDeletePathRules.CanonicalArcFileTarget.DataMapFile(zone, identifier)) ->
                match zone with
                | ArcDeletePathRules.AddZone.Assays -> ArcEntityRef.AssayDataMap identifier
                | ArcDeletePathRules.AddZone.Studies -> ArcEntityRef.StudyDataMap identifier
                | ArcDeletePathRules.AddZone.Workflows -> ArcEntityRef.WorkflowDataMap identifier
                | ArcDeletePathRules.AddZone.Runs -> ArcEntityRef.RunDataMap identifier
            | None -> ArcEntityRef.Unknown path
