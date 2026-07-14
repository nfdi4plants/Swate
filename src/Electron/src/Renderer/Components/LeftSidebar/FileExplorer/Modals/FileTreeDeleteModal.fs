namespace Renderer.Components.LeftSidebar.FileExplorer.Modals

open Fable.Core
open Feliz
open Swate.Components.Primitive.BaseModal

[<Erase; Mangle(false)>]
type FileTreeDeleteModal =

    [<ReactComponent>]
    static member Main(isOpen: bool, itemName: string option, close: unit -> unit, submit: unit -> JS.Promise<unit>) =

        let isDeleting, setIsDeleting = React.useState false

        let setIsOpen isOpen =
            if not isOpen && not isDeleting then
                close ()

        let displayName = itemName |> Option.defaultValue "this item"

        let onSubmit () =
            promise {
                if not isDeleting then
                    setIsDeleting true

                    try
                        do! submit ()
                    finally
                        setIsDeleting false
            }
            |> Promise.start

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:justify-end swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost"
                        prop.disabled isDeleting
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-error"
                        prop.disabled isDeleting
                        prop.onClick (fun _ -> onSubmit ())
                        prop.children [
                            if isDeleting then
                                Html.span [ prop.text "Deleting..." ]
                            else
                                Html.span [ prop.text "Delete" ]
                        ]
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Delete Item",
            description = Html.text $"Permanently delete '{displayName}'?",
            children = Html.none,
            footer = footer,
            debug = "arc-delete"
        )
