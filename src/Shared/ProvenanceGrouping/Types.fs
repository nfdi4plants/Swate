module Swate.Components.Shared.ProvenanceGrouping.Types

/// Name of a study, assay, or run table as known by the caller.
type ProvenanceTableName = string

/// Optional process name from the source model.
/// This is metadata for writeback/disambiguation, not a public process ID.
type ProvenanceProcessName = string

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

/// Normalized kind of an input or output table header.
/// Mirrors the relevant ARCtrl `IOType` cases without exposing ARCtrl types.
[<RequireQualifiedAccess>]
type ProvenanceIOKind =
    /// Source-like endpoint.
    | Source
    /// Sample-like endpoint.
    | Sample
    /// Data/file-like endpoint.
    | Data
    /// Material-like endpoint.
    | Material
    /// Source model provided a custom input/output header kind.
    | FreeText of string
    /// Adapter could not classify the endpoint kind.
    | Unknown

/// Header metadata for a loaded input/output endpoint.
type ProvenanceIOHeader =
    {
        /// Normalized input/output kind used for behavior decisions.
        Kind: ProvenanceIOKind
        /// Original or display-ready header text, such as `Input [Sample Name]`.
        Text: string
    }

/// Normalized kind of editable provenance property.
[<RequireQualifiedAccess>]
type ProvenancePropertyKind =
    /// Characteristic value on an input or output material-like endpoint.
    | Characteristic
    /// Factor value, normally on outputs.
    | Factor
    /// Process parameter value.
    | Parameter
    /// Component value when source tables expose components.
    | Component

/// Small ontology term projection used for property categories, units, and term values.
type ProvenanceTerm =
    {
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
type ProvenancePropertyHeader =
    {
        /// Category family, such as characteristic, factor, or parameter.
        Kind: ProvenancePropertyKind
        /// Category term, such as Species, Temperature, or Replicate.
        Category: ProvenanceTerm
    }

/// Source metadata needed to update an existing property value in its source model.
/// This is a writeback anchor, not a graph edge and not an ARCtrl object reference.
type ProvenanceWritebackAnchor =
    {
        /// Source table containing the property value occurrence.
        TableName: ProvenanceTableName
        /// Optional source process name when the adapter can provide one.
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
type ProvenancePropertyValue =
    {
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
type ProvenanceSet =
    {
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
    }

/// One exact connection between a loaded input endpoint and a loaded output endpoint.
/// Previous-table connections are intentionally not represented here.
type ProvenanceConnection =
    {
        /// Swate-local stable connection ID.
        Id: ProvenanceConnectionId
        /// Loaded table containing this editable connection.
        TableName: ProvenanceTableName
        /// Optional process name associated with this connection.
        ProcessName: ProvenanceProcessName option
        /// Loaded input endpoint ID.
        InputSetId: ProvenanceSetId
        /// Loaded output endpoint ID.
        OutputSetId: ProvenanceSetId
    }

/// Complete provenance projection for one loaded table session.
/// The model stores loaded endpoints, their property value pointers, and loaded connections.
type ProvenanceModel =
    {
        /// The table currently opened and editable in the viewer.
        LoadedTableName: ProvenanceTableName
        /// Shared property value occurrence store.
        PropertyValues: Map<ProvenancePropertyValueId, ProvenancePropertyValue>
        /// First-class loaded input endpoints, keyed by `ProvenanceSet.Id`.
        InputSets: Map<ProvenanceSetId, ProvenanceSet>
        /// First-class loaded output endpoints, keyed by `ProvenanceSet.Id`.
        OutputSets: Map<ProvenanceSetId, ProvenanceSet>
        /// Editable loaded-table connections.
        Connections: Map<ProvenanceConnectionId, ProvenanceConnection>
    }
