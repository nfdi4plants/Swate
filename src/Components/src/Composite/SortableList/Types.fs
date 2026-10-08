module Swate.Components.Composite.SortableList.Types

open Feliz

/// IDs must be unique and stable within a list, including when labels change.
type SortableListItem<'A> = {|
    id: string
    label: string
    data: 'A option
|}

module SortableListItem =
    let create (id: string, label: string, data: 'A option) : SortableListItem<'A> = {|
        id = id
        label = label
        data = data
    |}

/// Custom renderers return table cells; SortableList owns the surrounding row.
type SortableListRowRender<'A> = {|
    item: SortableListItem<'A>
    index: int
    isFirst: bool
    isLast: bool
    dragHandleProps: IReactProperty list
    moveUp: unit -> unit
    moveDown: unit -> unit
    remove: unit -> unit
|}

module SortableListRowRender =
    let create
        (
            item: SortableListItem<'A>,
            index: int,
            isFirst: bool,
            isLast: bool,
            dragHandleProps: IReactProperty list,
            moveUp: unit -> unit,
            moveDown: unit -> unit,
            remove: unit -> unit
        ) : SortableListRowRender<'A> =
        {|
            item = item
            index = index
            isFirst = isFirst
            isLast = isLast
            dragHandleProps = dragHandleProps
            moveUp = moveUp
            moveDown = moveDown
            remove = remove
        |}
