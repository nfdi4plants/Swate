module Swate.Components.Shared.ProvenanceGrouping.Types

/// Name of a study, assay, or run table as known by the caller.
type ProvenanceTableName = string

/// Optional process name from the source model.
/// This is display/logical metadata, not a unique process ID.
type ProvenanceProcessName = string

/// Optional stable source-model process identity for disambiguation/writeback.
/// Adapters should keep generated row or object IDs here instead of appending them to `ProvenanceProcessName`.
type ProvenanceProcessId = string

/// Swate-local stable ID for one loaded input or output endpoint.
/// The actual user-facing input/output name lives on `ProvenanceSet.Name`.
type ProvenanceSetId = string

/// Swate-local stable ID for one loaded input-to-output connection.
type ProvenanceConnectionId = string

/// Swate-local stable ID for one normalized editable property value in the model.
/// Adapters may collapse exact duplicate source occurrences and track multiplicity in source-specific sidecars.
type ProvenancePropertyValueId = string

/// Which collection or display side a projection/helper should use.
/// This is not stored on `ProvenanceSet`; loaded role is implied by `InputSets`/`OutputSets`.
[<RequireQualifiedAccess>]
type ProvenanceSide =
    /// Select loaded input endpoints or render an input-side display group.
    | Input
    /// Select loaded output endpoints or render an output-side display group.
    | Output

/// Source-provided endpoint or property role.
/// Adapters choose stable IDs for their own source model, for example ARC/ISA or another process format.
type ProvenanceKind = {
    /// Stable role identifier owned by the adapter that created the model.
    Id: string
    /// Human-readable label for display and fallback header creation.
    Label: string
}

module ProvenanceKind =

    let create id label : ProvenanceKind = { Id = id; Label = label }

    let displayName (kind: ProvenanceKind) =
        if System.String.IsNullOrWhiteSpace kind.Label then
            kind.Id
        else
            kind.Label

/// Header metadata for a loaded input/output endpoint.
type ProvenanceIOHeader = {
    /// Adapter-provided endpoint role.
    Kind: ProvenanceKind
    /// Original or display-ready header text, such as `Input [Sample Name]`.
    Text: string
}

/// Small ontology term projection used for property categories, units, and term values.
type ProvenanceTerm = {
    /// Human-readable term name.
    Name: string
    /// Optional ontology source name/reference.
    TermSource: string option
    /// Optional ontology accession/IRI.
    TermAccession: string option
}

/// Value projection that is simple to serialize and use from Fable.
[<RequireQualifiedAccess>]
type ProvenanceValue =
    /// Plain string or free text value.
    | Text of string
    /// Integer value.
    | Integer of int
    /// Floating point value.
    | Float of float
    /// Ontology-backed term value.
    | Term of ProvenanceTerm

/// Property key used for grouping, editing, and writeback.
type ProvenancePropertyHeader = {
    /// Adapter-provided property role.
    Kind: ProvenanceKind
    /// Category term, such as Species, Temperature, or Replicate.
    Category: ProvenanceTerm
}

/// Source metadata needed to update an existing property value in its source model.
/// This is a writeback anchor, not a graph edge and not an ARCtrl object reference.
type ProvenanceWritebackAnchor = {
    /// Source table containing the property value occurrence.
    TableName: ProvenanceTableName
    /// Optional source process ID when the adapter needs a unique process identity.
    ProcessId: ProvenanceProcessId option
    /// Optional source process name when the adapter can provide a logical/display name.
    ProcessName: ProvenanceProcessName option
    /// Property header to locate the source column/value family.
    Header: ProvenancePropertyHeader
    /// Source input names that identify the owning context.
    InputNames: string list
    /// Source output names that identify the owning context.
    OutputNames: string list
}

/// One concrete editable key/value in the normalized model.
/// Distinct values must remain distinct; exact duplicate source occurrences may collapse when they are not meaningfully distinguishable in the source-agnostic core model.
type ProvenancePropertyValue = {
    /// Stable Swate-local ID for this occurrence.
    Id: ProvenancePropertyValueId
    /// Property key/category for grouping and writeback.
    Header: ProvenancePropertyHeader
    /// Stored property value.
    Value: ProvenanceValue
    /// Optional unit term.
    Unit: ProvenanceTerm option
    /// Optional writeback anchor.
    /// Loaded values may omit this when the target can be derived from loaded set membership.
    /// Collapsed previous-context values must keep this when they should be editable.
    Source: ProvenanceWritebackAnchor option
}

/// One actual loaded input or output endpoint.
/// This is the first-class UI item before grouping; it is not a collapsed graph node.
type ProvenanceSet = {
    /// Swate-local stable endpoint ID used by connections and display groups.
    Id: ProvenanceSetId
    /// Loaded table this endpoint belongs to.
    TableName: ProvenanceTableName
    /// Loaded input/output header this endpoint came from.
    Header: ProvenanceIOHeader
    /// Actual loaded input/output name from the table cell or source adapter.
    Name: string
    /// Property value occurrences attached to this loaded endpoint.
    PropertyValueIds: ProvenancePropertyValueId list
    /// Property value occurrences inherited through loaded connections, keyed by connection ID.
    /// These pointers are connection-specific so removing a connection can remove the inherited values.
    InheritedPropertyValueIds: Map<ProvenanceConnectionId, ProvenancePropertyValueId list>
}

module ProvenanceSet =

    let private distinct values = values |> List.distinct

    let inheritedPropertyValueIds (set: ProvenanceSet) =
        set.InheritedPropertyValueIds
        |> Map.toList
        |> List.sortBy fst
        |> List.collect snd
        |> distinct

    let effectivePropertyValueIds (set: ProvenanceSet) =
        [
            yield! set.PropertyValueIds
            yield! inheritedPropertyValueIds set
        ]
        |> distinct

    let inheritPropertyValueIds connectionId propertyValueIds (set: ProvenanceSet) =
        let propertyValueIds = propertyValueIds |> distinct

        let inherited =
            if propertyValueIds.IsEmpty then
                set.InheritedPropertyValueIds |> Map.remove connectionId
            else
                set.InheritedPropertyValueIds |> Map.add connectionId propertyValueIds

        {
            set with
                InheritedPropertyValueIds = inherited
        }

    let removeInheritedPropertyValueIds connectionId (set: ProvenanceSet) = {
        set with
            InheritedPropertyValueIds = set.InheritedPropertyValueIds |> Map.remove connectionId
    }

/// One exact connection between a loaded input endpoint and a loaded output endpoint.
/// Previous-table connections are intentionally not represented here.
type ProvenanceConnection = {
    /// Swate-local stable connection ID.
    Id: ProvenanceConnectionId
    /// Loaded table containing this editable connection.
    TableName: ProvenanceTableName
    /// Optional process ID associated with this connection.
    ProcessId: ProvenanceProcessId option
    /// Optional process name associated with this connection.
    ProcessName: ProvenanceProcessName option
    /// Loaded input endpoint ID.
    InputSetId: ProvenanceSetId
    /// Loaded output endpoint ID.
    OutputSetId: ProvenanceSetId
}

/// Complete provenance projection for one loaded table session.
/// The model stores loaded endpoints, their property value pointers, and loaded connections.
type ProvenanceModel = {
    /// The table currently opened and editable in the viewer.
    LoadedTableName: ProvenanceTableName
    /// Shared property value occurrence store.
    PropertyValues: Map<ProvenancePropertyValueId, ProvenancePropertyValue>
    /// First-class loaded input endpoints, keyed by `ProvenanceSet.Id`.
    /// May be empty when the loaded table currently has only outputs.
    InputSets: Map<ProvenanceSetId, ProvenanceSet>
    /// First-class loaded output endpoints, keyed by `ProvenanceSet.Id`.
    /// May be empty when the loaded table currently has only inputs.
    OutputSets: Map<ProvenanceSetId, ProvenanceSet>
    /// Editable loaded-table connections.
    /// May be empty for one-sided loaded tables.
    Connections: Map<ProvenanceConnectionId, ProvenanceConnection>
}

module ProvenanceModel =

    let refreshInheritedOutputProperties (model: ProvenanceModel) =
        let inheritedByOutput =
            model.Connections
            |> Map.toList
            |> List.choose (fun (connectionId, connection) ->
                match
                    model.InputSets.TryFind connection.InputSetId, model.OutputSets.TryFind connection.OutputSetId
                with
                | Some inputSet, Some _ when connection.TableName = model.LoadedTableName ->
                    let propertyValueIds = ProvenanceSet.effectivePropertyValueIds inputSet

                    if propertyValueIds.IsEmpty then
                        None
                    else
                        Some(connection.OutputSetId, connectionId, propertyValueIds)
                | _ -> None
            )
            |> List.groupBy (fun (outputSetId, _, _) -> outputSetId)
            |> List.map (fun (outputSetId, inherited) ->
                outputSetId,
                inherited
                |> List.map (fun (_, connectionId, propertyValueIds) -> connectionId, propertyValueIds)
                |> Map.ofList
            )
            |> Map.ofList

        let outputSets =
            model.OutputSets
            |> Map.map (fun outputSetId outputSet -> {
                outputSet with
                    InheritedPropertyValueIds =
                        inheritedByOutput |> Map.tryFind outputSetId |> Option.defaultValue Map.empty
            })

        { model with OutputSets = outputSets }
