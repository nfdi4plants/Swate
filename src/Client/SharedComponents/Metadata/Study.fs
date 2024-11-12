module Components.Metadata.Study

open Feliz
open ARCtrl
open Components
open Components.Forms
open System

let Main(study: ArcStudy, assignedAssays: ArcAssay list, setArcStudy: (ArcStudy * ArcAssay list) -> unit, setDatamap: ArcStudy -> DataMap option -> unit) =
    Generic.Section [
        Generic.BoxedField
            (Some "Study Metadata")
            None
            [
                FormComponents.TextInput (
                    study.Identifier,
                    (fun s ->
                        let nextStudy = IdentifierSetters.setStudyIdentifier s study
                        setArcStudy (nextStudy , assignedAssays)),
                    "Identifier"
                )
                FormComponents.TextInput (
                    Option.defaultValue "" study.Description,
                    (fun s ->
                        study.Description <- s |> Option.whereNot String.IsNullOrWhiteSpace
                        setArcStudy (study , assignedAssays)),
                    "Description",
                    isarea=true
                )
                FormComponents.PersonsInput(
                    study.Contacts,
                    (fun persons ->
                        study.Contacts <- ResizeArray(persons)
                        setArcStudy (study , assignedAssays)),
                    "Contacts"
                )
                FormComponents.PublicationsInput (
                    study.Publications,
                    (fun pubs ->
                        study.Publications <- ResizeArray(pubs)
                        setArcStudy (study , assignedAssays)),
                    "Publications"
                )
                FormComponents.DateTimeInput(
                    Option.defaultValue "" study.SubmissionDate,
                    (fun s ->
                        study.SubmissionDate <- s |> Option.whereNot String.IsNullOrWhiteSpace
                        setArcStudy (study , assignedAssays)),
                    "Submission Date"
                )
                FormComponents.DateTimeInput (
                    Option.defaultValue "" study.PublicReleaseDate,
                    (fun s ->
                        study.PublicReleaseDate <- s |> Option.whereNot String.IsNullOrWhiteSpace
                        setArcStudy (study , assignedAssays)),
                    "Public ReleaseDate"
                )
                FormComponents.OntologyAnnotationsInput(
                    study.StudyDesignDescriptors,
                    (fun oas ->
                        study.StudyDesignDescriptors <- ResizeArray(oas)
                        setArcStudy (study , assignedAssays)),
                    "Study Design Descriptors"
                )
                //FormComponents.TextInputs(
                //    Array.ofSeq study.RegisteredAssayIdentifiers,
                //    "Registered Assay Identifiers",
                //    fun rais ->
                //        study.RegisteredAssayIdentifiers <- ResizeArray(rais)
                //        (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
                //)
                FormComponents.CommentsInput(
                    study.Comments,
                    (fun comments ->
                        study.Comments <- ResizeArray(comments)
                        setArcStudy (study , assignedAssays)),
                    "Comments"
                )
            ]
        Datamap.DatamapConfig.Main(
            study.DataMap,
            fun dataMap ->
                setDatamap study dataMap
        )
    ]