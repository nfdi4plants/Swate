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

/// Derives display text and property chips for one provenance group card.
module private GroupCardData =

    let values (group: DisplayGroup) (model: ProvenanceModel) =
        group.Members
        |> List.collect (fun member' -> member'.PropertyValueIds)
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
            ?debug: bool,
            ?key: string
        ) =
        let droppable =
            DndKit.useDroppable (
                {|
                    id = DragDrop.groupDropId side group.Id
                |}
            )

        let draggable =
            DndKit.useDraggable (
                {|
                    id = DragDrop.groupDragId side group.Id
                |}
            )

        let setNodeRef node =
            droppable.setNodeRef node
            draggable.setNodeRef node

        let values = GroupCardData.values group model
        let title = GroupCardData.title group

        Html.article [
            match key with
            | Some key -> prop.key key
            | None -> ()
            prop.ref setNodeRef
            prop.custom ("data-provenance-group-node", DragDrop.groupNodeId side group.Id)
            prop.className [
                "swt:rounded-box swt:border swt:bg-base-100 swt:p-3 swt:shadow-sm swt:space-y-2"
                if selected then
                    "swt:border-primary swt:bg-primary/5"
                else
                    "swt:border-base-300"
                if droppable.isOver then
                    "swt:ring-2 swt:ring-primary"
                yield! Styles.dragIndicatorClasses draggable.isDragging
            ]
            if defaultArg debug false then
                prop.testId $"provenance-group-{side}-{group.Id}"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-start swt:gap-2"
                    prop.children [
                        Html.h3 [
                            prop.className "swt:grow swt:font-semibold"
                            prop.text title
                        ]
                        Html.button [
                            prop.type'.button
                            yield! prop.spread (!!draggable.attributes)
                            yield! prop.spread (!!draggable.listeners)
                            prop.className [
                                "swt:btn swt:btn-ghost swt:btn-square swt:btn-sm"
                                yield! Styles.draggableButtonClasses draggable.isDragging
                            ]
                            prop.ariaLabel "Connect group"
                            prop.children [
                                Html.i [
                                    prop.className "swt:iconify swt:fluent--link-20-regular swt:size-4"
                                ]
                            ]
                        ]
                        Buttons.QuickAccessButton(
                            Html.i [
                                prop.className "swt:iconify swt:fluent--checkmark-circle-20-regular swt:size-4"
                            ],
                            "Select group",
                            (fun _ -> onSelect ())
                        )
                        Buttons.QuickAccessButton(
                            Html.i [
                                prop.className "swt:iconify swt:fluent--info-20-regular swt:size-4"
                            ],
                            "Show members",
                            (fun _ -> onExpand ())
                        )
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-1"
                    prop.children [
                        for value in values do
                            Controls.ValueLabel(value, ?debug = debug, key = DragDrop.propertyValueIdentity value)
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
