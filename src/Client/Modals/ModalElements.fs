module Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared
open SelectedColumns

open ARCtrl
open JsonImport
open Components
open Fable.React.Helpers

type ModalElements =

    static member LogicContainer (children: ReactElement list) =
        Html.div [
        //     prop.className "border-l-4 border-transparent px-4 py-2 shadow-md"
        //     prop.style [
        //         let rndVal = rnd.Next(30,70)
        //         let colorArr = [|NFDIColors.LightBlue.Lighter10; NFDIColors.Mint.Lighter10;|]
        //         style.custom("borderImageSlice", "1")
        //         style.custom("borderImageSource", $"linear-gradient({colorArr.[if order then 0 else 1]} {100-rndVal}%%, {colorArr.[if order then 1 else 0]})")
        //         order <- not order
        //     ]
            prop.className "relative flex p-4 animated-border shadow-md gap-4 flex-col" //experimental
            prop.children children
        ]

    static member Button(text: string, onClickAction, buttonInput, ?isDisabled: bool) =
        let isDisabled = defaultArg isDisabled false
        Daisy.button.a [
            button.success
            button.wide
            if isDisabled then
                button.error
            prop.disabled isDisabled
            prop.onClick (fun _ -> onClickAction buttonInput)
            
            prop.text text
        ]

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

    static member BoxWithChildren(children: ReactElement list, ?title: string, ?icon: string, ?className: string list) =
        Html.div [
            prop.className [
                "rounded shadow p-2 flex flex-col gap-2 border"
                if className.IsSome then
                    className.Value |> String.concat " "
            ]
            prop.children [
                Html.h3 [
                    prop.className "font-semibold gap-2 flex flex-row items-center"
                    if icon.IsSome || title.IsSome then
                        prop.children [
                            if icon.IsSome then
                                Html.i [prop.className icon.Value]
                            if title.IsSome then
                                Html.span title.Value
                        ]
                    prop.children children
                ]
            ]
        ]

    static member SelectorButton<'a when 'a : equality> (targetselector: 'a, selector: 'a, setSelector: 'a -> unit, ?isDisabled) =
        Daisy.button.button [
            join.item
            if isDisabled.IsSome then
                prop.disabled isDisabled.Value
            prop.style [style.flexGrow 1]
            if (targetselector = selector) then
                button.primary
            prop.onClick (fun _ -> setSelector targetselector)
            prop.text (string targetselector)
        ]
