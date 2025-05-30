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

open FileImport

type Templates =

    [<ReactComponent>]
    static member Main(model: Model, dispatch) =
        SidebarComponents.SidebarLayout.Container [
            SidebarComponents.SidebarLayout.Header "Templates"

            SidebarComponents.SidebarLayout.Description(
                Html.p [
                    Html.b "Search the database for templates."
                    Html.text " The building blocks from these templates can be inserted into the Swate table. "
                    Html.span [
                        prop.className "swt:text-error"
                        prop.text "Only missing building blocks will be added."
                    ]
                ]
            )

            SidebarComponents.SidebarLayout.LogicContainer [
                if model.ProtocolState.ShowSearch then
                    Protocol.SearchContainer.Main(model, dispatch)
                else
                    Modals.SelectiveTemplateFromDB.Main(model, dispatch, false)
            ]

        ]