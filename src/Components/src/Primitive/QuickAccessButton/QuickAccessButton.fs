namespace Swate.Components.Primitive.QuickAccessButton

open Fable.Core
open Feliz
open Browser.Types
open Swate.Components.Primitive

[<Erase; Mangle(false)>]
type QuickAccessButton =

    [<ExportDefault; NamedParams>]
    static member QuickAccessButton
        (
            desc: string,
            children: ReactElement,
            onclick: MouseEvent -> unit,
            ?isDisabled: bool,
            ?props: IReactProperty seq,
            ?color: DaisyuiColors,
            ?classes: string
        ) =
        let isDisabled = defaultArg isDisabled false

        Html.button [
            prop.className [
                "swt:btn swt:btn-ghost swt:btn-square swt:btn-transparent swt:bg-transparent swt:border-none swt:shadow-none"

                match color with
                | Some DaisyuiColors.Primary -> "swt:hover:text-primary!"
                | Some DaisyuiColors.Secondary -> "swt:hover:text-secondary!"
                | Some DaisyuiColors.Accent -> "swt:hover:text-accent!"
                | Some DaisyuiColors.Error -> "swt:hover:text-error!"
                | Some DaisyuiColors.Info -> "swt:hover:text-info!"
                | Some DaisyuiColors.Success -> "swt:hover:text-success!"
                | Some DaisyuiColors.Warning -> "swt:hover:text-warning!"
                | None -> "swt:hover:text-primary!"

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