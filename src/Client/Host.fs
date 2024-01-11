[<AutoOpen>]
module Host

[<RequireQualifiedAccess>]
type Swatehost =
| Browser
| Excel
| ARCitect //WIP

with
    static member ofQueryParam (queryInteger: int option) =
        match queryInteger with
        | Some 1 -> Swatehost.ARCitect
        | Some 2 -> Swatehost.Excel
        | _ -> Browser