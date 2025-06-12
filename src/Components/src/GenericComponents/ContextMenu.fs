namespace Swate.Components

open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types

[<Global; AllowNullLiteral>]
type ContextMenuItem
    [<ParamObjectAttribute; Emit("$0")>]
    (
        ?text: ReactElement,
        ?icon: ReactElement,
        ?kbdbutton:
            {|
                element: ReactElement
                label: string
            |},
        ?isDivider: bool,
        ?onClick:
            {|
                buttonEvent: Browser.Types.MouseEvent
                spawnData: obj
            |}
                -> unit
    ) =
    member val text = text with get, set
    member val icon = icon with get, set
    member val kbdbutton = kbdbutton with get, set
    member val isDivider: bool = defaultArg isDivider false with get, set
    member val onClick = onClick with get, set


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
            childInfo: obj -> ContextMenuItem list,
            ?ref: IRefValue<HTMLElement option>,
            ?onSpawn: Browser.Types.MouseEvent -> obj option
        ) =

        let (spawnData: obj), setSpawnData = React.useState (null)
        let children, setChildren = React.useState ([])
        let onSpawn = onSpawn |> Option.defaultValue (fun e -> box e |> Some)
        let (isOpen, setIsOpen) = React.useState (false)
        let (activeIndex: int option), setActiveIndex = React.useState (None)

        let allowMouseUpCloseRef = React.useRef (false)
        let timeout = React.useRef (None)
        /// This is used to store reference to element with listeners attached.
        /// Without this the listener cannot reliably be removed!
        let targetRef = React.useRef<HTMLElement option> (None)

        let listItemsRef: IRefValue<ResizeArray<HTMLElement>> = React.useRef (ResizeArray())

        let listContentRef = React.useRef (ResizeArray())

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
                        setSpawnData (data)
                        let children = childInfo data

                        if children.Length = 0 then
                            failwith "Context menu must have at least one item"

                        children |> setChildren

                        listContentRef.current.AddRange(
                            children
                            |> List.map (fun child -> child.kbdbutton |> Option.map (fun kbd -> kbd.label))
                        )

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
                    className = "swt:z-[9999]",
                    children =
                        FloatingUI.FloatingFocusManager(
                            context = floating.context,
                            initialFocus = floating.refs.floating,
                            visuallyHiddenDismiss = true,
                            children =
                                Html.div [
                                    prop.ref floating.refs.setFloating
                                    prop.custom ("style", floating.floatingStyles)
                                    for key, v in
                                        interactions.getFloatingProps () |> Fable.Core.JS.Constructors.Object.entries do
                                        prop.custom (key, v)
                                    prop.className
                                        "swt:grid swt:grid-cols-[auto_1fr_auto] swt:bg-base-100 swt:border-2 swt:border-base-300 swt:w-56 swt:rounded-md swt:focus:outline-hidden"
                                    prop.children [
                                        for index in 0 .. children.Length - 1 do
                                            let child = children.[index]

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
                                                        label = child.kbdbutton |> Option.map _.label
                                                    |}
                                                )
                                                |> Fable.Core.JS.Constructors.Object.entries

                                            if child.isDivider then
                                                Html.div [ prop.className "swt:divider swt:my-0 swt:col-span-3" ]
                                            else
                                                Html.button [
                                                    prop.key index
                                                    prop.className
                                                        "swt:col-span-3 swt:grid swt:grid-cols-subgrid swt:gap-x-2 swt:text-sm /
                                                        swt:text-base-content swt:px-2 swt:py-1 /
                                                        swt:w-full swt:text-left /
                                                        swt:hover:bg-base-100 /
                                                        swt:focus:bg-base-100 swt:focus:outline-hidden swt:focus:ring-2 swt:focus:ring-primary"
                                                    prop.children [
                                                        if child.icon.IsSome then
                                                            Html.div [
                                                                prop.className "swt:col-start-1 swt:justify-self-start"
                                                                prop.children child.icon.Value
                                                            ]
                                                        else
                                                            Html.none
                                                        if child.text.IsSome then
                                                            Html.div [
                                                                prop.className "swt:col-start-2 swt:justify-self-start"
                                                                prop.children child.text.Value
                                                            ]
                                                        if child.kbdbutton.IsSome then
                                                            Html.div [
                                                                prop.className "swt:col-start-3 swt:justify-self-end"
                                                                prop.children child.kbdbutton.Value.element
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
                "swt:w-full swt:h-72 swt:border swt:border-dashed swt:border-primary swt:rounded-sm swt:flex swt:items-center swt:justify-center swt:flex-col swt:gap-4"
            prop.ref containerRef

            prop.children [
                Html.span [ prop.className "swt:select-none"; prop.text "Click here for context menu!" ]
                Html.button [
                    prop.className "swt:btn swt:btn-primary"
                    prop.text "Example Table Cell"
                    prop.dataRow 12
                    prop.dataColumn 5
                ]
                ContextMenu.ContextMenu(
                    (fun (data: obj) -> [
                        for i in 0..5 do
                            ContextMenuItem(
                                text = Html.span $"Item {i}",
                                ?icon =
                                    (if i = 4 then
                                         Html.i [ prop.className "fa-solid fa-check" ] |> Some
                                     else
                                         None),
                                ?kbdbutton =
                                    (if i = 3 then
                                         {|
                                             element =
                                                 Html.kbd [
                                                     prop.className "swt:ml-auto swt:kbd swt:kbd-sm"
                                                     prop.text "Back"
                                                 ]
                                             label = "Back"
                                         |}
                                         |> Some
                                     else
                                         None),
                                onClick =
                                    (fun e ->
                                        e.buttonEvent.stopPropagation ()
                                        let index = e.spawnData |> unbox<CellCoordinate>
                                        console.log (sprintf "Item clicked: %i" i, index))
                            )
                    ]),
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