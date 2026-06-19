namespace Components

open Feliz
open Types
open Fable.SimpleJson
open System.Text.RegularExpressions
open ARCtrl
open Fable.Core.JsInterop
open Browser.Dom
open Fable.Core

open System.Threading.Tasks

module PDFjs =

  importSideEffects "react-pdf/dist/Page/TextLayer.css"
  importSideEffects "react-pdf/dist/Page/AnnotationLayer.css"
  emitJsStatement () """import { pdfjs } from 'react-pdf';

pdfjs.GlobalWorkerOptions.workerSrc = new URL(
  'pdfjs-dist/build/pdf.worker.min.mjs',
  import.meta.url,
).toString();"""

module RemarkImport =

  [<Import("default", "rehype-raw")>]
  let rehypeRaw: obj = jsNative
  
  [<Import("default", "rehype-stringify")>]
  let rehypeStringify: obj = jsNative

  [<Import("default", "remark-gfm")>]
  let remarkGfm: obj = jsNative
  
  [<Import("default", "remark-parse")>]
  let remarkParse: obj = jsNative

  [<Import("default", "remark-rehype")>]
  let remarkRehype: obj = jsNative

  [<Import("unified", "unified")>]
  let unified: unit -> obj  = jsNative


  // let rehypeStringify: obj = importDefault "rehype-stringify"
  // let remarkGfm: obj = importDefault "remark-gfm"
  // let remarkParse: obj = importDefault "remark-parse"
  // let remarkRehype: obj = importDefault "remark-rehype"
  // let unified: obj -> unit = importMember "unified"

// type Remark =

//   static member rehypeRaw: obj = jsNative
//   static member rehypeStringify: obj = jsNative
//   static member remarkGfm: obj = jsNative
//   static member remarkParse: obj = jsNative
//   static member remarkRehype: obj = jsNative
//   static member unified: unit -> unit = jsNative

type ReactElements =
  [<ReactComponent(import="Document", from="react-pdf")>]
  static member Document (file: string, onLoadSuccess: {|numPages: int|} -> unit, children: ReactElement list, ?externalLinkTarget: string) = React.Imported()

  [<ReactComponent(import="Page", from="react-pdf")>]
  static member Page (pageNumber: int, width: int, customTextRenderer:'c -> string, ?key: string) = React.Imported()

module private FileReaderHelper =
  open Fable.Core
  open Fable.Core.JsInterop

  [<Emit("new FileReader()")>]
  let newFileReader(): Browser.Types.FileReader = jsNative

  let readDocx (file: Browser.Types.File) setState setLocalFile = 
    let reader = newFileReader()
    reader.onload <- fun e ->
      let arrayBuffer = e.target?result
      promise {
        let! r = Mammoth.mammoth.convertToHtml({|arrayBuffer = arrayBuffer|})
        (Docx r.value)
        |> fun t ->
          t |> setState
          t |> setLocalFile "file"
      }
      |> Promise.start

    reader.onerror <- fun e ->
      Browser.Dom.console.error ("Error reading file", e)
    reader.readAsArrayBuffer(file)
 

  let inline usePlugin (plugin: obj) (instance: obj): obj = instance?``use``(plugin)
  let inline usePluginWithOpts (plugin: obj) (opts: obj) (instance: obj): obj = instance?``use``(plugin, opts)

  // Bind `process` properly via Emit
  [<Emit("$0.process($1)")>]
  let processUnified (processor: obj) (markdown: string) : JS.Promise<obj> = jsNative

  let processMarkdown (markdown: string): JS.Promise<obj> =
      let processor =
          RemarkImport.unified()
          |> usePlugin RemarkImport.remarkParse
          |> usePlugin RemarkImport.remarkGfm
          |> usePluginWithOpts RemarkImport.remarkRehype {| allowDangerousHtml = true |}
          |> usePlugin RemarkImport.rehypeRaw
          |> usePlugin RemarkImport.rehypeStringify

      promise {
          let! result = processUnified processor markdown
          return result 
      }

  let readTxt (file: Browser.Types.File) setState setLocalFile = 
    let reader = newFileReader()
    reader.onload <- fun e ->
      let text = reader.result |> unbox<string>
      let fileEnding = file.name.Split('.').[1]
      if fileEnding = "md" then 
        let prom = processMarkdown text
        prom.``then``(fun result ->
          let markdownString = result?value
          setState (Txt markdownString)
          setLocalFile "file" (Txt markdownString) 
        ) |> Promise.start
      else 
        setState (Txt (text.ToString()))
        setLocalFile "file" (Txt (text.ToString()))

    reader.onerror <- fun e ->
      Browser.Dom.console.error ("Error reading file", e)
    reader.readAsText(file)

  let readPdf (file: Browser.Types.File) setState setLocalFile = //put pdf to html string converter
    let reader = newFileReader()
    reader.onload <- fun e ->
      let base64 = reader.result
      setState (PDF (base64.ToString()))
      setLocalFile "file" (PDF (base64.ToString()))

    reader.readAsDataURL(file); // Converts to base64

  let readFromFile (file: Browser.Types.File) setState (fileType: UploadFileType) setLocalFile =
    match fileType with
    | UploadFileType.Docx -> readDocx file setState setLocalFile
    | UploadFileType.PDF -> readPdf file setState setLocalFile
    | UploadFileType.Txt -> readTxt file setState setLocalFile

module Lists =

  let keyList (highlight: Highlight)= 
    highlight.Keys
    |> Map.toArray
    |> Array.map snd

  let termList (highlight: Highlight)= 
    highlight.Terms
    |> Map.toArray
    |> Array.map snd

  let valueList (highlight: Highlight)= 
    highlight.Values
    |> Map.toArray
    |> Array.map snd

  // let termlist (annoList: Annotation list) = 
  //   annoList
  //   |> List.collect (fun a -> a.Highlight.Terms |> Map.toList |> List.map snd)
  //   |> List.toArray
  //   |> Array.filter (fun term -> term <> "")

    

  // let valuelist (annoList: Annotation list) =
  //   annoList
  //   |> List.map (fun a -> a.HighlightValues)
  //   |> List.toArray
  //   |> Array.filter (fun a -> a <> "")

type FileUpload =
    static member DisplayHtml(htmlString: string, highList: Highlight, elementID: string, isLocalStorageClear) = 
      Html.div [
        prop.className "swt:flex swt:justify-end"
        prop.children [
          PaperWithMarker.Main(htmlString, Lists.keyList highList, Lists.termList highList, Lists.valueList highList, elementID, isLocalStorageClear)
        ]
      ]

    [<ReactComponent>]
  //  https://stackoverflow.com/a/60539836/12858021
    static member DisplayPDF filehtml setNumPages (numPages: int option) (elementID: string) (highList: Highlight)  =

      let highlightPattern(text: string, anno: string, colorcode) = 
        text.Replace(anno, sprintf "<mark style='background-color: %s'>%s</mark>" colorcode anno)
      // #ffe699
      // #4fb3d9

      let textRender =
        React.useCallback(
          (fun text -> 
            let mutable txt = text?str
            for a in Lists.keyList highList do
              txt <- highlightPattern(txt, a, "#ffe699")
            for a in Lists.termList highList do
              txt <- highlightPattern(txt, a, "#4fb3d9")
            for a in Lists.valueList highList do
              txt <- highlightPattern(txt, a, "#4fd984")
            txt
          ),
          [|box highList|]
        )

      Html.div [
        // prop.className "mt-5"
        prop.id elementID
        prop.children [
          ReactElements.Document(
            filehtml, 
            (fun (props: {|numPages: int|}) -> 
              setNumPages (Some props.numPages)), 
            //virtualize this list
            [
              for i in 1 .. numPages |> Option.defaultValue 1 do
                ReactElements.Page(
                  i, 
                  750,
                  textRender,
                  "1")
            ],
            externalLinkTarget = "_blank"
          ) 
          Html.p [
              prop.text (
                  match numPages with
                  | Some np -> ""
                  | None -> "Loading..."
              )
          ]
        ]
      ]

    [<ReactComponent>]
    static member private FileInput setState setFilehtml setLocalFile setFileName setLocalFileName=
      let ref = React.useInputRef()
      Html.input [
        prop.className "swt:file-input swt:join-item"
        prop.ref ref
        prop.type'.file
        prop.accept ".docx, .pdf, .txt, .md"
        prop.onChange (fun (f: Browser.Types.File) -> 
          let fileType =
            match f.``type`` with
            |"application/vnd.openxmlformats-officedocument.wordprocessingml.document" -> UploadFileType.Docx
            |"application/pdf" -> UploadFileType.PDF
            | _-> UploadFileType.Txt

          log fileType
          FileReaderHelper.readFromFile f setFilehtml fileType setLocalFile
          if ref.current.IsSome then
            ref.current.Value.value <- null

          setFileName f.name
          setLocalFileName "fileName" f.name
          setState []
        )
      ]

    static member private RemoveUploadedFileButton (setFilehtml, setLocalFile, setState, setFileName, setLocalFileName) =
      Html.button [
        prop.className "swt:btn swt:btn-error swt:btn-block"
        prop.onClick (fun e -> 
          setFilehtml Unset
          setLocalFile "file" Unset

          setState []

          setFileName ""
          setLocalFileName "fileName" ""
        )
        prop.children [
          Html.span [
            Html.i [
              prop.className "swt:fa-solid swt:fa-trash-can"
            ]
          ]
        ]
      ]

    /// <summary>
    /// A stateful React component that maintains a counter
    /// </summary>
    [<ReactComponent>]
    static member UploadDisplay(filehtml, setFilehtml, setState, setFileName, setLocalFileName) =
    
        // let uploadFileType, setUploadFileType = React.useState(UploadFileType.PDF)

        let setLocalFile (id: string) (nextFile: UploadedFile) =
            let JSONString = Json.stringify nextFile 
            Browser.WebStorage.localStorage.setItem(id, JSONString)

        Html.div [
          prop.className "swt:flex swt:flex-col swt:gap-2"
          prop.children [
            Html.div [
              
              prop.children [
                // FileUpload.FileTypeSelect setUploadFileType
                FileUpload.FileInput setState setFilehtml setLocalFile setFileName setLocalFileName
                Html.h1 [
                  prop.className "swt:mt-2 swt:text-gray-600"
                  prop.text "compatible filetypes: .pdf | .docx | .md | .txt"
                ]
              ]
            ]
            match filehtml with
            | Unset -> Html.div []
            | _ ->
              FileUpload.RemoveUploadedFileButton(
                setFilehtml, setLocalFile, setState, setFileName, setLocalFileName
              )
          ]
        ]

