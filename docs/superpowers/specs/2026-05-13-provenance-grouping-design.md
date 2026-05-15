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
- Missing values for selected keys are shown explicitly as `Missing <key>` so items do not disappear.

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

Use `swt:iconify` with fully qualified Fluent icon classes for new icons.

## Group-Level Connections

Connections are rendered as lines between visible groups only. There are no per-entry connection lines in the main view.

A line between source group `A` and target group `B` means full coverage:

- Every entry in source group `A` has at least one connection to an entry in target group `B`.
- Every entry in target group `B` has at least one connection from an entry in source group `A`.

The line does not mean every source entry is connected to every target entry. Full mesh is not required.

Partial group connections are invalid. If sample data or an edit creates a state where only some entries in either group are covered, the UI should flag that connection as invalid instead of presenting it as a normal line.

Creating a group-level connection should create the minimum item-level links needed to satisfy full coverage. When connecting groups, target items inherit missing source parameters from connected sources. Existing target parameter values are preserved by connection inheritance.

## Editing Rules

Bulk edits happen at group level.

Adding a parameter:

- Applies to every item in the selected group.
- Propagates through the full downstream chain.
- Fails if any affected item already has the same parameter key.
- Shows an error telling the user to choose another parameter name.

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

- Adding a parameter key that already exists in the affected group or downstream chain.
- Attempting to present a partial grouped connection as a normal connection.
- Trying to connect groups when either group is empty.

Errors should not silently mutate state.

## Testing

Test the pure helper logic before relying on the UI:

- Grouping by zero, one, and multiple keys.
- Missing parameter bucket creation.
- Group connection full-coverage detection.
- Partial connection detection.
- Bulk add propagation success.
- Bulk add duplicate-key failure.
- Bulk update propagation with overwrite.
- Minimum-link creation for many-to-many full coverage.

Add Storybook play tests for the first mockup flow:

- Toggle `Temperature` grouping and verify distinct groups appear.
- Add `Species` grouping and verify refined group labels.
- Open parameter detail.
- Attempt duplicate parameter add and verify the error.
- Update an existing parameter and verify downstream output values change.
- Create a group connection and verify a group-level line appears.
- Add a downstream layer and verify the adjacent pair switches or becomes selectable.

## Non-Goals For The First Mockup

- No graph layout.
- No production persistence.
- No integration with `src/Client`, Electron, ARCtrl tables, or server APIs.
- No ontology-backed parameter editing.
- No drag-and-drop requirement unless later requested during review.
