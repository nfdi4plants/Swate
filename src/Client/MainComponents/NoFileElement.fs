namespace MainComponents

open Feliz
open Feliz.DaisyUI

open SpreadsheetInterface
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl
open Shared

open Elmish


module private UploadHandler =

    open Fable.Core.JsInterop

    let mutable styleCounter = 0

    [<Literal>]
    let id = "droparea"

    [<Literal>]
    let HightlightBorderClasses = "border-2 border-primary"
    let updateMsg = fun r -> r |> ImportXlsx |> InterfaceMsg

    let setActive_DropArea() =
        styleCounter <- styleCounter + 1
        let ele = Browser.Dom.document.getElementById(id)
        ele.classList.add HightlightBorderClasses

    let setInActive_DropArea() =
        styleCounter <- (System.Math.Max(styleCounter - 1,0))
        if styleCounter <= 0 then
            let ele = Browser.Dom.document.getElementById(id)
            ele.classList.remove HightlightBorderClasses

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

module private Helper =

    let uploadNewTable dispatch =
        let uploadId = "UploadFiles_MainWindowInit"
        Html.p [
            prop.children [
                Html.input [
                    prop.id uploadId
                    prop.type'.file;
                    prop.style [style.display.none]
                    prop.onChange (fun (ev: Event) ->
                        let fileList : FileList = ev.target?files

                        if fileList.length > 0 then
                            let file = fileList.item 0 |> fun f -> f.slice()

                            let reader = Browser.Dom.FileReader.Create()

                            reader.onload <- fun evt ->
                                let (r: byte []) = evt.target?result
                                r |> ImportXlsx |> InterfaceMsg |> dispatch

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
                Daisy.button.button [
                    button.lg
                    button.outline
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

    let createNewTableItem (txt: string, onclick: Event -> unit)=
        Html.li [Daisy.button.a [
            button.block
            button.ghost
            button.sm
            prop.className "justify-start"
            prop.onClick (fun e ->
                log "inner"
                onclick e
            )
            prop.text txt
        ]]

    let createNewFile (dispatch: Messages.Msg -> unit) =

        Daisy.dropdown [
            prop.children [
                Html.div [
                    prop.className "btn btn-lg btn-primary w-full"
                    prop.tabIndex 0
                    prop.text "New File"
                ]
                Daisy.dropdownContent [
                    prop.tabIndex 0
                    prop.className "bg-base-300 [&_a]:rounded-none shadow"
                    prop.children [
                        Html.ul [
                            createNewTableItem("Investigation", fun _ ->
                                let i = ArcInvestigation.init("New Investigation")
                                ArcFiles.Investigation i
                                |> UpdateArcFile
                                |> InterfaceMsg
                                |> dispatch
                            )
                            createNewTableItem("Study", fun _ ->
                                let s = ArcStudy.init("New Study")
                                let _ = s.InitTable("New Study Table")
                                ArcFiles.Study (s, [])
                                |> UpdateArcFile
                                |> InterfaceMsg
                                |> dispatch
                            )
                            createNewTableItem("Assay", fun _ ->
                                    log "Test"
                                    let a = ArcAssay.init("New Assay")
                                    let _ = a.InitTable("New Assay Table")
                                    log "New Assay!"
                                    ArcFiles.Assay a
                                    |> UpdateArcFile
                                    |> InterfaceMsg
                                    |> dispatch
                            )
                            Html.li [Daisy.divider [divider.horizontal]]
                            createNewTableItem("Template", fun _ ->
                                let template = Template.init("New Template")
                                let table = ArcTable.init("New Table")
                                template.Table <- table
                                template.Version <- "0.0.0"
                                template.Id <- System.Guid.NewGuid()
                                template.LastUpdated <- System.DateTime.Now
                                ArcFiles.Template template
                                |> UpdateArcFile
                                |> InterfaceMsg
                                |> dispatch
                            )
                        ]
                    ]
                ]
            ]
        ]

type NoFileElement =

    [<ReactComponent>]
    static member Main (args: {|dispatch: Messages.Msg -> unit|}) =
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
                    prop.className "grid grid-cols-1 @md/main:grid-cols-2 gap-4"
                    prop.children [
                        Helper.createNewFile args.dispatch
                        Helper.uploadNewTable args.dispatch
                    ]
                ]
            ]
        ]