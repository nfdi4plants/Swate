module MainComponents.Metadata.Template

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl
open Shared
open ARCtrl.Template
open Model


let Main(template: Template, model: Model, dispatch: Msg -> unit) = 
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
            Array.ofSeq template.Tags,
            "Tags",
            (fun (s) -> 
                template.Tags <- ResizeArray s
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.OntologyAnnotationsInput(
            Array.ofSeq template.EndpointRepositories,
            "Endpoint Repositories",
            (fun (s) -> 
                template.EndpointRepositories <- ResizeArray s
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
        FormComponents.PersonsInput(
            Array.ofSeq template.Authors,
            "Authors",
            (fun (s) -> 
                template.Authors <-ResizeArray s
                template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
        )
    ]