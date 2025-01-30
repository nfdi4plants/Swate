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

open JsonImport

type Templates =

    [<ReactComponent>]
    static member Main (model: Model, dispatch) =
        let isProtocolSearch, setProtocolSearch = React.useState(false)
        let importTypeStateData = React.useState(SelectiveImportModalState.init([]))
        if model.ProtocolState.TemplatesSelected.Length > 0 && (fst importTypeStateData).SelectedColumns.Length = 0 then
            let columns =
                model.ProtocolState.TemplatesSelected
                |> List.map (fun template -> Array.init template.Table.Columns.Length (fun _ -> true))
                |> Array.ofList
            {fst importTypeStateData with SelectedColumns = columns} |> snd importTypeStateData
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

            if isProtocolSearch then
                Protocol.SearchContainer.Main model setProtocolSearch importTypeStateData dispatch
            else
                SidebarComponents.SidebarLayout.LogicContainer [
                    Modals.SelectiveTemplateFromDB.Main(model, false, setProtocolSearch, importTypeStateData, dispatch)
                ]

            // Box 2
            SidebarComponents.SidebarLayout.Description (Html.p [
                Html.b "Import JSON files."
                Html.text " You can use \"Json Export\" to create these files from existing Swate tables. "
            ])

            TemplateFromFile.Main(model, dispatch)
        ]