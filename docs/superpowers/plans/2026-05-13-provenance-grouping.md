# Provenance Grouping Implementation Plan

> **For agentic workers:** Execute this plan inline in the current Swate workspace. Do not dispatch subagents for this mockup, per the user request. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Evolve the existing `ProvenanceGrouping` Storybook mockup so grouping is membership-based, supports repeated parameter values per entry, and treats connected output-only values as movable side controls.

**Architecture:** Keep the reusable UI in `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx` and the stateful sample workflow in `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`. Derive visible groups from entry memberships instead of unique item IDs, and keep all mutable mock behavior in the story.

**Tech Stack:** React 19, TypeScript, Storybook, Swate Tailwind/DaisyUI classes with `swt:` prefix and `swt:iconify` Fluent icons.

---

## Current Files

- Modify `docs/superpowers/specs/2026-05-13-provenance-grouping-design.md`
  - Source of truth for the mockup semantics.
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`
  - Public mockup types.
  - Pure grouping, value, sorting, connector, and propagation helpers.
  - Reusable component rendering.
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`
  - Stateful sample data and interaction callbacks.
- Leave `src/Components/src/index.js` as-is unless exports are missing.

## Updated Rules To Preserve

- No selected grouping keys means one visible element per entry, labeled by entry name only.
- Selecting a grouping parameter ignores non-selected parameters for group identity.
- Missing selected values do not create `Missing <key>` groups. If grouping is active and an entry has none of the selected keys, it remains its own item element labeled by entry name.
- One entry can have several values for the same parameter key.
- Grouping duplicates an entry into every group membership it belongs to.
- A visible group contains memberships, not necessarily unique item IDs.
- Group connector detail must use actual item-level connections only.
- Output-only property values can move to, or be represented on, the input side when connected items make them relevant there.
- Parameter rails list distinct values, not every individual parameter occurrence.
- Candidate values can be dragged onto groups. Assigning a value to a group appends that key/value to every member item if it is not already present.
- Updating an existing value targets a concrete key/value instance and replaces that value; it must not collapse other values for the same key.

## Task 1: Convert Parameter Access To Multi-Value Helpers

**Files:**
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`

- [ ] **Step 1: Replace single-value assumptions with list-returning helpers**

Add or update helpers so all model logic uses these shapes:

```tsx
function getParameterValues(item: ProvenanceItem, key: string): string[] {
  return item.parameters
    .filter((parameter) => parameter.key === key)
    .map((parameter) => parameter.value);
}

function hasParameterValue(item: ProvenanceItem, key: string, value: string): boolean {
  return getParameterValues(item, key).includes(value);
}

function addParameterValue(item: ProvenanceItem, key: string, value: string): ProvenanceItem {
  if (hasParameterValue(item, key, value)) {
    return item;
  }

  return {
    ...item,
    parameters: [...item.parameters, { key, value }],
  };
}
```

- [ ] **Step 2: Keep single-value reads only for ordering labels**

If a helper is still needed for sorting, make its limited role explicit:

```tsx
function getFirstSortedParameterValue(item: ProvenanceItem, key: string): string | undefined {
  return [...new Set(getParameterValues(item, key))]
    .map((value) => value.trim())
    .filter(Boolean)
    .sort((left, right) => left.localeCompare(right, undefined, { numeric: true, sensitivity: "base" }))[0];
}
```

- [ ] **Step 3: Search for old single-value usage**

Run:

```powershell
rg "getParameterValue|parameter.value|Missing <key>|Missing " src/Components/src/ProvenanceGrouping
```

Expected: any remaining single-value helper use is either removed or limited to display/sorting code. There should be no `Missing <key>` grouping path.

## Task 2: Make Grouping Membership-Based

**Files:**
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`

- [ ] **Step 1: Introduce explicit group membership data**

Update the internal group model to distinguish a membership from an item:

```tsx
type ProvenanceGroupMember = {
  item: ProvenanceItem;
  membershipId: string;
  groupingValues: ProvenanceGroupPart[];
};

type ProvenanceGroup = {
  id: string;
  layerId: string;
  label: string;
  parts: ProvenanceGroupPart[];
  members: ProvenanceGroupMember[];
};
```

If the existing component still expects `group.items`, either replace those reads with `group.members.map((member) => member.item)` or provide a local adapter while migrating the render code.

- [ ] **Step 2: Implement value combinations for selected grouping keys**

Use a cartesian product helper for entries with repeated values:

```tsx
function getGroupingValueSets(item: ProvenanceItem, groupingKeys: string[]): ProvenanceGroupPart[][] {
  if (groupingKeys.length === 0) {
    return [[]];
  }

  const valueSets = groupingKeys.map((key) => {
    const values = [...new Set(getParameterValues(item, key))].sort((left, right) =>
      left.localeCompare(right, undefined, { numeric: true, sensitivity: "base" }),
    );

    return values.map((value) => ({ key, value }));
  });

  const usableValueSets = valueSets.filter((values) => values.length > 0);

  if (usableValueSets.length === 0) {
    return [[]];
  }

  return usableValueSets.reduce<ProvenanceGroupPart[][]>(
    (combinations, values) =>
      combinations.flatMap((combination) => values.map((value) => [...combination, value])),
    [[]],
  );
}
```

- [ ] **Step 3: Change no-grouping output**

When `groupingKeys.length === 0`, `buildGroups` must return one visible group per item:

```tsx
const group: ProvenanceGroup = {
  id: item.id,
  layerId,
  label: item.name,
  parts: [],
  members: [{ item, membershipId: item.id, groupingValues: [] }],
};
```

- [ ] **Step 4: Preserve duplicate item memberships**

When grouping is active, build one `ProvenanceGroupMember` per value combination. The same `item.id` may appear in several groups with different `membershipId` values such as `${item.id}:${groupKey}`.

- [ ] **Step 5: Update count and expanded detail labels**

Visible counts should count memberships. Expanded detail rows may show the same item name in more than one group when it has several values for the grouping key.

## Task 3: Update Sorting For Membership Groups

**Files:**
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`

- [ ] **Step 1: Sort groups by member sort values**

When a layer is sorted by a non-grouping parameter, sort visible groups by the first normalized sorted value among their members:

```tsx
function getGroupSortValue(group: ProvenanceGroup, sortKey: string): string {
  return group.members
    .map((member) => getFirstSortedParameterValue(member.item, sortKey))
    .filter((value): value is string => Boolean(value))
    .sort((left, right) => left.localeCompare(right, undefined, { numeric: true, sensitivity: "base" }))[0] ?? "";
}
```

- [ ] **Step 2: Sort expanded members without deduplicating entries**

When a group is expanded, sort `group.members`, not a unique item list. Entries with repeated grouping values should remain visible once per membership.

- [ ] **Step 3: Verify manual behavior**

In Storybook:

1. Clear grouping.
2. Sort inputs by `Replicate`.
3. Confirm single-entry elements reorder by replicate value.
4. Group by `Species`.
5. Sort by `Temperature`.
6. Confirm entries inside each species group order by temperature while duplicated memberships remain visible.

## Task 4: Make Parameter Rails Connection-Aware

**Files:**
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

- [ ] **Step 1: Derive distinct rail values from visible memberships**

Parameter rail blocks should show each distinct value once:

```tsx
type ParameterRailValue = {
  key: string;
  value: string;
  side: "left" | "right";
  derivedFromConnection: boolean;
};
```

- [ ] **Step 2: Allow output-only values on the input side when connected**

For the displayed adjacent pair, include a right-side key/value in the left rail when at least one actual connection links a left item to a right item carrying that key/value. Mark it with `derivedFromConnection: true` so the UI can still distinguish it from values physically present on left-layer items.

- [ ] **Step 3: Preserve shared value drag behavior**

Dragging a value between rails changes where the grouping control is presented. It must not duplicate parameters onto items by itself. Only dropping a value onto a group assigns that value to member items.

- [ ] **Step 4: Keep right-side grouping behavior**

Clicking a value block on the right still groups both visible layers by that parameter. If the value moved from output-only to the left rail, clicking it on the left groups the left layer by that parameter using connected or physically present values.

## Task 5: Update Value Assignment And Propagation

**Files:**
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

- [ ] **Step 1: Append exact key/value pairs**

When a candidate value is dropped on a group, apply it to each membership item with `addParameterValue`. The same item may appear multiple times in the group memberships; update each item ID once.

- [ ] **Step 2: Preserve duplicate keys during updates**

When updating an existing value, replace only matching key/value instances:

```tsx
function replaceParameterValue(item: ProvenanceItem, key: string, oldValue: string, newValue: string): ProvenanceItem {
  return {
    ...item,
    parameters: item.parameters.map((parameter) =>
      parameter.key === key && parameter.value === oldValue
        ? { ...parameter, value: newValue }
        : parameter,
    ),
  };
}
```

- [ ] **Step 3: Propagate through actual downstream connections**

Propagation should follow the existing item-level connection graph. If a connected descendant already has other values for the same key, keep them unless they match the exact value being replaced.

- [ ] **Step 4: Keep duplicate-name add errors scoped to parameter creation**

Creating a new parameter key on a rail should still reject a duplicate key on that rail. Adding another value to an existing key is valid.

## Task 6: Update Connector Detail For Membership Groups

**Files:**
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`

- [ ] **Step 1: Build connector lines from actual item IDs**

When a group connector is clicked, derive member-level lines by filtering actual connections:

```tsx
const sourceIds = new Set(sourceGroup.members.map((member) => member.item.id));
const targetIds = new Set(targetGroup.members.map((member) => member.item.id));
const visibleConnections = connections.filter(
  (connection) => sourceIds.has(connection.sourceId) && targetIds.has(connection.targetId),
);
```

- [ ] **Step 2: Avoid all-to-all expansion unless all-to-all exists**

Do not synthesize detail lines from group membership. If `Input A -> Output A`, `Input A -> Output B`, `Input B -> Output B`, and `Input C -> Output C`, grouped output detail must show only those links.

- [ ] **Step 3: Handle duplicated memberships**

If the same item appears in two visible groups because of repeated values, the connector detail line should appear only in the group pair that contains the relevant duplicated membership.

## Task 7: Update Story Sample Data

**Files:**
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

- [ ] **Step 1: Add repeated values to one output**

Add a sample output that carries repeated values for the same key:

```tsx
{
  id: "output-b",
  name: "Output B",
  layerId: "outputs",
  parameters: [
    { key: "Species", value: "Arabidopsis" },
    { key: "Temperature", value: "24 C" },
    { key: "Replicate", value: "1" },
    { key: "Replicate", value: "2" },
    { key: "Analysis", value: "Mass Spectrometry" },
  ],
}
```

- [ ] **Step 2: Keep many-to-many connections explicit**

Use item-level sample connections that demonstrate the bug case:

```tsx
[
  { sourceId: "input-a", targetId: "output-a" },
  { sourceId: "input-a", targetId: "output-b" },
  { sourceId: "input-b", targetId: "output-b" },
  { sourceId: "input-c", targetId: "output-c" },
]
```

- [ ] **Step 3: Add an output-only value that can move left**

Keep at least one right-only key, such as `Analysis`, on connected outputs so the input rail can show it as connection-derived.

## Task 8: Browser Smoke Verification

**Files:** none

- [ ] **Step 1: Start Storybook if it is not already running**

Run from `src/Components`:

```powershell
npm run storybook -- --host 127.0.0.1
```

Expected: Storybook serves the component story at `http://127.0.0.1:6006/?path=/story/components-provenancegrouping--interactive-mockup`.

- [ ] **Step 2: Verify no-grouping display**

In the story:

1. Clear input and output grouping.
2. Confirm every entry appears once.
3. Confirm the visible labels are entry names, not full parameter signatures.

- [ ] **Step 3: Verify multi-value grouping**

In the story:

1. Group outputs by `Replicate`.
2. Confirm `Output B` appears in both `Replicate: 1` and `Replicate: 2`.
3. Group outputs by another parameter and confirm non-selected parameters no longer split groups.

- [ ] **Step 4: Verify connector detail**

In the story:

1. Group outputs by `Species`.
2. Click the group connector involving the `Arabidopsis` output group.
3. Confirm only actual item connections render: `Input A -> Output A`, `Input A -> Output B`, and `Input B -> Output B` if those are the only matching connections.
4. Confirm no all-to-all member lines are synthesized.

- [ ] **Step 5: Verify output-only value movement**

In the story:

1. Find a connected output-only value such as `Analysis: Mass Spectrometry`.
2. Move or present it on the left rail.
3. Click it on the left and confirm the left layer groups using connected or physically present values.

## Task 9: Commit The Revision

**Files:**
- Modify: all changed files from Tasks 1-8

- [ ] **Step 1: Check the diff**

Run:

```powershell
git diff -- src/Components/src/ProvenanceGrouping docs/superpowers
```

Expected: the diff is limited to the provenance grouping mockup and plan/spec docs.

- [ ] **Step 2: Run whitespace check**

Run:

```powershell
git diff --check
```

Expected: no output and exit code `0`.

- [ ] **Step 3: Commit**

Run:

```powershell
git add docs/superpowers/specs/2026-05-13-provenance-grouping-design.md docs/superpowers/plans/2026-05-13-provenance-grouping.md src/Components/src/ProvenanceGrouping
git commit -m "feat: support multi-value provenance grouping"
```

## Task 10: Keep Ungrouped Active-Grouping Entries As Individual Items

**Files:**
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

- [ ] **Step 1: Change active-grouping fallback groups**

When grouping is active and `getGroupingValueSets` returns no selected-key values for an item, `buildGroups` must create a single-item group instead of merging all such entries into `layerId-ungrouped`:

```tsx
const id = labelParts.length === 0 ? `${layerId}-item-${slug(item.id)}` : groupId(layerId, labelParts);
const label = labelParts.length === 0 ? item.name : groupLabel(labelParts);
```

- [ ] **Step 2: Update Storybook assertions**

In `InteractionFlow`, creating a right-layer item while grouped by `Analysis` should show the new item by name and should not show `Missing Analysis`.

## Task 11: Seed New Layers From Selected Groups

**Files:**
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx`
- Modify `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.stories.tsx`

- [ ] **Step 1: Support explicit layer pairs and scoped visible items**

Add optional component props:

```tsx
type ProvenanceLayerPair = {
  leftLayerId: string;
  rightLayerId: string;
};

layerPairs?: ProvenanceLayerPair[];
visibleItemIdsByLayer?: Record<string, string[]>;
```

Use `layerPairs` for the pair switcher when present. Use `visibleItemIdsByLayer` to filter the items used for visible grouping, parameter rails, value resolvers, and group connector summaries.

- [ ] **Step 2: Track story layer pairs and pair scopes**

Add story state:

```tsx
layerPairs: ProvenanceLayerPair[];
visibleItemIdsByPair: Record<string, Record<string, string[]>>;
```

The initial pair is `Inputs -> Outputs` with no scope. When adding a layer:

- Prefer the selected right/output group as the source.
- If no right group is selected, use the selected left/input group.
- If no group is selected, use all entries from the current right layer.
- Store the selected source item IDs as the visible scope for the source layer in the new pair.

- [ ] **Step 3: Verify selected group handoff**

Add or update a Storybook play test that selects one output group, clicks `Layer`, and verifies the next pair's left column contains only that selected output entry set.
