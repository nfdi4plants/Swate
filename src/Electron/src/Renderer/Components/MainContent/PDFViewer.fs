module Renderer.Components.MainContent.PDFViewer

open Feliz
open Fable.Core.JsInterop
open Fable.Core
open Swate.Components.Primitive
open Swate.Components.Primitive.LoadingSpinner

module PDFjs =

    importSideEffects "react-pdf/dist/Page/TextLayer.css"
    importSideEffects "react-pdf/dist/Page/AnnotationLayer.css"

    emitJsStatement
        ()
        """import { pdfjs } from 'react-pdf';

pdfjs.GlobalWorkerOptions.workerSrc = `https://unpkg.com/pdfjs-dist@${pdfjs.version}/build/pdf.worker.min.mjs`;"""

module private PDFData =

    [<Emit("new Uint8Array([...atob($0)].map(c => c.charCodeAt(0)))")>]
    let fromBase64 (value: string) : obj = jsNative

type ReactElements =
    [<ReactComponent(import = "Document", from = "react-pdf")>]
    static member Document
        (
            file: obj,
            onLoadSuccess: {| numPages: int |} -> unit,
            children: ReactElement list,
            ?externalLinkTarget: string,
            ?onLoadError: exn -> unit,
            ?loading: ReactElement
        ) =
        React.Imported()

    [<ReactComponent(import = "Page", from = "react-pdf")>]
    static member Page(pageNumber: int, width: int, customTextRenderer: 'c -> string, ?key: string) = React.Imported()

type DisplayPDF =

    [<ReactComponent>]

    static member Main(filehtml) =

        let (numPages: int option), setNumPages = React.useState (None)
        let pdfSource = {| data = PDFData.fromBase64 filehtml |}

        let textRender =
            React.useCallback (
                (fun text ->
                    let mutable txt = text?str
                    txt
                )
            )

        Html.div [
            prop.className "swt:flex swt:w-full swt:justify-center swt:overflow-auto swt:p-4"
            prop.children [
                ReactElements.Document(
                    pdfSource,
                    (fun (props: {| numPages: int |}) -> setNumPages (Some props.numPages)),

                    [
                        for i in 1 .. numPages |> Option.defaultValue 1 do
                            ReactElements.Page(i, 1200, textRender, $"page-{i}")
                    ],
                    externalLinkTarget = "_blank",
                    onLoadError = (fun e -> Browser.Dom.console.error ("Error loading PDF:", e)),
                    loading = LoadingSpinner.LoadingSpinner("Loading PDF", size = DaisyuiSize.XL)
                )
            ]
        ]
