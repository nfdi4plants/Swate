namespace DataAnnotator

[<RequireQualifiedAccess>]
type TargetColumn =
    | Input
    | Output
    | Autodetect

    static member fromString (str: string) =
        match str.ToLower() with
        | "input" -> Input
        | "output" -> Output
        | _ -> Autodetect

[<RequireQualifiedAccess>]
type DataTarget =
    | Cell of columnIndex:int * rowIndex: int
    | Row of int
    | Column of int

    member this.ToFragmentSelectorString(hasHeader: bool) =
        let rowOffset = if hasHeader then 2 else 1 // header counts and is 1-based
        match this with
        | Row ri -> sprintf "row=%i" (ri + rowOffset)
        | Column ci -> sprintf "col=%i" ci
        | Cell (ci, ri) -> sprintf "cell=%i,%i" ri ci

    member this.ToReactKey() =
        match this with
        | Row ri -> sprintf "row-%i" ri
        | Column ci -> sprintf "col-%i" ci
        | Cell (ci, ri) -> sprintf "cell-%i-%i" ri ci

type DataFile = {
    DataFileName: string
    DataFileType: string
    DataContent: string
    DataSize: float
} with
    static member create(dfn, dft, dc, ds) = {
        DataFileName = dfn
        DataFileType = dft
        DataContent = dc
        DataSize = ds
    }

    member this.ExpectedSeparator =
        if this.DataFileType.Contains("csv") then
            ","
        elif this.DataFileType.Contains("tsv") then
            "\t"
        else
            ","

type ParsedDataFile = {
    HeaderRow: string [] option
    BodyRows: string [] []
} with
    static member fromFileBySeparator (separator: string) (file: DataFile) =
        let rows = file.DataContent.Split("\n")
        let splitRows = rows |> Array.map (fun row -> row.Split(separator))
        if splitRows.Length > 1 then
            let headers = Some splitRows.[0]
            let data = splitRows.[1..]
            let parsedFile : ParsedDataFile = {
                HeaderRow = headers
                BodyRows = data
            }
            parsedFile
        else
            let parsedFile : ParsedDataFile = {
                HeaderRow = None
                BodyRows = splitRows
            }
            parsedFile

    member this.ToggleHeader() =
        match this.HeaderRow with
        | Some header -> { this with HeaderRow = None; BodyRows = Array.insertAt 0 header this.BodyRows }
        | None when this.BodyRows.Length > 1 ->
            {
                this with
                    HeaderRow = Some this.BodyRows.[0]
                    BodyRows = this.BodyRows.[1..]
            }
        | _ -> this

type Model =
    {
        DataFile: DataFile option
        ParsedFile: ParsedDataFile option
    }
    static member init () = {
        DataFile = None
        ParsedFile = None
    }

type Msg =
    | UpdateDataFile of DataFile option
    | ToggleHeader
    | UpdateSeperator of string