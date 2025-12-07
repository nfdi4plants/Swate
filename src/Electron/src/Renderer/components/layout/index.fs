module Components.Layout

open Fable.Core
open Feliz
open Swate.Components

module Context =


    type LayoutContextType = StateContext<bool>

    module LayoutContextType =

        let Empty: LayoutContextType = { state = false; setState = ignore }

    let LeftSidebarContext =
        React.createContext<LayoutContextType> (LayoutContextType.Empty)

    let RightSidebarContext =
        React.createContext<LayoutContextType> (LayoutContextType.Empty)

open Context

[<Erase>]
type Layout =

    [<ReactComponent>]
    static member LeftSidebarBtn(iconClassName: string, tooltip: string, onClick: unit -> unit) =
        Html.div [
            prop.className "swt:tooltip swt:tooltip-right"
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

        Layout.LeftSidebarBtn(
            iconClassName = "swt:material-symbols-light--left-panel-close",
            tooltip = "Toggle sidebar",
            onClick = fun () -> ctx.setState (not ctx.state)
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

            let disp =
                FsReact.createDisposable (fun () ->
                    Browser.Dom.document.removeEventListener ("pointermove", unbox onMove)
                    Browser.Dom.document.removeEventListener ("pointerup", unbox stop)
                )

            disp
        )

        Html.div [
            prop.onPointerDown (fun _ -> dragging.current <- true)
            prop.className
                "swt:w-1 swt:cursor-col-resize swt:hover:bg-primary swt:absolute swt:top-0 swt:bottom-0 swt:-right-0.5 swt:transition-colors swt:select-none"
        ]

    [<ReactComponent>]
    static member LeftSidebar(children: ReactElement, content: ReactElement) =
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
                    let recalculatedWidth = clientX - sidebarDockerRef.current.Value.offsetLeft |> int

                    if recalculatedWidth < toggleSizeParams.ToggleWidth then
                        ctx.setState false
                    else
                        ctx.setState true

                        let clampedWidth =
                            if recalculatedWidth < toggleSizeParams.MinWidth then
                                toggleSizeParams.MinWidth
                            else
                                recalculatedWidth

                        setWidth clampedWidth
                ),
                [| box ctx.state |]
            )

        Html.div [
            prop.className "swt:flex swt:h-full swt:w-full swt:flex-row"
            prop.children [
                Html.div [ // left navbar with buttons
                    prop.testId "layout-main-left-navbar"
                    prop.className "swt:flex swt:flex-col swt:gap-2 swt:p-1 swt:border-r swt:border-base-300"
                    prop.children [ Layout.LeftSidebarToggleBtn() ]
                ]
                Html.div [
                    prop.testId "layout-main-left-sidebar"
                    prop.className [ "swt:flex swt:flex-row swt:grow" ]
                    prop.children [
                        Html.div [ // left dock area
                            prop.ref sidebarDockerRef
                            prop.style [ style.width (if ctx.state then width else 0) ]
                            prop.className
                                "swt:flex swt:flex-row swt:h-full swt:relative swt:border-r swt:border-base-300"
                            prop.children [
                                Html.div [
                                    prop.className [
                                        "swt:w-full swt:h-full"
                                        if not ctx.state then
                                            "swt:overflow-hidden"
                                    ]

                                    prop.children [ children ]
                                ]
                                Layout.ResizeHandler(setWidthResizeHandler)
                            ]
                        ]
                        Html.div [ // main content area
                            prop.className "swt:grow"
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:size-full"
                                    prop.children content
                                ]
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
                React.Fragment [
                    Html.div [
                        Html.div [
                            prop.className "swt:h-10"
                            prop.testId "layout-main-navbar"
                            prop.children navbar
                        ]
                        Html.div [
                            prop.className "swt:flex swt:flex-row swt:h-[calc(100vh-2.5rem)]"
                            prop.children [
                                Layout.LeftSidebar(
                                    leftSidebar,
                                    Html.div [
                                        prop.className "swt:flex swt:flex-row swt:grow"
                                        prop.children [
                                            Html.div [
                                                prop.className "swt:grow"
                                                prop.testId "layout-main-content"
                                                prop.children children
                                            ]
                                            Html.div [
                                                prop.testId "layout-main-right-sidebar"
                                                prop.children rightSidebar
                                            ]
                                        ]
                                    ]
                                )
                            ]
                        ]
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
            children = Layout.Wrapper "Main Content" "swt:bg-red-300 swt:text-black swt:h-full",
            navbar = Layout.Wrapper "Navbar" "swt:bg-blue-300 swt:text-black swt:h-full",
            leftSidebar =
                Html.ul [
                    prop.className "swt:menu swt:w-full swt:bg-base-200 swt:p-2 swt:rounded-box swt:h-full"
                    prop.children [
                        Html.li [ Html.a [ prop.text "Sidebar Item 1" ] ]
                        Html.li [ Html.a [ prop.text "Sidebar Item 2" ] ]
                        Html.li [ Html.a [ prop.text "Sidebar Item 3" ] ]
                    ]
                ],
            rightSidebar = Layout.Wrapper "Right Sidebar" "swt:bg-yellow-300 swt:text-black swt:h-full",
            sidebarLeftDefault = true,
            sidebarRightDefault = true
        )