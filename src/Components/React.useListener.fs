[<AutoOpenAttribute>]
module ReactHelper

// https://github.com/Shmew/Feliz.UseListener/blob/master/src/Feliz.UseListener/Listener.fs

open Browser.Types
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Fable.Core
open Feliz
open System.ComponentModel

[<EditorBrowsable(EditorBrowsableState.Never)>]
module Impl =
    [<Emit("typeof window !== 'undefined'")>]
    let isWindowDefined () : bool = jsNative

    [<Emit("typeof window.addEventListener === 'function'")>]
    let isWindowListenerFunction () : bool = jsNative

    [<Emit("Object.defineProperty({}, 'passive', {get () { $0() }})")>]
    let definePassive (updater: unit -> unit) : JS.PropertyDescriptor = jsNative

    let allowsPassiveEvents =
        let mutable passive = false

        try
            if isWindowDefined() && isWindowListenerFunction() then
                let options =
                    jsOptions<AddEventListenerOptions>(fun o ->
                        o.passive <- true
                    )

                window.addEventListener("testPassiveEventSupport", ignore, options)
                window.removeEventListener("testPassiveEventSupport", ignore)
        with _ -> ()

        passive

    let defaultPassive = jsOptions<AddEventListenerOptions>(fun o -> o.passive <- true)

    let adjustPassive (maybeOptions: AddEventListenerOptions option) =
        maybeOptions
        |> Option.map (fun options ->
            if options.passive && not allowsPassiveEvents then
                jsOptions<AddEventListenerOptions>(fun o ->
                    o.capture <- options.capture
                    o.once <- options.once
                    o.passive <- false
                )
            else options)

    let createRemoveOptions (maybeOptions: AddEventListenerOptions option) =
        maybeOptions
        |> Option.bind (fun options ->
            if options.capture then
                Some (jsOptions<RemoveEventListenerOptions>(fun o -> o.capture <- true))
            else None)

type React =

    static member inline useDebouncedCallback<'A>(func: 'A -> unit, ?delay: int) =
        let timeout = React.useRef(None)
        let delay = defaultArg delay 500

        React.useCallback(
            (fun (arg: 'A) ->

                let later = fun () ->
                    timeout.current |> Option.iter(Fable.Core.JS.clearTimeout)
                    func arg

                timeout.current |> Option.iter(Fable.Core.JS.clearTimeout)
                timeout.current <- Some(Fable.Core.JS.setTimeout later delay)
            ),
            [| func; delay |]
        )

    static member inline useDebouncedCallbackWithCancel<'A>(func: 'A -> unit, ?delay: int, ?oncancel: unit -> unit, ?ondebouncestart: unit -> unit, ?ondebouncerun: unit -> unit) =
        let timeout = React.useRef(None)
        let delay = defaultArg delay 500

        let debouncedCallBack = React.useCallback(
            (fun (arg: 'A) ->

                let later = fun () ->
                    timeout.current |> Option.iter(Fable.Core.JS.clearTimeout)
                    ondebouncerun |> Option.iter(fun f -> f())
                    func arg

                ondebouncestart |> Option.iter(fun f -> f())
                timeout.current |> Option.iter(Fable.Core.JS.clearTimeout)
                timeout.current <- Some(Fable.Core.JS.setTimeout later delay)
            ),
            [| func; delay |]
        )
        let cancel = React.useCallback(
            (fun () ->
                if timeout.current.IsSome then
                    Fable.Core.JS.clearTimeout(timeout.current.Value)
                    oncancel |> Option.iter(fun f -> f())
            )
        )
        cancel, debouncedCallBack

[<Erase;RequireQualifiedAccess>]
module React =

    [<Erase>]
    type useListener =
        static member inline on (eventType: string, action: #Event -> unit, ?options: AddEventListenerOptions, ?dependencies: obj []) =
            let addOptions = React.useMemo((fun () -> Impl.adjustPassive options), [| options |])
            let removeOptions = React.useMemo((fun () -> Impl.createRemoveOptions options), [| options |])
            let fn = React.useMemo((fun () -> unbox<#Event> >> action), [| action; if dependencies.IsSome then yield! dependencies.Value |])

            let listener = React.useCallbackRef(fun () ->
                match addOptions with
                | Some options ->
                    document.addEventListener(eventType, fn, options)
                | None -> document.addEventListener(eventType, fn)

                React.createDisposable(fun () ->
                    match removeOptions with
                    | Some options -> document.removeEventListener(eventType, fn, options)
                    | None -> document.removeEventListener(eventType, fn)
                )
            )

            React.useEffect(listener)

        static member inline onMouseDown (action: MouseEvent -> unit, ?options: AddEventListenerOptions, ?dependencies) =
            useListener.on("mousedown", action, ?options = options, ?dependencies = dependencies)
        static member inline onTouchStart (action: TouchEvent -> unit, ?options: AddEventListenerOptions, ?dependencies) =
            useListener.on("touchstart", action, ?options = options, ?dependencies = dependencies)

        static member inline onResize (action: Event -> unit, ?options: AddEventListenerOptions) =
            useListener.on("resize", action, ?options = options)

        /// Invokes the callback when a click event is not within the given element.
        ///
        /// Uses separate handlers for touch and mouse events.
        ///
        /// This listener is passive by default.
        static member inline onClickAway (elemRef: IRefValue<#HTMLElement option>, callback: MouseEvent -> unit, touchCallback: TouchEvent -> unit, ?options: AddEventListenerOptions) =
            let options = Option.defaultValue Impl.defaultPassive options

            useListener.onMouseDown((fun ev ->
                match elemRef.current with
                | Some elem when not (elem.contains(unbox ev.target)) ->
                    callback ev
                | _ -> ()
            ), options)

            useListener.onTouchStart((fun ev ->
                match elemRef.current with
                | Some elem when not (elem.contains(unbox ev.target)) ->
                    touchCallback ev
                | _ -> ()
            ), options)

        /// Invokes the callback when a click event is not within the given element.
        ///
        /// Shares a common callback for both touch and mouse events.
        ///
        /// This listener is passive by default.
        static member inline onClickAway (elemRef: IRefValue<#HTMLElement option>, callback: UIEvent -> unit, ?options: AddEventListenerOptions, ?dependencies) =
            let options = Option.defaultValue Impl.defaultPassive options

            useListener.onMouseDown((fun ev ->
                match elemRef.current with
                | Some elem when not (elem.contains(unbox ev.target)) ->
                    callback ev
                | _ -> ()
            ), options, ?dependencies = dependencies)

            useListener.onTouchStart((fun ev ->
                match elemRef.current with
                | Some elem when not (elem.contains(unbox ev.target)) ->
                    callback ev
                | _ -> ()
            ), options, ?dependencies = dependencies)

    [<Erase>]
    type useElementListener =
        static member inline on (elemRef: IRefValue<#HTMLElement option>, eventType: string, action: #Event -> unit, ?options: AddEventListenerOptions) =
            let addOptions = React.useMemo((fun () -> Impl.adjustPassive options), [| options |])
            let removeOptions = React.useMemo((fun () -> Impl.createRemoveOptions options), [| options |])
            let fn = React.useMemo((fun () -> unbox<#Event> >> action), [| action |])

            let listener = React.useCallbackRef(fun () ->
                elemRef.current |> Option.iter(fun elem ->
                    match addOptions with
                    | Some options -> elem.addEventListener(eventType, fn, options)
                    | None -> elem.addEventListener(eventType, fn)
                )

                React.createDisposable(fun () ->
                    elemRef.current |> Option.iter(fun elem ->
                        match removeOptions with
                        | Some options -> elem.removeEventListener(eventType, fn, options)
                        | None -> elem.removeEventListener(eventType, fn)
                ))
            )

            React.useEffect(listener)

        static member inline onResize (elemRef: IRefValue<#HTMLElement option>, action: Event -> unit, ?options: AddEventListenerOptions) =
            useElementListener.on(elemRef, "resize", action, ?options = options)