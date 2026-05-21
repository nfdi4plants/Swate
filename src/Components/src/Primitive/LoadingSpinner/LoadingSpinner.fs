namespace Swate.Components.Primitive.LoadingSpinner

open Fable.Core
open Feliz
open Swate.Components.Primitive

[<Erase; Mangle(false)>]
type LoadingSpinner =

    [<ExportDefault; NamedParams>]
    static member LoadingSpinner(?text: string, ?size: DaisyuiSize, ?color: DaisyuiColors) =
        Html.span [
            prop.className "swt:flex swt:flex-col swt:items-center swt:gap-2 swt:py-10"
            prop.children [
                Html.div [
                    prop.className [
                        "swt:loading swt:loading-spinner"
                        match size with
                        | Some(DaisyuiSize.XS) -> $"swt:loading-xs"
                        | Some(DaisyuiSize.SM) -> $"swt:loading-sm"
                        | Some(DaisyuiSize.MD) -> $"swt:loading-md"
                        | Some(DaisyuiSize.LG) -> $"swt:loading-lg"
                        | Some(DaisyuiSize.XL) -> $"swt:loading-xl"
                        | None -> ()
                        match color with
                        | Some DaisyuiColors.Primary -> "swt:loading-primary"
                        | Some DaisyuiColors.Secondary -> "swt:loading-secondary"
                        | Some DaisyuiColors.Accent -> "swt:loading-accent"
                        | Some DaisyuiColors.Warning -> "swt:loading-warning"
                        | Some DaisyuiColors.Error -> "swt:loading-error"
                        | Some DaisyuiColors.Info -> "swt:loading-info"
                        | Some DaisyuiColors.Success -> "swt:loading-success"
                        | None -> ()
                    ]
                ]
                match text with
                | Some t -> Html.span [ prop.text t ]
                | None -> Html.none
            ]
        ]