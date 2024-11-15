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
open Feliz.DaisyUI
open ARCtrl

type Templates =

    static member Main (model:Model, dispatch) =
        SidebarComponents.SidebarLayout.Container [
            SidebarComponents.SidebarLayout.Header "Templates"

            // Box 1
            SidebarComponents.SidebarLayout.Description "Add template from database."

            TemplateFromDB.Main(model, dispatch)

            // Box 2
            SidebarComponents.SidebarLayout.Description "Add template(s) from file."

            TemplateFromFile.Main(model, dispatch)
        ]