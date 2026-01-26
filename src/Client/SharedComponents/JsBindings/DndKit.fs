namespace Components.JsBindings

open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types

type IObject =

    [<Emit("$0[$1]")>]
    member this.get(fieldName: string) : obj = jsNative


module Object =

    [<Emit("Object.keys($0)")>]
    let keys (o: IObject) = jsNative

module DndKit =

    type ISensor = obj

    type ITransform =
        abstract member toString: obj -> string

    type ICSS =
        abstract member Transform: ITransform

    type ISortable =
        abstract member attributes: IObject
        abstract member listeners: IObject
        abstract member setNodeRef: obj -> unit
        abstract member transform: obj
        abstract member transition: obj

    /// <summary>
    /// Not 100% sure that active and over are in fact HTMLElements. But currently we only use id.
    /// </summary>
    type IDndKitEvent =
        abstract member active: HTMLElement
        abstract member over: HTMLElement

    [<Import("closestCenter", "@dnd-kit/core")>]
    let closestCenter: obj = jsNative

    [<Import("KeyboardSensor", "@dnd-kit/core")>]
    let KeyboardSensor: obj = jsNative

    [<Import("PointerSensor", "@dnd-kit/core")>]
    let PointerSensor: obj = jsNative

    [<Import("arrayMove", "@dnd-kit/sortable")>]
    let arrayMove (items: ResizeArray<'a>, oldIndex: int, newIndex: int) : ResizeArray<'a> = jsNative

    [<Import("sortableKeyboardCoordinates", "@dnd-kit/sortable")>]
    let sortableKeyboardCoordinates: obj = jsNative

    [<Import("verticalListSortingStrategy", "@dnd-kit/sortable")>]
    let verticalListSortingStrategy: obj = jsNative


open DndKit

[<Erase>]
type DndKit =

    [<Import("CSS", "@dnd-kit/utilities")>]
    static member CSS: ICSS = jsNative

    [<Import("useSortable", "@dnd-kit/sortable")>]
    static member useSortable(props: obj) : ISortable = jsNative

    [<Import("useSensor", "@dnd-kit/core")>]
    static member useSensor(sensor, ?props) : ISensor = jsNative

    [<Import("useSensors", "@dnd-kit/core")>]
    static member useSensors([<ParamSeqAttribute>] sensors: ISensor[]) : obj = jsNative

    [<ReactComponent("DndContext", "@dnd-kit/core")>]
    static member DndContext(?onDragEnd, ?sensors, ?collisionDetection, ?children: ReactElement, ?key) =
        React.Imported()

    [<ReactComponent("SortableContext", "@dnd-kit/sortable")>]
    static member SortableContext(items: ResizeArray<string>, strategy, children: ReactElement) = React.Imported()

    [<ReactComponent("SortableContext", "@dnd-kit/sortable")>]
    static member SortableContext(items: ResizeArray<System.Guid>, strategy, children: ReactElement) = React.Imported()