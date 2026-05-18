# Provenance Edit Model Design

## Correction Scope

This design is for Swate's F#/Fable component library. The production model belongs in `.fs` files under `src/Components/src/ProvenanceGrouping`, with Storybook only acting as the browser preview layer.

The ARCtrl process list is a semantic reference. The Swate model must not expose ARCtrl types as its public model because the same model must accept several import sources later.

## Purpose

Build a Swate-owned provenance edit model that can:

- Represent loaded study, assay, or run tables.
- Represent downstream tables created during the viewer workflow.
- Preserve process-level input/output connections.
- Preserve repeated property value occurrences.
- Support grouped table/block display without graph layout.
- Support bulk edits, connection edits, new layers, sorting, and grouping.
- Produce explicit writeback patches that a caller can later apply to ARC tables.

The viewer opens from one loaded study, assay, or run table. New additions in that table and later generated tables are allowed. Previous-table edits are allowed only where the selected existing entries already have a compatible backing process/table location.

## ARCtrl Reference Read

The reference behavior comes from these ARCtrl files:

- `C:\Users\carol\source\repos\ARCtrl\src\Core\Process\Process.fs`
  - `Process` has `Name`, `ParameterValues`, `Inputs`, and `Outputs`.
  - `Process.getParameterValues` reads process parameters.
  - `Process.getInputCharacteristicValues` reads characteristics from inputs.
  - `Process.getOutputCharacteristicValues` reads characteristics from outputs.
  - `Process.getFactorValues` reads factors from process outputs.
- `C:\Users\carol\source\repos\ARCtrl\src\Core\Process\ProcessInput.fs`
  - `ProcessInput = Source | Sample | Data | Material`.
  - Inputs have `TryName`.
  - Source, sample, and material inputs can carry characteristics.
  - Data inputs do not carry characteristics.
- `C:\Users\carol\source\repos\ARCtrl\src\Core\Process\ProcessOutput.fs`
  - `ProcessOutput = Sample | Data | Material`.
  - Outputs have `TryName`.
  - Sample and material outputs can carry characteristics.
  - Sample outputs can carry factors.
  - Data and material outputs do not carry factors.
- `C:\Users\carol\source\repos\ARCtrl\src\Core\Process\ProcessParameterValue.fs`
  - Process parameters have `Category`, `Value`, and `Unit`.
  - As `IPropertyValue`, their additional type is `ProcessParameterValue`.
- `C:\Users\carol\source\repos\ARCtrl\src\Core\Process\MaterialAttributeValue.fs`
  - Characteristics have `Category`, `Value`, and `Unit`.
  - As `IPropertyValue`, their additional type is `MaterialAttributeValue`.
- `C:\Users\carol\source\repos\ARCtrl\src\Core\Process\FactorValue.fs`
  - Factors have `Category`, `Value`, and `Unit`.
  - As `IPropertyValue`, their additional type is `FactorValue`.
- `C:\Users\carol\source\repos\ARCtrl\src\Core\Value.fs`
  - Values are `Ontology`, `Int`, `Float`, or `Name`.
- `C:\Users\carol\source\repos\ARCtrl\src\Core\Table\CompositeHeader.fs`
  - Relevant table headers are `Input`, `Output`, `Characteristic`, `Factor`, `Parameter`, and `Component`.
- `C:\Users\carol\source\repos\ARCtrl\src\ARCtrl\Conversion\Process.fs`
  - Table rows are converted to processes.
  - `CompositeHeader.Parameter` becomes process parameter values.
  - `CompositeHeader.Characteristic` becomes characteristics on process inputs.
  - `CompositeHeader.Factor` becomes factors on process outputs.
  - `processToRows` later writes processes back by aligning inputs and outputs as table rows.

The Swate model keeps the same concepts, but does not copy the ARCtrl object model.

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

Use a normalized process model plus property value occurrences.

The model is not a table snapshot. It stores:

- Tables as edit scopes.
- Entries as named source/sample/data/material objects.
- Processes as table-scoped rows or row-like process occurrences.
- Connections as explicit input-to-output links in a process.
- Property values as occurrences, not as a map keyed by header.

Repeated values are valid. One entry may have multiple values for the same property category. When such a property is used for grouping, the entry appears in each group that matches one of its values.

If no grouping is applied, entries remain individual display items and only show their names. Ungrouped entries are not collapsed into a single group.

Missing a grouping property is not a group category. If an entry has no value for the active grouping property, it remains an individual item for that grouping layer.

## F# Core Types

Use aliases for IDs to keep the model ergonomic in Fable and simple to serialize.

```fsharp
module Swate.Components.ProvenanceGrouping.Types

type ProvenanceTableId = string
type ProvenanceProcessId = string
type ProvenanceEntryId = string
type ProvenanceConnectionId = string
type ProvenancePropertyValueId = string

[<RequireQualifiedAccess>]
type ProvenanceOrigin =
    | Loaded
    | Created

[<RequireQualifiedAccess>]
type ProvenanceTableKind =
    | Study
    | Assay
    | Run
    | Generated

[<RequireQualifiedAccess>]
type ProvenanceEntryKind =
    | Source
    | Sample
    | Data
    | Material
    | Unknown

[<RequireQualifiedAccess>]
type ProvenancePropertyKind =
    | Characteristic
    | Factor
    | Parameter
    | Component

[<RequireQualifiedAccess>]
type ProvenanceAssignmentScope =
    | Entry
    | Process

type ProvenanceTerm =
    {
        Name: string
        TermSource: string option
        TermAccession: string option
    }

[<RequireQualifiedAccess>]
type ProvenanceValue =
    | Text of string
    | Integer of int
    | Float of float
    | Term of ProvenanceTerm

type ProvenanceTable =
    {
        Id: ProvenanceTableId
        Name: string
        Kind: ProvenanceTableKind
        Origin: ProvenanceOrigin
        PreviousTableId: ProvenanceTableId option
    }

type ProvenanceEntry =
    {
        Id: ProvenanceEntryId
        Name: string
        Kind: ProvenanceEntryKind
        TableIds: ProvenanceTableId list
    }

type ProvenanceProcess =
    {
        Id: ProvenanceProcessId
        TableId: ProvenanceTableId
        TableName: string
        InputIds: ProvenanceEntryId list
        OutputIds: ProvenanceEntryId list
        Origin: ProvenanceOrigin
    }

type ProvenanceConnection =
    {
        Id: ProvenanceConnectionId
        TableId: ProvenanceTableId
        ProcessId: ProvenanceProcessId
        SourceEntryId: ProvenanceEntryId
        TargetEntryId: ProvenanceEntryId
        Origin: ProvenanceOrigin
    }

type ProvenancePropertySource =
    {
        TableId: ProvenanceTableId
        TableName: string
        HeaderName: string
        HeaderKind: ProvenancePropertyKind
        ProcessId: ProvenanceProcessId option
        Origin: ProvenanceOrigin
    }

type ProvenancePropertyValue =
    {
        Id: ProvenancePropertyValueId
        Kind: ProvenancePropertyKind
        AssignmentScope: ProvenanceAssignmentScope
        TableId: ProvenanceTableId
        TableName: string
        ProcessId: ProvenanceProcessId option
        InputIds: ProvenanceEntryId list
        OutputIds: ProvenanceEntryId list
        AssignedEntryIds: ProvenanceEntryId list
        Category: ProvenanceTerm
        Value: ProvenanceValue
        Unit: ProvenanceTerm option
        Source: ProvenancePropertySource
    }

type ProvenanceModel =
    {
        Tables: Map<ProvenanceTableId, ProvenanceTable>
        Entries: Map<ProvenanceEntryId, ProvenanceEntry>
        Processes: Map<ProvenanceProcessId, ProvenanceProcess>
        Connections: Map<ProvenanceConnectionId, ProvenanceConnection>
        Properties: Map<ProvenancePropertyValueId, ProvenancePropertyValue>
    }
```

## Property Semantics

Process parameters:

- Map from ARCtrl `Process.ParameterValues`.
- `Kind = ProvenancePropertyKind.Parameter`.
- `AssignmentScope = ProvenanceAssignmentScope.Process`.
- `AssignedEntryIds` normally includes the process inputs and outputs so grouping can expose the value on both sides.
- `InputIds` and `OutputIds` preserve the process context.

Characteristics:

- Map from `ProcessInput.tryGetCharacteristicValues` and `ProcessOutput.tryGetCharacteristicValues`.
- `Kind = ProvenancePropertyKind.Characteristic`.
- `AssignmentScope = ProvenanceAssignmentScope.Entry`.
- `AssignedEntryIds` contains the owning input or output entry.
- Process context is still preserved via `ProcessId`, `InputIds`, and `OutputIds`.

Factors:

- Map from `ProcessOutput.tryGetFactorValues`.
- `Kind = ProvenancePropertyKind.Factor`.
- `AssignmentScope = ProvenanceAssignmentScope.Entry`.
- `AssignedEntryIds` contains the owning output sample entry.

Components:

- Map from `CompositeHeader.Component` when available.
- Included for completeness, but the first UI pass does not need component editing.

## Source Tracking

Source tracking is table/header/process based:

- `TableId`
- `TableName`
- `HeaderName`
- `HeaderKind`
- `ProcessId`
- `Origin`

The model intentionally does not store row index or column position.

Rows are implied by process ID plus the relevant input/output IDs. Existing columns are located by `HeaderName` and `HeaderKind`. Column position does not matter. If writeback cannot disambiguate a target cell from those fields, the writeback adapter must return an explicit error instead of guessing.

## Import Boundary

The core import API accepts plain F# records, not ARCtrl types.

```fsharp
module Swate.Components.ProvenanceGrouping.Import

open Swate.Components.ProvenanceGrouping.Types

type ImportedEntry =
    {
        Id: ProvenanceEntryId
        Name: string
        Kind: ProvenanceEntryKind
    }

type ImportedProperty =
    {
        Id: ProvenancePropertyValueId
        Kind: ProvenancePropertyKind
        AssignmentScope: ProvenanceAssignmentScope
        AssignedEntryIds: ProvenanceEntryId list option
        HeaderName: string
        Category: ProvenanceTerm
        Value: ProvenanceValue
        Unit: ProvenanceTerm option
    }

type ImportedProcess =
    {
        Id: ProvenanceProcessId
        InputIds: ProvenanceEntryId list
        OutputIds: ProvenanceEntryId list
        Inputs: ImportedEntry list
        Outputs: ImportedEntry list
        Properties: ImportedProperty list
    }

type ImportedTable =
    {
        Id: ProvenanceTableId
        Name: string
        Kind: ProvenanceTableKind
        Origin: ProvenanceOrigin
        PreviousTableId: ProvenanceTableId option
        Processes: ImportedProcess list
    }

type ImportResult =
    {
        Model: ProvenanceModel
        Warnings: string list
    }
```

An ARCtrl caller can map `Process list` to these records before calling the model import function:

- `Process.Name` or caller table name -> `ImportedTable.Name`.
- Process row identity -> `ImportedProcess.Id`.
- `Process.Inputs` -> `ImportedEntry` plus `InputIds`.
- `Process.Outputs` -> `ImportedEntry` plus `OutputIds`.
- `Process.ParameterValues` -> `ImportedProperty` with `Kind = Parameter`.
- Input/output `MaterialAttributeValue` -> `ImportedProperty` with `Kind = Characteristic`.
- Output `FactorValue` -> `ImportedProperty` with `Kind = Factor`.

## Grouping Projection

The display layer is derived from the normalized model.

```fsharp
module Swate.Components.ProvenanceGrouping.Grouping

open Swate.Components.ProvenanceGrouping.Types

[<RequireQualifiedAccess>]
type ProvenanceSide =
    | Inputs
    | Outputs

type GroupingKey =
    {
        Kind: ProvenancePropertyKind
        Name: string
    }

type DisplayMember =
    {
        EntryId: ProvenanceEntryId
        EntryName: string
        PropertyValueIds: ProvenancePropertyValueId list
    }

type DisplayGroup =
    {
        Id: string
        TableId: ProvenanceTableId
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

- With no grouping keys, each entry is one display group with one member.
- Grouping by one or more keys groups entries by their actual values.
- Multiple values for the same key duplicate the entry into multiple display groups.
- Missing values do not create a "missing" group.
- Non-grouped properties are ignored for group identity.
- Sorting by a non-grouped property sorts members inside groups. With no groups, it sorts the individual item groups.

Connection behavior:

- The model stores individual `ProvenanceConnection` values.
- The view aggregates those into group-level `DisplayConnection` lines.
- A group line must never be expanded as a Cartesian product.
- Clicking a group line expands both connected groups and shows only the individual connections listed by `ConnectionIds`.

This prevents the previous bug where an output species group expanded to all-to-all item links even though the source data only contained specific connections.

## Parameter Value Controls

Grouping controls are generated from property occurrences.

- Values shared by both current sides, or present only on the input side, appear on the left by default.
- Values present only on outputs appear on the right by default.
- Shared values can be dragged between left and right control areas.
- Controls show unique property values, not one chip per occurrence.
- Each property value control connects to the display groups it applies to by rendered connector lines, not by a text list.

If a value is dragged onto a group, all members of that group receive that value. Multiple values for the same property category may coexist on the same entry.

If an output-only value is assigned to input entries through this interaction, it becomes part of those input entries' property occurrences in the current edit scope.

## Edit Rules

Existing value edits:

- Target an existing `ProvenancePropertyValueId`.
- Preserve source table/header/process context.
- Produce an update patch.
- If this is a change to an existing parameter value, connected outputs that already carry the propagated occurrence are overwritten.

New value creation in current scope:

- Allowed in the loaded table and generated downstream tables.
- Creates new property occurrences.
- If the requested header already exists, add values under that header.
- If the requested header does not exist, add the header and values.
- If the user asks to create a new property header with a duplicate name and kind, return an explicit duplicate-header error.

Previous-table edits:

- Allowed only for existing entries with a compatible source table and process context.
- Must not invent rows, inputs, outputs, or process connections in previous tables.
- If the selected entries do not share a valid backing process/table location, return an explicit error.

Connection edits:

- The model stores item-level connections.
- Dragging group-to-group creates all item-level connections required by that operation or returns an error if the operation cannot be represented without partial state.
- Fan-in and fan-out are allowed: one input may connect to many outputs, and one output may connect to many inputs.

Layer creation:

- Creating a downstream layer uses the current selection as the new layer's inputs.
- The selection may contain a mix of entries currently shown on input and output sides.
- If nothing is selected, the UI may use the current right-side outputs as the default, but this is a UI policy rather than a model requirement.

## Patch Output

Edits return patches; they do not mutate ARC tables directly.

```fsharp
module Swate.Components.ProvenanceGrouping.Edit

open Swate.Components.ProvenanceGrouping.Types

[<RequireQualifiedAccess>]
type EditError =
    | PropertyNotFound of ProvenancePropertyValueId
    | ProcessNotFound of ProvenanceProcessId
    | TableNotFound of ProvenanceTableId
    | MissingProcessContext of ProvenancePropertyValueId
    | DuplicateHeader of tableId: ProvenanceTableId * headerKind: ProvenancePropertyKind * headerName: string
    | PreviousTableRequiresExistingConnectedEntries of tableId: ProvenanceTableId
    | PartialGroupConnectionNotAllowed of sourceGroupId: string * targetGroupId: string

[<RequireQualifiedAccess>]
type ProvenanceTablePatch =
    | UpdatePropertyValue of
        tableId: ProvenanceTableId *
        processId: ProvenanceProcessId *
        headerKind: ProvenancePropertyKind *
        headerName: string *
        inputIds: ProvenanceEntryId list *
        outputIds: ProvenanceEntryId list *
        assignedEntryIds: ProvenanceEntryId list *
        oldValue: ProvenanceValue *
        newValue: ProvenanceValue *
        unit: ProvenanceTerm option
    | AddPropertyValue of
        tableId: ProvenanceTableId *
        processId: ProvenanceProcessId *
        headerKind: ProvenancePropertyKind *
        headerName: string *
        inputIds: ProvenanceEntryId list *
        outputIds: ProvenanceEntryId list *
        assignedEntryIds: ProvenanceEntryId list *
        value: ProvenanceValue *
        unit: ProvenanceTerm option
    | AddProcessConnection of
        tableId: ProvenanceTableId *
        processId: ProvenanceProcessId *
        sourceEntryId: ProvenanceEntryId *
        targetEntryId: ProvenanceEntryId
    | AddTable of ProvenanceTable

type EditResult =
    Result<ProvenanceModel * ProvenanceTablePatch list, EditError>
```

Patch application remains outside the reusable component. It may use ARCtrl, another table model, or a test fixture.

## Non-Goals

- No direct ARCtrl types in the public Swate model.
- No graph visualization.
- No row index or column position in the normalized source model.
- No creation of new entries in previous tables.
- No automatic ontology lookup.
- No production ARC writeback implementation in the first model pass.
