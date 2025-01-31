namespace Swate.Components

open Fable.Core
open Feliz

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
type TermSearchStyle
    [<ParamObjectAttribute; Emit("$0")>]
    (?inputLabel: U2<string, ResizeArray<string>>) =
    member val inputLabel: U2<string, ResizeArray<string>> option = jsNative with get, set

module TermSearchStyle =
    let resolveStyle (style: U2<string, ResizeArray<string>>) =
        match style with
        | U2.Case1 className -> className
        | U2.Case2 classNames -> classNames |> String.concat " "


type AdvancedSearchController = {|
    startSearch: unit -> unit
    cancel: unit -> unit
|}

type AdvancedSearch = {|
    search: unit -> JS.Promise<ResizeArray<Term>>
    form: AdvancedSearchController -> ReactElement
|}