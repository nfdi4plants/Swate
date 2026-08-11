namespace Components

open Feliz
open Types
open Fable.SimpleJson
open System.Text.RegularExpressions
open ARCtrl
open Fable.Core.JsInterop
open Browser.Dom
open Fable.Core
open Swate.Components.Primitive
open Swate.Components.Primitive.LoadingSpinner

type FileDisplay =

    [<ReactComponent>]
    
    static member DisplayHtml(htmlString: string, elementID: string, isLocalStorageClear) =
        Html.div [
            prop.className "swt:flex swt:w-full swt:justify-center"
            prop.children [
                Html.div [
                    prop.custom ("data-theme", "light")
                    prop.dangerouslySetInnerHTML htmlString
                    prop.className
                        "swt:prose swt:p-2 swt:pb-24 swt:rounded-lg swt:w-full swt:max-w-none swt:bg-base-300
                        swt:box-border swt:[&_pre]:box-border swt:[&_code]:box-border swt:[&_pre]:whitespace-pre-wrap 
                        swt:[&_code]:whitespace-pre-wrap swt:[&_pre]:wrap-break-word swt:[&_code]:wrap-break-word"
                    prop.id elementID
                ]
            ]
        ]

    [<ReactComponent>]
    //  https://stackoverflow.com/a/60539836/12858021
    static member DisplayPDF filehtml setNumPages (numPages: int option) (elementID: string)=

        let textRender =
            React.useCallback (
                (fun text ->
                    let mutable txt = text?str
                    txt
                )
            )

        Html.div [
            prop.className "swt:flex swt:w-full swt:justify-center"
            prop.id elementID
            prop.children [
                ReactElements.Document(
                    filehtml,
                    (fun (props: {| numPages: int |}) -> setNumPages (Some props.numPages)),
                    //virtualize this list
                    [
                        for i in 1 .. numPages |> Option.defaultValue 1 do
                            ReactElements.Page(i, 750, textRender, $"page-{i}")
                    ],
                    externalLinkTarget = "_blank",
                    onLoadError = (fun e -> Browser.Dom.console.error ("Error loading PDF:", e)),
                    loading = LoadingSpinner.LoadingSpinner("Loading PDF", size = DaisyuiSize.XL)
                )
            ]
        ]
