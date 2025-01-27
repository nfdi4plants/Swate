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

type AdvancedSearchController = {|
    startSearch: unit -> unit
    cancel: unit -> unit
|}

type AdvancedSearch = {|
    search: unit -> JS.Promise<ResizeArray<Term>>
    form: AdvancedSearchController -> ReactElement
|}