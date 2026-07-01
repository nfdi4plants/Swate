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

[<Erase; Mangle(false)>]
type ConnectorOverlay =

    [<ReactComponent>]
    static member private LiveConnectorLayer(store: LiveDrag.Store, ?debug: bool) =
        let _, bump = React.useStateWithUpdater 0

        React.useEffect (
            (fun () ->
                let unsubscribe =
                    store |> LiveDrag.subscribe (fun () -> bump (fun version -> version + 1))

                FsReact.createDisposable unsubscribe
            ),
            [| box store |]
        )

        match ConnectorPaths.liveConnection store.Current with
        | Some measured ->
            Svg.svg [
                svg.className "swt:absolute swt:inset-0 swt:pointer-events-none swt:size-full"
                svg.children [
                    yield! ConnectorSvg.strokeElements measured measured.StrokeWidth 1.0 (defaultArg debug false)
                ]
            ]
        | None -> Html.none

    [<ReactComponent>]
    static member Main
        (
            containerRef: IRefValue<HTMLElement option>,
            layerId: ProvenanceLayerId,
            model: ProvenanceModel,
            inputGroups: DisplayGroup list,
            outputGroups: DisplayGroup list,
            connections: DisplayConnection list,
            inputRailProjection: PropertyRails.RailProjection,
            outputRailProjection: PropertyRails.RailProjection,
            overlayState: ConnectorOverlayState,
            layoutSignature: string,
            showPropertyHeaderConnectors: bool,
            liveDragStore: LiveDrag.Store,
            onSelect: DisplayConnection -> unit,
            ?onRemove: DisplayConnection -> unit,
            ?debug: bool,
            ?railColorByHeader: Map<ProvenancePropertyHeader, string option>
        ) =
        let paths, setPaths = React.useStateWithUpdater ([]: MeasuredConnector list)
        let hoveredKey, setHoveredKey = React.useState<string option> None
        let pendingFrame = React.useRef (None: float option)
        let debugEnabled = defaultArg debug false
        let colorByHeader = defaultArg railColorByHeader Map.empty

        let setMeasuredPaths next =
            setPaths (fun current -> if current = next then current else next)

        let measureNow () =
            pendingFrame.current <- None

            match containerRef.current with
            | None -> setMeasuredPaths []
            | Some container ->
                let context = ConnectorMeasure.createContext container

                ConnectorPaths.all
                    context
                    layerId
                    model
                    inputGroups
                    outputGroups
                    connections
                    inputRailProjection
                    outputRailProjection
                    colorByHeader
                    overlayState
                    showPropertyHeaderConnectors
                |> setMeasuredPaths

        let scheduleMeasure () =
            match pendingFrame.current with
            | Some _ -> ()
            | None -> pendingFrame.current <- Some(AnimationFrame.request measureNow)

        let cancelPendingFrame () =
            match pendingFrame.current with
            | Some handle ->
                AnimationFrame.cancel handle
                pendingFrame.current <- None
            | None -> ()

        React.useEffect (
            (fun () ->
                measureNow ()

                match containerRef.current with
                | None -> FsReact.createDisposable cancelPendingFrame
                | Some container ->
                    let onLayout = fun (_: Event) -> scheduleMeasure ()
                    container.addEventListener ("scroll", onLayout)
                    Browser.Dom.window.addEventListener ("resize", onLayout)

                    let observer = ConnectorObserver.create scheduleMeasure

                    let observeCurrentNodes () =
                        ConnectorObserver.observeNode observer container

                        ConnectorObserver.observeMatching
                            container
                            "[data-provenance-group-node],[data-provenance-connection-node],[data-provenance-resize-node]"
                            observer

                    let mutationFrame = ref (None: float option)

                    let cancelMutationFrame () =
                        match mutationFrame.Value with
                        | Some handle ->
                            AnimationFrame.cancel handle
                            mutationFrame.Value <- None
                        | None -> ()

                    let scheduleMutationMeasure () =
                        match mutationFrame.Value with
                        | Some _ -> ()
                        | None ->
                            mutationFrame.Value <-
                                Some(
                                    AnimationFrame.request (fun () ->
                                        mutationFrame.Value <- None
                                        observeCurrentNodes ()
                                        scheduleMeasure ()
                                    )
                                )

                    let mutationObserver =
                        ConnectorMutationObserver.create (fun () -> scheduleMutationMeasure ())

                    observeCurrentNodes ()
                    ConnectorMutationObserver.observe mutationObserver container

                    FsReact.createDisposable (fun () ->
                        container.removeEventListener ("scroll", onLayout)
                        Browser.Dom.window.removeEventListener ("resize", onLayout)
                        ConnectorObserver.disconnect observer
                        ConnectorMutationObserver.disconnect mutationObserver
                        cancelMutationFrame ()
                        cancelPendingFrame ()
                    )
            ),
            [|
                box layerId
                box model
                box inputGroups
                box outputGroups
                box connections
                box inputRailProjection
                box outputRailProjection
                box overlayState.ExpandedGroup
                box overlayState.ExpandedProperties
                box showPropertyHeaderConnectors
            |]
        )

        React.useEffect ((fun () -> scheduleMeasure ()), [| box layoutSignature |])

        let selectedConnectionId = overlayState.SelectedConnectionId

        React.Fragment [
            Svg.svg [
                svg.className "swt:absolute swt:inset-0 swt:pointer-events-none swt:size-full"
                svg.children [
                    for measured in paths do
                        let activateFromKeyboard (event: KeyboardEvent) =
                            match measured.InteractiveConnection, event.key with
                            | Some connection, "Enter"
                            | Some connection, " "
                            | Some connection, "Spacebar" ->
                                event.preventDefault ()
                                onSelect connection
                            | Some connection, "Delete"
                            | Some connection, "Backspace" ->
                                match onRemove with
                                | Some remove ->
                                    event.preventDefault ()
                                    remove connection
                                | None -> ()
                            | _ -> ()

                        let isSelected =
                            match measured.InteractiveConnection, selectedConnectionId with
                            | Some connection, Some selectedId -> connection.Id = selectedId
                            | _ -> false

                        let isEmphasized = isSelected || hoveredKey = Some measured.Key

                        let strokeWidth =
                            if isEmphasized then
                                measured.StrokeWidth + 1.25
                            else
                                measured.StrokeWidth

                        // Selecting a connection keeps it bright and recedes its siblings.
                        let strokeOpacity =
                            match measured.InteractiveConnection with
                            | Some _ when isEmphasized -> 1.0
                            | Some _ when selectedConnectionId.IsSome -> 0.3
                            | Some _ -> 0.85
                            | None -> 1.0

                        let debugAttributes = ConnectorSvg.debugAttributes debugEnabled measured

                        Svg.g [
                            svg.key measured.Key
                            svg.children [
                                yield! ConnectorSvg.strokeElements measured strokeWidth strokeOpacity debugEnabled
                                // A wide transparent stroke is the actual pointer/keyboard target,
                                // so selecting a thin curve no longer needs pixel accuracy.
                                match measured.InteractiveConnection with
                                | Some connection ->
                                    Svg.path [
                                        svg.d measured.Path
                                        svg.fill "none"
                                        svg.stroke "transparent"
                                        svg.strokeWidth 14
                                        svg.className
                                            "swt:pointer-events-auto swt:cursor-pointer swt:outline-none swt:shadow-none"
                                        svg.custom ("tabIndex", "0")
                                        svg.custom ("role", "button")
                                        svg.custom (
                                            "aria-label",
                                            measured.AriaLabel
                                            |> Option.defaultValue $"Select connection {connection.Id}"
                                        )
                                        svg.custom (ConnectorContextMenu.connectionKeyAttribute, measured.Key)
                                        yield! debugAttributes
                                        svg.onClick (fun _ -> onSelect connection)
                                        svg.onKeyDown activateFromKeyboard
                                        svg.onMouseEnter (fun _ -> setHoveredKey (Some measured.Key))
                                        svg.onMouseLeave (fun _ -> setHoveredKey None)
                                        svg.onFocus (fun _ -> setHoveredKey (Some measured.Key))
                                        svg.onBlur (fun _ -> setHoveredKey None)
                                    ]
                                | None -> ()
                            ]
                        ]
                ]
            ]
            ConnectorOverlay.LiveConnectorLayer(liveDragStore, ?debug = debug)
            match onRemove with
            | Some remove ->
                ContextMenu.ContextMenu(
                    ConnectorContextMenu.items remove,
                    ref = containerRef,
                    onSpawn = ConnectorContextMenu.spawnData paths,
                    debug = debugEnabled
                )
            | None -> Html.none
        ]
