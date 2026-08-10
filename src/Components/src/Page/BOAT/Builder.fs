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

        let del =
            fun () ->
                let setLocalFile (id: string) (nextFile: UploadedFile) =
                    let JSONString = Json.stringify nextFile
                    Browser.WebStorage.localStorage.setItem (id, JSONString)

                setFilehtml Unset

                setLocalFile "file" Unset

                setState []

                setFileName ""
                setLocalFileName "fileName" ""

        let (highlight: Highlight), setHighlight =
            React.useState (
                {
                    Keys = Map.empty
                    Terms = Map.empty
                    Values = Map.empty
                }
            )

        let contextMenuRef = React.useElementRef ()

        let placeholder =
            Html.div [
                prop.className "swt:flex swt:justify-center swt:items-center swt:w-full swt:p-10"
                prop.children [
                    Html.div [
                        prop.className
                            "swt:p-2 swt:md:p-5 swt:lg:p-10 swt:flex swt:justify-center swt:items-center swt:flex-col swt:bg-base-200/80 swt:shadow-lg swt:rounded-lg swt:max-w-2xl swt:border swt:border-primary"
                        prop.children [
                            Html.h1 [ prop.className "swt:my-2"; prop.text "Select file here for process annotations:" ]

                            Html.div [
                                FileUpload.UploadDisplay(filehtml, setFilehtml, setState, setFileName, setLocalFileName)
                            ]
                        ]
                    ]
                ]
            ]

        let paper (display: ReactElement) =
            Html.div [
                prop.className "swt:overflow-y-auto swt:h-full swt:flex swt:flex-row swt:w-full swt:relative swt:px-8"
                prop.children [
                    Html.div [
                        prop.className "swt:w-full"
                        prop.ref contextMenuRef
                        prop.children [
                            Html.div [
                                prop.className "swt:badge swt:badge-primary swt:m-2"
                                prop.text fileName
                            ]
                            display
                        ]
                    ]
                ]
            ]

        let contextMenu =
            Swate.Components.Primitive.ContextMenu.ContextMenu.ContextMenu(
                childInfo =
                    (fun _ -> [
                        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(
                            text =
                                Html.div [
                                    prop.className "swt:text-gray-500 swt:text-sm swt:p-1"
                                    prop.text "Add new annotation as .."
                                ]
                        )
                        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(
                            text = Html.span "Key",
                            onClick =
                                fun _ ->
                                    FunctionsContextmenu.addAnnotationKeyNew(
                                        annoState,
                                        setState,
                                        elementID,
                                        highlight,
                                        setHighlight
                                    ) ()
                        )
                        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(
                            text = Html.span "Term",
                            onClick =
                                fun _ ->
                                    FunctionsContextmenu.addAnnotationBodyNew(
                                        annoState,
                                        setState,
                                        elementID,
                                        highlight,
                                        setHighlight
                                    ) ()
                        )
                        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(
                            text = Html.span "Value",
                            onClick =
                                fun _ ->
                                    FunctionsContextmenu.addAnnotationValueNew(
                                        annoState,
                                        setState,
                                        elementID,
                                        highlight,
                                        setHighlight
                                    ) ()
                        )
                        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(isDivider = true)
                        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(
                            text =
                                Html.div [
                                    prop.className "swt:text-gray-500 swt:text-sm swt:p-1"
                                    prop.text "Add to last annotation as .."
                                ]
                        )
                        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(
                            text = Html.span "Key",
                            onClick =
                                fun _ ->
                                    FunctionsContextmenu.addToLastAnnoAsKey(
                                        annoState,
                                        setState,
                                        highlight,
                                        setHighlight
                                    ) ()
                        )
                        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(
                            text = Html.span "Term",
                            onClick =
                                fun _ ->
                                    FunctionsContextmenu.addToLastAnnoAsBody(
                                        annoState,
                                        setState,
                                        highlight,
                                        setHighlight
                                    ) ()
                        )
                        Swate.Components.Primitive.ContextMenu.Types.ContextMenuItem(
                            text = Html.span "Value",
                            onClick =
                                fun _ ->
                                    FunctionsContextmenu.addToLastAnnoAsValue(
                                        annoState,
                                        setState,
                                        highlight,
                                        setHighlight
                                    ) ()
                        )
                    ]),
                ref = contextMenuRef,
                onSpawn =
                    (fun e ->
                        let term = window.getSelection().ToString().Trim()

                        if term.Length <> 0 then
                            Some(box term)
                        else
                            None
                    )
            )

        React.Fragment [
            match filehtml with
            | Unset -> ()
            | _ -> ActionBar.Main(annoState, setState, del, fileName, highlight, setHighlight)
            Html.div [
                prop.className "swt:flex swt:flex-row swt:p-2 swt:overflow-y-auto swt:h-full"
                prop.id "main-parent"
                prop.children [
                    match filehtml with
                    | Unset -> placeholder
                    | Docx fileString ->
                        paper (FileUpload.DisplayHtml(fileString, highlight, elementID, isLocalStorageClear))
                    | PDF fileString ->
                        paper (FileUpload.DisplayPDF fileString setNumPages numPages elementID highlight)
                    | Txt fileString ->
                        paper (FileUpload.DisplayHtml(fileString, highlight, elementID, isLocalStorageClear))

                ]
            ]
            contextMenu
        ]
