# Provenance Edit Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Swate-owned normalized provenance edit model that can import process-like data, power the grouped provenance UI, and produce table/header-based writeback patches.

**Architecture:** Keep the model independent from ARCtrl runtime types. Implement pure TypeScript model types and helpers under `src/Components/src/ProvenanceGrouping`, then adapt the existing mockup to derive its display props from the normalized model. Use Storybook play tests as the available component-package verification path.

**Tech Stack:** TypeScript, React Storybook, Vitest browser project via Storybook, existing Swate Components package scripts.

---

## File Structure

- Create `src/Components/src/ProvenanceGrouping/ProvenanceModel.ts`
  - Owns normalized model types, import DTO types, selectors, validation helpers, edit command functions, and patch types.
  - Has no React imports and no ARCtrl imports.
- Create `src/Components/src/ProvenanceGrouping/ProvenanceModel.stories.tsx`
  - Test-only Storybook story that renders a small status element and uses `play` functions to verify pure model behavior.
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`
  - Later bridge the existing interactive mockup sample data through `ProvenanceModel` instead of maintaining a separate mock-only shape.
- Modify `src/Components/src/index.js`
  - Export the model helpers only after they are stable enough for package users. This can be delayed until Task 5.
- Modify `docs/superpowers/specs/2026-05-18-provenance-edit-model-design.md`
  - Keep it in sync if implementation reveals a naming issue.

## Task 1: Add Core Model Types And Import Builder

**Files:**
- Create: `src/Components/src/ProvenanceGrouping/ProvenanceModel.ts`
- Create: `src/Components/src/ProvenanceGrouping/ProvenanceModel.stories.tsx`

- [ ] **Step 1: Add a failing Storybook model invariant story**

Create `src/Components/src/ProvenanceGrouping/ProvenanceModel.stories.tsx`:

```tsx
import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect } from "storybook/test";
import { createProvenanceModelFromTables, type ProvenanceImportTable } from "./ProvenanceModel";

const sourceTables: ProvenanceImportTable[] = [
  {
    id: "assay-table",
    name: "Assay Table",
    kind: "assay",
    origin: "loaded",
    processes: [
      {
        id: "assay-table/process/0",
        inputIds: ["input-a"],
        outputIds: ["output-a"],
        inputs: [{ id: "input-a", name: "Input A", kind: "sample" }],
        outputs: [{ id: "output-a", name: "Output A", kind: "data" }],
        properties: [
          {
            id: "assay-table/process/0/parameter/temperature",
            kind: "parameter",
            assignmentScope: "process",
            headerName: "Temperature",
            category: { name: "Temperature" },
            value: { text: "12 C", kind: "text" },
          },
          {
            id: "assay-table/process/0/factor/analysis",
            kind: "factor",
            assignmentScope: "entry",
            assignedEntryIds: ["output-a"],
            headerName: "Analysis",
            category: { name: "Analysis" },
            value: { text: "Mass Spectrometry", kind: "text" },
          },
        ],
      },
    ],
  },
];

const meta = {
  title: "Components/ProvenanceModel",
  tags: ["autodocs"],
  render: () => <div data-testid="ProvenanceModel-root">Provenance model invariants</div>,
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const ImportModel: Story = {
  name: "Import model",
  play: async () => {
    const result = createProvenanceModelFromTables(sourceTables);
    expect(result.warnings).toEqual([]);
    expect(result.model.tables).toHaveLength(1);
    expect(result.model.entries.map((entry) => entry.id).sort()).toEqual(["input-a", "output-a"]);
    expect(result.model.processes[0].tableName).toBe("Assay Table");
    expect(result.model.connections).toEqual([
      {
        id: "assay-table/process/0/input-a/output-a",
        tableId: "assay-table",
        processId: "assay-table/process/0",
        sourceEntryId: "input-a",
        targetEntryId: "output-a",
        origin: "loaded",
      },
    ]);
    expect(result.model.properties).toHaveLength(2);
    expect(result.model.properties[0]).toMatchObject({
      tableId: "assay-table",
      tableName: "Assay Table",
      processId: "assay-table/process/0",
      inputIds: ["input-a"],
      outputIds: ["output-a"],
      assignedEntryIds: ["input-a", "output-a"],
      source: {
        tableId: "assay-table",
        tableName: "Assay Table",
        headerName: "Temperature",
        headerKind: "parameter",
        processId: "assay-table/process/0",
        origin: "loaded",
      },
    });
  },
};
```

- [ ] **Step 2: Run the failing story test**

Run:

```powershell
npm run test:run -- ProvenanceModel
```

Expected: fail because `./ProvenanceModel` does not exist.

- [ ] **Step 3: Add model types and import builder**

Create `src/Components/src/ProvenanceGrouping/ProvenanceModel.ts`:

```ts
export type ProvenanceTableKind = "study" | "assay" | "run" | "generated";
export type ProvenanceEntryKind = "source" | "sample" | "data" | "material" | "unknown";
export type ProvenancePropertyKind = "characteristic" | "factor" | "parameter" | "component";
export type ProvenanceAssignmentScope = "entry" | "process";
export type ProvenanceValueKind = "text" | "integer" | "float" | "term";

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

export type ProvenanceTable = {
  id: string;
  name: string;
  kind: ProvenanceTableKind;
  origin: "loaded" | "created";
  previousTableId?: string;
};

export type ProvenanceEntry = {
  id: string;
  name: string;
  kind: ProvenanceEntryKind;
  tableIds: string[];
};

export type ProvenanceProcess = {
  id: string;
  tableId: string;
  tableName: string;
  inputIds: string[];
  outputIds: string[];
  origin: "loaded" | "created";
};

export type ProvenanceConnection = {
  id: string;
  tableId: string;
  processId: string;
  sourceEntryId: string;
  targetEntryId: string;
  origin: "loaded" | "created";
};

export type ProvenancePropertySource = {
  tableId: string;
  tableName: string;
  headerName: string;
  headerKind: ProvenancePropertyKind;
  processId?: string;
  origin: "loaded" | "created";
};

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

export type ProvenanceModel = {
  tables: ProvenanceTable[];
  entries: ProvenanceEntry[];
  processes: ProvenanceProcess[];
  connections: ProvenanceConnection[];
  properties: ProvenancePropertyValue[];
};

export type ProvenanceImportEntry = {
  id: string;
  name: string;
  kind: ProvenanceEntryKind;
};

export type ProvenanceImportProperty = {
  id: string;
  kind: ProvenancePropertyKind;
  assignmentScope: ProvenanceAssignmentScope;
  assignedEntryIds?: string[];
  headerName: string;
  category: ProvenanceTerm;
  value: ProvenanceValue;
  unit?: ProvenanceTerm;
};

export type ProvenanceImportProcess = {
  id: string;
  inputIds: string[];
  outputIds: string[];
  inputs: ProvenanceImportEntry[];
  outputs: ProvenanceImportEntry[];
  properties: ProvenanceImportProperty[];
};

export type ProvenanceImportTable = {
  id: string;
  name: string;
  kind: ProvenanceTableKind;
  origin: "loaded" | "created";
  previousTableId?: string;
  processes: ProvenanceImportProcess[];
};

export type ProvenanceImportResult = {
  model: ProvenanceModel;
  warnings: string[];
};

const unique = <T,>(values: T[]): T[] => Array.from(new Set(values));

export function createProvenanceModelFromTables(tables: ProvenanceImportTable[]): ProvenanceImportResult {
  const entriesById = new Map<string, ProvenanceEntry>();
  const processes: ProvenanceProcess[] = [];
  const connections: ProvenanceConnection[] = [];
  const properties: ProvenancePropertyValue[] = [];
  const warnings: string[] = [];

  tables.forEach((table) => {
    table.processes.forEach((process) => {
      [...process.inputs, ...process.outputs].forEach((entry) => {
        const existing = entriesById.get(entry.id);
        entriesById.set(entry.id, {
          id: entry.id,
          name: existing?.name ?? entry.name,
          kind: existing?.kind ?? entry.kind,
          tableIds: unique([...(existing?.tableIds ?? []), table.id]),
        });
      });

      processes.push({
        id: process.id,
        tableId: table.id,
        tableName: table.name,
        inputIds: process.inputIds,
        outputIds: process.outputIds,
        origin: table.origin,
      });

      process.inputIds.forEach((sourceEntryId) => {
        process.outputIds.forEach((targetEntryId) => {
          connections.push({
            id: `${process.id}/${sourceEntryId}/${targetEntryId}`,
            tableId: table.id,
            processId: process.id,
            sourceEntryId,
            targetEntryId,
            origin: table.origin,
          });
        });
      });

      process.properties.forEach((property) => {
        const assignedEntryIds =
          property.assignedEntryIds ??
          (property.assignmentScope === "process" ? unique([...process.inputIds, ...process.outputIds]) : []);

        if (property.assignmentScope === "entry" && assignedEntryIds.length === 0) {
          warnings.push(`Property "${property.id}" has entry assignment scope but no assigned entries.`);
        }

        properties.push({
          id: property.id,
          kind: property.kind,
          assignmentScope: property.assignmentScope,
          tableId: table.id,
          tableName: table.name,
          processId: process.id,
          inputIds: process.inputIds,
          outputIds: process.outputIds,
          assignedEntryIds,
          category: property.category,
          value: property.value,
          unit: property.unit,
          source: {
            tableId: table.id,
            tableName: table.name,
            headerName: property.headerName,
            headerKind: property.kind,
            processId: process.id,
            origin: table.origin,
          },
        });
      });
    });
  });

  return {
    model: {
      tables: tables.map(({ processes: _processes, ...table }) => table),
      entries: Array.from(entriesById.values()),
      processes,
      connections,
      properties,
    },
    warnings,
  };
}
```

- [ ] **Step 4: Run the story test**

Run:

```powershell
npm run test:run -- ProvenanceModel
```

Expected: pass with `1 passed`.

- [ ] **Step 5: Commit**

```powershell
git add src/Components/src/ProvenanceGrouping/ProvenanceModel.ts src/Components/src/ProvenanceGrouping/ProvenanceModel.stories.tsx
git commit -m "feat: add provenance edit model"
```

## Task 2: Add Property Selectors For Grouping

**Files:**
- Modify: `src/Components/src/ProvenanceGrouping/ProvenanceModel.ts`
- Modify: `src/Components/src/ProvenanceGrouping/ProvenanceModel.stories.tsx`

- [ ] **Step 1: Add failing selector coverage**

Append this story to `ProvenanceModel.stories.tsx`:

```tsx
import { getPropertiesForEntry, getPropertyValuesForEntry } from "./ProvenanceModel";

export const EntryPropertySelectors: Story = {
  name: "Entry property selectors",
  play: async () => {
    const result = createProvenanceModelFromTables(sourceTables);

    expect(getPropertiesForEntry(result.model, "input-a").map((property) => property.category.name)).toEqual([
      "Temperature",
    ]);
    expect(getPropertiesForEntry(result.model, "output-a").map((property) => property.category.name)).toEqual([
      "Temperature",
      "Analysis",
    ]);
    expect(getPropertyValuesForEntry(result.model, "output-a", "Analysis")).toEqual(["Mass Spectrometry"]);
  },
};
```

Expected initial failure: named exports do not exist.

- [ ] **Step 2: Implement selectors**

Append to `ProvenanceModel.ts`:

```ts
const normalizeName = (value: string): string => value.trim().toLowerCase();

export function getPropertiesForEntry(model: ProvenanceModel, entryId: string): ProvenancePropertyValue[] {
  return model.properties.filter((property) => property.assignedEntryIds.includes(entryId));
}

export function getPropertyValuesForEntry(model: ProvenanceModel, entryId: string, categoryName: string): string[] {
  const normalizedCategory = normalizeName(categoryName);
  return getPropertiesForEntry(model, entryId)
    .filter((property) => normalizeName(property.category.name) === normalizedCategory)
    .map((property) => property.value.text);
}

export function getPropertyCategoriesForTable(model: ProvenanceModel, tableId: string): string[] {
  return unique(
    model.properties
      .filter((property) => property.tableId === tableId)
      .map((property) => property.category.name)
      .filter((name) => name.trim().length > 0),
  ).sort((left, right) => left.localeCompare(right));
}
```

- [ ] **Step 3: Run selector tests**

Run:

```powershell
npm run test:run -- ProvenanceModel
```

Expected: pass with `2 passed`.

- [ ] **Step 4: Commit**

```powershell
git add src/Components/src/ProvenanceGrouping/ProvenanceModel.ts src/Components/src/ProvenanceGrouping/ProvenanceModel.stories.tsx
git commit -m "feat: add provenance property selectors"
```

## Task 3: Add Edit Commands And Patch Output

**Files:**
- Modify: `src/Components/src/ProvenanceGrouping/ProvenanceModel.ts`
- Modify: `src/Components/src/ProvenanceGrouping/ProvenanceModel.stories.tsx`

- [ ] **Step 1: Add failing edit command stories**

Append this import and stories to `ProvenanceModel.stories.tsx`:

```tsx
import {
  createPropertyValueInScope,
  updatePropertyValue,
  validatePreviousTablePropertyTarget,
} from "./ProvenanceModel";

export const UpdateExistingProperty: Story = {
  name: "Update existing property",
  play: async () => {
    const result = createProvenanceModelFromTables(sourceTables);
    const update = updatePropertyValue(result.model, {
      propertyId: "assay-table/process/0/parameter/temperature",
      value: { text: "16 C", kind: "text" },
    });

    expect(update.ok).toBe(true);
    if (!update.ok) return;
    expect(update.model.properties.find((property) => property.id === "assay-table/process/0/parameter/temperature")?.value.text).toBe("16 C");
    expect(update.patches).toEqual([
      {
        kind: "updatePropertyValue",
        tableId: "assay-table",
        processId: "assay-table/process/0",
        headerName: "Temperature",
        headerKind: "parameter",
        inputIds: ["input-a"],
        outputIds: ["output-a"],
        oldValue: { text: "12 C", kind: "text" },
        newValue: { text: "16 C", kind: "text" },
        unit: undefined,
      },
    ]);
  },
};

export const CreateCurrentScopeProperty: Story = {
  name: "Create current scope property",
  play: async () => {
    const result = createProvenanceModelFromTables(sourceTables);
    const create = createPropertyValueInScope(result.model, {
      tableId: "assay-table",
      processId: "assay-table/process/0",
      kind: "parameter",
      assignmentScope: "process",
      headerName: "Instrument",
      category: { name: "Instrument" },
      value: { text: "Orbitrap", kind: "text" },
    });

    expect(create.ok).toBe(true);
    if (!create.ok) return;
    expect(create.model.properties.some((property) => property.category.name === "Instrument")).toBe(true);
    expect(create.patches[0]).toMatchObject({
      kind: "addPropertyValue",
      tableId: "assay-table",
      processId: "assay-table/process/0",
      headerName: "Instrument",
      headerKind: "parameter",
      inputIds: ["input-a"],
      outputIds: ["output-a"],
      assignedEntryIds: ["input-a", "output-a"],
      value: { text: "Orbitrap", kind: "text" },
    });
  },
};

export const RejectPreviousTableWithoutConnection: Story = {
  name: "Reject previous table without connection",
  play: async () => {
    const result = createProvenanceModelFromTables(sourceTables);
    expect(validatePreviousTablePropertyTarget(result.model, "missing-table", ["input-a"])).toEqual({
      ok: false,
      error: "Previous-table edits require existing connected entries in the target table.",
    });
  },
};
```

- [ ] **Step 2: Implement edit command result and patches**

Append to `ProvenanceModel.ts`:

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
    };

export type ProvenanceEditResult =
  | { ok: true; model: ProvenanceModel; patches: ProvenanceTablePatch[] }
  | { ok: false; error: string };

export type UpdatePropertyValueCommand = {
  propertyId: string;
  category?: ProvenanceTerm;
  value: ProvenanceValue;
  unit?: ProvenanceTerm;
};

export function updatePropertyValue(model: ProvenanceModel, command: UpdatePropertyValueCommand): ProvenanceEditResult {
  const property = model.properties.find((candidate) => candidate.id === command.propertyId);

  if (!property) {
    return { ok: false, error: "The selected property value no longer exists." };
  }

  if (!property.processId) {
    return { ok: false, error: "The selected property value is missing process context." };
  }

  const nextProperty: ProvenancePropertyValue = {
    ...property,
    category: command.category ?? property.category,
    value: command.value,
    unit: command.unit,
  };

  return {
    ok: true,
    model: {
      ...model,
      properties: model.properties.map((candidate) => (candidate.id === property.id ? nextProperty : candidate)),
    },
    patches: [
      {
        kind: "updatePropertyValue",
        tableId: property.tableId,
        processId: property.processId,
        headerName: property.source.headerName,
        headerKind: property.source.headerKind,
        inputIds: property.inputIds,
        outputIds: property.outputIds,
        oldValue: property.value,
        newValue: command.value,
        unit: command.unit,
      },
    ],
  };
}

export type CreatePropertyValueCommand = {
  tableId: string;
  processId: string;
  kind: ProvenancePropertyKind;
  assignmentScope: ProvenanceAssignmentScope;
  assignedEntryIds?: string[];
  headerName: string;
  category: ProvenanceTerm;
  value: ProvenanceValue;
  unit?: ProvenanceTerm;
};

export function createPropertyValueInScope(
  model: ProvenanceModel,
  command: CreatePropertyValueCommand,
): ProvenanceEditResult {
  const table = model.tables.find((candidate) => candidate.id === command.tableId);
  const process = model.processes.find((candidate) => candidate.id === command.processId && candidate.tableId === command.tableId);

  if (!table || !process) {
    return { ok: false, error: "The selected edit scope no longer exists." };
  }

  const assignedEntryIds =
    command.assignedEntryIds ??
    (command.assignmentScope === "process" ? unique([...process.inputIds, ...process.outputIds]) : []);

  if (command.assignmentScope === "entry" && assignedEntryIds.length === 0) {
    return { ok: false, error: "Entry-scoped property values need at least one assigned entry." };
  }

  const propertyId = `${command.processId}/${command.kind}/${command.headerName}/${model.properties.length + 1}`;
  const property: ProvenancePropertyValue = {
    id: propertyId,
    kind: command.kind,
    assignmentScope: command.assignmentScope,
    tableId: table.id,
    tableName: table.name,
    processId: process.id,
    inputIds: process.inputIds,
    outputIds: process.outputIds,
    assignedEntryIds,
    category: command.category,
    value: command.value,
    unit: command.unit,
    source: {
      tableId: table.id,
      tableName: table.name,
      headerName: command.headerName,
      headerKind: command.kind,
      processId: process.id,
      origin: "created",
    },
  };

  return {
    ok: true,
    model: { ...model, properties: [...model.properties, property] },
    patches: [
      {
        kind: "addPropertyValue",
        tableId: table.id,
        processId: process.id,
        headerName: command.headerName,
        headerKind: command.kind,
        inputIds: process.inputIds,
        outputIds: process.outputIds,
        assignedEntryIds,
        value: command.value,
        unit: command.unit,
      },
    ],
  };
}

export type ValidationResult = { ok: true } | { ok: false; error: string };

export function validatePreviousTablePropertyTarget(
  model: ProvenanceModel,
  tableId: string,
  selectedEntryIds: string[],
): ValidationResult {
  const table = model.tables.find((candidate) => candidate.id === tableId);
  if (!table) {
    return {
      ok: false,
      error: "Previous-table edits require existing connected entries in the target table.",
    };
  }

  const connectedEntryIds = new Set(
    model.connections
      .filter((connection) => connection.tableId === tableId)
      .flatMap((connection) => [connection.sourceEntryId, connection.targetEntryId]),
  );
  const allSelectedEntriesConnected = selectedEntryIds.every((entryId) => connectedEntryIds.has(entryId));

  return allSelectedEntriesConnected
    ? { ok: true }
    : {
        ok: false,
        error: "Previous-table edits require existing connected entries in the target table.",
      };
}
```

- [ ] **Step 3: Run edit command tests**

Run:

```powershell
npm run test:run -- ProvenanceModel
```

Expected: pass with `5 passed`.

- [ ] **Step 4: Commit**

```powershell
git add src/Components/src/ProvenanceGrouping/ProvenanceModel.ts src/Components/src/ProvenanceGrouping/ProvenanceModel.stories.tsx
git commit -m "feat: add provenance edit commands"
```

## Task 4: Bridge Normalized Model To Existing Grouping Props

**Files:**
- Modify: `src/Components/src/ProvenanceGrouping/ProvenanceModel.ts`
- Modify: `src/Components/src/ProvenanceGrouping/ProvenanceModel.stories.tsx`
- Modify: `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

- [ ] **Step 1: Add failing bridge story**

Append this import and story to `ProvenanceModel.stories.tsx`:

```tsx
import { toGroupingItems, toGroupingConnections } from "./ProvenanceModel";

export const GroupingBridge: Story = {
  name: "Grouping bridge",
  play: async () => {
    const result = createProvenanceModelFromTables(sourceTables);
    expect(toGroupingItems(result.model, "assay-table", "inputs")).toEqual([
      {
        id: "input-a",
        name: "Input A",
        layerId: "assay-table:inputs",
        parameters: [{ key: "Temperature", value: "12 C" }],
      },
    ]);
    expect(toGroupingItems(result.model, "assay-table", "outputs")).toEqual([
      {
        id: "output-a",
        name: "Output A",
        layerId: "assay-table:outputs",
        parameters: [
          { key: "Temperature", value: "12 C" },
          { key: "Analysis", value: "Mass Spectrometry" },
        ],
      },
    ]);
    expect(toGroupingConnections(result.model)).toEqual([{ sourceId: "input-a", targetId: "output-a" }]);
  },
};
```

- [ ] **Step 2: Implement bridge functions**

Append to `ProvenanceModel.ts`:

```ts
export type ProvenanceGroupingParameter = {
  key: string;
  value: string;
};

export type ProvenanceGroupingItem = {
  id: string;
  name: string;
  layerId: string;
  parameters: ProvenanceGroupingParameter[];
};

export type ProvenanceGroupingConnection = {
  sourceId: string;
  targetId: string;
};

export type ProvenanceGroupingTableSide = "inputs" | "outputs";

export function groupingLayerId(tableId: string, side: ProvenanceGroupingTableSide): string {
  return `${tableId}:${side}`;
}

export function toGroupingItems(
  model: ProvenanceModel,
  tableId: string,
  side: ProvenanceGroupingTableSide,
): ProvenanceGroupingItem[] {
  const tableProcesses = model.processes.filter((process) => process.tableId === tableId);
  const roleEntryIds = new Set(
    tableProcesses.flatMap((process) => (side === "inputs" ? process.inputIds : process.outputIds)),
  );

  return model.entries
    .filter((entry) => roleEntryIds.has(entry.id))
    .map((entry) => ({
      id: entry.id,
      name: entry.name,
      layerId: groupingLayerId(tableId, side),
      parameters: getPropertiesForEntry(model, entry.id)
        .filter((property) => property.tableId === tableId)
        .map((property) => ({ key: property.category.name, value: property.value.text })),
    }));
}

export function toGroupingConnections(model: ProvenanceModel): ProvenanceGroupingConnection[] {
  return model.connections.map((connection) => ({
    sourceId: connection.sourceEntryId,
    targetId: connection.targetEntryId,
  }));
}
```

- [ ] **Step 3: Run bridge tests**

Run:

```powershell
npm run test:run -- ProvenanceModel
```

Expected: pass with `6 passed`.

- [ ] **Step 4: Prepare the interactive story to use the bridge**

Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx` by importing the bridge types and functions near the existing imports:

```ts
import {
  createProvenanceModelFromTables,
  toGroupingConnections,
  toGroupingItems,
  type ProvenanceImportTable,
} from "./ProvenanceModel";
```

Do not replace every mock mutation in this task. Add a small internal conversion helper next to `initialModel`:

```ts
function initialNormalizedModel() {
  const source: ProvenanceImportTable[] = [
    {
      id: "inputs-outputs",
      name: "Inputs to Outputs",
      kind: "assay",
      origin: "loaded",
      processes: initialConnections.map((connection, index) => {
        const input = initialItems.find((item) => item.id === connection.sourceId);
        const output = initialItems.find((item) => item.id === connection.targetId);
        if (!input || !output) {
          throw new Error(`Missing fixture connection entry ${connection.sourceId} -> ${connection.targetId}`);
        }
        return {
          id: `inputs-outputs/process/${index}`,
          inputIds: [input.id],
          outputIds: [output.id],
          inputs: [{ id: input.id, name: input.name, kind: "sample" }],
          outputs: [{ id: output.id, name: output.name, kind: "data" }],
          properties: [
            ...input.parameters.map((parameter, parameterIndex) => ({
              id: `inputs-outputs/process/${index}/input/${input.id}/${parameter.key}/${parameterIndex}`,
              kind: "characteristic" as const,
              assignmentScope: "entry" as const,
              assignedEntryIds: [input.id],
              headerName: parameter.key,
              category: { name: parameter.key },
              value: { text: parameter.value, kind: "text" as const },
            })),
            ...output.parameters.map((parameter, parameterIndex) => ({
              id: `inputs-outputs/process/${index}/output/${output.id}/${parameter.key}/${parameterIndex}`,
              kind: parameter.key === "Analysis" ? ("factor" as const) : ("characteristic" as const),
              assignmentScope: "entry" as const,
              assignedEntryIds: [output.id],
              headerName: parameter.key,
              category: { name: parameter.key },
              value: { text: parameter.value, kind: "text" as const },
            })),
          ],
        };
      }),
    },
  ];

  return createProvenanceModelFromTables(source).model;
}
```

Use this helper only in a new story first, not in `InteractiveMockup`.

- [ ] **Step 5: Add a non-invasive normalized story**

Append a new story that verifies the bridge can feed the existing component:

```tsx
export const NormalizedModelPreview: Story = {
  name: "Normalized model preview",
  render: () => {
    const model = initialNormalizedModel();
    return (
      <ProvenanceGrouping
        layers={[
          { id: "inputs-outputs:inputs", label: "Inputs" },
          { id: "inputs-outputs:outputs", label: "Outputs" },
        ]}
        items={[
          ...toGroupingItems(model, "inputs-outputs", "inputs"),
          ...toGroupingItems(model, "inputs-outputs", "outputs"),
        ]}
        connections={toGroupingConnections(model)}
        leftLayerId="inputs-outputs:inputs"
        rightLayerId="inputs-outputs:outputs"
        groupingByLayer={{ "inputs-outputs:inputs": [], "inputs-outputs:outputs": [] }}
        onToggleGrouping={() => undefined}
        onSortLayer={() => undefined}
        onMoveParameter={() => undefined}
        onCreateParameter={() => undefined}
        onCreateParameterValue={() => undefined}
        onAssignParameterValue={() => undefined}
        onSelectGroup={() => undefined}
        onConnectGroups={() => undefined}
        onOpenDetail={() => undefined}
        onUpdateParameter={() => undefined}
        onCreateItem={() => undefined}
        onAddLayer={() => undefined}
        onSelectPair={() => undefined}
        onDismissError={() => undefined}
      />
    );
  },
};
```

- [ ] **Step 6: Run story tests**

Run:

```powershell
npm run test:run -- ProvenanceModel ProvenanceGrouping
```

Expected: both story files pass.

- [ ] **Step 7: Commit**

```powershell
git add src/Components/src/ProvenanceGrouping/ProvenanceModel.ts src/Components/src/ProvenanceGrouping/ProvenanceModel.stories.tsx src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx
git commit -m "feat: bridge provenance model to grouping view"
```

## Task 5: Export Stable Model API

**Files:**
- Modify: `src/Components/src/index.js`

- [ ] **Step 1: Export the model helpers**

Modify `src/Components/src/index.js`:

```js
export { ProvenanceGrouping } from './ProvenanceGrouping/ProvenanceGrouping';
export {
  createProvenanceModelFromTables,
  getPropertiesForEntry,
  getPropertyValuesForEntry,
  getPropertyCategoriesForTable,
  updatePropertyValue,
  createPropertyValueInScope,
  validatePreviousTablePropertyTarget,
  toGroupingItems,
  toGroupingConnections,
} from './ProvenanceGrouping/ProvenanceModel';
```

If the existing file has other exports, preserve them and add only the new export block.

- [ ] **Step 2: Build declarations**

Run:

```powershell
npm run build
```

Expected: command exits 0. Existing generated Fable declaration diagnostics may still print; verify no diagnostics reference `ProvenanceModel.ts`.

- [ ] **Step 3: Commit**

```powershell
git add src/Components/src/index.js
git commit -m "feat: export provenance edit model"
```

## Task 6: Final Verification

**Files:**
- Verify only.

- [ ] **Step 1: Run focused Storybook tests**

Run:

```powershell
npm run test:run -- ProvenanceModel ProvenanceGrouping
```

Expected: all tests in `ProvenanceModel.stories.tsx` and `ProvenanceGrouping.stories.tsx` pass.

- [ ] **Step 2: Run build**

Run:

```powershell
npm run build
```

Expected: exit 0. Existing generated declaration diagnostics may print; none should reference `ProvenanceModel.ts` or the new story.

- [ ] **Step 3: Check diff**

Run:

```powershell
git diff --check
git status --short
```

Expected: `git diff --check` reports no whitespace errors. `git status --short` is clean after the final commit.

- [ ] **Step 4: Manual Storybook smoke check**

Start Storybook if it is not already running:

```powershell
npm run storybook
```

Open:

```text
http://127.0.0.1:6006/?path=/story/components-provenancemodel--import-model
http://127.0.0.1:6006/?path=/story/components-provenancegrouping--normalized-model-preview
```

Expected: both stories render without an error overlay.

## Self-Review

Spec coverage:

- Normalized model independent from ARCtrl: Task 1.
- Table/header based source tracking without row/column position: Task 1 and Task 3 patch shape.
- Existing value edits: Task 3.
- New property values in current scope: Task 3.
- Previous table guard requiring existing connected entries: Task 3.
- New downstream table support: model fields in Task 1 and generated table path in Task 3 patch structure.
- Grouping bridge for current mockup: Task 4.

Placeholder scan:

- No placeholder markers or unspecified implementation steps remain.

Type consistency:

- `ProvenancePropertyKind`, `ProvenancePropertyValue`, `ProvenanceImportTable`, `ProvenanceTablePatch`, and bridge type names are consistent across tasks.
