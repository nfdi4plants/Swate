module Modals.ModalElements

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared

open ARCtrl
open JsonImport
open Components


type ModalElements =

    static member RadioPlugin(radioGroup: string, txt:string, isChecked, onChange: bool -> unit, ?isDisabled: bool) =
        let isDisabled = defaultArg isDisabled false
        Daisy.formControl [
            Daisy.label [
                prop.className [
                    "cursor-pointer transition-colors"
                    if isDisabled then
                        "!cursor-not-allowed"
                    else
                        "hover:bg-base-300"
                ]
                prop.children [
                    Daisy.radio [
                        prop.disabled isDisabled
                        radio.xs
                        prop.name radioGroup
                        prop.isChecked isChecked
                        prop.onChange onChange
                    ]
                    Html.span [
                        prop.className "text-sm"
                        prop.text txt
                    ]
                ]
            ]
        ]

    static member Box(title: string, icon: string, content: ReactElement, ?className: string list) =
        Html.div [
            prop.className [
                "rounded shadow p-2 flex flex-col gap-2 border"
                if className.IsSome then
                    className.Value |> String.concat " "
            ]
            prop.children [
                Html.h3 [
                    prop.className "font-semibold gap-2 flex flex-row items-center"
                    prop.children [
                        Html.i [prop.className icon]
                        Html.span title
                    ]
                ]
                content
            ]
        ]

    static member ImportRadioPlugins(importType: TableJoinOptions, radioData: (TableJoinOptions * string)[], setImportType: TableJoinOptions -> unit) =
        let myradio(target: TableJoinOptions, txt: string) =
            let isChecked = importType = target
            ModalElements.RadioPlugin("importType", txt, isChecked, fun (b: bool) -> if b then setImportType target)
        ModalElements.Box ("Import Type", "fa-solid fa-cog", React.fragment [
            Html.div [
                for i in 0..radioData.Length-1 do
                    myradio(radioData.[i])
            ]
        ])
