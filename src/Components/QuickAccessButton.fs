namespace Components

open Fable.Core
open Feliz
open Browser.Types

[<Erase; Mangle(false)>]
type QuickAccessButton =

    [<NamedParams>]
    static member QuickAccessButton(
            desc:string, children: ReactElement, onclick: Event -> unit,
            ?isDisabled: bool, ?props: IReactProperty seq, ?classes: string
        ) : ReactElement =
        let isDisabled = defaultArg isDisabled false
        Html.button [
            prop.className [
                "px-3 h-8 min-h-8 text-secondary-content transition-colors duration-300 inline-flex justify-center items-center hover:text-primary cursor-pointer disabled:cursor-not-allowed disabled:text-gray-500";
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
    [<NamedParams>]
    static member QuickAccessButtonT(
            desc:string, children: ReactElement, onclick: Event -> unit,
            ?isDisabled: bool, ?props: IReactProperty seq, ?classes: string
        ) : JSX.Element =
        QuickAccessButton.QuickAccessButton(
            desc, children, onclick,
            ?isDisabled = isDisabled, ?props=props, ?classes=classes
        )
        |> unbox