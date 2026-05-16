[<AutoOpen>]
module Swate.Components.Composite.ThemeSelector.Types

open Fable.Core

[<StringEnum(Fable.Core.CaseRules.LowerFirst)>]
type Theme =
    | Auto
    | Sunrise
    | Finster
    | Planti
    | Viola

module Theme =
    let toString (theme: Theme) =
        match theme with
        | Auto -> "auto"
        | Sunrise -> "light"
        | Finster -> "dark"
        | Planti -> "planti"
        | Viola -> "viola"

    let fromString (theme: string) =
        match theme with
        | "auto" -> Auto
        | "light" -> Sunrise
        | "dark" -> Finster
        | "planti" -> Planti
        | "viola" -> Viola
        | _ -> Auto // Default to Auto if the string does not match any known theme