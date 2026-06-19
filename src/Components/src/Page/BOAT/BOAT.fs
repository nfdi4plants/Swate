namespace App

open Feliz
open Types
open Components
open Fable.SimpleJson
open ARCtrl.Json
open Thoth.Json.Core


type View =
    [<ReactComponent>]
    static member Main() =

        let isLocalStorageClear (key: string) () =
            match (Browser.WebStorage.localStorage.getItem key) with
            | null -> true // Local storage is clear if the item doesn't exist
            | _ -> false //if false then something exists and the else case gets started

        let initialInteraction (id: string) =
            try
                if isLocalStorageClear id () = true then
                    []
                else
                    Decode.fromJsonString decoderAnno (Browser.WebStorage.localStorage.getItem id)
            with e ->
                Browser.Dom.console.warn (sprintf "Error parsing JSON from localStorage for key '%s': %s" id e.Message)
                []

        let (AnnotationState: Annotation list, setAnnotationState) =
            React.useState (initialInteraction "Annotations")

        let setLocalStorageAnnotation (id: string) (nextAnnos: Annotation list) =
            let JSONstring =

                nextAnnos |> List.map encoderAnno |> Encode.list |> Encode.toJsonString 0

            // log JSONstring
            Browser.WebStorage.localStorage.setItem (id, JSONstring)
            log JSONstring

        let setState (state: Annotation list) =
            setAnnotationState state
            setLocalStorageAnnotation "Annotations" state

        let setLocalFileName (id: string) (nextNAme: string) =
            let JSONstring = Json.stringify nextNAme
            Browser.WebStorage.localStorage.setItem (id, JSONstring)

        let initialFileName (id: string) =
            if isLocalStorageClear id () = true then
                ""
            else
                Json.parseAs<string> (Browser.WebStorage.localStorage.getItem id)

        let fileName, setFileName = React.useState (initialFileName "fileName")

        let fileNamewithoutType = fileName.Split('.').[0] //splits the file name and takes the first part before the dot


        let (modalState: ModalInfo, setModal) = React.useState (Contextmenu.initialModal)

        let myModalContext = { //makes setter and state in one record type
            modalState = modalState
            setter = setModal
        }

        let elementID = "Paper"

        let currentpage, setpage = React.useState (Types.Page.Builder)

        React.StrictMode [
            Contexts.ModalContext.createModalContext.Provider(
                myModalContext,
                React.Fragment [
                    Html.div [
                        prop.id "mainView"
                        prop.className "swt:flex swt:min-h-screen swt:flex-col swt:bg-accent swt:text-accent-content"
                        prop.children [
                            // Components.Navbar.Main(setpage, currentpage, AnnotationState, setState, fileNamewithoutType)
                            Html.div [
                                prop.testId "contentView"
                                prop.className "swt:grow"
                                prop.children [
                                    // match currentpage with
                                    // | Types.Page.Builder ->
                                    Builder.Main(
                                        AnnotationState,
                                        setState,
                                        isLocalStorageClear,
                                        elementID,
                                        modalState,
                                        fileName,
                                        setFileName,
                                        setLocalFileName

                                        )
                                    // | Types.Page.Contact -> Components.Contact.Main()
                                    // | Types.Page.Help -> Components.Help.Main()
                                ]
                            ]
                            // Components.Footer.Main
                        ]
                    ]
                ]
            )

        ]
