namespace Swate.Components.Composite.GraphCanvas

open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.JsBindings
open Swate.Components.Composite.GraphCanvas.Types
open Swate.Components.Composite.GraphCanvas.Pills
open Swate.Components.Composite.GraphCanvas.Layout

module private GraphCanvasTypes =

    /// The epoch counts the views the canvas has shown. It rises every time resetKey changes.
    type Navigation = { ViewKey: string; Epoch: int }

    /// The edges that are expanded, and the epoch they were expanded in.
    type Expansion = { ExpandedIn: int; Ids: string[] }

    /// The result of one layout and the epoch it was requested in. No drawn layout means the layout failed.
    type Outcome = {
        RequestedIn: int
        Drawn: GraphLayout option
    }

    /// The open pill list. It belongs to one layout and one box width.
    type OpenMenu = {
        MenuEdgeId: string
        MenuLayout: GraphLayout
        MenuBoxWidth: float
        Opener: Element
    }

module private GraphCanvasHelper =

    /// One shared empty array, so that "nothing expanded" keeps its identity between renders.
    let noExpansion: string[] = [||]

    let nodeRadius = 6.0
    let stripeWidth = 4.0
    // Where the 4px stripe meets the rounded corner of the node.
    let stripeCorner =
        nodeRadius - sqrt (nodeRadius ** 2.0 - (nodeRadius - stripeWidth) ** 2.0)

    let minZoom = 0.25
    let maxZoom = 2.0
    let zoomStep = 0.25

    let mutedText = "color-mix(in oklch, var(--color-base-content) 65%, transparent)"

    // Tailwind reads class names from the generated code, so these are complete literals. The colors come
    // from custom properties on the root of the component, which are set from accentColor and neutralBorder.
    let interactiveClass =
        "swt:cursor-pointer swt:focus-visible:outline-3 swt:focus-visible:outline-(color:--graph-accent) swt:focus-visible:outline-offset-2"

    /// The custom properties of the root: the palette and the colors of the chrome.
    let rootColors =
        palette
        @ [
            "--graph-accent", accentColor
            "--graph-border", neutralBorder
        ]

    let isActivationKey (key: string) = key = "Enter" || key = " "

    /// Left edge of a node: the rounded corners of the node cut a 4px wide stripe.
    let stripePath (height: float) : string =
        let bottom = height - stripeCorner

        $"M {stripeWidth} {stripeCorner} A {nodeRadius} {nodeRadius} 0 0 0 0 {nodeRadius} V {height - nodeRadius} A {nodeRadius} {nodeRadius} 0 0 0 {stripeWidth} {bottom} Z"

    let nodeBorderColor (color: CanvasColor) : string =
        match color with
        | CanvasColor.Neutral -> neutralBorder
        | other -> colorVar other

    let edgeColor (laid: LaidOutEdge) : string =
        if laid.Edge.Highlighted then
            accentColor
        else
            colorVar laid.Edge.Color

    /// The color a pill draws its border, dot and the dot of its list entry in.
    let pillColor (view: PillView) (highlighted: bool) : string =
        if view.Kind = PillViewKind.Process then
            if highlighted then
                accentColor
            else
                "var(--color-base-content)"
        else
            match view.Category with
            | Some category -> categoryVar category
            | None -> neutralBorder

    let listEntryColor (pill: CanvasPill) : string =
        if pill.Kind = PillKind.Process then
            "var(--color-base-content)"
        else
            match pill.Category with
            | Some category -> categoryVar category
            | None -> neutralBorder

    /// Marker names of the arrow heads, one per edge color and one for highlighted edges.
    let arrowNames = [| "study"; "assay"; "run"; "neutral"; "accent" |]

    let arrowName (laid: LaidOutEdge) : string =
        if laid.Edge.Highlighted then
            "accent"
        else
            match laid.Edge.Color with
            | CanvasColor.Study -> "study"
            | CanvasColor.Assay -> "assay"
            | CanvasColor.Run -> "run"
            | CanvasColor.Neutral -> "neutral"

    let arrowColor (name: string) : string =
        match name with
        | "study" -> colorVar CanvasColor.Study
        | "assay" -> colorVar CanvasColor.Assay
        | "run" -> colorVar CanvasColor.Run
        | "accent" -> accentColor
        | _ -> colorVar CanvasColor.Neutral

    /// Inline style of an HTML element.
    let css (pairs: (string * string) list) =
        prop.style [ for name, value in pairs -> style.custom (name, value) ]

    // SVG elements take their own attribute type, so role, label, test id and style have SVG versions.
    let svgStyle (pairs: (string * string) list) =
        svg.custom ("style", createObj [ for name, value in pairs -> name ==> value ])

    let svgRole (role: string) = svg.custom ("role", role)
    let svgLabel (label: string) = svg.custom ("aria-label", label)
    let svgTestId (id: string) = svg.custom ("data-testid", id)

    let pxOf (value: float) : string = $"{value}px"

open GraphCanvasTypes
open GraphCanvasHelper

/// Draws nodes, edges and pills of a graph as SVG. The layout comes from ELK and is computed
/// asynchronously. An expanded edge wraps its pills into rows when the box is wide enough, otherwise a
/// list of the hidden pills opens next to its toggle. A portrait box gets the compact top to bottom layout.
[<Erase; Mangle(false)>]
type GraphCanvas =

    [<ReactComponent>]
    static member private Pill
        (
            placed: PlacedPill,
            ?highlighted: bool,
            ?owner: string,
            ?listOpen: bool,
            ?edgeId: string,
            ?onToggle: Element -> unit
        ) =
        let view = placed.View
        let highlighted = defaultArg highlighted false
        let color = pillColor view highlighted
        let hasDot = view.Category.IsSome

        let valueStyle =
            if view.Kind = PillViewKind.Process then
                [ "fill", "var(--color-base-content)"; "fontWeight", "600" ]
            elif view.Empty then
                [ "fill", mutedText; "fontStyle", "italic" ]
            else
                [ "fill", "var(--color-base-content)" ]

        let content = [
            Svg.title view.Tooltip
            Svg.rect [
                svg.width view.Width
                svg.height pillHeight
                svg.rx (pillHeight / 2.0)
                svgStyle [
                    "fill", "var(--color-base-100)"
                    "stroke", color
                    "strokeWidth", "1"
                ]
            ]
            if hasDot then
                Svg.circle [
                    svg.cx 10.0
                    svg.cy (pillHeight / 2.0)
                    svg.r 3.0
                    svgStyle [ "fill", color ]
                ]
            Svg.text [
                svg.x (if hasDot then 17.0 else 7.0)
                svg.y (pillHeight / 2.0)
                svg.custom ("dominantBaseline", "central")
                svg.custom ("fontSize", 11)
                svg.children [
                    if view.NamePart <> "" then
                        Svg.tspan [ svgStyle [ "fill", mutedText ]; svg.text view.NamePart ]
                    Svg.tspan [ svgStyle valueStyle; svg.text view.ValuePart ]
                ]
            ]
        ]

        let transform = $"translate({placed.X} {placed.Y})"

        match view.Toggle, onToggle with
        | Some toggle, Some onToggle ->
            let activate (event: Event) =
                event.stopPropagation ()
                onToggle (event.currentTarget :?> Element)

            Svg.g [
                svg.custom ("transform", transform)
                svg.className interactiveClass
                svgRole "button"
                svg.tabIndex 0
                svgLabel (
                    match owner with
                    | Some owner -> $"{view.Tooltip} of {owner}"
                    | None -> view.Tooltip
                )
                if listOpen.IsSome then
                    svg.custom ("aria-haspopup", "dialog")
                svg.custom ("aria-expanded", defaultArg listOpen (toggle = ToggleKind.Less))
                match edgeId with
                | Some id -> svgTestId $"graph-edge-toggle-{id}"
                | None -> ()
                svg.onClick (fun (event: MouseEvent) -> activate event)
                svg.onKeyDown (fun (event: KeyboardEvent) ->
                    if isActivationKey event.key then
                        event.preventDefault ()
                        activate event
                )
                svg.children content
            ]
        | _ ->
            Svg.g [
                svg.custom ("transform", transform)
                svgRole "img"
                svgLabel view.Tooltip
                svg.children content
            ]

    [<ReactComponent>]
    static member private NodeShape(laid: LaidOutNode, ?onClick: string -> unit) =
        let node = laid.Node
        let handler = if node.Clickable then onClick else None
        // The pills hold information that a screen reader would otherwise miss. The tooltip is left out,
        // because ariaLabel already carries what it says.
        let label =
            node.AriaLabel :: [ for placed in laid.Pills -> placed.View.FullText ]
            |> String.concat ", "

        let borderStyle =
            if node.Selected then
                [ "stroke", accentColor; "strokeWidth", "3" ]
            elif node.Emphasized then
                [
                    "stroke", "var(--color-base-content)"
                    "strokeWidth", "2"
                ]
            else
                [ "stroke", nodeBorderColor node.Color; "strokeWidth", "1" ]

        Svg.g [
            svg.custom ("transform", $"translate({laid.X} {laid.Y})")
            svgLabel label
            svgTestId $"graph-node-{node.Id}"
            match handler with
            | Some onClick ->
                svg.className interactiveClass

                svgRole "button"
                svg.tabIndex 0
                svg.onClick (fun (_: MouseEvent) -> onClick node.Id)

                svg.onKeyDown (fun (event: KeyboardEvent) ->
                    if isActivationKey event.key then
                        event.preventDefault ()
                        onClick node.Id
                )
            | None -> svgRole "group"
            svg.children [
                Svg.title (defaultArg node.Tooltip node.Title)
                Svg.rect [
                    svg.width laid.Width
                    svg.height laid.Height
                    svg.rx nodeRadius
                    svgStyle [ "fill", "var(--color-base-200)" ]
                ]
                Svg.path [
                    svg.d (stripePath laid.Height)
                    svgStyle [ "fill", nodeBorderColor node.Color ]
                ]
                Svg.rect [
                    svg.width laid.Width
                    svg.height laid.Height
                    svg.rx nodeRadius
                    svg.fill "none"
                    svgStyle borderStyle
                    if node.Dashed then
                        svg.custom ("strokeDasharray", "6 4")
                ]
                Svg.text [
                    svg.x 15.0
                    svg.y laid.TitleY
                    svg.custom ("dominantBaseline", "central")
                    svg.custom ("fontSize", 13)
                    svg.custom ("fontWeight", 600)
                    if node.Mono then
                        svg.custom ("fontFamily", Dom.monoFamily)
                    svgStyle [ "fill", "var(--color-base-content)" ]
                    svg.text laid.TitleText
                ]
                if laid.SubtitleText <> "" then
                    Svg.text [
                        svg.x 15.0
                        svg.y laid.SubtitleY
                        svg.custom ("dominantBaseline", "central")
                        svg.custom ("fontSize", 12)
                        svgStyle [ "fill", mutedText ]
                        svg.text laid.SubtitleText
                    ]
                for placed in laid.Pills do
                    React.KeyedFragment(placed.View.Key, [ GraphCanvas.Pill(placed) ])
            ]
        ]

    /// The list of pills that did not fit on an edge. It sits in the scrolling content, so it moves with its toggle.
    [<ReactComponent>]
    static member private PillMenu
        (
            label: string,
            pills: CanvasPill[],
            boxHeight: float,
            rootRef: IRefValue<HTMLElement option>,
            opener: Element,
            onClose: unit -> unit
        ) =
        let dialogRef = React.useRef<HTMLElement option> None
        let listRef = React.useRef<HTMLElement option> None

        // The list takes the visible area of the box as its limit: it opens on the side of the toggle with
        // more room and scrolls inside itself when it needs more. The width follows the text up to the
        // visible width. boxHeight is a dependency only, so the list fits again when the box changes its height.
        React.useLayoutEffect (
            (fun () ->
                match dialogRef.current, rootRef.current with
                | Some dialog, Some root when not (isNull dialog.parentElement) ->
                    let canvas = dialog.parentElement
                    let rootRect = root.getBoundingClientRect ()
                    let anchor = opener.getBoundingClientRect ()
                    let origin = canvas.getBoundingClientRect ()

                    let rectOf (rect: ClientRect) : Rect = {
                        Left = rect.left
                        Top = rect.top
                        Right = rect.right
                        Bottom = rect.bottom
                    }

                    let geometry: PillMenuGeometry = {
                        Root = rectOf rootRect
                        RootClientLeft = root.clientLeft
                        RootClientTop = root.clientTop
                        RootClientWidth = root.clientWidth
                        RootClientHeight = root.clientHeight
                        Anchor = rectOf anchor
                        Origin = rectOf origin
                    }

                    let placement =
                        pillMenuPlacement
                            geometry
                            (fun maxWidth maxHeight ->
                                Dom.setStyleProperty dialog "max-width" (pxOf maxWidth)
                                Dom.setStyleProperty dialog "max-height" (pxOf maxHeight)
                                dialog.offsetWidth
                            )

                    Dom.setStyleProperty
                        dialog
                        "top"
                        (match placement.MenuTop with
                         | Some top -> pxOf top
                         | None -> "auto")

                    Dom.setStyleProperty
                        dialog
                        "bottom"
                        (match placement.MenuBottom with
                         | Some bottom -> pxOf bottom
                         | None -> "auto")

                    Dom.setStyleProperty dialog "left" (pxOf placement.MenuLeft)
                | _ -> ()
            ),
            [| box rootRef; box opener; box boxHeight |]
        )

        ReactHooks.useLayoutEffectWithCleanup (
            (fun () ->
                let dialog = dialogRef.current
                listRef.current |> Option.iter (fun list -> list.focus ())

                let insideDialog (target: Node) =
                    match dialog with
                    | Some dialog -> dialog.contains target
                    | None -> false

                // The list closes when the focus leaves it, and a tap on a blank spot moves no focus on some touch browsers.
                let close (event: Event) =
                    let target = event.target :?> Node

                    if not (insideDialog target) && not (opener.contains target) then
                        onClose ()

                let escape (event: Event) =
                    if (event :?> KeyboardEvent).key = "Escape" then
                        onClose ()

                Browser.Dom.document.addEventListener ("pointerdown", close, true)
                Browser.Dom.document.addEventListener ("keydown", escape)

                box (fun () ->
                    Browser.Dom.document.removeEventListener ("pointerdown", close, true)
                    Browser.Dom.document.removeEventListener ("keydown", escape)

                    if
                        not (isNull Browser.Dom.document.activeElement)
                        && insideDialog Browser.Dom.document.activeElement
                        && Dom.isConnected opener
                    then
                        (opener :?> HTMLElement).focus ()
                )
            ),
            [| box opener; box onClose |]
        )

        Html.div [
            prop.ref dialogRef
            prop.testId "graph-pill-menu"
            prop.className
                "swt:absolute swt:z-10 swt:w-max swt:overflow-auto swt:rounded-md swt:border swt:border-(color:--graph-border) swt:bg-base-100 swt:py-1 swt:text-xs swt:leading-snug swt:shadow-lg swt:focus:outline-none"
            prop.role.dialog
            prop.ariaLabel label
            prop.tabIndex -1
            prop.onKeyDown (fun (event: KeyboardEvent) ->
                // Tab would leave the canvas or land on its last node, so the list closes and the toggle takes the focus.
                if event.key = "Tab" then
                    event.preventDefault ()
                    onClose ()
            )
            prop.onBlur (fun (event: FocusEvent) ->
                let next = event.relatedTarget

                let focusStaysIn =
                    not (isNull next)
                    && ((match dialogRef.current with
                         | Some dialog -> dialog.contains (next :?> Node)
                         | None -> false)
                        || opener.contains (next :?> Node))

                if not focusStaysIn then
                    onClose ()
            )
            prop.children [
                Html.ul [
                    prop.ref listRef
                    prop.className
                        "swt:m-0 swt:list-none swt:p-0 swt:focus-visible:outline-2 swt:focus-visible:-outline-offset-2 swt:focus-visible:outline-(color:--graph-accent)"
                    prop.tabIndex 0
                    prop.ariaLabel label
                    prop.children [
                        for pill in pills ->
                            Html.li [
                                prop.key pill.Key
                                prop.className
                                    "swt:flex swt:items-baseline swt:gap-2 swt:px-2.5 swt:py-[3px] swt:wrap-anywhere"
                                if pill.Kind = PillKind.Property then
                                    prop.title (describePill pill).Tooltip
                                prop.children [
                                    Html.span [
                                        prop.className "swt:size-2 swt:flex-none swt:rounded-full"
                                        prop.ariaHidden true
                                        css [ "backgroundColor", listEntryColor pill ]
                                    ]
                                    Html.span (pillListText pill)
                                ]
                            ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member GraphCanvas
        (
            nodes: CanvasNode[],
            edges: CanvasEdge[],
            ariaLabel: string,
            resetKey: string,
            ?onNodeClick: string -> unit,
            ?centerNodeId: string,
            ?focusOnMount: bool,
            ?layout: CanvasNode[] -> CanvasEdge[] -> string[] -> MeasureText -> LayoutOptions -> JS.Promise<GraphLayout>
        ) =
        let rootRef = React.useRef<HTMLElement option> None
        let zoom, setZoom = React.useState 1.0
        let scrolledIn = React.useRef<int option> None
        let markerPrefix = (React.useId ()).Replace(":", "")

        // The layout function is read through a ref, so a new function identity per render never starts a layout.
        let layoutRef = React.useRef (defaultArg layout layoutGraph)

        React.useLayoutEffect (fun () -> layoutRef.current <- defaultArg layout layoutGraph)

        // A changed resetKey is a new view. React lets a component reset state while it renders, so the new
        // epoch is known before anything is drawn. Results, expansion and the scroll position are tied to an
        // epoch, so a result that arrives late for an earlier view is never drawn, and returning to an earlier
        // resetKey starts like a new view.
        let navigation, setNavigation = React.useState { ViewKey = resetKey; Epoch = 0 }

        let epoch =
            if navigation.ViewKey = resetKey then
                navigation.Epoch
            else
                navigation.Epoch + 1

        if navigation.ViewKey <> resetKey then
            setNavigation { ViewKey = resetKey; Epoch = epoch }

        let expansion, setExpansion =
            React.useStateWithUpdater { ExpandedIn = 0; Ids = noExpansion }

        let expanded =
            if expansion.ExpandedIn = epoch then
                expansion.Ids
            else
                noExpansion

        let toggle =
            React.useCallback (
                (fun (edgeId: string) ->
                    setExpansion (fun current ->
                        let ids =
                            if current.ExpandedIn = epoch then
                                current.Ids
                            else
                                noExpansion

                        let next =
                            if Array.contains edgeId ids then
                                ids |> Array.filter (fun id -> id <> edgeId)
                            else
                                Array.append ids [| edgeId |]

                        { ExpandedIn = epoch; Ids = next }
                    )
                ),
                [| box epoch |]
            )

        // Size of the box the canvas is shown in, None until it is measured. The shape comes from the border
        // box, which a scrollbar does not change. A scrollbar takes 15px off the client size, which near a
        // square box would flip the layout mode back and forth. The client size serves for the widths and heights.
        let measuredBox, setMeasuredBox = React.useStateWithUpdater<MeasuredBox option> None

        ReactHooks.useLayoutEffectWithCleanup (
            (fun () ->
                match rootRef.current with
                | None -> box (fun () -> ())
                | Some root ->
                    let measureBox () =
                        setMeasuredBox (fun current ->
                            let next: MeasuredBox = {
                                ClientWidth = root.clientWidth
                                ClientHeight = root.clientHeight
                                Portrait = isPortrait root.clientWidth root.offsetHeight root.offsetWidth
                            }

                            match current with
                            | Some current when sameBox current next -> Some current
                            | _ -> Some next
                        )

                    measureBox ()

                    if Dom.ResizeObserver.isAvailable then
                        let observer = Dom.ResizeObserver(fun _ -> measureBox ())
                        observer.observe root
                        box (fun () -> observer.disconnect ())
                    else
                        box (fun () -> ())
            ),
            [||]
        )

        let measured = measuredBox.IsSome

        let boxWidth =
            match measuredBox with
            | Some size -> size.ClientWidth
            | None -> 0.0

        let boxHeight =
            match measuredBox with
            | Some size -> size.ClientHeight
            | None -> 0.0

        let portrait =
            match measuredBox with
            | Some size -> size.Portrait
            | None -> false

        // The width shapes the layout only through the rows of an expanded edge. A portrait box takes the
        // compact layout, which has no expanded edges and does not depend on the width.
        let layoutWidth = effectiveLayoutWidth portrait expanded.Length boxWidth

        // The previous drawing stays until the next layout resolves, unless it belongs to another view.
        let outcome, setOutcome = React.useState<Outcome option> None

        React.useEffect (
            (fun () ->
                if not measured then
                    FsReact.createDisposable (fun () -> ())
                else
                    let current = ref true

                    let family =
                        match rootRef.current with
                        | Some root -> Dom.computedFontFamily root
                        | None -> ""

                    let measure = Dom.createTextMeasure (if family = "" then "sans-serif" else family)

                    let run =
                        try
                            layoutRef.current nodes edges expanded measure {
                                Width = layoutWidth
                                Portrait = portrait
                            }
                        with error ->
                            Promise.reject error

                    run.``then`` (
                        (fun drawn ->
                            if current.Value then
                                setOutcome (
                                    Some {
                                        RequestedIn = epoch
                                        Drawn = Some drawn
                                    }
                                )
                        ),
                        (fun error ->
                            Browser.Dom.console.error ("Could not lay out the graph canvas", error)

                            if current.Value then
                                setOutcome (Some { RequestedIn = epoch; Drawn = None })
                        )
                    )
                    |> ignore

                    FsReact.createDisposable (fun () -> current.Value <- false)
            ),
            [|
                box nodes
                box edges
                box expanded
                box epoch
                box measured
                box layoutWidth
                box portrait
            |]
        )

        let shown = outcome |> Option.filter (fun result -> result.RequestedIn = epoch)
        let drawn = shown |> Option.bind (fun result -> result.Drawn)
        let failed = shown.IsSome && drawn.IsNone

        let titles =
            match drawn with
            | Some drawn ->
                drawn.LaidOutNodes
                |> Array.fold (fun titles laid -> titles |> Map.add laid.Node.Id laid.Node.Title) Map.empty
            | None -> Map.empty

        let titleOf (id: string) =
            titles |> Map.tryFind id |> Option.defaultValue id

        let ownerOf (laid: LaidOutEdge) =
            $"{titleOf laid.Edge.From} to {titleOf laid.Edge.To}"

        // One list at a time. It belongs to the layout and the box width it was opened on, so a new layout
        // (an expansion, other data, a resize that relays out) or a new width closes it. The state is cleared
        // then, so the list cannot reappear when the box returns to the old width.
        let menu, setMenu = React.useState<OpenMenu option> None

        let openMenu =
            match menu, drawn with
            | Some open', Some drawn when
                LanguagePrimitives.PhysicalEquality open'.MenuLayout drawn
                && open'.MenuBoxWidth = boxWidth
                ->
                menu
            | _ -> None

        if menu.IsSome && openMenu.IsNone then
            setMenu None

        let closeMenu = React.useCallback ((fun () -> setMenu None), [||])

        let menuEdge =
            match openMenu, drawn with
            | Some open', Some drawn ->
                drawn.LaidOutEdges
                |> Array.tryFind (fun laid -> laid.Edge.Id = open'.MenuEdgeId)
            | _ -> None

        // After a navigation the container takes focus, because the focused node is gone. The first
        // mount leaves focus alone unless focusOnMount is set.
        let lastEpoch = React.useRef epoch
        let focusOnDraw = React.useRef (defaultArg focusOnMount false)

        React.useLayoutEffect (
            (fun () ->
                if lastEpoch.current <> epoch then
                    lastEpoch.current <- epoch
                    focusOnDraw.current <- true
            ),
            [| box epoch |]
        )

        // A new view starts at the top left, or on centerNodeId. Expanding an edge keeps the scroll position.
        // A failed layout takes the focus too, because the container then shows the alert.
        React.useLayoutEffect (
            (fun () ->
                match rootRef.current with
                | Some root when (drawn.IsSome || failed) && scrolledIn.current <> Some epoch ->
                    if focusOnDraw.current then
                        focusOnDraw.current <- false
                        Dom.focusWithoutScroll root

                    match drawn with
                    | None -> ()
                    | Some drawn ->
                        scrolledIn.current <- Some epoch
                        root.scrollLeft <- 0.0
                        root.scrollTop <- 0.0

                        let target =
                            centerNodeId
                            |> Option.bind (fun id ->
                                drawn.LaidOutNodes |> Array.tryFind (fun laid -> laid.Node.Id = id)
                            )

                        match target with
                        | None -> ()
                        | Some target ->
                            let clamp (value: float) (limit: float) = max 0.0 (min value limit)

                            // The compact layout keeps its main flow on the left, so it only scrolls down to the node.
                            if not drawn.Compact then
                                root.scrollLeft <-
                                    clamp
                                        ((target.X + target.Width / 2.0) * zoom - root.clientWidth / 2.0)
                                        (root.scrollWidth - root.clientWidth)

                            root.scrollTop <-
                                clamp
                                    ((target.Y + target.Height / 2.0) * zoom - root.clientHeight / 2.0)
                                    (root.scrollHeight - root.clientHeight)
                | _ -> ()
            ),
            [|
                box drawn
                box failed
                box epoch
                box centerNodeId
                box zoom
            |]
        )

        let edgeGroup (drawn: GraphLayout) (laid: LaidOutEdge) : ReactElement =
            let owner = ownerOf laid
            // The list takes over the "+N" toggle when the expanded edge would not fit the box, and in compact mode.
            let listed = drawn.Compact || not (fitsInline laid.MinInlineWidth boxWidth)

            // A count pill is a bare number, so its tooltip says what is counted. Property pills keep the full
            // text, because their tooltip only adds the category.
            let texts = [
                for placed in laid.EdgePills do
                    if placed.View.Kind = PillViewKind.Property then
                        placed.View.FullText
                    elif placed.View.Kind <> PillViewKind.Toggle then
                        placed.View.Tooltip
            ]

            let spoken = String.concat ", " texts

            let menuOpenOnEdge =
                match openMenu with
                | Some open' -> open'.MenuEdgeId = laid.Edge.Id
                | None -> false

            let pill (placed: PlacedPill) =
                let onToggle (anchor: Element) =
                    if not listed || placed.View.Toggle = Some ToggleKind.Less then
                        toggle laid.Edge.Id
                    elif menuOpenOnEdge then
                        closeMenu ()
                    else
                        setMenu (
                            Some {
                                MenuEdgeId = laid.Edge.Id
                                MenuLayout = drawn
                                MenuBoxWidth = boxWidth
                                Opener = anchor
                            }
                        )

                let listOpen =
                    if listed && placed.View.Toggle = Some ToggleKind.More then
                        Some menuOpenOnEdge
                    else
                        None

                React.KeyedFragment(
                    placed.View.Key,
                    [
                        GraphCanvas.Pill(
                            placed,
                            highlighted = laid.Edge.Highlighted,
                            owner = owner,
                            ?listOpen = listOpen,
                            edgeId = laid.Edge.Id,
                            onToggle = onToggle
                        )
                    ]
                )

            Svg.g [
                svgRole "group"
                svgTestId $"graph-edge-{laid.Edge.Id}"
                svgLabel (
                    if texts.IsEmpty then
                        $"From {owner}"
                    else
                        $"From {owner}: {spoken}"
                )
                svg.children [
                    if laid.Path <> "" then
                        Svg.path [
                            svg.d laid.Path
                            svg.fill "none"
                            svgStyle [
                                "stroke", edgeColor laid
                                "strokeWidth", (if laid.Edge.Highlighted then "2.5" else "1.5")
                            ]
                            svg.custom ("markerEnd", $"url(#{markerPrefix}-{arrowName laid})")
                            svg.children [
                                match laid.Edge.Tooltip with
                                | Some tooltip -> Svg.title tooltip
                                | None -> ()
                            ]
                        ]
                    for placed in laid.EdgePills do
                        pill placed
                ]
            ]

        let canvas (drawn: GraphLayout) =
            Html.div [
                prop.testId "graph-canvas-content"
                prop.custom ("data-compact", drawn.Compact)
                prop.className (
                    if drawn.Compact then
                        "swt:relative swt:flex-none swt:mt-auto swt:mr-auto swt:mb-auto swt:ml-0"
                    else
                        "swt:relative swt:flex-none swt:m-auto"
                )
                prop.children [
                    Svg.svg [
                        svg.xmlns "http://www.w3.org/2000/svg"
                        svg.className "swt:block swt:max-w-none"
                        svg.width (drawn.LayoutWidth * zoom)
                        svg.height (drawn.LayoutHeight * zoom)
                        svg.custom ("viewBox", $"0 0 {drawn.LayoutWidth} {drawn.LayoutHeight}")
                        svgRole "group"
                        svg.children [
                            Svg.defs [
                                for name in arrowNames do
                                    Svg.marker [
                                        svg.id $"{markerPrefix}-{name}"
                                        svg.markerWidth 8
                                        svg.markerHeight 8
                                        svg.custom ("refX", 7)
                                        svg.custom ("refY", 4)
                                        svg.custom ("orient", "auto")
                                        svg.children [
                                            Svg.path [
                                                svg.d "M0,0 L8,4 L0,8 z"
                                                svgStyle [ "fill", arrowColor name ]
                                            ]
                                        ]
                                    ]
                            ]
                            for laid in drawn.LaidOutEdges do
                                React.KeyedFragment(laid.Edge.Id, [ edgeGroup drawn laid ])
                            for laid in drawn.LaidOutNodes do
                                React.KeyedFragment(
                                    laid.Node.Id,
                                    [ GraphCanvas.NodeShape(laid, ?onClick = onNodeClick) ]
                                )
                        ]
                    ]
                    match openMenu, menuEdge with
                    | Some open', Some edge ->
                        GraphCanvas.PillMenu(
                            (if drawn.Compact then "Details" else "More properties") + " of " + ownerOf edge,
                            (if drawn.Compact then
                                 Array.append edge.Edge.Pills edge.Edge.MorePills
                             else
                                 edge.Edge.MorePills),
                            boxHeight,
                            rootRef,
                            open'.Opener,
                            closeMenu
                        )
                    | _ -> ()
                ]
            ]

        let zoomButton (testId: string) (label: string) (glyph: string) (disabled: bool) (next: float) =
            Html.button [
                prop.type'.button
                prop.testId testId
                prop.className "swt:btn swt:btn-ghost swt:btn-square swt:btn-sm swt:text-xl"
                prop.ariaLabel label
                prop.title label
                prop.disabled disabled
                prop.onClick (fun _ ->
                    closeMenu ()
                    setZoom next
                )
                prop.text glyph
            ]

        Html.div [
            prop.className "swt:relative swt:size-full swt:min-h-0"
            prop.style [
                for name, value in rootColors -> style.custom (name, value)
            ]
            prop.children [
                Html.div [
                    prop.ref rootRef
                    prop.testId "graph-canvas"
                    prop.className
                        "swt:relative swt:box-border swt:flex swt:size-full swt:overflow-auto swt:overscroll-auto swt:[scrollbar-gutter:stable] swt:text-base-content swt:focus:outline-none"
                    prop.role.region
                    prop.ariaLabel ariaLabel
                    prop.tabIndex -1
                    prop.children [
                        match drawn with
                        | Some drawn -> canvas drawn
                        | None when shown.IsSome ->
                            Html.p [
                                prop.testId "graph-status"
                                prop.className "swt:m-0 swt:px-3.5 swt:py-4 swt:text-base-content/60"
                                prop.role.alert
                                prop.text "Could not lay out this graph."
                            ]
                        | None ->
                            Html.p [
                                prop.testId "graph-status"
                                prop.className "swt:m-0 swt:px-3.5 swt:py-4 swt:text-base-content/60"
                                prop.role.status
                                prop.text "Laying out…"
                            ]
                    ]
                ]
                if drawn.IsSome then
                    Html.div [
                        prop.className
                            "swt:absolute swt:right-3 swt:bottom-3 swt:z-20 swt:flex swt:items-center swt:gap-1.5 swt:rounded-lg swt:border swt:border-(color:--graph-border) swt:bg-base-100 swt:p-1 swt:text-xs swt:tabular-nums swt:shadow-md"
                        prop.role.group
                        prop.ariaLabel "Graph zoom"
                        prop.children [
                            zoomButton "graph-zoom-out" "Zoom out" "−" (zoom <= minZoom) (max minZoom (zoom - zoomStep))
                            Html.span [
                                prop.className "swt:min-w-10 swt:text-center"
                                prop.custom ("aria-live", "polite")
                                prop.custom ("aria-atomic", "true")
                                prop.text (string (floor (zoom * 100.0 + 0.5)) + "%")
                            ]
                            zoomButton "graph-zoom-in" "Zoom in" "+" (zoom >= maxZoom) (min maxZoom (zoom + zoomStep))
                        ]
                    ]
            ]
        ]
