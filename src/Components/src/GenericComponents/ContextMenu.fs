namespace Swate.Components

open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types


module FloatingUI =
    [<AllowNullLiteral; Global>]
    type VirtualElement [<ParamObjectAttribute; Emit("$0")>] (getBoundingClientRect: unit -> ClientRect) =
        member val getBoundingClientRect = getBoundingClientRect with get, set

    type ReferenceElement = U2<HTMLElement, VirtualElement>

    [<AllowNullLiteral; Import("UseFloatingReturn", "@floating-ui/react")>]
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


    [<AllowNullLiteral; Import("UseInteractionsReturn", "@floating-ui/react")>]
    type UseInteractionsReturn =
        abstract member getItemProps: obj -> obj
        abstract member getFloatingProps: obj -> obj
        abstract member getReferenceProps: obj -> obj


    [<StringEnum(CaseRules.LowerFirst)>]
    type PressEvent =
        | Pointerdown
        | Mousedown
        | Click


    [<AllowNullLiteral; Import("UseDismissProps", "@floating-ui/react")>]
    type UseDismissProps
        [<ParamObjectAttribute; Emit("$0")>]
        (
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

    [<StringEnum(CaseRules.LowerFirst)>]
    type RoleAttribute =
        // Native ARIA roles
        | Dialog
        | Tooltip
        | Menu
        | Listbox
        | Grid
        | Tree
        // Custom component roles
        | Alertdialog
        | Label
        | Select
        | Combobox

    [<AllowNullLiteral; Global>]
    type UseRoleProps [<ParamObjectAttribute; Emit("$0")>] (?enabled: bool, ?role: RoleAttribute) =
        member val enabled = enabled
        member val role = role

    [<AllowNullLiteral; Import("UseListNavigationProps", "@floating-ui/react")>]
    type UseListNavigationProps
        [<ParamObjectAttribute; Emit("$0")>]
        (
            listRef,
            activeIndex: int option,
            ?onNavigate,
            ?enabled: bool,
            ?selectedIndex,
            ?loop: bool,
            ?nested: bool,
            ?rtl: bool,
            ?``virtual``: bool,
            ?virtualItemRef,
            ?allowEscape: bool,
            ?orientation: string,
            ?cols: int,
            ?focusItemOnOpen,
            ?focusItemOnHover: bool,
            ?openOnArrowKeyDown: bool,
            ?disabledIndices: int[],
            ?scrollItemIntoView,
            ?itemSizes,
            ?dense: bool
        ) =
        member val listRef = listRef
        member val activeIndex = activeIndex
        member val onNavigate = onNavigate
        member val enabled = enabled
        member val selectedIndex = selectedIndex
        member val loop = loop
        member val nested = nested
        member val rtl = rtl
        member val ``virtual`` = ``virtual``
        member val virtualItemRef = virtualItemRef
        member val allowEscape = allowEscape
        member val orientation = orientation
        member val cols = cols
        member val focusItemOnOpen = focusItemOnOpen
        member val focusItemOnHover = focusItemOnHover
        member val openOnArrowKeyDown = openOnArrowKeyDown
        member val disabledIndices = disabledIndices
        member val scrollItemIntoView = scrollItemIntoView
        member val itemSizes = itemSizes
        member val dense = dense

    [<AllowNullLiteral; ImportMember("@floating-ui/react")>]
    type UseTypeaheadProps
        [<ParamObjectAttribute; Emit("$0")>]
        (
            listRef,
            activeIndex: int option,
            ?onNavigate,
            ?onMatch,
            ?enabled: bool,
            ?resetMs,
            ?ignoreKeys: string[],
            ?selectedIndex: int,
            ?onTypingChange,
            ?findMatch
        ) =
        member val listRef = listRef
        member val activeIndex = activeIndex
        member val onNavigate = onNavigate
        member val onMatch = onMatch
        member val enabled = enabled
        member val resetMs = resetMs
        member val ignoreKeys = ignoreKeys
        member val selectedIndex = selectedIndex
        member val onTypingChange = onTypingChange
        member val findMatch = findMatch


    type IMiddleware =
        interface
        end

    type Middleware =
        [<ImportMemberAttribute("@floating-ui/react")>]
        static member offset(?options: obj) : IMiddleware = jsNative

        [<ImportMemberAttribute("@floating-ui/react")>]
        static member flip(?options: obj) : IMiddleware = jsNative

        [<ImportMemberAttribute("@floating-ui/react")>]
        static member shift(?options: obj) : IMiddleware = jsNative

[<Erase>]
type FloatingUI =
    // reference: ReferenceElement, floating: FloatingElement, update: () => void
    [<ImportMember("@floating-ui/react")>]
    static member autoUpdate (reference: obj) (floating: obj) (update: unit -> unit) : unit = jsNative

    [<ImportMember("@floating-ui/react"); ParamObjectAttribute>]
    static member useFloating
        (
            ?placement: obj,
            ?strategy: string,
            ?transform: bool,
            ?middleware: obj[],
            ?``open``: bool,
            ?onOpenChange: bool -> unit,
            ?elements: obj,
            ?whileElementsMounted: obj -> obj -> (unit -> unit) -> unit,
            ?nodeId: string
        ) : FloatingUI.UseFloatingReturn =
        jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useDismiss(context: obj, ?props: FloatingUI.UseDismissProps) : obj = jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useRole(context: obj, ?props: FloatingUI.UseRoleProps) : obj = jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useListNavigation(context: obj, ?props: FloatingUI.UseListNavigationProps) : obj = jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useTypeahead(conext: obj, ?props: FloatingUI.UseTypeaheadProps) : obj = jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useInteractions(interactions: obj[]) : FloatingUI.UseInteractionsReturn = jsNative

    [<ReactComponent("FloatingPortal", "@floating-ui/react")>]
    static member FloatingPortal(children: ReactElement) = React.imported ()

    [<ReactComponent("FloatingOverlay", "@floating-ui/react")>]
    static member FloatingOverlay(children: ReactElement, ?lockScroll: bool, ?className: string) = React.imported ()

    [<ReactComponent("FloatingFocusManager", "@floating-ui/react")>]
    static member FloatingFocusManager
        (
            context: obj,
            children: ReactElement,
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

[<Global; AllowNullLiteral>]
type ContextMenuItem
    [<ParamObjectAttribute; Emit("$0")>]
    (
        text: ReactElement,
        ?icon: ReactElement,
        ?kbdbutton: ReactElement,
        ?isDivider: bool,
        ?onClick:
            {|
                buttonEvent: Browser.Types.MouseEvent
                spawnData: obj option
            |}
                -> unit,
        ?label: string
    ) =
    member val text = text with get, set
    member val icon = icon with get, set
    member val kbdbutton = kbdbutton with get, set
    member val isDivider = isDivider with get, set
    member val onClick = onClick with get, set
    member val label = label with get, set


[<Erase>]
[<Mangle(false)>]
type ContextMenu =

    /// <summary></summary>
    /// <param name="childInfo"></param>
    /// <param name="ref">By default user Browser.document</param>
    /// <param name="onSpawn">A function that returns an option of any data. Data is stored in react
    /// state and can be accessed in context menu item onClick handlers. By default stores just the
    /// event target. Only if Some is returned context menu is opened.</param>
    /// <returns></returns>
    [<ReactComponent>]
    static member ContextMenu
        (
            childInfo: ContextMenuItem list,
            ?ref: IRefValue<HTMLElement option>,
            ?onSpawn: Browser.Types.MouseEvent -> obj option
        ) =

        let (spawnData: obj option), setSpawnData = React.useState (None)
        let onSpawn = onSpawn |> Option.defaultValue (fun e -> box e |> Some)
        let (isOpen, setIsOpen) = React.useState (false)
        let (activeIndex: int option), setActiveIndex = React.useState (None)

        let allowMouseUpCloseRef = React.useRef (false)
        let timeout = React.useRef (None)
        /// This is used to store reference to element with listeners attached.
        /// Without this the listener cannot reliably be removed!
        let targetRef = React.useRef<HTMLElement option> (None)

        let listItemsRef: IRefValue<ResizeArray<HTMLElement>> = React.useRef (ResizeArray())

        let listContentRef =
            React.useRef (ResizeArray(childInfo |> List.map (fun child -> child.label)))

        let floating =
            FloatingUI.useFloating (
                ``open`` = isOpen,
                onOpenChange = setIsOpen,
                middleware = [|
                    FloatingUI.Middleware.offset {| mainAxis = 5; alignmentAxis = 4 |}
                    FloatingUI.Middleware.flip {|
                        fallbackPlacements = [| "left-start" |]
                    |}
                    FloatingUI.Middleware.shift {| padding = 10 |}
                |],
                placement = "right-start",
                strategy = "fixed",
                whileElementsMounted = FloatingUI.autoUpdate
            )

        let dismiss = FloatingUI.useDismiss (floating.context)

        let role =
            FloatingUI.useRole (floating.context, FloatingUI.UseRoleProps(role = FloatingUI.RoleAttribute.Menu))

        let listNavigation =
            FloatingUI.useListNavigation (
                floating.context,
                FloatingUI.UseListNavigationProps(
                    listRef = listItemsRef,
                    activeIndex = activeIndex,
                    onNavigate = setActiveIndex
                )
            )

        let typeahead =
            FloatingUI.useTypeahead (
                floating.context,
                FloatingUI.UseTypeaheadProps(
                    enabled = isOpen,
                    listRef = listContentRef,
                    onMatch = setActiveIndex,
                    activeIndex = activeIndex
                )
            )

        let interactions =
            FloatingUI.useInteractions [| role; dismiss; listNavigation; typeahead |]

        React.useEffect (
            (fun _ ->

                let myClearTimeout () =
                    timeout.current
                    |> Option.iter (fun timeout -> Fable.Core.JS.clearTimeout timeout)

                let onContextMenu (e: Event) =
                    let e = e :?> Browser.Types.MouseEvent

                    match onSpawn e with
                    | Some data ->
                        e.preventDefault ()
                        setSpawnData (Some data)

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
                            |}
                            |> unbox

                        let vEl = FloatingUI.VirtualElement(fun () -> rect)

                        floating.refs.setPositionReference !^vEl

                        setIsOpen (true)
                        myClearTimeout ()

                        allowMouseUpCloseRef.current <- false
                        timeout.current <- Some(JS.setTimeout (fun _ -> allowMouseUpCloseRef.current <- true) 300)
                    | None -> ()

                let onMouseUp (e: Event) =
                    if allowMouseUpCloseRef.current then
                        setIsOpen false

                match ref with
                | Some ref ->
                    ref.current
                    |> Option.iter (fun el ->
                        el.addEventListener ("contextmenu", onContextMenu)
                        targetRef.current <- Some el)
                | None ->
                    Browser.Dom.document.addEventListener ("contextmenu", onContextMenu)
                    targetRef.current <- Some(Browser.Dom.document :?> HTMLElement)

                Browser.Dom.document.addEventListener ("mouseup", onMouseUp)

                React.createDisposable (fun () ->
                    console.log ("Effect cleanup")

                    Browser.Dom.document.removeEventListener ("mouseup", onMouseUp)

                    targetRef.current
                    |> Option.iter (fun el -> el.removeEventListener ("contextmenu", onContextMenu))


                    myClearTimeout ())),
            [| box floating.refs |]
        )

        let close =
            fun () ->
                setIsOpen false
                allowMouseUpCloseRef.current <- true

                timeout.current
                |> Option.iter (fun timeout -> Fable.Core.JS.clearTimeout timeout)

        FloatingUI.FloatingPortal(
            if isOpen then
                FloatingUI.FloatingOverlay(
                    lockScroll = true,
                    className = "z-[9999]",
                    children =
                        FloatingUI.FloatingFocusManager(
                            context = floating.context,
                            initialFocus = floating.refs.floating,
                            children =
                                Html.div [
                                    prop.ref floating.refs.setFloating
                                    prop.custom ("style", floating.floatingStyles)
                                    for key, v in
                                        interactions.getFloatingProps () |> Fable.Core.JS.Constructors.Object.entries do
                                        prop.custom (key, v)
                                    prop.className "grid grid-cols-[auto_1fr_auto] bg-base-100 border-2 border-base-300 w-56 rounded-md focus:outline-none"
                                    prop.children [
                                        for index in 0 .. childInfo.Length - 1 do
                                            let child = childInfo.[index]

                                            let triggerEvent =
                                                fun (e: Browser.Types.MouseEvent) ->
                                                    let d = {|
                                                        buttonEvent = e
                                                        spawnData = spawnData
                                                    |}

                                                    child.onClick |> Option.iter (fun f -> f d)
                                                    close ()

                                            let props =
                                                interactions.getItemProps (
                                                    {|
                                                        ref =
                                                            fun (node: HTMLElement) ->
                                                                listItemsRef.current.[index] <- node
                                                        tabIndex =
                                                            if activeIndex.IsSome && activeIndex.Value = index then
                                                                0
                                                            else
                                                                -1
                                                        onClick = triggerEvent
                                                        onMouseUp = triggerEvent
                                                        label = child.label
                                                    |}
                                                )
                                                |> Fable.Core.JS.Constructors.Object.entries

                                            Html.button [
                                                prop.key index
                                                prop.className
                                                    "col-span-3 grid grid-cols-subgrid gap-x-2
                                                    text-base-content px-2 py-1
                                                    w-full text-left
                                                    hover:bg-base-100
                                                    focus:bg-base-100 focus:outline-none focus:ring-2 focus:ring-primary"
                                                prop.children [
                                                    if child.icon.IsSome then
                                                        Html.div [
                                                            prop.className "col-start-1 justify-self-start"
                                                            prop.children child.icon.Value
                                                        ]
                                                    else
                                                        Html.none
                                                    Html.div [
                                                        prop.className "col-start-2 justify-self-start"
                                                        prop.children child.text
                                                    ]
                                                    if child.kbdbutton.IsSome then
                                                        Html.div [
                                                            prop.className "col-start-3 justify-self-end"
                                                            prop.children child.kbdbutton.Value
                                                        ]
                                                    else
                                                        Html.none
                                                ]
                                                for key, v in props do
                                                    prop.custom (key, v)
                                            ]
                                    ]
                                ]
                        )
                )
            else
                Html.none
        )

    [<ReactComponent>]
    static member Example() =

        let containerRef = React.useElementRef ()

        Html.div [
            prop.className
                "w-full h-72 border border-dashed border-primary rounded flex items-center justify-center flex-col gap-4"
            prop.ref containerRef

            prop.children [
                Html.span [ prop.className "select-none"; prop.text "Click here for context menu!" ]
                Html.button [
                    prop.className "btn btn-primary"
                    prop.text "Example Table Cell"
                    prop.dataRow 12
                    prop.dataColumn 5
                ]
                ContextMenu.ContextMenu(
                    [
                        for i in 0..5 do
                            ContextMenuItem(
                                text = Html.span $"Item {i}",
                                ?icon = (if i = 4 then Html.i [ prop.className "fa-solid fa-check" ] |> Some else None),
                                ?kbdbutton = (if i = 3 then Html.kbd [ prop.className "ml-auto kbd kbd-sm"; prop.text "Back" ] |> Some else None),
                                ?label = (if i = 3 then Some "Back" else None),
                                onClick =
                                    (fun e ->
                                        e.buttonEvent.stopPropagation ()
                                        let index = e.spawnData |> unbox<CellCoordinate>
                                        console.log (sprintf "Item clicked: %i" i, index))
                            )
                    ],
                    ref = containerRef,
                    onSpawn =
                        (fun e ->
                            let target = e.target :?> HTMLElement
                            let tableCell = target.closest ("[data-row][data-column]")

                            match tableCell with
                            | Some cell ->
                                let cell = cell :?> HTMLElement
                                let row = int cell?dataset?row
                                let col = int cell?dataset?column
                                let indices: CellCoordinate = {| y = row; x = col |}
                                console.log (indices)
                                Some indices
                            | _ ->
                                console.log ("No table cell found")
                                None)
                )
            ]
        ]