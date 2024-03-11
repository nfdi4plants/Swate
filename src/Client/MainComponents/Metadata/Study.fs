module MainComponents.Metadata.Study

open Feliz
open Feliz.Bulma
open Messages
open ARCtrl.ISA
open Shared

let Main(study: ArcStudy, assignedAssays: ArcAssay list, model: Messages.Model, dispatch: Msg -> unit) = 
    Bulma.section [ 
        FormComponents.TextInput (
            study.Identifier,
            "Identifier", 
            (fun s -> 
                let nextAssay = IdentifierSetters.setStudyIdentifier s study
                (nextAssay, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true
        )
        FormComponents.TextInput (
            Option.defaultValue "" study.Description,
            "Description", 
            (fun s -> 
                let s = if s = "" then None else Some s
                study.Description <- s
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true,
            isarea=true
        )
        FormComponents.PersonsInput(
            study.Contacts,
            "Contacts",
            fun persons ->
                study.Contacts <- persons
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch 
        )
        FormComponents.PublicationsInput (
            study.Publications,
            "Publications",
            fun pubs -> 
                study.Publications <- pubs
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.DateTimeInput(
            Option.defaultValue "" study.SubmissionDate,
            "Submission Date", 
            fun s -> 
                let s = if s = "" then None else Some s
                study.SubmissionDate <- s
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.DateTimeInput (
            Option.defaultValue "" study.PublicReleaseDate,
            "Public ReleaseDate", 
            fun s -> 
                let s = if s = "" then None else Some s
                study.PublicReleaseDate <- s
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.OntologyAnnotationsInput(
            study.StudyDesignDescriptors,
            "Study Design Descriptors",
            fun oas ->
                study.StudyDesignDescriptors <- oas
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.TextInputs(
            Array.ofSeq study.RegisteredAssayIdentifiers,
            "Registered Assay Identifiers",
            fun rais ->
                study.RegisteredAssayIdentifiers <- ResizeArray(rais)
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.FactorsInput(
            study.Factors,
            "Factors",
            fun factors ->
                study.Factors <- factors
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.CommentsInput(
            study.Comments,
            "Comments",
            fun comments ->
                study.Comments <- comments
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch 
        )
    ]