module Swate.Components.ClipboardCodec

open ARCtrl
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop

[<Literal>]
let MimeType = "web application/x-swate-cells+json"

[<Literal>]
let private PlainTextMimeType = "text/plain"

[<Literal>]
let private CurrentVersion = 1

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

[<Emit("new Blob([$0], { type: $1 })")>]
let private createBlob (_content: string) (_mimeType: string) : obj = jsNative

[<Emit("new ClipboardItem($0)")>]
let private createClipboardItem (_content: obj) : ClipboardItem = jsNative

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
        CompositeCell.createUnitizedFromString (
            cell.Value,
            cell.Name,
            cell.TermSourceRef,
            cell.TermAccessionNumber
        )
    | "data" ->
        let data = Data.empty
        data.FilePath <- cell.Value |> Option.ofObj |> Option.filter (System.String.IsNullOrWhiteSpace >> not)
        data.Selector <- cell.Selector |> Option.ofObj |> Option.filter (System.String.IsNullOrWhiteSpace >> not)
        data.Format <- cell.Format |> Option.ofObj |> Option.filter (System.String.IsNullOrWhiteSpace >> not)
        data.SelectorFormat <- cell.SelectorFormat |> Option.ofObj |> Option.filter (System.String.IsNullOrWhiteSpace >> not)
        CompositeCell.createData data
    | _ -> CompositeCell.createFreeText cell.Value

let createPayload (cells: CompositeCell[][]) =
    createObj [
        "Version" ==> CurrentVersion
        "Rows" ==> (cells |> Array.map (Array.map ofCompositeCell))
    ]
    |> unbox<Payload>

let encode payload = JS.JSON.stringify payload

let tryDecode json =
    try
        let payload = JS.JSON.parse(json) |> unbox<Payload>

        if isNull (box payload) || payload.Version <> CurrentVersion || isNull (box payload.Rows) then
            None
        else
            Some payload
    with _ ->
        None

let write (plainText: string) (cells: CompositeCell[][] option) = promise {
    match cells with
    | None -> do! Swate.Components.GlobalBindings.navigator.clipboard.writeText plainText
    | Some cells ->
        try
            let clipboard = Swate.Components.GlobalBindings.navigator.clipboard
            let content =
                createObj [
                    PlainTextMimeType ==> createBlob plainText PlainTextMimeType
                    MimeType ==> (cells |> createPayload |> encode |> fun json -> createBlob json MimeType)
                ]

            do! clipboard.write [| createClipboardItem content |]
        with _ ->
            do! Swate.Components.GlobalBindings.navigator.clipboard.writeText plainText
}

let read () = promise {
    let! plainText = Swate.Components.GlobalBindings.navigator.clipboard.readText ()

    try
        let clipboard = Swate.Components.GlobalBindings.navigator.clipboard
        let! items = clipboard.read ()

        let item = items |> Array.tryFind (fun item -> item.types |> Array.contains MimeType)

        match item with
        | Some item ->
            let! blob = item.getType MimeType
            let! json = blob.text ()
            return { PlainText = plainText; Payload = tryDecode json }
        | None -> return { PlainText = plainText; Payload = None }
    with _ ->
        return { PlainText = plainText; Payload = None }
}
