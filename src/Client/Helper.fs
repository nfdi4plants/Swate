[<AutoOpenAttribute>]
module Helper

open Fable.Core

let log (a) = Browser.Dom.console.log a

let logf a b = 
    let txt : string = sprintf a b
    log txt

open System.Collections.Generic

let mutable memoizations = Dictionary<string, int>(HashIdentity.Structural)

let debounce<'T> (key: string) (timeout: int) (fn: 'T -> unit) value =
    let key = key // fn.ToString()
    // Cancel previous debouncer
    match memoizations.TryGetValue(key) with
    | true, timeoutId -> printfn "CLEAR"; Fable.Core.JS.clearTimeout timeoutId
    | _ -> printfn "Not clear";()

    // Create a new timeout and memoize it
    let timeoutId = 
        Fable.Core.JS.setTimeout 
            (fun () -> 
                memoizations.Remove(key) |> ignore
                fn value
            ) 
            timeout
    memoizations.[key] <- timeoutId

let debouncel<'T> (key: string) (timeout: int) (setLoading: bool -> unit) (fn: 'T -> unit) value =
    let key = key // fn.ToString()
    // Cancel previous debouncer
    match memoizations.TryGetValue(key) with
    | true, timeoutId -> Fable.Core.JS.clearTimeout timeoutId
    | _ -> setLoading true; ()

    // Create a new timeout and memoize it
    let timeoutId = 
        Fable.Core.JS.setTimeout 
            (fun () -> 
                memoizations.Remove(key) |> ignore
                setLoading false
                fn value
            ) 
            timeout
    memoizations.[key] <- timeoutId