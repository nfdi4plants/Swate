module FilePickerView

open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Thoth.Json
open Thoth.Elmish
open ExcelColors
open Api
open Model
open Messages
open Update
open Shared
open Browser.Types

let filePickerComponent (model:Model) (dispatch:Msg -> unit) =
    div [] [
        File.input [
            Props [
                OnLoad (fun ev ->
                    let files : FileList = ev.target?files
                    let fileNames =
                        [ for i=0 to files.length do yield files.item 0 ]
                        |> List.map (fun f -> f.name)
                    ()
                    )
            ]
        ]
    ]