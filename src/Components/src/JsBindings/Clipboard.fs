namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop

[<Erase>]
type ClipboardBlob =
    abstract member text: unit -> JS.Promise<string>

[<Erase>]
type ClipboardItem =
    abstract member types: string[]
    abstract member getType: string -> JS.Promise<ClipboardBlob>

[<Erase>]
type Clipboard =
    abstract member writeText: string -> JS.Promise<unit>
    abstract member readText: unit -> JS.Promise<string>
    abstract member write: ClipboardItem[] -> JS.Promise<unit>
    abstract member read: unit -> JS.Promise<ClipboardItem[]>

[<Erase>]
type Navigator =
    abstract member clipboard: Clipboard

[<AutoOpen>]
module GlobalBindings =

    [<Emit("navigator")>]
    let navigator: Navigator = jsNative

module ClipboardBindings =

    [<Emit("typeof DOMParser !== 'undefined'")>]
    let isHtmlParserAvailable: bool = jsNative

    [<Emit("new DOMParser().parseFromString($0, 'text/html')")>]
    let parseHtml (_htmlText: string) : Browser.Types.Document = jsNative

    [<Emit("new Blob([$0], { type: $1 })")>]
    let createBlob (_value: string) (_mimeType: string) : obj = jsNative

    [<Emit("new ClipboardItem($0)")>]
    let private createItem (_values: obj) : ClipboardItem = jsNative

    let createItemWithTypedContent plainText mimeType typedText htmlText =
        createObj [
            "text/plain" ==> createBlob plainText "text/plain"
            mimeType ==> createBlob typedText mimeType
            "text/html" ==> createBlob htmlText "text/html"
        ]
        |> createItem

    let createItemWithHtmlContent plainText htmlText =
        createObj [
            "text/plain" ==> createBlob plainText "text/plain"
            "text/html" ==> createBlob htmlText "text/html"
        ]
        |> createItem
