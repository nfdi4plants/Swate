namespace MainComponents

open Feliz

open SpreadsheetInterface
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl
open Swate.Components.Shared
open Swate

open Elmish

module private Helper =

    let uploadNewTable dispatch =
        let uploadId = "UploadFiles_MainWindowInit"

        Html.p [
            prop.children [
                Html.input [
                    prop.id uploadId
                    prop.type'.file
                    prop.style [ style.display.none ]
                    prop.onChange (fun (ev: Event) ->
                        let fileList: FileList = ev.target?files

                        if fileList.length > 0 then
                            let file = fileList.item 0 |> fun f -> f.slice ()

                            let reader = Browser.Dom.FileReader.Create()

                            reader.onload <-
                                fun evt ->
                                    let (r: byte[]) = evt.target?result
                                    r |> ImportXlsx |> InterfaceMsg |> dispatch

                            reader.onerror <-
                                fun evt -> curry GenericLog Cmd.none ("Error", evt?Value) |> DevMsg |> dispatch

                            reader.readAsArrayBuffer (file)
                        else
                            ()

                        let picker = Browser.Dom.document.getElementById (uploadId)
                        // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                        picker?value <- null
                        ()
                    )
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-lg swt:btn-outline"
                    prop.onClick (fun e ->
                        e.preventDefault ()
                        let getUploadElement = Browser.Dom.document.getElementById uploadId
                        getUploadElement.click ()
                        ()
                    )
                    prop.children [ Html.div "Import File" ]
                ]
            ]
        ]

    let createNewTableItem (txt: string, onclick: Event -> unit) =
        Html.li [
            Html.a [
                prop.className "swt:btn swt:btn-block swt:btn-ghost swt:btn-sm swt:justify-start"
                prop.onClick (fun e -> onclick e)
                prop.text txt
            ]
        ]

    let createNewFile (dispatch: Messages.Msg -> unit) =
        Html.div [
            prop.className "swt:dropdown"
            prop.children [
                Html.div [
                    prop.className "swt:btn swt:btn-lg swt:btn-primary swt:w-full"
                    prop.tabIndex 0
                    prop.text "New File"
                ]
                Html.ul [
                    prop.tabIndex 0
                    prop.className
                        "swt:dropdown-content swt:menu swt:p-2 swt:shadow swt:bg-base-300 swt:rounded-box swt:w-64"
                    prop.children [
                        Html.ul [
                            createNewTableItem (
                                "Investigation",
                                fun _ ->
                                    let i = ArcInvestigation.init ("New Investigation")
                                    ArcFiles.Investigation i |> UpdateArcFile |> InterfaceMsg |> dispatch
                            )
                            createNewTableItem (
                                "Study",
                                fun _ ->
                                    let s = ArcStudy.init ("New Study")
                                    let _ = s.InitTable("New Study Table")
                                    ArcFiles.Study(s, []) |> UpdateArcFile |> InterfaceMsg |> dispatch
                            )
                            createNewTableItem (
                                "Assay",
                                fun _ ->
                                    let a = ArcAssay.init ("New Assay")
                                    let _ = a.InitTable("New Assay Table")
                                    ArcFiles.Assay a |> UpdateArcFile |> InterfaceMsg |> dispatch
                            )
                            createNewTableItem (
                                "Run",
                                fun _ ->
                                    let r = ArcRun.init ("New Run")
                                    let _ = r.InitTable("New Run Table")
                                    ArcFiles.Run r |> UpdateArcFile |> InterfaceMsg |> dispatch
                            )
                            createNewTableItem (
                                "Workflow",
                                fun _ ->
                                    let w = ArcWorkflow.init ("New Workflow")
                                    ArcFiles.Workflow w |> UpdateArcFile |> InterfaceMsg |> dispatch
                            )
                            createNewTableItem (
                                "Datamap",
                                fun _ ->
                                    let dataMap = DataMap.init ()
                                    let arcFile = ArcFiles.DataMap(None, dataMap)
                                    arcFile |> UpdateArcFile |> InterfaceMsg |> dispatch
                            )
                            Html.div [ prop.className "swt:divider swt:divider-neutral swt:m-0" ]
                            createNewTableItem (
                                "Template",
                                fun _ ->
                                    let template = Template.init ("New Template")
                                    let table = ArcTable.init ("New Table")
                                    template.Table <- table
                                    template.Version <- "0.0.0"
                                    template.Id <- System.Guid.NewGuid()
                                    template.LastUpdated <- System.DateTime.Now
                                    ArcFiles.Template template |> UpdateArcFile |> InterfaceMsg |> dispatch
                            )
                        ]
                    ]
                ]
            ]
        ]

open Fable.Core

type NoFileElement =

    [<ReactComponent>]
    static member Main(dispatch: Messages.Msg -> unit) =
        let draggedOverCounter, setDraggedOverCounter =
            React.useStateWithUpdater (None: int option)

        let __MIN_DRAGGEDOVER_COUNT__ = 0

        let isDraggedOver =
            match draggedOverCounter with
            | Some _ -> true
            | _ -> false

        let increaseDraggedOver =
            fun latest ->

                match latest with
                | Some c -> Some(c + 1)
                | _ -> Some __MIN_DRAGGEDOVER_COUNT__

        let decreaseDraggedOver =
            fun latest ->

                match latest with
                | Some c when c > __MIN_DRAGGEDOVER_COUNT__ -> Some(c - 1)
                | _ -> None

        Html.div [
            prop.onDragEnter (fun e ->
                e.preventDefault ()

                if e.dataTransfer.items <> null then
                    let item = e.dataTransfer.items.[0]

                    if item.kind = "file" then
                        setDraggedOverCounter increaseDraggedOver
            )
            prop.onDragLeave (fun e ->
                e.preventDefault ()

                if e.dataTransfer.items <> null then
                    let item = e.dataTransfer.items.[0]

                    if item.kind = "file" then
                        setDraggedOverCounter decreaseDraggedOver
            )
            prop.onDragOver (fun e -> e.preventDefault ())
            prop.onDrop (fun e ->
                e.preventDefault ()

                if e.dataTransfer.items <> null then
                    let item = e.dataTransfer.items.[0]

                    if item.kind = "file" then
                        let file = item.getAsFile ()
                        let extension = file.name.Split('.') |> Array.last

                        try
                            match extension.ToLower() with
                            | "xlsx" ->
                                let reader = Browser.Dom.FileReader.Create()

                                reader.onload <-
                                    (fun _ -> unbox<byte[]> reader.result |> ImportXlsx |> InterfaceMsg |> dispatch)

                                reader.readAsArrayBuffer (file)
                            | "json" ->
                                let reader = Browser.Dom.FileReader.Create()

                                reader.onload <-
                                    (fun e ->

                                        {|
                                            jsonString = unbox<string> reader.result
                                            jsonType = None
                                            filetype = None
                                            fileName = Some file.name
                                        |}
                                        |> ImportRawJson
                                        |> InterfaceMsg
                                        |> dispatch
                                    )

                                reader.readAsText (file)
                            | _ -> failwithf "Unsupported file extension: %s" extension
                        finally
                            setDraggedOverCounter (fun _ -> None)

            )
            prop.className [
                "swt:flex swt:h-full swt:w-full swt:justify-center swt:items-center swt:border-2 swt:border-dashed"
                if isDraggedOver then
                    "swt:border-primary"
                else
                    "swt:border-transparent"
            ]
            prop.children [
                Html.div [
                    prop.className "swt:grid swt:grid-cols-1 swt:@md/main:grid-cols-2 swt:gap-4"
                    prop.children [ Helper.createNewFile dispatch; Helper.uploadNewTable dispatch ]
                ]
            ]
        ]