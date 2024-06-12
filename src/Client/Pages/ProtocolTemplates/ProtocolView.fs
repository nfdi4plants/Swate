module Protocol.Core

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

open Shared

open OfficeInterop
open Protocol

open Messages
open Elmish

open Feliz
open Feliz.Bulma

module TemplateFromJsonFile =

    let fileUploadButton (model:Model) dispatch =
        let uploadId = "UploadFiles_ElementId"
        Bulma.label [
            Bulma.fileInput [
                prop.id uploadId
                prop.type' "file";
                prop.style [style.display.none]
                prop.onChange (fun (ev: File list) ->
                    let fileList = ev //: FileList = ev.target?files

                    if fileList.Length > 0 then
                        let file = fileList.Item 0 |> fun f -> f.slice()

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
            Bulma.button.a [
                Bulma.color.isInfo;
                Bulma.button.isFullWidth
                prop.onClick(fun e ->
                    e.preventDefault()
                    let getUploadElement = Browser.Dom.document.getElementById uploadId
                    getUploadElement.click()
                    ()
                )
                prop.text "Upload protocol"
            ]
        ]

    let fileUploadEle (model:Model) dispatch =
        let hasData = model.ProtocolState.UploadedFileParsed <> Array.empty
        Bulma.columns [
            Bulma.columns.isMobile
            prop.children [
                Bulma.column [
                    fileUploadButton model dispatch
                ]
                if hasData then
                    Bulma.column [
                        Bulma.column.isNarrow
                        Bulma.button.a [
                            prop.onClick (fun e -> RemoveUploadedFileParsed |> ProtocolMsg |> dispatch)
                            Bulma.color.isDanger
                            prop.children (Html.i [prop.className "fa-solid fa-times"])
                        ] |> prop.children
                    ]
            ]
        ]
    
    let importToTableEle (model:Model) (dispatch:Messages.Msg -> unit) =
        let hasData = model.ProtocolState.UploadedFileParsed <> Array.empty
        Bulma.field.div [
            Bulma.field.hasAddons
            Bulma.control.div [
                Bulma.control.isExpanded
                Bulma.button.a [
                    Bulma.color.isInfo
                    if hasData then
                        Bulma.button.isActive
                    else
                        Bulma.color.isDanger
                        prop.disabled true
                    Bulma.button.isFullWidth
                    prop.onClick(fun _ ->
                        Browser.Dom.window.alert("'SpreadsheetInterface.ImportFile' is not implemented")
                        //SpreadsheetInterface.ImportFile model.ProtocolState.UploadedFileParsed |> InterfaceMsg |> dispatch
                    )
                    prop.text "Insert json"
                ] |> prop.children
            ] |> prop.children
        ]

    let protocolInsertElement (model:Model) dispatch =
        mainFunctionContainer [
            Bulma.field.div [
                Bulma.help [
                    b [] [str "Insert tables via ISA-JSON files."]
                    str " You can use Swate.Experts to create these files from existing Swate tables. "
                    span [Style [Color NFDIColors.Red.Base]] [str "Only missing building blocks will be added."]
                ]
            ]

            Bulma.field.div [
                fileUploadEle model dispatch
            ]

            importToTableEle model dispatch
        ]

module TemplateFromDB = 

    let toProtocolSearchElement (model:Model) dispatch =
        Bulma.button.span [
            prop.onClick(fun _ -> UpdatePageState (Some Routing.Route.ProtocolSearch) |> dispatch)
            Bulma.color.isInfo
            Bulma.button.isFullWidth
            prop.style [style.margin (length.rem 1, length.px 0)]
            prop.text "Browse database" ]

    let addFromDBToTableButton (model:Messages.Model) dispatch =
        Bulma.columns [
            Bulma.columns.isMobile
            prop.children [
                Bulma.column [
                    prop.children [
                        Bulma.button.a [
                            Bulma.color.isSuccess
                            if model.ProtocolState.TemplateSelected.IsSome then
                                Bulma.button.isActive
                            else
                                Bulma.color.isDanger
                                prop.disabled true
                            Bulma.button.isFullWidth
                            prop.onClick (fun _ ->
                                if model.ProtocolState.TemplateSelected.IsNone then
                                    failwith "No template selected!"
                                // Remove existing columns
                                let mutable columnsToRemove = []
                                // find duplicate columns
                                let tablecopy = model.ProtocolState.TemplateSelected.Value.Table.Copy()
                                for header in tablecopy.Headers do
                                    let containsAtIndex = model.SpreadsheetModel.ActiveTable.Headers.FindIndex(fun h -> h = header)
                                    if containsAtIndex >= 0 then
                                        columnsToRemove <- containsAtIndex::columnsToRemove
                                tablecopy.RemoveColumns (Array.ofList columnsToRemove)
                                let index = Spreadsheet.Sidebar.Controller.SidebarControllerAux.getNextColumnIndex model.SpreadsheetModel
                                SpreadsheetInterface.JoinTable (tablecopy, Some index, Some ARCtrl.TableJoinOptions.WithUnit ) |> InterfaceMsg |> dispatch
                            )
                            prop.text "Add template"
                        ]
                    ]
                ]
                if model.ProtocolState.TemplateSelected.IsSome then
                    Bulma.column [
                        Bulma.column.isNarrow
                        Bulma.button.a [
                            prop.onClick (fun e -> RemoveSelectedProtocol |> ProtocolMsg |> dispatch)
                            Bulma.color.isDanger
                            Html.i [prop.className "fa-solid fa-times"] |> prop.children
                        ] |> prop.children
                    ]
            ]
        ]

    

    let displaySelectedProtocolEle (model:Model) dispatch =
        div [Style [OverflowX OverflowOptions.Auto; MarginBottom "1rem"]] [
            Bulma.table [
                Bulma.table.isFullWidth;
                Bulma.table.isBordered
                prop.children [
                    thead [] [
                        Html.tr [
                            Html.th "Column"
                            Html.th "Column TAN"
                            //Html.th "Unit"
                            //Html.th "Unit TAN"
                        ]
                    ]
                    tbody [] [
                        for column in model.ProtocolState.TemplateSelected.Value.Table.Columns do
                            //let unitOption = column.TryGetColumnUnits()
                            yield
                                Html.tr [
                                    td [] [str (column.Header.ToString())]
                                    td [] [str (if column.Header.IsTermColumn then column.Header.ToTerm().TermAccessionShort else "-")]
                                    //td [] [str (if unitOption.IsSome then insertBB.UnitTerm.Value.Name else "-")]
                                    //td [] [str (if insertBB.HasUnit then insertBB.UnitTerm.Value.TermAccession else "-")]
                                ]
                    ]
                ]
            ]
        ]

    let showDatabaseProtocolTemplate (model:Messages.Model) dispatch =
        mainFunctionContainer [
            Bulma.field.div [
                Bulma.help [
                    b [] [str "Search the database for templates."]
                    str " The building blocks from these templates can be inserted into the Swate table. "
                    span [Style [Color NFDIColors.Red.Base]] [str "Only missing building blocks will be added."]
                ]
            ]
            Bulma.field.div [
                toProtocolSearchElement model dispatch
            ]

            Bulma.field.div [
                addFromDBToTableButton model dispatch
            ]
            if model.ProtocolState.TemplateSelected.IsSome then
                Bulma.field.div [
                    displaySelectedProtocolEle model dispatch
                ]
                Bulma.field.div [
                    addFromDBToTableButton model dispatch
                ]
        ]


let fileUploadViewComponent (model:Messages.Model) dispatch =
    div [ 
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        
        pageHeader "Templates"

        // Box 1
        Bulma.label "Add template from database."

        TemplateFromDB.showDatabaseProtocolTemplate model dispatch

        //// Box 2
        //Bulma.label "Add template(s) from file."

        //TemplateFromJsonFile.protocolInsertElement model dispatch
    ]