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

let update (msg:Msg) (currentModel: Messages.Model) : Messages.Model * Cmd<Messages.Msg> =
    match msg with
    | CreateTemplateMetadataWorksheet ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.TemplateMetadataFunctions.createTemplateMetadataWorksheet
                ()
                (curry GenericLog Cmd.none >> Dev)
                (curry GenericError Cmd.none >> Dev)
        currentModel, cmd

open Messages

let defaultMessageEle (model:Model) dispatch =
    mainFunctionContainer [
        Button.a [
            Button.OnClick(fun e -> CreateTemplateMetadataWorksheet |> TemplateMetadataMsg |> dispatch)
        ][
            str "Click me!"
        ]
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