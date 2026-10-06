namespace Swate.Components.Composite.SortableList

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Types

[<Erase; Mangle(false)>]
type SortableList =
    [<ReactComponent>]
    static member private Row
        (
            item: SortableListItem<'A>,
            index: int,
            count: int,
            move: int -> int -> unit,
            remove: string -> unit,
            ?renderRow: SortableListRowRender<'A> -> ReactElement,
            ?rowProps: SortableListItem<'A> -> IReactProperty list,
            ?key: string
        ) =
        let sortable = DndKit.useSortable {| id = item.id |}

        let properties =
            rowProps |> Option.map (fun getProps -> getProps item) |> Option.defaultValue []

        let customStyle =
            properties
            |> List.rev
            |> List.tryPick (fun property ->
                let name, value = unbox<string * obj> property
                if name = "style" then Some value else None
            )

        let rowStyle =
            Object.merge (defaultArg customStyle null) {|
                transform = DndKit.CSS.Transform.toString sortable.transform
                transition = sortable.transition
            |}

        let row: SortableListRowRender<'A> = {|
            item = item
            index = index
            isFirst = index = 0
            isLast = index = count - 1
            dragHandleProps = [
                yield! prop.spread sortable.attributes
                yield! prop.spread sortable.listeners
            ]
            moveUp = fun () -> move index (index - 1)
            moveDown = fun () -> move index (index + 1)
            remove = fun () -> remove item.id
        |}

        Html.tr [
            yield! properties
            prop.testId $"sortable-list-row-{item.id}"
            prop.ref sortable.setNodeRef
            prop.style rowStyle
            prop.children [
                match renderRow with
                | Some render -> render row
                | None -> RowComponents.DefaultRow(row)
            ]
        ]

    /// The parent owns items. Reorders and removals emit a new array without mutating the input.
    /// renderRow supplies table cells; rowProps customizes the surrounding sortable row.
    /// Inline row styles are preserved, with transform and transition reserved for sorting.
    [<ReactComponent(true)>]
    static member SortableList
        (
            items: SortableListItem<'A>[],
            onItemsChange: SortableListItem<'A>[] -> unit,
            ?renderRow: SortableListRowRender<'A> -> ReactElement,
            ?rowProps: SortableListItem<'A> -> IReactProperty list,
            ?className: string
        ) =
        let move oldIndex newIndex =
            if
                oldIndex <> newIndex
                && oldIndex >= 0
                && newIndex >= 0
                && oldIndex < items.Length
                && newIndex < items.Length
            then
                DndKit.arrayMove (ResizeArray items, oldIndex, newIndex)
                |> Seq.toArray
                |> onItemsChange

        let remove id =
            items |> Array.filter (fun item -> item.id <> id) |> onItemsChange

        let pointerSensor =
            DndKit.useSensor (
                DndKit.PointerSensor,
                {|
                    activationConstraint = {| distance = 6 |}
                |}
            )

        let sensors = DndKit.useSensors [| pointerSensor |]

        let handleDragEnd (event: DndKit.IDndKitEvent) =
            if not (isNull event.over) then
                let oldIndex =
                    items |> Array.tryFindIndex (fun item -> item.id = string event.active.id)

                let newIndex =
                    items |> Array.tryFindIndex (fun item -> item.id = string event.over.id)

                match oldIndex, newIndex with
                | Some oldIndex, Some newIndex -> move oldIndex newIndex
                | _ -> ()

        let itemIds =
            React.useMemo ((fun () -> ResizeArray(items |> Array.map _.id)), [| box items |])

        DndKit.DndContext(
            sensors = sensors,
            collisionDetection = DndKit.pointerWithin,
            onDragEnd = handleDragEnd,
            accessibility = {|
                screenReaderInstructions = {|
                    draggable =
                        "Drag to reorder, or use the Move up and Move down buttons. Use the Remove button to remove an item."
                |}
            |},
            children =
                DndKit.SortableContext(
                    items = itemIds,
                    strategy = DndKit.verticalListSortingStrategy,
                    children =
                        Html.div [
                            prop.testId "sortable-list"
                            prop.className [
                                "swt:overflow-visible swt:border swt:border-base-300 swt:rounded-box"
                                defaultArg className ""
                            ]
                            prop.children [
                                Html.table [
                                    prop.className "swt:table swt:table-xs swt:table-fixed swt:min-w-full"
                                    prop.children [
                                        Html.tbody [
                                            match items with
                                            | [||] ->
                                                Html.tr [
                                                    Html.td [
                                                        prop.colSpan 3
                                                        prop.children [
                                                            Html.div [
                                                                prop.className "swt:text-center swt:opacity-50"
                                                                prop.text "No items available"
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            | _ ->
                                                for index in 0 .. items.Length - 1 do
                                                    let item = items.[index]

                                                    SortableList.Row(
                                                        item,
                                                        index,
                                                        items.Length,
                                                        move,
                                                        remove,
                                                        ?renderRow = renderRow,
                                                        ?rowProps = rowProps,
                                                        key = item.id
                                                    )
                                        ]
                                    ]
                                ]
                            ]
                        ]
                )
        )
