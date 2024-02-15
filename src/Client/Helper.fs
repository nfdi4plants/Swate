[<AutoOpenAttribute>]
module Helper

open Fable.Core

let log (a) = Browser.Dom.console.log a

let logf a b = 
    let txt : string = sprintf a b
    log txt

open System.Collections.Generic

let debounce<'T> (storage:Dictionary<string, int>) (key: string) (timeout: int) (fn: 'T -> unit) value =
    let key = key // fn.ToString()
    // Cancel previous debouncer
    match storage.TryGetValue(key) with
    | true, timeoutId -> Fable.Core.JS.clearTimeout timeoutId
    | _ -> ()

    // Create a new timeout and memoize it
    let timeoutId = 
        Fable.Core.JS.setTimeout 
            (fun () -> 
                storage.Remove(key) |> ignore
                fn value
            ) 
            timeout
    storage.[key] <- timeoutId

let debouncel<'T> (storage:Dictionary<string, int>) (key: string) (timeout: int) (setLoading: bool -> unit) (fn: 'T -> unit) value =
    let key = key // fn.ToString()
    // Cancel previous debouncer
    match storage.TryGetValue(key) with
    | true, timeoutId -> Fable.Core.JS.clearTimeout timeoutId
    | _ -> setLoading true; ()

    // Create a new timeout and memoize it
    let timeoutId = 
        Fable.Core.JS.setTimeout 
            (fun () -> 
                match storage.TryGetValue key with
                | true, _ ->
                    storage.Remove(key) |> ignore
                    setLoading false
                    fn value
                | false, _ ->
                    setLoading false
            ) 
            timeout
    storage.[key] <- timeoutId

let newDebounceStorage = fun () -> Dictionary<string, int>(HashIdentity.Structural)

type Clipboard =
    abstract member writeText: string -> JS.Promise<unit>
    abstract member readText: unit -> JS.Promise<string>

type Navigator =
    abstract member clipboard: Clipboard

[<Emit("navigator")>]
let navigator : Navigator = jsNative

let takeFromArray (count: int) (array: 'a []) =
    let exit (acc: 'a list) = List.rev acc |> Array.ofList
    let rec takeRec (l2: 'a list) (acc: 'a list) index =
      if index >= count then 
        exit acc
      else
        match l2 with
        | [] -> exit acc
        | item::tail ->
          let newAcc = item::acc
          takeRec tail newAcc (index+1)

    takeRec (Array.toList array) [] 0