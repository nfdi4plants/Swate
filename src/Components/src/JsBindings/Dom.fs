namespace Swate.Components.JsBindings

open Browser.Types
open Fable.Core

/// Small typed bindings for browser APIs that Fable.Browser.Dom does not cover.
/// requestAnimationFrame and cancelAnimationFrame are not repeated here, Browser.Dom.window has them.
module Dom =

    [<AllowNullLiteral>]
    type ResizeObserverContentRect =
        abstract width: float
        abstract height: float

    [<AllowNullLiteral>]
    type ResizeObserverEntry =
        abstract target: Element
        abstract contentRect: ResizeObserverContentRect

    [<Global>]
    type ResizeObserver(callback: ResizeObserverEntry[] -> unit) =
        /// False where the browser has no ResizeObserver, as in jsdom.
        [<Emit("typeof ResizeObserver !== 'undefined'")>]
        static member isAvailable: bool = jsNative

        member _.observe(target: Element) : unit = jsNative
        member _.disconnect() : unit = jsNative

    [<AllowNullLiteral>]
    type private TextMetrics =
        abstract width: float

    [<AllowNullLiteral>]
    type private MeasureContext =
        abstract font: string with get, set
        abstract measureText: string -> TextMetrics

    // Null without OffscreenCanvas (jsdom, node) and where the browser gives no 2d context.
    [<Emit("typeof OffscreenCanvas === 'undefined' ? null : new OffscreenCanvas(1, 1).getContext('2d')")>]
    let private tryCreateMeasureContext () : MeasureContext = jsNative

    /// Width of a text without a canvas. Every character counts 7 px, which keeps layouts deterministic.
    let estimateTextWidth (text: string) : float = float text.Length * 7.0

    /// The monospace font stack of createTextMeasure. Elements that draw mono text use it too, so the
    /// drawn text has the width that was measured.
    let monoFamily =
        "ui-monospace, SFMono-Regular, \"SF Mono\", Menlo, Consolas, monospace"

    /// Creates a text measure: text, font weight, font size in px and monospace, to the width in px.
    /// The font is sansFamily, or a monospace stack for mono text. OffscreenCanvas needs no DOM element,
    /// and jsdom has none, so tests get estimateTextWidth without jsdom reporting an unimplemented canvas.
    let createTextMeasure (sansFamily: string) : string -> float -> float -> bool -> float =
        let context = tryCreateMeasureContext ()

        if isNull context then
            fun text _ _ _ -> estimateTextWidth text
        else
            fun text weight size mono ->
                let family = if mono then monoFamily else sansFamily
                context.font <- $"{weight} {size}px {family}"
                (context.measureText text).width

    [<Emit("getComputedStyle($0).fontFamily")>]
    let computedFontFamily (el: Element) : string = jsNative

    /// The computed CSS color-scheme of an element, such as "light" or "dark".
    [<Emit("getComputedStyle($0).colorScheme")>]
    let computedColorScheme (el: Element) : string = jsNative

    [<Emit("$0.isConnected")>]
    let isConnected (el: Element) : bool = jsNative

    /// Sets one inline style property by its CSS name, such as "max-width".
    [<Emit("$0.style.setProperty($1, $2)")>]
    let setStyleProperty (el: HTMLElement) (name: string) (value: string) : unit = jsNative

    [<Emit("$0.focus({ preventScroll: true })")>]
    let focusWithoutScroll (el: HTMLElement) : unit = jsNative
