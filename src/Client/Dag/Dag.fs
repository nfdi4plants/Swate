[<RequireQualifiedAccess>]
module Dag

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

open Dag

let update (msg:Msg) (currentModel: Messages.Model) : Messages.Model * Cmd<Messages.Msg> =
    match msg with
    | UpdateLoading loading ->
        let nextModel = {
            currentModel.DagModel with
                Loading = loading
        }
        currentModel.updateByDagModel nextModel, Cmd.none
    | ParseTablesOfficeInteropRequest ->
        let nextModel = {
            currentModel.DagModel with
                Loading = true
        }
        let cmd =
            Cmd.OfPromise.either
                OfficeInterop.getBuildingBlocksAndSheets
                ()
                (ParseTablesDagServerRequest >> DagMsg)
                (curry GenericError (Dag.UpdateLoading false |> DagMsg |> Cmd.ofMsg) >> DevMsg)
        currentModel.updateByDagModel nextModel, cmd
    | ParseTablesDagServerRequest (worksheetBuildingBlocksTuple) ->
        let cmd =
            Cmd.OfAsync.either
                Api.dagApi.parseAnnotationTablesToDagHtml
                worksheetBuildingBlocksTuple
                (ParseTablesDagServerResponse >> DagMsg)
                (curry GenericError (Dag.UpdateLoading false |> DagMsg |> Cmd.ofMsg) >> DevMsg)

        currentModel, cmd
    //
    | ParseTablesDagServerResponse dagHtml ->
        let nextModel = {
            currentModel.DagModel with
                Loading = false
                DagHtml = Some dagHtml
        }
        currentModel.updateByDagModel nextModel, Cmd.none

open Messages

let defaultMessageEle (model:Model) dispatch =
    mainFunctionContainer [
        Button.a [
            Button.OnClick(fun e -> ())
        ][
            str "Click me!"
        ]
    ]

let mainElement (model:Messages.Model) dispatch =
    Content.content [ Content.Props [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ]] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "DAG"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Display acyclic graph"]

        defaultMessageEle model dispatch
    ]