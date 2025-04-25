namespace Swate.Components

open Fable.Core
open Browser.Types
open Feliz

module Virtual =

    [<Literal>]
    let ImportPath = "@tanstack/react-virtual"

    [<ImportMember(ImportPath)>]
    type Range =
        interface end

    [<ImportMember(ImportPath)>]
    type VirtualItem =
        member this.key: string = jsNative
        member this.index: int = jsNative
        member this.start: int = jsNative
        member this.``end``: int = jsNative
        member this.size: int = jsNative

    [<StringEnum(CaseRules.LowerFirst)>]
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
    type Virtualizer<'A,'B> =
        member this.getVirtualItems() : VirtualItem [] = jsNative
        member this.getVirtualIndexes(): int [] = jsNative
        member this.getTotalSize() : int = jsNative
        member this.scrollToIndex (index: int, ?options: {|align: AlignOption option; behavior: ScrollBehavior option|}) : unit = jsNative
        member this.scrollRect: {|height: int; width: int|} = jsNative
        member this.scrollOffset: int = jsNative
        member this.measureElement: IRefValue<Browser.Types.HTMLElement option> = jsNative

type Virtual =

    [<ImportMember(Virtual.ImportPath)>]
    static member defaultRangeExtractor(range: Virtual.Range) : int [] = jsNative

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
            ?rangeExtractor: Virtual.Range -> int [],
            ?debug: bool,
            ?onChange: (Virtual.Virtualizer<_,_> * bool) -> unit,
            ?horizontal: bool,
            ?paddingStart: int,
            ?paddingEnd: int,
            ?gap: int,
            ?lanes: int
        ) : Virtual.Virtualizer<obj, obj> = jsNative