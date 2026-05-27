module Swate.Components.Primitive.Blankslate.Types

open Fable.Core

[<StringEnum>]
[<RequireQualifiedAccess>]
type BlankslateTextSize =
    | Small
    | Medium
    | Large

[<StringEnum>]
[<RequireQualifiedAccess>]
type BlankslateActionKind =
    | Primary
    | Secondary


type BlankslateAction = {
    Label: string
    OnClick: unit -> unit
    IconClassName: string option
    Disabled: bool
    Kind: BlankslateActionKind
} with

    static member create
        (label: string, onClick: unit -> unit, ?iconClassName: string, ?disabled: bool, ?kind: BlankslateActionKind)
        =
        {
            Label = label
            OnClick = onClick
            IconClassName = iconClassName
            Disabled = defaultArg disabled false
            Kind = defaultArg kind BlankslateActionKind.Primary
        }