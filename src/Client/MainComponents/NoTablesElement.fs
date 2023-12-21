module MainComponents.NoTablesElement

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl.ISA
open Shared

open Elmish

let private buttonStyle = prop.style [style.margin(length.rem 1.5)]

module private UploadHandler =

    open Fable.Core.JsInterop

    let mutable styleCounter = 0

    [<Literal>]
    let id = "droparea"
    let updateMsg = fun r -> r |> SetArcFileFromBytes |> SpreadsheetMsg

    let setActive_DropArea() =
        styleCounter <- styleCounter + 1
        let ele = Browser.Dom.document.getElementById(id)
        ele?style?border <- $"2px solid {NFDIColors.Mint.Base}"

    let setInActive_DropArea() =
        styleCounter <- (System.Math.Max(styleCounter - 1,0))
        if styleCounter <= 0 then
            let ele = Browser.Dom.document.getElementById(id)
            ele?style?border <- "unset"

    let ondrop dispatch =
        fun (e: Browser.Types.DragEvent) ->
            e.preventDefault()
            if e.dataTransfer.items <> null then
                let item = e.dataTransfer.items.[0]
                if item.kind = "file" then
                    setInActive_DropArea()
                    styleCounter <- 0
                    let file = item.getAsFile()
                    let reader = Browser.Dom.FileReader.Create()
                    reader.onload <- (fun _ -> updateMsg !!reader.result |> dispatch)
                    reader.readAsArrayBuffer(file)

let private uploadNewTable dispatch =
    let uploadId = "UploadFiles_MainWindowInit"
    Bulma.label [
        //prop.onDragEnter <| UploadHandler.dontBubble
        //prop.onDragLeave <| UploadHandler.dontBubble
        prop.style [style.fontWeight.normal]
        prop.children [
            Html.input [
                prop.id uploadId
                prop.type' "file";
                prop.style [style.display.none]
                prop.onChange (fun (ev: Event) ->
                    let fileList : FileList = ev.target?files

                    if fileList.length > 0 then
                        let file = fileList.item 0 |> fun f -> f.slice()

                        let reader = Browser.Dom.FileReader.Create()

                        reader.onload <- fun evt ->
                            let (r: byte []) = evt.target?result
                            r |> SetArcFileFromBytes |> SpreadsheetMsg |> dispatch
                                   
                        reader.onerror <- fun evt ->
                            curry GenericLog Cmd.none ("Error", evt?Value) |> DevMsg |> dispatch

                        reader.readAsArrayBuffer(file)
                    else
                        ()
                    let picker = Browser.Dom.document.getElementById(uploadId)
                    // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                    picker?value <- null
                    ()
                )
            ]
            Bulma.button.span [
                Bulma.button.isLarge
                buttonStyle
                Bulma.color.isInfo
                prop.onClick(fun e ->
                    e.preventDefault()
                    let getUploadElement = Browser.Dom.document.getElementById uploadId
                    getUploadElement.click()
                    ()
                )
                prop.children [
                    Html.div "Import File"
                ]
            ]
        ]
    ]

open ARCtrl.Template

let private createNewTable isActive toggle (dispatch: Messages.Msg -> unit) =
    
    Bulma.dropdown [
        if isActive then 
            Bulma.dropdown.isActive
        buttonStyle
        prop.children [
            Bulma.dropdownTrigger [
                Bulma.button.span [
                    Bulma.button.isLarge
                    Bulma.color.isLink
                    prop.onClick toggle
                    //prop.onClick(fun e -> SpreadsheetInterface.CreateAnnotationTable e.ctrlKey |> Messages.InterfaceMsg |> dispatch)
                    prop.children [
                        Html.div "New File"
                    ]
                ]
            ]
            Bulma.dropdownMenu [
                Bulma.dropdownContent [
                    Bulma.dropdownItem.a [
                        prop.onClick(fun _ ->
                            let i = ArcInvestigation.init("New Investigation")
                            ArcFiles.Investigation i
                            |> UpdateArcFile
                            |> Messages.SpreadsheetMsg
                            |> dispatch
                        )
                        prop.text "Investigation"
                    ]
                    Bulma.dropdownItem.a [
                        prop.onClick(fun _ ->
                            let s = ArcStudy.init("New Study")
                            let newTable = s.InitTable("New Study Table")
                            ArcFiles.Study (s, [])
                            |> UpdateArcFile
                            |> Messages.SpreadsheetMsg
                            |> dispatch
                        )
                        prop.text "Study"
                    ]
                    Bulma.dropdownItem.a [
                        prop.onClick(fun _ ->
                            let a = ArcAssay.init("New Assay")
                            let newTable = a.InitTable("New Assay Table")
                            ArcFiles.Assay a
                            |> UpdateArcFile
                            |> Messages.SpreadsheetMsg
                            |> dispatch
                        )
                        prop.text "Assay"
                    ]
                    Bulma.dropdownDivider []
                    Bulma.dropdownItem.a [
                        prop.onClick(fun _ ->
                            let template = Template.init("New Template")
                            let table = ArcTable.init("New Table")
                            template.Table <- table
                            template.Version <- "0.0.0"
                            template.Id <- System.Guid.NewGuid()
                            template.LastUpdated <- System.DateTime.UtcNow
                            ArcFiles.Template template
                            |> UpdateArcFile
                            |> Messages.SpreadsheetMsg
                            |> dispatch
                        )
                        prop.text "Template"
                    ]
                ]
            ]
        ]
    ]

[<ReactComponent>]
let Main (args: {|dispatch: Messages.Msg -> unit|}) =
    let isActive, setIsActive = React.useState(true)
    Html.div [
        prop.id UploadHandler.id
        prop.onDragEnter (fun e ->
            e.preventDefault()
            if e.dataTransfer.items <> null then
                let item = e.dataTransfer.items.[0]
                if item.kind = "file" then
                    UploadHandler.setActive_DropArea()
        )
        prop.onDragLeave(fun e ->
            //e.preventDefault()
            UploadHandler.setInActive_DropArea()
        )
        prop.onDragOver(fun e -> e.preventDefault())
        prop.onDrop <| UploadHandler.ondrop args.dispatch
        prop.style [
            style.height.inheritFromParent
            style.width.inheritFromParent
            style.display.flex
            style.justifyContent.center
            style.alignItems.center
        ]
        prop.children [
            Html.div [
                //prop.style [style.height.minContent; style.display.inheritFromParent; style.justifyContent.spaceBetween]
                prop.style [style.display.flex; style.justifyContent.spaceBetween]
                prop.children [
                    createNewTable isActive (fun _ -> not isActive |> setIsActive) args.dispatch
                    uploadNewTable args.dispatch
                ]
            ]
        ]
    ]