namespace Swate.Components.Page.ProvenanceGrouping

open System
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Primitive.Buttons
open Swate.Components.Primitive.Popover
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Page.ProvenanceGrouping.Types
open Swate.Components.Composite.TermSearch
open Swate.Components.Composite.TermSearch.Types

type private DraftValueKind =
    | DraftText
    | DraftInteger
    | DraftFloat
    | DraftTerm

/// Converts provenance sides into lower-case text used in labels and test ids.
module private SideLabels =

    let sideName side =
        match side with
        | ProvenanceSide.Input -> "input"
        | ProvenanceSide.Output -> "output"

module private ColorPicker =

    let fallbackColor =
        State.PropertyColors.palette |> Array.tryHead |> Option.defaultValue "#2563eb"

    let currentOrFallback color =
        match color with
        | Some c when c <> "" -> c
        | _ -> fallbackColor

    let content ariaLabel (draftColor: string) (setDraftColor: string -> unit) onSetColor =
        Html.div [
            prop.className "swt:flex swt:items-center swt:gap-2 swt:p-2"
            prop.children [
                Html.input [
                    prop.custom ("type", "color")
                    prop.className "swt:h-8 swt:w-10 swt:cursor-pointer swt:rounded swt:border swt:border-base-300"
                    prop.value draftColor
                    prop.ariaLabel ariaLabel
                    prop.onChange (fun (color: string) -> setDraftColor color)
                ]
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-xs swt:btn-primary swt:min-h-0 swt:py-0"
                    prop.text "Select"
                    prop.onClick (fun _ -> onSetColor (Some draftColor))
                ]
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-xs swt:btn-ghost swt:min-h-0 swt:py-0"
                    prop.text "Clear"
                    prop.onClick (fun _ ->
                        setDraftColor fallbackColor
                        onSetColor None
                    )
                ]
            ]
        ]

/// Converts between draft form state and typed provenance values.
module private ValueDrafts =

    let kindForValue value =
        match value with
        | ProvenanceValue.Text _ -> DraftText
        | ProvenanceValue.Integer _ -> DraftInteger
        | ProvenanceValue.Float _ -> DraftFloat
        | ProvenanceValue.Term _ -> DraftTerm

    let kindName kind =
        match kind with
        | DraftText -> "Text"
        | DraftInteger -> "Integer"
        | DraftFloat -> "Float"
        | DraftTerm -> "Term"

    let kindFromName value =
        match value with
        | "Integer" -> DraftInteger
        | "Float" -> DraftFloat
        | "Term" -> DraftTerm
        | _ -> DraftText

    let textForValue value =
        match value with
        | ProvenanceValue.Text text -> text
        | ProvenanceValue.Integer value -> string value
        | ProvenanceValue.Float value -> string value
        | ProvenanceValue.Term term -> term.Name

    let termForValue value =
        match value with
        | ProvenanceValue.Term term -> Some term
        | _ -> None

    let tryValue kind (text: string) term =
        match kind with
        | DraftText -> Some(ProvenanceValue.Text text)
        | DraftInteger ->
            match Int32.TryParse text with
            | true, value -> Some(ProvenanceValue.Integer value)
            | _ -> None
        | DraftFloat ->
            match Double.TryParse text with
            | true, value when not (Double.IsNaN value || Double.IsInfinity value) -> Some(ProvenanceValue.Float value)
            | _ -> None
        | DraftTerm -> term |> Option.map ProvenanceValue.Term

/// Maps provenance terms to the TermSearch component shape and back.
module private TermSearchMapping =

    let toTermSearchTerm (term: ProvenanceTerm) =
        Term(name = term.Name, ?id = term.TermAccession, ?source = term.TermSource)

    let fromTermSearchTerm (term: Term) =
        term.name
        |> Option.map (fun name -> {
            Name = name
            TermSource = term.source
            TermAccession = term.id
        })

/// Creates editor-owned generic provenance kinds for user-created values.
module private KindNames =

    let editorProperty = ProvenanceKind.create "editor:property" "Property"

[<Erase; Mangle(false)>]
type Controls =

    [<ReactComponent>]
    static member ConnectionHandle
        (handle: ConnectionHandleRef, ?label: string, ?className: string, ?debug: bool, ?key: string)
        =
        let draggable =
            DndKit.useDraggable (
                {|
                    id = DragDrop.connectionHandleDragId handle
                |}
            )

        let droppable =
            DndKit.useDroppable (
                {|
                    id = DragDrop.connectionHandleDropId handle
                |}
            )

        let setNodeRef node =
            draggable.setNodeRef node
            droppable.setNodeRef node

        Html.span [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.ref setNodeRef
            yield! prop.spread (!!draggable.attributes)
            yield! prop.spread (!!draggable.listeners)
            prop.role.button
            prop.tabIndex 0
            prop.ariaLabel (label |> Option.defaultValue "Connect")
            prop.custom ("data-provenance-connection-node", DragDrop.connectionHandleNodeId handle)
            prop.custom ("data-provenance-connection-drop-id", DragDrop.connectionHandleDropId handle)
            prop.className [
                "swt:inline-flex swt:size-3 swt:shrink-0 swt:cursor-crosshair swt:items-center swt:justify-center swt:rounded-full swt:border swt:border-primary swt:bg-primary/70 swt:align-middle swt:opacity-55 swt:transition"
                "hover:swt:opacity-100 focus:swt:opacity-100 focus:swt:outline-none focus:swt:ring-2 focus:swt:ring-primary/40"
                if droppable.isOver then
                    "swt:opacity-100 swt:ring-2 swt:ring-primary"
                if draggable.isDragging then
                    "swt:opacity-100 swt:ring-2 swt:ring-primary swt:ring-offset-2 swt:ring-offset-base-100"
                match className with
                | Some className -> className
                | None -> ()
            ]
            if defaultArg debug false then
                prop.testId $"provenance-connection-handle-{handle.Side}-{handle.Kind}"
        ]

    /// Measurement-only connector endpoint: the overlay reads its position, but it is
    /// never draggable or droppable and never receives pointer events.
    [<ReactComponent>]
    static member ConnectionAnchor(handle: ConnectionHandleRef, className: string, ?debug: bool) =
        Html.span [
            prop.ariaHidden true
            prop.custom ("data-provenance-connection-node", DragDrop.connectionHandleNodeId handle)
            prop.className [
                "swt:pointer-events-none swt:absolute swt:size-px"
                className
            ]
            if defaultArg debug false then
                prop.testId $"provenance-connection-anchor-{handle.Side}-{handle.Kind}"
        ]

    [<ReactComponent>]
    static member LayerTabs
        (
            session: ProvenanceSession,
            onSelect: ProvenanceLayerId -> unit,
            onAddLayer: unit -> unit,
            ?debug: bool,
            ?layerColors: Map<ProvenanceLayerId, ProvenanceColor>,
            ?onSetLayerColor: ProvenanceLayerId -> ProvenanceColor option -> unit
        ) =
        Html.div [
            prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
            prop.children [
                for layerId in session.LayerOrder do
                    let layer = Session.layerById layerId session

                    let layerColor =
                        layerColors |> Option.bind (fun colors -> colors |> Map.tryFind layer.Id)

                    let layerButtonClasses = [
                        "swt:btn swt:btn-sm swt:join-item"
                        if layerId = session.ActiveLayerId then
                            "swt:btn-primary"
                        else
                            "swt:btn-outline"
                    ]

                    let swatchButtonClass =
                        [
                            yield! layerButtonClasses
                            "swt:min-h-8 swt:w-8 swt:px-0"
                        ]
                        |> String.concat " "

                    Html.div [
                        prop.className "swt:join"
                        prop.children [
                            match onSetLayerColor with
                            | Some setLayerColor ->
                                Controls.LayerColorButton(
                                    layer.Id,
                                    layerColor,
                                    setLayerColor layer.Id,
                                    triggerClassName = swatchButtonClass
                                )
                            | None ->
                                Html.span [
                                    prop.className [
                                        yield! layerButtonClasses
                                        "swt:min-h-8 swt:w-8 swt:px-0"
                                    ]
                                    match layerColor with
                                    | Some color when color <> "" -> prop.style [ style.backgroundColor color ]
                                    | _ -> ()
                                ]
                            Html.button [
                                prop.title $"View provenance layer {layer.Label}"
                                prop.className layerButtonClasses
                                prop.ariaLabel $"View provenance layer {layer.Label}"
                                if defaultArg debug false then
                                    prop.testId $"provenance-layer-{layerId}"
                                    prop.custom ("data-provenance-layer-color", layerColor |> Option.defaultValue "")
                                prop.onClick (fun _ -> onSelect layerId)
                                prop.children [ Html.span layer.Label ]
                            ]
                        ]
                    ]
                Html.button [
                    prop.title "Add layer"
                    prop.className "swt:btn swt:btn-sm swt:btn-primary"
                    if defaultArg debug false then
                        prop.testId "provenance-add-layer"
                    prop.onClick (fun _ -> onAddLayer ())
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--add-square-multiple-20-regular swt:size-4"
                        ]
                        Html.span "Layer"
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member LayerColorButton
        (
            layerId: ProvenanceLayerId,
            currentColor: ProvenanceColor option,
            onSetColor: ProvenanceColor option -> unit,
            ?triggerClassName: string
        ) : ReactElement =
        let draftColor, setDraftColor =
            React.useState (ColorPicker.currentOrFallback currentColor)

        React.useEffect ((fun () -> setDraftColor (ColorPicker.currentOrFallback currentColor)), [| box currentColor |])

        let triggerClassName =
            defaultArg
                triggerClassName
                "swt:size-4 swt:shrink-0 swt:rounded swt:border swt:border-base-300 swt:cursor-pointer swt:align-middle"

        Popover.Simple(
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className triggerClassName
                    match currentColor with
                    | Some c when c <> "" -> prop.style [ style.backgroundColor c ]
                    | _ -> ()
                    prop.ariaLabel $"Set color for layer {layerId}"
                ],
            content = ColorPicker.content $"Choose color for layer {layerId}" draftColor setDraftColor onSetColor
        )

    [<ReactComponent>]
    static member private PropertySwapButton
        (
            side: ProvenanceSide,
            header: ProvenancePropertyHeader,
            onSwitch: ProvenancePropertyHeader -> unit,
            ?debug: bool
        ) =
        let sideName = SideLabels.sideName side

        Html.button [
            prop.title $"Move {header.Category.Name} grouping from {sideName}"
            prop.type'.button
            prop.className "swt:btn swt:btn-xs swt:btn-ghost swt:btn-square swt:btn-outline swt:z-10 swt:bg-base-100/90"
            prop.ariaLabel $"Move {header.Category.Name} grouping from {sideName}"
            if defaultArg debug false then
                prop.testId $"provenance-property-drag-{side}-{header.Category.Name}"
            prop.onPointerUp (fun _ -> onSwitch header)
            prop.onClick (fun _ -> onSwitch header)
            prop.children [
                Html.i [
                    prop.className "swt:iconify swt:fluent--arrow-swap-20-regular swt:size-4"
                ]
            ]
        ]

    [<ReactComponent>]
    static member private PropertyRailItem
        (
            side: ProvenanceSide,
            header: ProvenancePropertyHeader,
            propertyValues: ProvenancePropertyValue list,
            active: GroupingAssignment list,
            canSwitch: bool,
            expanded: bool,
            onToggleSide: ProvenancePropertyHeader -> unit,
            onToggleBoth: ProvenancePropertyHeader -> unit,
            onSwitch: ProvenancePropertyHeader -> unit,
            onToggleExpanded: ProvenancePropertyHeader -> unit,
            onAddValue: ProvenancePropertyHeader -> ProvenanceValue -> ProvenanceTerm option -> unit,
            setIsValueChipDragging: bool -> unit,
            ?debug: bool,
            ?key: string,
            ?stats: PropertyStats,
            ?badge: PropertyCountBadge,
            ?color: ProvenanceColor,
            ?origins: Set<PropertyOrigin>,
            ?onSetColor: ProvenanceColor option -> unit,
            ?sourceInfoForValue: ProvenancePropertyValue -> PropertyValueSourceInfo option
        ) =
        let draggable =
            DndKit.useDraggable (
                {|
                    id = DragDrop.propertyDragId side header
                |}
            )

        let density = React.useContext Density.context
        let controlsVisible, setControlsVisible = React.useState false

        let sideScope =
            match side with
            | ProvenanceSide.Input -> GroupingScope.Input
            | ProvenanceSide.Output -> GroupingScope.Output

        let sideSelected =
            active
            |> List.exists (fun assignment -> assignment.Key.Header = header && assignment.Scope = sideScope)

        let bothSelected =
            active
            |> List.exists (fun assignment -> assignment.Key.Header = header && assignment.Scope = GroupingScope.Both)

        let sideName = SideLabels.sideName side

        // Property headers are never draggable connection sources; the overlay derives
        // their connectors from model data and only needs this anchor to measure the
        // group-facing edge of the header button. The anchor lives inside the button so
        // it follows the button edge as the button resizes with its content.
        let propertyAnchor =
            Controls.ConnectionAnchor(
                {
                    Kind = ConnectionHandleKind.PropertyHeader
                    Side = side
                    Id = DragDrop.propertyHeaderIdentity header
                    ParentGroupId = None
                },
                (match side with
                 | ProvenanceSide.Input -> "swt:top-1/2 swt:right-0 swt:translate-x-1/2 swt:-translate-y-1/2"
                 | ProvenanceSide.Output -> "swt:top-1/2 swt:left-0 swt:-translate-x-1/2 swt:-translate-y-1/2"),
                ?debug = debug
            )

        let propertyButton =
            Html.button [
                prop.type'.button
                if canSwitch then
                    prop.ref draggable.setNodeRef
                    yield! prop.spread (!!draggable.attributes)
                    yield! prop.spread (!!draggable.listeners)
                prop.className [
                    // swt:shrink overrides the btn class's flex-shrink:0 so the button can
                    // give way (and truncate) when the rail row runs out of space.
                    "swt:btn swt:btn-sm swt:h-auto swt:relative swt:shrink swt:min-w-0 swt:max-w-[18rem] swt:justify-start "
                    "swt:@max-xs/provenancePanel:px-2 swt:@max-xs/provenancePanel:text-[0.7rem]"
                    match density with
                    | Density.EditorDensity.Compact -> "swt:min-h-6 swt:py-0.5 swt:text-[0.7rem]"
                    | _ -> "swt:min-h-8 swt:py-1"

                    match side with
                    | ProvenanceSide.Input -> "swt:rounded-l-none"
                    | ProvenanceSide.Output -> "swt:rounded-r-none"
                    if sideSelected then
                        "swt:btn-primary"
                    else
                        "swt:btn-outline"
                    if not sideSelected then
                        "swt:bg-base-100/90"
                    if canSwitch then
                        yield! Styles.draggableButtonClasses draggable.isDragging

                ]
                prop.custom ("data-provenance-resize-node", "true")
                if defaultArg debug false then
                    prop.testId $"provenance-property-{side}-{header.Category.Name}"
                prop.onClick (fun _ -> onToggleSide header)
                prop.children [
                    propertyAnchor
                    match color with
                    | Some c when c <> "" ->
                        Html.span [
                            prop.className "swt:size-2.5 swt:shrink-0 swt:rounded"
                            prop.style [ style.backgroundColor c ]
                        ]
                    | _ -> Html.none
                    Html.span [
                        prop.className "swt:min-w-0 swt:truncate swt:text-left"
                        prop.text header.Category.Name
                    ]
                    match badge with
                    | Some badge ->
                        match badge with
                        | PropertyCountBadge.Hide -> Html.none
                        | PropertyCountBadge.DistinctValues n ->
                            Html.span [
                                prop.className "swt:badge swt:badge-xs swt:shrink-0"
                                prop.text (string n)
                            ]
                        | PropertyCountBadge.Coverage(setsWithValue, total) ->
                            Html.span [
                                prop.className "swt:badge swt:badge-xs swt:badge-warning swt:shrink-0"
                                prop.text $"{setsWithValue}/{total}"
                            ]
                    | None -> Html.none
                    match origins with
                    | Some origins ->
                        let hasCurrent =
                            origins
                            |> Set.exists (
                                function
                                | PropertyOrigin.Current _ -> true
                                | _ -> false
                            )

                        let hasUpstream =
                            origins
                            |> Set.exists (
                                function
                                | PropertyOrigin.UpstreamLayer _
                                | PropertyOrigin.PreviousContext _ -> true
                                | _ -> false
                            )

                        if hasCurrent && hasUpstream then
                            Html.i [
                                prop.className
                                    "swt:iconify swt:fluent--layer-diagonal-20-regular swt:size-3 swt:shrink-0 swt:text-base-content/60"
                                prop.title "Current and upstream"
                            ]
                        elif hasCurrent then
                            Html.i [
                                prop.className
                                    "swt:iconify swt:fluent--target-arrow-20-regular swt:size-3 swt:shrink-0 swt:text-base-content/60"
                                prop.title "Current"
                            ]
                        elif hasUpstream then
                            Html.i [
                                prop.className
                                    "swt:iconify swt:fluent--arrow-trending-lines-20-regular swt:size-3 swt:shrink-0 swt:text-base-content/60"
                                prop.title "Upstream"
                            ]
                        else
                            Html.none
                    | None -> Html.none
                ]
            ]

        let expandButton =
            Html.button [
                prop.type'.button
                prop.className [
                    "swt:btn swt:btn-xs swt:btn-square swt:z-10 swt:btn-outline swt:bg-base-100/90"

                    match side with
                    | ProvenanceSide.Input -> " swt:rounded-r-none swt:border-r-0"
                    | ProvenanceSide.Output -> "swt:rounded-l-none swt:border-l-0"
                    match density with
                    | Density.EditorDensity.Compact -> "swt:min-h-6 swt:py-0.5"
                    | _ -> "swt:min-h-8 swt:py-1"
                ]
                if defaultArg debug false then
                    prop.testId $"provenance-property-expand-{side}-{header.Category.Name}"
                prop.ariaLabel (
                    if expanded then
                        $"Collapse {header.Category.Name} values"
                    else
                        $"Expand {header.Category.Name} values"
                )
                prop.onClick (fun _ -> onToggleExpanded header)
                prop.children [
                    Html.i [
                        prop.className [
                            "swt:iconify swt:size-4"
                            if expanded then
                                "swt:fluent--chevron-up-20-regular"
                            else
                                "swt:fluent--chevron-down-20-regular"
                        ]
                    ]
                ]
            ]

        let propertyButtonWithExpand =
            Html.div [
                prop.className "swt:flex swt:min-w-0"
                prop.children [
                    match side with
                    | ProvenanceSide.Input ->
                        expandButton
                        propertyButton
                    | ProvenanceSide.Output ->
                        propertyButton
                        expandButton
                ]
            ]

        let bothButton =
            Html.button [
                prop.type'.button
                prop.title $"Group {header.Category.Name} on both sides"
                prop.className [
                    "swt:btn swt:btn-xs swt:btn-square swt:z-10"
                    if bothSelected then
                        "swt:btn-primary"
                    else
                        "swt:btn-outline"
                    if not bothSelected then
                        "swt:bg-base-100/90"
                ]
                if defaultArg debug false then
                    prop.testId $"provenance-property-both-{side}-{header.Category.Name}"
                prop.ariaLabel $"Group {header.Category.Name} on both sides"
                prop.onClick (fun _ -> onToggleBoth header)
                prop.children [
                    Html.i [
                        prop.className "swt:iconify swt:fluent--link-multiple-20-regular swt:size-4"
                    ]
                ]
            ]

        let swapButton =
            if canSwitch then
                Controls.PropertySwapButton(side, header, onSwitch, ?debug = debug)
            else
                Html.button [
                    prop.type'.button
                    prop.disabled true
                    prop.className
                        "swt:btn swt:btn-xs swt:btn-ghost swt:btn-square swt:z-10 swt:btn-outline swt:border-white swt:bg-base-100/90"
                    prop.ariaLabel $"Move {header.Category.Name} grouping from {sideName}"
                    if defaultArg debug false then
                        prop.testId $"provenance-property-drag-{side}-{header.Category.Name}"
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--arrow-swap-20-regular swt:size-4"
                        ]
                    ]
                ]

        let colorButton =
            match onSetColor with
            | Some setColor -> Controls.PropertyColorButton(header, color, setColor)
            | None -> Html.none

        // The secondary controls leave the layout entirely until their row is
        // hovered or holds focus, so idle rows are only as wide as their label.
        let rowControls =
            Html.span [
                prop.className [
                    "swt:flex swt:items-center swt:gap-0.5"
                    if not controlsVisible then
                        "swt:hidden"
                ]
                prop.children [
                    match side with
                    | ProvenanceSide.Input ->
                        colorButton
                        swapButton
                        bothButton

                    | ProvenanceSide.Output ->
                        bothButton
                        swapButton
                        colorButton
                ]
            ]

        Html.div [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.className "swt:flex swt:flex-col swt:gap-1"
            prop.children [
                Html.div [
                    prop.className [
                        "swt:flex swt:items-center swt:gap-1"
                        // The whole row hugs the rail-facing side, so the group-facing
                        // button edge (with its connector anchor) moves with the button's
                        // content size and frees the rest of the rail for connectors.
                        if side = ProvenanceSide.Output then
                            "swt:justify-end"
                    ]
                    prop.onMouseEnter (fun _ -> setControlsVisible true)
                    prop.onMouseLeave (fun _ -> setControlsVisible false)
                    // React maps onFocus/onBlur to focusin/focusout, so these bubble up
                    // from the row's buttons; blur only hides the controls when focus
                    // actually leaves the row.
                    prop.onFocus (fun _ -> setControlsVisible true)
                    prop.onBlur (fun event ->
                        let row: Browser.Types.HTMLElement = unbox event.currentTarget
                        let related: Browser.Types.HTMLElement = unbox event.relatedTarget

                        if isNull (box related) || not (row.contains related) then
                            setControlsVisible false
                    )
                    prop.children [
                        // The controls sit on the rail-facing side; the property text
                        // button faces the group cards so connectors attach directly to it.
                        match side with
                        | ProvenanceSide.Input ->
                            propertyButtonWithExpand
                            rowControls
                        | ProvenanceSide.Output ->
                            rowControls
                            propertyButtonWithExpand
                    ]
                ]
                if expanded then
                    Html.div [
                        prop.className [
                            "swt:flex swt:flex-col swt:gap-1 swt:z-20"
                            match side with
                            | ProvenanceSide.Input -> "swt:items-start swt:pl-2"
                            | ProvenanceSide.Output -> "swt:items-end swt:pr-2"
                        ]
                        if defaultArg debug false then
                            prop.testId $"provenance-property-values-{side}-{header.Category.Name}"
                        prop.children [
                            for propertyValue in propertyValues do
                                let sourceInfo =
                                    sourceInfoForValue |> Option.bind (fun resolver -> resolver propertyValue)

                                Controls.ValueChip(
                                    propertyValue,
                                    onDragChanged = setIsValueChipDragging,
                                    draggable = true,
                                    showHeader = false,
                                    anchorSide = side,
                                    ?debug = debug,
                                    ?sourceInfo = sourceInfo,
                                    key = DragDrop.propertyValueIdentity propertyValue
                                )
                            Controls.AddValuePopover(
                                Some header,
                                (fun addedHeader value unit -> onAddValue addedHeader value unit),
                                label = "Add value",
                                ?debug = debug
                            )
                        ]
                    ]
                elif propertyValues.IsEmpty then
                    Html.none
                else
                    Html.none
            ]
        ]

    [<ReactComponent>]
    static member PropertyRail
        (
            side: ProvenanceSide,
            headers: ProvenancePropertyHeader list,
            active: GroupingAssignment list,
            valuesForHeader: ProvenancePropertyHeader -> ProvenancePropertyValue list,
            isExpanded: ProvenancePropertyHeader -> bool,
            onToggleSide: ProvenancePropertyHeader -> unit,
            onToggleBoth: ProvenancePropertyHeader -> unit,
            onSwitch: ProvenancePropertyHeader -> unit,
            onToggleExpanded: ProvenancePropertyHeader -> unit,
            onAddValue: ProvenancePropertyHeader -> ProvenanceValue -> ProvenanceTerm option -> unit,
            canSwitch: ProvenancePropertyHeader -> bool,
            setIsValueChipDragging: bool -> unit,
            statsForHeader: ProvenancePropertyHeader -> PropertyStats option,
            badgeForHeader: ProvenancePropertyHeader -> PropertyCountBadge option,
            colorForHeader: ProvenancePropertyHeader -> ProvenanceColor option,
            originsForHeader: ProvenancePropertyHeader -> Set<PropertyOrigin> option,
            onSetColor: ProvenancePropertyHeader -> ProvenanceColor option -> unit,
            sourceInfoForValue: ProvenancePropertyValue -> PropertyValueSourceInfo option,
            ?sideId: ProvenanceLayerSideId,
            ?debug: bool
        ) =
        let droppable =
            DndKit.useDroppable (
                {|
                    id = DragDrop.propertyRailDropId side
                |}
            )

        let showAllHeaders, setShowAllHeaders = React.useState false

        // Long rails keep grouped/expanded properties visible and fold the rest
        // behind a toggle, so dense models do not bury the active properties.
        let collapseThreshold = 6

        let isPinned header =
            active |> List.exists (fun assignment -> assignment.Key.Header = header)
            || isExpanded header

        let visibleHeaders =
            if showAllHeaders || headers.Length <= collapseThreshold then
                headers
            else
                let pinned = headers |> List.filter isPinned

                let filler =
                    headers
                    |> List.filter (isPinned >> not)
                    |> List.truncate (max 0 (collapseThreshold - pinned.Length))

                headers
                |> List.filter (fun header -> pinned |> List.contains header || filler |> List.contains header)

        let hiddenHeaderCount = headers.Length - visibleHeaders.Length

        Html.aside [
            prop.ref droppable.setNodeRef
            prop.className [
                "swt:flex swt:min-w-0 swt:flex-col swt:gap-2"
                if droppable.isOver then
                    "swt:ring-2 swt:ring-primary swt:rounded"
            ]
            if defaultArg debug false then
                prop.testId $"provenance-property-rail-{side}"

                match sideId with
                | Some sideId -> prop.custom ("data-provenance-side-id", sideId)
                | None -> ()
            prop.children [
                Html.h3 [
                    prop.className [
                        "swt:text-sm swt:font-semibold swt:text-primary"
                        if side = ProvenanceSide.Output then
                            "swt:self-end"
                    ]
                    prop.text "Properties"
                ]
                for header in visibleHeaders do
                    Controls.PropertyRailItem(
                        side,
                        header,
                        valuesForHeader header,
                        active,
                        canSwitch header,
                        isExpanded header,
                        onToggleSide,
                        onToggleBoth,
                        onSwitch,
                        onToggleExpanded,
                        onAddValue,
                        setIsValueChipDragging,
                        ?stats = statsForHeader header,
                        ?badge = badgeForHeader header,
                        ?color = colorForHeader header,
                        ?origins = originsForHeader header,
                        onSetColor = onSetColor header,
                        sourceInfoForValue = sourceInfoForValue,
                        debug = defaultArg debug false,
                        key = DragDrop.propertyHeaderIdentity header
                    )
                if hiddenHeaderCount > 0 || showAllHeaders && headers.Length > collapseThreshold then
                    Html.button [
                        prop.type'.button
                        prop.className [
                            "swt:btn swt:btn-ghost swt:btn-xs swt:w-fit"
                            if side = ProvenanceSide.Output then
                                "swt:self-end"
                        ]
                        if defaultArg debug false then
                            prop.testId $"provenance-property-overflow-{side}"
                        prop.onClick (fun _ -> setShowAllHeaders (not showAllHeaders))
                        prop.text (
                            if showAllHeaders then
                                "Show fewer"
                            else
                                $"+{hiddenHeaderCount} more"
                        )
                    ]
                Html.div [
                    prop.className [
                        "swt:w-fit"
                        if side = ProvenanceSide.Output then
                            "swt:self-end"
                    ]
                    prop.children [
                        Controls.AddValuePopover(None, onAddValue, label = "Add property", ?debug = debug)
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member EditValuePopover
        (propertyValue: ProvenancePropertyValue, onApply: ProvenanceValue -> ProvenanceTerm option -> unit, ?debug: bool) =
        let category = propertyValue.Header.Category.Name
        let kind = ValueDrafts.kindForValue propertyValue.Value

        let value, setValue = React.useState (ValueDrafts.textForValue propertyValue.Value)

        let term, setTerm = React.useState (ValueDrafts.termForValue propertyValue.Value)

        let nextValue = ValueDrafts.tryValue kind value term
        let inputId = $"provenance-edit-{category}"

        Popover.Simple(
            ?debug =
                (if defaultArg debug false then
                     Some $"provenance-edit-{category}"
                 else
                     None),
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-ghost swt:btn-xs"
                    if defaultArg debug false then
                        prop.testId $"provenance-edit-trigger-{category}"
                    prop.ariaLabel $"Edit {category}"
                    prop.text $"Edit {category}"
                ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()
                        nextValue |> Option.iter (fun updated -> onApply updated propertyValue.Unit)
                    )
                    prop.children [
                        Html.label [
                            prop.htmlFor inputId
                            prop.className "swt:label"
                            prop.text $"{category} value"
                        ]
                        match kind with
                        | DraftTerm ->
                            TermSearch.TermSearch(
                                term |> Option.map TermSearchMapping.toTermSearchTerm,
                                (fun next -> setTerm (next |> Option.bind TermSearchMapping.fromTermSearchTerm))
                            )
                        | _ ->
                            Html.input [
                                prop.id inputId
                                prop.ariaLabel $"{category} value"
                                prop.className "swt:input swt:input-bordered swt:input-sm"
                                prop.value value
                                prop.onChange setValue
                            ]
                        if nextValue.IsNone then
                            Html.p [
                                prop.className "swt:text-xs swt:text-error"
                                prop.text "Enter a valid value."
                            ]
                        Html.button [
                            prop.type'.submit
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            prop.disabled nextValue.IsNone
                            if defaultArg debug false then
                                prop.testId "provenance-apply-value"
                            prop.text "Apply value"
                        ]
                    ]
                ]
        )

    [<ReactComponent>]
    static member AddValuePopover
        (
            header: ProvenancePropertyHeader option,
            onCreate: ProvenancePropertyHeader -> ProvenanceValue -> ProvenanceTerm option -> unit,
            ?label: string,
            ?debug: bool
        ) : ReactElement =
        let propertyKind =
            header
            |> Option.map (fun known -> known.Kind)
            |> Option.defaultValue KindNames.editorProperty

        let categoryTerm, setCategoryTerm =
            React.useState (header |> Option.map (fun known -> known.Category))

        let kind, setKind = React.useState DraftText
        let value, setValue = React.useState ""
        let term, setTerm = React.useState (None: ProvenanceTerm option)
        let unit', setUnit = React.useState (None: ProvenanceTerm option)

        let nextHeader =
            match header, categoryTerm with
            | Some known, _ -> Some known
            | None, Some category when not (String.IsNullOrWhiteSpace category.Name) ->
                Some {
                    Kind = propertyKind
                    Category = category
                }
            | _ -> None

        let category =
            nextHeader
            |> Option.map (fun next -> next.Category.Name)
            |> Option.defaultValue "Property"

        let nextValue = ValueDrafts.tryValue kind value term
        let canCreate = nextHeader.IsSome && nextValue.IsSome

        Popover.Simple(
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className Styles.addPropertyValueButtonClasses
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--add-20-regular swt:size-5 swt:shrink-0"
                        ]
                        Html.span [
                            prop.text (
                                label
                                |> Option.defaultValue (if header.IsSome then "Add value" else "Add property")
                            )
                        ]
                    ]
                ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()

                        match nextHeader, nextValue with
                        | Some createdHeader, Some created -> onCreate createdHeader created unit'
                        | _ -> ()
                    )
                    prop.children [
                        if header.IsNone then
                            Html.label [
                                prop.className "swt:label"
                                prop.text "Property category"
                            ]

                            TermSearch.TermSearch(
                                categoryTerm |> Option.map TermSearchMapping.toTermSearchTerm,
                                (fun next -> setCategoryTerm (next |> Option.bind TermSearchMapping.fromTermSearchTerm))
                            )
                        Html.label [ prop.className "swt:label"; prop.text "Value type" ]
                        Html.select [
                            prop.ariaLabel "Value type"
                            prop.className "swt:select swt:select-bordered swt:select-sm"
                            prop.value (ValueDrafts.kindName kind)
                            prop.onChange (ValueDrafts.kindFromName >> setKind)
                            prop.children [
                                Html.option [ prop.value "Text"; prop.text "Text" ]
                                Html.option [ prop.value "Integer"; prop.text "Integer" ]
                                Html.option [ prop.value "Float"; prop.text "Float" ]
                                Html.option [ prop.value "Term"; prop.text "Term" ]
                            ]
                        ]
                        Html.label [
                            prop.className "swt:label"
                            prop.text $"{category} value"
                        ]
                        match kind with
                        | DraftTerm ->
                            TermSearch.TermSearch(
                                term |> Option.map TermSearchMapping.toTermSearchTerm,
                                (fun next -> setTerm (next |> Option.bind TermSearchMapping.fromTermSearchTerm))
                            )
                        | _ ->
                            Html.input [
                                prop.ariaLabel $"{category} value"
                                prop.className "swt:input swt:input-bordered swt:input-sm"
                                prop.value value
                                prop.onChange setValue
                            ]
                        Html.label [ prop.className "swt:label"; prop.text "Unit" ]
                        TermSearch.TermSearch(
                            unit' |> Option.map TermSearchMapping.toTermSearchTerm,
                            (fun next -> setUnit (next |> Option.bind TermSearchMapping.fromTermSearchTerm))
                        )
                        if not canCreate then
                            Html.p [
                                prop.className "swt:text-xs swt:text-error"
                                prop.text "Enter a valid value."
                            ]
                        Html.button [
                            prop.type'.submit
                            prop.disabled (not canCreate)
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            prop.text (if header.IsSome then "Add value" else "Add property")
                        ]
                    ]
                ],
            ?debug =
                (if defaultArg debug false then
                     Some $"provenance-add-value-{category}"
                 else
                     None)
        )

    [<ReactComponent>]
    static member ValueLabel
        (propertyValue: ProvenancePropertyValue, ?debug: bool, ?key: string, ?sourceInfo: PropertyValueSourceInfo)
        : ReactElement =
        let label =
            $"{propertyValue.Header.Category.Name}: {Formatting.formatValue propertyValue.Value propertyValue.Unit}"

        let sourceTitle =
            match sourceInfo with
            | Some info ->
                let parts = [
                    match info.TableName with
                    | Some tn -> $"Table: {tn}"
                    | None -> ()
                    match info.ProcessName with
                    | Some pn -> $"Process: {pn}"
                    | None -> ()
                    if info.IsCurrentTable then
                        "Current table"
                ]

                if parts.IsEmpty then
                    None
                else
                    Some(System.String.Join("; ", parts))
            | _ -> None

        Html.div [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.className
                "swt:group swt:relative swt:flex swt:items-center swt:gap-1 swt:rounded swt:bg-base-200 swt:px-2 swt:py-1 swt:text-xs"
            match sourceTitle with
            | Some title -> prop.title title
            | None -> ()
            if defaultArg debug false then
                prop.testId $"provenance-value-{propertyValue.Id}"
            prop.children [
                Html.span [ prop.text label ]
                match sourceInfo with
                | Some info ->
                    Html.div [
                        prop.className
                            "swt:pointer-events-none swt:absolute swt:left-0 swt:top-full swt:z-30 swt:mt-1 swt:hidden swt:w-72 swt:rounded-md swt:border swt:border-base-300 swt:bg-base-100 swt:p-2 swt:shadow-lg group-hover:swt:block group-focus-within:swt:block"
                        prop.children [ Controls.SourceInfoPopover(Some info) ]
                    ]
                | None -> Html.none
            ]
        ]

    [<ReactComponent>]
    static member ValueChip
        (
            propertyValue: ProvenancePropertyValue,
            onDragChanged: bool -> unit,
            ?draggable: bool,
            ?showHeader: bool,
            ?anchorSide: ProvenanceSide,
            ?debug: bool,
            ?key: string,
            ?sourceInfo: PropertyValueSourceInfo
        ) : ReactElement =
        let canDrag = defaultArg draggable true
        let showHeader = defaultArg showHeader true
        let density = React.useContext Density.context

        let drag =
            DndKit.useDraggable (
                {|
                    id = DragDrop.valueDragId propertyValue.Id
                |}
            )

        let wasDragging = React.useRef false

        React.useEffect (
            (fun () ->
                if drag.isDragging <> wasDragging.current then
                    wasDragging.current <- drag.isDragging
                    onDragChanged (drag.isDragging)
            ),
            [| box drag.isDragging |]
        )

        // let isdragging = setIsValueChipDragging drag.isDragging


        let text = Formatting.formatValue propertyValue.Value propertyValue.Unit

        let label =
            if showHeader then
                $"{propertyValue.Header.Category.Name}: {text}"
            else
                text

        let valueAnchor =
            anchorSide
            |> Option.map (fun side ->
                Controls.ConnectionAnchor(
                    {
                        Kind = ConnectionHandleKind.PropertyValue
                        Side = side
                        Id = propertyValue.Id
                        ParentGroupId = None
                    },
                    (match side with
                     | ProvenanceSide.Input -> "swt:top-1/2 swt:right-0 swt:translate-x-1/2 swt:-translate-y-1/2"
                     | ProvenanceSide.Output -> "swt:top-1/2 swt:left-0 swt:-translate-x-1/2 swt:-translate-y-1/2"),
                    ?debug = debug
                )
            )

        Html.div [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.role.button
            prop.tabIndex 0
            if canDrag then
                prop.ref drag.setNodeRef
                yield! prop.spread (!!drag.attributes)
                yield! prop.spread (!!drag.listeners)
            prop.className [
                "swt:group swt:relative"
                yield! Styles.propertyValueButtonClasses density drag.isDragging
                if not canDrag then
                    "swt:cursor-default"
            ]
            prop.custom ("data-provenance-resize-node", "true")
            if defaultArg debug false then
                prop.testId $"provenance-value-{propertyValue.Id}"
            prop.ariaLabel $"Drag {propertyValue.Header.Category.Name} value"
            match sourceInfo with
            | Some info ->
                let parts = [
                    match info.TableName with
                    | Some tn -> $"Table: {tn}"
                    | None -> ()
                    match info.ProcessName with
                    | Some pn -> $"Process: {pn}"
                    | None -> ()
                    if info.IsCurrentTable then
                        "Current table"
                ]

                if not parts.IsEmpty then
                    prop.title (System.String.Join("; ", parts))
            | _ -> ()
            prop.children [
                match valueAnchor with
                | Some anchor -> anchor
                | None -> Html.none
                Html.span [
                    prop.className "swt:grow swt:min-w-0 swt:truncate swt:text-left"
                    prop.text label
                ]
                match sourceInfo with
                | Some info ->
                    Html.div [
                        prop.className
                            "swt:pointer-events-none swt:absolute swt:left-0 swt:top-full swt:z-30 swt:mt-1 swt:hidden swt:w-72 swt:rounded-md swt:border swt:border-base-300 swt:bg-base-100 swt:p-2 swt:shadow-lg group-hover:swt:block group-focus-within:swt:block"
                        prop.children [ Controls.SourceInfoPopover(Some info) ]
                    ]
                | None -> Html.none
            ]
        ]

    [<ReactComponent>]
    static member ValueDragPreview
        (propertyValue: ProvenancePropertyValue, ?showHeader: bool, ?debug: bool)
        : ReactElement =
        let showHeader = defaultArg showHeader true
        let text = Formatting.formatValue propertyValue.Value propertyValue.Unit

        let label =
            if showHeader then
                $"{propertyValue.Header.Category.Name}: {text}"
            else
                text

        Html.div [
            prop.className Styles.propertyValueOverlayClasses
            if defaultArg debug false then
                prop.testId "provenance-drag-overlay-value"
            prop.children [
                Html.span [
                    prop.className "swt:min-w-0 swt:truncate"
                    prop.text label
                ]
            ]
        ]

    [<ReactComponent>]
    static member AddEndpointPopover
        (
            side: ProvenanceSide,
            endpointKinds: ProvenanceKind list,
            existingEndpointNames: string list,
            onCreate: CreateLoadedSetCommand -> unit,
            ?debug: bool,
            ?key: string
        ) =
        let sideName = SideLabels.sideName side
        let name, setName = React.useState ""

        let availableKinds =
            if endpointKinds.IsEmpty then
                [ Endpoints.fallbackKind ]
            else
                endpointKinds

        let selectedKindId, setSelectedKindId = React.useState (availableKinds.Head.Id)

        let selectedKind =
            availableKinds
            |> List.tryFind (fun kind -> kind.Id = selectedKindId)
            |> Option.defaultValue availableKinds.Head

        let trimmedName = name.Trim()

        let duplicateName =
            existingEndpointNames
            |> List.exists (fun existing ->
                String.Equals(existing.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase)
            )

        let nameError =
            if String.IsNullOrWhiteSpace trimmedName then
                Some "Enter an endpoint name."
            elif duplicateName then
                Some "This endpoint already exists in the current layer."
            else
                None

        Popover.Simple(
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className Styles.addPropertyValueButtonClasses
                    if defaultArg debug false then
                        prop.testId $"provenance-add-{sideName}-trigger"
                    prop.ariaLabel $"Add {sideName}"
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--add-20-regular swt:size-5 swt:shrink-0"
                        ]
                        Html.span [ prop.text $"Add {sideName}" ]
                    ]
                ],
            content =
                Html.form [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.onSubmit (fun event ->
                        event.preventDefault ()

                        if nameError.IsNone then
                            onCreate {
                                Side = side
                                Header = selectedKind |> Endpoints.endpointHeader side
                                Name = trimmedName
                            }
                    )
                    prop.children [
                        Html.label [ prop.className "swt:label"; prop.text "Endpoint name" ]
                        Html.input [
                            prop.ariaLabel "Endpoint name"
                            prop.className [
                                "swt:input swt:input-bordered swt:input-sm"
                                if nameError.IsSome then
                                    "swt:input-error"
                            ]
                            prop.required true
                            prop.value name
                            prop.onChange setName
                        ]
                        match nameError with
                        | Some error ->
                            Html.p [
                                prop.className "swt:text-xs swt:text-error"
                                prop.text error
                            ]
                        | None -> Html.none
                        Html.label [ prop.className "swt:label"; prop.text "Endpoint kind" ]
                        Html.select [
                            prop.ariaLabel "Endpoint kind"
                            prop.className "swt:select swt:select-bordered swt:select-sm"
                            prop.value selectedKind.Id
                            prop.onChange setSelectedKindId
                            prop.children [
                                for kind in availableKinds do
                                    Html.option [
                                        prop.value kind.Id
                                        prop.text (ProvenanceKind.displayName kind)
                                    ]
                            ]
                        ]
                        Html.button [
                            prop.type'.submit
                            prop.className "swt:btn swt:btn-primary swt:btn-sm"
                            prop.disabled nameError.IsSome
                            if defaultArg debug false then
                                prop.testId $"provenance-create-{sideName}"
                            prop.text "Create endpoint"
                        ]
                    ]
                ],
            ?debug =
                (if defaultArg debug false then
                     Some $"provenance-add-{sideName}"
                 else
                     None)
        )

    [<ReactComponent>]
    static member FilterToolbar
        (
            filters: FilterState,
            onSearch: string -> unit,
            onPropertySort: PropertySort -> unit,
            onGroupSort: GroupSort -> unit,
            onValueCountFilter: PropertyValueCountFilter -> unit,
            onOriginFilter: PropertyOriginFilter -> unit,
            ?debug: bool
        ) =
        Html.div [
            prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2 swt:p-2"
            prop.children [
                Html.div [
                    prop.className "swt:relative swt:flex swt:items-center"
                    prop.children [
                        Html.i [
                            prop.className
                                "swt:iconify swt:fluent--search-20-regular swt:absolute swt:left-2 swt:size-4 swt:text-base-content/50"
                        ]
                        Html.input [
                            prop.className "swt:input swt:input-bordered swt:input-sm swt:pl-8"
                            prop.placeholder "Search properties & values..."
                            prop.value filters.SearchText
                            prop.onChange onSearch
                        ]
                    ]
                ]
                Html.select [
                    prop.className "swt:select swt:select-bordered swt:select-sm"
                    prop.value (
                        match filters.PropertySort with
                        | PropertySort.ValueCountDesc -> "ValueCountDesc"
                        | PropertySort.NameAsc -> "NameAsc"
                        | PropertySort.Origin -> "Origin"
                    )
                    prop.onChange (fun v ->
                        match v with
                        | "ValueCountDesc" -> onPropertySort PropertySort.ValueCountDesc
                        | "NameAsc" -> onPropertySort PropertySort.NameAsc
                        | "Origin" -> onPropertySort PropertySort.Origin
                        | _ -> ()
                    )
                    prop.children [
                        Html.option [ prop.value "ValueCountDesc"; prop.text "Value count" ]
                        Html.option [ prop.value "NameAsc"; prop.text "Name" ]
                        Html.option [ prop.value "Origin"; prop.text "Origin" ]
                    ]
                ]
                Html.select [
                    prop.className "swt:select swt:select-bordered swt:select-sm"
                    prop.value (
                        match filters.GroupSort with
                        | GroupSort.NameAsc -> "NameAsc"
                        | GroupSort.MemberCountDesc -> "MemberCountDesc"
                        | GroupSort.ConnectionCountDesc -> "ConnectionCountDesc"
                    )
                    prop.onChange (fun v ->
                        match v with
                        | "NameAsc" -> onGroupSort GroupSort.NameAsc
                        | "MemberCountDesc" -> onGroupSort GroupSort.MemberCountDesc
                        | "ConnectionCountDesc" -> onGroupSort GroupSort.ConnectionCountDesc
                        | _ -> ()
                    )
                    prop.children [
                        Html.option [ prop.value "NameAsc"; prop.text "Name" ]
                        Html.option [ prop.value "MemberCountDesc"; prop.text "Members" ]
                        Html.option [
                            prop.value "ConnectionCountDesc"
                            prop.text "Connections"
                        ]
                    ]
                ]
                Html.select [
                    prop.className "swt:select swt:select-bordered swt:select-sm"
                    prop.value (
                        match filters.ValueCountFilter with
                        | PropertyValueCountFilter.Any -> "Any"
                        | PropertyValueCountFilter.Singleton -> "Singleton"
                        | PropertyValueCountFilter.Multiple -> "Multiple"
                        | PropertyValueCountFilter.CoverageGap -> "CoverageGap"
                    )
                    prop.onChange (fun v ->
                        match v with
                        | "Any" -> onValueCountFilter PropertyValueCountFilter.Any
                        | "Singleton" -> onValueCountFilter PropertyValueCountFilter.Singleton
                        | "Multiple" -> onValueCountFilter PropertyValueCountFilter.Multiple
                        | "CoverageGap" -> onValueCountFilter PropertyValueCountFilter.CoverageGap
                        | _ -> ()
                    )
                    prop.children [
                        Html.option [ prop.value "Any"; prop.text "Any" ]
                        Html.option [ prop.value "Singleton"; prop.text "1 value" ]
                        Html.option [ prop.value "Multiple"; prop.text "2+ values" ]
                        Html.option [ prop.value "CoverageGap"; prop.text "Coverage gap" ]
                    ]
                ]
                Html.select [
                    prop.className "swt:select swt:select-bordered swt:select-sm"
                    prop.value (
                        match filters.OriginFilter with
                        | PropertyOriginFilter.AnyOrigin -> "AnyOrigin"
                        | PropertyOriginFilter.CurrentOnly -> "CurrentOnly"
                        | PropertyOriginFilter.AnyUpstream -> "AnyUpstream"
                        | PropertyOriginFilter.UpstreamLayer _ -> "UpstreamLayer"
                        | PropertyOriginFilter.PreviousContext _ -> "PreviousContext"
                    )
                    prop.onChange (fun v ->
                        match v with
                        | "AnyOrigin" -> onOriginFilter PropertyOriginFilter.AnyOrigin
                        | "CurrentOnly" -> onOriginFilter PropertyOriginFilter.CurrentOnly
                        | "AnyUpstream" -> onOriginFilter PropertyOriginFilter.AnyUpstream
                        | _ -> ()
                    )
                    prop.children [
                        Html.option [ prop.value "AnyOrigin"; prop.text "Any" ]
                        Html.option [ prop.value "CurrentOnly"; prop.text "Current only" ]
                        Html.option [ prop.value "AnyUpstream"; prop.text "Upstream" ]
                    ]
                ]
                if defaultArg debug false then
                    Html.span [ prop.testId "provenance-filter-toolbar" ]
            ]
        ]

    [<ReactComponent>]
    static member PropertyColorButton
        (
            header: ProvenancePropertyHeader,
            currentColor: ProvenanceColor option,
            onSetColor: ProvenanceColor option -> unit
        ) =
        let draftColor, setDraftColor =
            React.useState (ColorPicker.currentOrFallback currentColor)

        React.useEffect ((fun () -> setDraftColor (ColorPicker.currentOrFallback currentColor)), [| box currentColor |])

        Popover.Simple(
            trigger =
                Html.button [
                    prop.type'.button
                    prop.className
                        "swt:btn swt:btn-xs swt:btn-square swt:z-10 swt:btn-outline swt:shrink-0 swt:bg-base-100/90"
                    match currentColor with
                    | Some c when c <> "" -> prop.style [ style.backgroundColor c ]
                    | _ -> ()
                    prop.ariaLabel $"Set color for property {header.Category.Name}"
                ],
            content =
                ColorPicker.content
                    $"Choose color for property {header.Category.Name}"
                    draftColor
                    setDraftColor
                    onSetColor,
            triggerClassName = "swt:relative swt:z-10 swt:shrink-0"
        )

    [<ReactComponent>]
    static member SourceInfoPopover(sourceInfo: PropertyValueSourceInfo option) =
        match sourceInfo with
        | None -> Html.none
        | Some info ->
            Html.div [
                prop.className "swt:text-xs swt:space-y-1 swt:p-1"
                prop.children [
                    if info.IsCurrentTable then
                        Html.span [
                            prop.className "swt:font-semibold"
                            prop.text "Current table"
                        ]
                    else
                        match info.TableName with
                        | Some tableName ->
                            Html.span [
                                prop.className "swt:font-semibold"
                                prop.text $"Table: {tableName}"
                            ]
                        | None -> Html.none

                    match info.ProcessName with
                    | Some processName -> Html.span [ prop.text $"Process: {processName}" ]
                    | None -> Html.none

                    if not info.InputNames.IsEmpty then
                        Html.div [
                            prop.children [
                                Html.span [ prop.className "swt:font-medium"; prop.text "Inputs:" ]
                                for inputName in info.InputNames do
                                    Html.span [ prop.text $" {inputName}" ]
                            ]
                        ]
                    else
                        Html.none

                    if not info.OutputNames.IsEmpty then
                        Html.div [
                            prop.children [
                                Html.span [ prop.className "swt:font-medium"; prop.text "Outputs:" ]
                                for outputName in info.OutputNames do
                                    Html.span [ prop.text $" {outputName}" ]
                            ]
                        ]
                    else
                        Html.none
                ]
            ]
