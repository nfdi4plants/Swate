namespace Protocol

open System

open Fable
open Fable.React
open Fable.React.Props
//open Fable.Core.JS
open Fable.Core.JsInterop

//open ISADotNet

open Model
open Messages
open Browser.Types
open SpreadsheetInterface
open Messages
open Elmish

open Feliz
open Feliz.Bulma
open ARCtrl

type Templates =

    static member Main (model:Model, dispatch) =
        Html.div [
            prop.onSubmit (fun e -> e.preventDefault())
            prop.onKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
            prop.children [
                pageHeader "Templates"

                // Box 1
                Bulma.label "Add template from database."

                TemplateFromDB.Main(model, dispatch)

                // Box 2
                Bulma.label "Add template(s) from file."

                TemplateFromFile.Main(model, dispatch)
            ]
        ]