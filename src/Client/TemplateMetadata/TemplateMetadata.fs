module TemplateMetadata

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Fable.Core.JsInterop
open Elmish

open Shared

open ExcelColors
open Model
open Messages

open TemplateMetadata

open ProtocolTemplateTypes

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

let defaultMessageEle (model:Model) dispatch =
    
    mainFunctionContainer [
        Field.div [][
            Help.help [][
                str "Use this function to create a prewritten template metadata worksheet."
            ]
        ]
        Field.div [][
            Button.a [
                Button.OnClick(fun e -> CreateTemplateMetadataWorksheet TemplateMetadata.root |> TemplateMetadataMsg |> dispatch)
                Button.IsFullWidth
                Button.Color IsInfo
            ][
                str "Create metadata"
            ]
        ]
    ]

let newNameMainElement (model:Messages.Model) dispatch =
    Content.content [] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Template Metadata"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Create template metadata worksheet"]

        defaultMessageEle model dispatch
    ]