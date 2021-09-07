module XLSXConverterView

open Fable.React
open Fable.React.Props
open Browser.Types
open Fable.Core.JsInterop
open Fable.FontAwesome
open Fulma
open ExcelColors
open Model
open Messages

let fileUploadButton (model:Model) dispatch id =
    Label.label [Label.Props [Style [FontWeight "normal";MarginBottom "0.5rem"]]][
        Input.input [
            Input.Props [
                Id id
                Type "file"; Style [Display DisplayOptions.None]
                OnChange (fun ev ->
                    let files : FileList = ev.target?files

                    let blobs =
                        [ for i=0 to (files.length - 1) do yield files.item i ]
                        |> List.map (fun f -> f.slice() )

                    let reader = Browser.Dom.FileReader.Create()

                    reader.onload <- fun evt ->
                        let byteArr =
                            let arraybuffer : Fable.Core.JS.ArrayBuffer = evt.target?result
                            let uintArr = Fable.Core.JS.Constructors.Uint8Array.Create arraybuffer
                            uintArr.ToString().Split([|","|], System.StringSplitOptions.RemoveEmptyEntries)
                            |> Array.map (fun byteStr -> byte byteStr)
                            
                        StoreXLSXByteArray byteArr |> XLSXConverterMsg |> dispatch
                                   
                    reader.onerror <- fun evt ->
                        GenericLog ("Error", evt.Value) |> Dev |> dispatch

                    reader.readAsArrayBuffer(blobs |> List.head)

                    let picker = Browser.Dom.document.getElementById(id)
                    // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                    picker?value <- null
                )
            ]
        ]
        Button.a [Button.Color Color.IsInfo; Button.IsFullWidth][
            str "Upload Excel file"
        ]
    ]

let textFieldEle (model:Model) dispatch =
    Columns.columns [
        Columns.IsMobile
        Columns.Props [Style [MarginBottom "0.5rem"]]
    ][
        Column.column [][
            Textarea.textarea [
                Textarea.IsReadOnly true
                Textarea.Value model.XLSXJSONResult.[0 .. 500]
            ][]
        ]
        Column.column [Column.Width (Screen.All, Column.IsNarrow)] [
            Field.div [][
                Button.a [
                    Button.Props [
                        Style [Width "40.5px"]
                        Title "Copy to Clipboard"
                    ]
                    Button.Color IsInfo
                    Button.OnClick (fun e ->
                        CustomComponents.ResponsiveFA.triggerResponsiveReturnEle "clipboard_settingsDataSteward"
                        let txt = model.XLSXJSONResult
                        let textArea = Browser.Dom.document.createElement "textarea"
                        textArea?value <- txt
                        textArea?style?top <- "0"
                        textArea?style?left <- "0"
                        textArea?style?position <- "fixed"

                        Browser.Dom.document.body.appendChild textArea |> ignore

                        textArea.focus()
                        /// Can't belive this actually worked
                        textArea?select()

                        let t = Browser.Dom.document.execCommand("copy")
                        Browser.Dom.document.body.removeChild(textArea) |> ignore
                        ()
                    )
                ][
                    CustomComponents.ResponsiveFA.responsiveReturnEle "clipboard_settingsDataSteward" Fa.Regular.Clipboard Fa.Solid.Check
                ]
            ]
        ]
    ]

let xlsxConverterMainView (model:Model) dispatch =
    let inputId = "xlsxConverter_uploadButton"

    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        Field.div [][
            Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "XLSX Upload and Convertion"]

            /// Box 1
            Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Upload Swate conform XLSX file."]

            fileUploadButton model dispatch inputId

            Button.button [
                Button.IsFullWidth
                Button.Color IsSuccess
                Button.OnClick (fun e ->
                    GetAssayJsonRequest model.XLSXByteArray |> XLSXConverterMsg |> dispatch
                )
                Button.Props [Style [FontWeight "normal";MarginBottom "0.5rem"]]
            ][
                str "Parse XLSX to ISA-JSON Assay"
            ]

            textFieldEle model dispatch
        ]
        
    ]