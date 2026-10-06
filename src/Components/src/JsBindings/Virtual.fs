namespace Swate.Components

open Fable.Core
open Browser.Types
open Feliz

// TanStack's measureElement is a React callback ref, not an IRefValue object.
type VirtualMeasureElementRef = Element option -> unit

module Virtual =

    [<Literal>]
    let ImportPath = "@tanstack/react-virtual"

    [<ImportMember(ImportPath)>]
    type Range = interface end

    [<ImportMember(ImportPath)>]
    type VirtualItem =
        member this.key: string = jsNative
        member this.index: int = jsNative
        member this.start: int = jsNative
        member this.``end``: int = jsNative
        member this.size: int = jsNative

    [<StringEnum(CaseRules.LowerFirst); Global>]
    type AlignOption =
        | Auto
        | Start
        | Center
        | End

    [<StringEnum(CaseRules.LowerFirst)>]
    type ScrollBehavior =
        | Auto
        | Smooth

    [<ImportMember(ImportPath)>]
    type Virtualizer<'A, 'B> =
        member this.getVirtualItems() : VirtualItem[] = jsNative
        member this.getVirtualIndexes() : int[] = jsNative
        member this.getTotalSize() : int = jsNative

        [<ParamObject(1)>]
        member this.scrollToIndex(index: int, ?align: AlignOption, ?behavior: ScrollBehavior) : unit = jsNative

        [<ParamObject>]
        member this.scrollToEnd(?behavior: ScrollBehavior option) : unit = jsNative

        [<ParamObject(1)>]
        member this.scrollBy(delta: int, ?behavior: ScrollBehavior option) : unit = jsNative

        [<ParamObject(1)>]
        member this.scrollToOffset(offset: int, ?align: AlignOption, ?behavior: ScrollBehavior) : unit = jsNative

        member this.scrollRect: {| height: int; width: int |} = jsNative
        member this.scrollOffset: int = jsNative
        member this.measureElement: VirtualMeasureElementRef = jsNative

[<Erase>]
type Virtual =

    [<ImportMember(Virtual.ImportPath)>]
    static member defaultRangeExtractor(range: Virtual.Range) : int[] = jsNative

    [<ImportMember(Virtual.ImportPath)>]
    [<NamedParamsAttribute>]
    static member useVirtualizer
        (
            // required
            count: int,
            getScrollElement: unit -> option<Browser.Types.HTMLElement>,
            estimateSize: int -> int,
            // optional
            ?scrollMargin: float,
            ?scrollPaddingStart: float,
            ?scrollPaddingEnd: float,
            ?overscan: int,
            ?rangeExtractor: Virtual.Range -> int[],
            ?debug: bool,
            ?onChange: (Virtual.Virtualizer<_, _> * bool) -> unit,
            ?horizontal: bool,
            ?paddingStart: int,
            ?paddingEnd: int,
            ?gap: int,
            ?lanes: int,
            ?scrollEndThreshold: int
        ) : Virtual.Virtualizer<obj, obj> =
        jsNative
