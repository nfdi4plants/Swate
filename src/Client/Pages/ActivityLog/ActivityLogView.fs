module ActivityLog

open Fable
open Fable.Core.JsInterop

open Model
open Messages
open Browser.Types

open Feliz
open Feliz.DaisyUI
//TO-DO: Save log as tab seperated file

let debugBox model dispatch =
    Html.div [
        //Button.button [
        //    Button.Color Color.IsInfo
        //    Button.IsFullWidth
        //    Button.OnClick (fun e -> OfficeInterop.TryExcel |> OfficeInteropMsg |> dispatch )
        //    Button.Props [Style [MarginBottom "1rem"]]
        //] [
        //    str "Try Excel"
        //]
        Daisy.button.button [
            prop.onClick(fun e -> TestMyAPI |> dispatch)
            prop.text "Test api"]
        Daisy.button.button [
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
    ]

let activityLogComponent (model:Model) dispatch =
    Html.div [

        Html.h5 "Activity Log"

        //debugBox model dispatch

        Html.p "Display all recorded activities of this session."
        Html.div [
            prop.style [style.borderLeft(5, borderStyle.solid, NFDIColors.Mint.Base); style.padding(length.rem 0.25, length.rem 1); style.marginBottom(length.rem 1) ]
            prop.children [
                Daisy.table [
                    Html.tbody (
                        model.DevState.Log
                        |> List.map LogItem.toTableRow
                    )
                    |> prop.children
                ]
            ]
        ]
    ]

