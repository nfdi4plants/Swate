module ActivityLogView

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
open Fable.Core.JS
open Fable.Core.JsInterop

open Model
open Messages
open Browser.Types

//TO-DO: Save log as tab seperated file

let activityLogComponent (model:Model) dispatch =
    div [][
        Button.button [
            Button.Color Color.IsLink
            Button.IsFullWidth
            Button.OnClick (fun e -> UpdatePageState (Some Routing.Route.TermSearch) |> dispatch)
            Button.Props [Style [MarginBottom "1rem"]]
        ] [
            str "Back to Term Search"
        ]
        Help.help [][str "This page is used for development/debugging."]
        Button.button [
            Button.Color Color.IsInfo
            Button.IsFullWidth
            Button.OnClick (fun e -> TryExcel |> ExcelInterop |> dispatch )
            Button.Props [Style [MarginBottom "1rem"]]
        ] [
            str "Try Excel"
        ]
        //Button.button [
        //    Button.Color Color.IsInfo
        //    Button.IsFullWidth
        //    Button.OnClick (fun e -> TryExcel2 |> ExcelInterop |> dispatch )
        //    Button.Props [Style [MarginBottom "1rem"]]
        //] [
        //    str "Try Excel2"
        //]
        Label.label [][str "Dangerzone"]
        Container.container [
            Container.Props [Style [
                Padding "1rem"
                Border (sprintf "2.5px solid %s" NFDIColors.Red.Base)
                BorderRadius "10px"
            ]]
        ][
            Button.a [
                Button.Color Color.IsWarning
                Button.IsFullWidth
                Button.OnClick (fun e -> GetSwateCustomXml |> ExcelInterop |> dispatch )
                Button.Props [Style [MarginBottom "1rem"]; Title "Show record type data of Swate validation Xml"]
            ] [
                span [] [str "Show Swate Validation Xml!"]
            ]
            Button.a [
                Button.Color Color.IsDanger
                Button.IsFullWidth
                Button.OnClick (fun e -> DeleteAllCustomXml |> ExcelInterop |> dispatch )
                Button.Props [Style []; Title "Be sure you know what you do. This cannot be undone!"]
            ] [
                Icon.icon [ ] [
                    Fa.i [Fa.Solid.ExclamationTriangle][]
                ]
                span [] [str "Delete All Custom Xml!"]
                Icon.icon [ ] [
                    Fa.i [Fa.Solid.ExclamationTriangle][]
                ]
            ]
        ]
        Table.table [
            Table.IsFullWidth
            Table.Props [ExcelColors.colorBackground model.SiteStyleState.ColorMode]
        ] [
            tbody [] (
                model.DevState.Log
                |> List.map LogItem.toTableRow
            )
        ]
    ]

