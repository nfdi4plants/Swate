[<AutoOpenAttribute>]
module Helper

open Fable.Core

let log (a) = Browser.Dom.console.log a

let logw (a) = Browser.Dom.console.warn a

let logf a b = 
    let txt : string = sprintf a b
    log txt

open System.Collections.Generic

type DebounceStorage() =
    let mutable _storage = Dictionary<string, int>(HashIdentity.Structural)
    let mutable _fnStorage = Dictionary<string, unit -> unit>()

    member this.Add(key, timeoutId, ?fn: unit -> unit) =
        _storage.[key] <- timeoutId
        if fn.IsSome then
            _fnStorage.[key] <- fn.Value

    member this.TryGetValue(key) =
        match _storage.TryGetValue(key) with
        | true, timeoutId -> Some timeoutId
        | _ -> None

    member this.Remove(key) =
        _storage.Remove(key) |> ignore
        _fnStorage.Remove(key) |> ignore

    member this.ClearAndRun() =
        for kv in _storage do
            Fable.Core.JS.clearTimeout kv.Value
            match _fnStorage.TryGetValue kv.Key with
            | true, fn -> fn ()
            | _ -> ()
        _storage.Clear()
        _fnStorage.Clear()

    member this.Clear() =
        for kv in _storage do
            Fable.Core.JS.clearTimeout kv.Value
        _storage.Clear()
        _fnStorage.Clear()

let debounce<'T> (storage:DebounceStorage) (key: string) (timeout: int) (fn: 'T -> unit) value =
    let key = key // fn.ToString()
    // Cancel previous debouncer
    match storage.TryGetValue(key) with
    | Some timeoutId -> Fable.Core.JS.clearTimeout timeoutId
    | _ -> ()

    // Create a new timeout and memoize it
    let timeoutId = 
        Fable.Core.JS.setTimeout 
            (fun () -> 
                storage.Remove(key) |> ignore
                fn value
            ) 
            timeout
    storage.Add(key, timeoutId, fun () -> fn value)

let debouncel<'T> (storage:DebounceStorage) (key: string) (timeout: int) (setLoading: bool -> unit) (fn: 'T -> unit) value =
    let key = key // fn.ToString()
    // Cancel previous debouncer
    match storage.TryGetValue(key) with
    | Some timeoutId -> Fable.Core.JS.clearTimeout timeoutId
    | _ -> setLoading true; ()

    // Create a new timeout and memoize it
    let timeoutId = 
        Fable.Core.JS.setTimeout 
            (fun () -> 
                match storage.TryGetValue key with
                | Some _ ->
                    storage.Remove(key) |> ignore
                    setLoading false
                    fn value
                | None ->
                    setLoading false
            ) 
            timeout
    storage.Add(key, timeoutId, fun () -> fn value)

let newDebounceStorage = fun () -> DebounceStorage()

let debouncemin (fn: 'a -> unit, timeout: int) =
    let mutable id = null: string
    fun (arg: 'a) ->
        match id with
        | null -> ()
        | id -> Fable.Core.JS.clearTimeout (int id)
        let timeoutId = 
            Fable.Core.JS.setTimeout
                (fun () ->
                    fn arg
                )
                timeout
        id <- string timeoutId

let throttle (fn: 'a -> unit, interval: int) =
    let mutable lastCall = System.DateTime.MinValue

    fun (arg: 'a) ->
        let now = System.DateTime.UtcNow
        if (now - lastCall).TotalMilliseconds > float interval then
            lastCall <- now
            fn arg

let throttleAndDebounce(fn: 'a -> unit, timespan: int) =
    let throttleFn = throttle(fn, timespan)
    let debounceFn = debouncemin(fn, timespan)

    fun (arg: 'a) ->
        throttleFn arg
        debounceFn arg


type Clipboard =
    abstract member writeText: string -> JS.Promise<unit>
    abstract member readText: unit -> JS.Promise<string>

type Navigator =
    abstract member clipboard: Clipboard

[<Emit("navigator")>]
let navigator : Navigator = jsNative

/// <summary>
/// take "count" many items from array if existing. if not enough items return as many as possible
/// </summary>
/// <param name="count"></param>
/// <param name="array"></param>
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