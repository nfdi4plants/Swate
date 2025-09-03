namespace Swate.Components

open Feliz
open Feliz.DaisyUI
open Fable.Core

[<Mangle(false); Erase>]
type BaseModal =

    [<ReactComponent>]
    static member Modal
        (
            isOpen: bool,
            setIsOpen: bool -> unit,
            header: ReactElement,
            children: ReactElement,
            ?description: ReactElement,
            ?modalActions: ReactElement,
            ?footer: ReactElement,
            ?initialFocusRef: IRefValue<option<Browser.Types.HTMLElement>>,
            ?returnFocusRef: IRefValue<option<Browser.Types.HTMLElement>>,
            ?debug: string,
            ?className: string
        ) =

        let flui = FloatingUI.useFloating (isOpen, onOpenChange = setIsOpen)

        let click = FloatingUI.useClick (flui.context)

        let dismiss =
            FloatingUI.useDismiss (
                flui.context,
                FloatingUI.UseDismissProps(outsidePressEvent = FloatingUI.PressEvent.Mousedown)
            )

        let role = FloatingUI.useRole (flui.context)

        let useInteractions = FloatingUI.useInteractions ([| click; role; dismiss |])

        let ts = FloatingUI.useTransitionStatus (flui.context)

        let headerId = FloatingUI.useId ()
        let descId = FloatingUI.useId ()

        React.fragment [
            if isOpen then
                FloatingUI.FloatingPortal(
                    FloatingUI.FloatingOverlay(
                        lockScroll = true,
                        className = "swt:modal swt:modal-open",
                        children =
                            FloatingUI.FloatingFocusManager(
                                flui.context,
                                children =
                                    Html.div [
                                        if description.IsSome then
                                            prop.ariaDescribedBy descId
                                        if debug.IsSome then
                                            prop.testId ("modal_" + debug.Value)
                                        prop.ariaLabelledBy headerId
                                        prop.custom ("data-status", ts.status)
                                        prop.className [
                                            "swt:modal-box swt:card-body"
                                            if className.IsSome then
                                                className.Value
                                        ]
                                        prop.ref flui.refs.setFloating
                                        yield! prop.spread (useInteractions.getFloatingProps ())
                                        prop.children [
                                            Html.div [
                                                prop.className "swt:card-title"
                                                prop.id headerId
                                                prop.children [
                                                    header
                                                    Components.DeleteButton(
                                                        className = "swt:ml-auto swt:btn-sm",
                                                        props = [ prop.onClick (fun _ -> setIsOpen (false)) ]
                                                    )
                                                ]
                                            ]
                                            if description.IsSome then
                                                Html.p [
                                                    prop.className "swt:text-sm swt:opacity-60"
                                                    prop.id descId
                                                    prop.children description.Value
                                                ]
                                            if modalActions.IsSome then
                                                Html.div [
                                                    prop.className "swt:card-actions"
                                                    prop.children modalActions.Value
                                                ]
                                            Html.div [
                                                if debug.IsSome then
                                                    prop.testId ("modal_content_" + debug.Value)
                                                prop.className
                                                    "swt:overflow-y-auto swt:overflow-x-hidden swt:flex swt:flex-col swt:gap-2 swt:p-2"
                                                prop.children children
                                            ]
                                            if footer.IsSome then
                                                Html.div [
                                                    prop.className "swt:card-actions"
                                                    prop.children footer.Value
                                                ]
                                        ]
                                    ],
                                ?returnFocus = unbox returnFocusRef,
                                ?initialFocus = unbox initialFocusRef,
                                visuallyHiddenDismiss = true
                            )
                    )
                )
            else
                Html.none
        ]

    [<ReactComponentAttribute>]
    static member Entry() =

        let isOpen, setIsOpen = React.useState (false)
        let initialFocusRef = React.useInputRef ()
        let returnFocusRef = React.useButtonRef ()

        Html.div [
            Html.button [
                prop.onClick (fun _ -> setIsOpen (not isOpen))
                prop.className "swt:btn"
                prop.text "Open Modal"
                prop.ref returnFocusRef
            ]
            BaseModal.Modal(
                isOpen = isOpen,
                setIsOpen = setIsOpen,
                header = Html.text "Modal Header",
                description =
                    Html.text
                        "A modal is a focused, overlaying UI element that interrupts the main flow to display important information, prompts, or interactive content requiring user action.",
                children =
                    Html.div [
                        Html.table [
                            prop.className "swt:table swt:table-zebra"
                            prop.children [
                                Html.tbody [
                                    for i in 0..100 do
                                        Html.tr [
                                            Html.td [ prop.text (string i) ]
                                            Html.td [ prop.text (string (i * 2)) ]
                                            Html.td [ prop.text (sprintf "Row: %i" i) ]
                                        ]
                                ]
                            ]
                        ]
                    ],
                modalActions =
                    React.fragment [
                        Html.input [
                            prop.className "swt:input"
                            prop.placeholder "...filter"
                            prop.ref initialFocusRef
                        ]
                    ],
                footer =
                    React.fragment [
                        Html.button [
                            prop.className "swt:btn"
                            prop.onClick (fun _ -> setIsOpen (false))
                            prop.text "Cancel"
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-primary swt:ml-auto"
                            prop.onClick (fun _ -> setIsOpen (false))
                            prop.text "OK"
                        ]
                    ],
                initialFocusRef = unbox initialFocusRef,
                returnFocusRef = unbox returnFocusRef
            )
        ]