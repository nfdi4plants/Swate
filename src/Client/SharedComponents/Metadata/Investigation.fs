module Components.Metadata.Investigation

open Feliz.Bulma
open ARCtrl
open Components
open Components.Forms

let Main(investigation: ArcInvestigation, setInvestigation: ArcInvestigation -> unit) =
    Bulma.section [
        Generic.BoxedField
            "Investigation Metadata"
            None
            [
                FormComponents.TextInput (
                    investigation.Identifier,
                    "Identifier", 
                    (fun s -> 
                        let nextInvestigation = IdentifierSetters.setInvestigationIdentifier s investigation
                        //nextInvestigation |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setInvestigation nextInvestigation),
                    fullwidth=true
                )
                FormComponents.TextInput (
                    Option.defaultValue "" investigation.Title,
                    "Title", 
                    (fun s -> 
                        investigation.Title <- if s = "" then None else Some s
                        //investigation |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setInvestigation investigation),
                    fullwidth = true
                )
                FormComponents.TextInput (
                    Option.defaultValue "" investigation.Description,
                    "Description", 
                    (fun s -> 
                        investigation.Description <- if s = "" then None else Some s
                        //investigation |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setInvestigation investigation),
                    fullwidth = true,
                    isarea = true
                )
                FormComponents.PersonsInput(
                    Array.ofSeq investigation.Contacts,
                    "Contacts",
                    (fun i -> 
                        investigation.Contacts <- ResizeArray i
                        //investigation |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                        setInvestigation investigation)
                )
                FormComponents.PublicationsInput(
                    Array.ofSeq investigation.Publications,
                    "Publications",
                    (fun i -> 
                        investigation.Publications <- ResizeArray i
                        //investigation |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                        setInvestigation investigation)
                )
                FormComponents.DateTimeInput (
                    Option.defaultValue "" investigation.SubmissionDate,
                    "Submission Date", 
                    (fun s -> 
                        investigation.SubmissionDate <- if s = "" then None else Some s
                        //investigation |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                        setInvestigation investigation)
                )
                FormComponents.DateTimeInput (
                    Option.defaultValue "" investigation.PublicReleaseDate,
                    "Public Release Date", 
                    (fun s -> 
                        investigation.PublicReleaseDate <- if s = "" then None else Some s
                        //investigation |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                        setInvestigation investigation)
                )
                FormComponents.OntologySourceReferencesInput(
                    Array.ofSeq investigation.OntologySourceReferences,
                    "Ontology Source References",
                    (fun oas -> 
                        investigation.OntologySourceReferences <- ResizeArray oas
                        //investigation |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                        setInvestigation investigation)
                )
                // This code might become relevant in the future. We decided to use implicit registrations for now. If this results in issues we can switch back to explicit registrations.
                //FormComponents.TextInputs(
                //    Array.ofSeq inv.RegisteredStudyIdentifiers,
                //    "RegisteredStudyIdentifiers",
                //    (fun i -> 
                //        inv.RegisteredStudyIdentifiers <- ResizeArray i
                //        inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                //)
                FormComponents.CommentsInput(
                    Array.ofSeq investigation.Comments,
                    "Comments",
                    (fun comments -> 
                        investigation.Comments <- ResizeArray comments
                        //investigation |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                        setInvestigation investigation)
                )
            ]
    ]