module MainComponents.Metadata.Assay

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl.ISA
open Shared

let Main(assay: ArcAssay, model: Messages.Model, dispatch: Msg -> unit) = 
    Bulma.section [
        FormComponents.TextInput (
            assay.Identifier,
            "Identifier", 
            fun s -> 
                let nextAssay = IdentifierSetters.setAssayIdentifier s assay
                nextAssay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
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
        FormComponents.PersonInput(
            assay.Performers,
            "Performers",
            fun persons ->
                assay.Performers <- persons
                assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch 
        )
    ]