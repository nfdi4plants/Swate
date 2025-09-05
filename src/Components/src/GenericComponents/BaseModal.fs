namespace Swate.Components

open Feliz
open Feliz.DaisyUI
open Fable.Core

[<Mangle(false); Erase>]
type BaseModal =

    [<ReactComponentAttribute>]
    static member ModalHeader(children: ReactElement, close: unit -> unit) =
        let ctx = React.useContext (Contexts.BaseModal.BaseModalCtx)

        Html.div [
            prop.className "swt:card-title"
            if ctx.IsSome then
                prop.id ctx.Value.headerId
            prop.children [
                children
                Components.DeleteButton(
                    className = "swt:ml-auto swt:btn-sm",
                    props = [ prop.onClick (fun _ -> close ()) ]
                )
            ]
        ]

    [<ReactComponent>]
    static member ModalDescription(children: ReactElement) =
        let ctx = React.useContext (Contexts.BaseModal.BaseModalCtx)

        Html.p [
            prop.className "swt:text-sm swt:opacity-60"
            if ctx.IsSome then
                prop.id ctx.Value.descId
            prop.children children
        ]

    [<ReactComponent>]
    static member ModalActions(children: ReactElement) =
        Html.div [ prop.className "swt:card-actions"; prop.children children ]

    [<ReactComponent>]
    static member ModalContent(children: ReactElement, ?debug: string) =
        Html.div [
            if debug.IsSome then
                prop.testId ("modal_content_" + debug.Value)
            prop.className "swt:overflow-y-auto swt:overflow-x-hidden swt:flex swt:flex-col swt:gap-2 swt:p-2"
            prop.children children
        ]

    [<ReactComponent>]
    static member ModalFooter(children: ReactElement) =
        Html.div [ prop.className "swt:card-actions"; prop.children children ]

    [<ReactComponent>]
    static member BaseModal
        (
            isOpen: bool,
            setIsOpen: bool -> unit,
            children: ReactElement,
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

        let ctx: Contexts.BaseModal.BaseModalContext = {
            isOpen = isOpen
            setIsOpen = setIsOpen
            headerId = headerId
            descId = descId
        }


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
                                    React.contextProvider (
                                        Contexts.BaseModal.BaseModalCtx,
                                        Some ctx,
                                        Html.div [
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
                                            prop.children children
                                        ]
                                    ),
                                ?returnFocus = unbox returnFocusRef,
                                ?initialFocus = unbox initialFocusRef,
                                visuallyHiddenDismiss = true
                            )
                    )
                )
            else
                Html.none
        ]

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
        BaseModal.BaseModal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            children =
                React.fragment [
                    BaseModal.ModalHeader(header, fun () -> setIsOpen false)
                    if description.IsSome then
                        BaseModal.ModalDescription(description.Value)
                    if modalActions.IsSome then
                        BaseModal.ModalActions(modalActions.Value)
                    BaseModal.ModalContent(children, ?debug = debug)
                    if footer.IsSome then
                        BaseModal.ModalFooter(footer.Value)
                ],
            ?initialFocusRef = initialFocusRef,
            ?returnFocusRef = returnFocusRef,
            ?debug = debug,
            ?className = className
        )


    ///<summary>This modal is used to display errors from for example api communication</summary>
    static member ErrorBaseModal(isOpen: bool, setIsOpen: bool -> unit, error: string, ?debug: string) =

        let debug = debug |> Option.map (fun d -> "errormodal_" + d)

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Something went wrong",
            children =
                Html.div [
                    prop.className "swt:alert swt:alert-error"
                    prop.children [
                        Svg.svg [
                            svg.className "swt:w-6 swt:h-6 swt:stroke-current"
                            svg.viewBox (0, 0, 24, 24)
                            svg.fill "none"
                            svg.children [
                                Svg.path [
                                    svg.d "M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"
                                    svg.strokeLineCap "round"
                                    svg.strokeLineJoin "round"
                                    svg.strokeWidth 2
                                ]
                            ]
                        ]

                        Html.div [
                            yield!
                                error.Split('\n')
                                |> Array.collect (fun line -> [| Html.text line; Html.br [] |])
                        ]
                    ]
                ],
            footer =
                Html.button [
                    prop.className "swt:btn swt:bg-neutral-content swt:btn-outline swt:ml-auto"
                    prop.text "Ok"
                    prop.onClick (fun _ -> setIsOpen (false))
                ],
            ?debug = debug
        )

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