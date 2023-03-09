module Protocol.Core

open System

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
//open Fable.Core.JS
open Fable.Core.JsInterop

//open ISADotNet

open Model
open Messages
open Browser.Types
open Fulma.Extensions.Wikiki

open Shared

open OfficeInterop
open Protocol

open Messages
open Elmish

module TemplateFromJsonFile =

    let fileUploadButton (model:Model) dispatch =
        let uploadId = "UploadFiles_ElementId"
        Label.label [Label.Props [Style [FontWeight "normal"]]] [
            Input.input [
                Input.Props [
                    Id uploadId
                    Type "file"; Style [Display DisplayOptions.None]
                    OnChange (fun ev ->
                        let fileList : FileList = ev.target?files

                        if fileList.length > 0 then
                            let file = fileList.item 0 |> fun f -> f.slice()

                            let reader = Browser.Dom.FileReader.Create()

                            reader.onload <- fun evt ->
                                let (r: byte []) = evt.target?result
                                r |> ParseUploadedFileRequest |> ProtocolMsg |> dispatch
                                   
                            reader.onerror <- fun evt ->
                                curry GenericLog Cmd.none ("Error", evt.Value) |> DevMsg |> dispatch

                            reader.readAsArrayBuffer(file)
                        else
                            ()
                        let picker = Browser.Dom.document.getElementById(uploadId)
                        // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                        picker?value <- null
                    )
                ]
            ]
            Button.a [
                Button.Color Color.IsInfo; Button.IsFullWidth
                Button.OnClick(fun e ->
                    e.preventDefault()
                    let getUploadElement = Browser.Dom.document.getElementById uploadId
                    getUploadElement.click()
                    ()
                )
            ] [
                str "Upload protocol"
            ]
        ]

    let fileUploadEle (model:Model) dispatch =
        let hasData = model.ProtocolState.UploadedFileParsed <> Array.empty
        Columns.columns [Columns.IsMobile] [
            Column.column [] [
                fileUploadButton model dispatch
            ]
            if hasData then
                Column.column [Column.Width(Screen.All, Column.IsNarrow)] [
                    Button.a [
                        Button.OnClick (fun e -> RemoveUploadedFileParsed |> ProtocolMsg |> dispatch)
                        Button.Color IsDanger
                    ] [
                        Fa.i [Fa.Solid.Times] []
                    ]
                ]
        ]
    
    let importToTableEle (model:Model) (dispatch:Messages.Msg -> unit) =
        let hasData = model.ProtocolState.UploadedFileParsed <> Array.empty
        Field.div [Field.HasAddons] [
            Control.div [Control.IsExpanded] [
                Button.a [
                    if hasData then
                        Button.IsActive true
                    else
                        Button.Color Color.IsDanger
                        Button.Props [Disabled true]
                    Button.Color IsInfo
                    Button.IsFullWidth
                    Button.OnClick(fun _ ->
                        SpreadsheetInterface.ImportFile model.ProtocolState.UploadedFileParsed |> InterfaceMsg |> dispatch
                    )
                ] [
                    str "Insert json"
                ]
            ]
        ]

    let protocolInsertElement (model:Model) dispatch =
        mainFunctionContainer [
            Field.div [] [
                Help.help [] [
                    b [] [str "Insert tables via ISA-JSON files."]
                    str " You can use Swate.Experts to create these files from existing Swate tables. "
                    span [Style [Color NFDIColors.Red.Base]] [str "Only missing building blocks will be added."]
                ]
            ]

            Field.div [] [
                fileUploadEle model dispatch
            ]

            importToTableEle model dispatch
        ]

module TemplateFromDB = 

    let toProtocolSearchElement (model:Model) dispatch =
        Button.span [
            Button.OnClick(fun e -> UpdatePageState (Some Routing.Route.ProtocolSearch) |> dispatch)
            Button.Color IsInfo
            Button.IsFullWidth
            Button.Props [Style [Margin "1rem 0"]]
        ] [str "Browse database"]

    let addFromDBToTableButton (model:Messages.Model) dispatch =
        Columns.columns [Columns.IsMobile] [
            Column.column [] [
                Field.div [] [
                    Control.div [] [
                        Button.a [
                            if model.ProtocolState.ProtocolSelected.IsSome (*&& model.ProtocolInsertState.ValidationXml.IsSome*) then
                                Button.IsActive true
                            else
                                Button.Color Color.IsDanger
                                Button.Props [Disabled true]
                            Button.IsFullWidth
                            Button.Color IsSuccess
                            Button.OnClick (fun e ->
                                let p = model.ProtocolState.ProtocolSelected.Value
                                // Use x.Value |> Some to force an error if isNone. Otherwise AddAnnotationBlocks would just ignore it and it might be overlooked.
                                //let validation =
                                //    model.ProtocolInsertState.ValidationXml.Value |> Some
                                ProtocolIncreaseTimesUsed p.Id |> ProtocolMsg |> dispatch
                                SpreadsheetInterface.AddAnnotationBlocks (Array.ofList p.TemplateBuildingBlocks) |> InterfaceMsg |> dispatch
                            )
                        ] [
                            str "Add template"
                        ]
                    ]
                ]
            ]
            if model.ProtocolState.ProtocolSelected.IsSome then
                Column.column [Column.Width(Screen.All, Column.IsNarrow)] [
                    Button.a [
                        Button.OnClick (fun e -> RemoveSelectedProtocol |> ProtocolMsg |> dispatch)
                        Button.Color IsDanger
                    ] [
                        Fa.i [Fa.Solid.Times] []
                    ]
                ]
        ]

    let displaySelectedProtocolEle (model:Model) dispatch =
        [
            div [Style [OverflowX OverflowOptions.Auto; MarginBottom "1rem"]] [
                Table.table [
                    Table.IsFullWidth;
                    Table.IsBordered
                    Table.Props [Style [Color model.SiteStyleState.ColorMode.Text; BackgroundColor model.SiteStyleState.ColorMode.BodyBackground]]
                ] [
                    thead [] [
                        tr [] [
                            th [Style [Color model.SiteStyleState.ColorMode.Text]] [str "Column"]
                            th [Style [Color model.SiteStyleState.ColorMode.Text]] [str "Column TAN"]
                            th [Style [Color model.SiteStyleState.ColorMode.Text]] [str "Unit"]
                            th [Style [Color model.SiteStyleState.ColorMode.Text]] [str "Unit TAN"]
                        ]
                    ]
                    tbody [] [
                        for insertBB in model.ProtocolState.ProtocolSelected.Value.TemplateBuildingBlocks do
                            yield
                                tr [] [
                                    td [] [str (insertBB.ColumnHeader.toAnnotationTableHeader())]
                                    td [] [str (if insertBB.HasExistingTerm then insertBB.ColumnTerm.Value.TermAccession else "-")]
                                    td [] [str (if insertBB.HasUnit then insertBB.UnitTerm.Value.Name else "-")]
                                    td [] [str (if insertBB.HasUnit then insertBB.UnitTerm.Value.TermAccession else "-")]
                                ]
                    ]
                ]
            ]
            addFromDBToTableButton model dispatch
        ]
    

    let showDatabaseProtocolTemplate (model:Messages.Model) dispatch =
        mainFunctionContainer [
            Field.div [] [
                Help.help [] [
                    b [] [str "Search the database for templates."]
                    str " The building blocks from these templates can be inserted into the Swate table. "
                    span [Style [Color NFDIColors.Red.Base]] [str "Only missing building blocks will be added."]
                ]
            ]
            Field.div [] [
                toProtocolSearchElement model dispatch
            ]

            Field.div [] [
                addFromDBToTableButton model dispatch
            ]
            if model.ProtocolState.ProtocolSelected.IsSome then
                Field.div [] [
                    yield! displaySelectedProtocolEle model dispatch
                ]
        ]


let fileUploadViewComponent (model:Messages.Model) dispatch =
    div [ 
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Templates"]

        // Box 1
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Add template from database."]

        TemplateFromDB.showDatabaseProtocolTemplate model dispatch

        // Box 2
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Add template(s) from file."]

        TemplateFromJsonFile.protocolInsertElement model dispatch
    ]