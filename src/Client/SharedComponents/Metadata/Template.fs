module Components.Metadata.Template

open Feliz
open Feliz.DaisyUI
open ARCtrl
open Components
open Components.Forms

[<ReactComponent>]
let Main(template: Template, setTemplate: Template -> unit) =
    Generic.Section [
        Generic.BoxedField(
            "Template Metadata",
            content = [
                FormComponents.GUIDInput (
                    template.Id,
                    (fun guid ->
                        template.Id <- guid
                        setTemplate template
                    ),
                    "Identifier"
                )
                FormComponents.TextInput (
                    template.Name,
                    (fun (s:string) ->
                        template.Name <- s
                        setTemplate template),
                    "Name"
                )
                FormComponents.TextInput (
                    template.Description,
                    (fun (s:string) ->
                        template.Description <- s
                        //template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setTemplate template),
                    "Description",
                    isarea=true
                )
                FormComponents.TextInput (
                    template.Organisation.ToString(),
                    (fun (s:string) ->
                        template.Organisation <- Organisation.ofString(s)
                        setTemplate template),
                    "Organisation"
                )
                FormComponents.TextInput(
                    template.Version,
                    (fun (s:string) ->
                        template.Version <- s
                        //template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setTemplate template),
                    "Version"
                )
                FormComponents.DateTimeInput(
                    template.LastUpdated,
                    (fun dt ->
                        template.LastUpdated <- dt
                        //template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                        setTemplate template),
                    "Last Updated"
                )
                FormComponents.OntologyAnnotationsInput(
                    template.Tags,
                    (fun (s) ->
                        template.Tags <- ResizeArray s
                        //template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                        setTemplate template),
                    "Tags"
                )
                FormComponents.OntologyAnnotationsInput(
                    template.EndpointRepositories,
                    (fun (s) ->
                        template.EndpointRepositories <- ResizeArray s
                        //template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                        setTemplate template),
                    "Endpoint Repositories"
                )
                FormComponents.PersonsInput(
                    template.Authors,
                    (fun (s) ->
                        template.Authors <-ResizeArray s
                        //template |> ArcFiles.Template |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch)
                        setTemplate template),
                    label="Authors"
                )
            ]
        )
    ]