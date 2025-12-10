namespace Swate.Components

open Fable.Core
open Browser.Types
open Feliz

module FloatingUI =

    [<StringEnum(CaseRules.KebabCase)>]
    type Placement =
        | Top
        | TopStart
        | TopEnd
        | Right
        | RightStart
        | RightEnd
        | Bottom
        | BottomStart
        | BottomEnd
        | Left
        | LeftStart
        | LeftEnd

    [<AllowNullLiteral; Global>]
    type VirtualElement [<ParamObjectAttribute; Emit("$0")>] (getBoundingClientRect: unit -> ClientRect) =
        member val getBoundingClientRect = getBoundingClientRect with get, set

    type ReferenceElement = U2<HTMLElement, VirtualElement>

    [<AllowNullLiteral; Import("UseFloatingReturn", "@floating-ui/react")>]
    type UseFloatingReturn
        [<ParamObjectAttribute; Emit("$0")>]
        (
            context: obj,
            placement: Placement,
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
                    setReference: IRefValue<HTMLElement option>
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
        member val selectedIndex: int option = selectedIndex
        member val onTypingChange = onTypingChange
        member val findMatch = findMatch

    [<Erase>]
    type IMiddleware = interface end

    [<Erase>]
    type Middleware =
        [<ImportMemberAttribute("@floating-ui/react")>]
        static member offset(?options: obj) : IMiddleware = jsNative

        [<ImportMemberAttribute("@floating-ui/react")>]
        static member flip(?options: obj) : IMiddleware = jsNative

        [<ImportMemberAttribute("@floating-ui/react")>]
        static member shift(?options: obj) : IMiddleware = jsNative

        [<ImportMemberAttribute("@floating-ui/react")>]
        static member size(?options: obj) : IMiddleware = jsNative

    [<StringEnum(CaseRules.LowerAll)>]
    type Status =
        | Unmounted
        | Initial
        | Open
        | Close


    type UseTransitionStatusReturn =
        abstract member isMounted: bool
        abstract member status: Status

[<Erase>]
type FloatingUI =

    [<ImportMember("@floating-ui/react")>]
    static member useId() : string = jsNative

    // reference: ReferenceElement, floating: FloatingElement, update: () => void
    [<ImportMember("@floating-ui/react")>]
    static member autoUpdate (reference: obj) (floating: obj) (update: unit -> unit) : unit = jsNative

    [<ImportMember("@floating-ui/react"); ParamObjectAttribute>]
    static member useFloating
        (
            ?``open``: bool,
            ?onOpenChange: bool -> unit,
            ?placement: FloatingUI.Placement,
            ?strategy: string,
            ?transform: bool,
            ?middleware: FloatingUI.IMiddleware[],
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
    static member useClick(context: obj) : obj = jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useListNavigation(context: obj, ?props: FloatingUI.UseListNavigationProps) : obj = jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useTypeahead(context: obj, ?props: FloatingUI.UseTypeaheadProps) : obj = jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useInteractions(interactions: obj[]) : FloatingUI.UseInteractionsReturn = jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useListItem
        (?label: string)
        : {|
              ref: IRefValue<#Browser.Types.HTMLElement option>
              index: int
          |}
        =
        jsNative

    [<ImportMember("@floating-ui/react")>]
    static member useTransitionStatus(context: obj) : FloatingUI.UseTransitionStatusReturn = jsNative

    [<ReactComponent("FloatingPortal", "@floating-ui/react")>]
    static member FloatingPortal(children: ReactElement) = React.Imported()

    [<ReactComponent("FloatingOverlay", "@floating-ui/react")>]
    static member FloatingOverlay(children: ReactElement, ?lockScroll: bool, ?className: string) = React.Imported()

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
            ?order: string[],
            ?key: string
        ) =
        React.Imported()

    [<ReactComponent("FloatingList", "@floating-ui/react")>]
    static member FloatingList
        (
            children: ReactElement,
            elementsRef: IRefValue<Browser.Types.HTMLElement option[]>,
            labelsRef: IRefValue<string option[]>
        ) =
        React.Imported()