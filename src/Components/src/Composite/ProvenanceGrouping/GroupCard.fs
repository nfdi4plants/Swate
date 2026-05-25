namespace Swate.Components.Composite.ProvenanceGrouping

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Primitive.Buttons
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Composite.ProvenanceGrouping.Helper

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
            onUpdateValue: ProvenancePropertyValueId -> ProvenanceValue -> ProvenanceTerm option -> unit,
            onCreateValue: CreateLoadedPropertyValueCommand -> unit,
            ?debug: bool
        ) =
        let droppable = DndKit.useDroppable ({| id = groupDropId side group.Id |})
        let draggable = DndKit.useDraggable ({| id = groupDragId side group.Id |})
        let setNodeRef node =
            droppable.setNodeRef node
            draggable.setNodeRef node
        let values =
            group.Members
            |> List.collect (fun member' -> member'.PropertyValueIds)
            |> List.distinct
            |> List.choose (fun id -> model.PropertyValues.TryFind id)
        let title =
            match group.GroupingValues with
            | [] -> group.Members.Head.Name
            | values ->
                values
                |> List.map (fun value -> $"{value.Key.Header.Category.Name}: {formatValue value.Value value.Unit}")
                |> String.concat ", "

        Html.article [
            prop.ref setNodeRef
            prop.custom ("data-provenance-group-node", groupNodeId side group.Id)
            prop.className [
                "swt:rounded-box swt:border swt:bg-base-100 swt:p-3 swt:shadow-sm swt:space-y-2"
                if selected then "swt:border-primary swt:bg-primary/5" else "swt:border-base-300"
                if droppable.isOver then "swt:ring-2 swt:ring-primary"
                if draggable.isDragging then "swt:opacity-50"
            ]
            if defaultArg debug false then prop.testId $"provenance-group-{side}-{group.Id}"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-start swt:gap-2"
                    prop.children [
                        Html.h3 [ prop.className "swt:grow swt:font-semibold"; prop.text title ]
                        Html.button [
                            prop.type'.button
                            yield! prop.spread (!!draggable.attributes)
                            yield! prop.spread (!!draggable.listeners)
                            prop.className "swt:btn swt:btn-ghost swt:btn-square swt:btn-sm"
                            prop.ariaLabel "Connect group"
                            prop.children [ Html.i [ prop.className "swt:iconify swt:fluent--link-20-regular swt:size-4" ] ]
                        ]
                        Buttons.QuickAccessButton(
                            Html.i [ prop.className "swt:iconify swt:fluent--checkmark-circle-20-regular swt:size-4" ],
                            "Select group",
                            (fun _ -> onSelect ())
                        )
                        Buttons.QuickAccessButton(
                            Html.i [ prop.className "swt:iconify swt:fluent--info-20-regular swt:size-4" ],
                            "Show members",
                            (fun _ -> onExpand ())
                        )
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-1"
                    prop.children [
                        for value in values do
                            Controls.ValueChip(value, (fun nextValue unit -> onUpdateValue value.Id nextValue unit), ?debug = debug)
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-1 swt:border-t swt:border-base-300 swt:pt-2"
                    prop.children [
                        for header in headersForSide side model do
                            Controls.AddValuePopover(
                                targetForGroup side group,
                                header,
                                onCreateValue,
                                ?debug = debug)
                    ]
                ]
                if expanded then
                    Html.ul [
                        prop.className "swt:space-y-1 swt:border-t swt:border-base-300 swt:pt-2 swt:text-sm"
                        prop.children [
                            for member' in group.Members do
                                Html.li [ prop.text member'.Name ]
                        ]
                    ]
            ]
        ]
