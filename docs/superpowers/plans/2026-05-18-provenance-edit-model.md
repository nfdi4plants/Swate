# Provenance Edit Model Implementation Plan

**Goal:** Add a Swate-owned F#/Fable provenance edit model that can import process-like table data, power the grouped provenance UI, and return table/header based writeback patches.

**Architecture:** Implement the model as pure F# modules under `src/Components/src/ProvenanceGrouping`. The model has no React dependency and no ARCtrl types in its public signatures. Storybook remains the browser preview shell; the state and edit behavior live in F#.

**Reference:** The design follows ARCtrl `Process` semantics from `C:\Users\carol\source\repos\ARCtrl`: process parameters live on `Process.ParameterValues`, characteristics live on process inputs/outputs, and factors live on sample outputs.

---

## File Structure

- Create `src/Components/src/ProvenanceGrouping/Types.fs`
  - Public model types, IDs, property value occurrences, connections, and patch-independent primitives.
- Create `src/Components/src/ProvenanceGrouping/Import.fs`
  - Plain import DTOs and `fromImportedTables`.
  - No ARCtrl types in public signatures.
- Create `src/Components/src/ProvenanceGrouping/Grouping.fs`
  - Selectors and grouped display projections used by the component/story.
- Create `src/Components/src/ProvenanceGrouping/Edit.fs`
  - Edit commands, validation, and writeback patch output.
- Create `src/Components/src/ProvenanceGrouping/Fixtures.fs`
  - F# sample data for the mockup story and loaded-table context checks.
- Modify `src/Components/src/Swate.Components.fsproj`
  - Add the new `.fs` files in compile order before any component file that consumes them.
- Later modify the existing Storybook preview
  - Keep the visual Storybook shell as browser-facing preview code.
  - Source all sample data and edit behavior from the compiled F# modules.

Compile order in `Swate.Components.fsproj` should be:

```xml
<Compile Include="ProvenanceGrouping\Types.fs" />
<Compile Include="ProvenanceGrouping\Import.fs" />
<Compile Include="ProvenanceGrouping\Grouping.fs" />
<Compile Include="ProvenanceGrouping\Edit.fs" />
<Compile Include="ProvenanceGrouping\Fixtures.fs" />
```

---

## Task 1: Add F# Core Types

**Files:**
- Create `src/Components/src/ProvenanceGrouping/Types.fs`
- Modify `src/Components/src/Swate.Components.fsproj`

- [ ] Create `Types.fs` with module `Swate.Components.ProvenanceGrouping.Types`.
- [ ] Add ID aliases, table/entry/process/connection/property records, and the normalized `ProvenanceModel`.
- [ ] Use `Map<id, record>` for model collections so lookups are explicit and duplicate IDs are rejected by construction during import.
- [ ] Keep property values as occurrences, not as a single value per header.
- [ ] Add source tracking as table/header/process metadata, without row index or column position.

Initial type skeleton:

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

[<RequireQualifiedAccess>]
type ProvenanceTableRole =
    | LoadedTable
    | PreviousContext

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
        Role: ProvenanceTableRole
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
        LoadedTableId: ProvenanceTableId
        Tables: Map<ProvenanceTableId, ProvenanceTable>
        Entries: Map<ProvenanceEntryId, ProvenanceEntry>
        Processes: Map<ProvenanceProcessId, ProvenanceProcess>
        Connections: Map<ProvenanceConnectionId, ProvenanceConnection>
        Properties: Map<ProvenancePropertyValueId, ProvenancePropertyValue>
    }
```

- [ ] Run:

```powershell
dotnet build src/Components/src/Swate.Components.fsproj
```

Expected: the project compiles with the new type file.

---

## Task 2: Add Plain Import Builder

**Files:**
- Create `src/Components/src/ProvenanceGrouping/Import.fs`
- Modify `src/Components/src/Swate.Components.fsproj`

- [ ] Define `ImportedEntry`, `ImportedProperty`, `ImportedProcess`, `ImportedTable`, and `ImportResult`.
- [ ] Implement `fromImportedTables : loadedTableId: ProvenanceTableId -> tables: ImportedTable list -> ImportResult`.
- [ ] Require exactly one imported table with `Role = ProvenanceTableRole.LoadedTable` and make its ID match `loadedTableId`.
- [ ] Allow any number of imported tables with `Role = ProvenanceTableRole.PreviousContext`.
- [ ] Generate one item-level `ProvenanceConnection` for each imported process input/output pair.
- [ ] For process-scoped parameters, default `AssignedEntryIds` to all input and output IDs.
- [ ] For entry-scoped values, require explicit assigned entries and emit warnings when missing.
- [ ] Merge repeated entries by ID and accumulate their table IDs.
- [ ] Preserve every property value occurrence, including repeated values with the same category on the same entry.
- [ ] Preserve previous-context property values with their original source table/header/process metadata.

Important ARCtrl mapping notes for adapter authors:

- `Process.ParameterValues` maps to imported properties with `Kind = Parameter` and `AssignmentScope = Process`.
- Input/output `MaterialAttributeValue` maps to imported properties with `Kind = Characteristic` and `AssignmentScope = Entry`.
- Output `FactorValue` maps to imported properties with `Kind = Factor` and `AssignmentScope = Entry`.
- `Value.Ontology`, `Value.Int`, `Value.Float`, and `Value.Name` map to `ProvenanceValue.Term`, `Integer`, `Float`, and `Text`.
- `CompositeHeader.Parameter`, `Characteristic`, `Factor`, and `Component` map to `ProvenancePropertyKind`.

Validation after implementation:

```powershell
dotnet build src/Components/src/Swate.Components.fsproj
```

Expected: import module compiles and has no ARCtrl imports.

---

## Task 3: Add Grouping And Sorting Projection

**Files:**
- Create `src/Components/src/ProvenanceGrouping/Grouping.fs`
- Modify `src/Components/src/Swate.Components.fsproj`

- [ ] Define `ProvenanceSide`, `GroupingKey`, `DisplayMember`, `DisplayGroup`, and `DisplayConnection`.
- [ ] Implement selectors:
  - `propertiesForEntry`
  - `upstreamPropertiesForLoadedEntry`
  - `propertyValuesForEntry`
  - `groupingKeysForSide`
  - `displayGroups`
  - `displayConnections`
  - `sortDisplayGroups`
- [ ] Build visible input and output groups only from the loaded table.
- [ ] Include previous-context property values for a loaded-table entry when they belong to that entry or to upstream entries connected to it by previous-context processes.
- [ ] With no grouping keys, return one display group per loaded-table entry.
- [ ] When grouping by a key, group by actual property values only.
- [ ] Do not create a "missing" group for entries without the grouped property.
- [ ] Duplicate an entry into multiple display groups when it has multiple values for the grouped key.
- [ ] Sort members inside groups by a selected non-grouping parameter.
- [ ] When no grouping exists, sort the individual item groups by the selected parameter.
- [ ] Aggregate group-level connectors from actual `ProvenanceConnection` IDs only.
- [ ] On line expansion, return only the individual connections represented by that group line, never an all-to-all expansion.

Key behavior to verify manually in fixtures:

- Input A connects to Output A and Output B.
- Input B connects to Output B.
- Input C connects to Output C.
- If outputs are grouped by species, expanding the species group line shows only the original item connections.

Validation:

```powershell
dotnet build src/Components/src/Swate.Components.fsproj
```

Expected: grouping module compiles.

---

## Task 4: Add Edit Commands And Patches

**Files:**
- Create `src/Components/src/ProvenanceGrouping/Edit.fs`
- Modify `src/Components/src/Swate.Components.fsproj`

- [ ] Define `EditError`.
- [ ] Define `ProvenanceTablePatch`.
- [ ] Define edit command records for:
  - update existing property value
  - create property value in the loaded table
  - assign existing value to group members
  - connect groups
  - create loaded-table layer/process from selected entries
- [ ] Implement `updatePropertyValue`.
- [ ] Implement `createPropertyValueInLoadedTable`.
- [ ] Implement `assignPropertyValueToEntries`.
- [ ] Implement `connectGroups`.
- [ ] Implement `createLoadedTableLayerFromSelection`.

Patch shape:

```fsharp
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
type EditResult =
    Result<ProvenanceModel * ProvenanceTablePatch list, EditError>
```

Rules to implement:

- Existing value edits overwrite existing propagated values on connected outputs when the edited occurrence already exists there.
- Adding a new property header with an existing name and kind returns `DuplicateHeader`.
- Adding a new value under an existing header is valid.
- New property values, headers, connections, and layer/process additions are allowed only in the loaded table.
- Previous-context property values can be edited only when the selected existing occurrence has compatible process/table context.
- Previous-context edits must never create property values, headers, rows, inputs, outputs, or process connections.
- Group-to-group connection commands either create the required item-level connections or return an explicit error.
- Creating a new loaded-table layer/process uses every selected entry, including mixed input and output selections, as the new layer's inputs.

Validation:

```powershell
dotnet build src/Components/src/Swate.Components.fsproj
```

Expected: edit module compiles.

---

## Task 5: Add F# Fixtures For Preview Data

**Files:**
- Create `src/Components/src/ProvenanceGrouping/Fixtures.fs`
- Modify `src/Components/src/Swate.Components.fsproj`

- [ ] Move the current mock scenario into F# fixtures.
- [ ] Include repeated property values for the same entry.
- [ ] Include values present only on outputs that can be assigned to inputs.
- [ ] Include fan-out and fan-in connections.
- [ ] Include the known connector case:
  - Input A -> Output A
  - Input A -> Output B
  - Input B -> Output B
  - Input C -> Output C
- [ ] Include a previous-context table whose output properties are visible for loaded-table input grouping.
- [ ] Include a mixed selection fixture for loaded-table layer creation.

Validation:

```powershell
dotnet build src/Components/src/Swate.Components.fsproj
```

Expected: fixtures compile and can be imported by the preview layer.

---

## Task 6: Wire The Browser Preview To F# State

**Files:**
- Modify the existing `src/Components/src/ProvenanceGrouping` Storybook preview files.

- [ ] Keep the browser preview broad/resizable.
- [ ] Replace mock-only data logic with calls into F# fixtures and grouping/edit modules.
- [ ] Keep direct element expansion for parameter values and connection lines.
- [ ] Display parameter value controls as draggable value controls with connector lines to groups.
- [ ] Remove UI actions that conflict with the model:
  - no "connect selected" button
  - no partial-connection action
  - no parameter add button on individual input/output groups
- [ ] Ensure group connection line expansion calls the F# selector that returns actual item-level connections.
- [ ] Ensure new layer creation uses the full mixed selection.

Validation:

```powershell
npm run storybook
```

Manual preview checks:

- The provenance story renders full width or resizable.
- No grouping shows individual entry items.
- Grouping by a property groups by values and ignores non-grouping properties.
- Missing values do not create a missing-value group.
- Entries with multiple values appear in multiple groups.
- Previous-context property values can be used for grouping loaded-table entries.
- Adding a new property value writes to the loaded table only.
- Sorting works inside groups and on individual ungrouped items.
- Dragging a property value to a group assigns the value to all members.
- Clicking a group connection line expands both groups and shows only actual item-level connections.
- New loaded-table layer uses every selected entry, including mixed input/output selections.

---

## Task 7: Final Verification

- [ ] Run:

```powershell
dotnet build src/Components/src/Swate.Components.fsproj
```

- [ ] Run the component package build if the preview was rewired:

```powershell
npm run build
```

- [ ] Run:

```powershell
git diff --check
```

- [ ] Manually inspect that the new F# files have the correct module names and are listed in the `.fsproj` compile order.

Expected outcome:

- The normalized model and edit commands are implemented in F#.
- The model is independent of ARCtrl public types.
- The ARCtrl process semantics are documented and preserved through the import DTO mapping.
- The browser preview is still viewable through Storybook, but the data model is no longer mock-only browser state.
