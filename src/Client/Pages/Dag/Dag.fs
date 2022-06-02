[<RequireQualifiedAccess>]
module Dag.Core

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
                OfficeInterop.Core.getBuildingBlocksAndSheets
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
        Field.div [] [
            Help.help [] [
                str "A "
                b [] [str "D"]
                str "irected "
                b [] [str "A"]
                str "cyclic "
                b [] [str "G"]
                str "raph represents the chain of applied protocols to samples. Within are all intermediate products as well as protocols displayed."
            ]
        ]
        
        Field.div [] [
            Button.a [
                Button.IsFullWidth
                Button.Color Color.IsInfo
                Button.OnClick(fun e -> ParseTablesOfficeInteropRequest |> DagMsg |> dispatch)
            ] [
                str "Display dag"
            ]
        ]

        if model.DagModel.DagHtml.IsSome then
            Field.div [] [
                iframe [SrcDoc model.DagModel.DagHtml.Value; Style [Width "100%"; Height "400px"] ] []
            ]
    ]

let mainElement (model:Messages.Model) dispatch =
    Content.content [ Content.Props [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ]] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "DAG"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Display directed acyclic graph"]

        defaultMessageEle model dispatch
    ]