namespace Swate.Components

open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types


module FloatingUI =

    [<AllowNullLiteral; Global>]
    type UseInteractionsReturn =
        abstract member getItemProps: obj -> Map<string, obj>
        abstract member getFloatingProps: obj -> Map<string, obj>
        abstract member getReferenceProps: obj -> Map<string, obj>

    [<AllowNullLiteral; Global>]
    type VirtualElement [<ParamObjectAttribute; Emit("$0")>] (getBoundingClientRect: unit -> ClientRect) =
        member val getBoundingClientRect = getBoundingClientRect with get, set

    type ReferenceElement = U2<HTMLElement, VirtualElement>

    [<AllowNullLiteral; Global>]
    type UseFloatingReturn
        [<ParamObjectAttribute; Emit("$0")>]
        (
            context: obj,
            placement: obj,
            strategy: obj,
            x: int,
            y: int,
            middlewareData: obj,
            isPositioned: bool,
            update: unit -> unit,
            floatingStyles: obj,
            refs:
                {|
                    reference: IRefValue<ReferenceElement>
                    floating: IRefValue<HTMLElement option>
                    domReference: IRefValue<HTMLElement>
                    setReference: obj -> unit
                    setFloating: IRefValue<HTMLElement option>
                    setPositionReference: ReferenceElement -> unit
                |},
            elements:
                {|
                    reference: obj
                    floating: HTMLElement
                |}
        ) =
        member val context = context with get, set
        member val placement = placement with get, set
        member val strategy = strategy with get, set
        member val x = x with get, set
        member val y = y with get, set
        member val middlewareData = middlewareData with get, set
        member val isPositioned = isPositioned with get, set
        member val update = update with get, set
        member val floatingStyles = floatingStyles with get, set
        member val refs = refs with get, set
        member val elements = elements with get, set

    [<StringEnum(CaseRules.LowerFirst)>]
    type PressEvent =
        | Pointerdown
        | Mousedown
        | Click


    [<AllowNullLiteral; Global>]
    type UseDismissProps [<ParamObjectAttribute; Emit("$0")>] (
        ?enabled: bool,
        ?escapeKey: bool,
        ?referencePress: bool,
        ?referencePressEvent: PressEvent,
        ?outsidePress: bool,
        ?outsidePressEvent: PressEvent,
        ?ancestorScroll: bool,
        ?bubbles: bool,
        ?capture: bool
    ) =
        member val enabled = enabled
        member val escapeKey = escapeKey
        member val referencePress = referencePress
        member val referencePressEvent = referencePressEvent
        member val outsidePress = outsidePress
        member val outsidePressEvent = outsidePressEvent
        member val ancestorScroll = ancestorScroll
        member val bubbles = bubbles
        member val capture = capture

[<Erase>]
type FloatingUI =

    [<ImportMember("@floating-ui/react")>]
    static member autoUpdate (reference: obj) (floating: obj) (update: obj -> unit) : unit = jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useFloating
        (
            ?placement: obj,
            ?strategy: string,
            ?transform: bool,
            ?middleware: obj[],
            ?open': bool,
            ?onOpenChange: bool -> unit,
            ?elements: obj,
            ?whileElementsMounted: obj -> obj -> (obj -> unit) -> unit,
            ?nodeId: string
        ) : FloatingUI.UseFloatingReturn =
        jsNative

    [<ReactComponent("FloatingPortal", "@floating-ui/react")>]
    static member FloatingPortal(children: ReactElement list) = React.imported ()

    [<ReactComponent("FloatingPortal", "@floating-ui/react")>]
    static member FloatingOverlay(children: ReactElement list, ?lockScroll: bool) = React.imported ()

    [<ReactComponent("FloatingFocusManager", "@floating-ui/react")>]
    static member FloatingFocusManager
        (
            context: obj,
            children: ReactElement list,
            ?disabled: bool,
            ?initialFocus: obj,
            ?returnFocus: obj,
            ?restoreFocus: bool,
            ?guards: bool,
            ?modal: bool,
            ?visuallyHiddenDismiss: bool,
            ?closeOnFocusOut: bool,
            ?outsideElementsInert: bool,
            ?getInsideElements: unit -> ReactElement[],
            ?order: string[]
        ) =
        React.imported ()


[<Erase>]
[<Mangle(false)>]
type ContextMenu =

    [<ReactComponent>]
    static member ContextMenu(children: ReactElement, ?ref: IRefValue<HTMLElement option>) =
        let (isOpen, setIsOpen) = React.useState (false)

        let allowMouseUpCloseRef = React.useRef (false)
        let timeout = React.useRef (None)
        /// This is used to store reference to element with listeners attached.
        /// Without this the listener cannot reliably be removed!
        let targetRef = React.useRef<HTMLElement option>(None)

        let floating =
            FloatingUI.useFloating (
                open' = isOpen,
                onOpenChange = setIsOpen,
                placement = "right-start",
                strategy = "fixed",
                whileElementsMounted = FloatingUI.autoUpdate
            )


        React.useEffect (
            (fun _ ->

                console.log("Effect mounted")

                let myClearTimeout () =
                    timeout.current |> Option.iter (fun timeout -> Fable.Core.JS.clearTimeout timeout)

                let onContextMenu (e: Event) =
                    let e = e :?> Browser.Types.MouseEvent
                    e.preventDefault ()

                    let rect: ClientRect =
                        {|
                            width = 0
                            height = 0
                            x = e.clientX
                            y = e.clientY
                            top = e.clientY
                            left = e.clientX
                            right = e.clientX
                            bottom = e.clientY
                        |} |> unbox

                    let vEl = FloatingUI.VirtualElement(fun () -> rect)

                    floating.refs.setPositionReference !^vEl

                    setIsOpen (true)
                    myClearTimeout ()

                    allowMouseUpCloseRef.current <- false
                    timeout.current <- Some(JS.setTimeout (fun _ -> allowMouseUpCloseRef.current <- true) 300)

                let onMouseUp (e: Event) =
                    if allowMouseUpCloseRef.current then
                        setIsOpen false

                Browser.Dom.document.addEventListener ("mouseup", onMouseUp)

                match ref with
                | Some ref ->
                    ref.current
                    |> Option.iter (fun el ->
                        el.addEventListener ("contextmenu", onContextMenu)
                        targetRef.current <- Some el
                    )
                | None ->
                    Browser.Dom.document.addEventListener ("contextmenu", onContextMenu)
                    targetRef.current <- Some (Browser.Dom.document :?> HTMLElement)

                React.createDisposable (fun () ->
                    console.log("Effect cleanup")

                    Browser.Dom.document.removeEventListener ("mouseup", onMouseUp)

                    targetRef.current
                    |> Option.iter (fun el ->
                        el.removeEventListener ("contextmenu", onContextMenu)
                    )


                    myClearTimeout ())),
            [| box floating.refs |]
        )

        FloatingUI.FloatingPortal [
            if isOpen then
                FloatingUI.FloatingOverlay(
                    lockScroll = true,
                    children = [
                        FloatingUI.FloatingFocusManager(
                            context = floating.context,
                            initialFocus = floating.refs.floating,
                            children = [
                                Html.div [
                                    prop.ref floating.refs.setFloating
                                    prop.custom ("style", floating.floatingStyles)
                                    prop.children children
                                ]
                            ]
                        )

                    ]
                )
        ]

    [<ReactComponent>]
    static member Example() =

        let containerRef = React.useElementRef ()

        Html.div [
            prop.className "w-full h-72 border border-dashed border-primary rounded flex items-center justify-center"
            prop.text "Click here for context menu!"
            prop.ref containerRef

            prop.children [ ContextMenu.ContextMenu(
                Html.ul [
                    Html.li "1"
                    Html.li "2"
                    Html.li "3"
                    Html.li "4"
                    Html.li "5"
                ],
                containerRef
            ) ]
        ]