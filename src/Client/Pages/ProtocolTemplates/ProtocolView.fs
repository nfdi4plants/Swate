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
                        let files : FileList = ev.target?files

                        let fileNames =
                            [ for i=0 to (files.length - 1) do yield files.item i ]
                            |> List.map (fun f -> f.slice() )

                        let reader = Browser.Dom.FileReader.Create()

                        reader.onload <- fun evt ->
                            UpdateUploadFile evt.target?result |> ProtocolMsg |> dispatch
                                   
                        reader.onerror <- fun evt ->
                            curry GenericLog Cmd.none ("Error", evt.Value) |> DevMsg |> dispatch

                        reader.readAsText(fileNames |> List.head)

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
        let hasData = model.ProtocolState.UploadedFile <> ""
        Columns.columns [Columns.IsMobile] [
            Column.column [] [
                fileUploadButton model dispatch
            ]
            if hasData then
                Column.column [Column.Width(Screen.All, Column.IsNarrow)] [
                    Button.a [
                        Button.OnClick (fun e -> UpdateUploadFile "" |> ProtocolMsg |> dispatch)
                        Button.Color IsDanger
                    ] [
                        Fa.i [Fa.Solid.Times] []
                    ]
                ]
        ]

    let dropdownItem (exportType:JsonExportType) (model:Model) msg (isActive:bool) =
        Dropdown.Item.a [
            Dropdown.Item.Props [
                TabIndex 0
                OnClick (fun e ->
                    e.stopPropagation()
                    exportType |> msg
                )
                OnKeyDown (fun k -> if (int k.which) = 13 then exportType |> msg)
                Style [if isActive then BackgroundColor model.SiteStyleState.ColorMode.ControlForeground]
            ]
    
        ] [
            Text.span [
                CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
                Props [
                    Tooltip.dataTooltip (exportType.toExplanation)
                    Style [FontSize "1.1rem"; PaddingRight "10px"; TextAlign TextAlignOptions.Center; Color NFDIColors.Yellow.Darker20]
                ]
            ] [
                Fa.i [Fa.Solid.InfoCircle] []
            ]
    
            Text.span [] [str (exportType.ToString())]
        ]
    
    let parseJsonToTableEle (model:Model) (dispatch:Messages.Msg -> unit) =
        let hasData = model.ProtocolState.UploadedFile <> ""
        Field.div [Field.HasAddons] [
            Control.div [] [
                Dropdown.dropdown [
                    Dropdown.IsActive model.ProtocolState.ShowJsonTypeDropdown
                ] [
                    Dropdown.trigger [] [
                        Button.a [
                            Button.OnClick (fun e -> e.stopPropagation(); UpdateShowJsonTypeDropdown (not model.ProtocolState.ShowJsonTypeDropdown) |> ProtocolMsg |> dispatch )
                        ] [
                            span [Style [MarginRight "5px"]] [str (model.ProtocolState.JsonExportType.ToString())]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [] [
                        Dropdown.content [] [
                            let msg = (UpdateJsonExportType >> ProtocolMsg >> dispatch)
                            dropdownItem JsonExportType.Assay model msg (model.ProtocolState.JsonExportType = JsonExportType.Assay)
                            dropdownItem JsonExportType.Table model msg (model.ProtocolState.JsonExportType = JsonExportType.Table)
                            dropdownItem JsonExportType.ProcessSeq model msg (model.ProtocolState.JsonExportType = JsonExportType.ProcessSeq)
                        ]
                    ]
                ]
            ]
            Control.div [Control.IsExpanded] [
                Button.a [
                    if hasData then
                        Button.IsActive true
                    else
                        Button.Color Color.IsDanger
                        Button.Props [Disabled true]
                    Button.Color IsInfo
                    Button.IsFullWidth
                    Button.OnClick(fun e ->
                        ProtocolMsg ParseUploadedFileRequest |> dispatch
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
                    //b [] [
                    //    str "Upload a "
                    //    a [Href "https://github.com/nfdi4plants/Swate/wiki/Insert-via-Process.json"; Target "_Blank"] [ str "process.json" ]
                    //    str " file."
                    //]
                    b [] [str "Insert tables via ISA-JSON files."]
                    str " You can use Swate.Experts to create these files from existing Swate tables. "
                    span [Style [Color NFDIColors.Red.Base]] [str "Only missing building blocks will be added."]
                ]
            ]

            Field.div [] [
                fileUploadEle model dispatch
            ]

            parseJsonToTableEle model dispatch
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
                                /// Use x.Value |> Some to force an error if isNone. Otherwise AddAnnotationBlocks would just ignore it and it might be overlooked.
                                //let validation =
                                //    model.ProtocolInsertState.ValidationXml.Value |> Some
                                ProtocolIncreaseTimesUsed p.Id |> ProtocolMsg |> dispatch
                                AddAnnotationBlocks (Array.ofList p.TemplateBuildingBlocks) |> OfficeInteropMsg |> dispatch
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
    Content.content [ Content.Props [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
        OnClick (fun e ->
            if model.ProtocolState.ShowJsonTypeDropdown then
                UpdateShowJsonTypeDropdown false |> ProtocolMsg |> dispatch
        )
        Style [MinHeight "100vh"]
    ]] [
        
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Templates"]

        /// Box 1
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Add template from database."]

        TemplateFromDB.showDatabaseProtocolTemplate model dispatch

        /// Box 2
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Add template(s) from file."]

        TemplateFromJsonFile.protocolInsertElement model dispatch

        //div [] [
        //    str (
        //        let dataStr = model.ProtocolState.UploadData
        //        if dataStr = "" then "no upload data found" else sprintf "%A" model.ProtocolState.UploadData
        //    )
        //]
    ]