module Swate.Components.ClipboardCodec

open ARCtrl
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop

[<Literal>]
let MimeType = "web application/x-swate-cells+json"

[<Literal>]
let private CurrentVersion = 1

[<Literal>]
let private HtmlPayloadAttribute = "data-swate-cells"

[<AllowNullLiteral>]
type CellDto =
    abstract Kind: string
    abstract Value: string
    abstract Name: string
    abstract TermSourceRef: string
    abstract TermAccessionNumber: string
    abstract Selector: string
    abstract Format: string
    abstract SelectorFormat: string

[<AllowNullLiteral>]
type Payload =
    abstract Version: int
    abstract Rows: CellDto[][]

type ClipboardContent = {
    PlainText: string
    Payload: Payload option
}

type private CellData = {
    Kind: string
    Value: string
    Name: string
    TermSourceRef: string
    TermAccessionNumber: string
    Selector: string
    Format: string
    SelectorFormat: string
}

let private createCell (data: CellData) =
    createObj [
        "Kind" ==> data.Kind
        "Value" ==> data.Value
        "Name" ==> data.Name
        "TermSourceRef" ==> data.TermSourceRef
        "TermAccessionNumber" ==> data.TermAccessionNumber
        "Selector" ==> data.Selector
        "Format" ==> data.Format
        "SelectorFormat" ==> data.SelectorFormat
    ]
    |> unbox<CellDto>

let ofCompositeCell (cell: CompositeCell) =
    match cell with
    | CompositeCell.FreeText value ->
        createCell {
            Kind = "freetext"
            Value = value
            Name = ""
            TermSourceRef = ""
            TermAccessionNumber = ""
            Selector = ""
            Format = ""
            SelectorFormat = ""
        }
    | CompositeCell.Term term ->
        createCell {
            Kind = "term"
            Value = ""
            Name = term.NameText
            TermSourceRef = term.TermSourceREF |> Option.defaultValue ""
            TermAccessionNumber = term.TermAccessionNumber |> Option.defaultValue ""
            Selector = ""
            Format = ""
            SelectorFormat = ""
        }
    | CompositeCell.Unitized(value, unit) ->
        createCell {
            Kind = "unitized"
            Value = value
            Name = unit.NameText
            TermSourceRef = unit.TermSourceREF |> Option.defaultValue ""
            TermAccessionNumber = unit.TermAccessionNumber |> Option.defaultValue ""
            Selector = ""
            Format = ""
            SelectorFormat = ""
        }
    | CompositeCell.Data data ->
        createCell {
            Kind = "data"
            Value = data.FilePath |> Option.defaultValue ""
            Name = ""
            TermSourceRef = ""
            TermAccessionNumber = ""
            Selector = data.Selector |> Option.defaultValue ""
            Format = data.Format |> Option.defaultValue ""
            SelectorFormat = data.SelectorFormat |> Option.defaultValue ""
        }

let toCompositeCell (cell: CellDto) =
    match cell.Kind with
    | "term" -> CompositeCell.createTermFromString (cell.Name, cell.TermSourceRef, cell.TermAccessionNumber)
    | "unitized" ->
        CompositeCell.createUnitizedFromString (cell.Value, cell.Name, cell.TermSourceRef, cell.TermAccessionNumber)
    | "data" ->
        let data = Data.empty

        data.FilePath <-
            cell.Value
            |> Option.ofObj
            |> Option.filter (System.String.IsNullOrWhiteSpace >> not)

        data.Selector <-
            cell.Selector
            |> Option.ofObj
            |> Option.filter (System.String.IsNullOrWhiteSpace >> not)

        data.Format <-
            cell.Format
            |> Option.ofObj
            |> Option.filter (System.String.IsNullOrWhiteSpace >> not)

        data.SelectorFormat <-
            cell.SelectorFormat
            |> Option.ofObj
            |> Option.filter (System.String.IsNullOrWhiteSpace >> not)

        CompositeCell.createData data
    | "freetext" -> CompositeCell.createFreeText cell.Value
    | kind -> failwith $"Unknown clipboard cell kind: {kind}"

let validateCellDto (cell: CellDto) =
    cell
    |> Option.ofObj
    |> Option.bind (fun cell ->
        match cell.Kind |> Option.ofObj with
        | Some "freetext" -> Some [| cell.Value |]
        | Some "term" ->
            Some [|
                cell.Name
                cell.TermSourceRef
                cell.TermAccessionNumber
            |]
        | Some "unitized" ->
            Some [|
                cell.Value
                cell.Name
                cell.TermSourceRef
                cell.TermAccessionNumber
            |]
        | Some "data" ->
            Some [|
                cell.Value
                cell.Selector
                cell.Format
                cell.SelectorFormat
            |]
        | _ -> None
    )
    |> Option.exists (Array.forall (Option.ofObj >> Option.isSome))

let createPayload (cells: CompositeCell[][]) =
    createObj [
        "Version" ==> CurrentVersion
        "Rows" ==> (cells |> Array.map (Array.map ofCompositeCell))
    ]
    |> unbox<Payload>

let encode payload = JS.JSON.stringify payload

let tryDecode json =
    try
        let payload = JS.JSON.parse (json) |> unbox<Payload>

        if
            isNull (box payload)
            || payload.Version <> CurrentVersion
            || isNull (box payload.Rows)
            || not (payload.Rows |> Array.forall (Array.forall validateCellDto))
        then
            None
        else
            Some payload
    with _ ->
        None

let private escapeHtml (text: string) =
    text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;")

let createHtmlRepresentation (plainText: string) (cells: CompositeCell[][]) =
    let encodedPayload = cells |> createPayload |> encode |> JS.encodeURIComponent

    let tableRows =
        plainText.TrimEnd([| '\r'; '\n' |]).Split([| "\r\n"; "\n"; "\r" |], System.StringSplitOptions.None)
        |> Array.map (fun row ->
            row.Split '\t'
            |> Array.map (fun value -> $"<td>{escapeHtml value}</td>")
            |> String.concat ""
            |> fun columns -> $"<tr>{columns}</tr>"
        )
        |> String.concat ""

    $"<table {HtmlPayloadAttribute}=\"{encodedPayload}\">{tableRows}</table>"

let tryDecodeHtml (htmlText: string) =
    if isNull htmlText then
        None
    else
        try
            htmlText
            |> ClipboardBindings.parseHtml
            |> _.querySelector($"[{HtmlPayloadAttribute}]")
            |> Option.ofObj
            |> Option.bind (fun element -> element.getAttribute HtmlPayloadAttribute |> Option.ofObj)
            |> Option.bind (JS.decodeURIComponent >> tryDecode)
        with _ ->
            None

let private hasType mimeType (item: ClipboardItem) = item.types |> Array.contains mimeType

let private tryReadPayload (item: ClipboardItem) = promise {
    try
        if hasType MimeType item then
            let! blob = item.getType MimeType
            let! json = blob.text ()
            return tryDecode json
        elif hasType "text/html" item then
            let! blob = item.getType "text/html"
            let! htmlText = blob.text ()
            return tryDecodeHtml htmlText
        else
            return None
    with _ ->
        return None
}

let write (plainText: string) (cells: CompositeCell[][] option) = promise {
    match cells with
    | None -> do! Swate.Components.GlobalBindings.navigator.clipboard.writeText plainText
    | Some cells ->
        let clipboard = Swate.Components.GlobalBindings.navigator.clipboard
        let htmlText = createHtmlRepresentation plainText cells
        let payloadText = cells |> createPayload |> encode

        try
            do!
                ClipboardBindings.createItemWithTypedContent plainText MimeType payloadText htmlText
                |> Array.singleton
                |> clipboard.write
        with _ ->
            try
                do!
                    ClipboardBindings.createItemWithHtmlContent plainText htmlText
                    |> Array.singleton
                    |> clipboard.write
            with _ ->
                do! clipboard.writeText plainText
}

let read () = promise {
    let clipboard = Swate.Components.GlobalBindings.navigator.clipboard

    try
        let! items = clipboard.read ()

        let item =
            items
            |> Array.tryFind (fun item -> hasType "text/plain" item && hasType MimeType item)
            |> Option.orElseWith (fun () ->
                items
                |> Array.tryFind (fun item -> hasType "text/plain" item && hasType "text/html" item)
            )
            |> Option.orElseWith (fun () -> items |> Array.tryFind (hasType "text/plain"))

        match item with
        | None ->
            return!
                clipboard.readText ()
                |> Promise.map (fun plainText -> {
                    PlainText = plainText
                    Payload = None
                })
        | Some item ->
            let! plainBlob = item.getType "text/plain"
            let! plainText = plainBlob.text ()

            let! payload = tryReadPayload item

            return {
                PlainText = plainText
                Payload = payload
            }
    with _ ->
        let! plainText = clipboard.readText ()

        return {
            PlainText = plainText
            Payload = None
        }
}
