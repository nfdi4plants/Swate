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

type Templates =

    static member Main (model:Model, dispatch) =
        div [ 
            OnSubmit (fun e -> e.preventDefault())
            // https://keycode.info/
            OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
        ] [
        
            pageHeader "Templates"

            // Box 1
            Bulma.label "Add template from database."

            TemplateFromDB.Main(model, dispatch)

            // Box 2
            Bulma.label "Add template(s) from file."

            TemplateFromFile.Main(model, dispatch)
        ]