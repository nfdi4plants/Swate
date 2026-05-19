module ARC

open ARCtrl
open ARCtrl.ArcPathHelper
open ARCtrl.Contract
open ARCtrl.Helper
open Swate.Components.Shared

type ARC with

    member this.TryArcFileByPath(split, arc) =

        match split with
        | InvestigationPath _ ->
            ArcFiles.Investigation arc |> Some
        | AssayPath p ->
            let identifier = (Identifier.Assay.identifierFromFileName p)
            let assay = arc.TryGetAssay identifier
            assay |> Option.map ArcFiles.Assay
        | StudyPath p ->
            let identifier = (Identifier.Study.identifierFromFileName p)
            let study = arc.TryGetStudy identifier

            study
            |> Option.map (fun s ->
                let assignedAssays =
                    s.RegisteredAssayIdentifiers |> Seq.choose arc.TryGetAssay |> List.ofSeq

                ArcFiles.Study(s, assignedAssays)
            )
        | WorkflowPath p ->
            let identifier = (Identifier.Workflow.identifierFromFileName p)
            let workflow = arc.TryGetWorkflow identifier
            workflow |> Option.map ArcFiles.Workflow
        | RunPath p ->
            let identifier = (Identifier.Run.identifierFromFileName p)
            let run = arc.TryGetRun identifier
            run |> Option.map ArcFiles.Run
        | DatamapPath _ ->
            match split with
            | [| AssaysFolderName; anyAssayName; DataMapFileName |] ->
                let assay = arc.TryGetAssay(Identifier.Assay.identifierFromFileName anyAssayName)

                let datamap =
                    assay
                    |> Option.bind (fun a -> a.DataMap)
                    |> Option.map (fun dm ->
                        let dmpi = DatamapParentInfo.create anyAssayName DataMapParent.Assay |> Some
                        dmpi, dm
                    )

                datamap |> Option.map ArcFiles.DataMap
            | [| StudiesFolderName; anyStudyName; DataMapFileName |] ->
                let study = arc.TryGetStudy(Identifier.Study.identifierFromFileName anyStudyName)

                let datamap =
                    study
                    |> Option.bind (fun s -> s.DataMap)
                    |> Option.map (fun dm ->
                        let dmpi = DatamapParentInfo.create anyStudyName DataMapParent.Study |> Some
                        dmpi, dm
                    )

                datamap |> Option.map ArcFiles.DataMap
            | [| WorkflowsFolderName; anyWorkflowName; DataMapFileName |] ->
                let workflow =
                    arc.TryGetWorkflow(Identifier.Workflow.identifierFromFileName anyWorkflowName)

                let datamap =
                    workflow
                    |> Option.bind (fun w -> w.DataMap)
                    |> Option.map (fun dm ->
                        let dmpi = DatamapParentInfo.create anyWorkflowName DataMapParent.Workflow |> Some
                        dmpi, dm
                    )

                datamap |> Option.map ArcFiles.DataMap
            | [| RunsFolderName; anyRunName; DataMapFileName |] ->
                let run = arc.TryGetRun(Identifier.Run.identifierFromFileName anyRunName)

                let datamap =
                    run
                    |> Option.bind (fun r -> r.DataMap)
                    |> Option.map (fun dm ->
                        let dmpi = DatamapParentInfo.create anyRunName DataMapParent.Run |> Some
                        dmpi, dm
                    )

                datamap |> Option.map ArcFiles.DataMap
            | _ -> None
        | _ -> None
