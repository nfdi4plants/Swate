namespace Swate.Components.Page.ProvenanceGrouping

open Fable.Core
open Feliz
open Swate.Components.Composite.TutorialOverlay
open Swate.Components.Composite.TutorialOverlay.Types
open Swate.Components.Shared.ProvenanceGrouping
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Page.ProvenanceGrouping.Types

/// How the tutorial sandbox should be seeded when a checkpoint is (re)entered:
/// the UI state the sample editor starts from and which rail begins unfolded
/// on layouts that collapse the rails.
type ProvenanceTutorialCheckpoint = {
    InitUiState: ProvenanceSession -> UiState
    OpenRail: ProvenanceSide option
}

/// The guided tour through the provenance editor's features. Selectors rely on
/// always-rendered hooks: `data-tutorial` anchors, the stable `title`/`aria-label`
/// texts of toolbar buttons, and the `data-provenance-*` measurement attributes.
/// The tour runs against the sample fixture session, so the task steps can name
/// concrete properties (Species) and predict which cards exist.
module ProvenanceTutorialSteps =

    // The Species rail button carries this title until Species is grouped, and
    // it only exists at all once Species has been dragged out of the shelf -
    // which makes it both the drag step's success condition and the group
    // step's click target.
    let private speciesGroupButton = "button[title='Group input entities by Species']"

    // On medium/narrow layouts the input rail folds behind a toggle button;
    // the fallback keeps the spotlight meaningful there.
    let private inputRail =
        "[data-tutorial='provenance-rail-Input'], button[aria-label='Show input properties']"

    let private explain id title description selector = {
        Id = id
        Title = title
        Description = description
        TargetSelector = Some selector
        Task = None
        Advance = TutorialAdvance.Manual
        Checkpoint = None
    }

    // Every hands-on step carries its own checkpoint, so jumping or going back
    // to it always rebuilds the sandbox state its instructions assume - no
    // matter what the user changed on other steps.
    let private task id title description selector instruction eventSelector checkpoint = {
        Id = id
        Title = title
        Description = description
        TargetSelector = Some selector
        Task = Some instruction
        Advance = TutorialAdvance.OnEvent("click", eventSelector)
        Checkpoint = Some checkpoint
    }

    let all: TutorialStep[] = [|
        {
            Id = "welcome"
            Title = "Welcome"
            Description =
                "This tour runs on sample data, so nothing you do here touches your real tables. Each step highlights one feature; hands-on steps advance once you try the interaction yourself, the rest move on with Next. Use the list on the right to jump around."
            TargetSelector = None
            Task = None
            Advance = TutorialAdvance.Manual
            Checkpoint = None
        }
        explain
            "layers"
            "Layer navigation"
            "The bar at the bottom pages through the tables of the provenance chain: arrows step to the previous or next layer, the dropdown jumps anywhere directly, and the Layer button adds a follow-up layer that continues from the current outputs."
            "[data-tutorial='provenance-layer-pagination']"
        explain
            "shelf"
            "Property shelf"
            "All properties known for this layer, grouped into index-card tabs per source table. The active card lists its properties and has its own small search; properties wait here until you pull them onto a side rail."
            "[data-tutorial='provenance-property-shelf']"
        explain
            "rails"
            "Property rails"
            "The left rail holds input-side properties, the right rail output-side ones. Rails start empty; properties you drop here become available for grouping and annotation. On narrow screens the rails fold behind 'Properties' toggles."
            inputRail
        {
            Id = "shelf-to-rail"
            Title = "Pull a property onto a rail"
            Description = "Dragging a property out of its shelf folder onto a rail activates it for that side."
            // Highlights the drag source (the Species shelf item, once its
            // folder is open) and the dropzone (rail or, on folded layouts,
            // its toggle) together; task steps keep the whole surface
            // interactive so the drag can travel between them.
            TargetSelector = Some $"button[aria-label='Drag Species'], {inputRail}"
            Task =
                Some
                    "Drag Species from the assay-table card in the shelf onto the dashed left rail (unfold the rail first if it is collapsed)."
            // Done once the Species button exists in the (sandboxed) rail.
            Advance =
                TutorialAdvance.OnCondition(fun container ->
                    container.querySelector speciesGroupButton |> isNull |> not
                )
            Checkpoint = Some "fresh-editor"
        }
        task
            "group"
            "Group by a property"
            "Clicking a rail property merges all entities that share a value into one card - four inputs become two species cards."
            speciesGroupButton
            "Click the Species property in the left rail to group the inputs by species."
            speciesGroupButton
            "species-on-rail"
        task
            "members"
            "Inspect group members"
            "Grouped cards summarize their members. Expanding a card lists every member with its own values and connection handles."
            "button[title='Show members']"
            "Click 'Show members' on one of the grouped cards."
            "button[title='Show members']"
            "species-grouped"
        task
            "values"
            "Property values"
            "A rail property expands into its distinct values. Drag a value chip onto a card to assign it to every member at once - or add brand-new values first."
            // The chevron only enters the layout while its row is hovered, so
            // the rail stays the fallback highlight until then.
            $"button[aria-label='Expand Species values'], {inputRail}"
            "Hover the Species property in the left rail, then click the chevron next to it to expand its values."
            "button[aria-label='Expand Species values']"
            "species-values"
        {
            Id = "connect"
            Title = "Connect inputs to outputs"
            Description =
                "The round handles on the facing edges of cards create input-to-output connections. Drag from handle to handle, or tap one and then the other."
            // Rings every card's connection handle, marking both ends of the
            // tap-tap (or drag) gesture.
            TargetSelector = Some "[data-provenance-connection-drop-id^='provenance-connection-drop|GroupCard|']"
            Task = Some "Tap the round handle on an input card, then tap the handle of an output card."
            Advance =
                TutorialAdvance.OnEvent(
                    "click",
                    "[data-provenance-connection-drop-id^='provenance-connection-drop|GroupCard|Output']"
                )
            Checkpoint = Some "species-connect"
        }
        explain
            "filters"
            "Search, sort and filter"
            "The toolbar narrows big models down: search properties and groups, sort by name or connection count, and filter by value coverage or origin."
            "[data-tutorial='provenance-filter-toolbar']"
        explain
            "undo"
            "Undo"
            "Every published edit can be taken back with one step - even after switching layers. The button is enabled whenever there is something to undo."
            "button[title='Undo last change']"
        explain
            "add-layer"
            "Continue the chain"
            "Select cards with their checkboxes and press 'Add layer': the selection seeds the inputs of the new layer, growing the provenance chain table by table."
            "button[title='Add layer']"
    |]

    // -- Checkpoint seeds ---------------------------------------------------
    // Seeding applies exactly the state transitions the earlier hands-on steps
    // would have produced (rail placement, grouping toggle), so a rebuilt
    // sandbox is indistinguishable from one the user worked through.

    let private speciesHeader =
        Fixtures.propertyHeader Fixtures.FixtureKinds.characteristicProperty "Species"

    let private withSpeciesOnInputRail (session: ProvenanceSession) (state: UiState) =
        let layer = Session.activeLayer session
        State.PropertyPlacement.place layer.Id ProvenanceSide.Input speciesHeader state

    let private withSpeciesGrouped (session: ProvenanceSession) (state: UiState) =
        let layer = Session.activeLayer session

        withSpeciesOnInputRail session state
        |> State.GroupingAssignments.toggleSide layer.InputSideId ProvenanceSide.Input speciesHeader

    /// Resolves the overlay's (inherited) checkpoint key to the sandbox seed to
    /// rebuild; unknown keys fall back to a fresh sample editor.
    let checkpointSeed (checkpoint: string option) : ProvenanceTutorialCheckpoint =
        match checkpoint with
        | Some "species-on-rail" -> {
            InitUiState = fun session -> State.init session |> withSpeciesOnInputRail session
            OpenRail = Some ProvenanceSide.Input
          }
        | Some "species-grouped"
        | Some "species-values"
        | Some "species-connect" -> {
            InitUiState = fun session -> State.init session |> withSpeciesGrouped session
            OpenRail = Some ProvenanceSide.Input
          }
        | _ -> {
            InitUiState = State.init
            OpenRail = None
          }

[<Erase; Mangle(false)>]
type ProvenanceTutorial =

    /// Full-screen tutorial: the interactive tour chrome wrapped around a
    /// sandboxed sample-data editor the host builds per checkpoint - the
    /// overlay remounts it whenever the active step's checkpoint changes.
    [<ReactComponent>]
    static member Modal(onClose: unit -> unit, renderEditor: string option -> ReactElement, ?debug: bool) =
        let debug = defaultArg debug false

        Html.div [
            prop.className "swt:fixed swt:inset-0 swt:z-50 swt:bg-base-200"
            if debug then
                prop.testId "provenance-tutorial-modal"
            prop.children [
                TutorialOverlay.Main(
                    ProvenanceTutorialSteps.all,
                    onClose,
                    renderEditor,
                    title = "Provenance editor tour",
                    debug = debug
                )
            ]
        ]
