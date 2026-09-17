module Main.Bindings.Abort

open Fable.Core

[<AllowNullLiteral>]
type IAbortSignal =
    abstract member aborted: bool
    abstract member reason: obj option

[<AllowNullLiteral>]
type IAbortController =
    abstract member signal: IAbortSignal
    abstract member abort: ?reason: obj -> unit

[<Erase>]
type AbortController =
    [<Emit("new AbortController()")>]
    static member create() : IAbortController = jsNative
