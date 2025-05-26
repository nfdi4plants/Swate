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
            ?color: DaisyUIColors,
            ?classes: string
        ) =
        let isDisabled = defaultArg isDisabled false

        Html.button [
            prop.className [
                "swt:btn swt:btn-ghost swt:btn-square"
                match color with
                | Some DaisyUIColors.Primary -> "swt:hover:!text-primary"
                | Some DaisyUIColors.Secondary -> "swt:hover:!text-secondary"
                | Some DaisyUIColors.Accent -> "swt:hover:!text-accent"
                | Some DaisyUIColors.Error -> "swt:hover:!text-error"
                | Some DaisyUIColors.Info -> "swt:hover:!text-info"
                | Some DaisyUIColors.Success -> "swt:hover:!text-success"
                | Some DaisyUIColors.Warning -> "swt:hover:!text-warning"
                | None -> "swt:hover:!text-success"

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