namespace Swate.Components

open Feliz
open Browser.Types
open WidgetsLocalStorage
open Swate
open Swate.Components
open Swate.Components.Widgets.Context

module InitExtensions =

    type Rect with

        static member initSizeFromPrefix(prefix: string) =
            match Size.load prefix with
            | Some p -> Some p
            | None -> None

        static member initPositionFromPrefix(prefix: string) =
            match Position.load prefix with
            | Some p -> Some p
            | None -> None

open InitExtensions

open Fable.Core
open Fable.Core.JsInterop

module MoveEventListener =

    open Fable.Core.JsInterop

    let ensurePositionInsideWindow (element: IRefValue<HTMLElement option>) (position: Rect) =
        let maxX = Browser.Dom.window.innerWidth - element.current.Value.offsetWidth
        let tempX = position.X
        let newX = System.Math.Min(System.Math.Max(tempX, 0), int maxX)
        let maxY = Browser.Dom.window.innerHeight - element.current.Value.offsetHeight
        let tempY = position.Y
        let newY = System.Math.Min(System.Math.Max(tempY, 0), int maxY)
        { X = newX; Y = newY }

    let calculatePosition (element: IRefValue<HTMLElement option>) (startPosition: Rect) =
        fun (e: Event) ->
            let e: MouseEvent = !!e
            let tempX = int e.clientX - startPosition.X
            let tempY = int e.clientY - startPosition.Y
            let tempPosition = { X = tempX; Y = tempY }
            ensurePositionInsideWindow element tempPosition

    let onmousemove (element: IRefValue<HTMLElement option>) (startPosition: Rect) setPosition =
        fun (e: Event) ->
            let nextPosition = calculatePosition element startPosition e
            setPosition (Some nextPosition)

    let onmouseup (prefix, element: IRefValue<HTMLElement option>) onmousemove =
        Browser.Dom.document.removeEventListener ("mousemove", onmousemove)

        if element.current.IsSome then
            let rect = element.current.Value.getBoundingClientRect ()
            let position = { X = int rect.left; Y = int rect.top }
            Position.write (prefix, position)

module ResizeEventListener =

    open Fable.Core.JsInterop

    let adaptElement (innerWidth: int) (innerHeight: int) (size: Rect) (position: Rect) setWidth setPosition =
        let combinedWidth = size.X + position.X
        let combinedHeight = size.Y + position.Y

        if innerWidth <= size.X then
            (Some { X = innerWidth; Y = size.Y }) |> setWidth

        let newXPosition =
            if innerWidth <= combinedWidth then
                System.Math.Max(0, innerWidth - size.X)
            else
                position.X

        let newYPosition =
            if innerHeight <= combinedHeight then
                System.Math.Max(0, innerHeight - size.Y)
            else
                position.Y

        setPosition (Some { X = newXPosition; Y = newYPosition })


    let onmousemove (startPosition: Rect) (startSize: Rect) setSize =
        fun (e: Event) ->
            let e: MouseEvent = !!e
            let width = int e.clientX - startPosition.X + startSize.X
            // I did not enable this, as it creates issues with overlays such as the term search dropdown.
            // The widget card itself has overflow: visible, which makes a set height impossible,
            // but wihout the visible overflow term search results might require scrolling.
            // // let height = int e.clientY - startPosition.Y + startSize.Y
            setSize (Some { X = width; Y = startSize.Y })

    let onmouseup (prefix, element: IRefValue<HTMLElement option>) onmousemove =
        Browser.Dom.document.removeEventListener ("mousemove", onmousemove)

        if element.current.IsSome then
            Size.write (
                prefix,
                {
                    X = int element.current.Value.offsetWidth
                    Y = int element.current.Value.offsetHeight
                }
            )

    let windowSizeChange setInnerWidth setInnerHeight =
        React.useEffect (fun () ->
            let onResize _ =
                setInnerWidth Browser.Dom.window.innerWidth
                setInnerHeight Browser.Dom.window.innerHeight

            Browser.Dom.window.addEventListener ("resize", onResize)
            // Cleanup function to remove event listener when the component unmounts
            FsReact.createDisposable (fun () -> Browser.Dom.window.removeEventListener ("resize", onResize))
        )

type WidgetBlock =
    {
        prefix: string
        content: ReactElement
    }

    static member CreateWidgetBlock prefix content : WidgetBlock =
        { prefix = prefix; content = content }

[<RequireQualifiedAccess>]
[<Erase; Mangle(false)>]
type Widget =


    [<ReactComponent>]
    static member Base
        (
            content: ReactElement,
            prefix: string,
            rmv: MouseEvent -> unit,
            key: string,
            ?zIndex: int,
            ?onFocus: unit -> unit
        ) =
        let zIndex = defaultArg zIndex 40
        let onFocus = defaultArg onFocus (fun () -> ())

        let position, setPosition =
            React.useState (fun _ -> Rect.initPositionFromPrefix prefix)

        let size, setSize = React.useState (fun _ -> Rect.initSizeFromPrefix prefix)

        let innerWidth, setInnerWidth =
            React.useState (fun _ -> Browser.Dom.window.innerWidth)

        let innerHeight, setInnerHeight =
            React.useState (fun _ -> Browser.Dom.window.innerHeight)

        let element = React.useElementRef ()

        ResizeEventListener.windowSizeChange setInnerWidth setInnerHeight

        let debouncedAdaptElement =
            React.useDebouncedCallback (
                fun () ->
                    match position, size with
                    | Some position, Some size ->
                        ResizeEventListener.adaptElement
                            (int innerWidth)
                            (int innerHeight)
                            size
                            position
                            setSize
                            setPosition
                    | _, _ -> ()
                , 100
            )

        React.useEffectOnce (fun _ -> debouncedAdaptElement ())

        React.useEffect (
            (fun () ->
                //Adapt position when the size of the element is changed so that it is visible
                debouncedAdaptElement ()
            //React shall only be used, when the size of the element is changed
            ),
            [| box innerWidth; box innerHeight |]
        )

        React.useLayoutEffectOnce (fun _ ->
            position
            |> Option.iter (fun position ->
                MoveEventListener.ensurePositionInsideWindow element position
                |> Some
                |> setPosition
            )
        ) // Reposition widget inside window

        let resizeElement (content: ReactElement) =
            Html.div [
                prop.ref element
                prop.onMouseDown (fun e -> // resize
                    onFocus ()
                    e.preventDefault ()
                    e.stopPropagation ()
                    let startPosition = { X = int e.clientX; Y = int e.clientY }

                    let startSize = {
                        X = int element.current.Value.offsetWidth
                        Y = int element.current.Value.offsetHeight
                    }

                    let onmousemove = ResizeEventListener.onmousemove startPosition startSize setSize
                    let onmouseup = fun e -> ResizeEventListener.onmouseup (prefix, element) onmousemove
                    Browser.Dom.document.addEventListener ("mousemove", onmousemove)
                    let config = createEmpty<AddEventListenerOptions>
                    config.once <- true
                    Browser.Dom.document.addEventListener ("mouseup", onmouseup, config)
                )
                prop.className
                    "swt:shadow-md swt:border swt:border-base-300 swt:space-y-4 swt:rounded-lg swt:border-r-2 swt:bg-base-100"
                prop.style [
                    style.zIndex zIndex
                    style.cursor.eastWestResize //style.cursor.northWestSouthEastResize;
                    style.display.flex
                    style.position.fixedRelativeToWindow
                    style.minWidth.minContent
                    if size.IsSome then
                        style.width size.Value.X
                    else
                        style.maxWidth 600
                    //style.height size.Value.Y
                    if position.IsNone then
                        //style.transform.translate (length.perc -50,length.perc -50)
                        style.top (length.perc 20)
                        style.left (length.perc 20)
                    else
                        style.top position.Value.Y
                        style.left position.Value.X
                ]
                prop.children content
            ]

        resizeElement
        <| Html.div [
            prop.onMouseDown (fun e ->
                onFocus ()
                e.stopPropagation ()
            )
            prop.className "swt:cursor-default swt:flex swt:flex-col swt:grow swt:max-h-[60%] swt:overflow-visible"
            prop.children [
                Html.div [
                    prop.onMouseDown (fun e -> // move
                        onFocus ()
                        e.preventDefault ()
                        e.stopPropagation ()
                        let x = e.clientX - element.current.Value.offsetLeft
                        let y = e.clientY - element.current.Value.offsetTop
                        let startPosition = { X = int x; Y = int y }
                        let onmousemove = MoveEventListener.onmousemove element startPosition setPosition
                        let onmouseup = fun _ -> MoveEventListener.onmouseup (prefix, element) onmousemove
                        Browser.Dom.document.addEventListener ("mousemove", onmousemove)
                        let config = createEmpty<AddEventListenerOptions>
                        config.once <- true
                        Browser.Dom.document.addEventListener ("mouseup", onmouseup, config)
                    )
                    prop.className
                        "swt:cursor-move swt:flex swt:justify-end swt:bg-linear-to-br swt:from-primary swt:to-base-200 swt:rounded-lg"
                    prop.children [
                        Components.Components.DeleteButton(
                            className = "swt:btn-ghost swt:bg-primary/30",
                            props = [
                                prop.onMouseDown (fun e -> e.stopPropagation ())
                                prop.onClick (fun e ->
                                    e.stopPropagation ()
                                    rmv e
                                )
                            ]
                        )
                    ]
                ]
                Html.div [
                    prop.className "swt:p-2 swt:max-h-[80vh] swt:overflow-visible swt:flex swt:flex-col"
                    prop.children [ content ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member WidgetController
        (
            widgets: Map<WidgetType, WidgetDefinition>,
            ?children: ReactElement list,
            ?closeAllWhen: bool
        ) =

        let activeWidgets, setActiveWidgets =
            React.useStateWithUpdater<WidgetType list> ([])

        let children = defaultArg children []
        let closeAllWhen = defaultArg closeAllWhen false

        React.useEffect (
            (fun () ->
                if closeAllWhen then
                    setActiveWidgets (fun _ -> [])
            ),
            [| box closeAllWhen |]
        )

        let closeWidget (widgetType: WidgetType) =
            setActiveWidgets (fun widgets -> widgets |> List.filter (fun widget -> widget <> widgetType))

        let focusWidget (widgetType: WidgetType) =
            setActiveWidgets (fun widgets ->
                if widgets |> List.contains widgetType then
                    widgets
                    |> List.filter (fun widget -> widget <> widgetType)
                    |> fun nextWidgets -> nextWidgets @ [ widgetType ]
                else
                    widgets
            )

        let openWidget (widgetType: WidgetType) =
            setActiveWidgets (fun widgets ->
                if widgets |> List.contains widgetType then
                    widgets
                    |> List.filter (fun widget -> widget <> widgetType)
                    |> fun nextWidgets -> nextWidgets @ [ widgetType ]
                else
                    widgets @ [ widgetType ]
            )

        let toggleWidget (widgetType: WidgetType) =
            setActiveWidgets (fun widgets ->
                if widgets |> List.contains widgetType then
                    widgets |> List.filter (fun widget -> widget <> widgetType)
                else
                    widgets @ [ widgetType ]
            )

        let widgetContext: WidgetControllerContext = {
            activeWidgets = activeWidgets
            isActive = fun widgetType -> activeWidgets |> List.contains widgetType
            openWidget = openWidget
            closeWidget = closeWidget
            toggleWidget = toggleWidget
            focusWidget = focusWidget
        }

        ActiveWidgetContext.Provider(
            widgetContext,
            [
                yield! children

                for index, widgetType in activeWidgets |> List.indexed do
                    match widgets.TryFind widgetType with
                    | Some widget ->
                        Widget.Base(
                            content = widget.content,
                            prefix = widget.prefix,
                            rmv = (fun _ -> closeWidget widgetType),
                            key = widgetType.ToString(),
                            zIndex = 40 + index,
                            onFocus = (fun () -> focusWidget widgetType)
                        )
                    | None -> Html.none
            ]
        )

    /// This component is only used for testing and development via playground
    [<ReactComponent>]
    static member private EntryControls(widgetTypes: WidgetType list) =
        let context = useWidgetController ()

        let controlButton (widgetType: WidgetType) =
            let isActive = context.isActive widgetType

            Html.button [
                prop.className [
                    "swt:btn swt:btn-sm"
                    if isActive then "swt:btn-primary" else "swt:btn-outline"
                ]
                prop.textf "%s %s" (if isActive then "Close" else "Open") (widgetType.ToString())
                prop.onClick (fun _ -> context.toggleWidget widgetType)
            ]

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-3 swt:items-center swt:justify-center"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:justify-center"
                    prop.children [
                        for widgetType in widgetTypes do
                            controlButton widgetType
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:justify-center"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-sm swt:btn-secondary"
                            prop.text "Open All"
                            prop.onClick (fun _ -> widgetTypes |> List.iter context.openWidget)
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Close All"
                            prop.onClick (fun _ -> widgetTypes |> List.iter context.closeWidget)
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:text-xs swt:opacity-70"
                    prop.textf
                        "Active order: %s"
                        (if context.activeWidgets.IsEmpty then
                             "none"
                         else
                             context.activeWidgets |> List.map _.ToString() |> String.concat " > ")
                ]
            ]
        ]

    /// This component is only used for testing and development via playground
    [<ReactComponent>]
    static member Entry() =
        let term, setTerm = React.useState None
        let clickCounter, setClickCounter = React.useState 0
        let fileName, setFileName = React.useState "example.tsv"
        let annotateEnabled, setAnnotateEnabled = React.useState false
        let note, setNote = React.useState "Playground note"

        let widgetTypes = [
            WidgetType.BuildingBlock
            WidgetType.Template
            WidgetType.FilePicker
            WidgetType.DataAnnotator
            WidgetType.Playground
        ]

        let widgets: Map<WidgetType, WidgetDefinition> =
            [
                WidgetType.BuildingBlock,
                {|
                    prefix = "TEST_ENTRY_BUILDINGBLOCK"
                    content =
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-64"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:font-bold"
                                    prop.text "BuildingBlock POC"
                                ]
                                Html.span [ prop.textf "Counter: %i" clickCounter ]
                                Html.button [
                                    prop.className "swt:btn swt:btn-sm swt:btn-primary swt:w-max"
                                    prop.text "Increment"
                                    prop.onClick (fun _ -> setClickCounter (clickCounter + 1))
                                ]
                            ]
                        ]
                |}

                WidgetType.Template,
                {|
                    prefix = "TEST_ENTRY_TEMPLATE"
                    content =
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-80"
                            prop.children [
                                Html.h3 [ prop.className "swt:font-bold"; prop.text "Template POC" ]
                                TermSearch.TermSearch.TermSearch(term, setTerm)
                                Html.span [
                                    prop.className "swt:text-xs swt:opacity-70"
                                    prop.textf
                                        "Selected term: %s"
                                        (term |> Option.map (fun t -> t.ToString()) |> Option.defaultValue "None")
                                ]
                            ]
                        ]
                |}

                WidgetType.FilePicker,
                {|
                    prefix = "TEST_ENTRY_FILEPICKER"
                    content =
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-72"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:font-bold"
                                    prop.text "FilePicker POC"
                                ]
                                Html.input [
                                    prop.className "swt:input swt:input-sm swt:input-bordered"
                                    prop.value fileName
                                    prop.onChange setFileName
                                ]
                                Html.span [
                                    prop.className "swt:text-xs swt:opacity-70"
                                    prop.textf "Current: %s" fileName
                                ]
                            ]
                        ]
                |}

                WidgetType.DataAnnotator,
                {|
                    prefix = "TEST_ENTRY_DATAANNOTATOR"
                    content =
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-64"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:font-bold"
                                    prop.text "DataAnnotator POC"
                                ]
                                Html.label [
                                    prop.className "swt:label swt:cursor-pointer swt:justify-start swt:gap-2"
                                    prop.children [
                                        Html.input [
                                            prop.type'.checkbox
                                            prop.className "swt:checkbox swt:checkbox-sm"
                                            prop.isChecked annotateEnabled
                                            prop.onChange (fun (isChecked: bool) -> setAnnotateEnabled isChecked)
                                        ]
                                        Html.span [ prop.text "Enable annotation" ]
                                    ]
                                ]
                                Html.span [
                                    prop.className "swt:text-xs swt:opacity-70"
                                    prop.text (
                                        if annotateEnabled then
                                            "Status: enabled"
                                        else
                                            "Status: disabled"
                                    )
                                ]
                            ]
                        ]
                |}

                WidgetType.Playground,
                {|
                    prefix = "TEST_ENTRY_PLAYGROUND"
                    content =
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-80"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:font-bold"
                                    prop.text "Playground POC"
                                ]
                                Html.textarea [
                                    prop.className "swt:textarea swt:textarea-bordered swt:textarea-sm"
                                    prop.rows 3
                                    prop.value note
                                    prop.onChange setNote
                                ]
                                Html.span [
                                    prop.className "swt:text-xs swt:opacity-70"
                                    prop.textf "Length: %i" note.Length
                                ]
                            ]
                        ]
                |}
            ]
            |> Map.ofList

        Widget.WidgetController(widgets, children = [ Widget.EntryControls(widgetTypes) ])
