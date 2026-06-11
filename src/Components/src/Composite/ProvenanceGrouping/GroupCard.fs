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

/// Derives display text and property chips for one provenance group card.
module private GroupCardData =

    let values (group: DisplayGroup) (model: ProvenanceModel) =
        group.Members
        |> List.collect (fun member' -> member'.PropertyValueIds)
        |> List.distinct
        |> List.choose (fun id -> model.PropertyValues.TryFind id)

    let memberValues (member': DisplayMember) (model: ProvenanceModel) =
        member'.PropertyValueIds
        |> List.distinct
        |> List.choose (fun id -> model.PropertyValues.TryFind id)

    let title (group: DisplayGroup) =
        match group.GroupingValues with
        | [] -> group.Members.Head.Name
        | values ->
            values
            |> List.groupBy (fun value -> value.Key)
            |> List.sortBy (fun (key, _) -> $"{key.Header.Kind.Id}:{key.Header.Category.Name}")
            |> List.map (fun (key, groupedValues) ->
                let valuesText =
                    groupedValues
                    |> List.sortBy (fun value -> Formatting.formatValue value.Value value.Unit)
                    |> List.map (fun value -> Formatting.formatValue value.Value value.Unit)
                    |> String.concat " | "

                $"{key.Header.Category.Name}: {valuesText}"
            )
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
                Controls.ConnectionHandle(
                    groupHandle,
                    label = "Connect group",
                    className = groupHandleEdge,
                    ?debug = debug
                )
                Html.div [
                    prop.className "swt:flex swt:items-start swt:gap-2"
                    prop.children [
                        Html.h3 [
                            prop.className "swt:grow swt:min-w-0 swt:truncate swt:text-sm swt:font-semibold"
                            prop.title title
                            prop.text title
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
                            // The member count doubles as the expand trigger, replacing a
                            // generic info icon without taking extra header space.
                            Buttons.QuickAccessButton(
                                Html.span [
                                    prop.className "swt:badge swt:badge-ghost swt:badge-sm swt:font-semibold"
                                    prop.text $"×{group.Members.Length}"
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
                                                    prop.className "swt:min-w-0 swt:grow swt:truncate swt:rounded-md swt:px-2 swt:py-1 swt:outline-none swt:transition-colors hover:swt:bg-base-200 focus:swt:bg-base-200 focus:swt:ring-2 focus:swt:ring-primary/40"
                                                    if defaultArg debug false then
                                                        prop.testId $"provenance-group-member-{side}-{member'.SetId}"
                                                    prop.onMouseEnter (fun _ -> setHoveredMemberId (Some member'.SetId))
                                                    prop.onMouseLeave (fun _ -> setHoveredMemberId None)
                                                    prop.onFocus (fun _ -> setHoveredMemberId (Some member'.SetId))
                                                    prop.onBlur (fun _ -> setHoveredMemberId None)
                                                    prop.text member'.Name
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
