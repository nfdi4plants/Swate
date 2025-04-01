namespace Modals

open Fable.React
open Model
open Messages
open Swate.Components.Shared
open Feliz
open Feliz.DaisyUI
open Swate.Components

type InteropLogging =
    static member Main(model: DevState, dispatch) =
        let rmv = Util.RMV_MODAL dispatch

        let closeMsg =
            fun e ->
                rmv e
                UpdateDisplayLogList [] |> DevMsg |> dispatch

        let logs = model.DisplayLogList

        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop [ prop.onClick closeMsg ]
                Daisy.modalBox.div [
                    prop.style [ style.width (length.percent 80); style.maxHeight (length.percent 80) ]
                    prop.className "flex flex-col gap-4"
                    prop.children [
                        Html.div [
                            prop.className "grow flex justify-end"
                            prop.children [ Components.DeleteButton(props = [ prop.onClick closeMsg ]) ]
                        ]
                        Daisy.table [ table.xs; prop.children [ Html.tbody (logs |> List.map LogItem.toTableRow) ] ]
                        Daisy.button.a [
                            button.warning
                            button.wide
                            prop.className "mx-auto"
                            prop.onClick closeMsg
                            prop.text "Continue"
                        ]
                    ]
                ]
            ]
        ]