module MainComponents.Metadata.Assay

open Feliz
open Feliz.Bulma
open Messages
open ARCtrl.ISA
open Shared

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
            "Measurement Type",
            fun oa -> 
                let oa = if oa = OntologyAnnotation.empty then None else Some oa
                assay.MeasurementType <- oa
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.OntologyAnnotationInput(
            assay.TechnologyType |> Option.defaultValue OntologyAnnotation.empty,
            "Technology Type",
            fun oa -> 
                let oa = if oa = OntologyAnnotation.empty then None else Some oa
                assay.TechnologyType <- oa
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.OntologyAnnotationInput(
            assay.TechnologyPlatform |> Option.defaultValue OntologyAnnotation.empty,
            "Technology Platform",
            fun oa -> 
                let oa = if oa = OntologyAnnotation.empty then None else Some oa
                assay.TechnologyPlatform <- oa
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.PersonsInput(
            assay.Performers,
            "Performers",
            fun persons ->
                assay.Performers <- persons
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch 
        )
        FormComponents.CommentsInput(
            assay.Comments,
            "Comments",
            fun comments ->
                assay.Comments <- comments
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch 
        )
    ]