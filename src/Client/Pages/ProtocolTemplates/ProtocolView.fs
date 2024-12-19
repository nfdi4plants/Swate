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

            SidebarComponents.SidebarLayout.Description (Html.p [
                Html.b "Search the database for templates."
                Html.text " The building blocks from these templates can be inserted into the Swate table. "
                Html.span [
                    prop.className "text-error"
                    prop.text "Only missing building blocks will be added."
                ]
            ])
            // Box 1
            SidebarComponents.SidebarLayout.Description "Add template from database."

            SidebarComponents.SidebarLayout.LogicContainer [
                Modals.SelectiveTemplateFromDB.Main(model, dispatch)
            ]

            // Box 2
            SidebarComponents.SidebarLayout.Description (Html.p [
                Html.b "Import JSON files."
                Html.text " You can use \"Json Export\" to create these files from existing Swate tables. "
            ])

            TemplateFromFile.Main(model, dispatch)
        ]