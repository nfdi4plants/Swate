# Provenance Grouping Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an interactive Storybook mockup for layered provenance grouping in `src/Components`.

**Architecture:** Create a reusable TypeScript React component with colocated pure model helpers and a stateful Storybook story. The component renders one adjacent layer pair, derives group views from parameters, shows group-level connector lines, and emits callback intents; the story owns mock data mutations.

**Tech Stack:** React 19, TypeScript, Storybook play tests, Vitest browser runner, Swate Tailwind/DaisyUI classes with `swt:` prefix and `swt:iconify` Fluent icons.

---

## File Structure

- Create `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`
  - Public types.
  - Pure helper functions for grouping, connection coverage, propagation, and link creation.
  - `ProvenanceGrouping` React component.
- Create `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`
  - Stateful mock data wrapper.
  - Interaction story and Storybook play tests.
- Modify `src/Components/src/index.js`
  - Export `ProvenanceGrouping` for package consumers.

## Task 1: Red Tests For Model Helpers And Story Surface

**Files:**
- Create: `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

- [ ] **Step 1: Add a failing Storybook story that imports the missing component**

Create a story that imports:

```tsx
import {
  ProvenanceGrouping,
  addParameterToGroup,
  buildGroups,
  classifyGroupConnection,
  connectGroups,
  updateParameterInGroup,
  type ProvenanceConnection,
  type ProvenanceItem,
  type ProvenanceLayer,
} from "./ProvenanceGrouping";
```

The play tests must assert:

```tsx
await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-left-Temperature"));
await expect(await canvas.findByText("Temperature: 12 C")).toBeInTheDocument();
await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-left-Species"));
await expect(await canvas.findByText(/Species: Arabidopsis/)).toBeInTheDocument();
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test:run -- src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

Expected: FAIL because `./ProvenanceGrouping` does not exist yet.

## Task 2: Implement Pure Model Helpers

**Files:**
- Create: `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`

- [ ] **Step 1: Write minimal helper implementation**

Implement these exported functions with real behavior:

```tsx
export function buildGroups(
  items: ProvenanceItem[],
  layerId: string,
  groupingKeys: string[],
): ProvenanceGroup[];

export function classifyGroupConnection(
  sourceGroup: ProvenanceGroup,
  targetGroup: ProvenanceGroup,
  connections: ProvenanceConnection[],
): ConnectionCoverage;

export function addParameterToGroup(
  items: ProvenanceItem[],
  connections: ProvenanceConnection[],
  group: ProvenanceGroup,
  key: string,
  value: string,
): ModelMutationResult<ProvenanceItem[]>;

export function updateParameterInGroup(
  items: ProvenanceItem[],
  connections: ProvenanceConnection[],
  group: ProvenanceGroup,
  key: string,
  value: string,
): ModelMutationResult<ProvenanceItem[]>;

export function connectGroups(
  connections: ProvenanceConnection[],
  sourceGroup: ProvenanceGroup,
  targetGroup: ProvenanceGroup,
): ProvenanceConnection[];
```

Rules:

- Missing grouping values render as `Missing <key>`.
- Full coverage means each source item and each target item has at least one link across the two groups.
- Partial coverage is detected but not treated as a normal line.
- Add fails if any affected group/downstream item already has the key.
- Update overwrites or inserts the key on the group and downstream chain.
- Group connection creates minimum round-robin links to satisfy full coverage.

- [ ] **Step 2: Run test to verify helper-backed story now compiles further**

Run: `npm run test:run -- src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

Expected: FAIL because the component UI is not implemented yet, not because helper exports are missing.

## Task 3: Implement Reusable Component UI

**Files:**
- Modify: `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`

- [ ] **Step 1: Add component props and render layout**

Implement:

```tsx
export function ProvenanceGrouping(props: ProvenanceGroupingProps): React.ReactElement;
```

The component must render:

- Toolbar with current layer pair and add-layer action.
- Left and right parameter rails.
- Left and right group columns.
- Center connector lane with group-level line rows.
- Detail panel for group and parameter drill-ins.
- Inline add/update parameter editors for selected groups.
- Create item controls for each visible layer.

- [ ] **Step 2: Run story test**

Run: `npm run test:run -- src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

Expected: FAIL only for story-owned mutation behavior that is not wired yet.

## Task 4: Implement Stateful Story Mock

**Files:**
- Modify: `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

- [ ] **Step 1: Wire story state and callbacks**

The story wrapper must:

- Start with `Inputs` and `Outputs`.
- Store `layers`, `items`, `connections`, selected adjacent pair, grouping keys, selected groups, detail state, and error.
- Toggle grouping through parameter blocks.
- Add and update parameters through helper functions.
- Connect selected source/target groups through `connectGroups`.
- Copy missing source parameters into target items on connection.
- Create items with empty parameter lists.
- Add a downstream layer and switch to the new adjacent pair.

- [ ] **Step 2: Run story test**

Run: `npm run test:run -- src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

Expected: PASS for the provenance grouping story.

## Task 5: Export And Verify Package Build

**Files:**
- Modify: `src/Components/src/index.js`

- [ ] **Step 1: Export the component**

Add:

```js
export { ProvenanceGrouping } from './ProvenanceGrouping/ProvenanceGrouping';
```

- [ ] **Step 2: Run full component verification**

Run:

```powershell
npm run test:run
npm run build
```

Expected: both commands exit `0`.

## Task 6: Start Storybook For Review

**Files:** none

- [ ] **Step 1: Start Storybook**

Run:

```powershell
npm run storybook -- --host 127.0.0.1
```

Expected: Storybook serves the `Components/ProvenanceGrouping` story at `http://127.0.0.1:6006/`.
