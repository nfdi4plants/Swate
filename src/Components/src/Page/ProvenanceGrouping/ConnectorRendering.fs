namespace Swate.Components.Page.ProvenanceGrouping

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Primitive.ContextMenu
open Swate.Components.Primitive.ContextMenu.Types
open Swate.Components.Page.ProvenanceGrouping.Types

module ConnectorSvg =

    let debugAttributes debug (measured: MeasuredConnector) = [
        if debug then
            svg.custom ("data-testid", measured.TestId)
            svg.custom ("data-provenance-connection-key", measured.Key)
    ]

    /// Paths are also written as the CSS `d` property so discrete layout changes can
    /// morph the curve instead of snapping; continuous tracking (scroll, drags) keeps
    /// `animate = false` and snaps per frame. Browsers without CSS `d` fall back to
    /// the attribute and simply skip the morph.
    let pathStyle (path: string) (animate: bool) =
        createObj [
            "d" ==> $"path('{path}')"
            "transition"
            ==> (if animate then
                     "d 160ms ease, stroke-width 120ms ease, stroke-opacity 120ms ease"
                 else
                     "stroke-width 120ms ease, stroke-opacity 120ms ease")
        ]

    let strokeElements (measured: MeasuredConnector) strokeWidth strokeOpacity animate debug =
        let strokeColor = measured.Color |> Option.defaultValue "currentColor"
        let debugAttributes = debugAttributes debug measured

        [
            // A surface-colored halo keeps crossing connectors readable.
            Svg.path [
                svg.d measured.Path
                svg.custom ("style", pathStyle measured.Path animate)
                svg.fill "none"
                svg.stroke "currentColor"
                svg.strokeWidth (strokeWidth + 2.5)
                svg.strokeLineCap "round"
                svg.className "swt:text-base-200"
                match measured.StrokeDasharray with
                | Some dash -> svg.custom ("strokeDasharray", dash)
                | None -> ()
            ]
            Svg.path [
                svg.d measured.Path
                svg.custom ("style", pathStyle measured.Path animate)
                svg.fill "none"
                svg.stroke strokeColor
                svg.strokeWidth strokeWidth
                svg.strokeLineCap "round"
                svg.custom ("strokeOpacity", strokeOpacity)
                svg.className measured.ClassName
                match measured.StrokeDasharray with
                | Some dash -> svg.custom ("strokeDasharray", dash)
                | None -> ()
                match measured.Color with
                | Some color -> svg.custom ("data-provenance-color", color)
                | None -> ()
                if measured.InteractiveConnection.IsNone then
                    yield! debugAttributes
            ]
        ]

module ConnectorContextMenu =

    let connectionKeyAttribute = "data-provenance-interactive-connection-key"

    let private tryTargetElement (event: Browser.Types.Event) : Browser.Types.Element option =
        let targetObj: obj = box event.target

        if isNullOrUndefined targetObj || isNullOrUndefined targetObj?closest then
            None
        else
            Some(unbox<Browser.Types.Element> targetObj)

    let private interactiveConnectionKey (event: Browser.Types.MouseEvent) =
        event
        |> tryTargetElement
        |> Option.bind (fun target ->
            let node: Browser.Types.Element = !!target?closest($"[{connectionKeyAttribute}]")

            if isNull node then
                None
            else
                Some(node.getAttribute connectionKeyAttribute)
        )
        |> Option.bind (fun key -> if isNull key then None else Some key)

    let spawnData (paths: MeasuredConnector list) event =
        interactiveConnectionKey event
        |> Option.bind (fun key ->
            paths
            |> List.tryFind (fun path -> path.Key = key)
            |> Option.bind (fun path -> path.InteractiveConnection)
        )
        |> Option.map box

    let items remove (data: obj) =
        let connection = data |> unbox<DisplayConnection>

        [
            ContextMenuItem(
                text = Html.span "Delete",
                icon =
                    Html.i [
                        prop.className "swt:iconify swt:fluent--delete-20-regular swt:size-4"
                    ],
                onClick =
                    (fun event ->
                        event.buttonEvent.stopPropagation ()
                        remove connection
                    )
            )
        ]

/// Thin ResizeObserver bindings used to remeasure connector paths after layout changes.
module ConnectorObserver =

    [<Emit("new ResizeObserver(() => $0())")>]
    let create (callback: unit -> unit) : obj = jsNative

    [<Emit("$0.observe($1)")>]
    let observeNode (observer: obj) (target: HTMLElement) : unit = jsNative

    [<Emit("$0.querySelectorAll($1).forEach(node => $2.observe(node))")>]
    let observeMatching (container: HTMLElement) (selector: string) (observer: obj) : unit = jsNative

    [<Emit("$0.disconnect()")>]
    let disconnect (observer: obj) : unit = jsNative

module ConnectorMutationObserver =

    [<Emit("new MutationObserver(() => $0())")>]
    let create (callback: unit -> unit) : obj = jsNative

    [<Emit("$0.observe($1, { childList: true, subtree: true, attributes: true, attributeFilter: ['class', 'style'] })")>]
    let observe (observer: obj) (target: HTMLElement) : unit = jsNative

    [<Emit("$0.disconnect()")>]
    let disconnect (observer: obj) : unit = jsNative

module AnimationFrame =

    [<Emit("requestAnimationFrame($0)")>]
    let request (_callback: unit -> unit) : float = jsNative

    [<Emit("cancelAnimationFrame($0)")>]
    let cancel (_handle: float) : unit = jsNative
