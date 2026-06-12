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

    /// One (category, joined values) pair per grouping category, rendered as chips.
    let chips (group: DisplayGroup) =
        group.GroupingValues
        |> List.groupBy (fun value -> value.Key)
        |> List.sortBy (fun (key, _) -> $"{key.Header.Kind.Id}:{key.Header.Category.Name}")
        |> List.map (fun (key, groupedValues) ->
            let valuesText =
                groupedValues
                |> List.sortBy (fun value -> Formatting.formatValue value.Value value.Unit)
                |> List.map (fun value -> Formatting.formatValue value.Value value.Unit)
                |> String.concat " | "

            key.Header.Category.Name, valuesText)

    let title (group: DisplayGroup) =
        match chips group with
        | [] -> group.Members.Head.Name
        | chips ->
            chips
            |> List.map (fun (category, valuesText) -> $"{category}: {valuesText}")
            |> String.concat ", "

[<Erase; Mangle(false)>]
type GroupCard =

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
            ?connectionCount: int,
            ?debug: bool,
            ?key: string
        ) =
        let hoveredMemberId, setHoveredMemberId = React.useState<ProvenanceSetId option> None
        let density = React.useContext Density.context

        let droppable =
            DndKit.useDroppable (
                {|
                    id = DragDrop.groupDropId side group.Id
                |}
            )

        let title = GroupCardData.title group
        let isGroup = group.Members.Length > 1

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
                if droppable.isOver then
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
                Html.div [
                    prop.className "swt:flex swt:items-start swt:gap-2"
                    prop.children [
                        match GroupCardData.chips group with
                        | [] ->
                            // A chip-less card is always a single loaded endpoint, so the type
                            // line sits above its name to mirror the expanded member rows.
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
                        | chips ->
                            // Chips show only the grouping values; the category lives in
                            // the tooltip and is already visible on the connected rail
                            // property, so the card stays narrow without losing it.
                            Html.div [
                                prop.className "swt:flex swt:min-w-0 swt:grow swt:flex-wrap swt:items-center swt:gap-1"
                                prop.title title
                                prop.ariaLabel title
                                prop.children [
                                    for category, valuesText in chips do
                                        Html.span [
                                            prop.key category
                                            prop.className
                                                "swt:badge swt:badge-outline swt:badge-sm swt:max-w-full swt:truncate swt:font-medium"
                                            prop.title $"{category}: {valuesText}"
                                            prop.text valuesText
                                        ]
                                ]
                            ]
                        // Stacked layouts hide the connector overlay; this badge keeps
                        // the connection information visible on the card itself.
                        match connectionCount with
                        | Some count when count > 0 ->
                            Html.span [
                                prop.className "swt:badge swt:badge-primary swt:badge-sm swt:shrink-0"
                                prop.title $"{count} connections"
                                prop.ariaLabel $"{count} connections"
                                prop.text $"⇄ {count}"
                            ]
                        | _ -> Html.none
                        Buttons.QuickAccessButton(
                            Html.i [
                                prop.className "swt:iconify swt:fluent--checkmark-circle-20-regular swt:size-4"
                            ],
                            "Select group",
                            (fun _ -> onSelect ())
                        )
                        if isGroup then
                            // The collapsed group is drawn as a folder holding its members' type
                            // symbols. When few enough to fit, every member contributes one symbol
                            // side by side; otherwise it collapses to the dominant type symbol with a
                            // count. The preview is aria-hidden; the button keeps its "Show members"
                            // name and stays the expand trigger.
                            let memberDescriptors =
                                group.Members
                                |> List.choose (fun member' ->
                                    GroupCardData.endpointKind side model member'.SetId
                                    |> Option.map EntityType.descriptor)

                            let maxInlineSymbols = 4

                            let symbolIcon (descriptor: EntityType.Descriptor) =
                                Html.i [
                                    prop.className $"swt:iconify {descriptor.Icon} swt:size-4 swt:shrink-0"
                                ]

                            let countLabel count =
                                Html.span [
                                    prop.className "swt:text-xs swt:font-semibold"
                                    prop.text (string count)
                                ]

                            Buttons.QuickAccessButton(
                                Html.span [
                                    prop.ariaHidden true
                                    prop.className "swt:inline-flex swt:items-center swt:gap-1"
                                    if defaultArg debug false then
                                        prop.testId $"provenance-group-symbols-{side}-{group.Id}"
                                    prop.children [
                                        Html.i [
                                            prop.className "swt:iconify swt:fluent--folder-20-regular swt:size-4 swt:shrink-0 swt:text-base-content/70"
                                        ]
                                        Html.span [
                                            prop.className "swt:inline-flex swt:items-center swt:gap-0.5 swt:text-base-content/70"
                                            prop.children [
                                                match memberDescriptors with
                                                | [] -> countLabel group.Members.Length
                                                | descriptors when descriptors.Length <= maxInlineSymbols ->
                                                    for index, descriptor in List.indexed descriptors do
                                                        Html.span [ prop.key (string index); prop.children [ symbolIcon descriptor ] ]
                                                | descriptors ->
                                                    let dominant =
                                                        descriptors
                                                        |> List.countBy (fun descriptor -> descriptor.Label)
                                                        |> List.maxBy snd
                                                        |> fst

                                                    let icon =
                                                        descriptors
                                                        |> List.find (fun descriptor -> descriptor.Label = dominant)

                                                    symbolIcon icon
                                                    countLabel descriptors.Length
                                            ]
                                        ]
                                    ]
                                ],
                                "Show members",
                                (fun _ -> onExpand ())
                            )
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
