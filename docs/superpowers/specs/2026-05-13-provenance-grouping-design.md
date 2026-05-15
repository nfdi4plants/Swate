# Provenance Grouping Component Design

## Purpose

Build a reusable `src/Components` mockup component for exploring layered provenance grouping. The first target is an interactive Storybook story with sample data, not production integration. The component must stay application-agnostic: it receives data and callbacks through props, while the story owns mock state and applies realistic transformations.

The component helps users inspect and edit provenance entities by grouped parameter values instead of individual rows. It starts with two layers, `Inputs` and `Outputs`. Additional downstream layers can be added during the workflow, and the UI displays one adjacent layer pair at a time.

## Placement

Create a new component folder:

`src/Components/src/ProvenanceGrouping`

The implementation should follow `docs/ReactComponentDesign.md`:

- PascalCase component files.
- Public shared types in a separate `Types.fs`.
- Pure grouping and propagation helpers outside the component class.
- A colocated `ProvenanceGrouping.stories.tsx`.
- No imports from `src/Client`, Electron renderer state, or app workflow code.

## Core Model

The model is layer-based rather than hardcoded to inputs and outputs.

```fsharp
type ProvenanceParameter = {
    Key: string
    Value: string
}

type ProvenanceItem = {
    Id: string
    Name: string
    LayerId: string
    Parameters: ProvenanceParameter list
}

type ProvenanceConnection = {
    SourceId: string
    TargetId: string
}

type ProvenanceLayer = {
    Id: string
    Label: string
}
```

The initial story data contains two layers. When a new layer is added, the previous output layer becomes the left layer of the next adjacent pair, and the newly created layer becomes the right layer.

Connections are many-to-many between items. A single source item can connect to multiple target items, and a single target item can connect to multiple source items.

## Grouping Semantics

Grouping is configured per displayed layer. A layer starts grouped by each entry's complete parameter signature, and can then be grouped by any ordered set of selected parameter keys.

Groups are derived from the selected keys:

- No selected grouping keys: one group for each distinct complete parameter signature.
- One key: one group for each distinct value.
- Multiple keys: one group for each distinct ordered tuple of values.
- Once a key is selected for grouping, non-selected parameters are ignored for grouping.
- Missing selected keys are ignored for grouping rather than becoming their own `Missing <key>` category. If an item has none of the selected keys, it appears in a single `Ungrouped` bucket.

The UI displays groups, not raw entries. Entries appear only in drill-in/detail views.

## Layout

The component shows one adjacent layer pair at a time:

- Left grouping-control rail for input/shared parameters.
- Left group column.
- A center connector area with group-level lines.
- Right group column.
- Right grouping-control rail for output-only parameters and shared parameters moved there.

The layout should feel like a dense work surface or table/block editor, not a graph view. Group cards or blocks should contain concise labels, item counts, connection status, and available actions. Raw item names and per-entry parameter details are hidden until a group or parameter detail view is opened.

Parameter blocks are grouping controls. Clicking a block on the left groups the left layer by that parameter. Clicking a block on the right groups both displayed layers by that parameter. Shared parameter blocks can be moved between rails by drag/drop or the move affordance. Each block shows distinct values only; SVG connector lines link each value to the group cards it applies to.

Users can add parameters to either rail and add candidate values to those parameters. Candidate values appear as draggable value rows even before they are assigned to any group. Dragging a value onto a group assigns that parameter value to every item in the group. A group can still show multiple values for a parameter when those values already exist among its member items before grouping by a different parameter.

Use `swt:iconify` with fully qualified Fluent icon classes for new icons.

## Group-Level Connections

Connections are rendered as clickable lines between visible groups only. There is no separate group-link list or box in the connector area.

A line between source group `A` and target group `B` means a complete group link.

The mockup does not expose partial group links. Creating a group link connects all source entries in the source group with all target entries in the target group. Clicking a group connector expands both connected groups and replaces that connector with member-level connector lines between the expanded entries.

Creating a group-level connection should create all item-level links between the source and target group. When connecting groups, target items inherit missing source parameters from connected sources. Existing target parameter values are preserved by connection inheritance.

## Editing Rules

Bulk edits happen through group-level controls and parameter rails.

Adding a parameter value from a rail:

- Adds the value as a candidate in the parameter block.
- Applies to every item in the drop target group when dragged onto a group.
- Propagates through the full downstream chain.

Updating an existing parameter:

- Applies to every item in the selected group.
- Propagates through the full downstream chain.
- Overwrites the same parameter key in connected descendants.

Creating a new item:

- New items start with only `Id`, `Name`, and `LayerId`.
- Parameters can be added later through group edits.

Creating a new layer:

- Adds a downstream layer after the current right layer.
- Lets the user navigate to the new adjacent pair.
- Uses the same item model, so outputs from one step can act as inputs to the next displayed pair.

## Component Boundary

Use a controlled component for the reusable UI and keep mutable mock behavior in the Storybook story.

The reusable component should accept:

- `layers`
- `items`
- `connections`
- selected left and right layer IDs
- grouping keys per layer
- selection/detail state where needed
- callback props for user intents

The Storybook story should own state with React and provide callbacks that mutate the sample model. This keeps the component reusable while making the mockup genuinely interactive.

Pure helper modules should cover:

- Available parameter keys per layer.
- Group derivation.
- Group connection coverage classification.
- Downstream traversal.
- Bulk add/update propagation.
- Minimum-link creation for group connections.

## Storybook Mockup

Create a story with sample data that includes:

- Two starting layers: `Inputs` and `Outputs`.
- Input items with shared and varying `Species`, `Temperature`, and `Replicate`.
- Output items with inherited parameters plus extra `Analysis`.
- Some unconnected outputs.
- At least one many-to-many connection.

The first story should let the user:

- Toggle grouping keys through parameter blocks.
- Inspect group entries and per-entry parameter values.
- Add a new parameter to a group.
- Update an existing parameter on a group.
- Connect a right-layer target group to a left-layer source group, producing source-to-target item links.
- Create a new item in the visible layers.
- Add a downstream layer and switch to the new adjacent pair.

Story interaction tests should cover the core behavior, but the story itself is the main review artifact because the user expects to make edit requests after viewing the mockup.

## Error Handling

The mockup should surface errors inline near the action that caused them. Required errors:

- Trying to connect groups when either group is empty.

Errors should not silently mutate state.

## Testing

Test the pure helper logic before relying on the UI:

- Grouping by zero, one, and multiple keys.
- Group connection full-coverage detection.
- Bulk update propagation with overwrite.
- Complete item-link creation for many-to-many group connections.

Add Storybook play tests for the first mockup flow:

- Toggle `Temperature` grouping and verify distinct groups appear.
- Add `Species` grouping and verify refined group labels.
- Open parameter detail.
- Update an existing parameter and verify downstream output values change.
- Create a group connection and verify a group-level line appears.
- Click a group connection and verify both groups expand with member-level connector lines.
- Add a downstream layer and verify the adjacent pair switches or becomes selectable.

## Non-Goals For The First Mockup

- No graph layout.
- No production persistence.
- No integration with `src/Client`, Electron, ARCtrl tables, or server APIs.
- No ontology-backed parameter editing.
- No drag-and-drop requirement unless later requested during review.
