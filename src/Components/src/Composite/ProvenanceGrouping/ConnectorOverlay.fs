namespace Swate.Components.Composite.ProvenanceGrouping

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Composite.ProvenanceGrouping.Helper

type private MeasuredConnection =
    {
        Connection: DisplayConnection
        Path: string
    }

module private ConnectorMeasure =

    let tryNode (container: HTMLElement) nodeId =
        let node: HTMLElement = !!container?querySelector($"[data-provenance-group-node='{nodeId}']")
        if isNull node then None else Some node

    let path container connection =
        match
            tryNode container (groupNodeId ProvenanceSide.Input connection.SourceGroupId),
            tryNode container (groupNodeId ProvenanceSide.Output connection.TargetGroupId)
        with
        | Some source, Some target ->
            let origin = container.getBoundingClientRect()
            let sourceRect = source.getBoundingClientRect()
            let targetRect = target.getBoundingClientRect()
            let startX = sourceRect.right - origin.left + float container.scrollLeft
            let startY = sourceRect.top - origin.top + float container.scrollTop + sourceRect.height / 2.
            let endX = targetRect.left - origin.left + float container.scrollLeft
            let endY = targetRect.top - origin.top + float container.scrollTop + targetRect.height / 2.
            let bend = max 72. (abs (endX - startX) / 2.)
            Some $"M {startX} {startY} C {startX + bend} {startY}, {endX - bend} {endY}, {endX} {endY}"
        | _ -> None

[<Erase; Mangle(false)>]
type ConnectorOverlay =

    [<ReactComponent>]
    static member Main(containerRef: IRefValue<HTMLElement option>, connections: DisplayConnection list, onSelect: DisplayConnection -> unit, ?debug: bool) =
        let paths, setPaths = React.useState ([]: MeasuredConnection list)

        let measure () =
            match containerRef.current with
            | None -> setPaths []
            | Some container ->
                connections
                |> List.choose (fun connection ->
                    ConnectorMeasure.path container connection
                    |> Option.map (fun path -> { Connection = connection; Path = path }))
                |> setPaths

        React.useLayoutEffect (
            (fun () ->
                measure ()
                match containerRef.current with
                | None -> FsReact.createDisposable (fun () -> ())
                | Some container ->
                    let onLayout = fun (_: Event) -> measure ()
                    container.addEventListener ("scroll", onLayout)
                    Browser.Dom.window.addEventListener ("resize", onLayout)
                    FsReact.createDisposable (fun () ->
                        container.removeEventListener ("scroll", onLayout)
                        Browser.Dom.window.removeEventListener ("resize", onLayout))),
            [| box connections |]
        )

        Svg.svg [
            svg.className "swt:absolute swt:inset-0 swt:pointer-events-none swt:size-full"
            svg.children [
                for measured in paths do
                    Svg.path [
                        svg.d measured.Path
                        svg.fill "none"
                        svg.stroke "currentColor"
                        svg.className "swt:text-primary swt:pointer-events-auto swt:cursor-pointer"
                        if defaultArg debug false then svg.custom ("data-testid", "provenance-connection")
                        svg.onClick (fun _ -> onSelect measured.Connection)
                    ]
            ]
        ]
