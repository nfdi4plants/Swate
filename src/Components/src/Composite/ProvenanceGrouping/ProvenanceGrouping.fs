namespace Swate.Components.Composite.ProvenanceGrouping

open System.Globalization
open Fable.Core
open Feliz
open Swate.Components.JsBindings
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Composite.ProvenanceGrouping.Types

type private EditorLookups =
    {
        FindGroup: ProvenanceSide -> string -> DisplayGroup option
        FindHeader: string -> ProvenancePropertyHeader option
        FindPropertyValue: ProvenancePropertyValueId -> ProvenancePropertyValue option
        SourceForValue: ProvenancePropertyValueId -> ProvenancePropertyValue -> ValueAssignmentSource
    }

type private DragContext =
    {
        Session: ProvenanceSession
        Pair: ProvenanceLayerPair
        UiState: UiState
        Publish: SessionResult -> unit
        SetUiState: UiState -> unit
        Lookups: EditorLookups
        ConnectGroups: DisplayGroup -> DisplayGroup -> unit
    }

/// Resizable three-panel surface helpers.
module private Splitter =

    type SplitterSide =
        | Left
        | Right

    let template (ratios: PanelRatios) =
        let left = ratios.Left.ToString(CultureInfo.InvariantCulture)
        let middle = ratios.Middle.ToString(CultureInfo.InvariantCulture)
        let right = ratios.Right.ToString(CultureInfo.InvariantCulture)
        $"minmax(10rem, {left}fr) 0.75rem minmax(28rem, {middle}fr) 0.75rem minmax(10rem, {right}fr)"

    let testId side =
        match side with
        | Left -> "provenance-left-splitter"
        | Right -> "provenance-right-splitter"

    let handle side onPointerDown debug =
        Html.div [
            prop.className "swt:group swt:flex swt:min-h-full swt:cursor-col-resize swt:items-stretch swt:justify-center swt:rounded hover:swt:bg-base-300/60"
            prop.onPointerDown onPointerDown
            prop.style [ style.custom ("touch-action", "none") ]
            prop.custom ("role", "separator")
            prop.custom ("aria-orientation", "vertical")
            prop.ariaLabel "Resize provenance panels"
            if debug then prop.testId (testId side)
            prop.children [
                Html.div [
                    prop.className "swt:my-2 swt:w-1 swt:rounded-full swt:bg-base-content/15 swt:transition-colors group-hover:swt:bg-base-content/35"
                ]
            ]
        ]

/// Resolves drag payload ids and display ids against the active pair and UI palette.
module private EditorLookups =

    let create pair uiState inputGroups outputGroups =
        let findGroup side groupId =
            let groups : DisplayGroup list =
                if side = ProvenanceSide.Input then inputGroups else outputGroups
            groups |> List.tryFind (fun (group: DisplayGroup) -> group.Id = groupId)

        let findHeader headerId =
            PropertyRails.headersForModel pair.Model
            |> List.tryFind (fun header -> DragDrop.propertyHeaderIdentity header = headerId)

        let findPropertyValue propertyValueId =
            pair.Model.PropertyValues.TryFind propertyValueId
            |> Option.orElseWith (fun () -> State.Palette.tryFindValue propertyValueId uiState)

        let sourceForValue propertyValueId (propertyValue: ProvenancePropertyValue) : ValueAssignmentSource =
            {
                CopiedFrom =
                    if pair.Model.PropertyValues.ContainsKey propertyValueId then Some propertyValueId else None
                Header = propertyValue.Header
                Value = propertyValue.Value
                Unit = propertyValue.Unit
            }

        {
            FindGroup = findGroup
            FindHeader = findHeader
            FindPropertyValue = findPropertyValue
            SourceForValue = sourceForValue
        }

/// User-facing messages for value assignment planning failures.
module private AssignmentErrors =

    let text error =
        match error with
        | ValueAssignmentError.EmptyTarget -> "Drop a value onto a group with at least one entity."
        | ValueAssignmentError.MixedPropertyValueCounts header ->
            $"Cannot assign {header.Category.Name}: every target must either have no value or exactly one value for this property."
        | ValueAssignmentError.MultiplePropertyValues(header, setIds) ->
            let targets = setIds |> String.concat ", "
            $"Cannot overwrite {header.Category.Name}: {targets} already has multiple values for this property."

/// Session-changing actions that publish patches back to the host component.
module private EditorActions =

    let addLayer session pairId inputGroups outputGroups uiState publish =
        Display.layerCommand pairId inputGroups outputGroups uiState
        |> fun command -> Session.addLayer command session
        |> publish

    let createSet session publish command =
        Session.createLoadedSet command session
        |> publish

    let confirmPendingOverwrite session publish setUiState uiState warning =
        let rec apply current patches propertyValueIds =
            match propertyValueIds with
            | [] -> Ok(current, patches)
            | propertyValueId :: rest ->
                match Session.updatePropertyValue propertyValueId warning.Value warning.Unit current with
                | Ok(next, addedPatches) -> apply next (patches @ addedPatches) rest
                | Error error -> Error error

        match warning.ExistingValueIds with
        | [] ->
            setUiState { uiState with Error = Some "Cannot overwrite because the target value context is no longer available." }
        | propertyValueIds ->
            apply session [] propertyValueIds |> publish

    let connectGroups session publish inputGroup outputGroup =
        [
            for input in inputGroup.Members do
                for output in outputGroup.Members do
                    input.SetId, output.SetId
        ]
        |> List.fold
            (fun (result: SessionResult) (inputId, outputId) ->
                result
                |> Result.bind (fun (current, patches) ->
                    Session.connectSets inputId outputId None current
                    |> Result.map (fun (next, added) -> next, patches @ added)))
            (Ok(session, []))
        |> publish

/// DnD event handlers that translate library events into session or UI state changes.
module private DragHandlers =

    let handleStart setActiveDrag (event: DndKit.IDndKitEvent) =
        DragDrop.tryDragId (string event.active.id)
        |> setActiveDrag

    let private layerIdForSide pair side =
        match side with
        | ProvenanceSide.Input -> pair.LeftLayerId
        | ProvenanceSide.Output -> pair.RightLayerId

    let handleEnd context (event: DndKit.IDndKitEvent) =
        if isNull event.over then
            ()
        else
            let dragPayload = DragDrop.tryDragId (string event.active.id)
            let groupDrop = DragDrop.tryDropId (string event.over.id)
            let propertyDrop = DragDrop.tryPropertyDropId (string event.over.id)

            match dragPayload, groupDrop, propertyDrop with
            | Some(DragDrop.Payload.PropertyValue propertyValueId), Some(side, groupId), _ ->
                match context.Lookups.FindGroup side groupId, context.Lookups.FindPropertyValue propertyValueId with
                | Some group, Some propertyValue ->
                    match ValueAssignment.planPropertyValueDrop (context.Lookups.SourceForValue propertyValueId propertyValue) group context.Pair.Model with
                    | Ok(ValueAssignmentPlan.AddCurrent command) ->
                        Session.createCurrentLoadedPropertyValue command context.Session |> context.Publish
                    | Ok(ValueAssignmentPlan.ConfirmOverwrite warning) ->
                        State.Overwrite.set warning context.UiState |> context.SetUiState
                    | Error error ->
                        context.SetUiState { context.UiState with Error = Some(AssignmentErrors.text error) }
                | _ -> ()
            | Some(DragDrop.Payload.Group(ProvenanceSide.Input, inputGroupId)), Some(ProvenanceSide.Output, outputGroupId), _ ->
                match context.Lookups.FindGroup ProvenanceSide.Input inputGroupId, context.Lookups.FindGroup ProvenanceSide.Output outputGroupId with
                | Some inputGroup, Some outputGroup -> context.ConnectGroups inputGroup outputGroup
                | _ -> ()
            | Some(DragDrop.Payload.PropertyHeader(sourceSide, headerId)), _, Some targetSide when sourceSide <> targetSide ->
                match context.Lookups.FindHeader headerId with
                | Some header when PropertyRails.canSwitchHeader header context.Pair.Model ->
                    State.GroupingAssignments.move
                        context.Pair.Id
                        (layerIdForSide context.Pair sourceSide)
                        (layerIdForSide context.Pair targetSide)
                        targetSide
                        header
                        context.UiState
                    |> context.SetUiState
                | _ -> ()
            | _ -> ()

/// Alert and detail panels rendered around the main grouping surface.
module private EditorPanels =

    let errorAlert (error: string) =
        Html.div [
            prop.className "swt:alert swt:alert-error"
            prop.text error
        ]

    let overwriteWarning debug warning onConfirm onCancel =
        let count = warning.ExistingValueIds.Length
        let valueText = Formatting.formatValue warning.Value warning.Unit

        Html.div [
            prop.className "swt:alert swt:alert-warning swt:items-start"
            if debug then prop.testId "provenance-overwrite-warning"
            prop.children [
                Html.i [ prop.className "swt:iconify swt:fluent--warning-20-regular swt:size-5" ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.strong [
                            prop.text (
                                if count > 1 then
                                    $"Overwrite {count} {warning.Header.Category.Name} values?"
                                else
                                    $"Overwrite {warning.Header.Category.Name} value?")
                        ]
                        Html.span [
                            prop.className "swt:text-sm"
                            prop.text $"The selected targets already have {warning.Header.Category.Name}. Confirm to replace with {valueText} using the existing edit path."
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:ml-auto swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-warning swt:btn-sm"
                            if debug then prop.testId "provenance-confirm-overwrite"
                            prop.onPointerUp (fun _ -> onConfirm warning)
                            prop.onClick (fun _ -> onConfirm warning)
                            prop.text "Overwrite"
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-ghost swt:btn-sm"
                            prop.onClick (fun _ -> onCancel ())
                            prop.text "Cancel"
                        ]
                    ]
                ]
            ]
        ]

    let connectionDetails debug (connections: DisplayConnection list) detail =
        match detail with
        | Some(ProvenanceDetail.Connection connectionId) ->
            let resolved = connections |> List.tryFind (fun c -> c.Id = connectionId)
            match resolved with
            | Some conn ->
                Html.div [
                    prop.className "swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
                    if debug then prop.testId "provenance-connection-details"
                    prop.children [
                        Html.h3 [
                            prop.className "swt:font-semibold swt:text-primary"
                            prop.text $"Connection: {connectionId}"
                        ]
                        Html.p [
                            prop.className "swt:text-sm"
                            prop.text $"Source: {conn.SourceGroupId}"
                        ]
                        Html.p [
                            prop.className "swt:text-sm"
                            prop.text $"Target: {conn.TargetGroupId}"
                        ]
                        let connectionIds = conn.ConnectionIds |> String.concat ", "
                        Html.p [
                            prop.className "swt:text-sm"
                            prop.text $"Connection IDs: {connectionIds}"
                        ]
                    ]
                ]
            | None -> Html.none
        | _ -> Html.none

/// Render helpers for side rails, group columns, and drag overlays.
module private EditorSurface =

    let propertyRail side (pair: ProvenanceLayerPair) (model: ProvenanceModel) uiState activeAssignments toggleSide toggleBoth move toggleExpanded addPaletteValue debug =
        Controls.PropertyRail(
            side,
            PropertyRails.propertyRailHeadersForSide pair.Id side model uiState,
            activeAssignments,
            (fun header -> PropertyRails.propertyValuesForSideHeader pair.Id side header model uiState),
            (fun header -> State.PropertyExpansion.isExpanded pair.Id side header uiState),
            toggleSide,
            toggleBoth,
            move,
            toggleExpanded,
            addPaletteValue,
            (fun header -> PropertyRails.canSwitchHeader header model),
            debug = debug)

    let groupColumn side (pair: ProvenanceLayerPair) model (groups: DisplayGroup list) endpointKind createSet uiState toggleSelection toggleDetail debug =
        let keyPrefix =
            match side with
            | ProvenanceSide.Input -> "Input"
            | ProvenanceSide.Output -> "Output"

        Html.div [
            prop.className "swt:flex swt:min-w-0 swt:flex-col swt:gap-3"
            prop.children [
                for group in groups do
                    GroupCard.Main(
                        side,
                        group,
                        model,
                        State.Selection.contains pair.Id side group.Id uiState,
                        State.Detail.isGroupExpanded side group.Id uiState,
                        (fun () -> toggleSelection side group.Id),
                        (fun () -> toggleDetail side group.Id),
                        debug = debug,
                        key = $"{keyPrefix}:{group.Id}")
                if groups.IsEmpty then
                    Html.p [
                        prop.className "swt:text-sm swt:text-base-content/60"
                        prop.text "No entries in this layer"
                    ]
                    Controls.AddEndpointPopover(
                        side,
                        endpointKind,
                        createSet,
                        debug = debug,
                        key = $"{pair.Id}:{keyPrefix}:{Endpoints.endpointKindIdentity endpointKind}")
            ]
        ]

    let dragOverlay findPropertyValue debug activeDrag =
        match activeDrag with
        | Some(DragDrop.Payload.PropertyValue propertyValueId) ->
            match findPropertyValue propertyValueId with
            | Some propertyValue ->
                Controls.ValueDragPreview(propertyValue, showHeader = false, debug = debug)
            | None -> Html.none
        | _ -> Html.none

[<Erase; Mangle(false)>]
type ProvenanceGrouping =

    [<ReactComponent>]
    static member Main(session: ProvenanceSession, onChange: ProvenanceEditorChange -> unit, ?height: int, ?debug: bool) =
        let debug = defaultArg debug false
        let rawUiState, setUiState = React.useState (State.init session)
        let activeDrag, setActiveDrag = React.useState<DragDrop.Payload option> None
        let surfaceRef = React.useElementRef ()
        let splitDrag = React.useRef (None: Splitter.SplitterSide option)

        let uiState = State.Layers.ensure session rawUiState
        let pair, inputGroups, outputGroups, connections = Display.displayPair session uiState
        let latestUiState = React.useRef uiState
        let activePairId = React.useRef pair.Id
        latestUiState.current <- uiState
        activePairId.current <- pair.Id
        let panelRatios = State.PanelLayout.get pair.Id uiState
        let inputEndpointKind = Endpoints.defaultEndpointKind ProvenanceSide.Input pair.Model
        let outputEndpointKind = Endpoints.defaultEndpointKind ProvenanceSide.Output pair.Model
        let lookups = EditorLookups.create pair uiState inputGroups outputGroups

        let commitPanelRatio side clientX =
            match surfaceRef.current with
            | None -> ()
            | Some surface ->
                let rect = surface.getBoundingClientRect ()
                let rawPercent =
                    if rect.width <= 0. then
                        0.
                    else
                        ((clientX - rect.left) / rect.width) * 100.

                let splitPercent =
                    rawPercent
                    |> max 0.
                    |> min 100.
                    |> round
                    |> int

                let current = latestUiState.current
                let next =
                    match side with
                    | Splitter.Left -> State.PanelLayout.setLeft activePairId.current splitPercent current
                    | Splitter.Right -> State.PanelLayout.setRight activePairId.current (100 - splitPercent) current

                latestUiState.current <- next
                setUiState next

        React.useEffectOnce (fun () ->
            let onMove =
                fun (event: Browser.Types.PointerEvent) ->
                    match splitDrag.current with
                    | Some side -> commitPanelRatio side event.clientX
                    | None -> ()

            let stopDragging =
                fun (_: Browser.Types.PointerEvent) ->
                    splitDrag.current <- None

            Browser.Dom.document.addEventListener ("pointermove", unbox onMove)
            Browser.Dom.document.addEventListener ("pointerup", unbox stopDragging)
            Browser.Dom.document.addEventListener ("pointercancel", unbox stopDragging)

            FsReact.createDisposable (fun () ->
                Browser.Dom.document.removeEventListener ("pointermove", unbox onMove)
                Browser.Dom.document.removeEventListener ("pointerup", unbox stopDragging)
                Browser.Dom.document.removeEventListener ("pointercancel", unbox stopDragging)
            )
        )

        let startPanelDrag side (event: Browser.Types.PointerEvent) =
            event.preventDefault ()
            splitDrag.current <- Some side
            commitPanelRatio side event.clientX

        let pointerSensor =
            DndKit.useSensor (
                DndKit.PointerSensor,
                {|
                    activationConstraint = {| distance = 6 |}
                |}
            )
        let sensors = DndKit.useSensors [| pointerSensor |]

        let publish result =
            match result with
            | Ok(next, patches) ->
                let nextUiState = State.Layers.ensure next uiState
                setUiState { nextUiState with Error = None; PendingOverwrite = None }
                onChange { Session = next; Patches = patches }
            | Error error ->
                setUiState { uiState with Error = Some(string error) }

        let createSet command =
            EditorActions.createSet session publish command

        let addPaletteValue side header value unit =
            State.Palette.addValue pair.Id side header value unit uiState
            |> setUiState

        let toggleSideGrouping layerId side header =
            State.GroupingAssignments.toggleSide layerId side header uiState
            |> setUiState

        let togglePropertyExpanded side header =
            State.PropertyExpansion.toggle pair.Id side header uiState
            |> setUiState

        let toggleSelection side groupId =
            State.Selection.toggle pair.Id side groupId uiState
            |> setUiState

        let toggleGroupDetail side groupId =
            State.Detail.toggleGroup side groupId uiState
            |> setUiState

        let confirmPendingOverwrite =
            EditorActions.confirmPendingOverwrite session publish setUiState uiState

        let connectGroups =
            EditorActions.connectGroups session publish

        let dragContext =
            {
                Session = session
                Pair = pair
                UiState = uiState
                Publish = publish
                SetUiState = setUiState
                Lookups = lookups
                ConnectGroups = connectGroups
            }

        let content =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-4 swt:bg-base-200 swt:p-4 swt:overflow-auto"
                prop.style [ style.height (defaultArg height 720) ]
                if debug then prop.testId "provenance-editor-root"
                prop.children [
                    Controls.LayerTabs(
                        session,
                        (fun pairId -> Session.selectPair pairId session |> publish),
                        (fun () -> EditorActions.addLayer session pair.Id inputGroups outputGroups uiState publish),
                        debug = debug)
                    match uiState.Error with
                    | Some error -> EditorPanels.errorAlert error
                    | None -> Html.none
                    match uiState.PendingOverwrite with
                    | Some warning ->
                        EditorPanels.overwriteWarning
                            debug
                            warning
                            confirmPendingOverwrite
                            (fun () -> State.Overwrite.clear uiState |> setUiState)
                    | None -> Html.none
                    Html.div [
                        prop.ref surfaceRef
                        prop.className "swt:relative swt:grid swt:min-w-0 swt:items-start"
                        prop.style [
                            style.custom ("grid-template-columns", Splitter.template panelRatios)
                        ]
                        if debug then prop.testId "provenance-surface"
                        prop.children [
                            ConnectorOverlay.Main(
                                surfaceRef,
                                connections,
                                (fun connection -> State.Detail.showConnection connection.Id uiState |> setUiState),
                                debug = debug)
                            Html.div [
                                prop.className "swt:@container/provenancePanel swt:min-w-0 swt:overflow-hidden"
                                prop.children [
                                    EditorSurface.propertyRail
                                        ProvenanceSide.Input
                                        pair
                                        pair.Model
                                        uiState
                                        (State.Layers.get pair.LeftLayerId uiState).GroupingAssignments
                                        (fun header -> toggleSideGrouping pair.LeftLayerId ProvenanceSide.Input header)
                                        (fun header -> State.GroupingAssignments.toggleBoth pair.LeftLayerId pair.RightLayerId header uiState |> setUiState)
                                        (fun header -> State.GroupingAssignments.move pair.Id pair.LeftLayerId pair.RightLayerId ProvenanceSide.Output header uiState |> setUiState)
                                        (fun header -> togglePropertyExpanded ProvenanceSide.Input header)
                                        (fun header value unit -> addPaletteValue ProvenanceSide.Input header value unit)
                                        debug
                                ]
                            ]
                            Splitter.handle Splitter.Left (startPanelDrag Splitter.Left) debug
                            Html.div [
                                prop.className "swt:@container/provenancePanel swt:grid swt:min-w-0 swt:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] swt:items-start swt:gap-4"
                                prop.children [
                                    EditorSurface.groupColumn
                                        ProvenanceSide.Input
                                        pair
                                        pair.Model
                                        inputGroups
                                        inputEndpointKind
                                        createSet
                                        uiState
                                        toggleSelection
                                        toggleGroupDetail
                                        debug
                                    EditorSurface.groupColumn
                                        ProvenanceSide.Output
                                        pair
                                        pair.Model
                                        outputGroups
                                        outputEndpointKind
                                        createSet
                                        uiState
                                        toggleSelection
                                        toggleGroupDetail
                                        debug
                                ]
                            ]
                            Splitter.handle Splitter.Right (startPanelDrag Splitter.Right) debug
                            Html.div [
                                prop.className "swt:@container/provenancePanel swt:min-w-0 swt:overflow-hidden"
                                prop.children [
                                    EditorSurface.propertyRail
                                        ProvenanceSide.Output
                                        pair
                                        pair.Model
                                        uiState
                                        (State.Layers.get pair.RightLayerId uiState).GroupingAssignments
                                        (fun header -> toggleSideGrouping pair.RightLayerId ProvenanceSide.Output header)
                                        (fun header -> State.GroupingAssignments.toggleBoth pair.LeftLayerId pair.RightLayerId header uiState |> setUiState)
                                        (fun header -> State.GroupingAssignments.move pair.Id pair.RightLayerId pair.LeftLayerId ProvenanceSide.Input header uiState |> setUiState)
                                        (fun header -> togglePropertyExpanded ProvenanceSide.Output header)
                                        (fun header value unit -> addPaletteValue ProvenanceSide.Output header value unit)
                                        debug
                                ]
                            ]
                        ]
                    ]
                    EditorPanels.connectionDetails debug connections uiState.Detail
                ]
            ]

        DndKit.DndContext(
            sensors = sensors,
            collisionDetection = DndKit.pointerWithin,
            onDragStart = DragHandlers.handleStart setActiveDrag,
            onDragCancel = (fun _ -> setActiveDrag None),
            onDragEnd = (fun event ->
                setActiveDrag None
                DragHandlers.handleEnd dragContext event),
            children =
                React.Fragment [
                    content
                    DndKit.DragOverlay(children = EditorSurface.dragOverlay lookups.FindPropertyValue debug activeDrag)
                ]
        )

    [<ReactComponent>]
    static member Editor(initialModel: ProvenanceModel, onChange: ProvenanceEditorChange -> unit, ?height: int, ?debug: bool) =
        let session, setSession = React.useState (Session.init initialModel)
        let change (next: ProvenanceEditorChange) =
            setSession next.Session
            onChange next
        ProvenanceGrouping.Main(session, change, ?height = height, ?debug = debug)
