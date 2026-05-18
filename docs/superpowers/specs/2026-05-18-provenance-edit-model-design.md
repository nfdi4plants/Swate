# Provenance Edit Model Design

## Correction Scope

This design is for Swate's F#/Fable component library. The production model belongs in `.fs` files under `src/Components/src/ProvenanceGrouping`, with Storybook only acting as the browser preview layer.

The Swate model must not expose ARCtrl types as its public model because the same component must later accept import sources other than ARCtrl.

## Purpose

Build a Swate-owned provenance edit model that can:

- Represent one loaded study, assay, or run table.
- Treat the loaded table's actual inputs and outputs as first-class editable items.
- Represent previous study, assay, or run tables as collapsed property value context.
- Preserve repeated property value occurrences.
- Support grouped table/block display without graph layout.
- Support full editing and addition of loaded-table property values and loaded-table connections.
- Support editing of existing collapsed property values and simple reuse of them on existing loaded connections.
- Produce explicit writeback patches that a caller can later apply to ARC tables or another source model.

The viewer opens one loaded table. That loaded table provides the visible input and output sides. Previous tables may be imported with it so their provenance properties can be used for grouping and existing-value edits, but their graph is not expanded in the Swate model.

## ARCtrl Reference Read

The local ARCtrl reference is `C:\Users\jonat\source\repos\ARCtrl`.

Relevant ARCtrl behavior:

- `src\Core\Process\Process.fs`
  - A process has `Name`, `ParameterValues`, `Inputs`, and `Outputs`.
  - Parameters describe the input/output process context.
- `src\Core\Process\ProcessInput.fs`
  - Inputs are `Source`, `Sample`, `Data`, or `Material`.
  - `ProcessInput.TryName` and `ProcessInput.Name` expose the actual input name.
  - Source, sample, and material inputs can carry characteristics.
  - Data inputs do not carry characteristics.
- `src\Core\Process\ProcessOutput.fs`
  - Outputs are `Sample`, `Data`, or `Material`.
  - `ProcessOutput.TryName` and `ProcessOutput.Name` expose the actual output name.
  - Sample and material outputs can carry characteristics.
  - Sample outputs can carry factors.
  - Data and material outputs do not carry factors.
- `src\Core\Table\CompositeHeader.fs`
  - `CompositeHeader.Input` and `CompositeHeader.Output` carry the input/output header kind.
  - `CompositeHeader.Characteristic`, `Factor`, `Parameter`, and `Component` carry the property header category.
- `src\ARCtrl\Conversion\Process.fs`
  - Table rows are converted into process-like values.
  - `CompositeHeader.Parameter` becomes process parameter values.
  - `CompositeHeader.Characteristic` becomes characteristics on process inputs or outputs.
  - `CompositeHeader.Factor` becomes factors on process outputs.

The Swate model keeps those semantics, but it does not copy the ARCtrl object model. The public model does not need a process graph, row index, column index, or `ProcessId`. When an ARCtrl adapter exists, it can read actual loaded input/output names from `ProcessInput.Name` and `ProcessOutput.Name` and pass plain Swate records into this model.

## Component Boundary

The reusable model lives in `src/Components`.

Recommended files:

- `src/Components/src/ProvenanceGrouping/Types.fs`
- `src/Components/src/ProvenanceGrouping/Import.fs`
- `src/Components/src/ProvenanceGrouping/Grouping.fs`
- `src/Components/src/ProvenanceGrouping/Edit.fs`
- `src/Components/src/ProvenanceGrouping/Fixtures.fs`

These are non-component F# files, so their modules follow the component design rule:

```fsharp
module Swate.Components.ProvenanceGrouping.Types
```

The public model must not use `ARCtrl.Process.Process`, `ProcessInput`, `ProcessOutput`, `ProcessParameterValue`, `MaterialAttributeValue`, or `FactorValue` in its signatures. ARCtrl-specific conversion can exist outside this core model, or as a thin adapter that returns plain import DTOs before entering the model.

## Model Principles

Use a reduced loaded-endpoint/property-value projection.

The model is not a table snapshot and not a graph model. It stores:

- Loaded input sets and loaded output sets as the actual visible input/output names from the loaded table.
- Property value occurrences in a shared store.
- Pointers from loaded input/output sets to property value occurrence IDs.
- Loaded-table input/output connections between loaded set IDs.
- Optional writeback anchors on imported property values, especially collapsed previous-context values.

Input/output sets are first-class loaded table items. A set is the thing the user sees as an input or output item before grouping. Its `Name` is the actual loaded input/output name, and its `Header` is the loaded table input/output header. Display groups are derived later and must not be confused with these sets.

The set record does not store side. Native loaded-table role is implied by whether the set lives in `InputSets` or `OutputSets`. Current left/right display role is projection state because outputs can become the input side of a later edit layer.

Property values do not provide the loaded input/output display names. Loaded names come from `ProvenanceSet.Name`. A property value only provides a key/value occurrence plus optional source metadata for writeback.

Repeated values are valid. One set may point to multiple values for the same property header. When such a property is used for grouping, the set appears in each group that matches one of its values.

Previous provenance is collapsed. Previous tables contribute property values and writeback anchors, but previous-table input/output sets, rows, and connections are not represented as public model entities. A loaded input/output set may point to a previous-context property value so the value can participate in grouping.

If no grouping is applied, sets remain individual display items and show their own loaded input/output names. Ungrouped sets are not collapsed into a single group.

Missing a grouping property is not a group category. If a set has no value for the active grouping property, it remains an individual item for that grouping layer.

## F# Core Types

Use aliases for IDs to keep the model ergonomic in Fable and simple to serialize.

```fsharp
module Swate.Components.ProvenanceGrouping.Types

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

/// Swate-local stable ID for one property value occurrence.
/// Repeated equal values must have separate IDs.
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

/// One concrete key/value occurrence.
/// Sets point to these occurrences; repeated equal values remain separate records.
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
```

## Loaded Table And Previous Context

The model has exactly one loaded table per viewer session.

- The loaded table is the only table whose input/output sets are first-class editable items.
- The loaded table is the only table whose input/output connections are rendered as editable connector lines.
- Previous tables are imported as collapsed property value context.
- Previous-context property values keep `Source` with table/process/header/input-name/output-name information for writeback.
- Existing previous-context values can be edited only when their source anchor is present and compatible.
- New property values, new headers, new loaded connections, and new layer additions are written only to the loaded table.

This is not a "focused table among many equivalent tables" model. The loaded table is the edit target; previous tables are retained provenance context.

## Where Input And Output Names Come From

Loaded input and output names come from the loaded input/output sets:

- `ProvenanceSet.Header` says which loaded input/output header was used, such as `Input [Sample Name]` or `Output [Data]`.
- `ProvenanceSet.Name` is the actual input/output name from the loaded table cell.
- The native loaded-table role is implied by whether the set is stored in `InputSets` or `OutputSets`.
- The current display role is projection state and may change when an output becomes the input side of a later edit layer.

Property values must not be used as the source of loaded input/output display names. Their optional `Source.InputNames` and `Source.OutputNames` exist for writeback, especially when the property came from collapsed previous context.

## Property Semantics

Process parameters:

- Map from ARCtrl `Process.ParameterValues`.
- `Header.Kind = ProvenancePropertyKind.Parameter`.
- A loaded-table parameter value should be attached to the relevant loaded input/output sets or existing loaded connection.
- A collapsed previous parameter value keeps `Source.InputNames` and `Source.OutputNames` in its writeback anchor.

Characteristics:

- Map from `ProcessInput.tryGetCharacteristicValues` and `ProcessOutput.tryGetCharacteristicValues`.
- `Header.Kind = ProvenancePropertyKind.Characteristic`.
- Input characteristics attach to loaded input sets.
- Output characteristics attach to loaded output sets.
- Collapsed previous characteristics keep writeback anchors with the original input or output names.

Factors:

- Map from `ProcessOutput.tryGetFactorValues`.
- `Header.Kind = ProvenancePropertyKind.Factor`.
- Factor values attach to loaded output sets.
- Collapsed previous factors keep writeback anchors with the original output names.

Components:

- Map from `CompositeHeader.Component` when available.
- Included for completeness, but the first UI pass does not need component editing.

## Source Tracking

Source tracking is source-model metadata for writeback. It is not a UI graph pointer.

The writeback anchor contains:

- `TableName`
- `ProcessName option`
- `Header`
- `InputNames`
- `OutputNames`

The model intentionally does not store row index, column position, process ID, or a public process graph.

Rows or process-like occurrences are identified by the source adapter using the table name, optional process name, property header, and input/output names. Existing columns are located by `Header.Kind` and `Header.Category`. Column position does not matter. If a writeback adapter cannot disambiguate a target value from those fields, the adapter must return an explicit error instead of guessing.

## Import Boundary

The core import API accepts plain F# records, not ARCtrl types. The adapter is responsible for converting source-specific rows/processes into the reduced loaded-endpoint/property-value projection.

```fsharp
module Swate.Components.ProvenanceGrouping.Import

open Swate.Components.ProvenanceGrouping.Types

type ImportedPropertyValue =
    {
        Id: ProvenancePropertyValueId
        Header: ProvenancePropertyHeader
        Value: ProvenanceValue
        Unit: ProvenanceTerm option
        Source: ProvenanceWritebackAnchor option
    }

type ImportedSet =
    {
        Id: ProvenanceSetId
        TableName: ProvenanceTableName
        Header: ProvenanceIOHeader
        Name: string
        PropertyValueIds: ProvenancePropertyValueId list
    }

type ImportedConnection =
    {
        Id: ProvenanceConnectionId
        TableName: ProvenanceTableName
        ProcessName: ProvenanceProcessName option
        InputSetId: ProvenanceSetId
        OutputSetId: ProvenanceSetId
    }

type ImportedProvenance =
    {
        LoadedTableName: ProvenanceTableName
        PropertyValues: ImportedPropertyValue list
        InputSets: ImportedSet list
        OutputSets: ImportedSet list
        Connections: ImportedConnection list
    }

type ImportResult =
    {
        Model: ProvenanceModel
        Warnings: string list
    }

val fromImportedProvenance : imported: ImportedProvenance -> ImportResult
```

An ARCtrl caller can map process-like data to these records before calling the model import function:

- Loaded process inputs become `ImportedSet` values in `InputSets` with `Header` and actual input `Name`.
- Loaded process outputs become `ImportedSet` values in `OutputSets` with `Header` and actual output `Name`.
- `Process.ParameterValues` become `ImportedPropertyValue` values and are referenced by the relevant loaded sets or loaded connection.
- Input/output `MaterialAttributeValue` values become `ImportedPropertyValue` values and are referenced by the owning loaded input/output sets.
- Output `FactorValue` values become `ImportedPropertyValue` values and are referenced by the owning loaded output sets.
- Previous-context values become `ImportedPropertyValue` values with a `Source` anchor and may be referenced by loaded sets.
- Loaded-table input/output relationships become `ImportedConnection` values.

## Grouping Projection

The display layer is derived from the reduced model.

Only the loaded table creates the main input and output display groups. Previous context tables do not create additional visible table columns by themselves. Their property values are available because imported loaded-table sets can point to those property value occurrences.

```fsharp
module Swate.Components.ProvenanceGrouping.Grouping

open Swate.Components.ProvenanceGrouping.Types

type GroupingKey =
    {
        Header: ProvenancePropertyHeader
    }

type DisplayMember =
    {
        SetId: ProvenanceSetId
        Name: string
        PropertyValueIds: ProvenancePropertyValueId list
    }

type DisplayGroup =
    {
        Id: string
        TableName: ProvenanceTableName
        Side: ProvenanceSide
        GroupingValues: (GroupingKey * ProvenanceValue) list
        Members: DisplayMember list
    }

type DisplayConnection =
    {
        Id: string
        SourceGroupId: string
        TargetGroupId: string
        ConnectionIds: ProvenanceConnectionId list
    }
```

Grouping behavior:

- With no grouping keys, each loaded-table set is one display group labeled by `ProvenanceSet.Name`.
- Grouping by one or more keys groups loaded sets by the actual property values they point to.
- Multiple values for the same key duplicate the set into multiple display groups.
- Missing values do not create a "missing" group.
- Non-grouped properties are ignored for group identity.
- Sorting by a non-grouped property sorts members inside groups. With no groups, it sorts the individual item groups.
- A group may include property values sourced from previous context tables when the loaded set points to those occurrences.

Connection behavior:

- The model stores loaded-table `ProvenanceConnection` values between loaded input and output set IDs.
- The view aggregates those into group-level `DisplayConnection` lines.
- A group line must never be expanded as a synthetic Cartesian product.
- Clicking a group line expands both connected groups and shows only the represented loaded input/output set connections from the listed `ConnectionIds`.

## Parameter Value Controls

Grouping controls are generated from property occurrences that are referenced by loaded sets.

- Values shared by both current sides, or present only on the input side, appear on the left by default.
- Values present only on outputs appear on the right by default.
- Shared values can be dragged between left and right control areas.
- Controls show unique property values, not one chip per occurrence.
- Each property value control connects to the display groups it applies to by rendered connector lines, not by a text list.

If a value is dragged onto a loaded group, all members of that group receive a new or reused loaded-table property value occurrence in the current edit scope. Multiple values for the same property header may coexist on the same set.

If a collapsed previous value is dragged onto loaded input/output sets or an existing loaded connection, Swate creates a loaded-table occurrence copied from that value. The previous-context value remains anchored to its original source and is not moved.

## Edit Rules

Loaded-table edits:

- Loaded input/output sets support full property value editing.
- Loaded input/output sets support adding new property values.
- Loaded input/output sets support creating new loaded input/output connections.
- Loaded connections may receive new parameter values when the target is an existing loaded connection.
- New loaded values are written to `model.LoadedTableName`.

Existing value edits:

- Target an existing `ProvenancePropertyValueId`.
- If the value has a `Source` anchor, preserve table/process/header/input/output writeback context.
- If the value is a loaded-table value without an explicit `Source`, derive the writeback target from the loaded sets that point to the value.
- Produce an update patch.

Previous-context edits:

- Allowed only for existing property occurrences with a compatible `Source` anchor.
- Must not invent rows, inputs, outputs, connections, or hidden graph structure in previous tables.
- Must never create a new property value or new header in a previous table.
- Simple reuse of a previous value on loaded sets or existing loaded connections creates a loaded-table copy and leaves the previous source untouched.

Connection edits:

- The model stores loaded-table connections between actual loaded input set IDs and output set IDs.
- Dragging group-to-group creates all individual loaded input/output set connections required by the visible group operation.
- Fan-in and fan-out are allowed: one input set may connect to many output sets, and one output set may connect to many input sets.

Layer creation:

- Creating a new layer inside the loaded table uses the current selection as the new layer's input sets.
- The selection may contain loaded sets currently shown on input and output sides.
- If nothing is selected, the UI may use the current right-side output sets as the default, but this is a UI policy rather than a model requirement.

## Patch Output

Edits return patches; they do not mutate ARC tables directly.

```fsharp
module Swate.Components.ProvenanceGrouping.Edit

open Swate.Components.ProvenanceGrouping.Types

[<RequireQualifiedAccess>]
type EditError =
    | PropertyNotFound of ProvenancePropertyValueId
    | SetNotFound of ProvenanceSetId
    | ConnectionNotFound of ProvenanceConnectionId
    | TableNotLoaded of ProvenanceTableName
    | MissingSourceAnchor of ProvenancePropertyValueId
    | DuplicateHeader of tableName: ProvenanceTableName * header: ProvenancePropertyHeader
    | PreviousContextCreationNotAllowed of tableName: ProvenanceTableName
    | PreviousContextConnectionCreationNotAllowed of tableName: ProvenanceTableName

[<RequireQualifiedAccess>]
type ProvenancePropertyTarget =
    | InputSets of ProvenanceSetId list
    | OutputSets of ProvenanceSetId list
    | Connections of ProvenanceConnectionId list

[<RequireQualifiedAccess>]
type ProvenanceTablePatch =
    | UpdatePropertyValue of
        propertyValueId: ProvenancePropertyValueId *
        source: ProvenanceWritebackAnchor *
        oldValue: ProvenanceValue *
        newValue: ProvenanceValue *
        unit: ProvenanceTerm option
    | AddLoadedPropertyValue of
        target: ProvenancePropertyTarget *
        copiedFrom: ProvenancePropertyValueId option *
        header: ProvenancePropertyHeader *
        value: ProvenanceValue *
        unit: ProvenanceTerm option
    | AddLoadedConnection of
        tableName: ProvenanceTableName *
        processName: ProvenanceProcessName option *
        inputSetId: ProvenanceSetId *
        outputSetId: ProvenanceSetId

type EditResult =
    Result<ProvenanceModel * ProvenanceTablePatch list, EditError>
```

Patch application remains outside the reusable component. It may use ARCtrl, another table model, or a test fixture. For loaded targets, the patch consumer resolves `ProvenanceSetId` and `ProvenanceConnectionId` through the current Swate model to get loaded table names, input/output headers, and actual input/output names. For collapsed previous values, the patch consumer uses the stored `ProvenanceWritebackAnchor`.

## Non-Goals

- No direct ARCtrl types in the public Swate model.
- No graph visualization.
- No public process model.
- No process ID in the public model.
- No `ProvenanceOrigin` field in the core model; patch types and caller session state represent new/imported edit state.
- No row index or column position in the normalized source model.
- No first-class previous-table input/output sets, rows, or connections.
- No creation of new property values, entries, rows, or connections in previous context tables.
- No automatic ontology lookup.
- No production ARC writeback implementation in the first model pass.
