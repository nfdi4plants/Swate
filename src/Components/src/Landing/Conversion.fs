namespace Swate.Components.Landing

open ARCtrl

[<RequireQualifiedAccess>]
module Conversion =

    let private normalizeFiles (files: string list) =
        files
        |> List.map _.Trim()
        |> List.filter (System.String.IsNullOrWhiteSpace >> not)

    let isSafePathSegment (value: string) =
        not (System.String.IsNullOrWhiteSpace value)
        && not (value.Contains "/")
        && not (value.Contains "\\")
        && not (value.Contains "..")

    let private fillInputColumnIfFilesExist (table: ArcTable) (files: string list) =
        let normalizedFiles = normalizeFiles files

        if normalizedFiles.Length > 0 then
            if table.TryGetInputColumn().IsNone then
                table.AddColumn(CompositeHeader.Input IOType.Data)

            let rows =
                normalizedFiles
                |> List.map (fun fileName -> ResizeArray [ CompositeCell.createDataFromString (fileName) ])
                |> ResizeArray

            table.AddRows(rows)

    let private applyCommonFieldsToStudy (draft: LandingDraft) (study: ArcStudy) =
        study.Title <- Some(draft.Title.Trim())
        study.Description <- Some(draft.Description.Trim())
        study.Contacts <- ResizeArray draft.InvolvedPeople
        study.Comments <- ResizeArray draft.Comments

    let private applyCommonFieldsToAssay (draft: LandingDraft) (assay: ArcAssay) =
        assay.Title <- Some(draft.Title.Trim())
        assay.Description <- Some(draft.Description.Trim())
        assay.Performers <- ResizeArray draft.InvolvedPeople
        assay.Comments <- ResizeArray draft.Comments

    let private toStudy (draft: LandingDraft) (identifier: string) =
        let study = ArcStudy.init identifier
        study.InitTable($"{identifier} Table") |> ignore

        applyCommonFieldsToStudy draft study
        study.Publications <- ResizeArray draft.Publications
        study.SubmissionDate <- Validation.toOptionalString draft.SubmissionDate
        study.PublicReleaseDate <- Validation.toOptionalString draft.PublicReleaseDate
        study.StudyDesignDescriptors <- ResizeArray draft.StudyDesignDescriptors

        let firstTable = study.Tables.[0]
        fillInputColumnIfFilesExist firstTable draft.Files

        study

    let private toAssay (draft: LandingDraft) (identifier: string) =
        let assay = ArcAssay.init identifier
        assay.InitTable($"{identifier} Table") |> ignore

        applyCommonFieldsToAssay draft assay
        assay.MeasurementType <- draft.MeasurementType
        assay.TechnologyType <- draft.TechnologyType
        assay.TechnologyPlatform <- draft.TechnologyPlatform

        let firstTable = assay.Tables.[0]
        fillInputColumnIfFilesExist firstTable draft.Files

        assay

    let private toProtocolIntent (draft: LandingDraft) (target: LandingTarget) (identifier: string) =
        if isSafePathSegment identifier |> not then
            None
        else
            match Validation.toOptionalString draft.MainText with
            | None -> None
            | Some content ->
                let folder =
                    match target with
                    | LandingTarget.Study -> "studies"
                    | LandingTarget.Assay -> "assays"

                Some {
                    RelativePath = $"{folder}/{identifier}/protocols/{identifier}_protocol.md"
                    Content = content
                }

    let toArcFile (draft: LandingDraft) (target: LandingTarget) =
        let identifier = Validation.resolveIdentifier draft

        let arcFile =
            match target with
            | LandingTarget.Study ->
                let study = toStudy draft identifier
                ArcFiles.Study(study, [])
            | LandingTarget.Assay ->
                let assay = toAssay draft identifier
                ArcFiles.Assay assay

        identifier, arcFile

    let toSubmitPayload (draft: LandingDraft) (target: LandingTarget) =
        let identifier, arcFile = toArcFile draft target
        let protocolIntent = toProtocolIntent draft target identifier

        {
            ArcFile = arcFile
            ProtocolIntent = protocolIntent
            Target = target
            Identifier = identifier
        }