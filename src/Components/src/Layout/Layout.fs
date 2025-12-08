namespace Swate.Components

open Fable.Core
open Feliz

module Context =

    type LayoutContextType = StateContext<bool>

    module LayoutContextType =

        let Empty: LayoutContextType = { state = false; setState = ignore }

    let LeftSidebarContext =
        React.createContext<LayoutContextType> (LayoutContextType.Empty)

    let RightSidebarContext =
        React.createContext<LayoutContextType> (LayoutContextType.Empty)

open Context

module private LayoutHelper =

    [<Literal>]
    let RightSidebarWidth = 300

    let MainContentMinWidth = 300

[<Erase; Mangle(false)>]
type Layout =

    [<ReactComponent>]
    static member LayoutBtn(iconClassName: string, tooltip: string, onClick: unit -> unit, ?tooltipClassName: string) =
        Html.div [
            prop.className [
                "swt:tooltip"
                tooltipClassName |> Option.defaultValue "swt:tooltip-right"
            ]
            prop.ariaLabel tooltip
            prop.children [
                Html.div [ prop.className "swt:tooltip-content"; prop.text tooltip ]
                Html.button [
                    prop.className "swt:btn swt:btn-square swt:btn-ghost swt:btn-sm"
                    prop.children [
                        Html.i [
                            prop.className ("swt:iconify " + iconClassName + " swt:size-6")
                        ]
                    ]
                    prop.onClick (fun _ -> onClick ())
                ]
            ]
        ]

    [<ReactComponent>]
    static member LeftSidebarToggleBtn() =
        let ctx = React.useContext (LeftSidebarContext)

        Layout.LayoutBtn(
            iconClassName =
                (if ctx.state then
                     "swt:material-symbols-light--left-panel-close"
                 else
                     "swt:material-symbols-light--left-panel-open"),
            tooltip = "Toggle left sidebar",
            onClick = fun () -> ctx.setState (not ctx.state)
        )

    [<ReactComponent>]
    static member RightSidebarToggleBtn() =
        let ctx = React.useContext RightSidebarContext

        Layout.LayoutBtn(
            iconClassName =
                (if ctx.state then
                     "swt:material-symbols-light--right-panel-close"
                 else
                     "swt:material-symbols-light--right-panel-open"),
            tooltip = "Toggle right sidebar",
            onClick = (fun () -> ctx.setState (not ctx.state)),
            tooltipClassName = "swt:tooltip-left"
        )

    [<ReactComponent>]
    static member ResizeHandler(setWidth: float -> unit) =
        let dragging = React.useRef false

        React.useEffectOnce (fun () ->

            let onMove =
                fun (e: Browser.Types.PointerEvent) -> if dragging.current then setWidth e.clientX else ()

            let rec stop =
                fun () ->
                    dragging.current <- false
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
            prop.className
                "swt:w-1 swt:cursor-col-resize swt:hover:bg-primary swt:absolute swt:top-0 swt:bottom-0 swt:-right-0.5 swt:transition-colors swt:select-none"
        ]

    [<ReactComponent>]
    static member LeftSidebar(content: ReactElement, children: ReactElement) =
        let ctx = React.useContext (LeftSidebarContext)

        let toggleSizeParams =
            React.useMemo (fun () -> {|
                MinWidth = 200
                // If it goes below this width, auto toggle the sidebar
                ToggleWidth = 75
                StartWidth = 400
            |})

        let width, setWidth =
            React.useLocalStorage ("swate-left-panel-width", toggleSizeParams.StartWidth)

        let sidebarDockerRef = React.useElementRef ()

        let setWidthResizeHandler =
            React.useCallback (
                (fun (clientX: float) ->
                    let windowX = Browser.Dom.window.innerWidth
                    let recalculatedWidth = clientX - sidebarDockerRef.current.Value.offsetLeft |> int

                    let MaxWidth =
                        int windowX - LayoutHelper.RightSidebarWidth - LayoutHelper.MainContentMinWidth

                    if recalculatedWidth < toggleSizeParams.ToggleWidth then
                        ctx.setState false
                    else
                        ctx.setState true

                        let clampedWidth =
                            if recalculatedWidth < toggleSizeParams.MinWidth then
                                toggleSizeParams.MinWidth
                            elif recalculatedWidth > MaxWidth then
                                MaxWidth
                            else
                                recalculatedWidth

                        setWidth clampedWidth
                ),
                [| box ctx.state |]
            )

        Html.div [
            prop.className "swt:flex swt:flex-row swt:size-full"
            prop.children [
                Html.div [ // left navbar with buttons
                    prop.testId "layout-main-left-navbar"
                    prop.className "swt:flex swt:flex-col swt:gap-2 swt:p-1 swt:border-r swt:border-base-content/50"
                    prop.children [ Layout.LeftSidebarToggleBtn() ]
                ]
                Html.div [
                    prop.testId "layout-main-left-sidebar"
                    prop.className [ "swt:flex swt:flex-row swt:grow" ]
                    prop.children [
                        Html.div [ // left dock area
                            prop.ref sidebarDockerRef
                            prop.style [
                                if not ctx.state then style.width 0 else style.width width
                            ]
                            prop.className
                                "swt:flex swt:flex-row swt:h-full swt:relative swt:border-r swt:border-base-content/50 swt:bg-base-100"
                            prop.children [
                                Html.div [
                                    prop.className [
                                        "swt:w-full swt:h-full swt:overflow-x-scroll swt:overflow-y-auto"
                                    ]

                                    prop.children content
                                ]
                                Layout.ResizeHandler(setWidthResizeHandler)
                            ]
                        ]
                        Html.div [ // main content area
                            prop.className "swt:grow"
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:size-full"
                                    prop.children children
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]


    [<ReactComponent>]
    static member RightSidebar(content: ReactElement, children: ReactElement) =

        let ctx = React.useContext (RightSidebarContext)

        Html.div [
            prop.className [
                "swt:drawer swt:drawer-end swt:drawer-open swt:h-[calc(100dvh-2.5rem)]! swt:overflow-y-hidden"
            ]
            prop.children [
                Html.input [
                    prop.id "right-sidebar-toggle"
                    prop.className "swt:drawer-toggle"
                    prop.type'.checkbox
                    prop.defaultChecked true
                ]
                Html.div [
                    prop.className "swt:drawer-content swt:flex swt:h-[calc(100dvh-2.5rem)]!"
                    prop.children children
                ]
                Html.div [
                    prop.testId "layout-main-right-sidebar"
                    prop.style [ style.direction.rightToLeft ]
                    prop.style [ style.width LayoutHelper.RightSidebarWidth ]
                    prop.className [
                        "swt:drawer-side swt:z-10 swt:bg-base-100 swt:border-l swt:border-base-content"
                        if not ctx.state then
                            "swt:w-0!"
                    ]
                    prop.children [
                        Html.div [
                            prop.className "swt:size-full"
                            prop.style [ style.direction.leftToRight ]
                            prop.children [
                                if ctx.state then
                                    content
                            ]
                        ]
                    ]
                ]
            ]
        ]


    [<ReactComponent(true)>]
    static member Main
        (
            children: ReactElement,
            navbar: ReactElement,
            leftSidebar: ReactElement,
            rightSidebar: ReactElement,
            ?sidebarLeftDefault: bool,
            ?sidebarRightDefault: bool
        ) =

        let leftSidebarState, setLeftSidebarState =
            React.useState (defaultArg sidebarLeftDefault false)

        let rightSidebarState, setRightSidebarState =
            React.useState (defaultArg sidebarRightDefault false)

        RightSidebarContext.Provider(
            {
                state = rightSidebarState
                setState = setRightSidebarState
            },
            LeftSidebarContext.Provider(
                {
                    state = leftSidebarState
                    setState = setLeftSidebarState
                },
                Html.div [
                    prop.className "swt:flex-1 swt:flex swt:flex-col"
                    prop.children [
                        Html.div [ // navbar
                            prop.className
                                "swt:h-10 swt:flex swt:flex-row swt:items-center swt:grow-0 swt:border-b swt:border-base-content"
                            prop.testId "layout-main-navbar"
                            prop.children [
                                navbar
                                Html.div [
                                    prop.className "swt:ml-auto"
                                    prop.children [ Layout.RightSidebarToggleBtn() ]
                                ]
                            ]
                        ]
                        Layout.RightSidebar(
                            rightSidebar,
                            Html.div [ // content + left side docking
                                prop.className "swt:size-full"
                                prop.children [
                                    Layout.LeftSidebar( // left sidebar + docking
                                        leftSidebar,
                                        Html.div [
                                            prop.className "swt:grow"
                                            prop.testId "layout-main-content"
                                            prop.children children
                                        ]
                                    )
                                ]
                            ]
                        )
                    ]
                ]
            )
        )


    [<ReactComponent>]
    static member private Wrapper (txt: string) (className: string) =
        Html.div [
            prop.className [
                "swt:flex swt:grow swt:items-center swt:justify-center"
                className
            ]
            prop.text txt
        ]

    [<ReactComponent>]
    static member Entry() =

        Layout.Main(
            children = Layout.Wrapper "Main Content" "swt:bg-base-300 swt:h-full",
            navbar = Layout.Wrapper "Navbar" "swt:swt:h-full",
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
            sidebarLeftDefault = true,
            sidebarRightDefault = true
        )