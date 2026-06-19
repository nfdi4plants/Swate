namespace Swate.Components.Page.BOAT

open Fable.Core
open Feliz
open Types
open Components
open Fable.SimpleJson
open ARCtrl.Json
open Thoth.Json.Core

module private BOATViewHelper =

    let private pageLabel page =
        match page with
        | Types.Page.Builder -> "Builder"
        | Types.Page.Help -> "Help"
        | Types.Page.Contact -> "Contact"

    let pageButton (currentPage: Types.Page) (setPage: Types.Page -> unit) page =
        Html.button [
            prop.className [
                "swt:btn swt:btn-sm"
                if currentPage = page then
                    "swt:btn-primary"
                else
                    "swt:btn-ghost"
            ]
            prop.onClick (fun _ -> setPage page)
            prop.text (pageLabel page)
        ]

    let header (currentPage: Types.Page) (setPage: Types.Page -> unit) (fileName: string) annotationCount =
        Html.header [
            prop.className
                "swt:h-16 swt:min-h-16 swt:bg-base-200 swt:text-base-content swt:flex swt:items-center swt:gap-3 swt:px-4 swt:shadow swt:z-50"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:gap-2 swt:min-w-0"
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--document-text-24-regular swt:size-5 swt:shrink-0"
                        ]
                        Html.div [
                            prop.className "swt:font-semibold swt:truncate"
                            prop.text "BOAT"
                        ]
                    ]
                ]
                Html.nav [
                    prop.className "swt:flex swt:items-center swt:gap-1"
                    prop.children [
                        pageButton currentPage setPage Types.Page.Builder
                        pageButton currentPage setPage Types.Page.Help
                        pageButton currentPage setPage Types.Page.Contact
                    ]
                ]
                Html.div [
                    prop.className "swt:ml-auto swt:flex swt:items-center swt:gap-2 swt:min-w-0 swt:text-sm swt:opacity-80"
                    prop.children [
                        if not (System.String.IsNullOrWhiteSpace fileName) then
                            Html.span [
                                prop.className "swt:truncate swt:max-w-64"
                                prop.title fileName
                                prop.text fileName
                            ]
                        Html.span [ prop.text $"{annotationCount} annotations" ]
                    ]
                ]
            ]
        ]

    let footer =
        Html.footer [
            prop.className "swt:bg-base-200 swt:text-base-content/70 swt:text-xs swt:px-4 swt:py-2"
            prop.text "BOAT"
        ]

    let infoPage (title: string) (icon: string) (body: string) =
        Html.div [
            prop.className "swt:h-full swt:flex swt:items-center swt:justify-center swt:p-8"
            prop.children [
                Html.div [
                    prop.className "swt:max-w-xl swt:bg-base-200 swt:rounded-box swt:p-6 swt:shadow swt:flex swt:flex-col swt:gap-3"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-center swt:gap-2 swt:text-lg swt:font-semibold"
                            prop.children [
                                Html.i [ prop.className $"swt:iconify {icon} swt:size-5" ]
                                Html.span [ prop.text title ]
                            ]
                        ]
                        Html.p [ prop.className "swt:opacity-80"; prop.text body ]
                    ]
                ]
            ]
        ]

[<Erase; Mangle(false)>]
type BOAT =

    [<ReactComponent>]
    static member Entry() =

        let isLocalStorageClear (key: string) () =
            match Browser.WebStorage.localStorage.getItem key with
            | null -> true
            | _ -> false

        let initialInteraction (id: string) =
            try
                if isLocalStorageClear id () = true then
                    []
                else
                    Decode.fromJsonString decoderAnno (Browser.WebStorage.localStorage.getItem id)
            with e ->
                Browser.Dom.console.warn (sprintf "Error parsing JSON from localStorage for key '%s': %s" id e.Message)
                []

        let annotationState, setAnnotationState =
            React.useState (initialInteraction "Annotations")

        let setLocalStorageAnnotation (id: string) (nextAnnos: Annotation list) =
            let json =
                nextAnnos
                |> List.map encoderAnno
                |> Encode.list
                |> Encode.toJsonString 0

            Browser.WebStorage.localStorage.setItem (id, json)

        let setState (state: Annotation list) =
            setAnnotationState state
            setLocalStorageAnnotation "Annotations" state

        let setLocalFileName (id: string) (nextName: string) =
            let json = Json.stringify nextName
            Browser.WebStorage.localStorage.setItem (id, json)

        let initialFileName (id: string) =
            if isLocalStorageClear id () = true then
                ""
            else
                Json.parseAs<string> (Browser.WebStorage.localStorage.getItem id)

        let fileName, setFileName = React.useState (initialFileName "fileName")

        let modalState, setModal =
            React.useState (Contextmenu.initialModal)

        let modalContext = {
            modalState = modalState
            setter = setModal
        }

        let elementID = "Paper"

        let currentPage, setPage =
            React.useState Types.Page.Builder

        let pageContent =
            Html.div [
                prop.testId "contentView"
                prop.className "swt:grow swt:min-h-0 swt:overflow-hidden"
                prop.children [
                    match currentPage with
                    | Types.Page.Builder ->
                        Components.Builder.Main(
                            annotationState,
                            setState,
                            isLocalStorageClear,
                            elementID,
                            modalState,
                            fileName,
                            setFileName,
                            setLocalFileName
                        )
                    | Types.Page.Help ->
                        BOATViewHelper.infoPage
                            "Help"
                            "swt:fluent--question-circle-24-regular"
                            "Load a document, select text, and annotate terms from the document view."
                    | Types.Page.Contact ->
                        BOATViewHelper.infoPage
                            "Contact"
                            "swt:fluent--mail-24-regular"
                            "For BOAT feedback, use the Swate project channels."
                ]
            ]

        Contexts.ModalContext.createModalContext.Provider(
            modalContext,
            Html.div [
                prop.id "mainView"
                prop.className "swt:flex swt:h-screen swt:flex-col swt:bg-base-100 swt:text-base-content"
                prop.children [
                    BOATViewHelper.header currentPage setPage fileName annotationState.Length
                    pageContent
                    BOATViewHelper.footer
                ]
            ]
        )
