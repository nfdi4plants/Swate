﻿[<AutoOpenAttribute>]
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

[<Erase;RequireQualifiedAccess>]
module React =
    [<Erase>]
    type useListener =
        static member inline on (eventType: string, action: #Event -> unit, ?options: AddEventListenerOptions) =
            let addOptions = React.useMemo((fun () -> Impl.adjustPassive options), [| options |])
            let removeOptions = React.useMemo((fun () -> Impl.createRemoveOptions options), [| options |])
            let fn = React.useMemo((fun () -> unbox<#Event> >> action), [| action |])

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