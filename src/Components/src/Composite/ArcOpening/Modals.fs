namespace Swate.Components.Composite.ArcOpening

open Fable.Core
open Feliz
open Swate.Components.Primitive.BaseModal

[<Erase; Mangle(false)>]
type Modals =

    [<ReactComponent>]
    static member OpeningArc(isOpen: bool) =
        BaseModal.BaseModal(
            isOpen,
            ignore,
            Html.div [
                prop.className "swt:flex swt:flex-col swt:items-center swt:gap-4 swt:p-4"
                prop.role.status
                prop.ariaLive.polite
                prop.children [
                    Html.span [
                        prop.className "swt:loading swt:loading-spinner swt:loading-lg"
                    ]
                    Html.p [
                        prop.className "swt:font-semibold"
                        prop.text "Opening ARC..."
                    ]
                    Html.p [
                        prop.className "swt:text-sm swt:opacity-70"
                        prop.text "Checking the selected folder. This may take a moment."
                    ]
                ]
            ],
            debug = "opening-arc"
        )
