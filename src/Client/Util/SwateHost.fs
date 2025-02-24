[<AutoOpen>]
module Host

[<RequireQualifiedAccess>]
type Swatehost =
| Browser
| Excel
| ARCitect 
with
    static member ofQueryParam (queryInteger: int option) =
        match queryInteger with
        | Some 1 -> Swatehost.ARCitect
        | Some 2 -> Swatehost.Excel
        | _ -> Browser

    member this.IsStandalone =
        this = Swatehost.Browser || this = Swatehost.ARCitect

