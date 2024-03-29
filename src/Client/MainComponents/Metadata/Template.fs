﻿module MainComponents.Metadata.Template

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl.ISA
open Shared
open ARCtrl.Template


let Main(template: Template, model: Messages.Model, dispatch: Msg -> unit) = 
    Bulma.section [
        FormComponents.GUIDInput (
            template.Id,
            "Identifier", 
            (fun (s:string) -> 
                template.Id <- System.Guid(s)
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true
        )
        FormComponents.TextInput (
            template.Name,
            "Name",
            (fun (s:string) -> 
                template.Name <- s
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true
        )
        FormComponents.TextInput (
            template.Description,
            "Description",
            (fun (s:string) -> 
                template.Description <- s
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true,
            isarea=true
        )
        FormComponents.TextInput (
            template.Organisation.ToString(),
            "Organisation",
            (fun (s:string) -> 
                template.Organisation <- Organisation.ofString(s)
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true
        )
        FormComponents.TextInput(
            template.Version,
            "Version",
            (fun (s:string) -> 
                template.Version <- s
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true
        )
        FormComponents.DateTimeInput(
            template.LastUpdated,
            "Last Updated",
            (fun dt -> 
                template.LastUpdated <- dt
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.OntologyAnnotationsInput(
            template.Tags,
            "Tags",
            (fun (s) -> 
                template.Tags <- s
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.OntologyAnnotationsInput(
            template.EndpointRepositories,
            "Endpoint Repositories",
            (fun (s) -> 
                template.EndpointRepositories <- s
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.PersonsInput(
            template.Authors,
            "Authors",
            (fun (s) -> 
                template.Authors <- s
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
    ]