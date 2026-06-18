namespace Swate.Components.Page.ProvenanceGrouping

open System.Globalization
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.JsBindings
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Page.ProvenanceGrouping.Types

type private EditorLookups = {
    FindGroup: ProvenanceSide -> string -> DisplayGroup option
    FindHeader: string -> ProvenancePropertyHeader option
    FindPropertyValue: ProvenancePropertyValueId -> ProvenancePropertyValue option
    SourceForValue: ProvenancePropertyValueId -> ProvenancePropertyValue -> ValueAssignmentSource
}

type private DragContext = {
    Session: ProvenanceSession
    Layer: ProvenanceLayer
    UiState: UiState
    Publish: SessionResult -> unit
    SetUiState: UiState -> unit
    Lookups: EditorLookups
    ConnectSetPairs: (ProvenanceSetId * ProvenanceSetId) list -> unit
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
        // The splitter tracks double as connector gutters; their generous fixed width
        // keeps readable space between the rails and the cards their connectors attach to.
        $"minmax(10rem, {left}fr) 4rem minmax(28rem, {middle}fr) 4rem minmax(10rem, {right}fr)"

    let testId side =
        match side with
        | Left -> "provenance-left-splitter"
        | Right -> "provenance-right-splitter"

    let handle side onPointerDown debug =
        Html.div [
            prop.className
                "swt:group swt:flex swt:min-h-full swt:cursor-col-resize swt:items-stretch swt:justify-center swt:rounded hover:swt:bg-base-300/60"
            prop.onPointerDown onPointerDown
            prop.style [ style.custom ("touchAction", "none") ]
            prop.custom ("role", "separator")
            prop.custom ("aria-orientation", "vertical")
            prop.ariaLabel "Resize provenance panels"
            if debug then
                prop.testId (testId side)
            prop.children [
                Html.div [
                    prop.className
                        "swt:my-2 swt:w-1 swt:rounded-full swt:bg-base-content/15 swt:transition-colors group-hover:swt:bg-base-content/35"
                ]
            ]
        ]

/// Width tiers of the editor root that drive the responsive layout variants.
[<RequireQualifiedAccess>]
type private LayoutTier =
    | Wide
    | Medium
    | Narrow

module private LayoutTier =

    let forWidth (width: float) =
        if width < 640. then LayoutTier.Narrow
        elif width < 1024. then LayoutTier.Medium
        else LayoutTier.Wide

/// Thin ResizeObserver binding used to derive the layout tier from the editor width.
module private TierObserver =

    [<Emit("new ResizeObserver(() => $0())")>]
    let create (callback: unit -> unit) : obj = jsNative

    [<Emit("$0.observe($1)")>]
    let observe (observer: obj) (target: Browser.Types.HTMLElement) : unit = jsNative

    [<Emit("$0.disconnect()")>]
    let disconnect (observer: obj) : unit = jsNative

/// Resolves drag payload ids and display ids against the active layer and UI palette.
module private EditorLookups =

    let create layer uiState inputGroups outputGroups =
        let findGroup side groupId =
            let groups: DisplayGroup list =
                if side = ProvenanceSide.Input then
                    inputGroups
                else
                    outputGroups

            groups |> List.tryFind (fun (group: DisplayGroup) -> group.Id = groupId)

        let findHeader headerId =
            PropertyRails.headersForModel layer.Model
            |> List.tryFind (fun header -> DragDrop.propertyHeaderIdentity header = headerId)

        let findPropertyValue propertyValueId =
            layer.Model.PropertyValues.TryFind propertyValueId
            |> Option.orElseWith (fun () -> State.Palette.tryFindValue propertyValueId uiState)

        let sourceForValue propertyValueId (propertyValue: ProvenancePropertyValue) : ValueAssignmentSource = {
            CopiedFrom =
                if layer.Model.PropertyValues.ContainsKey propertyValueId then
                    Some propertyValueId
                else
                    None
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

    let addLayer session layerId inputGroups outputGroups uiState publish =
        Display.layerCommand layerId inputGroups outputGroups uiState
        |> fun command -> Session.addLayer command session
        |> publish

    let createSet session publish command =
        Session.createLoadedSet command session |> publish

    let private applyOverwrite result warning =
        warning.ExistingValueIds
        |> List.distinct
        |> List.fold
            (fun result propertyValueId ->
                result
                |> Result.bind (fun (currentSession, patches) ->
                    Session.updatePropertyValue propertyValueId warning.Value warning.Unit currentSession
                    |> Result.map (fun (next, added) -> next, patches @ added)
                )
            )
            result

    let private applyAdd result command =
        result
        |> Result.bind (fun (currentSession, patches) ->
            Session.createLoadedPropertyValue command currentSession
            |> Result.map (fun (next, added) -> next, patches @ added)
        )

    let applyAssignmentBatch session publish batch =
        batch.Overwrites
        |> List.fold applyOverwrite (Ok(session, []))
        |> Result.bind (fun afterOverwrites -> batch.Adds |> List.fold applyAdd (Ok afterOverwrites))
        |> publish

    let connectSetPairs session publish pairs =
        pairs
        |> List.distinct
        |> List.fold
            (fun (result: SessionResult) (inputId, outputId) ->
                result
                |> Result.bind (fun (current, patches) ->
                    Session.connectSets inputId outputId None current
                    |> Result.map (fun (next, added) -> next, patches @ added)
                )
            )
            (Ok(session, []))
        |> publish

    let orderedMemberPairs (inputGroup: DisplayGroup) (outputGroup: DisplayGroup) =
        if inputGroup.Members.Length = outputGroup.Members.Length then
            List.zip inputGroup.Members outputGroup.Members
            |> List.map (fun (input, output) -> input.SetId, output.SetId)
            |> Some
        else
            None

    let allMemberPairs (inputGroup: DisplayGroup) (outputGroup: DisplayGroup) = [
        for input in inputGroup.Members do
            for output in outputGroup.Members do
                input.SetId, output.SetId
    ]

/// Reads the current DOM position of a connection handle relative to the editor surface.
module private HandleMeasure =

    let private tryDocumentNode (handle: ConnectionHandleRef) =
        let node: Browser.Types.HTMLElement =
            !!Browser.Dom.document.querySelector($"[data-provenance-connection-node='{DragDrop.connectionHandleNodeId handle}']")

        if isNull node then None else Some node

    let tryCenter (surface: Browser.Types.HTMLElement) (handle: ConnectionHandleRef) =
        match tryDocumentNode handle with
        | Some node ->
            let origin = surface.getBoundingClientRect ()
            let rect = node.getBoundingClientRect ()

            Some {
                X = rect.left - origin.left + float surface.scrollLeft + rect.width / 2.
                Y = rect.top - origin.top + float surface.scrollTop + rect.height / 2.
            }
        | None -> None

    let tryViewportCenter (handle: ConnectionHandleRef) =
        tryDocumentNode handle
        |> Option.map (fun node ->
            let rect = node.getBoundingClientRect ()

            {
                X = rect.left + rect.width / 2.
                Y = rect.top + rect.height / 2.
            }
        )

/// Resolves destination handles from final pointer coordinates when DnD reports
/// the active handle as its own drop target.
module private DropHitTesting =

    [<Emit("document.elementsFromPoint($0, $1)")>]
    let private elementsFromPoint (_x: float) (_y: float) : Browser.Types.HTMLElement[] = jsNative

    [<Emit("$0.closest($1)")>]
    let private closest (_element: Browser.Types.HTMLElement) (_selector: string) : Browser.Types.HTMLElement = jsNative

    let private attribute name (element: Browser.Types.HTMLElement) =
        let value = element.getAttribute name
        if isNull value then None else Some value

    let private closestAttribute selector attributeName (element: Browser.Types.HTMLElement) =
        let node = closest element selector
        if isNull node then None else attribute attributeName node

    let private endpoint source (event: DndKit.IDndKitEvent) =
        HandleMeasure.tryViewportCenter source
        |> Option.map (fun start -> {
            X = start.X + event.delta.x
            Y = start.Y + event.delta.y
        })

    let private targetHandleAt point source =
        elementsFromPoint point.X point.Y
        |> Array.tryPick (fun element ->
            closestAttribute "[data-provenance-connection-drop-id]" "data-provenance-connection-drop-id" element
            |> Option.bind DragDrop.tryConnectionDropId
            |> Option.bind (fun target -> if target = source then None else Some target)
        )

    let connectionTarget source event =
        endpoint source event |> Option.bind (fun point -> targetHandleAt point source)

/// DnD event handlers that translate library events into session or UI state changes.
module private DragHandlers =

    let handleStart
        (surfaceRef: IRefValue<Browser.Types.HTMLElement option>)
        setActiveDrag
        (liveDragStore: LiveDrag.Store)
        (event: DndKit.IDndKitEvent)
        =
        let payload = DragDrop.tryDragId (string event.active.id)
        setActiveDrag payload

        match payload, surfaceRef.current with
        | Some(DragDrop.Payload.ConnectionHandle handle), Some surface ->
            HandleMeasure.tryCenter surface handle
            |> Option.iter (fun point -> LiveDrag.start handle point liveDragStore)
        | _ -> ()

    let handleMove (liveDragStore: LiveDrag.Store) (event: DndKit.IDndKitMoveEvent) =
        match liveDragStore.Current with
        | Some live ->
            LiveDrag.moveTo
                {
                    X = live.Start.X + event.delta.x
                    Y = live.Start.Y + event.delta.y
                }
                liveDragStore
        | None -> ()

    let private layerIdForSide (layer: ProvenanceLayer) side =
        match side with
        | ProvenanceSide.Input -> layer.InputSideId
        | ProvenanceSide.Output -> layer.OutputSideId

    let private routePropertyValueDrop context side groupId propertyValueId =
        match context.Lookups.FindPropertyValue propertyValueId with
        | Some propertyValue ->
            let targetGroups =
                ValueAssignment.selectedTargetGroupsForDrop
                    context.Layer.Id
                    side
                    groupId
                    context.UiState.SelectedInputs
                    context.UiState.SelectedOutputs
                    context.Lookups.FindGroup

            match
                ValueAssignment.planPropertyValueDropToGroups
                    (context.Lookups.SourceForValue propertyValueId propertyValue)
                    targetGroups
                    context.Layer.Model
            with
            | Ok batch ->
                let affectedValueCount =
                    batch.Overwrites |> List.sumBy (fun w -> w.ExistingValueIds.Length)

                let sideForTarget target =
                    match target with
                    | ProvenancePropertyTarget.InputSets _ -> Some ProvenanceSide.Input
                    | ProvenancePropertyTarget.OutputSets _ -> Some ProvenanceSide.Output
                    | ProvenancePropertyTarget.Connections _ -> None

                let affectedSideCount =
                    [
                        yield! batch.Adds |> List.choose (fun command -> sideForTarget command.Target)
                        yield! batch.Overwrites |> List.choose (fun warning -> sideForTarget warning.Target)
                    ]
                    |> List.distinct
                    |> List.length

                if batch.Overwrites.IsEmpty then
                    if not batch.Adds.IsEmpty then
                        batch.Adds
                        |> List.fold
                            (fun (result: SessionResult) cmd ->
                                match result with
                                | Ok(current, patches) ->
                                    Session.createCurrentLoadedPropertyValue cmd current
                                    |> Result.map (fun (next, added) -> next, patches @ added)
                                | Error _ -> result
                            )
                            (Ok(context.Session, []))
                        |> context.Publish
                else
                    let pendingBatch = {
                        Batch = batch
                        AffectedSideCount = affectedSideCount
                        AffectedValueCount = affectedValueCount
                    }

                    State.AssignmentBatch.set pendingBatch context.UiState |> context.SetUiState
            | Error error ->
                context.SetUiState {
                    context.UiState with
                        Error = Some(AssignmentErrors.text error)
                }
        | _ -> ()

    let private routeGroupConnection context inputGroupId outputGroupId =
        match
            context.Lookups.FindGroup ProvenanceSide.Input inputGroupId,
            context.Lookups.FindGroup ProvenanceSide.Output outputGroupId
        with
        | Some inputGroup, Some outputGroup ->
            match EditorActions.orderedMemberPairs inputGroup outputGroup with
            | Some pairs -> context.ConnectSetPairs pairs
            | None ->
                State.MemberResolution.request
                    {
                        LayerId = context.Layer.Id
                        InputGroupId = inputGroup.Id
                        OutputGroupId = outputGroup.Id
                        InputMemberCount = inputGroup.Members.Length
                        OutputMemberCount = outputGroup.Members.Length
                    }
                    context.UiState
                |> context.SetUiState
        | _ -> ()

    let private routeMemberToGroupConnection context inputGroupId outputGroupId memberSetId memberSide =
        match
            context.Lookups.FindGroup ProvenanceSide.Input inputGroupId,
            context.Lookups.FindGroup ProvenanceSide.Output outputGroupId
        with
        | Some inputGroup, Some outputGroup ->
            let pairs =
                match memberSide with
                | ProvenanceSide.Input -> outputGroup.Members |> List.map (fun output -> memberSetId, output.SetId)
                | ProvenanceSide.Output -> inputGroup.Members |> List.map (fun input -> input.SetId, memberSetId)

            context.ConnectSetPairs pairs
        | _ -> ()

    let private routeConnectionHandle context source target =
        match ConnectionRouting.action source target with
        | Some(ConnectionRouting.ConnectionAction.ConnectGroups(inputGroupId, outputGroupId)) ->
            routeGroupConnection context inputGroupId outputGroupId
        | Some(ConnectionRouting.ConnectionAction.ConnectMembers(_, _, inputSetId, outputSetId)) ->
            context.ConnectSetPairs [ inputSetId, outputSetId ]
        | Some(ConnectionRouting.ConnectionAction.ConnectMemberToGroup(inputGroupId,
                                                                       outputGroupId,
                                                                       memberSetId,
                                                                       memberSide)) ->
            routeMemberToGroupConnection context inputGroupId outputGroupId memberSetId memberSide
        | None -> ()

    let private routeExistingValueAndPropertyDrags context dragPayload groupDrop propertyDrop =
        match dragPayload, groupDrop, propertyDrop with
        | Some(DragDrop.Payload.PropertyValue propertyValueId), Some(side, groupId), _ ->
            routePropertyValueDrop context side groupId propertyValueId
        | Some(DragDrop.Payload.PropertyHeader(sourceSide, headerId)), _, Some targetSide when sourceSide <> targetSide ->
            match context.Lookups.FindHeader headerId with
            | Some header when PropertyRails.canSwitchHeader header context.Layer.Model ->
                State.GroupingAssignments.move
                    context.Layer.Id
                    (layerIdForSide context.Layer sourceSide)
                    (layerIdForSide context.Layer targetSide)
                    targetSide
                    header
                    context.UiState
                |> context.SetUiState
            | _ -> ()
        | _ -> ()

    let handleEnd context (event: DndKit.IDndKitEvent) =
        let dragPayload = DragDrop.tryDragId (string event.active.id)

        let groupDrop, propertyDrop, connectionDrop =
            if isNull event.over then
                None, None, None
            else
                DragDrop.tryDropId (string event.over.id),
                DragDrop.tryPropertyDropId (string event.over.id),
                DragDrop.tryConnectionDropId (string event.over.id)

        match dragPayload, connectionDrop with
        | Some(DragDrop.Payload.ConnectionHandle source), Some target ->
            let resolvedTarget =
                if target = source then
                    DropHitTesting.connectionTarget source event
                else
                    Some target

            resolvedTarget |> Option.iter (routeConnectionHandle context source)
        | Some(DragDrop.Payload.ConnectionHandle source), None ->
            DropHitTesting.connectionTarget source event
            |> Option.iter (routeConnectionHandle context source)
        | _ -> routeExistingValueAndPropertyDrags context dragPayload groupDrop propertyDrop

/// Alert and detail panels rendered around the main grouping surface.
module private EditorPanels =

    let errorAlert (error: string) =
        Html.div [
            prop.className "swt:alert swt:alert-error"
            prop.text error
        ]

    let assignmentBatchWarning debug (pending: PendingAssignmentBatch) onConfirm onCancel =
        let overwriteCount = pending.AffectedValueCount
        let sideCount = pending.AffectedSideCount

        let headerText =
            pending.Batch.Overwrites
            |> List.tryHead
            |> Option.map (fun w -> w.Header.Category.Name)
            |> Option.defaultValue "property"

        let valueText =
            pending.Batch.Overwrites
            |> List.tryHead
            |> Option.map (fun w -> Formatting.formatValue w.Value w.Unit)
            |> Option.defaultValue "new value"

        Html.div [
            prop.className "swt:alert swt:alert-warning swt:flex-wrap swt:items-start"
            if debug then
                prop.testId "provenance-overwrite-warning"
            prop.children [
                Html.i [
                    prop.className "swt:iconify swt:fluent--warning-20-regular swt:size-5"
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.strong [
                            prop.text (
                                if overwriteCount > 1 then
                                    $"Overwrite {overwriteCount} {headerText} values?"
                                else
                                    $"Overwrite {headerText} value?"
                            )
                        ]
                        Html.span [
                            prop.className "swt:text-sm"
                            prop.text
                                $"The selected targets already have {headerText}. Confirm to replace with {valueText} across {sideCount} side(s) using the existing edit path."
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:ml-auto swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-warning swt:btn-sm"
                            if debug then
                                prop.testId "provenance-confirm-overwrite"
                            prop.onPointerUp (fun _ -> onConfirm pending)
                            prop.onClick (fun _ -> onConfirm pending)
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

    let memberResolutionPrompt debug (pending: PendingMemberResolution) onAllToAll onManual onCancel =
        let memberText count side =
            if count = 1 then
                $"{count} {side} member"
            else
                $"{count} {side} members"

        let inputMemberText = memberText pending.InputMemberCount "input"
        let outputMemberText = memberText pending.OutputMemberCount "output"

        Html.div [
            prop.className "swt:alert swt:alert-warning swt:flex-wrap swt:items-start"
            if debug then
                prop.testId "provenance-member-resolution-prompt"
            prop.children [
                Html.i [
                    prop.className "swt:iconify swt:fluent--text-paragraph-24-regular swt:size-5"
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.strong "Resolve member mismatch"
                        Html.span [
                            prop.className "swt:text-sm"
                            prop.text $"This connection has {inputMemberText} and {outputMemberText}."
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:ml-auto swt:flex swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-warning swt:btn-sm"
                            prop.ariaLabel "Create all-to-all connections"
                            if debug then
                                prop.testId "provenance-member-resolution-all-to-all"
                            prop.onClick (fun _ -> onAllToAll pending)
                            prop.text "All-to-all"
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-outline swt:btn-sm"
                            prop.ariaLabel "Resolve manually"
                            if debug then
                                prop.testId "provenance-member-resolution-manual"
                            prop.onPointerUp (fun _ -> onManual pending)
                            prop.onClick (fun _ -> onManual pending)
                            prop.text "Resolve manually"
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-ghost swt:btn-sm"
                            prop.ariaLabel "Cancel member resolution"
                            if debug then
                                prop.testId "provenance-member-resolution-cancel"
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
                    prop.className
                        "swt:mx-4 swt:mt-4 swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
                    if debug then
                        prop.testId "provenance-connection-details"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:grow swt:font-semibold swt:text-primary"
                                    prop.text $"Connection: {connectionId}"
                                ]
                            ]
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

    let propertyRail
        side
        sideId
        (projection: PropertyRails.RailProjection)
        activeAssignments
        toggleSide
        toggleBoth
        move
        toggleExpanded
        addPaletteValue
        setPropertyColor
        sourceInfoForValue
        debug
        setIsValueChipDragging

        =
        Controls.PropertyRail(
            side,
            projection.Headers,
            activeAssignments,
            (fun header -> projection.ValuesByHeader |> Map.tryFind header |> Option.defaultValue []),
            (fun header -> projection.ExpandedHeaders.Contains header),
            toggleSide,
            toggleBoth,
            move,
            toggleExpanded,
            addPaletteValue,
            (fun header -> projection.CanSwitchHeaders.Contains header),
            setIsValueChipDragging,
            (fun header -> projection.StatsByHeader |> Map.tryFind header),
            (fun header -> projection.BadgeByHeader |> Map.tryFind header),
            (fun header -> projection.ColorByHeader |> Map.tryFind header |> Option.bind id),
            (fun header -> projection.OriginByHeader |> Map.tryFind header),
            setPropertyColor,
            sourceInfoForValue,
            sideId = sideId,
            debug = debug
        )

    let groupColumn
        side
        (layer: ProvenanceLayer)
        model
        (groups: DisplayGroup list)
        endpointKinds
        existingEndpointNames
        createSet
        uiState
        isExpanded
        toggleSelection
        toggleDetail
        (connectionCountFor: string -> int option)
        sourceInfoForValue
        debug
        isValueChipDragging
        =
        let keyPrefix =
            match side with
            | ProvenanceSide.Input -> "Input"
            | ProvenanceSide.Output -> "Output"

        let endpointKindsKey =
            endpointKinds |> List.map Endpoints.endpointKindIdentity |> String.concat "|"

        Html.div [
            prop.className [
                "swt:@container/provenancePanel swt:flex swt:min-w-0 swt:flex-col swt:gap-3"
                // Fit-content cards hug the column edge facing their property rail, so
                // the space between the two card columns stays free for group connectors.
                match side with
                | ProvenanceSide.Input -> "swt:items-start"
                | ProvenanceSide.Output -> "swt:items-end"
            ]
            prop.children [
                Controls.AddEndpointPopover(
                    side,
                    endpointKinds,
                    existingEndpointNames,
                    createSet,
                    debug = debug,
                    key = $"{layer.Id}:{keyPrefix}:{endpointKindsKey}"
                )
                for group in groups do
                    GroupCard.Main(
                        side,
                        group,
                        model,
                        State.Selection.contains layer.Id side group.Id uiState,
                        isExpanded side group.Id,
                        (fun () -> toggleSelection side group.Id),
                        (fun () -> toggleDetail side group.Id),
                        isValueChipDragging,
                        ?connectionCount = connectionCountFor group.Id,
                        sourceInfoForValue = sourceInfoForValue,
                        debug = debug,
                        key = $"{keyPrefix}:{group.Id}"
                    )
                if groups.IsEmpty then
                    Html.p [
                        prop.className "swt:text-sm swt:text-base-content/60"
                        prop.text "No entries in this layer"
                    ]

            ]
        ]

    let dragOverlay findPropertyValue debug activeDrag =
        match activeDrag with
        | Some(DragDrop.Payload.PropertyValue propertyValueId) ->
            match findPropertyValue propertyValueId with
            | Some propertyValue -> Controls.ValueDragPreview(propertyValue, showHeader = false, debug = debug)
            | None -> Html.none
        | _ -> Html.none

[<Erase; Mangle(false)>]
type ProvenanceGrouping =

    [<ReactComponent>]
    static member Main
        (session: ProvenanceSession, onChange: ProvenanceEditorChange -> unit, ?height: int, ?debug: bool)
        =
        let debug = defaultArg debug false
        let rawUiState, setUiState = React.useState (State.init session)
        let activeDrag, setActiveDrag = React.useState<DragDrop.Payload option> None
        let surfaceRef = React.useElementRef ()
        let splitDrag = React.useRef (None: Splitter.SplitterSide option)
        let rootRef = React.useElementRef ()
        let tier, setTier = React.useState LayoutTier.Wide
        let openRail, setOpenRail = React.useState<ProvenanceSide option> None
        let density, setDensity = React.useState Density.EditorDensity.Comfortable

        let showPropertyHeaderConnectors, setShowPropertyHeaderConnectors =
            React.useState true

        let liveDragStore = React.useRef (LiveDrag.create ())
        let isValueChipDragging, setIsValueChipDragging = React.useState false

        React.useEffectOnce (fun () ->
            let applyTier () =
                match rootRef.current with
                | Some root -> setTier (LayoutTier.forWidth root.clientWidth)
                | None -> ()

            applyTier ()
            let observer = TierObserver.create applyTier

            match rootRef.current with
            | Some root -> TierObserver.observe observer root
            | None -> ()

            FsReact.createDisposable (fun () -> TierObserver.disconnect observer)
        )

        let uiState = State.Sides.ensure session rawUiState

        let layer, inputGroups, outputGroups, connections =
            React.useMemo ((fun () -> Display.displayLayer session uiState), [| box session; box uiState.SideStates |])

        let latestUiState = React.useRef uiState
        let activeLayerId = React.useRef layer.Id
        latestUiState.current <- uiState
        activeLayerId.current <- layer.Id
        let panelRatios = State.PanelLayout.get layer.Id uiState

        let endpointKinds = Endpoints.endpointKindOptions ()

        let existingEndpointNames = [
            yield!
                inputGroups
                |> List.collect (fun group -> group.Members |> List.map (fun member' -> member'.Name))
            yield!
                outputGroups
                |> List.collect (fun group -> group.Members |> List.map (fun member' -> member'.Name))
        ]

        let lookups = EditorLookups.create layer uiState inputGroups outputGroups

        let inputRailProjection =
            React.useMemo (
                (fun () ->
                    PropertyProjection.railProjectionWithFilters
                        session
                        layer.Id
                        ProvenanceSide.Input
                        layer.Model
                        uiState
                ),
                [|
                    box session
                    box layer.Id
                    box layer.Model
                    box uiState.PropertyRailPlacements
                    box uiState.ExpandedProperties
                    box uiState.PaletteValues
                    box uiState.PropertyColors
                    box uiState.Filters
                |]
            )

        let outputRailProjection =
            React.useMemo (
                (fun () ->
                    PropertyProjection.railProjectionWithFilters
                        session
                        layer.Id
                        ProvenanceSide.Output
                        layer.Model
                        uiState
                ),
                [|
                    box session
                    box layer.Id
                    box layer.Model
                    box uiState.PropertyRailPlacements
                    box uiState.ExpandedProperties
                    box uiState.PaletteValues
                    box uiState.PropertyColors
                    box uiState.Filters
                |]
            )

        let applyUiState update =
            let next = update latestUiState.current
            latestUiState.current <- next
            setUiState next

        let commitUiState next =
            latestUiState.current <- next
            setUiState next

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

                let splitPercent = rawPercent |> max 0. |> min 100. |> round |> int

                let current = latestUiState.current

                let next =
                    match side with
                    | Splitter.Left -> State.PanelLayout.setLeft activeLayerId.current splitPercent current
                    | Splitter.Right -> State.PanelLayout.setRight activeLayerId.current (100 - splitPercent) current

                latestUiState.current <- next
                setUiState next

        React.useEffectOnce (fun () ->
            let onMove =
                fun (event: Browser.Types.PointerEvent) ->
                    match splitDrag.current with
                    | Some side -> commitPanelRatio side event.clientX
                    | None -> ()

            let stopDragging = fun (_: Browser.Types.PointerEvent) -> splitDrag.current <- None

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
                LiveDrag.clear liveDragStore.current

                let nextUiState = latestUiState.current |> State.Sides.ensure next

                commitUiState {
                    nextUiState with
                        Error = None
                        PendingAssignmentBatch = None
                        PendingMemberResolution = None
                }

                onChange { Session = next; Patches = patches }
            | Error error ->
                LiveDrag.clear liveDragStore.current

                commitUiState {
                    latestUiState.current with
                        Error = Some(string error)
                }

        let createSet command =
            EditorActions.createSet session publish command

        let addPaletteValue side header value unit =
            State.Palette.addValue layer.Id side header value unit uiState |> setUiState

        let toggleSideGrouping layerId side header =
            State.GroupingAssignments.toggleSide layerId side header uiState |> setUiState

        let togglePropertyExpanded side header =
            State.PropertyExpansion.toggle layer.Id side header uiState |> setUiState

        let toggleSelection side groupId =
            State.Selection.toggle layer.Id side groupId uiState |> setUiState

        let toggleGroupDetail side groupId =
            State.Detail.toggleGroup side groupId uiState |> setUiState

        let sourceInfoForValue value =
            Session.propertyValueSourceInfo layer value

        let setPropertyColor header color =
            let update =
                match color with
                | Some selectedColor -> State.PropertyColors.setColor header selectedColor
                | None -> State.PropertyColors.clearColor header

            applyUiState update

        let setLayerColor layerId color =
            let update =
                match color with
                | Some selectedColor -> State.PropertyColors.setLayerColor layerId selectedColor
                | None -> State.PropertyColors.clearLayerColor layerId

            applyUiState update

        let confirmBatch (pending: PendingAssignmentBatch) =
            EditorActions.applyAssignmentBatch session publish pending.Batch

        let connectSetPairs = EditorActions.connectSetPairs session publish

        let removeDisplayConnection (connection: DisplayConnection) =
            match Session.removeConnections connection.ConnectionIds session with
            | Ok(next, patches) ->
                LiveDrag.clear liveDragStore.current

                let nextUiState = latestUiState.current |> State.Sides.ensure next

                commitUiState {
                    nextUiState with
                        Error = None
                        PendingAssignmentBatch = None
                        PendingMemberResolution = None
                        Detail = None
                }

                onChange { Session = next; Patches = patches }
            | Error error ->
                LiveDrag.clear liveDragStore.current

                commitUiState {
                    latestUiState.current with
                        Error = Some(string error)
                }

        let resolveAllToAll (pending: PendingMemberResolution) =
            match
                lookups.FindGroup ProvenanceSide.Input pending.InputGroupId,
                lookups.FindGroup ProvenanceSide.Output pending.OutputGroupId
            with
            | Some inputGroup, Some outputGroup ->
                EditorActions.allMemberPairs inputGroup outputGroup |> connectSetPairs
            | _ -> State.MemberResolution.clearPending uiState |> setUiState

        let isManuallyResolving side groupId =
            uiState.ManualResolutionPairs
            |> List.exists (fun resolution ->
                resolution.LayerId = layer.Id
                && ((side = ProvenanceSide.Input && resolution.InputGroupId = groupId)
                    || (side = ProvenanceSide.Output && resolution.OutputGroupId = groupId))
            )

        let isConnectedToExpanded side groupId =
            connections
            |> List.exists (fun connection ->
                match side with
                | ProvenanceSide.Input ->
                    connection.SourceGroupId = groupId
                    && State.Detail.isGroupExpanded ProvenanceSide.Output connection.TargetGroupId uiState
                | ProvenanceSide.Output ->
                    connection.TargetGroupId = groupId
                    && State.Detail.isGroupExpanded ProvenanceSide.Input connection.SourceGroupId uiState
            )

        let isGroupExpanded side groupId =
            State.Detail.isGroupExpanded side groupId uiState
            || isManuallyResolving side groupId
            || isConnectedToExpanded side groupId

        let dragContext = {
            Session = session
            Layer = layer
            UiState = uiState
            Publish = publish
            SetUiState = commitUiState
            Lookups = lookups
            ConnectSetPairs = connectSetPairs
        }

        let railSideLabel side =
            match side with
            | ProvenanceSide.Input -> "input"
            | ProvenanceSide.Output -> "output"

        let toggleRail side =
            setOpenRail (if openRail = Some side then None else Some side)

        let railPanel side =
            let layer = Session.activeLayer session

            let sideId, oppositeSideId, targetSide =
                match side with
                | ProvenanceSide.Input -> layer.InputSideId, layer.OutputSideId, ProvenanceSide.Output
                | ProvenanceSide.Output -> layer.OutputSideId, layer.InputSideId, ProvenanceSide.Input

            EditorSurface.propertyRail
                side
                sideId
                (match side with
                 | ProvenanceSide.Input -> inputRailProjection
                 | ProvenanceSide.Output -> outputRailProjection)
                (State.Sides.get sideId uiState).GroupingAssignments
                (fun header -> toggleSideGrouping sideId side header)
                (fun header ->
                    State.GroupingAssignments.toggleBoth layer.InputSideId layer.OutputSideId header uiState
                    |> setUiState
                )
                (fun header ->
                    State.GroupingAssignments.move layer.Id sideId oppositeSideId targetSide header uiState
                    |> setUiState
                )
                (fun header -> togglePropertyExpanded side header)
                (fun header value unit -> addPaletteValue side header value unit)
                setPropertyColor
                sourceInfoForValue
                debug
                setIsValueChipDragging

        let railColumn side =
            Html.div [
                prop.className "swt:@container/provenancePanel swt:min-w-0 swt:overflow-hidden"
                prop.children [ railPanel side ]
            ]

        let connectionCounts =
            React.useMemo (
                (fun () ->
                    connections
                    |> List.collect (fun connection -> [
                        (ProvenanceSide.Input, connection.SourceGroupId), connection.ConnectionIds.Length
                        (ProvenanceSide.Output, connection.TargetGroupId), connection.ConnectionIds.Length
                    ])
                    |> List.groupBy fst
                    |> List.map (fun (key, grouped) -> key, grouped |> List.sumBy snd)
                    |> Map.ofList
                ),
                [| box connections |]
            )

        let connectionCountFor side groupId =
            connectionCounts |> Map.tryFind (side, groupId) |> Option.defaultValue 0

        let groupColumnFor side withConnectionBadges =
            let unsorted =
                match side with
                | ProvenanceSide.Input -> inputGroups
                | ProvenanceSide.Output -> outputGroups

            let groups = Display.sortGroups uiState.Filters.GroupSort connections unsorted

            let counts groupId =
                if withConnectionBadges then
                    Some(connectionCountFor side groupId)
                else
                    None

            EditorSurface.groupColumn
                side
                layer
                layer.Model
                groups
                endpointKinds
                existingEndpointNames
                createSet
                uiState
                isGroupExpanded
                toggleSelection
                toggleGroupDetail
                counts
                sourceInfoForValue
                debug
                isValueChipDragging

        // Medium tier: rails fold into slim vertical strips, one open at a time.
        let mediumRailColumn side =
            if openRail = Some side then
                Html.div [
                    prop.className
                        "swt:@container/provenancePanel swt:flex swt:min-w-0 swt:flex-col swt:gap-2 swt:overflow-hidden"
                    prop.children [
                        Html.button [
                            prop.title (
                                if side = ProvenanceSide.Input then
                                    "Hide input properties"
                                else
                                    "Hide output properties"
                            )
                            prop.type'.button
                            prop.className [
                                "swt:btn swt:btn-ghost swt:btn-xs swt:w-fit"
                                if side = ProvenanceSide.Output then
                                    "swt:self-end"
                            ]
                            prop.ariaLabel $"Hide {railSideLabel side} properties"
                            if debug then
                                prop.testId $"provenance-rail-toggle-{side}"
                            prop.onClick (fun _ -> toggleRail side)
                            prop.children [
                                Html.i [

                                    prop.className [
                                        "swt:iconify swt:size-4"
                                        if side = ProvenanceSide.Input then
                                            "swt:fluent--chevron-left-20-regular"
                                        else
                                            "swt:fluent--chevron-right-20-regular"
                                    ]
                                ]
                            ]
                        ]
                        railPanel side
                    ]
                ]
            else
                Html.button [
                    prop.title (
                        if side = ProvenanceSide.Input then
                            "Show input properties"
                        else
                            "Show output properties"
                    )
                    prop.type'.button
                    prop.className
                        "swt:btn swt:btn-ghost swt:btn-xs swt:h-auto swt:min-h-24 swt:w-fit swt:px-1 swt:py-2"
                    prop.ariaLabel $"Show {railSideLabel side} properties"
                    if debug then
                        prop.testId $"provenance-rail-toggle-{side}"
                    prop.onClick (fun _ -> toggleRail side)
                    prop.children [
                        Html.span [
                            prop.className "swt:[writing-mode:vertical-rl] swt:text-xs"
                            prop.text "Properties"
                        ]
                    ]
                ]

        // Narrow tier: everything stacks and the rails collapse behind toggles.
        let narrowRailSection side =
            Html.div [
                prop.className "swt:@container/provenancePanel swt:flex swt:min-w-0 swt:flex-col swt:gap-2"
                prop.children [
                    Html.button [
                        prop.type'.button
                        prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:w-fit"
                        prop.ariaLabel (
                            if openRail = Some side then
                                $"Hide {railSideLabel side} properties"
                            else
                                $"Show {railSideLabel side} properties"
                        )
                        if debug then
                            prop.testId $"provenance-rail-toggle-{side}"
                        prop.onClick (fun _ -> toggleRail side)
                        prop.children [
                            Html.i [
                                prop.className [
                                    "swt:iconify swt:size-4"
                                    if openRail = Some side then
                                        "swt:fluent--chevron-up-20-regular"
                                    else
                                        "swt:fluent--chevron-down-20-regular"
                                ]
                            ]
                            Html.span (
                                match side with
                                | ProvenanceSide.Input -> "Input properties"
                                | ProvenanceSide.Output -> "Output properties"
                            )
                        ]
                    ]
                    if openRail = Some side then
                        railPanel side
                ]
            ]

        let connectorOverlay =
            ConnectorOverlay.Main(
                surfaceRef,
                layer.Id,
                layer.Model,
                inputGroups,
                outputGroups,
                connections,
                inputRailProjection,
                outputRailProjection,
                ConnectorOverlayState.fromUiState uiState,
                showPropertyHeaderConnectors,
                liveDragStore.current,
                (fun connection -> State.Detail.showConnection connection.Id uiState |> setUiState),
                onRemove = removeDisplayConnection,
                debug = debug
            )

        let surface =
            match tier with
            | LayoutTier.Wide ->
                Html.div [
                    prop.ref surfaceRef
                    prop.className "swt:relative swt:mx-4 swt:grid swt:min-w-0 swt:items-start"
                    prop.style [
                        style.custom ("gridTemplateColumns", Splitter.template panelRatios)
                    ]
                    if debug then
                        prop.testId "provenance-surface"
                    prop.children [
                        connectorOverlay
                        railColumn ProvenanceSide.Input
                        Splitter.handle Splitter.Left (startPanelDrag Splitter.Left) debug
                        Html.div [
                            // The wide column gap is the gutter the group-to-group
                            // connectors are drawn in.
                            prop.className [
                                "swt:grid swt:min-w-0 swt:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] swt:items-start"
                                match density with
                                | Density.EditorDensity.Compact -> "swt:gap-12"
                                | _ -> "swt:gap-16"
                            ]
                            prop.children [
                                groupColumnFor ProvenanceSide.Input false
                                groupColumnFor ProvenanceSide.Output false
                            ]
                        ]
                        Splitter.handle Splitter.Right (startPanelDrag Splitter.Right) debug
                        railColumn ProvenanceSide.Output
                    ]
                ]
            | LayoutTier.Medium ->
                let railTrack side =
                    if openRail = Some side then
                        "minmax(10rem, 16rem)"
                    else
                        "auto"

                Html.div [
                    prop.ref surfaceRef
                    prop.className "swt:relative swt:mx-4 swt:grid swt:min-w-0 swt:items-start swt:gap-x-8"
                    prop.style [
                        style.custom (
                            "gridTemplateColumns",
                            $"{railTrack ProvenanceSide.Input} minmax(0, 1fr) {railTrack ProvenanceSide.Output}"
                        )
                    ]
                    if debug then
                        prop.testId "provenance-surface"
                    prop.children [
                        connectorOverlay
                        mediumRailColumn ProvenanceSide.Input
                        Html.div [
                            prop.className
                                "swt:grid swt:min-w-0 swt:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] swt:items-start swt:gap-8"
                            prop.children [
                                groupColumnFor ProvenanceSide.Input false
                                groupColumnFor ProvenanceSide.Output false
                            ]
                        ]
                        mediumRailColumn ProvenanceSide.Output
                    ]
                ]
            | LayoutTier.Narrow ->
                // Stacked cards cannot host readable connector curves; connection
                // badges on the cards carry that information instead.
                Html.div [
                    prop.ref surfaceRef
                    prop.className "swt:relative swt:mx-4 swt:flex swt:min-w-0 swt:flex-col swt:gap-4"
                    if debug then
                        prop.testId "provenance-surface"
                    prop.children [
                        narrowRailSection ProvenanceSide.Input
                        groupColumnFor ProvenanceSide.Input true
                        groupColumnFor ProvenanceSide.Output true
                        narrowRailSection ProvenanceSide.Output
                    ]
                ]

        let content =
            Html.div [
                prop.ref rootRef
                prop.className [
                    "swt:flex swt:flex-col swt:bg-base-200 swt:overflow-auto swt:pb-4"
                    // Without an explicit height the editor fills its host instead of
                    // forcing a fixed pixel height into responsive layouts.
                    if height.IsNone then
                        "swt:h-full swt:min-h-0"
                ]
                match height with
                | Some height -> prop.style [ style.height height ]
                | None -> ()
                if debug then
                    prop.testId "provenance-editor-root"
                prop.children [
                    // The toolbar and pending prompts stay pinned while the surface scrolls.
                    Html.div [
                        prop.className
                            "swt:sticky swt:top-0 swt:z-20 swt:flex swt:flex-col swt:gap-4 swt:bg-base-200 swt:p-4"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:flex-wrap swt:items-center swt:justify-between swt:gap-2"
                                prop.children [
                                    Controls.LayerTabs(
                                        session,
                                        (fun layerId -> Session.selectLayer layerId session |> publish),
                                        (fun () ->
                                            EditorActions.addLayer
                                                session
                                                layer.Id
                                                inputGroups
                                                outputGroups
                                                uiState
                                                publish
                                        ),
                                        layerColors = uiState.PropertyColors.LayerColors,
                                        debug = debug
                                    )
                                    Html.div [
                                        prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-1"
                                        prop.children [
                                            for layerId in session.LayerOrder do
                                                let layer = Session.layerById layerId session

                                                Controls.LayerColorButton(
                                                    layer.Id,
                                                    uiState.PropertyColors.LayerColors |> Map.tryFind layer.Id,
                                                    setLayerColor layer.Id
                                                )
                                        ]
                                    ]
                                    Html.div [
                                        prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                                        prop.children [
                                            Html.button [
                                                prop.title (
                                                    if showPropertyHeaderConnectors then
                                                        "Hide property header connectors"
                                                    else
                                                        "Show property header connectors"
                                                )
                                                prop.type'.button
                                                prop.className [
                                                    "swt:btn swt:btn-xs"
                                                    if showPropertyHeaderConnectors then
                                                        "swt:btn-primary"
                                                    else
                                                        "swt:btn-ghost"
                                                ]
                                                prop.custom ("aria-pressed", showPropertyHeaderConnectors)
                                                prop.ariaLabel (
                                                    if showPropertyHeaderConnectors then
                                                        "Hide property header connectors"
                                                    else
                                                        "Show property header connectors"
                                                )
                                                if debug then
                                                    prop.testId "provenance-property-connectors-toggle"
                                                prop.onClick (fun _ ->
                                                    setShowPropertyHeaderConnectors (not showPropertyHeaderConnectors)
                                                )
                                                prop.children [
                                                    Html.i [
                                                        prop.className [
                                                            "swt:iconify swt:size-4"
                                                            if showPropertyHeaderConnectors then
                                                                "swt:fluent--eye-20-regular"
                                                            else
                                                                "swt:fluent--eye-hide-20-regular"
                                                        ]
                                                    ]
                                                    Html.span "Property connectors"
                                                ]
                                            ]
                                            Html.button [
                                                match density with
                                                | Density.EditorDensity.Compact ->
                                                    prop.title "Toggle comfortable density"
                                                | Density.EditorDensity.Comfortable ->
                                                    prop.title "Toggle compact density"

                                                prop.type'.button
                                                prop.className [
                                                    "swt:btn swt:btn-xs"
                                                    if density = Density.EditorDensity.Compact then
                                                        "swt:btn-primary"
                                                    else
                                                        "swt:btn-ghost"
                                                ]
                                                prop.custom ("aria-pressed", (density = Density.EditorDensity.Compact))
                                                prop.ariaLabel (
                                                    if density = Density.EditorDensity.Compact then
                                                        "Switch to comfortable density"
                                                    else
                                                        "Switch to compact density"
                                                )
                                                if debug then
                                                    prop.testId "provenance-density-toggle"
                                                prop.onClick (fun _ ->
                                                    setDensity (
                                                        if density = Density.EditorDensity.Compact then
                                                            Density.EditorDensity.Comfortable
                                                        else
                                                            Density.EditorDensity.Compact
                                                    )
                                                )
                                                prop.text "Compact"
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                            Controls.FilterToolbar(
                                uiState.Filters,
                                (fun text -> applyUiState (State.Filters.setSearch text)),
                                (fun sort -> applyUiState (State.Filters.setPropertySort sort)),
                                (fun sort -> applyUiState (State.Filters.setGroupSort sort)),
                                (fun filter -> applyUiState (State.Filters.setValueCountFilter filter)),
                                (fun filter -> applyUiState (State.Filters.setOriginFilter filter)),
                                debug = debug
                            )
                            match uiState.Error with
                            | Some error -> EditorPanels.errorAlert error
                            | None -> Html.none
                            match uiState.PendingAssignmentBatch with
                            | Some batch ->
                                EditorPanels.assignmentBatchWarning
                                    debug
                                    batch
                                    confirmBatch
                                    (fun () -> State.AssignmentBatch.clear uiState |> setUiState)
                            | None -> Html.none
                            match uiState.PendingMemberResolution with
                            | Some pending ->
                                EditorPanels.memberResolutionPrompt
                                    debug
                                    pending
                                    resolveAllToAll
                                    (fun pending -> applyUiState (State.MemberResolution.chooseManual pending))
                                    (fun () -> applyUiState State.MemberResolution.clearPending)
                            | None -> Html.none
                        ]
                    ]
                    surface
                    EditorPanels.connectionDetails debug connections uiState.Detail
                ]
            ]

        DndKit.DndContext(
            sensors = sensors,
            collisionDetection = DndKit.pointerWithin,
            onDragStart = DragHandlers.handleStart surfaceRef setActiveDrag liveDragStore.current,
            onDragMove = DragHandlers.handleMove liveDragStore.current,
            onDragCancel =
                (fun _ ->
                    setActiveDrag None
                    LiveDrag.clear liveDragStore.current
                ),
            onDragEnd =
                (fun event ->
                    setActiveDrag None
                    LiveDrag.clear liveDragStore.current
                    DragHandlers.handleEnd dragContext event
                ),
            children =
                Density.provider
                    density
                    (React.Fragment [
                        content
                        DndKit.DragOverlay(
                            children = EditorSurface.dragOverlay lookups.FindPropertyValue debug activeDrag
                        )
                    ])
        )

    [<ReactComponent>]
    static member Editor
        (initialModel: ProvenanceModel, onChange: ProvenanceEditorChange -> unit, ?height: int, ?debug: bool)
        =
        let session, setSession = React.useState (Session.init initialModel)

        let change (next: ProvenanceEditorChange) =
            setSession next.Session
            onChange next

        ProvenanceGrouping.Main(session, change, ?height = height, ?debug = debug)
