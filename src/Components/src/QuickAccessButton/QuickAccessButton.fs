namespace Swate.Components

open Fable.Core
open Feliz
open Browser.Types

[<Erase; Mangle(false)>]
type QuickAccessButton =

    [<ExportDefault; NamedParams>]
    static member QuickAccessButton
        (
            desc: string,
            children: ReactElement,
            onclick: Event -> unit,
            ?isDisabled: bool,
            ?props: IReactProperty seq,
            ?classes: string
        ) =
        let isDisabled = defaultArg isDisabled false

        Html.button [
            prop.className [
                "swt:btn swt:btn-ghost swt:bg-transparent swt:border-none swt:shadow-none swt:disabled:cursor-not-allowed swt:disabled:text-gray-500"

                if classes.IsSome then
                    classes.Value

                if not isDisabled then
                    "swt:text-white"
            ]
            prop.tabIndex (if isDisabled then -1 else 0)
            prop.title desc
            prop.disabled isDisabled
            prop.onClick onclick
            if props.IsSome then
                yield! props.Value
            prop.children children
        ]