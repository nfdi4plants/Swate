module JSONExporter

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

open Shared.OfficeInteropTypes
open Validation
open Messages
open JSONExporter

let update (msg:Msg) (currentModel: Messages.Model) : Messages.Model * Cmd<Messages.Msg> =
    match msg with
    // Style
    | UpdateLoading isLoading ->
        let nextModel = {
            currentModel.JSONExporterModel with
                Loading = isLoading
        }
        currentModel.updateByJSONExporterModel nextModel, Cmd.none
    //
    | ParseTableOfficeInteropRequest ->
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getTableRepresentation
                ()
                (snd >> ParseTableServerRequest >> JSONExporterMsg)
                (curry GenericError (UpdateLoading false |> JSONExporterMsg |> Cmd.ofMsg) >> Dev)
        currentModel, cmd
    | ParseTableServerRequest buildingBlocks ->
        currentModel, Cmd.none
    | ParseTableServerResponse parsedJson ->
        currentModel, Cmd.none

open Messages

open Browser.Dom
open Fable.Core.JS
open Fable.Core.JsInterop

let download(filename, text) =
  let element = document.createElement("a");
  element.setAttribute("href", "data:text/plain;charset=utf-8," +  encodeURIComponent(text));
  element.setAttribute("download", filename);

  element?style?display <- "None";
  let _ = document.body.appendChild(element);

  element.click();

  document.body.removeChild(element);

let defaultMessageEle (model:Model) dispatch =
    mainFunctionContainer [
        Button.a [
            Button.OnClick(fun e ->
                ()
            )
        ][
            str "Click me!"
        ]
    ]

let jsonExporterMainElement (model:Messages.Model) (dispatch: Messages.Msg -> unit) =
    form [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "JSON Exporter"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Function 1"]

        defaultMessageEle model dispatch

    ]