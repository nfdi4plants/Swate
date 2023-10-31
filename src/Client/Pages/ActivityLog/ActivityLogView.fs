module ActivityLog

open Fable
open Fable.Core.JsInterop

open Model
open Messages
open Browser.Types

open Feliz
open Feliz.Bulma
//TO-DO: Save log as tab seperated file

let debugBox model dispatch =
    Bulma.box [
        //Button.button [
        //    Button.Color Color.IsInfo
        //    Button.IsFullWidth
        //    Button.OnClick (fun e -> OfficeInterop.TryExcel |> OfficeInteropMsg |> dispatch )
        //    Button.Props [Style [MarginBottom "1rem"]]
        //] [
        //    str "Try Excel"
        //]
        Bulma.button.button [
            prop.onClick(fun e -> TestMyAPI |> dispatch)
            prop.text "Test api"]
        Bulma.button.button [
            prop.onClick(fun e -> TestMyPostAPI |> dispatch)
            prop.text "Test post api"]
        //Button.button [
        //    Button.Color Color.IsInfo
        //    Button.IsFullWidth
        //    Button.OnClick (fun e -> TryExcel2 |> ExcelInterop |> dispatch )
        //    Button.Props [Style [MarginBottom "1rem"]]
        //] [
        //    str "Try Excel2"
        //]
        //Button.a [
        //    Button.IsFullWidth
        //    Button.OnClick (fun e ->
        //        let msg = UpdateWarningModal None
        //        let message = "This is a warning modal. Be careful if you know what you are doing."
        //        let nM = {|ModalMessage = message; NextMsg = msg|} |> Some
        //        UpdateWarningModal nM |> dispatch
        //    )
        //] [
        //    str "Test"
        //]
        Bulma.label "Dangerzone"
        Bulma.container [
            prop.style [style.padding (length.rem 1); style.border(length.px 2.5, borderStyle.solid, NFDIColors.Red.Base); style.borderRadius 10]
            prop.children [
                Bulma.button.a [
                    Bulma.color.isWarning
                    Bulma.button.isFullWidth
                    prop.onClick (fun e -> OfficeInterop.GetSwateCustomXml |> OfficeInteropMsg |> dispatch )
                    prop.style [style.marginBottom(length.rem 1)];
                    prop.title "Show record type data of Swate custom Xml"
                    prop.text "Show Custom Xml!"
                ]
                Bulma.button.a [
                    Bulma.color.isDanger
                    Bulma.button.isFullWidth
                    prop.onClick (fun e -> OfficeInterop.DeleteAllCustomXml |> OfficeInteropMsg |> dispatch )
                    prop.title "Be sure you know what you do. This cannot be undone!"
                    prop.text "Delete All Custom Xml!"
                ]
            ]
        ]
    ]

let activityLogComponent (model:Model) dispatch =
    Html.div [

        Bulma.label "Activity Log"

        //debugBox model dispatch

        Bulma.label "Display all recorded activities of this session."
        Html.div [
            prop.style [style.borderLeft(5, borderStyle.solid, NFDIColors.Mint.Base); style.padding(length.rem 0.25, length.rem 1); style.marginBottom(length.rem 1) ]
            prop.children [
                Bulma.table [
                    Bulma.table.isFullWidth
                    Html.tbody (
                        model.DevState.Log
                        |> List.map LogItem.toTableRow
                    )
                    |> prop.children
                ]
            ]
        ]
    ]

