# Provenance Edit Model Design

## Purpose

Build a Swate-owned provenance edit model that can power the grouped provenance UI and later write changes back to ARC tables. The model must be independent from ARCtrl runtime types. ARCtrl's `ARC.Process list` is a reference for shape and semantics, but the reusable component and model helpers should work with plain Swate data.

The model must support conversion from several inputs:

- Loaded assay tables.
- Loaded study tables.
- Loaded run tables.
- Mock/story data.
- Future plain DTOs that can describe table processes, entries, connections, and property values.

The first real viewer session always starts from a loaded assay, study, or run table. Editing must support that loaded table and any downstream tables created during the viewer workflow.

## ARCtrl Reference

ARCtrl `Process` rows provide the reference semantics:

- A process has a name. For tables, this normally corresponds to the table name, with row numbering for multi-row tables.
- A process has inputs and outputs.
- A process has process parameter values.
- Characteristics belong to material-like input or output entries.
- Factors belong to output sample-like entries.
- Property values have category, value, and unit.

The Swate model must not directly expose or depend on `ARCtrl.Process`, `ProcessInput`, `ProcessOutput`, `ProcessParameterValue`, `MaterialAttributeValue`, or `FactorValue`. Adapters may read those types outside the reusable component boundary and then create plain Swate model objects.

## Core Approach

Use a normalized graph plus occurrence-level property values.

The model should not be a table-row snapshot. It should also not copy ARCtrl's object model. Instead it should make these concepts explicit:

- Tables are editable scopes.
- Entries are named provenance objects, such as sources, samples, data, and materials.
- Processes connect entries inside a table.
- Connections are explicit input-to-output links scoped by a process.
- Property values are individual occurrences with table, process, input, output, owner, and writeback metadata.

This model keeps grouping efficient while preserving enough provenance to write back edits later.

## Types

```ts
export type ProvenanceTableKind = "study" | "assay" | "run" | "generated";

export type ProvenanceEntryKind = "source" | "sample" | "data" | "material" | "unknown";

export type ProvenancePropertyKind = "characteristic" | "factor" | "parameter" | "component";

export type ProvenanceAssignmentScope = "entry" | "process";

export type ProvenanceValueKind = "text" | "integer" | "float" | "term";
```

```ts
export type ProvenanceTerm = {
  name: string;
  termSource?: string;
  termAccession?: string;
};

export type ProvenanceValue = {
  text: string;
  kind: ProvenanceValueKind;
  term?: ProvenanceTerm;
};
```

```ts
export type ProvenanceTable = {
  id: string;
  name: string;
  kind: ProvenanceTableKind;
  origin: "loaded" | "created";
  previousTableId?: string;
};
```

```ts
export type ProvenanceEntry = {
  id: string;
  name: string;
  kind: ProvenanceEntryKind;
  tableIds: string[];
};
```

```ts
export type ProvenanceProcess = {
  id: string;
  tableId: string;
  tableName: string;
  inputIds: string[];
  outputIds: string[];
  origin: "loaded" | "created";
};
```

```ts
export type ProvenanceConnection = {
  id: string;
  tableId: string;
  processId: string;
  sourceEntryId: string;
  targetEntryId: string;
  origin: "loaded" | "created";
};
```

Property value source tracking is table and header based. It intentionally does not store table row or column position.

```ts
export type ProvenancePropertySource = {
  tableId: string;
  tableName: string;
  headerName: string;
  headerKind: ProvenancePropertyKind;
  processId?: string;
  origin: "loaded" | "created";
};
```

Rows are implied by the process plus input/output IDs. Existing columns can be found by `headerName` and `headerKind`; column position is not part of the model. If an adapter later cannot disambiguate several matching cells for the same header and old value, it must return an explicit writeback error rather than guessing.

```ts
export type ProvenancePropertyValue = {
  id: string;
  kind: ProvenancePropertyKind;
  assignmentScope: ProvenanceAssignmentScope;
  tableId: string;
  tableName: string;
  processId?: string;
  inputIds: string[];
  outputIds: string[];
  assignedEntryIds: string[];
  category: ProvenanceTerm;
  value: ProvenanceValue;
  unit?: ProvenanceTerm;
  source: ProvenancePropertySource;
};
```

```ts
export type ProvenanceModel = {
  tables: ProvenanceTable[];
  entries: ProvenanceEntry[];
  processes: ProvenanceProcess[];
  connections: ProvenanceConnection[];
  properties: ProvenancePropertyValue[];
};
```

## Property Ownership

Every property value needs process context and grouping ownership.

For a process parameter:

- `assignmentScope` is `"process"`.
- `processId` points to the process.
- `inputIds` and `outputIds` are copied from the process.
- `assignedEntryIds` contains the entries that should expose the value for grouping, usually the process inputs and outputs.

For a characteristic:

- `assignmentScope` is `"entry"`.
- `assignedEntryIds` contains the input or output entry that owns the characteristic.
- `inputIds` and `outputIds` still describe the process context where this value was observed.

For a factor:

- `assignmentScope` is `"entry"`.
- `assignedEntryIds` contains the output entry that owns the factor.
- `inputIds` and `outputIds` describe the process context.

Components may be imported for completeness because ARCtrl exposes them in process conversion, but the first UI iteration does not need component editing.

## Editing Rules

### Existing Values

Editing an existing property value targets `property.id`.

The edit may change:

- `category`
- `value`
- `unit`

The change set must retain:

- `source.tableId`
- `source.headerName`
- `source.headerKind`
- `processId`
- `inputIds`
- `outputIds`
- `assignedEntryIds`

When writing back, the adapter finds the existing row from the process and connected input/output IDs, and finds the column by header name and property kind. Column position is irrelevant.

### New Values In Current Scope

Creating completely new property values is allowed in the current edit scope. Current scope means:

- The loaded table that opened the viewer.
- A table created during the current viewer workflow.

The command creates new `ProvenancePropertyValue` occurrences with `source.origin = "created"`. If the header does not exist, the writeback patch may request a new header. If the header exists, the writeback patch may request new cells under that header.

Creating a new property header with a duplicate name and kind should return an error. Adding a new value occurrence under an existing header is allowed.

### New Values In Previous Tables

Adding new property values to previous tables is allowed only for already existing entries with relevant connections in those previous tables.

The model must reject the action when:

- The selected entry is not present in the target previous table.
- There is no process in the target previous table connecting the relevant input/output entries.
- The action would require inventing new inputs or outputs in a previous table.

The error should explain that previous-table edits require existing connected entries.

### New Tables

New downstream tables created after the loaded table are editable scopes. They may contain created processes, created connections, and created property values. New property additions in those tables are allowed.

## Grouping And Display

The grouped UI should derive its current `ProvenanceItem`-like view from the normalized model.

For a displayed layer/table:

- Entry cards come from `entries`.
- Grouping values come from `properties` where `assignedEntryIds` includes the entry ID.
- Connection-derived values can be derived through `connections` and process context.
- Repeated values are preserved because `properties` is a list of occurrences.

The UI adapter should expose table sides as display layers. For example, a loaded assay table can produce `assay-table:inputs` and `assay-table:outputs` layers while both still point back to the same editable `ProvenanceTable`.

No grouping helper should collapse property values into a map keyed only by category name.

## Adapter Boundary

Adapters convert external data into `ProvenanceModel` and convert accepted edit commands into writeback patches.

```ts
export type ProvenanceImportResult = {
  model: ProvenanceModel;
  warnings: string[];
};

export type ProvenanceWritePatch = {
  tablePatches: ProvenanceTablePatch[];
  errors: string[];
};
```

The reusable component should not know whether the adapter came from ARCtrl, a table DTO, or story data.

For ARCtrl-based callers, conversion should happen outside the component:

1. Read assay, study, or run tables with ARCtrl.
2. Convert their Process list or process-like DTOs into plain Swate import objects.
3. Build `ProvenanceModel`.
4. Let the UI edit the Swate model.
5. Convert edit commands into table patches.
6. Apply patches with the caller's table implementation.

## Writeback Patch Shape

Writeback should be patch based, not direct mutation.

```ts
export type ProvenanceTablePatch =
  | {
      kind: "updatePropertyValue";
      tableId: string;
      processId: string;
      headerName: string;
      headerKind: ProvenancePropertyKind;
      inputIds: string[];
      outputIds: string[];
      oldValue: ProvenanceValue;
      newValue: ProvenanceValue;
      unit?: ProvenanceTerm;
    }
  | {
      kind: "addPropertyValue";
      tableId: string;
      processId: string;
      headerName: string;
      headerKind: ProvenancePropertyKind;
      inputIds: string[];
      outputIds: string[];
      assignedEntryIds: string[];
      value: ProvenanceValue;
      unit?: ProvenanceTerm;
    }
  | {
      kind: "addTable";
      table: ProvenanceTable;
    }
  | {
      kind: "addProcessConnection";
      tableId: string;
      processId: string;
      sourceEntryId: string;
      targetEntryId: string;
    };
```

Patch application is outside the reusable component. It may use ARCtrl, table DTOs, or another backing model.

## Non-Goals

- No direct ARCtrl dependency inside `src/Components/src/ProvenanceGrouping`.
- No table row or column position in the normalized edit model.
- No creation of new entries in previous tables.
- No automatic ontology lookup.
- No production writeback implementation in the first model pass.
- No graph visualization.
