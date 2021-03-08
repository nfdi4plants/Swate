module DwnButton

open Fable.Core
open Fable.Core.JsInterop
open Browser
open Browser.Dom
open Browser.Blob
open Fulma

open Model
open Messages
open Fable.React
open Fable.Core

// Copied from Fable.Browser.URL because it is not available in the REPL
// But please include Fable.Broser.URL in your project
type [<AllowNullLiteral>] URL =
    abstract hash: string with get, set
    abstract host: string with get, set
    abstract hostname: string with get, set
    abstract href: string with get, set
    abstract origin: string
    abstract password: string with get, set
    abstract pathname: string with get, set
    abstract port: string with get, set
    abstract protocol: string with get, set
    abstract search: string with get, set
    abstract username: string with get, set
    // abstract searchParams: URLSearchParams
    abstract toString: unit -> string
    abstract toJSON: unit -> string

type [<AllowNullLiteral>] URLType =
    [<Emit("new $0($1...)")>] abstract Create: url: string -> URL
    /// Returns a DOMString containing a unique blob URL, that is a URL with blob: as its scheme, followed by an opaque string uniquely identifying the object in the browser.
    abstract createObjectURL: obj -> string
    /// Revokes an object URL previously created using URL.createObjectURL().
    abstract revokeObjectURL: string -> unit

module Url =
    let [<Global>] URL: URLType = jsNative


let blobOptions =
    jsOptions<Types.BlobPropertyBag>(fun o ->
        o.``type`` <- "text/csv"
    )

// download property seems to be missing the binding definition so we use dynamic typing

// Sadly this also does not work in local excel
let dwnButton (model:Model) dispatch csvData =
    Button.a [
        Button.IsFullWidth
        Button.Color IsInfo
        Button.OnClick (fun e ->
            let blob = Blob.Create([| box csvData |], blobOptions)
            let elem = window.document.createElement("a") :?> Types.HTMLAnchorElement
            elem.href <- Url.URL.createObjectURL(box blob)

            elem?download <- "filename.csv"
            document.body.appendChild(elem) |> ignore
            elem.click()
            document.body.removeChild(elem) |> ignore
        )
    ][
        str "Download"
    ]

open Fable.Core.JS

/// Client Side Download; This works online but not in local Excel
let text = encodeURIComponent("This is a test input")
let csvData =
    "col1;col2\n1;2\n3;4"

//a [Href (sprintf """data:text/plain;charset=utf-8,%s""" text); Download "Texti.txt"; Target "_Blank"][str "Click me"]
//Button.button [
//    Button.Color Color.IsLink
//    Button.IsFullWidth
//    Button.OnClick (fun e ->
//        let iframeId = "dwn-iframe"
//        // https://ourcodeworld.com/articles/read/189/how-to-create-a-file-and-generate-a-download-with-javascript-in-the-browser-without-a-server
//        // https://stackoverflow.com/questions/3665115/how-to-create-a-file-in-memory-for-user-to-download-but-not-through-server
//        // https://stackoverflow.com/questions/61323775/how-to-open-a-link-in-the-standard-browser-from-an-office-addin/65594049#65594049

//        //let element = Browser.Dom.document.getElementById "frame-dwn"

//        let iframe =
//            let iframe = Browser.Dom.document.createElement("iframe")
//            iframe.setAttribute("sandbox", "allow-top-navigation allow-downloads allow-same-origin")
//            iframe.setAttribute("id", iframeId)
//            iframe

//        let downloadInIFrame (iframe:HTMLElement) =
//            let p = iframe :?> Browser.Types.HTMLIFrameElement
//            let a = p.contentWindow.document.createElement ("a")
//            a.setAttribute ("href", sprintf """data:text/plain;charset=utf-8,%s""" text)
//            a.setAttribute("download", "TestFile.txt")
//            a.innerText <- "Click me"
//            let _ = p.contentWindow.document.body.appendChild a
//            //a.click()
//            //let _ = p.contentWindow.document.body.removeChild a
//            ()

//        downloadInIFrame iframe
//        //element.setAttribute("srcdoc", "<div></div>")
//        //let _ = Browser.Dom.document.body.removeChild(element)
//        ()
//    )
//    Button.Props [Style [MarginBottom "1rem"]]
//][
//    str "Download Activity Log"
//]