[<AutoOpen>]
module Host

[<RequireQualifiedAccess>]
type ARCitISAhost =
    | Browser
    | Excel
    | ARCitect

    static member ofQueryParam(queryInteger: int option) =
        match queryInteger with
        | Some 1 -> ARCitISAhost.ARCitect
        | Some 2 -> ARCitISAhost.Excel
        | _ -> Browser

    member this.IsStandalone = this = ARCitISAhost.Browser || this = ARCitISAhost.ARCitect
