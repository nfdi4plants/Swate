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

module React =
    open Fable.Core
    open Browser.Types
    open Feliz
    open Fable.Core.JsInterop

    type ElementSize = {
        Width: float
        Height: float
    } with
        static member init() = {
            Width = 0.
            Height = 0.
        }

    [<Emit("$0?.[$1]")>]
    let (!?) (opt: 't option) (property: string) : obj = nativeOnly

    let useElementSize () =
        let initialValue : HTMLElement option = None
        let ref, setRef = React.useState(initialValue)
        // https://usehooks-ts.com/react-hook/use-element-size
        // Mutable values like 'ref.current' aren't valid dependencies
        // because mutating them doesn't re-render the component.
        // Instead, we use a state as a ref to be reactive.
        let size, setSize = React.useState(ElementSize.init())
        // Prevent too many rendering using useCallback
        let handleSize = 
            React.useCallback(
                (fun () ->
                    setSize {
                        Width = ref |> Option.map (fun r -> r.offsetWidth) |> Option.defaultValue 0.
                        Height = ref |> Option.map (fun r -> r.offsetHeight) |> Option.defaultValue 0.
                    }
                ), 
                [| !? ref "offsetWidth"; !? ref "offsetHeight" |]
            )
        React.useLayoutEffect((fun () -> handleSize()), [|!? ref "offsetWidth"; !? ref "offsetHeight"|])
        size, 
        (fun (ele: Element) -> 
            setRef (ele :?> HTMLElement |> Some)
        )

