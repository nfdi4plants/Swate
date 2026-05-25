namespace Swate.Components.Composite.ProvenanceGrouping

open Fable.Core
open Feliz
open Swate.Components.JsBindings
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Composite.ProvenanceGrouping.Types
open Swate.Components.Composite.ProvenanceGrouping.State
open Swate.Components.Composite.ProvenanceGrouping.Helper

[<Erase; Mangle(false)>]
type ProvenanceGrouping =

    [<ReactComponent>]
    static member Main(session: ProvenanceSession, onChange: ProvenanceEditorChange -> unit, ?height: int, ?debug: bool) =
        let debug = defaultArg debug false
        let uiState, setUiState = React.useState (State.init session)
        let surfaceRef = React.useElementRef ()
        let uiState = State.ensureLayers session uiState
        let pair, inputGroups, outputGroups, connections = displayPair session uiState

        let publish result =
            match result with
            | Ok(next, patches) ->
                setUiState (State.ensureLayers next uiState)
                onChange { Session = next; Patches = patches }
            | Error error ->
                setUiState { uiState with Error = Some(string error) }

        let addLayer () =
            layerCommand inputGroups outputGroups uiState
            |> fun command -> Session.addLayer command session
            |> publish

        let updateValue propertyValueId value unit =
            Session.updatePropertyValue propertyValueId value unit session
            |> publish

        let createSet command =
            Session.createLoadedSet command session
            |> publish

        let createPropertyValue command =
            Session.createLoadedPropertyValue command session
            |> publish

        let findGroup side groupId =
            let groups : DisplayGroup list = if side = ProvenanceSide.Input then inputGroups else outputGroups
            groups |> List.tryFind (fun (group: DisplayGroup) -> group.Id = groupId)

        let connectGroups inputGroup outputGroup =
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

        let handleDragEnd (event: DndKit.IDndKitEvent) =
            if not (isNull event.over) then
                match tryDragId (string event.active.id), tryDropId (string event.over.id) with
                | Some(DragPayload.PropertyValue propertyValueId), Some(side, groupId) ->
                    match findGroup side groupId with
                    | Some group ->
                        let memberIds = group.Members |> List.map (fun member' -> member'.SetId)
                        let target =
                            match side with
                            | ProvenanceSide.Input -> ProvenancePropertyTarget.InputSets memberIds
                            | ProvenanceSide.Output -> ProvenancePropertyTarget.OutputSets memberIds
                        Session.copyPropertyValueToLoadedTarget propertyValueId target session |> publish
                    | None -> ()
                | Some(DragPayload.Group(ProvenanceSide.Input, inputGroupId)), Some(ProvenanceSide.Output, outputGroupId) ->
                    match findGroup ProvenanceSide.Input inputGroupId, findGroup ProvenanceSide.Output outputGroupId with
                    | Some inputGroup, Some outputGroup -> connectGroups inputGroup outputGroup
                    | _ -> ()
                | _ -> ()

        let isExpanded side groupId =
            uiState.Detail = Some(ProvenanceDetail.Group(side, groupId))

        let toggleExpanded side groupId =
            let next =
                if isExpanded side groupId then None
                else Some(ProvenanceDetail.Group(side, groupId))
            setUiState { uiState with Detail = next }

        let content =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-4 swt:bg-base-200 swt:p-4 swt:overflow-auto"
                prop.style [ style.height (defaultArg height 720) ]
                if debug then prop.testId "provenance-editor-root"
                prop.children [
                Controls.LayerTabs(session, (fun pairId -> Session.selectPair pairId session |> publish), addLayer, debug = debug)
                match uiState.Error with
                | Some error -> Html.div [ prop.className "swt:alert swt:alert-error"; prop.text error ]
                | None -> Html.none
                Html.div [
                    prop.ref surfaceRef
                    prop.className "swt:relative swt:grid swt:grid-cols-[minmax(10rem,12rem)_minmax(16rem,1fr)_6rem_minmax(16rem,1fr)_minmax(10rem,12rem)] swt:gap-4 swt:items-start"
                    prop.children [
                        ConnectorOverlay.Main(surfaceRef, connections, (fun connection ->
                            setUiState { uiState with Detail = Some(ProvenanceDetail.Connection connection.Id) }), debug = debug)
                        Controls.PropertyRail(ProvenanceSide.Input, headersForSide ProvenanceSide.Input pair.Model, (State.layerState pair.LeftLayerId uiState).GroupingKeys, (fun header -> setUiState (State.toggleGrouping pair.LeftLayerId header uiState)), debug = debug)
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-3"
                            prop.children [
                                for group in inputGroups do
                                    GroupCard.Main(ProvenanceSide.Input, group, pair.Model, uiState.SelectedInputs.Contains group.Id, isExpanded ProvenanceSide.Input group.Id, (fun () -> setUiState (State.select ProvenanceSide.Input group.Id uiState)), (fun () -> toggleExpanded ProvenanceSide.Input group.Id), updateValue, createPropertyValue, debug = debug, key = $"Input:{group.Id}")
                                if inputGroups.IsEmpty then
                                    Html.p [ prop.className "swt:text-sm swt:text-base-content/60"; prop.text "No entries in this layer" ]
                                    Controls.AddEndpointPopover(ProvenanceSide.Input, defaultEndpointKind ProvenanceSide.Input pair.Model, createSet, debug = debug)
                            ]
                        ]
                        Html.div [ prop.className "swt:min-h-full" ]
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-3"
                            prop.children [
                                for group in outputGroups do
                                    GroupCard.Main(ProvenanceSide.Output, group, pair.Model, uiState.SelectedOutputs.Contains group.Id, isExpanded ProvenanceSide.Output group.Id, (fun () -> setUiState (State.select ProvenanceSide.Output group.Id uiState)), (fun () -> toggleExpanded ProvenanceSide.Output group.Id), updateValue, createPropertyValue, debug = debug, key = $"Output:{group.Id}")
                                if outputGroups.IsEmpty then
                                    Html.p [ prop.className "swt:text-sm swt:text-base-content/60"; prop.text "No entries in this layer" ]
                                    Controls.AddEndpointPopover(ProvenanceSide.Output, defaultEndpointKind ProvenanceSide.Output pair.Model, createSet, debug = debug)
                            ]
                        ]
                        Controls.PropertyRail(ProvenanceSide.Output, headersForSide ProvenanceSide.Output pair.Model, (State.layerState pair.RightLayerId uiState).GroupingKeys, (fun header -> setUiState (State.toggleGrouping pair.RightLayerId header uiState)), debug = debug)
                    ]
                ]
                match uiState.Detail with
                | Some(ProvenanceDetail.Connection connectionId) ->
                    let resolved = connections |> List.tryFind (fun c -> c.Id = connectionId)
                    match resolved with
                    | Some conn ->
                        Html.div [
                            prop.className "swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
                            if debug then prop.testId "provenance-connection-details"
                            prop.children [
                                Html.h3 [ prop.className "swt:font-semibold swt:text-primary"; prop.text $"Connection: {connectionId}" ]
                                Html.p [ prop.className "swt:text-sm"; let txt = $"Source: {conn.SourceGroupId}" in prop.text txt ]
                                Html.p [ prop.className "swt:text-sm"; let txt = $"Target: {conn.TargetGroupId}" in prop.text txt ]
                                Html.p [ prop.className "swt:text-sm"; let ids = conn.ConnectionIds |> String.concat ", " in prop.text $"Connection IDs: {ids}" ]
                                Html.div [
                                    prop.className "swt:flex swt:flex-wrap swt:gap-1 swt:pt-2"
                                    prop.children [
                                        for header in headersForModel pair.Model do
                                            Controls.AddValuePopover(
                                                ProvenancePropertyTarget.Connections conn.ConnectionIds,
                                                Some header,
                                                createPropertyValue,
                                                debug = debug)
                                        Controls.AddValuePopover(
                                            ProvenancePropertyTarget.Connections conn.ConnectionIds,
                                            None,
                                            createPropertyValue,
                                            debug = debug)
                                    ]
                                ]
                            ]
                        ]
                    | None -> Html.none
                | _ -> Html.none
                ]
            ]

        DndKit.DndContext(
            collisionDetection = DndKit.pointerWithin,
            onDragEnd = handleDragEnd,
            children = content
        )

    [<ReactComponent>]
    static member Editor(initialModel: ProvenanceModel, onChange: ProvenanceEditorChange -> unit, ?height: int, ?debug: bool) =
        let session, setSession = React.useState (Session.init initialModel)
        let change next =
            setSession next.Session
            onChange next
        ProvenanceGrouping.Main(session, change, ?height = height, ?debug = debug)
