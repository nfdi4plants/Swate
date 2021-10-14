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

module ParseTemplateMetadataSchema =
    open Fable.SimpleJson
    
    let parseJsonSchema (str:string) =
        SimpleJson.tryParse str

    let Description = "description"
    let JType       = "type"
    let Properties  = "properties"
    let Items       = "items"

    let JArrayStr   = "array"
    let JObjectStr  = "object"

    let getJStringValue (JString v) = v

    let rec parseJsonToMetadataFields keyName (json:Json) =
        let parseObjectSchemaChildren (JObject json:Json) =
            let l = Map.toList json
            l
            |> List.map (fun (k,v) ->
                parseJsonToMetadataFields k v
            )
        match json with
        | JObject dict ->
            //printfn $"new {keyName}"
            let description = dict |> Map.tryFind Description |> Option.bind (getJStringValue >> Some)
            let jsonType    =
                dict
                |> Map.tryFind JType
                /// Cannot be found in case of "anyOf". 
                |> Option.defaultValue (JString "string")
                |> getJStringValue
            let properties  = dict |> Map.tryFind Properties
            let items       = dict |> Map.tryFind Items
            let list        =
                if jsonType = JArrayStr then
                    parseJsonToMetadataFields keyName items.Value
                    |> Some
                else
                    None
            let children =
                if jsonType = JObjectStr then
                    //printfn $"parse children as object {keyName}"
                    parseObjectSchemaChildren properties.Value 
                else []
            //printfn $"solved: {keyName}"
            MetadataField.create
                keyName
                list
                description
                children
        | bottomEle ->
            MetadataField.create
                keyName
                None
                None
                []

let update (msg:Msg) (currentModel: Messages.Model) : Messages.Model * Cmd<Messages.Msg> =
    match msg with
    | CreateTemplateMetadataWorksheet metadataFieldsOpt ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.TemplateMetadataFunctions.createTemplateMetadataWorksheet
                (metadataFieldsOpt)
                (curry GenericLog Cmd.none >> Dev)
                (curry GenericError Cmd.none >> Dev)
        currentModel, cmd
    | GetTemplateMetadataJsonSchemaRequest ->
        let cmd =
            Cmd.OfAsync.either
                Api.expertAPIv1.getTemplateMetadataJsonSchema
                ()
                (GetTemplateMetadataJsonSchemaResponse >> TemplateMetadataMsg)
                (curry GenericError Cmd.none >> Dev)
        currentModel, cmd
    | GetTemplateMetadataJsonSchemaResponse json ->
        let tryJson =
            ParseTemplateMetadataSchema.parseJsonSchema json
            |> Option.bind (ParseTemplateMetadataSchema.parseJsonToMetadataFields "Root" >> Some)
        let nextModel = {
            currentModel.TemplateMetadataModel with
                MetadataFields = tryJson
        }
        let cmd = CreateTemplateMetadataWorksheet tryJson |> TemplateMetadataMsg |> Cmd.ofMsg
        currentModel.updateByTemplateMetadataModel nextModel, cmd

open Messages

let defaultMessageEle (model:Model) dispatch =
    
    mainFunctionContainer [
        Button.a [
            Button.OnClick(fun e -> GetTemplateMetadataJsonSchemaRequest |> TemplateMetadataMsg |> dispatch)
        ][
            str "Click me!"
        ]

        if model.TemplateMetadataModel.MetadataFields.IsSome then 
            Text.div [][
                str (model.TemplateMetadataModel.MetadataFields.Value.ToString())
            ]
        else
            str "not parsable"

    ]

let newNameMainElement (model:Messages.Model) dispatch =
    form [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "JSON Exporter"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Function 1"]

        defaultMessageEle model dispatch
    ]