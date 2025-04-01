namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Swate.Components.Shared

open ARCtrl
open FileImport
open Fable.React.Helpers

type ModalElements =

    static member Button(text: string, onClickAction, buttonInput, ?isDisabled: bool, ?className: string) =
        let isDisabled = defaultArg isDisabled false

        Daisy.button.a [
            button.primary
            if isDisabled then
                button.error
            if className.IsSome then
                prop.className className.Value
            prop.disabled isDisabled
            prop.onClick (fun _ -> onClickAction buttonInput)
            prop.text text
        ]

    static member RadioPlugin(radioGroup: string, txt: string, isChecked, onChange: bool -> unit, ?isDisabled: bool) =
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
                    Html.span [ prop.className "text-sm"; prop.text txt ]
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
                    prop.children [ Html.i [ prop.className icon ]; Html.span title ]
                ]
                content
            ]
        ]

    static member SelectorButton<'a when 'a: equality>
        (targetselector: 'a, selector: 'a, setSelector: 'a -> unit, ?isDisabled)
        =
        Daisy.button.button [
            join.item
            if isDisabled.IsSome then
                prop.disabled isDisabled.Value
            prop.style [ style.flexGrow 1 ]
            if (targetselector = selector) then
                button.primary
            prop.onClick (fun _ -> setSelector targetselector)
            prop.text (string targetselector)
        ]