module ElectronRenderer.ProvenanceGraphPageTests

// The Electron renderer builds without the React compiler, so a component there memoizes nothing by itself.
// The provenance graph page must not lay out its canvas again when the parent renders with the same index
// object, and it returns to the datasets when the parent passes another index object. This test renders the
// page the way the renderer does, through ReactDOM.createRoot without a compiler, counts the layouts through
// the layout seam of the page, and builds the index from a structuredClone of the data, like the renderer
// gets it over IPC.

open Browser.Dom
open Browser.Types
open Fable.Core
open Feliz
open Vitest
open ARCtrl
open Swate.Components.Shared.ProvenanceGraph.Types
open Swate.Components.Shared.ProvenanceGraph.BuildIndex
open Swate.Components.Composite.GraphCanvas.Types
open Swate.Components.Composite.GraphCanvas.Layout
open Swate.Components.Page.ProvenanceGraph

let rec private waitUntil (what: string) (predicate: unit -> bool) (attempts: int) = promise {
    if predicate () then
        return ()
    elif attempts <= 0 then
        failwith $"Timed out waiting for {what}."
    else
        do! Promise.sleep 10
        return! waitUntil what predicate (attempts - 1)
}

let private renderToBody (element: ReactElement) = promise {
    let container = document.createElement ("div") :?> HTMLDivElement
    document.body.appendChild container |> ignore
    let root = ReactDOM.createRoot container
    root.render element
    do! Promise.sleep 0

    return
        container,
        (fun () ->
            root.unmount ()
            container.remove ()
        )
}

[<Emit("$0.dispatchEvent(new MouseEvent('click', { bubbles: true }))")>]
let private dispatchClick (el: Element) : unit = jsNative

[<Emit("structuredClone($0)")>]
let private structuredClone<'T> (value: 'T) : 'T = jsNative

let private text (value: string) = CompositeCell.createFreeText value

let private table (name: string) (headers: CompositeHeader list) (rows: CompositeCell list list) =
    let rows: ResizeArray<ResizeArray<CompositeCell>> =
        rows
        |> List.map (fun (row: CompositeCell list) -> ResizeArray<CompositeCell>(row :> seq<CompositeCell>))
        |> ResizeArray

    ArcTable.createFromRows (name, ResizeArray<CompositeHeader>(headers :> seq<CompositeHeader>), rows)

/// A study that makes two samples and an assay that reads them. Every call builds a new index object from a
/// clone of the data, like the renderer gets it over IPC.
let private buildIndex () : ProvenanceIndex =
    let headers = [
        CompositeHeader.Input IOType.Sample
        CompositeHeader.Output IOType.Sample
    ]

    let study =
        ArcStudy.create (
            identifier = "S",
            tables =
                ResizeArray [
                    table "sampling" headers [
                        [ text "source-a"; text "sample-a" ]
                        [ text "source-b"; text "sample-b" ]
                    ]
                ]
        )

    let assay =
        ArcAssay.create (
            identifier = "A",
            tables =
                ResizeArray [
                    table "extraction" headers [
                        [ text "sample-a"; text "extract-a" ]
                        [ text "sample-b"; text "extract-b" ]
                    ]
                ]
        )

    let arc =
        ArcInvestigation.create (identifier = "arc-1", studies = ResizeArray [ study ], assays = ResizeArray [ assay ])

    ProvenanceIndex.ofData (structuredClone (ProvenanceGraphData.ofInvestigation arc))

/// The parent holds the index in state, like the page that loads an ARC. The counter forces a render of the
/// parent without a change of the index.
[<ReactComponent>]
let private Parent
    (
        first: ProvenanceIndex,
        second: ProvenanceIndex,
        layout: CanvasNode[] -> CanvasEdge[] -> string[] -> MeasureText -> LayoutOptions -> JS.Promise<GraphLayout>
    ) =
    let useSecond, setUseSecond = React.useState false
    let renders, setRenders = React.useState 0

    Html.div [
        Html.button [
            prop.testId "render-again"
            prop.onClick (fun _ -> setRenders (renders + 1))
            prop.text $"{renders}"
        ]
        Html.button [
            prop.testId "other-index"
            prop.onClick (fun _ -> setUseSecond true)
        ]
        Html.div [
            prop.style [ style.height 400; style.width 800 ]
            prop.children [
                ProvenanceGraph.ProvenanceGraph((if useSecond then second else first), layout = layout)
            ]
        ]
    ]

let private find (container: HTMLElement) (testId: string) : Element =
    container.querySelector $"[data-testid='{testId}']"

let private present (container: HTMLElement) (testId: string) : bool = not (isNull (find container testId))

let private crumbCount (container: HTMLElement) : int =
    container.querySelectorAll("[data-testid^='provenance-crumb-']:not([data-testid*='menu'])").length

Vitest.afterEach (fun () -> document.body.innerHTML <- "")

Vitest.describe (
    "ProvenanceGraph page without the React compiler",
    fun () ->
        Vitest.test (
            "lays out once per view while the parent renders again with the same index object, and starts over for a new one",
            TestOptions(timeout = 60000),
            fun () -> promise {
                let first = buildIndex ()
                let second = buildIndex ()
                let layouts = ref 0

                // The real layout, with a count of its calls.
                let countingLayout
                    (nodes: CanvasNode[])
                    (edges: CanvasEdge[])
                    (expanded: string[])
                    (measure: MeasureText)
                    (options: LayoutOptions)
                    =
                    layouts.Value <- layouts.Value + 1
                    layoutGraph nodes edges expanded measure options

                let! container, cleanup = renderToBody (Parent(first, second, countingLayout))

                try
                    do! waitUntil "the datasets" (fun () -> present container "graph-node-studies/S/") 500

                    dispatchClick (find container "graph-node-studies/S/")
                    do! waitUntil "the processes" (fun () -> present container "graph-node-studies/S/::sampling") 500
                    Vitest.expect(crumbCount container).toBe 2

                    // The datasets and the processes were laid out. Nothing else is running now.
                    do! Promise.sleep 100
                    let afterNavigation = layouts.Value
                    Vitest.expect(afterNavigation).toBeGreaterThanOrEqual 2

                    // A render of the parent with the same index object leaves the view alone and starts no layout.
                    for round in 1..3 do
                        dispatchClick (find container "render-again")

                        do!
                            waitUntil
                                "the next render"
                                (fun () -> (find container "render-again").textContent = string round)
                                500

                        do! Promise.sleep 100

                    Vitest.expect(crumbCount container).toBe 2
                    Vitest.expect(present container "graph-node-studies/S/::sampling").toBe true
                    Vitest.expect(layouts.Value).toBe afterNavigation

                    // Another index object, even with the same data, is another ARC.
                    dispatchClick (find container "other-index")
                    do! waitUntil "the datasets again" (fun () -> present container "graph-node-assays/A/") 500
                    Vitest.expect(crumbCount container).toBe 1
                    Vitest.expect(present container "graph-node-studies/S/::sampling").toBe false
                    Vitest.expect(layouts.Value).toBeGreaterThan afterNavigation
                finally
                    cleanup ()
            }
        )
)
