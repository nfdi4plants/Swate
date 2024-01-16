[<RequireQualifiedAccess>]
module AsyncIterable

open Fable.Core
open Fable.Core.JsInterop

[<Emit("""{
    [Symbol.asyncIterator]() {
        return {
            next: $0,
            return: $1
        }
    }
}""")>]
let private createAsyncIterator (onNext: unit -> JS.Promise<obj>) (onCancel: unit -> obj): JS.AsyncIterable<'T> = jsNative

/// Creates AsyncIterable. See https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/for-await...of#specifications
let create (onNext: unit -> JS.Promise<'T option>): JS.AsyncIterable<'T> =
    createAsyncIterator
        (fun () ->
            onNext() |> Promise.map (function
                | Some value -> createObj ["value" ==> value; "done" ==> false]
                | None -> createObj ["done" ==> true]
            ))
        (fun () -> createObj [ "done" ==> true ])

/// Creates AsyncIterable with a cleaning function for cancellation (JS caller invokes `break` or `return` during iteration)
let createCancellable (onCancel: unit -> unit) (onNext: unit -> JS.Promise<'T option>): JS.AsyncIterable<'T> =
    createAsyncIterator
        (fun () ->
            onNext() |> Promise.map (function
                | Some value -> createObj ["value" ==> value; "done" ==> false]
                | None -> createObj ["done" ==> true]
            ))
        (fun () ->
            onCancel()
            createObj [ "done" ==> true ])

type CancellationToken() =
    member this.Cancel(): unit =
        raise !!this

/// Iterates AsyncIterable. See https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/for-await...of
let iter (action: CancellationToken -> 'T -> unit) (iterable: JS.AsyncIterable<'T>): JS.Promise<unit> =
    let token = CancellationToken()
    emitJsExpr () """(async () => {
    for await (const value of iterable) {
        try {
            action(token, value)
        } catch (err) {
            if (err instanceof token.constructor) {
                break;
            }
            throw(err);
        }
    }
})()"""
