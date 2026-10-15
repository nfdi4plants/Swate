module internal Swate.Components.Page.ProvenanceGraph.ProvenanceGraphTests

// Ported from ProvenanceExplorer.test.tsx of arc-portal-ui, on the example ARC of the sample file and on small
// ARCs built in the tests. The real ELK runs in jsdom with the 7px text estimate.
//
// Ported: the start with the datasets and one crumb, the processes, groups, detail and lineage levels and the
// navigation inside the lineage, the return through a crumb, the dataset switch for an external process, the
// dataset without processes, the process without inputs and outputs, the focus when a view without a graph
// comes and goes, the new index that returns to the datasets, the ARC without datasets, the cap of 80 nodes,
// and the cases of the header (all crumbs when they fit, the fraction of a pixel, the first crumb with a menu
// and the last two, the menu that navigates, the full header again when the box grows or the path gets
// shorter, three crumbs, four crumbs).
//
// New: a selection that the index does not know, the same index object that keeps the navigation, the
// legend swatches, the list of hidden crumbs (state, Escape, click outside), the focus that stays where the
// user put it when the page renders again, and the canvas that stays mounted when the cap note comes and goes.
//
// Dropped: the query name next to the crumbs (the label is not part of this page), and the assertions on the
// texts of the legend, the empty messages, the hint and the aria labels. The tests find the elements by test id,
// and the empty states by the data-empty attribute.

open System.Collections.Generic
open Browser.Dom
open Browser.Types
open Fable.Core
open Feliz
open Vitest
open ARCtrl
open Swate.Components.Shared.ProvenanceGraph.Types
open Swate.Components.Shared.ProvenanceGraph.BuildIndex
open Swate.Components.Page.ProvenanceGraph.Types

module Sample = Swate.Components.Page.ProvenanceGraphSample

// jsdom has no ResizeObserver. The stub keeps the observers and reports the widths a test sets. A frame runs at
// once, because the header applies a measurement in the next animation frame.

[<Emit("""(() => {
    globalThis.__provResize = [];
    globalThis.ResizeObserver = class {
        constructor(callback) { this.callback = callback; this.targets = []; globalThis.__provResize.push(this) }
        observe(target) { this.targets.push(target) }
        unobserve() {}
        disconnect() { this.targets = [] }
    };
    globalThis.__provFrame = globalThis.requestAnimationFrame;
    globalThis.requestAnimationFrame = callback => { callback(0); return 0 };
})()""")>]
let private installObservers () : unit = jsNative

[<Emit("""(() => {
    delete globalThis.ResizeObserver;
    delete globalThis.__provResize;
    if (globalThis.__provFrame) { globalThis.requestAnimationFrame = globalThis.__provFrame; delete globalThis.__provFrame }
})()""")>]
let private removeObservers () : unit = jsNative

// Reports the width of the header content area and the natural width of its full crumbs. The header observes
// a box and, inside it, the ruler. The canvas observes one element only.
[<Emit("""(() => {
    const observer = globalThis.__provResize.find(o => o.targets.length === 2 && o.targets[0].contains(o.targets[1]));
    if (!observer) throw new Error('The page observes no header box.');
    const [box, full] = observer.targets;
    observer.callback([{ target: box, contentRect: { width: $0 } }, { target: full, contentRect: { width: $1 } }]);
})()""")>]
let private reportHeaderWidthsNow (available: float) (natural: float) : unit = jsNative

[<Emit("$0.style.getPropertyValue($1)")>]
let private styleProperty (el: HTMLElement) (name: string) : string = jsNative


let private reportHeaderWidths (available: float) (natural: float) : JS.Promise<unit> =
    RTL.act (fun () -> promise { reportHeaderWidthsNow available natural })

Vitest.afterEach (fun () ->
    RTL.cleanup ()
    removeObservers ()
)

// Ids of the example ARC

let private extraction = "assays/Extraction/"
let private measurement = "assays/Measurement/"
let private extractionProcess = "assays/Extraction/::Extraction"
let private inputGroup = extractionProcess + "#in0"

// Queries

let private eventually (check: unit -> unit) : JS.Promise<unit> =
    RTL.waitFor ((fun () -> check ()), RTLWaitForOptions(timeout = 20000))

let private byTestId (id: string) : HTMLElement = RTL.screen.getByTestId id
let private maybeTestId (id: string) : HTMLElement option = RTL.screen.queryByTestId id
let private nodeEl (id: string) : HTMLElement = byTestId $"graph-node-{id}"

let private waitNode (id: string) : JS.Promise<unit> =
    eventually (fun () -> nodeEl id |> ignore)

let private click (el: HTMLElement) = RTL.fireEvent.click el

let private press (el: HTMLElement) (key: string) =
    RTL.fireEvent.custom ("keyDown", el, {| key = key |})

let private crumbText (index: int) : string =
    (byTestId $"provenance-crumb-{index}").textContent

let private isFocused (el: HTMLElement) : bool =
    obj.ReferenceEquals(document.activeElement, el)

let private crumbElements () =
    RTL.screen.getAllByTestId (System.Text.RegularExpressions.Regex "^provenance-crumb-[0-9]+$")

let private crumbCount () : int = crumbElements().Count

let private nodeCount () : int =
    document.querySelectorAll("[data-testid^='graph-node-']").length

let private empty () : HTMLElement = byTestId "provenance-empty"
let private emptyKind () : string = (empty ()).getAttribute "data-empty"

let private render (index: ProvenanceIndex) : RenderResult =
    RTL.render (ProvenanceGraph.ProvenanceGraph(index))

let private present (el: HTMLElement option) : bool = el.IsSome

// Navigation helpers: click a node and wait for the next view.

let private open' (nodeId: string) (next: string) : JS.Promise<unit> = promise {
    click (nodeEl nodeId)
    do! waitNode next
}

let private openGroups () : JS.Promise<unit> = promise {
    do! waitNode extraction
    do! open' extraction extractionProcess
    do! open' extractionProcess inputGroup
}

let private openLineage () : JS.Promise<unit> = promise {
    do! openGroups ()
    do! open' inputGroup "leaf-1"
    do! open' "leaf-1" "data/m1.raw"
}

// ARCs for the special cases

let private text (value: string) = CompositeCell.createFreeText value

let private table (name: string) (headers: CompositeHeader list) (rows: CompositeCell list list) =
    let rows =
        rows |> List.map (fun row -> ResizeArray<CompositeCell>(row)) |> ResizeArray

    ArcTable.createFromRows (name, ResizeArray<CompositeHeader>(headers), rows)

let private indexOfAssay (assay: ArcAssay) : ProvenanceIndex =
    let arc =
        ArcInvestigation.create (identifier = "inv", assays = ResizeArray [ assay ])

    ProvenanceIndex.ofData (ProvenanceGraphData.ofInvestigation arc)

/// A process whose rows have a protocol and no inputs and outputs.
let private withoutInputsAndOutputs: ProvenanceIndex =
    indexOfAssay (
        ArcAssay.create (
            identifier = "NoIo",
            tables =
                ResizeArray [
                    table "notes" [ CompositeHeader.ProtocolREF ] [ [ text "notes.md" ] ]
                ]
        )
    )

/// A chain of samples n0 -> n1 -> ... with `length` samples. The process "first" holds the first row, so its
/// groups stay small. The process "rest" holds the others, and the lineage of n0 grows large.
let private chainIndex (length: int) : ProvenanceIndex =
    let headers = [
        CompositeHeader.Input IOType.Sample
        CompositeHeader.Output IOType.Sample
    ]

    let row i = [ text $"n{i}"; text $"n{i + 1}" ]

    indexOfAssay (
        ArcAssay.create (
            identifier = "chain",
            tables =
                ResizeArray [
                    table "first" headers [ row 0 ]
                    table "rest" headers [ for i in 1 .. length - 2 -> row i ]
                ]
        )
    )

let private longTest = TestOptions(timeout = 60000)

// Levels

Vitest.describe (
    "ProvenanceGraph navigation",
    fun () ->
        Vitest.test (
            "starts with the datasets and a single current crumb",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! waitNode extraction

                Vitest.expect(nodeCount ()).toBe Sample.index.Datasets.Length
                Vitest.expect(crumbCount ()).toBe 1
                Vitest.expect((byTestId "provenance-crumb-0").getAttribute "aria-current").toBe "page"
                Vitest.expect(present (maybeTestId "provenance-hint")).toBe false
                Vitest.expect(present (maybeTestId "provenance-empty")).toBe false
            }
        )

        Vitest.test (
            "drills down through all five levels and keeps navigating inside the lineage",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! waitNode extraction

                // Processes of the dataset. The sampling process of the study is shown as an external one.
                do! open' extraction extractionProcess
                Vitest.expect(crumbCount ()).toBe 2
                Vitest.expect(crumbText 1).toBe "Extraction"
                Vitest.expect(present (maybeTestId "graph-node-studies/Plants/::Sampling")).toBe true

                // Groups of the process: two input and two output groups.
                do! open' extractionProcess inputGroup
                Vitest.expect(crumbCount ()).toBe 3
                Vitest.expect(crumbText 2).toBe "Extraction"
                Vitest.expect(nodeCount ()).toBe 4

                // Detail of a group, with the hint to select a node.
                do! open' inputGroup "leaf-1"
                Vitest.expect(crumbCount ()).toBe 4
                Vitest.expect(crumbText 3).toBe "Inputs A"
                Vitest.expect(present (maybeTestId "provenance-hint")).toBe true

                // Lineage of a node.
                do! open' "leaf-1" "data/m1.raw"
                Vitest.expect(crumbCount ()).toBe 5
                Vitest.expect(crumbText 4).toBe "leaf-1"
                Vitest.expect(present (maybeTestId "provenance-hint")).toBe false

                // A click inside the lineage centers it on the clicked node.
                do! open' "extract-1" "extract-1"
                Vitest.expect(crumbCount ()).toBe 5
                Vitest.expect(crumbText 4).toBe "extract-1"
                Vitest.expect(crumbText 3).toBe "Inputs A"
            }
        )

        Vitest.test (
            "returns to a level through its crumb and drops the deeper crumbs",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()

                click (byTestId "provenance-crumb-2")
                do! waitNode inputGroup

                Vitest.expect(crumbCount ()).toBe 3
                Vitest.expect(nodeCount ()).toBe 4

                click (byTestId "provenance-crumb-0")
                do! waitNode extraction

                Vitest.expect(crumbCount ()).toBe 1
                Vitest.expect(nodeCount ()).toBe Sample.index.Datasets.Length
            }
        )

        Vitest.test (
            "switches to the dataset of an external process when it is clicked",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! waitNode measurement

                do! open' measurement extractionProcess
                // The extraction belongs to the other assay. Its groups open, and the crumb names that assay.
                Vitest.expect(crumbText 1).toBe "Measurement"
                do! open' extractionProcess inputGroup
                Vitest.expect(crumbCount ()).toBe 3
                Vitest.expect(crumbText 1).toBe "Extraction"

                click (byTestId "provenance-crumb-1")
                do! waitNode extractionProcess
                Vitest.expect(crumbCount ()).toBe 2
            }
        )

        Vitest.test (
            "shows a legend whose swatches carry their own color",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! waitNode extraction

                let swatches = RTL.screen.getAllByTestId "provenance-legend-swatch"
                Vitest.expect(swatches.Count).toBeGreaterThan 0
                Vitest.expect(present (maybeTestId "provenance-legend")).toBe true

                // The page defines the custom properties the swatches read, so they resolve inside the page root.
                let root = byTestId "provenance-graph"
                Vitest.expect((styleProperty root "--prov-study") <> "").toBe true
            }
        )
)

// Empty states

Vitest.describe (
    "ProvenanceGraph empty states",
    fun () ->
        Vitest.test (
            "tells that an ARC without datasets has none and keeps the legend",
            fun () -> promise {
                render Sample.emptyIndex |> ignore

                Vitest.expect(emptyKind ()).toBe "no-datasets"
                Vitest.expect(present (maybeTestId "provenance-legend")).toBe true
                Vitest.expect(present (maybeTestId "graph-canvas")).toBe false
            }
        )

        Vitest.test (
            "tells that a dataset without processes has none and moves the focus to the message",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! waitNode "assays/Empty/"

                click (nodeEl "assays/Empty/")

                Vitest.expect(emptyKind ()).toBe "no-processes"
                Vitest.expect(isFocused (empty ())).toBe true
                Vitest.expect(present (maybeTestId "provenance-legend")).toBe true
                Vitest.expect(present (maybeTestId "graph-canvas")).toBe false
            }
        )

        Vitest.test (
            "gives the focus back to the canvas when a view without a graph goes",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! waitNode "assays/Empty/"
                click (nodeEl "assays/Empty/")
                Vitest.expect(isFocused (empty ())).toBe true

                click (byTestId "provenance-crumb-0")
                do! waitNode extraction
                do! eventually (fun () -> Vitest.expect(isFocused (byTestId "graph-canvas")).toBe true)
            }
        )

        Vitest.test (
            "leaves the focus where the user put it when the page renders again",
            longTest,
            fun () -> promise {
                let result = render Sample.index
                do! waitNode "assays/Empty/"
                click (nodeEl "assays/Empty/")
                Vitest.expect(isFocused (empty ())).toBe true

                let crumb = byTestId "provenance-crumb-0"
                crumb.focus ()
                result.rerender (ProvenanceGraph.ProvenanceGraph(Sample.index))

                Vitest.expect(isFocused crumb).toBe true
            }
        )

        Vitest.test (
            "tells that a process whose rows have no inputs and outputs has none",
            longTest,
            fun () -> promise {
                render withoutInputsAndOutputs |> ignore
                do! waitNode "assays/NoIo/"
                do! open' "assays/NoIo/" "assays/NoIo/::notes"

                click (nodeEl "assays/NoIo/::notes")

                Vitest.expect(emptyKind ()).toBe "no-groups"
                Vitest.expect(present (maybeTestId "graph-canvas")).toBe false
            }
        )

        Vitest.test (
            "tells that a selection the index does not know is not part of the ARC",
            longTest,
            fun () -> promise {
                // The process is listed in the processes, but the lookup that the groups query reads does not have it.
                let broken = {
                    Sample.index with
                        ProcessByKey = Dictionary<string, ProvenanceProcess>()
                }

                render broken |> ignore
                do! waitNode extraction
                do! open' extraction extractionProcess

                click (nodeEl extractionProcess)

                Vitest.expect(emptyKind ()).toBe "not-in-arc"
                Vitest.expect(present (maybeTestId "provenance-hint")).toBe false
            }
        )
)

// A new index

Vitest.describe (
    "ProvenanceGraph index identity",
    fun () ->
        Vitest.test (
            "returns to the datasets when the parent passes another index object",
            longTest,
            fun () -> promise {
                let result = render Sample.index
                do! waitNode extraction
                do! open' extraction extractionProcess
                Vitest.expect(crumbCount ()).toBe 2

                result.rerender (ProvenanceGraph.ProvenanceGraph(ProvenanceIndex.ofData Sample.data))

                Vitest.expect(crumbCount ()).toBe 1
                do! waitNode extraction
                Vitest.expect(nodeCount ()).toBe Sample.index.Datasets.Length
            }
        )

        Vitest.test (
            "keeps the navigation when the parent renders again with the same index object",
            longTest,
            fun () -> promise {
                let result = render Sample.index
                do! waitNode extraction
                do! open' extraction extractionProcess

                result.rerender (ProvenanceGraph.ProvenanceGraph(Sample.index))

                Vitest.expect(crumbCount ()).toBe 2
                Vitest.expect(present (maybeTestId $"graph-node-{extractionProcess}")).toBe true
            }
        )
)

// The cap of the lineage

Vitest.describe (
    "ProvenanceGraph lineage cap",
    fun () ->
        Vitest.test (
            "shows the 80 nodes nearest to the selected one of a larger lineage and says so",
            longTest,
            fun () -> promise {
                render (chainIndex 100) |> ignore
                do! waitNode "assays/chain/"
                do! open' "assays/chain/" "assays/chain/::first"
                do! open' "assays/chain/::first" "assays/chain/::first#in0"
                do! open' "assays/chain/::first#in0" "n0"

                click (nodeEl "n0")
                do! waitNode "n79"

                let note = byTestId "provenance-lineage-cap"
                Vitest.expect(note.textContent.Contains "80").toBe true
                Vitest.expect(note.textContent.Contains "100").toBe true
                Vitest.expect(nodeCount ()).toBe 80
            }
        )

        Vitest.test (
            "shows no note when the lineage fits",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()

                Vitest.expect(present (maybeTestId "provenance-lineage-cap")).toBe false
            }
        )

        Vitest.test (
            "keeps the canvas mounted when the note comes and goes",
            longTest,
            fun () -> promise {
                render (chainIndex 100) |> ignore
                do! waitNode "assays/chain/"
                do! open' "assays/chain/" "assays/chain/::first"
                do! open' "assays/chain/::first" "assays/chain/::first#in0"
                do! open' "assays/chain/::first#in0" "n0"
                let canvasBefore = byTestId "graph-canvas"

                click (nodeEl "n0")
                do! waitNode "n79"
                Vitest.expect(present (maybeTestId "provenance-lineage-cap")).toBe true
                Vitest.expect(obj.ReferenceEquals(byTestId "graph-canvas", canvasBefore)).toBe true

                click (byTestId "provenance-crumb-3")
                do! eventually (fun () -> Vitest.expect(present (maybeTestId "provenance-lineage-cap")).toBe false)
                do! eventually (fun () -> Vitest.expect(nodeCount () < 10).toBe true)
                Vitest.expect(obj.ReferenceEquals(byTestId "graph-canvas", canvasBefore)).toBe true
            }
        )
)

// The header

Vitest.describe (
    "ProvenanceGraph header",
    fun () ->
        Vitest.beforeEach (fun () -> installObservers ())

        let menuButton () = maybeTestId "provenance-crumb-menu"

        let hasCrumb (index: int) =
            present (maybeTestId $"provenance-crumb-{index}")

        let menuItem (index: int) =
            maybeTestId $"provenance-crumb-menu-item-{index}"

        let openMenu () =
            click (byTestId "provenance-crumb-menu")

        Vitest.test (
            "shows all crumbs when the full header fits",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()

                do! reportHeaderWidths 900.0 640.0

                Vitest.expect(crumbCount ()).toBe 5
                Vitest.expect(present (menuButton ())).toBe false
                Vitest.expect((byTestId "provenance-crumb-4").getAttribute "title" |> isNull).toBe true
            }
        )

        Vitest.test (
            "keeps the full header when it fits to the pixel and switches when it is a fraction wider",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()

                do! reportHeaderWidths 640.4 640.0
                Vitest.expect(present (menuButton ())).toBe false

                do! reportHeaderWidths 640.0 640.2
                Vitest.expect(present (menuButton ())).toBe true
            }
        )

        Vitest.test (
            "shows the first crumb, a menu button and the last two crumbs when the full header does not fit",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()

                do! reportHeaderWidths 330.0 640.0

                Vitest
                    .expect(
                        [|
                            hasCrumb 0
                            hasCrumb 1
                            hasCrumb 2
                            hasCrumb 3
                            hasCrumb 4
                        |]
                    )
                    .toEqual
                    [| true; false; false; true; true |]

                Vitest.expect(present (menuButton ())).toBe true
                Vitest.expect((byTestId "provenance-crumb-4").getAttribute "aria-current").toBe "page"
                // The two crumbs that the CSS can cut carry their full text.
                Vitest.expect((byTestId "provenance-crumb-4").getAttribute "title").toBe (crumbText 4)
                Vitest.expect((byTestId "provenance-crumb-3").getAttribute "title").toBe (crumbText 3)
            }
        )

        Vitest.test (
            "lists the hidden crumbs in the menu and navigates to the chosen one",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()
                do! reportHeaderWidths 330.0 640.0

                Vitest.expect(present (menuItem 1)).toBe false
                openMenu ()

                Vitest.expect(present (menuItem 1)).toBe true
                Vitest.expect(present (menuItem 2)).toBe true
                Vitest.expect(present (menuItem 3)).toBe false

                click (byTestId "provenance-crumb-menu-item-2")
                do! waitNode inputGroup

                Vitest.expect(crumbCount ()).toBe 3
                Vitest.expect(present (menuButton ())).toBe false
            }
        )

        Vitest.test (
            "brings the full header back when the box grows",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()
                do! reportHeaderWidths 330.0 640.0

                do! reportHeaderWidths 900.0 640.0

                Vitest.expect(crumbCount ()).toBe 5
                Vitest.expect(present (menuButton ())).toBe false
            }
        )

        Vitest.test (
            "brings the full header back when the path gets shorter",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()
                do! reportHeaderWidths 330.0 640.0

                openMenu ()
                click (byTestId "provenance-crumb-menu-item-1")
                do! waitNode extractionProcess
                do! reportHeaderWidths 330.0 250.0

                Vitest.expect(crumbCount ()).toBe 2
                Vitest.expect(present (menuButton ())).toBe false
            }
        )

        Vitest.test (
            "collapses nothing with three crumbs",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openGroups ()

                do! reportHeaderWidths 200.0 480.0

                Vitest.expect(crumbCount ()).toBe 3
                Vitest.expect(present (menuButton ())).toBe false
                Vitest.expect((byTestId "provenance-crumb-2").getAttribute "title").toBe (crumbText 2)
            }
        )

        Vitest.test (
            "collapses the one crumb in the middle of four",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openGroups ()
                do! open' inputGroup "leaf-1"

                do! reportHeaderWidths 300.0 560.0
                openMenu ()

                Vitest.expect([| hasCrumb 0; hasCrumb 1; hasCrumb 2; hasCrumb 3 |]).toEqual [|
                    true
                    false
                    true
                    true
                |]

                Vitest.expect(present (maybeTestId "provenance-crumb-menu-item-1")).toBe true
                Vitest.expect(present (maybeTestId "provenance-crumb-menu-item-2")).toBe false
            }
        )

        Vitest.test (
            "opens the list with the button, reports its state and closes it with a second click",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()
                do! reportHeaderWidths 330.0 640.0

                let expanded () =
                    (byTestId "provenance-crumb-menu").getAttribute "aria-expanded"

                Vitest.expect(expanded ()).toBe "false"
                Vitest.expect(present (menuItem 1)).toBe false

                openMenu ()
                Vitest.expect(expanded ()).toBe "true"
                Vitest.expect(present (menuItem 1)).toBe true

                openMenu ()
                Vitest.expect(expanded ()).toBe "false"
                Vitest.expect(present (menuItem 1)).toBe false
            }
        )

        Vitest.test (
            "closes the list on Escape and gives the button the focus",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()
                do! reportHeaderWidths 330.0 640.0
                openMenu ()
                Vitest.expect(present (menuItem 1)).toBe true

                press (menuItem 1).Value "Escape"

                Vitest.expect(present (menuItem 1)).toBe false
                Vitest.expect(isFocused (byTestId "provenance-crumb-menu")).toBe true
            }
        )

        Vitest.test (
            "closes the list on a click outside of it",
            longTest,
            fun () -> promise {
                render Sample.index |> ignore
                do! openLineage ()
                do! reportHeaderWidths 330.0 640.0
                openMenu ()
                Vitest.expect(present (menuItem 1)).toBe true

                RTL.fireEvent.custom ("mouseDown", document.body)

                Vitest.expect(present (menuItem 1)).toBe false
            }
        )
)
