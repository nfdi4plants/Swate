namespace Swate.Components.Composite.ProvenanceGrouping

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Primitive.Buttons
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Composite.ProvenanceGrouping.Types

/// Maps loaded endpoint kinds (Source, Sample, Data, ...) to a display label and icon.
/// The kind id carries an adapter prefix (`arc-isa:endpoint:sample`, `fixture:endpoint:data`, ...),
/// so we classify on the role suffix to stay source-agnostic.
module private EntityType =

    type Descriptor = { Label: string; Icon: string }

    let descriptor (kind: ProvenanceKind) : Descriptor =
        let id = kind.Id.ToLowerInvariant()
        let contains (token: string) = id.Contains token

        if contains "endpoint:source" then
            { Label = "Source"; Icon = "swt:fluent--branch-fork-20-regular" }
        elif contains "endpoint:sample" then
            { Label = "Sample"; Icon = "swt:fluent--beaker-20-regular" }
        elif contains "endpoint:data" then
            { Label = "File"; Icon = "swt:fluent--document-20-regular" }
        elif contains "endpoint:material" then
            { Label = "Material"; Icon = "swt:fluent--cube-20-regular" }
        elif contains "endpoint:free-text" then
            { Label = ProvenanceKind.displayName kind; Icon = "swt:fluent--text-field-20-regular" }
        else
            { Label = ProvenanceKind.displayName kind; Icon = "swt:fluent--tag-20-regular" }

    /// Small type line shown above an entity name.
    let line (descriptor: Descriptor) =
        Html.span [
            prop.className
                "swt:inline-flex swt:items-center swt:gap-1 swt:text-xs swt:font-medium swt:uppercase swt:tracking-wide swt:text-base-content/60"
            prop.children [
                Html.i [
                    prop.className $"swt:iconify {descriptor.Icon} swt:size-3 swt:shrink-0"
                    prop.ariaHidden true
                ]
                Html.span [ prop.text descriptor.Label ]
            ]
        ]

/// Derives display text and property chips for one provenance group card.
module private GroupCardData =

    /// Loaded endpoint kind for one member, resolved from the side-specific set map.
    let endpointKind side (model: ProvenanceModel) (setId: ProvenanceSetId) =
        let sets =
            match side with
            | ProvenanceSide.Input -> model.InputSets
            | ProvenanceSide.Output -> model.OutputSets

        sets.TryFind setId |> Option.map (fun set -> set.Header.Kind)

    let values (group: DisplayGroup) (model: ProvenanceModel) =
        group.Members
        |> List.collect (fun member' -> member'.PropertyValueIds)
        |> List.distinct
        |> List.choose (fun id -> model.PropertyValues.TryFind id)

    let memberValues (member': DisplayMember) (model: ProvenanceModel) =
        member'.PropertyValueIds
        |> List.distinct
        |> List.choose (fun id -> model.PropertyValues.TryFind id)

    /// One organizer tab per grouping value, ordered by category then value text.
    let tabs (group: DisplayGroup) =
        group.GroupingValues
        |> List.sortBy (fun value ->
            $"{value.Key.Header.Kind.Id}:{value.Key.Header.Category.Name}",
            Formatting.formatValue value.Value value.Unit)

    /// Members whose own property values carry the given grouping value.
    let membersWithValue (model: ProvenanceModel) (groupingValue: DisplayGroupingValue) (members: DisplayMember list) =
        members
        |> List.filter (fun member' ->
            member'.PropertyValueIds
            |> List.exists (fun id ->
                match model.PropertyValues.TryFind id with
                | Some value ->
                    value.Header = groupingValue.Key.Header
                    && value.Value = groupingValue.Value
                    && value.Unit = groupingValue.Unit
                | None -> false))

    let title (group: DisplayGroup) =
        match tabs group with
        | [] -> group.Members.Head.Name
        | tabs ->
            tabs
            |> List.map (fun value ->
                $"{value.Key.Header.Category.Name}: {Formatting.formatValue value.Value value.Unit}")
            |> String.concat ", "

/// Thin ResizeObserver binding used to re-check whether a tab's header still fits.
module private TabObserver =

    [<Emit("new ResizeObserver(() => $0())")>]
    let create (callback: unit -> unit) : obj = jsNative

    [<Emit("$0.observe($1)")>]
    let observe (observer: obj) (target: Browser.Types.HTMLElement) : unit = jsNative

    [<Emit("$0.disconnect()")>]
    let disconnect (observer: obj) : unit = jsNative

[<Erase; Mangle(false)>]
type GroupCard =

    /// One colored organizer tab showing "Category: Value". The category header is
    /// all-or-nothing: as soon as the full text stops fitting the header is dropped
    /// entirely and the value truncates on its own.
    [<ReactComponent>]
    static member private OrganizerTab
        (
            category: string,
            valueText: string,
            paletteClasses: string,
            isHighlighted: bool,
            setHighlighted: bool -> unit,
            ?testId: string,
            ?key: string
        ) =
        let tabRef = React.useRef<Browser.Types.HTMLElement option> None
        let fullLabelRef = React.useRef<Browser.Types.HTMLElement option> None
        let showHeader, setShowHeader = React.useState true

        let measure () =
            match fullLabelRef.current with
            | Some fullLabel ->
                setShowHeader (fullLabel.scrollWidth <= fullLabel.clientWidth + 1.0)
            | _ -> ()

        React.useEffectOnce (fun () ->
            measure ()
            let observer = TabObserver.create measure

            match tabRef.current with
            | Some element -> TabObserver.observe observer element
            | None -> ()

            FsReact.createDisposable (fun () -> TabObserver.disconnect observer))

        Html.span [
            prop.ref (fun element ->
                tabRef.current <- (if isNull element then None else Some(unbox element)))
            prop.tabIndex 0
            prop.title $"{category}: {valueText}"
            prop.custom ("data-hovered", isHighlighted)
            match testId with
            | Some testId -> prop.testId testId
            | None -> ()
            prop.onMouseEnter (fun _ -> setHighlighted true)
            prop.onMouseLeave (fun _ -> setHighlighted false)
            prop.onFocus (fun _ -> setHighlighted true)
            prop.onBlur (fun _ -> setHighlighted false)
            prop.className [
                "swt:relative swt:flex swt:min-w-0 swt:cursor-default swt:overflow-hidden swt:whitespace-nowrap swt:rounded-t-md swt:text-xs swt:outline-none swt:transition-all"
                paletteClasses
                if isHighlighted then
                    "swt:opacity-100 swt:shadow-md"
                else
                    "swt:opacity-75"
            ]
            prop.children [
                // Invisible in-flow copy: gives the tab its natural full width.
                Html.span [
                    prop.ariaHidden true
                    prop.className "swt:invisible swt:px-3 swt:py-1"
                    prop.text $"{category}: {valueText}"
                ]
                // Measurement overlay: checks whether the full untruncated label
                // fits in the actual visible tab width.
                Html.span [
                    prop.ref (fun element ->
                        fullLabelRef.current <- (if isNull element then None else Some(unbox element)))
                    prop.ariaHidden true
                    prop.className "swt:pointer-events-none swt:absolute swt:inset-0 swt:invisible swt:flex swt:items-baseline swt:overflow-visible swt:px-3 swt:py-1"
                    prop.children [
                        Html.span [
                            prop.className "swt:mr-1 swt:shrink-0"
                            prop.text $"{category}:"
                        ]
                        Html.span [
                            prop.className "swt:shrink-0 swt:font-medium"
                            prop.text valueText
                        ]
                    ]
                ]
                // Visible overlay: drops the header entirely once the tab shrinks.
                Html.span [
                    prop.className "swt:absolute swt:inset-0 swt:flex swt:items-baseline swt:px-3 swt:py-1"
                    prop.children [
                        if showHeader then
                            Html.span [
                                prop.className "swt:mr-1 swt:shrink-0"
                                prop.text $"{category}:"
                            ]
                        Html.span [
                            prop.className [
                                "swt:font-medium"
                                if showHeader then
                                    "swt:shrink-0"
                                else
                                    "swt:min-w-0 swt:truncate"
                            ]
                            prop.text valueText
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main
        (
            side: ProvenanceSide,
            group: DisplayGroup,
            model: ProvenanceModel,
            selected: bool,
            expanded: bool,
            onSelect: unit -> unit,
            onExpand: unit -> unit,
            isValueChipDragging: bool,
            ?connectionCount: int,
            ?debug: bool,
            ?key: string
        ) =
        let hoveredMemberId, setHoveredMemberId = React.useState<ProvenanceSetId option> None
        let hoveredTabIndex, setHoveredTabIndex = React.useState<int option> None
        let density = React.useContext Density.context

        let droppable =
            DndKit.useDroppable (
                {|
                    id = DragDrop.groupDropId side group.Id
                |}
            )

        let title = GroupCardData.title group
        let tabs = GroupCardData.tabs group

        // The folder previews all members by default; while a grouping tab is
        // hovered/focused it narrows to the members carrying that tab's value.
        // Inherited grouping values do not live on the members themselves, so an
        // empty match falls back to the full member list.
        let visibleMembers =
            match hoveredTabIndex |> Option.bind (fun index -> tabs |> List.tryItem index) with
            | Some groupingValue ->
                match GroupCardData.membersWithValue model groupingValue group.Members with
                | [] -> group.Members
                | members -> members
            | None -> group.Members

        // let dnd = DndKit.use()

        // let isValueChipDragging =
        //     match dnd.active with
        //     | Some active ->
        //         active.data.current?type = DragDrop.PropertyValue
        //     | None ->
        //         false

        // let dnd = DndKit.use()

        // let isValueChipDragging =
        //     match dnd.active with
        //     | Some active ->
        //         active.data.current?type = DragDrop.PropertyValue
        //     | None ->
        //         false

        // Two anchors at opposite card edges: the group-facing edge carries the draggable
        // group connection handle, the property-facing edge is measurement-only and is
        // where property/value connectors from the same-side rail attach.
        let groupHandleEdge, propertyAnchorEdge =
            match side with
            | ProvenanceSide.Input ->
                "swt:absolute swt:top-1/2 swt:right-0 swt:translate-x-1/2 swt:-translate-y-1/2 swt:z-10",
                "swt:top-1/2 swt:left-0 swt:-translate-x-1/2 swt:-translate-y-1/2"
            | ProvenanceSide.Output ->
                "swt:absolute swt:top-1/2 swt:left-0 swt:-translate-x-1/2 swt:-translate-y-1/2 swt:z-10",
                "swt:top-1/2 swt:right-0 swt:translate-x-1/2 swt:-translate-y-1/2"

        let groupHandle : ConnectionHandleRef =
            {
                Kind = ConnectionHandleKind.GroupCard
                Side = side
                Id = group.Id
                ParentGroupId = None
            }

        let propertyAnchor : ConnectionHandleRef =
            {
                Kind = ConnectionHandleKind.GroupPropertyAnchor
                Side = side
                Id = group.Id
                ParentGroupId = None
            }

        let memberDetailsPosition =
            match side with
            | ProvenanceSide.Input -> "swt:left-full swt:ml-2"
            | ProvenanceSide.Output -> "swt:right-full swt:mr-2"

        Html.article [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.ref droppable.setNodeRef
            prop.custom ("data-provenance-group-node", DragDrop.groupNodeId side group.Id)
            prop.custom ("data-provenance-group-drop-id", DragDrop.groupDropId side group.Id)
            prop.className [
                // Cards size to their content (the column aligns them toward their rail),
                // so the gap between the two card columns grows for group connectors. The
                // edge handles are positioned on the card border and move with its width.
                "swt:relative swt:flex swt:w-fit swt:max-w-full swt:flex-col swt:rounded-box swt:border swt:bg-base-100 swt:shadow-sm"
                match density with
                | Density.EditorDensity.Compact -> "swt:gap-1 swt:p-1.5"
                | _ -> "swt:gap-1.5 swt:p-2.5"
                if selected then
                    "swt:border-primary swt:bg-primary/5"
                else
                    "swt:border-base-300"
                if droppable.isOver && isValueChipDragging then
                    "swt:ring-2 swt:ring-primary"
            ]
            if defaultArg debug false then
                prop.testId $"provenance-group-{side}-{group.Id}"
            prop.children [
                Controls.ConnectionAnchor(propertyAnchor, propertyAnchorEdge, ?debug = debug)
                if not expanded then
                    Controls.ConnectionHandle(
                        groupHandle,
                        label = "Connect group",
                        className = groupHandleEdge,
                        ?debug = debug
                    )
                // Stacked layouts hide the connector overlay; this badge keeps
                // the connection information visible on the card itself.
                let connectionBadge =
                    match connectionCount with
                    | Some count when count > 0 ->
                        Html.span [
                            prop.className "swt:badge swt:badge-primary swt:badge-sm swt:shrink-0"
                            prop.title $"{count} connections"
                            prop.ariaLabel $"{count} connections"
                            prop.text $"⇄ {count}"
                        ]
                    | _ -> Html.none

                let selectButton =
                    Buttons.QuickAccessButton(
                        Html.i [
                            prop.className "swt:iconify swt:fluent--checkmark-circle-20-regular swt:size-4"
                        ],
                        "Select group",
                        (fun _ -> onSelect ())
                    )

                match tabs with
                | [] ->
                    // A card without grouping values is always a single loaded endpoint,
                    // so the type line sits above its name to mirror the expanded member rows.
                    Html.div [
                        prop.className "swt:flex swt:items-start swt:gap-2"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:min-w-0 swt:grow swt:flex-col swt:gap-0.5"
                                prop.title title
                                prop.children [
                                    match GroupCardData.endpointKind side model group.Members.Head.SetId with
                                    | Some kind -> EntityType.line (EntityType.descriptor kind)
                                    | None -> Html.none
                                    Html.h3 [
                                        prop.className "swt:min-w-0 swt:truncate swt:text-sm swt:font-semibold"
                                        prop.text title
                                    ]
                                ]
                            ]
                            connectionBadge
                            selectButton
                        ]
                    ]
                | tabs ->
                    // A grouped card is drawn as a file organizer: one tab per grouping
                    // value sits on top of a folder body that holds the members' type
                    // symbols. Hovering a tab opens it into the folder and narrows the
                    // preview to that tab's members.
                    let symbolIcon (descriptor: EntityType.Descriptor) =
                        Html.i [
                            prop.className $"swt:iconify {descriptor.Icon} swt:size-4 swt:shrink-0"
                        ]

                    let countLabel count =
                        Html.span [
                            prop.className "swt:text-xs swt:font-semibold"
                            prop.text (string count)
                        ]

                    // When few enough to fit, every visible member contributes one symbol
                    // side by side; otherwise the preview collapses to the dominant type
                    // symbol with a count.
                    let memberDescriptors =
                        visibleMembers
                        |> List.choose (fun member' ->
                            GroupCardData.endpointKind side model member'.SetId
                            |> Option.map EntityType.descriptor)

                    let maxInlineSymbols = 4

                    // The tab bar escapes the card padding so the tabs sit flush on the
                    // card's top edge, like register tabs on an archive folder.
                    let tabBarMargins =
                        match density with
                        | Density.EditorDensity.Compact -> "swt:-mx-1.5 swt:-mt-1.5"
                        | _ -> "swt:-mx-2.5 swt:-mt-2.5"

                    // Each tab gets its own color, like the colored index tabs of an
                    // expanding file organizer.
                    let tabPalette = [|
                        "swt:bg-primary swt:text-primary-content"
                        "swt:bg-secondary swt:text-secondary-content"
                        "swt:bg-accent swt:text-accent-content"
                        "swt:bg-info swt:text-info-content"
                        "swt:bg-success swt:text-success-content"
                        "swt:bg-warning swt:text-warning-content"
                        "swt:bg-error swt:text-error-content"
                    |]

                    Html.div [
                        prop.className "swt:flex swt:min-w-0 swt:flex-col swt:gap-2"
                        prop.children [
                            Html.div [
                                prop.className [
                                    "swt:flex swt:min-w-0 swt:items-end swt:gap-1 swt:border-b swt:border-base-300 swt:px-3 swt:pt-1"
                                    tabBarMargins
                                ]
                                prop.children [
                                    for index, groupingValue in List.indexed tabs do
                                        let category = groupingValue.Key.Header.Category.Name

                                        let valueText =
                                            Formatting.formatValue groupingValue.Value groupingValue.Unit

                                        GroupCard.OrganizerTab(
                                            category,
                                            valueText,
                                            tabPalette.[index % tabPalette.Length],
                                            (hoveredTabIndex = Some index),
                                            (fun highlighted ->
                                                setHoveredTabIndex (
                                                    if highlighted then Some index else None
                                                )),
                                            ?testId =
                                                (if defaultArg debug false then
                                                     Some $"provenance-group-tab-{side}-{group.Id}-{index}"
                                                 else
                                                     None),
                                            key = $"{index}:{category}={valueText}"
                                        )
                                ]
                            ]
                            Html.div [
                                prop.className "swt:flex swt:items-center swt:gap-1"
                                prop.children [
                                    // The expand trigger is drawn as a folder: a clipped back
                                    // panel with its own index tab, the members' type symbols
                                    // resting inside, and a front pocket they tuck behind.
                                    Html.button [
                                        prop.type'.button
                                        prop.ariaLabel "Show members"
                                        prop.title "Show members"
                                        prop.onClick (fun _ -> onExpand ())
                                        prop.className "swt:group swt:relative swt:min-w-0 swt:grow swt:cursor-pointer"
                                        prop.children [
                                            Html.span [
                                                prop.ariaHidden true
                                                prop.className
                                                    "swt:absolute swt:inset-0 swt:rounded-md swt:bg-base-300 swt:transition-colors swt:group-hover:bg-primary/30 swt:[clip-path:polygon(0_0,2.5rem_0,3.25rem_0.5rem,100%_0.5rem,100%_100%,0_100%)]"
                                            ]
                                            Html.span [
                                                prop.ariaHidden true
                                                prop.className
                                                    "swt:relative swt:flex swt:min-h-9 swt:items-center swt:gap-1 swt:px-3 swt:pb-2.5 swt:pt-3.5 swt:text-base-content/80"
                                                if defaultArg debug false then
                                                    prop.testId $"provenance-group-symbols-{side}-{group.Id}"
                                                prop.children [
                                                    match memberDescriptors with
                                                    | [] -> countLabel visibleMembers.Length
                                                    | descriptors when descriptors.Length <= maxInlineSymbols ->
                                                        for index, descriptor in List.indexed descriptors do
                                                            Html.span [
                                                                prop.key (string index)
                                                                prop.children [ symbolIcon descriptor ]
                                                            ]
                                                    | descriptors ->
                                                        let dominant =
                                                            descriptors
                                                            |> List.countBy (fun descriptor -> descriptor.Label)
                                                            |> List.maxBy snd
                                                            |> fst

                                                        let icon =
                                                            descriptors
                                                            |> List.find (fun descriptor ->
                                                                descriptor.Label = dominant)

                                                        symbolIcon icon
                                                        countLabel descriptors.Length
                                                ]
                                            ]
                                            Html.span [
                                                prop.ariaHidden true
                                                prop.className
                                                    "swt:absolute swt:inset-x-0 swt:bottom-0 swt:h-[35%] swt:rounded-b-md swt:bg-base-200 swt:transition-colors swt:group-hover:bg-primary/20"
                                            ]
                                        ]
                                    ]
                                    connectionBadge
                                    selectButton
                                ]
                            ]
                        ]
                    ]
                if expanded then
                    Html.ul [
                        prop.className [
                            "swt:space-y-1 swt:border-t swt:border-base-300 swt:pt-2"
                            match density with
                            | Density.EditorDensity.Compact -> "swt:text-xs"
                            | _ -> "swt:text-sm"
                        ]
                        prop.children [
                            for member' in group.Members do
                                let memberValues = GroupCardData.memberValues member' model
                                let isHovered = hoveredMemberId = Some member'.SetId
                                let memberHandle : ConnectionHandleRef =
                                    {
                                        Kind = ConnectionHandleKind.GroupMember
                                        Side = side
                                        Id = member'.SetId
                                        ParentGroupId = Some group.Id
                                    }

                                Html.li [
                                    prop.className "swt:relative"
                                    prop.children [
                                        Html.div [
                                            prop.className "swt:flex swt:items-center swt:gap-2"
                                            prop.children [
                                                if side = ProvenanceSide.Output then
                                                    Controls.ConnectionHandle(
                                                        memberHandle,
                                                        label = $"Connect {member'.Name}",
                                                        ?debug = debug
                                                    )
                                                Html.div [
                                                    prop.tabIndex 0
                                                    prop.ariaLabel $"Show values for {member'.Name}"
                                                    prop.className "swt:flex swt:min-w-0 swt:grow swt:flex-col swt:gap-0.5 swt:rounded-md swt:px-2 swt:py-1 swt:outline-none swt:transition-colors hover:swt:bg-base-200 focus:swt:bg-base-200 focus:swt:ring-2 focus:swt:ring-primary/40"
                                                    if defaultArg debug false then
                                                        prop.testId $"provenance-group-member-{side}-{member'.SetId}"
                                                    prop.onMouseEnter (fun _ -> setHoveredMemberId (Some member'.SetId))
                                                    prop.onMouseLeave (fun _ -> setHoveredMemberId None)
                                                    prop.onFocus (fun _ -> setHoveredMemberId (Some member'.SetId))
                                                    prop.onBlur (fun _ -> setHoveredMemberId None)
                                                    prop.children [
                                                        match GroupCardData.endpointKind side model member'.SetId with
                                                        | Some kind -> EntityType.line (EntityType.descriptor kind)
                                                        | None -> Html.none
                                                        Html.span [
                                                            prop.className "swt:min-w-0 swt:truncate"
                                                            prop.text member'.Name
                                                        ]
                                                    ]
                                                ]
                                                if side = ProvenanceSide.Input then
                                                    Controls.ConnectionHandle(
                                                        memberHandle,
                                                        label = $"Connect {member'.Name}",
                                                        ?debug = debug
                                                    )
                                            ]
                                        ]

                                        if isHovered then
                                            Html.div [
                                                prop.className [
                                                    // The viewport cap keeps the popover readable when the
                                                    // editor itself is narrower than the preferred width.
                                                    "swt:absolute swt:top-0 swt:z-30 swt:w-72 swt:max-w-[60vw] swt:rounded-md swt:border swt:border-base-300 swt:bg-base-100 swt:p-2 swt:shadow-lg"
                                                    memberDetailsPosition
                                                ]
                                                if defaultArg debug false then
                                                    prop.testId $"provenance-member-values-{side}-{member'.SetId}"
                                                prop.children [
                                                    if memberValues.IsEmpty then
                                                        Html.p [
                                                            prop.className "swt:text-xs swt:text-base-content/60"
                                                            prop.text "No property values"
                                                        ]
                                                    else
                                                        Html.div [
                                                            prop.className "swt:flex swt:flex-wrap swt:gap-1"
                                                            prop.children [
                                                                for value in memberValues do
                                                                    Controls.ValueLabel(value, key = $"member:{member'.SetId}:{DragDrop.propertyValueIdentity value}")
                                                            ]
                                                        ]
                                                ]
                                            ]
                                    ]
                                ]
                        ]
                    ]
            ]
        ]
