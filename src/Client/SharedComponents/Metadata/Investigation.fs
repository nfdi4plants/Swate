module Components.Metadata.Investigation

open System
open Feliz
open Feliz.DaisyUI
open ARCtrl
open Components
open Components.Forms

let Main(investigation: ArcInvestigation, setInvestigation: ArcInvestigation -> unit) =
    Generic.Section [
        Generic.BoxedField
            (Some "Investigation Metadata")
            None
            [
                FormComponents.TextInput (
                    investigation.Identifier,
                    (fun s ->
                        let nextInvestigation = IdentifierSetters.setInvestigationIdentifier s investigation
                        setInvestigation nextInvestigation),
                    "Identifier"
                )
                FormComponents.TextInput (
                    Option.defaultValue "" investigation.Title,
                    (fun s ->
                        investigation.Title <- s |> Option.whereNot String.IsNullOrWhiteSpace
                        setInvestigation investigation),
                    "Title"
                )
                FormComponents.TextInput (
                    Option.defaultValue "" investigation.Description,
                    (fun s ->
                        investigation.Description <- s |> Option.whereNot String.IsNullOrWhiteSpace
                        setInvestigation investigation),
                    "Description",
                    isarea = true
                )
                FormComponents.PersonsInput(
                    investigation.Contacts,
                    (fun i ->
                        investigation.Contacts <- ResizeArray i
                        setInvestigation investigation),
                    "Contacts"
                )
                FormComponents.PublicationsInput(
                    investigation.Publications,
                    (fun i ->
                        investigation.Publications <- i
                        setInvestigation investigation),
                    "Publications"
                )
                FormComponents.DateTimeInput (
                    Option.defaultValue "" investigation.SubmissionDate,
                    (fun s ->
                        investigation.SubmissionDate <- if s = "" then None else Some s
                        setInvestigation investigation),
                    "Submission Date"
                )
                FormComponents.DateTimeInput (
                    Option.defaultValue "" investigation.PublicReleaseDate,
                    (fun s ->
                        investigation.PublicReleaseDate <- if s = "" then None else Some s
                        setInvestigation investigation),
                    "Public Release Date"
                )
                FormComponents.OntologySourceReferencesInput(
                    investigation.OntologySourceReferences,
                    (fun oas ->
                        investigation.OntologySourceReferences <- oas
                        setInvestigation investigation),
                    "Ontology Source References"
                )
                //// This code might become relevant in the future. We decided to use implicit registrations for now. If this results in issues we can switch back to explicit registrations.
                ////FormComponents.TextInputs(
                ////    Array.ofSeq inv.RegisteredStudyIdentifiers,
                ////    "RegisteredStudyIdentifiers",
                ////    (fun i ->
                ////        inv.RegisteredStudyIdentifiers <- ResizeArray i
                ////        inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                ////)
                FormComponents.CommentsInput(
                    investigation.Comments,
                    (fun comments ->
                        investigation.Comments <- ResizeArray comments
                        setInvestigation investigation),
                    "Comments"
                )
            ]
    ]