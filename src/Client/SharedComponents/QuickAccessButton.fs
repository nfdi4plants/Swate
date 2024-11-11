namespace Components

open Feliz
open Browser.Types
open Feliz.DaisyUI

type QuickAccessButton =
    static member Main(desc:string, children: ReactElement, onclick: Event -> unit, ?isDisabled, ?props, ?classes: string) =
        let isDisabled = defaultArg isDisabled false
        Html.button [
            prop.className [
                "px-3 h-8 min-h-8 text-primary inline-flex justify-center items-center transition-all hover:brightness-110 cursor-pointer disabled:cursor-not-allowed disabled:text-gray-500";
                if classes.IsSome then classes.Value
            ]
            prop.tabIndex (if isDisabled then -1 else 0)
            prop.title desc
            prop.disabled isDisabled
            prop.onClick onclick
            if props.IsSome then yield! props.Value
            prop.children [
                children
            ]
        ]