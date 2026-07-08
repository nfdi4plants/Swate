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
/// always-rendered hooks only - `data-tutorial*` anchors and the
/// `data-provenance-*` measurement attributes - never on `title`/`aria-label`
/// copy, so rewording a button cannot silently break the tour.
/// The tour runs against the sample fixture session, so the task steps can name
/// concrete properties (Species) and predict which cards exist.
module ProvenanceTutorialSteps =

    // The Species rail button only exists once Species has been dragged out of
    // the shelf - which makes it both the drag step's success condition and
    // the group step's click target.
    let private speciesGroupButton = "button[data-tutorial-group-by='Input:Species']"

    // On medium/narrow layouts the input rail folds behind a toggle button;
    // the fallback keeps the spotlight meaningful there.
    let private inputRail =
        "[data-tutorial='provenance-rail-Input'], button[aria-label='Show input annotations']"

    // The Species drag source can be hidden two ways before the drag can even
    // start: the whole shelf minimized behind its toggle, or another folder
    // tab active. Highlight whichever control currently leads to the item, so
    // every click on the way to the drag is guided too.
    let private speciesShelfSource =
        "button[data-foldered-item-label='Species'], "
        + "[role='tab'][data-foldered-folder-label='assay-table'][aria-selected='false'], "
        + "[data-tutorial='provenance-property-shelf-toggle'][aria-expanded='false']"

    // Every input-side card: they are what visibly merges when grouping kicks
    // in, so the group step rings them alongside the button that causes it.
    let private inputGroupCards =
        "[data-provenance-group-node^='provenance-node::Input::']"

    let private outputGroupCards =
        "[data-provenance-group-node^='provenance-node::Output::']"

    // The undo button enables on the first published edit, which makes it the
    // success signal for free-form tasks whose outcome is an edit rather than
    // a single click (assigning a value, creating a connection).
    let private editPublished (container: Browser.Types.HTMLElement) =
        container.querySelector "[data-tutorial='provenance-undo']:enabled"
        |> isNull
        |> not

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
            "Annotation shelf"
            "All annotations known for this layer, grouped into index-card tabs per source table. The active card lists its annotations and has its own small search; annotations wait here until you pull them onto a side rail."
            "[data-tutorial='provenance-property-shelf']"
        explain
            "rails"
            "Annotation rails"
            "The left rail holds input-side annotations, the right rail output-side ones. Rails start empty; annotations you drop here become available for grouping and value assignment. On narrow screens the rails fold behind 'Annotations' toggles."
            inputRail
        {
            Id = "shelf-to-rail"
            Title = "Pull an annotation onto a rail"
            Description = "Dragging an annotation out of its shelf folder onto a rail activates it for that side."
            // Highlights the way to the drag source (shelf toggle, folder
            // tab, then the Species item itself) and the dropzone (rail or,
            // on folded layouts, its toggle) together; task steps keep the
            // whole surface interactive so the drag can travel between them.
            TargetSelector = Some $"{speciesShelfSource}, {inputRail}"
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
            "Group by an annotation"
            "Clicking a rail annotation merges all entities that share a value into one card - four inputs become two species cards."
            // The input cards are in the spotlight too: they are what reacts
            // to the click, merging from four cards into two.
            $"{speciesGroupButton}, {inputGroupCards}"
            "Click the Species annotation in the left rail to group the inputs by species."
            speciesGroupButton
            "species-on-rail"
        task
            "members"
            "Inspect group members"
            "Grouped cards summarize their members. Expanding a card lists every member with its own values and connection handles."
            // The member list only exists after the click; the polling
            // spotlight picks it up the moment it unfolds.
            "button[data-tutorial='provenance-show-members'], [data-tutorial='provenance-group-members']"
            "Click 'Show members' on one of the grouped cards."
            "button[data-tutorial='provenance-show-members']"
            "species-grouped"
        task
            "values"
            "Annotation values"
            "A rail annotation expands into its distinct values. Drag a value chip onto a card to assign it to every member at once - or add brand-new values first."
            // The rail stays the fallback highlight for folded layouts, where
            // the chevron does not exist until the rail is unfolded; the
            // values panel joins the spotlight once the chevron opens it, so
            // the chips the description talks about are ringed as they appear.
            $"button[data-tutorial-expand-values='Input:Species'], [data-tutorial-property-values='Input:Species'], {inputRail}"
            "Click the chevron next to the Species annotation in the left rail to expand its values."
            "button[data-tutorial-expand-values='Input:Species']"
            "species-values"
        {
            Id = "assign"
            Title = "Assign values to cards"
            Description =
                "Value chips are live: drop one onto a card and the value is assigned to every member of that card. Where a member already has a different value, a prompt asks before overwriting."
            // Rings the expanded value chips (the drag sources) and the output
            // cards - the drop targets without a species value, so the invited
            // drop applies cleanly instead of hitting the overwrite prompt.
            TargetSelector = Some $"[data-tutorial-property-values='Input:Species'], {outputGroupCards}"
            Task = Some "Drag one of the species value chips onto an output card - outputs have no species value yet."
            Advance = TutorialAdvance.OnCondition editPublished
            Checkpoint = Some "species-values-expanded"
        }
        {
            Id = "connect"
            Title = "Connect inputs to outputs"
            Description =
                "The round handles on the facing edges of cards create input-to-output connections; the dashed lines are connections the sample data already has. Drag from handle to handle, or tap one and then the other. Cards holding several members first ask how their members should pair up."
            // Rings only the handles of single-member cards: connecting two of
            // those publishes directly, so the invited gesture never runs into
            // the member pairing prompt (multi-member handles stay usable,
            // the prompt is just explained rather than spotlighted).
            TargetSelector =
                Some(
                    "[data-provenance-card-members='1'] "
                    + "[data-provenance-connection-drop-id^='provenance-connection-drop|GroupCard|']"
                )
            Task =
                Some
                    "Create a connection that does not exist yet - drag from the Chlamydomonas card's round handle to Output E's handle, or tap one and then the other."
            // Creating the connection is the only edit this step leads to - so
            // both the drag and the tap-tap gesture complete it, while merely
            // arming a handle (which publishes nothing) does not.
            Advance = TutorialAdvance.OnCondition editPublished
            Checkpoint = Some "species-connect"
        }
        explain
            "filters"
            "Search, sort and filter"
            "The toolbar narrows big models down: search annotations and groups, sort by name or connection count, and filter by value coverage or origin."
            "[data-tutorial='provenance-filter-toolbar']"
        explain
            "undo"
            "Undo"
            "Every published edit can be taken back with one step - even after switching layers. The button is enabled whenever there is something to undo."
            "[data-tutorial='provenance-undo']"
        explain
            "add-layer"
            "Continue the chain"
            "Select cards with their checkboxes and press 'Add layer': the selection seeds the inputs of the new layer, growing the provenance chain table by table."
            "[data-tutorial='provenance-add-layer']"
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

    let private withSpeciesValuesExpanded (session: ProvenanceSession) (state: UiState) =
        let layer = Session.activeLayer session

        withSpeciesGrouped session state
        |> State.PropertyExpansion.toggle layer.Id ProvenanceSide.Input speciesHeader

    /// Resolves the overlay's (inherited) checkpoint key to the sandbox seed to
    /// rebuild; "fresh-editor" (the shelf-to-rail step) and unknown keys both
    /// fall back to a fresh sample editor with no rail unfolded.
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
        | Some "species-values-expanded" -> {
            InitUiState = fun session -> State.init session |> withSpeciesValuesExpanded session
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
