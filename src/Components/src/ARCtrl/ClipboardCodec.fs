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

type ClipboardRepresentations = { PlainText: string; HtmlText: string }

let private createCell kind value name termSourceRef termAccessionNumber selector format selectorFormat =
    createObj [
        "Kind" ==> kind
        "Value" ==> value
        "Name" ==> name
        "TermSourceRef" ==> termSourceRef
        "TermAccessionNumber" ==> termAccessionNumber
        "Selector" ==> selector
        "Format" ==> format
        "SelectorFormat" ==> selectorFormat
    ]
    |> unbox<CellDto>

let ofCompositeCell (cell: CompositeCell) =
    match cell with
    | CompositeCell.FreeText value -> createCell "freetext" value "" "" "" "" "" ""
    | CompositeCell.Term term ->
        createCell
            "term"
            ""
            term.NameText
            (term.TermSourceREF |> Option.defaultValue "")
            (term.TermAccessionNumber |> Option.defaultValue "")
            ""
            ""
            ""
    | CompositeCell.Unitized(value, unit) ->
        createCell
            "unitized"
            value
            unit.NameText
            (unit.TermSourceREF |> Option.defaultValue "")
            (unit.TermAccessionNumber |> Option.defaultValue "")
            ""
            ""
            ""
    | CompositeCell.Data data ->
        createCell
            "data"
            (data.FilePath |> Option.defaultValue "")
            ""
            ""
            ""
            (data.Selector |> Option.defaultValue "")
            (data.Format |> Option.defaultValue "")
            (data.SelectorFormat |> Option.defaultValue "")

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

let tryToCompositeCell (cell: CellDto) =
    try
        let require (value: string) =
            value
            |> Option.ofObj
            |> Option.defaultWith (fun () -> failwith "Missing clipboard cell field")

        match cell.Kind with
        | "freetext" -> require cell.Value |> ignore
        | "term" ->
            [|
                cell.Name
                cell.TermSourceRef
                cell.TermAccessionNumber
            |]
            |> Array.iter (require >> ignore)
        | "unitized" ->
            [|
                cell.Value
                cell.Name
                cell.TermSourceRef
                cell.TermAccessionNumber
            |]
            |> Array.iter (require >> ignore)
        | "data" ->
            [|
                cell.Value
                cell.Selector
                cell.Format
                cell.SelectorFormat
            |]
            |> Array.iter (require >> ignore)
        | kind -> failwith $"Unknown clipboard cell kind: {kind}"

        cell |> toCompositeCell |> Some
    with _ ->
        None

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
            || not (
                payload.Rows
                |> Array.forall (Array.forall (tryToCompositeCell >> Option.isSome))
            )
        then
            None
        else
            Some payload
    with _ ->
        None

let private escapeHtml (text: string) =
    text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;")

let createRepresentations (plainText: string) (cells: CompositeCell[][]) =
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

    {
        PlainText = plainText
        HtmlText = $"<table {HtmlPayloadAttribute}=\"{encodedPayload}\">{tableRows}</table>"
    }

let tryDecodeHtml (htmlText: string) =
    let marker = $"{HtmlPayloadAttribute}=\""

    if isNull htmlText then
        None
    else
        try
            let payloadStart = htmlText.IndexOf marker

            if payloadStart < 0 then
                None
            else
                let payloadStart = payloadStart + marker.Length
                let payloadEnd = htmlText.IndexOf('"', payloadStart)

                if payloadEnd < 0 then
                    None
                else
                    htmlText.Substring(payloadStart, payloadEnd - payloadStart)
                    |> JS.decodeURIComponent
                    |> tryDecode
        with _ ->
            None

let write (plainText: string) (cells: CompositeCell[][] option) = promise {
    match cells with
    | None -> do! Swate.Components.GlobalBindings.navigator.clipboard.writeText plainText
    | Some cells ->
        let clipboard = Swate.Components.GlobalBindings.navigator.clipboard
        let representations = createRepresentations plainText cells
        let payloadText = cells |> createPayload |> encode

        try
            do!
                ClipboardBindings.createItemWithTypedContent
                    representations.PlainText
                    MimeType
                    payloadText
                    representations.HtmlText
                |> Array.singleton
                |> clipboard.write
        with _ ->
            try
                do!
                    ClipboardBindings.createItemWithHtmlContent representations.PlainText representations.HtmlText
                    |> Array.singleton
                    |> clipboard.write
            with _ ->
                do! clipboard.writeText representations.PlainText
}

let read () = promise {
    let! plainText = Swate.Components.GlobalBindings.navigator.clipboard.readText ()

    try
        let clipboard = Swate.Components.GlobalBindings.navigator.clipboard
        let! items = clipboard.read ()

        let typedItem =
            items |> Array.tryFind (fun item -> item.types |> Array.contains MimeType)

        match typedItem with
        | Some item ->
            let! blob = item.getType MimeType
            let! json = blob.text ()

            return {
                PlainText = plainText
                Payload = tryDecode json
            }
        | None ->
            let htmlItem =
                items |> Array.tryFind (fun item -> item.types |> Array.contains "text/html")

            match htmlItem with
            | Some item ->
                let! blob = item.getType "text/html"
                let! htmlText = blob.text ()

                return {
                    PlainText = plainText
                    Payload = tryDecodeHtml htmlText
                }
            | None ->
                return {
                    PlainText = plainText
                    Payload = None
                }
    with _ ->
        return {
            PlainText = plainText
            Payload = None
        }
}
