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
                "swt:button swt:px-3 swt:h-8 swt:min-h-8 swt:text-secondary-content swt:transition-colors swt:duration-300 swt:inline-flex swt:justify-center swt:items-center swt:hover:text-primary swt:cursor-pointer swt:disabled:cursor-not-allowed swt:disabled:text-gray-500"
                if classes.IsSome then
                    classes.Value
            ]
            prop.tabIndex (if isDisabled then -1 else 0)
            prop.title desc
            prop.disabled isDisabled
            prop.onClick onclick
            if props.IsSome then
                yield! props.Value
            prop.children children
        ]