module MainComponents.Metadata.Investigation

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl.ISA
open Shared

let Main(inv: ArcInvestigation, model: Messages.Model, dispatch: Msg -> unit) = 
    Bulma.section [ 
        FormComponents.TextInput (
            inv.Identifier,
            "Identifier", 
            (fun s -> 
                let nextInv = IdentifierSetters.setInvestigationIdentifier s inv
                nextInv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true
        )
        FormComponents.TextInput (
            Option.defaultValue "" inv.Title,
            "Title", 
            (fun s -> 
                inv.Title <- if s = "" then None else Some s
                inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true
        )
        FormComponents.TextInput (
            Option.defaultValue "" inv.Description,
            "Description", 
            (fun s -> 
                inv.Description <- if s = "" then None else Some s
                inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true,
            isarea=true
        )
        FormComponents.PersonsInput(
            inv.Contacts,
            "Contacts",
            (fun i -> 
                inv.Contacts <- i
                inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.PublicationsInput(
            inv.Publications,
            "Publications",
            (fun i -> 
                inv.Publications <- i
                inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.DateTimeInput (
            Option.defaultValue "" inv.SubmissionDate,
            "Submission Date", 
            (fun s -> 
                inv.SubmissionDate <- if s = "" then None else Some s
                inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.DateTimeInput (
            Option.defaultValue "" inv.PublicReleaseDate,
            "Public Release Date", 
            (fun s -> 
                inv.PublicReleaseDate <- if s = "" then None else Some s
                inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.OntologySourceReferencesInput(
            inv.OntologySourceReferences,
            "Ontology Source References",
            (fun oas -> 
                inv.OntologySourceReferences <- oas
                inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.TextInputs(
            Array.ofSeq inv.RegisteredStudyIdentifiers,
            "RegisteredStudyIdentifiers",
            (fun i -> 
                inv.RegisteredStudyIdentifiers <- ResizeArray i
                inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.CommentsInput(
            inv.Comments,
            "Comments",
            (fun i -> 
                inv.Comments <- i
                inv |> Investigation |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
    ]