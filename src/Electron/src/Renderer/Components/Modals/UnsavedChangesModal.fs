namespace Renderer.Components.Modals

open Fable.Core
open Feliz
open Swate.Components.Primitive.BaseModal

[<Erase; Mangle(false)>]
type UnsavedChangesModal =

    [<ReactComponent>]
    static member Main
        (
            isOpen: bool,
            title: string,
            description: string,
            cancel: unit -> unit,
            discard: unit -> unit,
            save: unit -> unit,
            isBusy: bool,
            ?saveError: string,
            ?discardButtonText: string,
            ?saveButtonText: string,
            ?savingText: string,
            ?debug: string
        ) =

        let discardButtonText = defaultArg discardButtonText "Don't Save"
        let saveButtonText = defaultArg saveButtonText "Save"
        let savingText = defaultArg savingText "Saving..."

        let setIsOpen isOpen =
            if not isOpen && not isBusy then
                cancel ()

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-neutral"
                        prop.disabled isBusy
                        prop.text "Cancel"
                        prop.onClick (fun _ -> cancel ())
                    ]
                    Html.button [
                        prop.className "swt:btn swt:ml-auto"
                        prop.disabled isBusy
                        prop.text discardButtonText
                        prop.onClick (fun _ -> discard ())
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.disabled isBusy
                        prop.text (if isBusy then savingText else saveButtonText)
                        prop.onClick (fun _ -> save ())
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text title,
            description = Html.text description,
            children =
                (match saveError with
                 | Some message ->
                     Html.p [
                         prop.className "swt:text-error swt:text-sm swt:whitespace-pre-wrap"
                         prop.text message
                     ]
                 | None -> Html.none),
            footer = footer,
            ?debug = debug
        )
