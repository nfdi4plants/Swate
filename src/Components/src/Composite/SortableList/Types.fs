module Swate.Components.Composite.SortableList.Types

open Feliz

/// IDs must be unique and stable within a list, including when labels change.
type SortableListItem<'A> = {|
    id: string
    label: string
    data: 'A option
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
