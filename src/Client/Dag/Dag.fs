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

open Dag //!

let update (msg:Msg) (currentModel: Messages.Model) : Messages.Model * Cmd<Messages.Msg> =
    match msg with
    | DefaultMsg ->
        Fable.Core.JS.console.log "Default Msg"
        currentModel, Cmd.none

open Messages

let defaultMessageEle (model:Model) dispatch =
    mainFunctionContainer [
        Button.a [
            Button.OnClick(fun e -> DefaultMsg |> DagMsg |> dispatch)
        ][
            str "Click me!"
        ]
    ]

let mainElement (model:Messages.Model) dispatch =
    form [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "DAG"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Display acyclic graph"]

        defaultMessageEle model dispatch
    ]