[<RequireQualifiedAccess>]
module Dag.Core

open Fable.React
open Fable.React.Props
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
open Feliz
open Feliz.Bulma

let defaultMessageEle (model:Model) dispatch =
    mainFunctionContainer [
        Bulma.field.div [
            Bulma.help [
                str "A "
                b [] [str "D"]
                str "irected "
                b [] [str "A"]
                str "cyclic "
                b [] [str "G"]
                str "raph represents the chain of applied protocols to samples. Within are all intermediate products as well as protocols displayed."
            ]
            Bulma.help [
                str "This only works if your input and output columns have values."
            ]
        ]
        
        Bulma.field.div [
            Bulma.button.a [
                Bulma.button.isFullWidth
                Bulma.color.isInfo
                prop.onClick(fun _ -> SpreadsheetInterface.ParseTablesToDag |> InterfaceMsg |> dispatch)
                prop.text "Display dag"
            ]
        ]

        if model.DagModel.DagHtml.IsSome then
            Bulma.field.div [
                iframe [SrcDoc model.DagModel.DagHtml.Value; Style [Width "100%"; Height "400px"] ] []
            ]
    ]

let mainElement (model:Messages.Model) dispatch =
    Bulma.content [
        prop.onSubmit    (fun e -> e.preventDefault())
        prop.onKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
        prop.children [
            Bulma.label "DAG"

            Bulma.label "Display directed acyclic graph"

            defaultMessageEle model dispatch
        ]
    ]