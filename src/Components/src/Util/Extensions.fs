namespace Swate.Components

open Feliz

[<AutoOpen>]
module Extensions =

    type prop with
        static member testid (value: string) = prop.custom("data-testid", value)

