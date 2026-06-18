namespace Swate.Components.Page.ProvenanceGrouping

/// Formatting helpers for provenance values rendered in labels, chips, and sort keys.
module Formatting =

    open Swate.Components.Shared.ProvenanceGrouping.Types
    open Swate.Components.Shared.ProvenanceGrouping.Grouping

    let formatValue value unit' = valueText value unit'

/// Editor-wide density, shared through context so nested cards and chips can
/// tighten their spacing without prop drilling.
module Density =

    open Fable.Core
    open Fable.Core.JsInterop
    open Feliz

    [<RequireQualifiedAccess>]
    type EditorDensity =
        | Comfortable
        | Compact

    let context = React.createContext (defaultValue = EditorDensity.Comfortable)

    [<ImportMember("react")>]
    let private createElement (comp: obj) (props: obj) (children: ReactElement) : ReactElement = jsNative

    /// Feliz 3 ships no contextProvider helper, so render the provider directly.
    let provider (value: EditorDensity) (children: ReactElement) : ReactElement =
        createElement !!context?Provider {| value = value |} children

/// CSS class builders shared by ProvenanceGrouping draggable cards, buttons, chips, and overlay previews.
module Styles =

    let dragIndicatorClasses isDragging = [
        "swt:transition swt:duration-150"
        if isDragging then
            "swt:ring-2 swt:ring-primary swt:border-primary swt:bg-primary/10 swt:shadow-md swt:opacity-80"
    ]

    let draggableButtonClasses isDragging = [
        "swt:cursor-grab swt:active:cursor-grabbing"
        yield! dragIndicatorClasses isDragging
    ]

    let draggableBoxClasses isDragging = [
        "swt:rounded-md swt:border swt:border-base-300 swt:bg-base-100 swt:shadow-sm"
        yield! draggableButtonClasses isDragging
    ]

    /// Value chips hug their content up to the property-header cap; the cap yields to
    /// the panel width when the rail gets narrow.
    let propertyValueButtonClasses density isDragging = [
        "swt:btn swt:btn-sm swt:btn-primary swt:w-fit swt:max-w-[min(18rem,100%)] swt:h-auto swt:justify-start swt:normal-case swt:font-medium swt:@max-xs/provenancePanel:px-2 swt:@max-xs/provenancePanel:text-[0.7rem]"
        match density with
        | Density.EditorDensity.Compact -> "swt:min-h-6 swt:px-2 swt:py-1 swt:text-[0.7rem]"
        | _ -> "swt:min-h-8 swt:px-3 swt:py-1.5 swt:text-xs"
        yield! draggableButtonClasses isDragging
    ]

    let propertyValueOverlayClasses = [
        "swt:btn swt:btn-sm swt:btn-primary swt:w-fit swt:max-w-[18rem] swt:min-h-8 swt:h-auto swt:justify-start swt:normal-case swt:px-3 swt:py-1.5 swt:text-xs swt:font-medium swt:pointer-events-none swt:shadow-lg swt:ring-2 swt:ring-primary swt:ring-offset-2 swt:ring-offset-base-100"
    ]

    let addPropertyValueButtonClasses = [
        "swt:btn swt:btn-sm swt:btn-outline swt:btn-primary swt:w-fit swt:max-w-full swt:min-h-8 swt:h-auto swt:justify-start swt:normal-case swt:px-3 swt:py-1.5 swt:text-xs swt:font-medium swt:@max-xs/provenancePanel:px-2 swt:@max-xs/provenancePanel:text-[0.7rem]"
    ]

/// Stable identity strings for React keys, DOM lookup attributes, and DnD payload/drop parsing.
module DragDrop =

    open Swate.Components.Shared.ProvenanceGrouping.Types
    open Swate.Components.Page.ProvenanceGrouping.Types

    let private encode (value: string) = System.Uri.EscapeDataString value
    let private decode (value: string) = System.Uri.UnescapeDataString value

    let private termIdentity (term: ProvenanceTerm) =
        let source = term.TermSource |> Option.defaultValue ""
        let accession = term.TermAccession |> Option.defaultValue ""
        $"{encode term.Name}|{encode source}|{encode accession}"

    let propertyHeaderIdentity (header: ProvenancePropertyHeader) =
        $"{encode header.Kind.Id}:{termIdentity header.Category}"

    let propertyValueIdentity (propertyValue: ProvenancePropertyValue) =
        let value =
            match propertyValue.Value with
            | ProvenanceValue.Text text -> $"Text:{encode text}"
            | ProvenanceValue.Integer integer -> $"Integer:{integer}"
            | ProvenanceValue.Float float -> $"Float:{float}"
            | ProvenanceValue.Term term -> $"Term:{termIdentity term}"

        let unit = propertyValue.Unit |> Option.map termIdentity |> Option.defaultValue ""
        $"{propertyValue.Id}:{value}:Unit:{unit}"

    let valueDragId propertyValueId =
        $"provenance-value|{encode propertyValueId}"

    let propertyDragId side header =
        $"provenance-property|{side}|{encode (propertyHeaderIdentity header)}"

    let propertyRailDropId side = $"provenance-property-drop|{side}"

    let groupDragId side groupId =
        $"provenance-group|{side}|{encode groupId}"

    let groupDropId side groupId =
        $"provenance-drop|{side}|{encode groupId}"

    let groupNodeId side groupId =
        $"provenance-node::{side}::{encode groupId}"

    let private handleKindText kind =
        match kind with
        | ConnectionHandleKind.GroupCard -> "GroupCard"
        | ConnectionHandleKind.GroupMember -> "GroupMember"
        | ConnectionHandleKind.PropertyHeader -> "PropertyHeader"
        | ConnectionHandleKind.PropertyValue -> "PropertyValue"
        | ConnectionHandleKind.GroupPropertyAnchor -> "GroupPropertyAnchor"

    let private tryHandleKind value =
        match value with
        | "GroupCard" -> Some ConnectionHandleKind.GroupCard
        | "GroupMember" -> Some ConnectionHandleKind.GroupMember
        | "PropertyHeader" -> Some ConnectionHandleKind.PropertyHeader
        | "PropertyValue" -> Some ConnectionHandleKind.PropertyValue
        | "GroupPropertyAnchor" -> Some ConnectionHandleKind.GroupPropertyAnchor
        | _ -> None

    let connectionHandleIdentity (handle: ConnectionHandleRef) =
        let parent = handle.ParentGroupId |> Option.defaultValue ""
        $"{handleKindText handle.Kind}|{handle.Side}|{encode handle.Id}|{encode parent}"

    let connectionHandleDragId handle =
        $"provenance-connection-drag|{connectionHandleIdentity handle}"

    let connectionHandleDropId handle =
        $"provenance-connection-drop|{connectionHandleIdentity handle}"

    let connectionHandleNodeId handle =
        $"provenance-connection-node::{connectionHandleIdentity handle}"

    type Payload =
        | PropertyValue of ProvenancePropertyValueId
        | PropertyHeader of ProvenanceSide * string
        | Group of ProvenanceSide * string
        | ConnectionHandle of ConnectionHandleRef

    let private tryParseHandleParts kind side sourceId parent =
        match tryHandleKind kind, side with
        | Some kind, "Input" ->
            Some {
                Kind = kind
                Side = ProvenanceSide.Input
                Id = decode sourceId
                ParentGroupId = if parent = "" then None else Some(decode parent)
            }
        | Some kind, "Output" ->
            Some {
                Kind = kind
                Side = ProvenanceSide.Output
                Id = decode sourceId
                ParentGroupId = if parent = "" then None else Some(decode parent)
            }
        | _ -> None

    let tryDragId (id: string) =
        match id.Split('|') with
        | [| "provenance-value"; valueId |] -> Some(Payload.PropertyValue(decode valueId))
        | [| "provenance-property"; "Input"; headerId |] ->
            Some(Payload.PropertyHeader(ProvenanceSide.Input, decode headerId))
        | [| "provenance-property"; "Output"; headerId |] ->
            Some(Payload.PropertyHeader(ProvenanceSide.Output, decode headerId))
        | [| "provenance-group"; "Input"; groupId |] -> Some(Payload.Group(ProvenanceSide.Input, decode groupId))
        | [| "provenance-group"; "Output"; groupId |] -> Some(Payload.Group(ProvenanceSide.Output, decode groupId))
        | [| "provenance-connection-drag"; kind; side; sourceId; parent |] ->
            tryParseHandleParts kind side sourceId parent
            |> Option.map Payload.ConnectionHandle
        | _ -> None

    let tryDropId (id: string) =
        match id.Split('|') with
        | [| "provenance-drop"; "Input"; groupId |] -> Some(ProvenanceSide.Input, decode groupId)
        | [| "provenance-drop"; "Output"; groupId |] -> Some(ProvenanceSide.Output, decode groupId)
        | _ -> None

    let tryPropertyDropId (id: string) =
        match id.Split('|') with
        | [| "provenance-property-drop"; "Input" |] -> Some ProvenanceSide.Input
        | [| "provenance-property-drop"; "Output" |] -> Some ProvenanceSide.Output
        | _ -> None

    let tryConnectionDropId (id: string) =
        match id.Split('|') with
        | [| "provenance-connection-drop"; kind; side; sourceId; parent |] ->
            tryParseHandleParts kind side sourceId parent
        | _ -> None

/// Keeps transient connector dragging outside editor state so pointer moves only
/// repaint the overlay layer that needs the live path.
module LiveDrag =

    open Swate.Components.Page.ProvenanceGrouping.Types

    type Store = {
        mutable Current: LiveConnectionDrag option
        mutable Listeners: (unit -> unit) list
    }

    let create () : Store = { Current = None; Listeners = [] }

    let private notify store =
        for listener in store.Listeners do
            listener ()

    let subscribe listener store =
        store.Listeners <- listener :: store.Listeners

        fun () ->
            store.Listeners <-
                store.Listeners
                |> List.filter (fun current -> not (System.Object.ReferenceEquals(current, listener)))

    let start source point store =
        store.Current <-
            Some {
                Source = source
                Start = point
                Current = point
            }

        notify store

    let moveTo point store =
        store.Current <- store.Current |> Option.map (fun current -> { current with Current = point })
        notify store

    let clear store =
        if store.Current.IsSome then
            store.Current <- None
            notify store

/// Validates edge-handle drag/drop pairs and returns the editor action they imply.
module ConnectionRouting =

    open Swate.Components.Shared.ProvenanceGrouping.Types
    open Swate.Components.Page.ProvenanceGrouping.Types

    type ConnectionAction =
        | ConnectGroups of inputGroupId: string * outputGroupId: string
        | ConnectMembers of
            inputGroupId: string *
            outputGroupId: string *
            inputSetId: ProvenanceSetId *
            outputSetId: ProvenanceSetId
        | ConnectMemberToGroup of
            inputGroupId: string *
            outputGroupId: string *
            memberSetId: ProvenanceSetId *
            memberSide: ProvenanceSide

    let private oppositeSides left right = left.Side <> right.Side

    let private orderedGroups left right =
        match left.Side, right.Side with
        | ProvenanceSide.Input, ProvenanceSide.Output -> left.Id, right.Id
        | ProvenanceSide.Output, ProvenanceSide.Input -> right.Id, left.Id
        | _ -> left.Id, right.Id

    let private orderedMembers left right =
        match left.Side, right.Side, left.ParentGroupId, right.ParentGroupId with
        | ProvenanceSide.Input, ProvenanceSide.Output, Some inputGroupId, Some outputGroupId ->
            Some(inputGroupId, outputGroupId, left.Id, right.Id)
        | ProvenanceSide.Output, ProvenanceSide.Input, Some outputGroupId, Some inputGroupId ->
            Some(inputGroupId, outputGroupId, right.Id, left.Id)
        | _ -> None

    let action source target =
        match source.Kind, target.Kind with
        | ConnectionHandleKind.GroupCard, ConnectionHandleKind.GroupCard when oppositeSides source target ->
            let inputGroupId, outputGroupId = orderedGroups source target
            Some(ConnectionAction.ConnectGroups(inputGroupId, outputGroupId))
        | ConnectionHandleKind.GroupMember, ConnectionHandleKind.GroupMember when oppositeSides source target ->
            orderedMembers source target |> Option.map ConnectionAction.ConnectMembers
        | ConnectionHandleKind.GroupMember, ConnectionHandleKind.GroupCard when oppositeSides source target ->
            source.ParentGroupId
            |> Option.map (fun sourceParent ->
                let inputGroupId, outputGroupId =
                    if source.Side = ProvenanceSide.Input then
                        sourceParent, target.Id
                    else
                        target.Id, sourceParent

                ConnectionAction.ConnectMemberToGroup(inputGroupId, outputGroupId, source.Id, source.Side)
            )
        | ConnectionHandleKind.GroupCard, ConnectionHandleKind.GroupMember when oppositeSides source target ->
            target.ParentGroupId
            |> Option.map (fun targetParent ->
                let inputGroupId, outputGroupId =
                    if target.Side = ProvenanceSide.Input then
                        targetParent, source.Id
                    else
                        source.Id, targetParent

                ConnectionAction.ConnectMemberToGroup(inputGroupId, outputGroupId, target.Id, target.Side)
            )
        | _ -> None

/// Builds property rail headers and values from the persistent model plus UI-only palette state.
module PropertyRails =

    open Swate.Components.Shared.ProvenanceGrouping.Types
    open Swate.Components.Shared.ProvenanceGrouping.Grouping
    open Swate.Components.Shared.ProvenanceGrouping.Session
    open Swate.Components.Page.ProvenanceGrouping.Types

    type RailProjection = {
        Headers: ProvenancePropertyHeader list
        ValuesByHeader: Map<ProvenancePropertyHeader, ProvenancePropertyValue list>
        ExpandedHeaders: Set<ProvenancePropertyHeader>
        CanSwitchHeaders: Set<ProvenancePropertyHeader>
        StatsByHeader: Map<ProvenancePropertyHeader, PropertyStats>
        BadgeByHeader: Map<ProvenancePropertyHeader, PropertyCountBadge>
        ColorByHeader: Map<ProvenancePropertyHeader, ProvenanceColor option>
        OriginByHeader: Map<ProvenancePropertyHeader, Set<PropertyOrigin>>
        OriginFilterOptions: PropertyOriginFilter list
    }

    let setsForSide side (model: ProvenanceModel) =
        if side = ProvenanceSide.Input then
            model.InputSets
        else
            model.OutputSets

    let private headersFromSets
        (propertyValueIds: ProvenanceSet -> ProvenancePropertyValueId list)
        side
        (model: ProvenanceModel)
        =
        setsForSide side model
        |> Map.toList
        |> List.collect (fun (_, set) ->
            propertyValueIds set
            |> List.choose (fun id -> model.PropertyValues.TryFind id)
            |> List.map (fun value -> value.Header)
        )
        |> List.distinct
        |> List.sortBy (fun header -> header.Category.Name)

    let headersForSide side (model: ProvenanceModel) =
        headersFromSets ProvenanceSet.effectivePropertyValueIds side model

    let ownedHeadersForSide side (model: ProvenanceModel) =
        headersFromSets (fun set -> set.PropertyValueIds) side model

    let headersForModel (model: ProvenanceModel) =
        [
            yield! headersForSide ProvenanceSide.Input model
            yield! headersForSide ProvenanceSide.Output model
        ]
        |> List.distinct
        |> List.sortBy (fun header -> header.Category.Name)

    let hasHeaderForSide side header (model: ProvenanceModel) =
        headersForSide side model |> List.contains header

    let canSwitchHeader header (model: ProvenanceModel) =
        hasHeaderForSide ProvenanceSide.Input header model
        && hasHeaderForSide ProvenanceSide.Output header model

    let private placedHeadersForSide pairId side (uiState: UiState) =
        uiState.PropertyRailPlacements
        |> Map.toList
        |> List.choose (fun ((currentPairId, key), targetSide) ->
            if currentPairId = pairId && targetSide = side then
                Some key.Header
            else
                None
        )

    let private railPlacement pairId header (uiState: UiState) =
        uiState.PropertyRailPlacements |> Map.tryFind (pairId, { Header = header })

    let private hasPaletteHeaderForSide pairId side header uiState =
        State.Palette.headersForSide pairId side uiState |> List.contains header

    let private defaultRailSide header model =
        if hasHeaderForSide ProvenanceSide.Output header model then
            Some ProvenanceSide.Output
        elif hasHeaderForSide ProvenanceSide.Input header model then
            Some ProvenanceSide.Input
        else
            None

    let private referencedPropertyValuesForHeader header model =
        [
            yield! setsForSide ProvenanceSide.Input model |> Map.toList
            yield! setsForSide ProvenanceSide.Output model |> Map.toList
        ]
        |> List.collect (fun (_, set) -> ProvenanceSet.effectivePropertyValueIds set)
        |> List.distinct
        |> List.choose (fun propertyValueId -> model.PropertyValues.TryFind propertyValueId)
        |> List.filter (fun propertyValue -> propertyValue.Header = header)

    let private hasPreviousOrigin session pairId header model =
        referencedPropertyValuesForHeader header model
        |> List.exists (fun propertyValue ->
            match Session.propertyValueOriginInSession pairId ProvenanceSide.Input propertyValue.Id session with
            | Some(PropertyOrigin.UpstreamLayer _)
            | Some(PropertyOrigin.PreviousContext _) -> true
            | _ -> false
        )

    let private defaultRailSideInSession session pairId header model =
        if hasPreviousOrigin session pairId header model then
            Some ProvenanceSide.Input
        else
            defaultRailSide header model

    let private isValidRailSide pairId side header model uiState =
        hasHeaderForSide side header model
        || hasPaletteHeaderForSide pairId side header uiState

    let private propertyRailHeadersForSideUsing defaultSideForHeader pairId side model uiState =
        let paletteHeaders = State.Palette.headersForSide pairId side uiState

        let knownHeaders =
            [ yield! headersForModel model; yield! paletteHeaders ] |> List.distinct

        [
            yield! headersForModel model
            yield! placedHeadersForSide pairId side uiState
            yield! paletteHeaders
        ]
        |> List.distinct
        |> List.filter (fun header -> knownHeaders |> List.contains header)
        |> List.filter (fun header ->
            match railPlacement pairId header uiState with
            | Some targetSide when isValidRailSide pairId targetSide header model uiState -> targetSide = side
            | Some _ ->
                defaultSideForHeader header = Some side
                || hasPaletteHeaderForSide pairId side header uiState
            | None ->
                defaultSideForHeader header = Some side
                || (defaultSideForHeader header).IsNone
                   && hasPaletteHeaderForSide pairId side header uiState
        )
        |> List.sortBy (fun header -> header.Category.Name)

    let propertyRailHeadersForSide pairId side model uiState =
        propertyRailHeadersForSideUsing (fun header -> defaultRailSide header model) pairId side model uiState

    let propertyRailHeadersForSideInSession session pairId side model uiState =
        propertyRailHeadersForSideUsing
            (fun header -> defaultRailSideInSession session pairId header model)
            pairId
            side
            model
            uiState

    let propertyValuesForSideHeader pairId side header (model: ProvenanceModel) uiState =
        let modelValues =
            setsForSide side model
            |> Map.toList
            |> List.collect (fun (_, set) -> ProvenanceSet.effectivePropertyValueIds set)
            |> List.distinct
            |> List.choose (fun propertyValueId -> model.PropertyValues.TryFind propertyValueId)
            |> List.filter (fun propertyValue -> propertyValue.Header = header)

        [
            yield! modelValues
            yield! State.Palette.valuesForHeader pairId side header uiState
        ]
        |> List.groupBy (fun propertyValue -> propertyValue.Value, propertyValue.Unit)
        |> List.map (fun (_, values) -> values |> List.sortBy (fun value -> value.Id) |> List.head)
        |> List.sortBy (fun propertyValue -> Formatting.formatValue propertyValue.Value propertyValue.Unit)

    let railProjection pairId side model uiState =
        let headers = propertyRailHeadersForSide pairId side model uiState

        let valuesByHeader =
            headers
            |> List.map (fun header -> header, propertyValuesForSideHeader pairId side header model uiState)
            |> Map.ofList

        let expandedHeaders =
            headers
            |> List.filter (fun header -> State.PropertyExpansion.isExpanded pairId side header uiState)
            |> Set.ofList

        let canSwitchHeaders =
            headers
            |> List.filter (fun header -> canSwitchHeader header model)
            |> Set.ofList

        {
            Headers = headers
            ValuesByHeader = valuesByHeader
            ExpandedHeaders = expandedHeaders
            CanSwitchHeaders = canSwitchHeaders
            StatsByHeader = Map.empty
            BadgeByHeader = Map.empty
            ColorByHeader = Map.empty
            OriginByHeader = Map.empty
            OriginFilterOptions = []
        }

module Search =

    let contains (needle: string) (haystack: string) =
        let needle = if isNull needle then "" else needle.Trim()

        if System.String.IsNullOrWhiteSpace needle then
            true
        elif isNull haystack then
            false
        else
            haystack.ToLowerInvariant().Contains(needle.ToLowerInvariant())

module PropertyProjection =

    open Swate.Components.Shared.ProvenanceGrouping.Types
    open Swate.Components.Shared.ProvenanceGrouping.Session
    open Swate.Components.Page.ProvenanceGrouping.Types

    let propertyStatsForSide
        (side: ProvenanceSide)
        (header: ProvenancePropertyHeader)
        (model: ProvenanceModel)
        : PropertyStats =
        let sets = PropertyRails.setsForSide side model
        let setCount = sets.Count

        let distinctValues =
            sets
            |> Map.toList
            |> List.collect (fun (_, set) ->
                ProvenanceSet.effectivePropertyValueIds set
                |> List.choose (fun id -> model.PropertyValues.TryFind id)
                |> List.filter (fun pv -> pv.Header = header)
                |> List.map (fun pv -> pv.Value, pv.Unit)
            )
            |> List.distinct

        let setsWithValue =
            sets
            |> Map.toList
            |> List.filter (fun (_, set) ->
                ProvenanceSet.effectivePropertyValueIds set
                |> List.exists (fun id ->
                    model.PropertyValues.TryFind id |> Option.exists (fun pv -> pv.Header = header)
                )
            )
            |> List.length

        {
            Header = header
            DistinctValueCount = distinctValues.Length
            SetsWithValueCount = setsWithValue
            TotalSetCount = setCount
        }

    let badgeForStats (stats: PropertyStats) : PropertyCountBadge =
        if stats.DistinctValueCount = 1 && stats.SetsWithValueCount = stats.TotalSetCount then
            Hide
        elif stats.DistinctValueCount = 1 && stats.SetsWithValueCount < stats.TotalSetCount then
            Coverage(stats.SetsWithValueCount, stats.TotalSetCount)
        else
            DistinctValues stats.DistinctValueCount

    let headerMatchesProjectedValues
        (searchText: string)
        (header: ProvenancePropertyHeader)
        (projectedValues: ProvenancePropertyValue list)
        =
        Search.contains searchText header.Category.Name
        || Search.contains searchText header.Kind.Id
        || Search.contains searchText (ProvenanceKind.displayName header.Kind)
        || projectedValues
           |> List.exists (fun propertyValue ->
               Search.contains searchText (Formatting.formatValue propertyValue.Value propertyValue.Unit)
           )

    let valueCountFilterMatches (filter: PropertyValueCountFilter) (badge: PropertyCountBadge) =
        match filter, badge with
        | PropertyValueCountFilter.Any, _ -> true
        | PropertyValueCountFilter.Singleton, PropertyCountBadge.Hide -> true
        | PropertyValueCountFilter.Singleton, PropertyCountBadge.Coverage _ -> true
        | PropertyValueCountFilter.Multiple, PropertyCountBadge.DistinctValues n -> n > 1
        | PropertyValueCountFilter.CoverageGap, PropertyCountBadge.Coverage _ -> true
        | _ -> false

    let originFilterMatches (filter: PropertyOriginFilter) (origins: Set<PropertyOrigin>) =
        match filter with
        | PropertyOriginFilter.AnyOrigin -> true
        | PropertyOriginFilter.CurrentOnly ->
            origins
            |> Set.exists (
                function
                | PropertyOrigin.Current _ -> true
                | _ -> false
            )
        | PropertyOriginFilter.AnyUpstream ->
            origins
            |> Set.exists (
                function
                | PropertyOrigin.UpstreamLayer _
                | PropertyOrigin.PreviousContext _ -> true
                | _ -> false
            )
        | PropertyOriginFilter.UpstreamLayer layerId -> origins |> Set.contains (PropertyOrigin.UpstreamLayer layerId)
        | PropertyOriginFilter.PreviousContext(tableName, processName) ->
            origins
            |> Set.exists (
                function
                | PropertyOrigin.PreviousContext source ->
                    source.TableName = Some tableName && source.ProcessName = processName
                | _ -> false
            )

    let sortHeaders
        (sort: PropertySort)
        (statsByHeader: Map<ProvenancePropertyHeader, PropertyStats>)
        (headers: ProvenancePropertyHeader list)
        =
        match sort with
        | PropertySort.ValueCountDesc ->
            headers
            |> List.sortBy (fun header ->
                let count =
                    statsByHeader.TryFind header
                    |> Option.map (fun stats -> stats.DistinctValueCount)
                    |> Option.defaultValue 0

                -count, header.Category.Name, header.Kind.Id
            )
        | PropertySort.NameAsc -> headers |> List.sortBy (fun header -> header.Category.Name, header.Kind.Id)
        | PropertySort.Origin -> headers |> List.sortBy (fun header -> header.Category.Name)

    let originFilterOptions
        (_originByHeader: Map<ProvenancePropertyHeader, Set<PropertyOrigin>>)
        : PropertyOriginFilter list =
        [
            PropertyOriginFilter.AnyOrigin
            PropertyOriginFilter.CurrentOnly
            PropertyOriginFilter.AnyUpstream
        ]

    let railProjectionWithFilters
        (session: ProvenanceSession)
        (pairId: ProvenancePairId)
        (side: ProvenanceSide)
        (model: ProvenanceModel)
        (uiState: UiState)
        : PropertyRails.RailProjection =
        let pair = Session.activePair session
        let filters = uiState.Filters

        let headers =
            PropertyRails.propertyRailHeadersForSideInSession session pairId side model uiState

        let valuesByHeader =
            headers
            |> List.map (fun header ->
                header, PropertyRails.propertyValuesForSideHeader pairId side header model uiState
            )
            |> Map.ofList

        let statsByHeader =
            headers
            |> List.map (fun header -> header, propertyStatsForSide side header model)
            |> Map.ofList

        let originByHeader =
            headers
            |> List.map (fun header ->
                let values = valuesByHeader.[header]

                let origins =
                    values
                    |> List.choose (fun pv -> Session.propertyValueOriginInSession pairId side pv.Id session)
                    |> Set.ofList

                header, origins
            )
            |> Map.ofList

        let badgeByHeader = statsByHeader |> Map.map (fun _ stats -> badgeForStats stats)

        let stableSourceColor (source: PropertyValueSourceInfo) =
            let key =
                [
                    source.TableName |> Option.defaultValue ""
                    source.ProcessName |> Option.defaultValue ""
                    yield! source.InputNames
                    yield! source.OutputNames
                ]
                |> String.concat "|"

            let index = (hash key |> abs) % State.PropertyColors.palette.Length
            State.PropertyColors.palette.[index]

        let resolvedColorForHeader header origins =
            match uiState.PropertyColors.ManualPropertyColors |> Map.tryFind { Header = header } with
            | Some color -> Some color
            | None ->
                match origins |> Set.toList with
                | [ PropertyOrigin.UpstreamLayer layerId ] -> uiState.PropertyColors.LayerColors |> Map.tryFind layerId
                | [ PropertyOrigin.PreviousContext source ] -> Some(stableSourceColor source)
                | _ -> None

        let colorByHeader =
            headers
            |> List.map (fun header -> header, resolvedColorForHeader header originByHeader.[header])
            |> Map.ofList

        let filtered =
            headers
            |> List.filter (fun header ->
                let badge = badgeByHeader.[header]
                let values = valuesByHeader.[header]
                let origins = originByHeader.[header]

                headerMatchesProjectedValues filters.SearchText header values
                && valueCountFilterMatches filters.ValueCountFilter badge
                && originFilterMatches filters.OriginFilter origins
            )

        let sorted = sortHeaders filters.PropertySort statsByHeader filtered

        let expandedHeaders =
            sorted
            |> List.filter (fun header -> State.PropertyExpansion.isExpanded pairId side header uiState)
            |> Set.ofList

        let canSwitchHeaders =
            sorted
            |> List.filter (fun header -> PropertyRails.canSwitchHeader header model)
            |> Set.ofList

        {
            Headers = sorted
            ValuesByHeader = sorted |> List.map (fun header -> header, valuesByHeader.[header]) |> Map.ofList
            StatsByHeader = statsByHeader
            BadgeByHeader = badgeByHeader
            ColorByHeader = sorted |> List.map (fun header -> header, colorByHeader.[header]) |> Map.ofList
            OriginByHeader = originByHeader
            OriginFilterOptions = originFilterOptions originByHeader
            ExpandedHeaders = expandedHeaders
            CanSwitchHeaders = canSwitchHeaders
        }

/// Derives endpoint defaults, identities, and display headers for empty-side creation.
module Endpoints =

    open Swate.Components.Shared.ProvenanceGrouping.Types

    let fallbackKind: ProvenanceKind =
        ProvenanceKind.create "editor:endpoint" "Endpoint"

    let endpointKindOptions () : ProvenanceKind list = [
        ProvenanceKind.create "arc-isa:endpoint:source" "Source"
        ProvenanceKind.create "arc-isa:endpoint:sample" "Sample"
        ProvenanceKind.create "arc-isa:endpoint:material" "Material"
        ProvenanceKind.create "arc-isa:endpoint:data" "Data"
    ]

    let defaultEndpointKind () : ProvenanceKind =
        endpointKindOptions () |> List.tryHead |> Option.defaultValue fallbackKind

    let endpointKindIdentity (kind: ProvenanceKind) = kind.Id

    let endpointHeader side (kind: ProvenanceKind) =
        let prefix = if side = ProvenanceSide.Input then "Input" else "Output"
        let label = ProvenanceKind.displayName kind

        {
            Kind = kind
            Text = $"{prefix} [{label}]"
        }

/// Projects the active session pair into renderable groups, connections, and layer commands.
module Display =

    open Swate.Components.Shared.ProvenanceGrouping.Types
    open Swate.Components.Shared.ProvenanceGrouping.Grouping
    open Swate.Components.Shared.ProvenanceGrouping.Session
    open Swate.Components.Page.ProvenanceGrouping.Types

    let displayPair session uiState =
        let layer = Session.activeLayer session
        let inputState = State.Sides.get layer.InputSideId uiState
        let outputState = State.Sides.get layer.OutputSideId uiState

        let assignments =
            [
                yield! inputState.GroupingAssignments
                yield! outputState.GroupingAssignments
            ]
            |> List.distinct

        let inputs =
            displayGroupsForAssignments layer.Model ProvenanceSide.Input assignments

        let outputs =
            displayGroupsForAssignments layer.Model ProvenanceSide.Output assignments

        layer, inputs, outputs, displayConnections layer.Model inputs outputs

    let setsInGroups pairId (groups: DisplayGroup list) selectedIds =
        groups
        |> List.filter (fun (group: DisplayGroup) -> selectedIds |> Set.contains (pairId, group.Id))
        |> List.collect (fun (group: DisplayGroup) ->
            group.Members |> List.map (fun (member': DisplayMember) -> member'.SetId)
        )
        |> List.distinct

    let layerCommand pairId inputGroups outputGroups uiState =
        let inputs =
            setsInGroups pairId inputGroups uiState.SelectedInputs
            |> List.map (fun id -> ProvenanceSide.Input, id)

        let outputs =
            setsInGroups pairId outputGroups uiState.SelectedOutputs
            |> List.map (fun id -> ProvenanceSide.Output, id)

        {
            AddLayerCommand.SelectedSets = inputs @ outputs
        }

    let sortGroups (sort: GroupSort) (connections: DisplayConnection list) (groups: DisplayGroup list) =
        match sort with
        | GroupSort.NameAsc -> groups |> List.sortBy (fun group -> group.Id)
        | GroupSort.MemberCountDesc -> groups |> List.sortByDescending (fun group -> group.Members.Length)
        | GroupSort.ConnectionCountDesc ->
            groups
            |> List.sortByDescending (fun group ->
                connections
                |> List.sumBy (fun connection ->
                    if connection.SourceGroupId = group.Id || connection.TargetGroupId = group.Id then
                        connection.ConnectionIds.Length
                    else
                        0
                )
            )

/// Plans how dropped property values should be added or overwritten across a target group.
module ValueAssignment =

    open Swate.Components.Shared.ProvenanceGrouping.Types
    open Swate.Components.Shared.ProvenanceGrouping.Edit
    open Swate.Components.Shared.ProvenanceGrouping.Grouping
    open Swate.Components.Shared.ProvenanceGrouping.Session
    open Swate.Components.Page.ProvenanceGrouping.Types

    let private targetForGroup side (group: DisplayGroup) =
        let ids = group.Members |> List.map (fun m -> m.SetId)

        match side with
        | ProvenanceSide.Input -> ProvenancePropertyTarget.InputSets ids
        | ProvenanceSide.Output -> ProvenancePropertyTarget.OutputSets ids

    let private memberValuesForHeader header (model: ProvenanceModel) (member': DisplayMember) =
        member'.PropertyValueIds
        |> List.distinct
        |> List.choose (fun propertyValueId -> model.PropertyValues.TryFind propertyValueId)
        |> List.filter (fun propertyValue -> propertyValue.Header = header)

    let planPropertyValueDrop
        (source: ValueAssignmentSource)
        (group: DisplayGroup)
        (model: ProvenanceModel)
        : Result<ValueAssignmentPlan, ValueAssignmentError> =
        let memberValues =
            group.Members
            |> List.map (fun member' -> member'.SetId, memberValuesForHeader source.Header model member')

        if memberValues.IsEmpty then
            Error ValueAssignmentError.EmptyTarget
        else
            let membersWithMultipleValues =
                memberValues
                |> List.choose (fun (setId, values) -> if values.Length > 1 then Some setId else None)

            if not membersWithMultipleValues.IsEmpty then
                Error(ValueAssignmentError.MultiplePropertyValues(source.Header, membersWithMultipleValues))
            elif memberValues |> List.forall (fun (_, values) -> values.IsEmpty) then
                Ok(
                    AddCurrent {
                        Target = targetForGroup group.Side group
                        CopiedFrom = source.CopiedFrom
                        Header = source.Header
                        Value = source.Value
                        Unit = source.Unit
                    }
                )
            elif memberValues |> List.forall (fun (_, values) -> values.Length = 1) then
                Ok(
                    ConfirmOverwrite {
                        Target = targetForGroup group.Side group
                        ExistingValueIds =
                            memberValues
                            |> List.collect (fun (_, values) -> values |> List.map (fun value -> value.Id))
                            |> List.distinct
                        Header = source.Header
                        Value = source.Value
                        Unit = source.Unit
                    }
                )
            else
                Error(ValueAssignmentError.MixedPropertyValueCounts source.Header)

    let private combineGroupsForAssignment (groups: DisplayGroup list) : DisplayGroup option =
        match groups with
        | [] -> None
        | head :: _ ->
            let allMembers =
                groups
                |> List.collect (fun g -> g.Members)
                |> List.distinctBy (fun m -> m.SetId)

            Some { head with Members = allMembers }

    let planPropertyValueDropToGroups
        (source: ValueAssignmentSource)
        (groups: DisplayGroup list)
        (model: ProvenanceModel)
        : Result<PropertyAssignmentBatch, ValueAssignmentError> =
        groups
        |> List.groupBy (fun group -> group.Side)
        |> List.fold
            (fun result (side, sideGroups) ->
                result
                |> Result.bind (fun (batch: PropertyAssignmentBatch) ->
                    match combineGroupsForAssignment sideGroups with
                    | None -> Ok batch
                    | Some combinedGroup ->
                        planPropertyValueDrop source combinedGroup model
                        |> Result.map (fun plan ->
                            match plan with
                            | AddCurrent command -> {
                                batch with
                                    Adds = batch.Adds @ [ command ]
                              }
                            | ConfirmOverwrite warning -> {
                                batch with
                                    Overwrites = batch.Overwrites @ [ warning ]
                              }
                        )
                )
            )
            (Ok { Adds = []; Overwrites = [] })

    let selectedTargetGroupsForDrop
        (pairId: ProvenancePairId)
        (dropSide: ProvenanceSide)
        (dropGroupId: string)
        (selectedInputs: Set<ProvenancePairId * string>)
        (selectedOutputs: Set<ProvenancePairId * string>)
        (findGroup: ProvenanceSide -> string -> DisplayGroup option)
        : DisplayGroup list =
        let idsForPair selected =
            selected
            |> Set.filter (fun (currentPairId, _) -> currentPairId = pairId)
            |> Set.map snd

        let inputIds = idsForPair selectedInputs
        let outputIds = idsForPair selectedOutputs

        let dropIsSelected =
            match dropSide with
            | ProvenanceSide.Input -> inputIds.Contains dropGroupId
            | ProvenanceSide.Output -> outputIds.Contains dropGroupId

        if dropIsSelected && (not inputIds.IsEmpty || not outputIds.IsEmpty) then
            [
                yield! inputIds |> Set.toList |> List.choose (findGroup ProvenanceSide.Input)
                yield! outputIds |> Set.toList |> List.choose (findGroup ProvenanceSide.Output)
            ]
        else
            findGroup dropSide dropGroupId |> Option.toList

/// Builds property-value views with source and origin info for display.
module PropertyValueViewing =

    open Swate.Components.Shared.ProvenanceGrouping.Types
    open Swate.Components.Shared.ProvenanceGrouping.Session
    open Swate.Components.Page.ProvenanceGrouping.Types

    type PropertyValueView = {
        Value: ProvenancePropertyValue
        SourceInfo: PropertyValueSourceInfo option
        Origin: PropertyOrigin option
        Color: ProvenanceColor option
    }

    let buildPropertyValueView
        (pairId: ProvenancePairId)
        (side: ProvenanceSide)
        (session: ProvenanceSession)
        (color: ProvenanceColor option)
        (value: ProvenancePropertyValue)
        : PropertyValueView =
        let sourceInfo = Session.propertyValueSourceInfo (Session.activePair session) value
        let origin = Session.propertyValueOriginInSession pairId side value.Id session

        {
            Value = value
            SourceInfo = sourceInfo
            Origin = origin
            Color = color
        }
