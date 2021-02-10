module ProtocolInsertView

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
open Fable.Core.JS
open Fable.Core.JsInterop

//open ISADotNet

open Model
open Messages
open Browser.Types
open Fulma.Extensions.Wikiki

let fileUploadViewComponent (model:Model) dispatch =
    let uploadId = "UploadFiles_ElementId"
    div [][
        Button.button [
            Button.Props [Style [MarginBottom "1rem"]]
            Button.IsFullWidth
            Button.Color IsInfo
            Button.OnClick (fun e ->
                UpdateUploadData "" |> ProtocolInsert |> dispatch
            )
        ][
            str "Remove Test Data"
        ]

        Label.label [][
            Input.input [
                Input.Props [
                    Id uploadId
                    Type "file"; Style [Display DisplayOptions.None]
                    OnChange (fun ev ->
                        let files : FileList = ev.target?files

                        let fileNames =
                            [ for i=0 to (files.length - 1) do yield files.item i ]
                            |> List.map (fun f -> f.slice() )

                        let reader = Browser.Dom.FileReader.Create()

                        reader.onload <- fun evt ->
                            UpdateUploadData evt.target?result |> ProtocolInsert |> dispatch
                                       
                        reader.onerror <- fun evt ->
                            GenericLog ("Error", evt.Value) |> Dev |> dispatch

                        reader.readAsText(fileNames |> List.head)

                        let picker = Browser.Dom.document.getElementById(uploadId)
                        // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                        picker?value <- null
                    )
                ]
            ]
            Button.a [Button.Color Color.IsPrimary; Button.IsFullWidth][
                str "Upload"
            ]
        ]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Show uploaded file data."]
        str (
            let dataStr = model.ProtocolInsertState.UploadData
            if dataStr = "" then "no upload data found" else sprintf "%A" model.ProtocolInsertState.UploadData
        )

        div [][
            Button.button [
                Button.IsFullWidth
                Button.OnClick (fun e -> ParseJsonToProcessRequest model.ProtocolInsertState.UploadData |> ProtocolInsert |> dispatch)
            ][
                str "Parse json"
            ]
            div [][
                str (
                    let dataStr = model.ProtocolInsertState.ProcessModel
                    if dataStr.IsNone then "no upload data found" else sprintf "%A" model.ProtocolInsertState.ProcessModel.Value
                )
            ]
        ]
    ]