namespace Pages

open Fable
open Fable.Core.JsInterop

open Model
open Messages
open Browser.Types

open Feliz
open Feliz.DaisyUI
open Elmish
open Messages

[<AutoOpen>]
module rec PersistentStorage =

    type ActivityLog =

        static member Main (model: Model, dispatch) =
            Html.div [
                Daisy.toggle [
                    let autosaveConfig = model.PersistentStorageState.getAutosaveConfiguration()

                    if autosaveConfig.IsSome && autosaveConfig.Value <> model.PersistentStorageState.Autosave then
                        PersistentStorage.UpdateAutosave autosaveConfig.Value |> PersistentStorageMsg |> dispatch

                    prop.isChecked model.PersistentStorageState.Autosave

                    toggle.primary

                    prop.onChange (fun (b: bool) ->
                        PersistentStorage.UpdateAutosave (model.PersistentStorageState.Autosave) |> PersistentStorageMsg |> dispatch
                        model.PersistentStorageState.setAutosaveConfiguration(b)
                        if not b then LocalHistory.Model.ResetHistoryWebStorage()
                    )
                ]
                Html.span [ Html.text "Autosave"]
                Daisy.table [
                    prop.className "table-xs"
                    prop.children [
                        Html.tbody (
                            model.DevState.Log
                            |> List.map LogItem.toTableRow
                        )
                    ]
                ]
            ]
