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

let createFileList (model:Model) (dispatch: Msg -> unit) =
    if model.FilePickerState.FileNames.Length > 0 then
        model.FilePickerState.FileNames
        |> List.map (fun fileName ->
            tr [
                colorControl model.SiteStyleState.ColorMode
            ] [
                td [
                ] [
                    Delete.delete [
                        Delete.OnClick (fun _ -> fileName |> RemoveFileFromFileList |> FilePicker |> dispatch)
                    ][]
                ]
                td [] [
                    b [] [str fileName]
                ]

            ])
    else
        [
            tr [] [
                td [] [str "No Files selected."]
            ]
        ]

let filePickerComponent (model:Model) (dispatch:Msg -> unit) =
    let inputId = "filePicker_OnFilePickerMainFunc"
    Content.content [ Content.Props [colorControl model.SiteStyleState.ColorMode ]] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "File Picker"]
        File.file [] [
            File.label [] [
                File.input [
                    Props [
                        Id inputId
                        Multiple true
                        OnChange (fun ev ->

                            let files : FileList = ev.target?files
                            
                            let fileNames =
                                [ for i=0 to (files.length - 1) do yield files.item i ]
                                |> List.map (fun f -> f.name)

                            fileNames |> NewFilesLoaded |> FilePicker |> dispatch

                            let picker = Browser.Dom.document.getElementById(inputId)
                            // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                            picker?value <- null
                            )
                    ]
                ]
                File.cta [] [
                    File.icon [] [
                        Fa.i [
                            Fa.Solid.Upload
                        ] []
                    ]
                    File.name [Props [Style [BorderRight "none"]]] [
                        str "Chose one or multiple files"
                    ]
                ]
            ]
        ]
        Table.table [Table.IsFullWidth] [
            tbody [] (createFileList model dispatch)
        ]
        Button.button [
            Button.IsFullWidth
            if model.FilePickerState.FileNames |> List.isEmpty then
                yield! [
                    Button.Disabled true
                    Button.IsActive false
                    Button.Color Color.IsDanger
                ]
            else
                Button.Color Color.IsSuccess
            Button.OnClick (fun e ->
                (fun tableName -> InsertFileNames (tableName, model.FilePickerState.FileNames)) |> PipeActiveAnnotationTable |> ExcelInterop |> dispatch 
            )

        ][
            str "Insert File Names"
        ]
        
    ]