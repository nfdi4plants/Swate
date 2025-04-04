namespace Swate.Components

open Fable.Core
open Fable.Core.JS
open Feliz



type CellCoordinate = {| x: int; y: int |}
type CellCoordinateRange = {| yStart: int; yEnd: int; xStart: int; xEnd: int|}

type TableCellController = {
    Index: CellCoordinate
    onKeyDown: Browser.Types.KeyboardEvent -> unit
    onBlur: Browser.Types.FocusEvent -> unit
} with
    static member init(index, onKeyDown, onBlur) =
        {
            Index = index
            onKeyDown = onKeyDown
            onBlur = onBlur
        }

[<AllowNullLiteral>]
[<Global>]
type SelectHandle
    [<ParamObjectAttribute; Emit("$0")>]
    (contains: CellCoordinate -> bool, selectAt: (CellCoordinate * bool) -> unit, clear: unit -> unit) =
    member val contains: CellCoordinate -> bool = contains with get, set
    member val selectAt: (CellCoordinate * bool) -> unit = selectAt with get, set
    member val clear: unit -> unit = clear with get, set

[<AllowNullLiteral>]
[<Global>]
type TableHandle
    [<ParamObjectAttribute; Emit("$0")>]
    (scrollTo: CellCoordinate -> unit, select: SelectHandle) =
    member val scrollTo: CellCoordinate -> unit = scrollTo with get, set
    member val select: SelectHandle = select with get, set

[<AllowNullLiteral>]
[<Global>]
type Term
    [<ParamObjectAttribute; Emit("$0")>]
    (?name: string, ?id: string, ?description: string, ?source: string, ?href: string, ?isObsolete: bool, ?data: obj) =
    member val name: string option = jsNative with get, set
    member val id: string option = jsNative with get, set
    member val description: string option = jsNative with get, set
    member val source: string option = jsNative with get, set
    member val href: string option = jsNative with get, set
    member val isObsolete: bool option = jsNative with get, set
    member val data: obj option = jsNative with get, set

[<AllowNullLiteral>]
[<Global>]
type Data
    [<ParamObjectAttribute; Emit("$0")>]
    (?name: string, ?unit: string, ?id: string, ?description: string, ?source: string, ?href: string, ?isObsolete: bool, ?data: obj) =
    member val name: string option = jsNative with get, set
    member val unit: string option = jsNative with get, set
    member val id: string option = jsNative with get, set
    member val description: string option = jsNative with get, set
    member val source: string option = jsNative with get, set
    member val href: string option = jsNative with get, set
    member val isObsolete: bool option = jsNative with get, set
    member val data: obj option = jsNative with get, set

module Term =

    [<Emit("Object.assign({}, $0, $1)")>]
    let objectMerge (obj1: obj) (obj2: obj) = jsNative

    let joinLeft (t1: Term) (t2: Term) =
        let data =
            match t1.data, t2.data with
            | Some d1, None -> Some d1
            | None, Some d2 -> Some d2
            | None, None -> None
            | Some d1, Some d2 -> objectMerge d1 d2 |> Some
        Term(
            ?name = Option.orElse t2.name t1.name,
            ?id = Option.orElse t2.id t1.id,
            ?description = Option.orElse t2.description t1.description,
            ?source = Option.orElse t2.source t1.source,
            ?href = Option.orElse t2.href t1.href,
            ?isObsolete = Option.orElse t2.isObsolete t1.isObsolete,
            ?data = data
        )

    let joinRight (t1: Term) (t2: Term) =
        let data =
            match t1.data, t2.data with
            | Some d1, None -> Some d1
            | None, Some d2 -> Some d2
            | None, None -> None
            | Some d1, Some d2 -> objectMerge d1 d2 |> Some
        Term(
            ?name = Option.orElse t1.name t2.name,
            ?id = Option.orElse t1.id t2.id,
            ?description = Option.orElse t1.description t2.description,
            ?source = Option.orElse t1.source t2.source,
            ?href = Option.orElse t1.href t2.href,
            ?isObsolete = Option.orElse t1.isObsolete t2.isObsolete,
            ?data = data
        )

[<AllowNullLiteral>]
[<Global>]
type TermSearchStyle
    [<ParamObjectAttribute; Emit("$0")>]
    (?inputLabel: U2<string, ResizeArray<string>>) =
    member val inputLabel: U2<string, ResizeArray<string>> option = jsNative with get, set

module TermSearchStyle =
    let resolveStyle (style: U2<string, ResizeArray<string>>) =
        match style with
        | U2.Case1 className -> className
        | U2.Case2 classNames -> classNames |> String.concat " "

[<AllowNullLiteral>]
[<Global>]
type AdvancedSearchController
    [<ParamObjectAttribute; Emit("$0")>]
    (startSearch: unit -> unit, cancel: unit -> unit) =
    member val startSearch: unit -> unit = jsNative with get, set
    member val cancel: unit -> unit = jsNative with get, set

[<AllowNullLiteral>]
[<Global>]
type AdvancedSearch
    [<ParamObjectAttribute; Emit("$0")>]
    (search: unit -> JS.Promise<ResizeArray<Term>>, form: AdvancedSearchController -> ReactElement) =
    member val search: unit -> JS.Promise<ResizeArray<Term>> = jsNative with get, set
    member val form: AdvancedSearchController -> ReactElement = jsNative with get, set

///
/// A search function that resolves a list of terms.
/// @typedef {function(string): Promise<Term[]>} SearchCall
///
type SearchCall = string -> JS.Promise<ResizeArray<Term>>

//
// A parent search function that resolves a list of terms based on a parent ID and query.
// @typedef {function(string, string): Promise<Term[]>} ParentSearchCall
//
type ParentSearchCall = (string*string) -> JS.Promise<ResizeArray<Term>>

///
/// A function that fetches all child terms of a parent.
/// @typedef {function(string): Promise<Term[]>} AllChildrenSearchCall
///
type AllChildrenSearchCall = string -> JS.Promise<ResizeArray<Term>>