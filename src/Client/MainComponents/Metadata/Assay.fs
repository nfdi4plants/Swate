module MainComponents.Metadata.Assay

open Feliz
open Feliz.Bulma
open Messages
open ARCtrl
open Shared

[<ReactComponent>]
let Main(assay: ArcAssay, model: Messages.Model, dispatch: Msg -> unit) = 
    Bulma.section [
        FormComponents.TextInput (
            assay.Identifier,
            "Identifier", 
            (fun s -> 
                let nextAssay = IdentifierSetters.setAssayIdentifier s assay
                nextAssay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            fullwidth=true
        )
        FormComponents.OntologyAnnotationInput(
            assay.MeasurementType |> Option.defaultValue OntologyAnnotation.empty,
            (fun oa -> 
                let oa = if oa = OntologyAnnotation.empty then None else Some oa
                assay.MeasurementType <- oa
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            "Measurement Type"
        )
        FormComponents.OntologyAnnotationInput(
            assay.TechnologyType |> Option.defaultValue OntologyAnnotation.empty,
            (fun oa -> 
                let oa = if oa = OntologyAnnotation.empty then None else Some oa
                assay.TechnologyType <- oa
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            "Technology Type"
        )
        FormComponents.OntologyAnnotationInput(
            assay.TechnologyPlatform |> Option.defaultValue OntologyAnnotation.empty,
            (fun oa -> 
                let oa = if oa = OntologyAnnotation.empty then None else Some oa
                assay.TechnologyPlatform <- oa
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
            "Technology Platform"
        )
        FormComponents.PersonsInput(
            Array.ofSeq assay.Performers,
            "Performers",
            fun persons ->
                assay.Performers <- ResizeArray persons
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch 
        )
        FormComponents.CommentsInput(
            Array.ofSeq assay.Comments,
            "Comments",
            fun comments ->
                assay.Comments <- ResizeArray comments
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch 
        )
    ]