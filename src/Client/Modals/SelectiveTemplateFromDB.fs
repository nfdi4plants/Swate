namespace Modals.Template

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared

open ARCtrl
open JsonImport
open Components


type SelectiveTemplateFromDBModal = 

    static member private Radio(radioGroup: string, txt:string, isChecked, onChange: bool -> unit, ?isDisabled: bool) =
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