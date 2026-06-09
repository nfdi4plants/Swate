namespace Swate.Components.Primitive.ContextMenu

open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Swate.Components
open Swate.Components.Primitive
open Swate.Components.Primitive.ContextMenu.Types

module private ContextMenuDom =

    let tryGetEventTargetElement (e: Event) : Element option =
        let targetObj: obj = box e.target

        if isNullOrUndefined targetObj then
            None
        elif isNullOrUndefined targetObj?closest then
            let parentElement: obj = targetObj?parentElement

            if isNullOrUndefined parentElement then
                None
            else
                Some(unbox<Element> parentElement)
        else
            Some(unbox<Element> targetObj)

[<Erase>]
[<Mangle(false)>]
type ContextMenu =

    /// <summary></summary>
    /// <param name="childInfo"></param>
    /// <param name="ref">Element that owns the local context-menu listener.</param>
    /// <param name="onSpawn">A function that returns an option of any data. Data is stored in react
    /// state and can be accessed in context menu item onClick handlers. By default stores just the
    /// event target. Only if Some is returned context menu is opened.</param>
    /// <returns></returns>
    [<ReactComponent>]
    static member ContextMenu
        (
            childInfo: obj -> ContextMenuItem list,
            ref: IRefValue<HTMLElement option>,
            ?onSpawn: Browser.Types.MouseEvent -> obj option,
            ?debug: bool
        ) =

        let (spawnData: obj), setSpawnData = React.useState (null)
        let children, setChildren = React.useState ([])
        let onSpawn = onSpawn |> Option.defaultValue (fun e -> box e |> Some)
        let (isOpen, setIsOpen) = React.useState (false)
        let (activeIndex: int option), setActiveIndex = React.useState (None)

        let allowMouseUpCloseRef = React.useRef (false)
        let timeout = React.useRef (None)
        let listItemsRef: IRefValue<ResizeArray<HTMLElement>> = React.useRef (ResizeArray())

        let listContentRef = React.useRef (ResizeArray())

        let debug = defaultArg debug false

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
                placement = FloatingUI.Placement.RightStart,
                strategy = FloatingUI.FloatingStrategy.Fixed,
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

        let functionIsCalled = React.useRef (false)

        React.useEffect (
            (fun () ->

                let myClearTimeout () =
                    timeout.current
                    |> Option.iter (fun timeout -> Fable.Core.JS.clearTimeout timeout)

                let onMouseUp (e: Event) =
                    if allowMouseUpCloseRef.current then
                        setIsOpen false

                let cleanupListeners =
                    match ref.current with
                    | Some scopedElement ->
                        let onContextMenu (e: Event) =
                            let e = e :?> Browser.Types.MouseEvent

                            let isInsideContextMenuScope =
                                e
                                |> ContextMenuDom.tryGetEventTargetElement
                                |> Option.exists (fun target -> scopedElement.contains target)

                            if isInsideContextMenuScope then
                                match onSpawn e with
                                | Some data ->
                                    functionIsCalled.current <- false
                                    let children = childInfo data

                                    match children with
                                    | [] ->
                                        setSpawnData null
                                        setChildren []
                                        setIsOpen false
                                    | children ->
                                        e.preventDefault ()
                                        setSpawnData (data)

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
                                        timeout.current <-
                                            Some(JS.setTimeout (fun _ -> allowMouseUpCloseRef.current <- true) 300)

                                | None -> ()

                        scopedElement.addEventListener ("contextmenu", onContextMenu)
                        scopedElement.addEventListener ("mouseup", onMouseUp)

                        fun () ->
                            scopedElement.removeEventListener ("mouseup", onMouseUp)
                            scopedElement.removeEventListener ("contextmenu", onContextMenu)
                    | None -> ignore

                fun () ->

                    cleanupListeners ()

                    myClearTimeout ()
            ),
            [| box floating.refs; box childInfo |]
        )

        let close =
            fun () ->
                setIsOpen false
                allowMouseUpCloseRef.current <- true

                timeout.current
                |> Option.iter (fun timeout -> Fable.Core.JS.clearTimeout timeout)

        if isOpen then
            FloatingUI.FloatingPortal(
                FloatingUI.FloatingOverlay(
                    lockScroll = true,
                    className = "swt:z-9999",
                    children =
                        FloatingUI.FloatingFocusManager(
                            context = floating.context,
                            initialFocus = floating.refs.floating,
                            visuallyHiddenDismiss = true,
                            children =
                                Html.div [
                                    prop.ref floating.refs.setFloating
                                    if debug then
                                        prop.testId "context_menu"
                                    prop.custom ("style", floating.floatingStyles)
                                    for key, v in
                                        interactions.getFloatingProps (null)
                                        |> Fable.Core.JS.Constructors.Object.entries do
                                        prop.custom (key, v)
                                    prop.className
                                        "swt:grid swt:grid-cols-[auto_1fr_auto] swt:bg-base-100 swt:border-2 swt:border-base-300 swt:min-w-56 swt:rounded-md swt:focus:outline-hidden"
                                    prop.children [
                                        for index in 0 .. children.Length - 1 do
                                            let child = children.[index]

                                            let triggerEvent =
                                                fun (e: Browser.Types.MouseEvent) ->
                                                    if not functionIsCalled.current then
                                                        functionIsCalled.current <- true

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
                                                                prop.className
                                                                    "swt:col-start-1 swt:justify-self-start swt:self-center swt:flex swt:items-center"
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
            )
        else
            Html.none

    [<ReactComponent>]
    static member Example() =

        let containerRef = React.useElementRef ()

        Html.div [
            prop.className
                "swt:w-full swt:h-72 swt:border swt:border-dashed swt:border-primary swt:rounded-sm swt:flex swt:items-center swt:justify-center swt:flex-col swt:gap-4"
            prop.ref containerRef

            prop.children [
                Html.span [
                    prop.className "swt:select-none"
                    prop.text "Click here for context menu!"
                ]
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
                                ?icon = (if i = 4 then Icons.Check() |> Some else None),
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
                                        console.log (sprintf "Item clicked: %i" i, index)
                                    )
                            )
                    ]),
                    ref = containerRef,
                    onSpawn =
                        (fun e ->
                            let tableCell =
                                e
                                |> ContextMenuDom.tryGetEventTargetElement
                                |> Option.bind (fun target -> target.closest ("[data-row][data-column]"))

                            match tableCell with
                            | Some cell ->
                                let cell = cell :?> HTMLElement
                                let row = int cell?dataset?row
                                let col = int cell?dataset?column
                                let indices: CellCoordinate = {| y = row; x = col |}
                                Some indices
                            | _ -> None
                        )
                )
            ]
        ]
