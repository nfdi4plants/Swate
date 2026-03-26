namespace Swate.Components

open Feliz
open Fable.Core


module private Mocks =

    [<StringEnum>]
    type LeftSidebarStateMock =
        | Home
        | Settings
        | Info

module LayoutContext =

    type LayoutContextType = StateContext<bool>

    type SidebarState<'A> = {
        isOpen: bool
        setIsOpen: bool -> unit
        sidebarType: 'A
        setSidebarType: 'A -> unit
    } with

        static member Empty<'A>() : SidebarState<'A> = {
            isOpen = false
            setIsOpen = ignore
            sidebarType = Unchecked.defaultof<'A>
            setSidebarType = ignore
        }

    module LayoutContextType =

        let Empty: LayoutContextType = { state = false; setState = ignore }

    /// Holds one stable React context instance for left sidebar. Otherwise we run into consistency issues with a generic argument
    type private LeftSidebarContextHolder<'A>() =
        static member val Context = React.createContext<SidebarState<'A>> (SidebarState<'A>.Empty()) with get

    let LeftSidebarContext<'A> = LeftSidebarContextHolder<'A>.Context

    let RightSidebarContext =
        React.createContext<LayoutContextType> (LayoutContextType.Empty)

open LayoutContext

module private LayoutHelper =

    module Sidebar =
        [<Literal>]
        let DefaultWidth = 300

        [<Literal>]
        let MinWidth = 200

        /// If it goes below this width, auto toggle the sidebar
        [<Literal>]
        let ToggleWidth = 75

        [<RequireQualifiedAccess>]
        type Side =
            | Left
            | Right

    [<Literal>]
    let MainContentMinWidth = 300

    let clamp (min: int, max: int, value: int) : int =
        if value < min then min
        elif value > max then max
        else value

    type ResizeState =
        | CloseTargetSidebar
        | ResizeTarget of int
        | ResizeTargetCollapseOther of int
        | ResizeBoth of int * int

    let calcResize (newWidth: int) (windowX: int) (otherWidth: int) =

        if newWidth < Sidebar.ToggleWidth then // collapse resizing sidebar
            CloseTargetSidebar
        elif newWidth < Sidebar.MinWidth then // resize to min width
            ResizeTarget Sidebar.MinWidth
        else // other sidebar is open and may be resized
            let mainContent = windowX - newWidth - otherWidth

            if mainContent >= MainContentMinWidth then // enough main content space, just resize
                ResizeTarget newWidth
            else // must change other sidebar
                let deficit = MainContentMinWidth - mainContent // how much to take from other sidebar
                let newRightWidth = otherWidth - deficit

                if newRightWidth < Sidebar.MinWidth then // collapse other sidebar if below min
                    let maxWidth = windowX - MainContentMinWidth
                    let clampedWidth = clamp (Sidebar.MinWidth, maxWidth, newWidth)

                    ResizeTargetCollapseOther clampedWidth
                else // resize both
                    ResizeBoth(newWidth, newRightWidth)



open LayoutHelper
open Fable.Core.JsInterop

[<Erase; Mangle(false)>]
type Layout =

    [<ReactComponent>]
    static member LayoutBtn
        (iconClassName: string, tooltip: string, onClick: unit -> unit, ?tooltipClassName: string, ?isActive: bool)
        =
        let isActive = defaultArg isActive false

        Html.div [
            prop.className [
                "swt:tooltip swt:border-r-2"
                tooltipClassName |> Option.defaultValue "swt:tooltip-right"
                if isActive then
                    "swt:border-primary"
                else
                    "swt:border-transparent"
            ]
            prop.ariaLabel tooltip
            prop.children [
                Html.div [ prop.className "swt:tooltip-content"; prop.text tooltip ]
                Html.button [
                    prop.className "swt:btn swt:btn-square swt:btn-ghost swt:btn-sm"
                    prop.children [
                        Html.i [ prop.className ("swt:iconify " + iconClassName) ]
                    ]
                    prop.onClick (fun _ -> onClick ())
                ]
            ]
        ]

    [<ReactComponent>]
    static member LeftSidebarToggleBtn(?activeBorderStyle: bool) =
        let ctx = React.useContext (LeftSidebarContext)
        let showIsActive = activeBorderStyle |> Option.map (fun _ -> ctx.isOpen)

        Layout.LayoutBtn(
            iconClassName =
                (if ctx.isOpen then
                     "swt:fluent--panel-left-48-filled"
                 else
                     "swt:fluent--panel-left-48-regular"),
            tooltip = "Toggle left sidebar",
            tooltipClassName = "swt:tooltip-left",
            ?isActive = showIsActive,
            onClick = fun () -> ctx.setIsOpen (not ctx.isOpen)
        )

    [<ReactComponent>]
    static member RightSidebarToggleBtn(?activeBorderStyle: bool) =
        let ctx = React.useContext RightSidebarContext
        let showIsActive = activeBorderStyle |> Option.map (fun _ -> ctx.state)

        Layout.LayoutBtn(
            iconClassName =
                (if ctx.state then
                     "swt:fluent--panel-right-48-filled"
                 else
                     "swt:fluent--panel-right-48-regular"),
            tooltip = "Toggle right sidebar",
            onClick = (fun () -> ctx.setState (not ctx.state)),
            tooltipClassName = "swt:tooltip-left",
            ?isActive = showIsActive
        )

    [<ReactComponent>]
    static member private ResizeHandler(setPosition: float option -> unit, side: Sidebar.Side) =
        let dragging = React.useRef false

        React.useEffectOnce (fun () ->

            let onMove =
                fun (e: Browser.Types.PointerEvent) ->
                    if dragging.current then
                        Some e.clientX |> setPosition
                    else
                        ()

            let rec stop =
                fun () ->
                    dragging.current <- false
                    setPosition None
                    Browser.Dom.document.removeEventListener ("pointermove", unbox onMove)
                    Browser.Dom.document.removeEventListener ("pointerup", unbox stop)

            Browser.Dom.document.addEventListener ("pointermove", unbox onMove)
            Browser.Dom.document.addEventListener ("pointerup", unbox stop)

            FsReact.createDisposable (fun () ->
                Browser.Dom.document.removeEventListener ("pointermove", unbox onMove)
                Browser.Dom.document.removeEventListener ("pointerup", unbox stop)
            )
        )

        Html.div [
            prop.onPointerDown (fun _ -> dragging.current <- true)
            prop.style [
                match side with
                | Sidebar.Side.Left -> style.right -1
                | Sidebar.Side.Right -> style.left -1
            ]
            prop.className
                "swt:w-1 swt:cursor-col-resize swt:hover:bg-primary swt:absolute swt:top-0 swt:bottom-0 swt:transition-colors swt:select-none"
        ]

    [<ReactMemoComponent(AreEqualFn.FsEquals)>]
    static member private SidebarActions(children: ReactElement, side: Sidebar.Side, ?key: string) =
        Html.div [
            prop.testId (
                match side with
                | Sidebar.Side.Left -> "layout-main-left-navbar"
                | Sidebar.Side.Right -> "layout-main-right-navbar"
            )
            prop.className [
                "swt:flex swt:flex-col swt:gap-2 swt:p-1 swt:border-base-content/50"
                match side with
                | Sidebar.Side.Left -> "swt:border-r"
                | Sidebar.Side.Right -> "swt:border-l"
            ]
            prop.children children
        ]

    [<ReactMemoComponent>]
    static member private SidebarArea
        (
            children: ReactElement,
            width: int,
            isOpen,
            ref: IRefValue<option<Browser.Types.HTMLElement>>,
            setPointerPosition,
            side: Sidebar.Side
        ) =
        Html.div [
            prop.ref ref
            prop.style [
                if not isOpen then style.width 0 else style.width width
            ]
            prop.className [
                "swt:flex swt:flex-row swt:h-full swt:relative swt:border-base-content/50 swt:bg-base-100"
                match side with
                | Sidebar.Side.Left -> "swt:border-r"
                | Sidebar.Side.Right -> "swt:border-l"
            ]
            prop.children [
                Html.div [
                    prop.className [
                        "swt:w-full swt:h-full swt:overflow-x-hidden swt:overflow-y-auto"
                        "swt:scrollbar-fade"
                    ]
                    prop.children children
                ]
                Layout.ResizeHandler(setPointerPosition, side)
            ]
        ]

    [<ReactMemoComponent>]
    static member private Sidebars
        (
            children: ReactElement,
            ?leftContent: ReactElement,
            ?leftActions: ReactElement,
            ?rightContent: ReactElement,
            ?rightActions: ReactElement,
            ?hasNavbar: bool
        ) =
        let hasNavbar = defaultArg hasNavbar false
        let ctxLeft = React.useContext (LeftSidebarContext)
        console.log ctxLeft
        let ctxRight = React.useContext (RightSidebarContext)

        let widthLeft, setWidthLeft =
            React.useLocalStorage (
                Keys.mkLocalStorageKey "layout" "main" "leftSidebarWidth",
                LayoutHelper.Sidebar.DefaultWidth
            )

        let widthRight, setWidthRight =
            React.useLocalStorage (
                Keys.mkLocalStorageKey "layout" "main" "rightSidebarWidth",
                LayoutHelper.Sidebar.DefaultWidth
            )

        let StartResizeState = React.useRef (None: {| isOpen: bool; width: int |} option)

        let leftPointerPosition, setLeftPointerPosition =
            React.useState (None: float option)

        let setLeftPointerPositionWrapper =
            function
            | Some pos ->
                if StartResizeState.current.IsNone then
                    StartResizeState.current <-
                        Some {|
                            isOpen = ctxRight.state
                            width = widthRight
                        |}

                setLeftPointerPosition (Some pos)
            | None ->
                StartResizeState.current <- None
                setLeftPointerPosition None


        let throttledLeftPointerPosition = React.useThrottle (leftPointerPosition, 16)

        let rightPointerPosition, setRightPointerPosition =
            React.useState (None: float option)

        let setRightPointerPositionWrapper =
            function
            | Some pos ->
                if StartResizeState.current.IsNone then
                    StartResizeState.current <-
                        Some {|
                            isOpen = ctxLeft.isOpen
                            width = widthLeft
                        |}

                setRightPointerPosition (Some pos)
            | None ->
                StartResizeState.current <- None
                setRightPointerPosition None

        let throttledRightPointerPosition = React.useThrottle (rightPointerPosition, 16)

        let leftRef = React.useElementRef ()
        let rightRef = React.useElementRef ()

        React.useEffect (
            (fun () ->
                if
                    throttledLeftPointerPosition.IsSome
                    && StartResizeState.current.IsSome
                    && leftRef.current.IsSome
                then
                    let windowX = int Browser.Dom.window.innerWidth

                    let newWidth =
                        throttledLeftPointerPosition.Value - leftRef.current.Value.offsetLeft |> int

                    match calcResize newWidth windowX widthRight with
                    | CloseTargetSidebar ->
                        ctxRight.setState StartResizeState.current.Value.isOpen
                        ctxLeft.setIsOpen false
                    | ResizeTarget width ->
                        ctxRight.setState StartResizeState.current.Value.isOpen
                        ctxLeft.setIsOpen true
                        setWidthLeft width
                    | ResizeBoth(leftWidth, rightWidth) ->
                        ctxRight.setState StartResizeState.current.Value.isOpen
                        setWidthLeft leftWidth
                        setWidthRight rightWidth
                    | ResizeTargetCollapseOther width ->
                        setWidthLeft width
                        ctxRight.setState false
            ),
            [| box throttledLeftPointerPosition |]
        )

        React.useEffect (
            (fun () ->

                if
                    throttledRightPointerPosition.IsSome
                    && StartResizeState.current.IsSome
                    && rightRef.current.IsSome
                then
                    let windowX = int Browser.Dom.window.innerWidth

                    let offsetRight =
                        rightRef.current.Value.offsetLeft + rightRef.current.Value.clientWidth

                    let newWidth = int offsetRight - int throttledRightPointerPosition.Value

                    match calcResize newWidth windowX widthLeft with
                    | CloseTargetSidebar ->
                        ctxLeft.setIsOpen StartResizeState.current.Value.isOpen
                        ctxRight.setState false
                    | ResizeTarget width ->
                        ctxLeft.setIsOpen StartResizeState.current.Value.isOpen
                        ctxRight.setState true
                        setWidthRight width
                    | ResizeBoth(rightWidth, leftWidth) ->
                        ctxLeft.setIsOpen StartResizeState.current.Value.isOpen
                        setWidthRight rightWidth
                        setWidthLeft leftWidth
                    | ResizeTargetCollapseOther width ->
                        setWidthRight width
                        ctxLeft.setIsOpen false
            ),
            [| box throttledRightPointerPosition |]
        )

        Html.div [
            prop.className [
                "swt:flex swt:flex-row swt:w-full"
                if hasNavbar then
                    "swt:grow-0 swt:h-[calc(100%-2.5rem)]"
                else
                    "swt:grow swt:h-full"
            ]
            prop.children [
                if leftActions.IsSome then
                    Layout.SidebarActions(leftActions.Value, Sidebar.Side.Left)
                if leftContent.IsSome then
                    Layout.SidebarArea(
                        leftContent.Value,
                        widthLeft,
                        ctxLeft.isOpen,
                        leftRef,
                        setLeftPointerPositionWrapper,
                        Sidebar.Side.Left
                    )
                Html.div [
                    prop.className "swt:grow"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:size-full"
                            prop.children children
                        ]
                    ]
                ]
                if rightContent.IsSome then
                    Layout.SidebarArea(
                        rightContent.Value,
                        widthRight,
                        ctxRight.state,
                        rightRef,
                        setRightPointerPositionWrapper,
                        Sidebar.Side.Right
                    )
                if rightActions.IsSome then
                    Layout.SidebarActions(rightActions.Value, Sidebar.Side.Right)
            ]
        ]

    [<ReactComponent(true)>]
    static member Main<'A>
        (
            children: ReactElement,
            ?navbar: ReactElement,
            ?leftSidebar: ReactElement,
            ?rightSidebar: ReactElement,
            ?leftActions: ReactElement,
            ?rightActions: ReactElement,
            ?leftSidebarState: SidebarState<'A>,
            ?sidebarRightDefault: bool
        ) =

        let sidebarRightDefault = defaultArg sidebarRightDefault false

        let rightSidebarState, setRightSidebarState =
            React.useLocalStorage (Keys.mkLocalStorageKey "layout" "main" "rightSidebarOpen", sidebarRightDefault)

        let leftSidebarState = defaultArg leftSidebarState (SidebarState.Empty())
        console.log leftSidebarState

        let navbar = React.useMemo ((fun () -> navbar), [| box navbar |])

        let leftSidebar = React.useMemo ((fun () -> leftSidebar), [| box leftSidebar |])

        let rightSidebar = React.useMemo ((fun () -> rightSidebar), [| box rightSidebar |])
        let leftActions = React.useMemo ((fun () -> leftActions), [| box leftActions |])
        let rightActions = React.useMemo ((fun () -> rightActions), [| box rightActions |])

        let children =
            React.useMemo (
                (fun () ->
                    Html.div [
                        prop.className "swt:grow"
                        prop.testId "layout-main-content"
                        prop.children children
                    ]
                ),
                [| box children |]
            )

        RightSidebarContext.Provider(
            {
                state = rightSidebarState
                setState = setRightSidebarState
            },
            LeftSidebarContext.Provider(
                leftSidebarState,
                Html.div [
                    prop.className "swt:flex-1 swt:flex swt:flex-col swt:h-screen swt:overflow-hidden"
                    prop.children [
                        if navbar.IsSome then
                            Html.div [
                                prop.className
                                    "swt:h-12 swt:flex swt:flex-row swt:items-center swt:grow-0 swt:border-b swt:border-base-content/50"
                                prop.testId "layout-main-navbar"
                                prop.children navbar.Value
                            ]
                        Layout.Sidebars(
                            children,
                            ?leftContent = leftSidebar,
                            ?leftActions = leftActions,
                            ?rightContent = rightSidebar,
                            ?rightActions = rightActions,
                            hasNavbar = navbar.IsSome
                        )
                    ]
                ]
            )
        )


    [<ReactComponent>]
    static member private Wrapper (children: ReactElement) (className: string) =
        Html.div [
            prop.className [
                "swt:flex swt:flex-col swt:gap-2 swt:grow swt:items-center swt:justify-center"
                className
            ]
            prop.children children
        ]

    [<ReactComponent>]
    static member private TestBtn() =
        let ctx = React.useContext (LeftSidebarContext)

        Html.button [
            prop.className "swt:btn"
            prop.onClick (fun _ -> console.log ctx)
            prop.text "DEBUG"
        ]

    [<ReactComponent>]
    static member Entry() =

        let leftSidebarTarget, setLeftSidebarTarget =
            React.useLocalStorage (
                Keys.mkLocalStorageKey "layout" "main" "leftSidebarTarget",
                Mocks.LeftSidebarStateMock.Home
            )

        let leftSidebarIsOpen, setLeftSidebarIsOpen =
            React.useLocalStorage (Keys.mkLocalStorageKey "layout" "main" "leftSidebarIsOpen", true)

        let toggleLeftSidebarTarget =
            fun target ->
                if leftSidebarTarget = target then
                    setLeftSidebarIsOpen (not leftSidebarIsOpen)
                else
                    setLeftSidebarIsOpen true
                    setLeftSidebarTarget target

        Layout.Main(
            children =
                Layout.Wrapper
                    (React.Fragment [
                        Html.div "Main Content"
                        Html.div (string leftSidebarIsOpen)
                        Html.div (string leftSidebarTarget)
                    ])
                    "swt:bg-base-300 swt:h-full",
            navbar = Navbar.Entry(),
            leftSidebar =
                Html.ul [
                    prop.className "swt:menu swt:w-full swt:p-2 swt:rounded-box swt:h-full swt:flex-nowrap"
                    prop.children [
                        for i in 0..100 do
                            Html.li [ Html.a [ prop.text $"Sidebar Item {i}" ] ]
                    ]
                ],
            rightSidebar =
                Html.ul [
                    prop.className "swt:menu swt:w-full swt:flex-nowrap swt:p-2 swt:h-full"
                    prop.children [
                        for i in 0..100 do
                            Html.li [ Html.a [ prop.text $"Right Sidebar Item {i}" ] ]
                    ]
                ],
            rightActions =
                React.Fragment [
                    Layout.LayoutBtn(
                        iconClassName = "swt:fluent--search-24-regular",
                        tooltip = "Search",
                        onClick = fun () -> Browser.Dom.window.alert "Search clicked"
                    )
                    Layout.LayoutBtn(
                        iconClassName = "swt:fluent--info-24-regular",
                        tooltip = "Help",
                        onClick = fun () -> Browser.Dom.window.alert "Help clicked"
                    )
                ],
            leftActions =
                React.Fragment [
                    Layout.LayoutBtn(
                        iconClassName = "swt:fluent--home-24-regular",
                        tooltip = "Home",
                        isActive = (leftSidebarTarget = Mocks.LeftSidebarStateMock.Home),
                        onClick = fun () -> toggleLeftSidebarTarget Mocks.LeftSidebarStateMock.Home
                    )
                    Layout.LayoutBtn(
                        iconClassName = "swt:fluent--settings-24-regular",
                        tooltip = "Settings",
                        isActive = (leftSidebarTarget = Mocks.LeftSidebarStateMock.Settings),
                        onClick = fun () -> toggleLeftSidebarTarget Mocks.LeftSidebarStateMock.Settings
                    )
                    Layout.LayoutBtn(
                        iconClassName = "swt:fluent--info-24-regular",
                        tooltip = "Info",
                        isActive = (leftSidebarTarget = Mocks.LeftSidebarStateMock.Info),
                        onClick = fun () -> toggleLeftSidebarTarget Mocks.LeftSidebarStateMock.Info
                    )
                ],
            leftSidebarState = {
                isOpen = leftSidebarIsOpen
                setIsOpen = setLeftSidebarIsOpen
                sidebarType = leftSidebarTarget
                setSidebarType = setLeftSidebarTarget
            },
            sidebarRightDefault = true
        )