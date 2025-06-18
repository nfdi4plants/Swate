[<AutoOpenAttribute>]
module Swate.Components.Types

open Fable.Core
open Feliz

type DaisyUIColors =
    | Primary
    | Secondary
    | Accent
    | Info
    | Success
    | Warning
    | Error

type Context<'T> = { data: 'T; setData: 'T -> unit }

[<StringEnum(Fable.Core.CaseRules.LowerFirst)>]
type Theme =
    | Auto
    | Sunrise
    | Finster
    | Planti
    | Viola

module Theme =
    let toString (theme: Theme) =
        match theme with
        | Auto -> "auto"
        | Sunrise -> "light"
        | Finster -> "dark"
        | Planti -> "planti"
        | Viola -> "viola"

    let fromString (theme: string) =
        match theme with
        | "auto" -> Auto
        | "light" -> Sunrise
        | "dark" -> Finster
        | "planti" -> Planti
        | "viola" -> Viola
        | _ -> Auto // Default to Auto if the string does not match any known theme

type CellCoordinate = {| x: int; y: int |}

type CellCoordinateRange = {|
    yStart: int
    yEnd: int
    xStart: int
    xEnd: int
|}

module CellCoordinateRange =

    let count (range: CellCoordinateRange) : int =
        (range.yEnd - range.yStart + 1) * (range.xEnd - range.xStart + 1)

    let toArray (range: CellCoordinateRange) : ResizeArray<CellCoordinate> =
        let result = ResizeArray<CellCoordinate>()

        for y in range.yStart .. range.yEnd do
            for x in range.xStart .. range.xEnd do
                result.Add({| x = x; y = y |})

        result

// [<AllowNullLiteral>]
// [<Global>]
type TableCellController = {
    Index: CellCoordinate
    IsActive: bool
    IsSelected: bool
    IsOrigin: bool
    onKeyDown: Browser.Types.KeyboardEvent -> unit
    onBlur: Browser.Types.FocusEvent -> unit
    onClick: Browser.Types.MouseEvent -> unit
} with

    static member init(index, isActive, isSelected, isOrigin, onKeyDown, onBlur, onClick) = {
        Index = index
        IsActive = isActive
        IsSelected = isSelected
        IsOrigin = isOrigin
        onKeyDown = onKeyDown
        onBlur = onBlur
        onClick = onClick
    }

[<AllowNullLiteral>]
[<Global>]
type SelectHandle
    [<ParamObjectAttribute; Emit("$0")>]
    (
        contains: CellCoordinate -> bool,
        selectAt: (CellCoordinate * bool) -> unit,
        clear: unit -> unit,
        getSelectedCellRange,
        getSelectedCells,
        getCount
    ) =
    member val contains: CellCoordinate -> bool = contains with get, set
    member val selectAt: (CellCoordinate * bool) -> unit = selectAt with get, set
    member val clear: unit -> unit = clear with get, set
    member val getSelectedCellRange: unit -> CellCoordinateRange option = getSelectedCellRange with get, set
    member val getSelectedCells: unit -> ResizeArray<CellCoordinate> = getSelectedCells with get, set
    member val getCount: unit -> int = getCount with get, set

[<AllowNullLiteral>]
[<Global>]
type TableHandle
    [<ParamObjectAttribute; Emit("$0")>]
    (focus: unit -> unit, scrollTo: CellCoordinate -> unit, SelectHandle: SelectHandle) =
    member val focus: unit -> unit = focus with get, set
    member val scrollTo: CellCoordinate -> unit = scrollTo with get, set
    member val SelectHandle: SelectHandle = SelectHandle with get, set

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

    module ConvertLiterals =
        [<Literal>]
        let Description = "description"

        [<Literal>]
        let Data = "data"

        [<Literal>]
        let IsObsolete = "isObsolete"

    open Swate.Components.Shared
    open Fable.SimpleJson

    open ARCtrl

    let toOntologyAnnotation (term: Term) =
        let comments =
            ResizeArray [
                if term.description.IsSome then
                    Comment(ConvertLiterals.Description, JS.JSON.stringify term.description.Value)
                if term.data.IsSome then
                    Comment(ConvertLiterals.Data, JS.JSON.stringify term.data.Value)
                if term.isObsolete.IsSome then
                    Comment(ConvertLiterals.IsObsolete, JS.JSON.stringify term.isObsolete.Value)
            ]
            |> Option.whereNot Seq.isEmpty

        ARCtrl.OntologyAnnotation(?name = term.name, ?tsr = term.source, ?tan = term.id, ?comments = comments)

    let fromOntologyAnnotation (oa: ARCtrl.OntologyAnnotation) =
        let description =
            oa.Comments
            |> Seq.tryFind (fun c -> c.Name = Some ConvertLiterals.Description)
            |> Option.map (fun c -> Json.parseAs<string> (c.Value.Value))

        let data =
            oa.Comments
            |> Seq.tryFind (fun c -> c.Name = Some ConvertLiterals.Data)
            |> Option.map (fun c -> Json.parseAs<obj> (c.Value.Value))

        let isObsolete =
            oa.Comments
            |> Seq.tryFind (fun c -> c.Name = Some ConvertLiterals.IsObsolete)
            |> Option.map (fun c -> Json.parseAs<bool> (c.Value.Value))

        Term(
            ?name = oa.Name,
            ?id = oa.TermAccessionNumber,
            ?description = description,
            ?source = oa.TermSourceREF,
            ?href = Option.whereNot System.String.IsNullOrWhiteSpace oa.TermAccessionOntobeeUrl,
            ?isObsolete = isObsolete,
            ?data = data
        )

[<AllowNullLiteral>]
[<Global>]
type TermSearchStyle [<ParamObjectAttribute; Emit("$0")>] (?inputLabel: U2<string, ResizeArray<string>>) =
    member val inputLabel: U2<string, ResizeArray<string>> option = jsNative with get, set

module TermSearchStyle =
    let resolveStyle (style: U2<string, ResizeArray<string>>) =
        match style with
        | U2.Case1 className -> className
        | U2.Case2 classNames -> classNames |> String.concat " "

[<AllowNullLiteral>]
[<Global>]
type AdvancedSearchController [<ParamObjectAttribute; Emit("$0")>] (startSearch: unit -> unit, cancel: unit -> unit) =
    member val startSearch: unit -> unit = jsNative with get, set
    member val cancel: unit -> unit = jsNative with get, set

[<AllowNullLiteral>]
[<Global>]
type AdvancedSearch
    [<ParamObjectAttribute; Emit("$0")>]
    (search: unit -> JS.Promise<ResizeArray<Term>>, form: AdvancedSearchController -> ReactElement) =
    member val search: unit -> JS.Promise<ResizeArray<Term>> = jsNative with get, set
    member val form: AdvancedSearchController -> ReactElement = jsNative with get, set

[<AllowNullLiteral>]
[<Global>]
type PortalTermDropdown
    [<ParamObjectAttribute; Emit("$0")>]
    (portal: Browser.Types.HTMLElement, renderer: Browser.Types.ClientRect -> ReactElement -> Fable.React.ReactElement)
    =
    member val portal = portal with get, set
    member val renderer = renderer with get, set

///
/// A search function that resolves a list of terms.
/// @typedef {function(string): Promise<Term[]>} SearchCall
///
type SearchCall = string -> JS.Promise<ResizeArray<Term>>

//
// A parent search function that resolves a list of terms based on a parent ID and query.
// @typedef {function(string, string): Promise<Term[]>} ParentSearchCall
//
type ParentSearchCall = (string * string) -> JS.Promise<ResizeArray<Term>>

///
/// A function that fetches all child terms of a parent.
/// @typedef {function(string): Promise<Term[]>} AllChildrenSearchCall
///
type AllChildrenSearchCall = string -> JS.Promise<ResizeArray<Term>>

module AnnotationTableContextMenu =

    open ARCtrl

    type PasteCases =
        | AddColumns of
            {|
                data: ResizeArray<CompositeColumn>
                columnIndex: int
            |}
        | PasteColumns of
            {|
                data: CompositeCell[][]
                coordinates: CellCoordinate [][]
            |}

module AnnotationTable =

    open AnnotationTableContextMenu

    [<RequireQualifiedAccess>]
    type ModalTypes =
        | Details of CellCoordinate
        | PasteCaseUserInput of PasteCases
        /// 👀 Uses CellCoordinate to identify if clicked cell is part of selected range
        | MoveColumn of uiTableIndex: CellCoordinate * arcTableIndex: CellCoordinate
        | Error of string
        | None