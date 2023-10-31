module TemplateMetadata.Core

open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Elmish

open Shared

open ExcelColors
open Model
open Messages

open TemplateMetadata

open TemplateTypes

let update (msg:Msg) (currentModel: Messages.Model) : Messages.Model * Cmd<Messages.Msg> =
    match msg with
    | CreateTemplateMetadataWorksheet metadataFieldsOpt ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.TemplateMetadataFunctions.createTemplateMetadataWorksheet
                (metadataFieldsOpt)
                (curry GenericLog Cmd.none >> DevMsg)
                (curry GenericError Cmd.none >> DevMsg)
        currentModel, cmd

open Messages
open Feliz
open Feliz.Bulma

let defaultMessageEle (model:Model) dispatch =
    
    mainFunctionContainer [
        Bulma.field.div [
            Bulma.help [
                str "Use this function to create a prewritten template metadata worksheet."
            ]
        ]
        Bulma.field.div [
            Bulma.button.a [
                prop.onClick(fun e -> CreateTemplateMetadataWorksheet Metadata.root |> TemplateMetadataMsg |> dispatch)
                Bulma.button.isFullWidth
                Bulma.color.isInfo
                prop.text "Create metadata"
            ]
        ]
    ]

let newNameMainElement (model:Messages.Model) dispatch =
    Bulma.content [

        Bulma.label "Template Metadata"

        Bulma.label "Create template metadata worksheet"

        defaultMessageEle model dispatch
    ]