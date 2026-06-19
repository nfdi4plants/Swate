namespace Components

open Feliz
open Browser.Dom
open Browser.Types
open Types
open Fable.SimpleJson
open Fable.Core.JS
open FSharp.Collections
open System
open ARCtrl
open Fable.Core.JsInterop

module List =
    let rec removeAt index list =
        match index, list with
        | _, [] -> failwith "Index out of bounds"
        | 0, _ :: tail -> tail
        | _, head :: tail -> head :: removeAt (index - 1) tail



type Builder =
    [<ReactComponent>]
    static member Main
        (
            annoState: Annotation list,
            setState: Annotation list -> unit,
            isLocalStorageClear: string -> unit -> bool,
            elementID,
            modalState,
            fileName: string,
            setFileName,
            setLocalFileName
        ) =

        let initialFile (id: string) =
            if isLocalStorageClear id () = true then
                Unset
            else
                Json.parseAs<UploadedFile> (Browser.WebStorage.localStorage.getItem id)

        let (filehtml: UploadedFile), setFilehtml = React.useState (initialFile "file")

        let (numPages: int option), setNumPages = React.useState (None)

        let initialModal = { isActive = false; location = (0, 0) }

        let modalContext = React.useContext (Contexts.ModalContext.createModalContext)

        let del =
            fun () ->
                let setLocalFile (id: string) (nextFile: UploadedFile) = // I copied this from FileUploader. This is not DRY.
                    let JSONString = Json.stringify nextFile
                    Browser.WebStorage.localStorage.setItem (id, JSONString)

                setFilehtml Unset

                setLocalFile "file" Unset

                setState []

                setFileName ""
                setLocalFileName "fileName" ""

        let turnOffContext (event: Browser.Types.Event) = modalContext.setter initialModal

        let (highlight: Highlight), setHighlight =
            React.useState (
                {
                    Keys = Map.empty
                    Terms = Map.empty
                    Values = Map.empty
                }
            )

        React.useEffectOnce (fun () ->
            Browser.Dom.window.addEventListener ("resize", turnOffContext)

            { new IDisposable with
                member this.Dispose() =
                    window.removeEventListener ("resize", turnOffContext)
            }
        )

        let placeholder =
            Html.div [
                prop.className "flex justify-center items-center w-full p-10"
                prop.children [
                    Html.div [
                        prop.className
                            "p-2 md:p-5 lg:p-10 flex justify-center items-center flex-col bg-base-200/80 shadow-lg rounded-lg max-w-2xl"
                        prop.children [
                            Html.h1 [ prop.className "my-2"; prop.text "Select file here:" ]

                            Html.div [
                                FileUpload.UploadDisplay(filehtml, setFilehtml, setState, setFileName, setLocalFileName)
                            ]
                        ]
                    ]
                ]
            ]

        let paper (width: string) (display: ReactElement) =
            Html.div [
                prop.className "overflow-y-hidden h-full flex flex-row gap-2 w-full relative p-2"
                prop.children [
                    match modalState.isActive with
                    | true ->
                        Contextmenu.onContextMenu (
                            modalContext,
                            annoState,
                            setState,
                            elementID,
                            highlight,
                            setHighlight
                        )
                    | false -> Html.none
                    Html.div [
                        prop.className [ width ]
                        prop.onContextMenu (fun e ->
                            // https://stackoverflow.com/a/2614472/12858021
                            let Selection = window.getSelection ()
                            let term = Selection.ToString().Trim()
                            let rect = Selection.getRangeAt(0).getBoundingClientRect ()
                            let relativeParent = document.getElementById(elementID).getBoundingClientRect ()

                            if term.Length <> 0 then
                                modalContext.setter {
                                    isActive = true
                                    location =
                                        rect.right - relativeParent.left, rect.bottom - relativeParent.top + 12.0
                                }

                                e.stopPropagation ()
                                e.preventDefault ()
                            else
                                ()
                        )
                        prop.children [
                            Html.div [ prop.text fileName; prop.className "p-2" ]
                            display
                        ]
                    ]
                    Html.div [
                        prop.className "w-1/3"
                        prop.children [
                            Html.div [
                                prop.text "Annotations"
                                prop.className "p-2"
                                prop.style [ style.width.inheritFromParent ]
                            ]
                        // for a in 0 .. annoState.Length - 1 do
                        //     App.Components.AnnoBlockwithSwate(annoState, setState, a, highlight, setHighlight)
                        ]
                    ]
                ]
            ]

        React.Fragment [
            match filehtml with
            | Unset -> ()
            | _ -> ActionB.Main(annoState, setState, del, fileName, highlight, setHighlight)
            Html.div [
                prop.className "flex flex-row p-2"
                prop.id "main-parent"
                prop.onClick (fun e -> modalContext.setter initialModal)
                prop.children [

                    match filehtml with
                    | Unset -> placeholder
                    | Docx fileString ->
                        paper "w-2/3" (FileUpload.DisplayHtml(fileString, highlight, elementID, isLocalStorageClear))
                    | PDF fileString ->
                        paper "" (FileUpload.DisplayPDF fileString setNumPages numPages elementID highlight)
                    | Txt fileString ->
                        paper "w-2/3" (FileUpload.DisplayHtml(fileString, highlight, elementID, isLocalStorageClear))

                ]
            ]
        ]
