namespace Renderer.components.Widgets

open Feliz

open ARCtrl

open Swate.Components

open LocalStorage.Widgets

open MainComponents
open MainComponents.InitExtensions

type Widget =

    static member AddWidget widgets setWidgets (widget: MainComponents.Widget) =
        let add widget widgets =
            widget :: widgets |> List.rev |> setWidgets

        if widgets |> List.contains widget then
            List.filter (fun w -> w <> widget) widgets
            |> fun filteredWidgets -> add widget filteredWidgets
        else
            add widget widgets

    [<ReactComponent>]
    static member BaseWidget
        (prefix: string)
        (content: ReactElement)
        (onRemove: Browser.Types.MouseEvent -> unit)
        (allowContentOverflowVisible: bool)
        =
        let position, setPosition = React.useState (fun _ -> Rect.initPositionFromPrefix prefix)
        let size, setSize = React.useState (fun _ -> Rect.initSizeFromPrefix prefix)

        let innerWidth, setInnerWidth = React.useState (fun _ -> Browser.Dom.window.innerWidth)
        let innerHeight, setInnerHeight = React.useState (fun _ -> Browser.Dom.window.innerHeight)

        let element = React.useElementRef ()

        ResizeEventListener.windowSizeChange setInnerWidth setInnerHeight

        let debouncedAdaptElement =
            React.useDebouncedCallback (
                fun () ->
                    match position, size with
                    | Some p, Some s ->
                        ResizeEventListener.adaptElement (int innerWidth) (int innerHeight) s p setSize setPosition
                    | _ -> ()
                , 100
            )

        React.useEffectOnce (fun _ -> debouncedAdaptElement ())
        React.useEffect ((fun () -> debouncedAdaptElement ()), [| box innerWidth; box innerHeight |])

        React.useLayoutEffectOnce (fun _ ->
            position
            |> Option.iter (fun p ->
                MoveEventListener.ensurePositionInsideWindow element p
                |> Some
                |> setPosition
            )
        )

        let startMove (e: Browser.Types.MouseEvent) =
            e.preventDefault()
            e.stopPropagation()

            let startMouse = { X = int e.clientX; Y = int e.clientY }

            let startPos =
                match position with
                | Some p -> p
                | None ->
                    { X = int element.current.Value.offsetLeft
                      Y = int element.current.Value.offsetTop }

            let onmousemove : Browser.Types.Event -> unit =
                fun ev ->
                    let me = ev :?> Browser.Types.MouseEvent
                    let dx = int me.clientX - startMouse.X
                    let dy = int me.clientY - startMouse.Y
                    setPosition (Some { X = startPos.X + dx; Y = startPos.Y + dy })

            let onmouseup : Browser.Types.Event -> unit =
                fun _ ->
                    Browser.Dom.document.removeEventListener("mousemove", onmousemove)

            Browser.Dom.document.addEventListener("mousemove", onmousemove)
            let opts = Fable.Core.JsInterop.createEmpty<Browser.Types.AddEventListenerOptions>
            opts.once <- true
            Browser.Dom.document.addEventListener("mouseup", onmouseup, opts)

        let startResize (e: Browser.Types.MouseEvent) =
            e.preventDefault()
            e.stopPropagation()

            let startPosition : Rect = { X = int e.clientX; Y = int e.clientY }
            let startSize : Rect = { X = int element.current.Value.offsetWidth; Y = int element.current.Value.offsetHeight }

            let onmousemove = ResizeEventListener.onmousemove startPosition startSize setSize
            let onmouseup = fun _ -> ResizeEventListener.onmouseup (prefix, element) onmousemove

            Browser.Dom.document.addEventListener("mousemove", onmousemove)
            let opts = Fable.Core.JsInterop.createEmpty<Browser.Types.AddEventListenerOptions>
            opts.once <- true
            Browser.Dom.document.addEventListener("mouseup", onmouseup, opts)

        Html.div [
            prop.ref element
            prop.className "swt:shadow-md swt:border swt:border-base-300 swt:rounded-lg swt:border-r-2 swt:bg-base-100"
            prop.style [
                style.zIndex 999
                style.position.fixedRelativeToWindow
                style.display.flex
                style.flexDirection.column

                if size.IsSome then
                    style.width size.Value.X
                    style.height size.Value.Y

                if position.IsNone then
                    style.top (length.perc 20)
                    style.left (length.perc 20)
                else
                    style.top position.Value.Y
                    style.left position.Value.X
            ]
            prop.children [
                Html.div [
                    prop.className "swt:cursor-move swt:flex swt:justify-between swt:items-center swt:px-2 swt:py-1 swt:bg-gradient-to-br swt:from-primary swt:to-base-200 swt:rounded-t-lg"
                    prop.onMouseDown startMove
                    prop.children [
                        Html.span [ prop.className "swt:text-sm"; prop.text prefix ]
                        Html.button [
                            prop.className "swt:btn swt:btn-ghost swt:btn-xs"
                            prop.text "x"
                            prop.onClick onRemove
                        ]
                    ]
                ]
                Html.div [
                    prop.className [
                        "swt:flex-1 swt:p-2"
                        if allowContentOverflowVisible then
                            "swt:overflow-visible"
                        else
                            "swt:overflow-auto"
                    ]
                    prop.children [ content ]
                ]
                Html.div [
                    prop.className "swt:opacity-60 hover:swt:opacity-100"
                    prop.style [
                        style.position.absolute
                        style.right 6
                        style.bottom 6
                        style.width 14
                        style.height 14
                        style.cursor.northWestSouthEastResize
                    ]
                    prop.onMouseDown startResize
                ]
            ]
        ]

    static member prefixOf (w: MainComponents.Widget) =
        match w with
        | MainComponents.Widget._Template -> WidgetLiterals.Templates
        | MainComponents.Widget._FilePicker -> WidgetLiterals.FilePicker
        | MainComponents.Widget._DataAnnotator -> WidgetLiterals.DataAnnotator
        | MainComponents.Widget._BuildingBlock -> WidgetLiterals.BuildingBlock

    [<ReactComponent>]
    static member CreateBuildingBlockWidget (activeTableData: ActiveTableData option) (onTableMutated: unit -> unit) =
        Html.div [
            prop.title "Building Block"
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:overflow-visible"
            prop.children [
                BasicComponent.Main(activeTableData, onTableMutated)
            ]
        ]

    [<ReactComponent>]
    static member CreateTemplateWidget (activeTableData: ActiveTableData option) (onTableMutated: unit -> unit) =
        Html.div [
            prop.title "Template"
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:overflow-y-hidden"
            prop.children [
                TemplateWidget.Main(activeTableData, onTableMutated)
            ]
        ]

    [<ReactComponent>]
    static member CreateFilePickerWidget
        (activeTableData: ActiveTableData option)
        (activeDataMapData: ActiveDataMapData option)
        (onTableMutated: unit -> unit)
        =
        Html.div [
            prop.title "FilePicker"
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:overflow-y-hidden"
            prop.children [
                FilePickerWidget.Main(activeTableData, activeDataMapData, onTableMutated)
            ]
        ]

    [<ReactComponent>]
    static member WidgetView
        (widget: MainComponents.Widget)
        (onRemove: Browser.Types.MouseEvent -> unit)
        (onBringToFront: unit -> unit)
        (activeTableData: ActiveTableData option)
        (activeDataMapData: ActiveDataMapData option)
        (onTableMutated: unit -> unit)
        =
        let content =
            match widget with
            | MainComponents.Widget._BuildingBlock -> Widget.CreateBuildingBlockWidget activeTableData onTableMutated
            | MainComponents.Widget._Template -> Widget.CreateTemplateWidget activeTableData onTableMutated
            | MainComponents.Widget._FilePicker -> Widget.CreateFilePickerWidget activeTableData activeDataMapData onTableMutated
            | MainComponents.Widget._DataAnnotator ->
                Html.div [
                    prop.title "DataAnnotator"
                    prop.className "swt:flex swt:flex-col swt:gap-2 swt:overflow-y-hidden"
                    prop.children [
                        DataAnnotatorWidget.Main(activeTableData, activeDataMapData, onTableMutated)
                    ]
                ]

        Html.div [
            prop.onMouseDown (fun _ -> onBringToFront())
            prop.children [
                Widget.BaseWidget (Widget.prefixOf widget) content onRemove (widget = MainComponents.Widget._BuildingBlock)
            ]
        ]

    [<ReactComponent>]
    static member FloatingWidgetLayer
        (widgets: MainComponents.Widget list)
        (setWidgets: MainComponents.Widget list -> unit)
        (activeTableData: ActiveTableData option)
        (activeDataMapData: ActiveDataMapData option)
        (onTableMutated: unit -> unit)
        =

        let rmvWidget w = widgets |> List.except [w] |> setWidgets
        let bringToFront w =
            widgets |> List.except [w] |> fun rest -> (w :: rest |> List.rev) |> setWidgets

        Html.div [
            for w in widgets do
                Html.div [
                    prop.key (string w)
                    prop.children [
                        Widget.WidgetView
                            w
                            (fun e -> e.stopPropagation(); rmvWidget w)
                            (fun () -> bringToFront w)
                            activeTableData
                            activeDataMapData
                            onTableMutated
                    ]
                ]
        ]
