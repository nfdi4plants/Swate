import React from "react";

export type ProvenanceParameter = {
  key: string;
  value: string;
};

export type ProvenanceItem = {
  id: string;
  name: string;
  layerId: string;
  parameters: ProvenanceParameter[];
};

export type ProvenanceConnection = {
  sourceId: string;
  targetId: string;
};

export type ProvenanceLayer = {
  id: string;
  label: string;
};

export type ProvenanceGroupPart = {
  key: string;
  value: string;
};

export type ProvenanceGroup = {
  id: string;
  layerId: string;
  label: string;
  groupingKeys: string[];
  labelParts: ProvenanceGroupPart[];
  items: ProvenanceItem[];
};

export type ConnectionCoverage = "none" | "full";

export type ModelMutationResult<T> =
  | { ok: true; value: T }
  | { ok: false; error: string };

export type ProvenanceSide = "left" | "right";

export type ProvenanceDetail =
  | { kind: "group"; side: ProvenanceSide; groupId: string }
  | { kind: "parameter"; layerId: string; key: string }
  | { kind: "connection"; sourceGroupId: string; targetGroupId: string };

export type ProvenanceGroupingProps = {
  layers: ProvenanceLayer[];
  items: ProvenanceItem[];
  connections: ProvenanceConnection[];
  leftLayerId: string;
  rightLayerId: string;
  groupingByLayer: Record<string, string[]>;
  sortingByLayer?: Record<string, string | undefined>;
  parameterValuesByLayer?: Record<string, Record<string, string[]>>;
  parameterRailByKey?: Record<string, ProvenanceSide>;
  selectedSourceGroupId?: string;
  selectedTargetGroupId?: string;
  detail?: ProvenanceDetail;
  error?: string;
  onToggleGrouping: (side: ProvenanceSide, key: string) => void;
  onSortLayer: (layerId: string, key: string | undefined) => void;
  onMoveParameter: (key: string, side: ProvenanceSide) => void;
  onCreateParameter: (side: ProvenanceSide, key: string) => void;
  onCreateParameterValue: (side: ProvenanceSide, key: string, value: string) => void;
  onAssignParameterValue: (side: ProvenanceSide, key: string, value: string, groupId: string) => void;
  onSelectGroup: (side: ProvenanceSide, groupId: string) => void;
  onOpenDetail: (detail: ProvenanceDetail) => void;
  onUpdateParameter: (side: ProvenanceSide, groupId: string, key: string, value: string) => void;
  onConnectSelectedGroups: () => void;
  onCreateItem: (layerId: string, name: string) => void;
  onAddLayer: () => void;
  onSelectPair: (leftLayerId: string, rightLayerId: string) => void;
  onDismissError: () => void;
};

type ConnectionSummary = {
  id: string;
  sourceGroup: ProvenanceGroup;
  targetGroup: ProvenanceGroup;
  links: ProvenanceConnectionDetail[];
};

type ProvenanceConnectionDetail = {
  id: string;
  source: ProvenanceItem;
  target: ProvenanceItem;
};

type EditorState = {
  side: ProvenanceSide;
  groupId: string;
};

type ParameterAvailability = "shared" | "left-only" | "right-only";

type ParameterRailEntry = {
  key: string;
  availability: ParameterAvailability;
  movable: boolean;
  active: boolean;
};

type ParameterValueConnector = {
  value: string;
  groups: ProvenanceGroup[];
};

type ValueGroupConnection = {
  id: string;
  side: ProvenanceSide;
  valueNodeId: string;
  groupNodeId: string;
};

type RenderedConnectorPath = {
  id: string;
  side: ProvenanceSide;
  path: string;
  start: { x: number; y: number };
  end: { x: number; y: number };
};

type ConnectorOverlay = {
  width: number;
  height: number;
  valuePaths: RenderedConnectorPath[];
  groupPaths: RenderedGroupConnectorPath[];
  itemPaths: RenderedItemConnectorPath[];
};

type RenderedGroupConnectorPath = {
  id: string;
  path: string;
  start: { x: number; y: number };
  end: { x: number; y: number };
  sourceGroupId: string;
  targetGroupId: string;
  sourceLabel: string;
  targetLabel: string;
};

type RenderedItemConnectorPath = {
  id: string;
  path: string;
  sourceGroupId: string;
  targetGroupId: string;
  sourceName: string;
  targetName: string;
};

type DraggedParameterValue = {
  side: ProvenanceSide;
  key: string;
  value: string;
};

const normalizeKey = (key: string) => key.trim().toLowerCase();

const slug = (value: string) =>
  value
    .trim()
    .replace(/[^A-Za-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 120) || "empty";

const connectionKey = (connection: ProvenanceConnection) => `${connection.sourceId}->${connection.targetId}`;

export function getParameterValue(item: ProvenanceItem, key: string): string | undefined {
  const normalized = normalizeKey(key);
  return item.parameters.find((parameter) => normalizeKey(parameter.key) === normalized)?.value;
}

export function availableParameterKeys(items: ProvenanceItem[], layerId: string): string[] {
  const keys: string[] = [];
  const seen = new Set<string>();

  items
    .filter((item) => item.layerId === layerId)
    .forEach((item) => {
      item.parameters.forEach((parameter) => {
        const normalized = normalizeKey(parameter.key);
        if (!seen.has(normalized)) {
          seen.add(normalized);
          keys.push(parameter.key);
        }
      });
    });

  return keys;
}

function parameterValueConfigForLayer(
  parameterValuesByLayer: Record<string, Record<string, string[]>> | undefined,
  layerId: string,
): Record<string, string[]> {
  return parameterValuesByLayer?.[layerId] ?? {};
}

function configuredKeys(parameterValues: Record<string, string[]>): string[] {
  return Object.keys(parameterValues).filter((key) => key.trim());
}

function mergeKeys(left: string[], right: string[]): string[] {
  const keys: string[] = [];
  const seen = new Set<string>();

  [...left, ...right].forEach((key) => {
    const normalized = normalizeKey(key);
    if (normalized && !seen.has(normalized)) {
      seen.add(normalized);
      keys.push(key);
    }
  });

  return keys;
}

function keyLookup(keys: string[]): Map<string, string> {
  return new Map(keys.map((key) => [normalizeKey(key), key]));
}

function containsKey(keys: string[], key: string): boolean {
  const normalized = normalizeKey(key);
  return keys.some((existingKey) => normalizeKey(existingKey) === normalized);
}

function canonicalKey(keys: string[], key: string | undefined): string | undefined {
  if (!key) {
    return undefined;
  }

  const normalized = normalizeKey(key);
  return keys.find((existingKey) => normalizeKey(existingKey) === normalized);
}

function displayKey(leftKey: string | undefined, rightKey: string | undefined): string {
  return leftKey ?? rightKey ?? "";
}

function buildParameterRailEntries(
  leftKeys: string[],
  rightKeys: string[],
  leftGroupingKeys: string[],
  rightGroupingKeys: string[],
  placements: Record<string, ProvenanceSide> | undefined,
): { left: ParameterRailEntry[]; right: ParameterRailEntry[] } {
  const leftByKey = keyLookup(leftKeys);
  const rightByKey = keyLookup(rightKeys);
  const normalizedKeys = Array.from(new Set([...leftByKey.keys(), ...rightByKey.keys()])).sort((left, right) =>
    displayKey(leftByKey.get(left), rightByKey.get(left)).localeCompare(displayKey(leftByKey.get(right), rightByKey.get(right))),
  );

  const entries = normalizedKeys.map((normalizedKey) => {
    const leftKey = leftByKey.get(normalizedKey);
    const rightKey = rightByKey.get(normalizedKey);
    const shared = Boolean(leftKey && rightKey);
    const availability: ParameterAvailability = shared ? "shared" : leftKey ? "left-only" : "right-only";
    const rail = shared ? placements?.[normalizedKey] ?? "left" : leftKey ? "left" : "right";
    const key = displayKey(leftKey, rightKey);
    const active =
      rail === "left"
        ? containsKey(leftGroupingKeys, key)
        : containsKey(leftGroupingKeys, key) && containsKey(rightGroupingKeys, key);

    return {
      rail,
      entry: {
        key,
        availability,
        movable: shared,
        active,
      },
    };
  });

  return {
    left: entries.filter((entry) => entry.rail === "left").map((entry) => entry.entry),
    right: entries.filter((entry) => entry.rail === "right").map((entry) => entry.entry),
  };
}

function valueConnectorsForKey(
  items: ProvenanceItem[],
  layerId: string,
  groups: ProvenanceGroup[],
  key: string,
  configuredValues: string[],
): ParameterValueConnector[] {
  const layerItems = items.filter((item) => item.layerId === layerId);
  const values = new Set([
    ...configuredValues.filter((value) => value.trim()),
    ...layerItems.flatMap((item) => {
      const value = getParameterValue(item, key);
      return value === undefined ? [] : [value];
    }),
  ]);

  return Array.from(values)
    .sort((left, right) => left.localeCompare(right))
    .map((value) => ({
      value,
      groups: groups.filter((group) => group.items.some((item) => getParameterValue(item, key) === value)),
    }));
}

function valueNodeId(side: ProvenanceSide, layerId: string, key: string, value: string): string {
  return `value-${side}-${slug(layerId)}-${slug(key)}-${slug(value)}`;
}

function groupNodeId(side: ProvenanceSide, groupIdValue: string): string {
  return `group-${side}-${slug(groupIdValue)}`;
}

function itemNodeId(side: ProvenanceSide, itemId: string): string {
  return `item-${side}-${slug(itemId)}`;
}

function readDraggedParameterValue(event: React.DragEvent): DraggedParameterValue | undefined {
  const rawValue = event.dataTransfer.getData("application/x-provenance-value");

  if (!rawValue) {
    return undefined;
  }

  try {
    const parsed = JSON.parse(rawValue) as DraggedParameterValue;
    return (parsed.side === "left" || parsed.side === "right") && parsed.key && parsed.value ? parsed : undefined;
  } catch {
    return undefined;
  }
}

function valueGroupConnectionsForRail(
  side: ProvenanceSide,
  layerId: string,
  entries: ParameterRailEntry[],
  items: ProvenanceItem[],
  groups: ProvenanceGroup[],
  parameterValues: Record<string, string[]>,
): ValueGroupConnection[] {
  return entries.flatMap((entry) =>
    valueConnectorsForKey(items, layerId, groups, entry.key, parameterValues[entry.key] ?? []).flatMap((value) =>
      value.groups.map((group) => ({
        id: `${valueNodeId(side, layerId, entry.key, value.value)}-${groupNodeId(side, group.id)}`,
        side,
        valueNodeId: valueNodeId(side, layerId, entry.key, value.value),
        groupNodeId: groupNodeId(side, group.id),
      })),
    ),
  );
}

function fullParameterSignature(item: ProvenanceItem): ProvenanceGroupPart[] {
  const partsByKey = new Map<string, ProvenanceGroupPart>();

  item.parameters.forEach((parameter) => {
    const key = parameter.key.trim();
    const value = parameter.value.trim();
    const normalized = normalizeKey(key);

    if (key && !partsByKey.has(normalized)) {
      partsByKey.set(normalized, { key, value });
    }
  });

  return Array.from(partsByKey.values()).sort((left, right) => {
    const keyComparison = normalizeKey(left.key).localeCompare(normalizeKey(right.key));
    return keyComparison !== 0 ? keyComparison : left.value.localeCompare(right.value);
  });
}

function groupLabel(labelParts: ProvenanceGroupPart[]): string {
  return labelParts.length === 0
    ? "No parameters"
    : labelParts.map((part) => `${part.key}: ${part.value}`).join(", ");
}

function groupId(layerId: string, labelParts: ProvenanceGroupPart[]): string {
  return `${layerId}-${
    labelParts.length === 0
      ? "no-parameters"
      : labelParts.map((part) => `${slug(part.key)}-${slug(part.value)}`).join("-")
  }`;
}

export function buildGroups(
  items: ProvenanceItem[],
  layerId: string,
  groupingKeys: string[],
): ProvenanceGroup[] {
  const layerItems = items.filter((item) => item.layerId === layerId);

  if (layerItems.length === 0) {
    return [];
  }

  if (groupingKeys.length === 0) {
    const groups = new Map<string, ProvenanceGroup>();

    layerItems.forEach((item) => {
      const labelParts = fullParameterSignature(item);
      const id = groupId(layerId, labelParts);
      const existing = groups.get(id);

      if (existing) {
        existing.items.push(item);
      } else {
        groups.set(id, {
          id,
          layerId,
          label: groupLabel(labelParts),
          groupingKeys: [],
          labelParts,
          items: [item],
        });
      }
    });

    return Array.from(groups.values()).sort((left, right) => left.label.localeCompare(right.label));
  }

  const groups = new Map<string, ProvenanceGroup>();

  layerItems.forEach((item) => {
    const labelParts = groupingKeys.flatMap((key) => {
      const value = getParameterValue(item, key);
      return value === undefined ? [] : [{ key, value }];
    });
    const id = labelParts.length === 0 ? `${layerId}-ungrouped` : groupId(layerId, labelParts);
    const existing = groups.get(id);

    if (existing) {
      existing.items.push(item);
    } else {
      groups.set(id, {
        id,
        layerId,
        label: labelParts.length === 0 ? "Ungrouped" : groupLabel(labelParts),
        groupingKeys,
        labelParts,
        items: [item],
      });
    }
  });

  return Array.from(groups.values()).sort((left, right) => left.label.localeCompare(right.label));
}

function compareOptionalValues(left: string | undefined, right: string | undefined): number {
  if (left === undefined && right === undefined) {
    return 0;
  }

  if (left === undefined) {
    return 1;
  }

  if (right === undefined) {
    return -1;
  }

  return left.localeCompare(right, undefined, { numeric: true, sensitivity: "base" });
}

function compareItemsByParameter(key: string | undefined) {
  return (left: ProvenanceItem, right: ProvenanceItem): number => {
    if (!key) {
      return left.name.localeCompare(right.name, undefined, { numeric: true, sensitivity: "base" });
    }

    const valueComparison = compareOptionalValues(getParameterValue(left, key), getParameterValue(right, key));
    return valueComparison !== 0
      ? valueComparison
      : left.name.localeCompare(right.name, undefined, { numeric: true, sensitivity: "base" });
  };
}

function sortGroups(groups: ProvenanceGroup[], sortKey: string | undefined): ProvenanceGroup[] {
  if (!sortKey) {
    return groups.map((group) => ({
      ...group,
      items: [...group.items].sort(compareItemsByParameter(undefined)),
    }));
  }

  return groups
    .map((group) => ({
      ...group,
      items: [...group.items].sort(compareItemsByParameter(sortKey)),
    }))
    .sort((left, right) => {
      const leftValue = left.items.length === 0 ? undefined : getParameterValue(left.items[0], sortKey);
      const rightValue = right.items.length === 0 ? undefined : getParameterValue(right.items[0], sortKey);
      const valueComparison = compareOptionalValues(leftValue, rightValue);
      return valueComparison !== 0
        ? valueComparison
        : left.label.localeCompare(right.label, undefined, { numeric: true, sensitivity: "base" });
    });
}

function sortOptionsForLayer(parameterKeys: string[], groupingKeys: string[]): string[] {
  return parameterKeys
    .filter((key) => !containsKey(groupingKeys, key))
    .sort((left, right) => left.localeCompare(right, undefined, { numeric: true, sensitivity: "base" }));
}

export function classifyGroupConnection(
  sourceGroup: ProvenanceGroup,
  targetGroup: ProvenanceGroup,
  connections: ProvenanceConnection[],
): ConnectionCoverage {
  if (sourceGroup.items.length === 0 || targetGroup.items.length === 0) {
    return "none";
  }

  const sourceIds = new Set(sourceGroup.items.map((item) => item.id));
  const targetIds = new Set(targetGroup.items.map((item) => item.id));
  const links = connections.filter(
    (connection) => sourceIds.has(connection.sourceId) && targetIds.has(connection.targetId),
  );

  if (links.length === 0) {
    return "none";
  }

  return "full";
}

function downstreamIds(connections: ProvenanceConnection[], startIds: string[]): Set<string> {
  const visited = new Set<string>();
  const queue = [...startIds];

  while (queue.length > 0) {
    const currentId = queue.shift();
    if (!currentId || visited.has(currentId)) {
      continue;
    }

    visited.add(currentId);
    connections
      .filter((connection) => connection.sourceId === currentId)
      .forEach((connection) => {
        if (!visited.has(connection.targetId)) {
          queue.push(connection.targetId);
        }
      });
  }

  return visited;
}

function hasParameter(item: ProvenanceItem, key: string): boolean {
  const normalized = normalizeKey(key);
  return item.parameters.some((parameter) => normalizeKey(parameter.key) === normalized);
}

function setParameter(item: ProvenanceItem, key: string, value: string): ProvenanceItem {
  let replaced = false;
  const normalized = normalizeKey(key);
  const parameters = item.parameters.map((parameter) => {
    if (normalizeKey(parameter.key) !== normalized) {
      return parameter;
    }

    replaced = true;
    return { ...parameter, value };
  });

  return {
    ...item,
    parameters: replaced ? parameters : [...parameters, { key, value }],
  };
}

export function updateParameterInGroup(
  items: ProvenanceItem[],
  connections: ProvenanceConnection[],
  group: ProvenanceGroup,
  key: string,
  value: string,
): ModelMutationResult<ProvenanceItem[]> {
  const trimmedKey = key.trim();
  const trimmedValue = value.trim();

  if (!trimmedKey || !trimmedValue) {
    return { ok: false, error: "Enter both a parameter name and value." };
  }

  if (!group.items.some((item) => hasParameter(item, trimmedKey))) {
    return { ok: false, error: `Cannot update "${trimmedKey}" because it is not present in the selected group.` };
  }

  const affectedIds = downstreamIds(connections, group.items.map((item) => item.id));

  return {
    ok: true,
    value: items.map((item) => (affectedIds.has(item.id) ? setParameter(item, trimmedKey, trimmedValue) : item)),
  };
}

export function connectGroups(
  connections: ProvenanceConnection[],
  sourceGroup: ProvenanceGroup,
  targetGroup: ProvenanceGroup,
): ProvenanceConnection[] {
  if (sourceGroup.items.length === 0 || targetGroup.items.length === 0) {
    return connections;
  }

  const nextConnections = [...connections];
  const existing = new Set(nextConnections.map(connectionKey));

  sourceGroup.items.forEach((source) => {
    targetGroup.items.forEach((target) => {
      const next = { sourceId: source.id, targetId: target.id };
      const key = connectionKey(next);

      if (!existing.has(key)) {
        existing.add(key);
        nextConnections.push(next);
      }
    });
  });

  return nextConnections;
}

function groupConnectionSummaries(
  sourceGroups: ProvenanceGroup[],
  targetGroups: ProvenanceGroup[],
  connections: ProvenanceConnection[],
): ConnectionSummary[] {
  const summaries: ConnectionSummary[] = [];

  sourceGroups.forEach((sourceGroup) => {
    targetGroups.forEach((targetGroup) => {
      const links = connectionDetailsForGroups(sourceGroup, targetGroup, connections);

      if (links.length > 0) {
        summaries.push({
          id: `${sourceGroup.id}-${targetGroup.id}`,
          sourceGroup,
          targetGroup,
          links,
        });
      }
    });
  });

  return summaries;
}

function connectionDetailsForGroups(
  sourceGroup: ProvenanceGroup,
  targetGroup: ProvenanceGroup,
  connections: ProvenanceConnection[],
): ProvenanceConnectionDetail[] {
  const sourceItems = new Map(sourceGroup.items.map((item) => [item.id, item]));
  const targetItems = new Map(targetGroup.items.map((item) => [item.id, item]));

  return connections
    .flatMap((connection) => {
      const source = sourceItems.get(connection.sourceId);
      const target = targetItems.get(connection.targetId);

      return source && target
        ? [
            {
              id: connectionKey(connection),
              source,
              target,
            },
          ]
        : [];
    })
    .sort((left, right) => {
      const sourceComparison = left.source.name.localeCompare(right.source.name);
      return sourceComparison !== 0 ? sourceComparison : left.target.name.localeCompare(right.target.name);
    })
    .filter((link, index, links) => links.findIndex((candidate) => candidate.id === link.id) === index);
}

function parameterKeysForGroup(group: ProvenanceGroup): string[] {
  const keys: string[] = [];
  const seen = new Set<string>();

  group.items.forEach((item) => {
    item.parameters.forEach((parameter) => {
      const normalized = normalizeKey(parameter.key);
      if (!seen.has(normalized)) {
        seen.add(normalized);
        keys.push(parameter.key);
      }
    });
  });

  return keys;
}

function findLayer(layers: ProvenanceLayer[], layerId: string): ProvenanceLayer {
  return layers.find((layer) => layer.id === layerId) ?? { id: layerId, label: layerId };
}

function adjacentPairs(layers: ProvenanceLayer[]): Array<[ProvenanceLayer, ProvenanceLayer]> {
  return layers.slice(0, -1).map((layer, index) => [layer, layers[index + 1]]);
}

function GroupEditor(props: {
  group: ProvenanceGroup;
  editor?: EditorState;
  side: ProvenanceSide;
  onStartEditor: (editor: EditorState) => void;
  onCancel: () => void;
  onSubmit: (side: ProvenanceSide, groupId: string, key: string, value: string) => void;
}) {
  const active = props.editor?.groupId === props.group.id && props.editor.side === props.side;
  const [keyDraft, setKeyDraft] = React.useState("");
  const [valueDraft, setValueDraft] = React.useState("");
  const groupKeys = parameterKeysForGroup(props.group);
  const defaultUpdateKey = groupKeys[0] ?? "";

  React.useEffect(() => {
    if (!active) {
      return;
    }

    setKeyDraft(defaultUpdateKey);
    setValueDraft("");
  }, [active, defaultUpdateKey]);

  if (!active) {
    if (groupKeys.length === 0) {
      return null;
    }

    return (
      <div className="swt:flex swt:flex-wrap swt:gap-2">
        <button
          className="swt:btn swt:btn-xs swt:btn-outline"
          data-testid="ProvenanceGrouping-group-update-param"
          type="button"
          onClick={(event) => {
            event.stopPropagation();
            props.onStartEditor({ side: props.side, groupId: props.group.id });
          }}
        >
          <i className="swt:iconify swt:fluent--edit-20-regular swt:size-3" />
          Update
        </button>
      </div>
    );
  }

  return (
    <form
      className="swt:flex swt:flex-col swt:gap-2 swt:rounded swt:border swt:border-base-content/10 swt:bg-base-200 swt:p-2"
      onSubmit={(event) => {
        event.preventDefault();
        props.onSubmit(props.side, props.group.id, keyDraft, valueDraft);
      }}
    >
      <select
        className="swt:select swt:select-xs swt:w-full"
        data-testid="ProvenanceGrouping-update-key-select"
        value={keyDraft}
        onChange={(event) => setKeyDraft(event.currentTarget.value)}
      >
        {groupKeys.map((key) => (
          <option key={key} value={key}>
            {key}
          </option>
        ))}
      </select>
      <input
        className="swt:input swt:input-xs swt:w-full"
        data-testid="ProvenanceGrouping-param-value-input"
        placeholder="Value"
        value={valueDraft}
        onChange={(event) => setValueDraft(event.currentTarget.value)}
      />
      <div className="swt:flex swt:justify-end swt:gap-2">
        <button className="swt:btn swt:btn-ghost swt:btn-xs" type="button" onClick={props.onCancel}>
          Cancel
        </button>
        <button className="swt:btn swt:btn-primary swt:btn-xs" data-testid="ProvenanceGrouping-param-submit" type="submit">
          Apply
        </button>
      </div>
    </form>
  );
}

function AddParameterControl(props: {
  side: ProvenanceSide;
  onCreateParameter: (side: ProvenanceSide, key: string) => void;
}) {
  const [keyDraft, setKeyDraft] = React.useState("");

  return (
    <form
      className="swt:flex swt:gap-2"
      onSubmit={(event) => {
        event.preventDefault();
        props.onCreateParameter(props.side, keyDraft);
        setKeyDraft("");
      }}
    >
      <input
        className="swt:input swt:input-xs swt:min-w-0 swt:flex-1"
        data-testid={`ProvenanceGrouping-create-parameter-${props.side}-input`}
        placeholder="New parameter"
        value={keyDraft}
        onChange={(event) => setKeyDraft(event.currentTarget.value)}
      />
      <button
        className="swt:btn swt:btn-xs swt:btn-outline swt:btn-square"
        data-testid={`ProvenanceGrouping-create-parameter-${props.side}-submit`}
        title="Create parameter"
        type="submit"
      >
        <i className="swt:iconify swt:fluent--add-20-regular swt:size-3" />
      </button>
    </form>
  );
}

function AddParameterValueControl(props: {
  side: ProvenanceSide;
  parameterKey: string;
  onCreateParameterValue: (side: ProvenanceSide, key: string, value: string) => void;
}) {
  const [valueDraft, setValueDraft] = React.useState("");

  return (
    <form
      className="swt:flex swt:gap-2"
      onSubmit={(event) => {
        event.preventDefault();
        props.onCreateParameterValue(props.side, props.parameterKey, valueDraft);
        setValueDraft("");
      }}
    >
      <input
        className="swt:input swt:input-xs swt:min-w-0 swt:flex-1"
        data-testid={`ProvenanceGrouping-create-value-${props.side}-${props.parameterKey}-input`}
        placeholder="New value"
        value={valueDraft}
        onChange={(event) => setValueDraft(event.currentTarget.value)}
      />
      <button
        className="swt:btn swt:btn-xs swt:btn-outline swt:btn-square"
        data-testid={`ProvenanceGrouping-create-value-${props.side}-${props.parameterKey}-submit`}
        title="Create value"
        type="submit"
      >
        <i className="swt:iconify swt:fluent--add-20-regular swt:size-3" />
      </button>
    </form>
  );
}

function ParameterRail(props: {
  side: ProvenanceSide;
  layer: ProvenanceLayer;
  items: ProvenanceItem[];
  groups: ProvenanceGroup[];
  entries: ParameterRailEntry[];
  parameterValues: Record<string, string[]>;
  onToggleGrouping: (side: ProvenanceSide, key: string) => void;
  onMoveParameter: (key: string, side: ProvenanceSide) => void;
  onCreateParameter: (side: ProvenanceSide, key: string) => void;
  onCreateParameterValue: (side: ProvenanceSide, key: string, value: string) => void;
}) {
  const [dragOver, setDragOver] = React.useState(false);
  const targetSide = props.side === "left" ? "right" : "left";

  const moveLabel = props.side === "left" ? "Move to output rail" : "Move to input rail";

  return (
    <aside
      className={[
        "swt:relative swt:z-10 swt:flex swt:min-w-0 swt:flex-col swt:gap-2 swt:rounded swt:border swt:border-dashed swt:p-2",
        dragOver ? "swt:border-primary swt:bg-primary/5" : "swt:border-transparent",
      ].join(" ")}
      onDragLeave={() => setDragOver(false)}
      onDragOver={(event) => {
        event.preventDefault();
        event.dataTransfer.dropEffect = "move";
        setDragOver(true);
      }}
      onDrop={(event) => {
        event.preventDefault();
        const key = event.dataTransfer.getData("application/x-provenance-parameter");
        setDragOver(false);

        if (key) {
          props.onMoveParameter(key, props.side);
        }
      }}
    >
      <div className="swt:text-xs swt:font-semibold swt:uppercase swt:tracking-normal swt:text-base-content/60">
        {props.layer.label} parameters
      </div>
      <AddParameterControl side={props.side} onCreateParameter={props.onCreateParameter} />
      <div className="swt:flex swt:flex-col swt:gap-2">
        {props.entries.length === 0 ? (
          <div className="swt:rounded swt:border swt:border-dashed swt:border-base-content/20 swt:p-3 swt:text-sm swt:text-base-content/60">
            No parameters
          </div>
        ) : (
          props.entries.map((entry) => {
            const values = valueConnectorsForKey(
              props.items,
              props.layer.id,
              props.groups,
              entry.key,
              props.parameterValues[entry.key] ?? [],
            );

            return (
              <div
                className={[
                  "swt:flex swt:flex-col swt:gap-2 swt:rounded swt:border swt:p-2",
                  entry.active ? "swt:border-primary swt:bg-primary/10" : "swt:border-base-content/10 swt:bg-base-100",
                ].join(" ")}
                data-testid={`ProvenanceGrouping-param-${props.side}-${entry.key}-block`}
                key={entry.key}
              >
                <div className="swt:flex swt:items-center swt:gap-2">
                  <button
                    className="swt:flex swt:min-w-0 swt:flex-1 swt:items-center swt:gap-2 swt:text-left swt:text-sm swt:font-medium"
                    data-testid={`ProvenanceGrouping-param-${props.side}-${entry.key}`}
                    draggable={entry.movable}
                    type="button"
                    onClick={() => props.onToggleGrouping(props.side, entry.key)}
                    onDragStart={(event) => {
                      if (!entry.movable) {
                        return;
                      }

                      event.dataTransfer.effectAllowed = "move";
                      event.dataTransfer.setData("application/x-provenance-parameter", entry.key);
                    }}
                  >
                    <i
                      className={[
                        "swt:iconify swt:size-4 swt:shrink-0",
                        entry.active ? "swt:fluent--group-list-24-filled" : "swt:fluent--group-list-24-regular",
                      ].join(" ")}
                    />
                    <span className="swt:truncate">{entry.key}</span>
                    <span className="swt:badge swt:badge-outline swt:badge-xs swt:shrink-0">
                      {entry.availability === "shared"
                        ? "shared"
                        : entry.availability === "left-only"
                          ? "input only"
                          : "output only"}
                    </span>
                  </button>
                  {entry.movable ? (
                    <button
                      aria-label={moveLabel}
                      className="swt:btn swt:btn-ghost swt:btn-xs swt:btn-square"
                      data-testid={`ProvenanceGrouping-param-${props.side}-${entry.key}-move`}
                      title={moveLabel}
                      type="button"
                      onClick={() => props.onMoveParameter(entry.key, targetSide)}
                    >
                      <i className="swt:iconify swt:fluent--arrow-swap-20-regular swt:size-4" />
                    </button>
                  ) : null}
                </div>
                <div
                  className="swt:flex swt:max-h-64 swt:flex-col swt:gap-1 swt:overflow-auto swt:rounded swt:bg-base-200 swt:p-2"
                  data-testid={`ProvenanceGrouping-param-${props.side}-${entry.key}-values`}
                >
                  <AddParameterValueControl
                    parameterKey={entry.key}
                    side={props.side}
                    onCreateParameterValue={props.onCreateParameterValue}
                  />
                  {values.map((value) => (
                    <div
                      className={[
                        "swt:flex swt:cursor-grab swt:items-center swt:gap-2 swt:rounded swt:bg-base-100 swt:px-2 swt:py-1 swt:text-xs swt:active:cursor-grabbing",
                        props.side === "right" ? "swt:justify-start" : "swt:justify-end",
                      ].join(" ")}
                      data-provenance-value-node={valueNodeId(props.side, props.layer.id, entry.key, value.value)}
                      data-testid={`ProvenanceGrouping-param-${props.side}-${entry.key}-value`}
                      draggable
                      key={`${entry.key}-${value.value}`}
                      onDragStart={(event) => {
                        event.dataTransfer.effectAllowed = "copy";
                        event.dataTransfer.setData(
                          "application/x-provenance-value",
                          JSON.stringify({ side: props.side, key: entry.key, value: value.value }),
                        );
                      }}
                    >
                      {props.side === "right" ? (
                        <span className="swt:h-px swt:w-8 swt:shrink-0 swt:bg-base-content/40" />
                      ) : null}
                      <span className="swt:badge swt:badge-outline swt:badge-sm swt:max-w-32 swt:truncate">
                        {value.value}
                      </span>
                      {props.side === "left" ? (
                        <span className="swt:h-px swt:w-8 swt:shrink-0 swt:bg-base-content/40" />
                      ) : null}
                    </div>
                  ))}
                </div>
              </div>
            );
          })
        )}
      </div>
    </aside>
  );
}

function CreateItemControl(props: {
  side: ProvenanceSide;
  layerId: string;
  onCreateItem: (layerId: string, name: string) => void;
}) {
  const [name, setName] = React.useState("");

  return (
    <form
      className="swt:flex swt:gap-2"
      onSubmit={(event) => {
        event.preventDefault();
        props.onCreateItem(props.layerId, name);
        setName("");
      }}
    >
      <input
        className="swt:input swt:input-sm swt:min-w-0 swt:flex-1"
        data-testid={`ProvenanceGrouping-create-item-${props.side}-input`}
        placeholder="New entry name"
        value={name}
        onChange={(event) => setName(event.currentTarget.value)}
      />
      <button
        className="swt:btn swt:btn-sm swt:btn-outline swt:btn-square"
        data-testid={`ProvenanceGrouping-create-item-${props.side}-submit`}
        title="Create entry"
        type="submit"
      >
        <i className="swt:iconify swt:fluent--add-20-regular swt:size-4" />
      </button>
    </form>
  );
}

function GroupSortControl(props: {
  side: ProvenanceSide;
  layerId: string;
  options: string[];
  value: string | undefined;
  onSortLayer: (layerId: string, key: string | undefined) => void;
}) {
  return (
    <label className="swt:flex swt:min-w-0 swt:items-center swt:gap-1 swt:text-xs swt:text-base-content/60">
      <i className="swt:iconify swt:fluent--arrow-sort-20-regular swt:size-4 swt:shrink-0" />
      <select
        className="swt:select swt:select-xs swt:w-36 swt:max-w-full"
        data-testid={`ProvenanceGrouping-sort-${props.side}`}
        value={props.value ?? ""}
        onChange={(event) => props.onSortLayer(props.layerId, event.currentTarget.value || undefined)}
      >
        <option value="">Default</option>
        {props.options.map((key) => (
          <option key={key} value={key}>
            {key}
          </option>
        ))}
      </select>
    </label>
  );
}

export function ProvenanceGrouping(props: ProvenanceGroupingProps): React.ReactElement {
  const [editor, setEditor] = React.useState<EditorState | undefined>();
  const [dragOverGroup, setDragOverGroup] = React.useState<{ side: ProvenanceSide; groupId: string } | undefined>();
  const mainRef = React.useRef<HTMLElement | null>(null);
  const [connectorOverlay, setConnectorOverlay] = React.useState<ConnectorOverlay>({
    width: 0,
    height: 0,
    valuePaths: [],
    groupPaths: [],
    itemPaths: [],
  });

  const leftLayer = findLayer(props.layers, props.leftLayerId);
  const rightLayer = findLayer(props.layers, props.rightLayerId);
  const leftGroupingKeys = props.groupingByLayer[props.leftLayerId] ?? [];
  const rightGroupingKeys = props.groupingByLayer[props.rightLayerId] ?? [];
  const leftParameterValues = parameterValueConfigForLayer(props.parameterValuesByLayer, props.leftLayerId);
  const rightParameterValues = parameterValueConfigForLayer(props.parameterValuesByLayer, props.rightLayerId);
  const leftParameterKeys = mergeKeys(availableParameterKeys(props.items, props.leftLayerId), configuredKeys(leftParameterValues));
  const rightParameterKeys = mergeKeys(availableParameterKeys(props.items, props.rightLayerId), configuredKeys(rightParameterValues));
  const leftSortOptions = sortOptionsForLayer(leftParameterKeys, leftGroupingKeys);
  const rightSortOptions = sortOptionsForLayer(rightParameterKeys, rightGroupingKeys);
  const leftSortKey = canonicalKey(leftSortOptions, props.sortingByLayer?.[props.leftLayerId]);
  const rightSortKey = canonicalKey(rightSortOptions, props.sortingByLayer?.[props.rightLayerId]);
  const leftGroups = sortGroups(buildGroups(props.items, props.leftLayerId, leftGroupingKeys), leftSortKey);
  const rightGroups = sortGroups(buildGroups(props.items, props.rightLayerId, rightGroupingKeys), rightSortKey);
  const parameterRails = buildParameterRailEntries(
    leftParameterKeys,
    rightParameterKeys,
    leftGroupingKeys,
    rightGroupingKeys,
    props.parameterRailByKey,
  );
  const summaries = groupConnectionSummaries(leftGroups, rightGroups, props.connections);
  const selectedConnectionSummary =
    props.detail?.kind === "connection"
      ? summaries.find(
          (summary) =>
            summary.sourceGroup.id === props.detail?.sourceGroupId &&
            summary.targetGroup.id === props.detail?.targetGroupId,
        )
      : undefined;
  const valueGroupConnections = [
    ...valueGroupConnectionsForRail("left", props.leftLayerId, parameterRails.left, props.items, leftGroups, leftParameterValues),
    ...valueGroupConnectionsForRail(
      "right",
      props.rightLayerId,
      parameterRails.right,
      props.items,
      rightGroups,
      rightParameterValues,
    ),
  ];

  React.useLayoutEffect(() => {
    const container = mainRef.current;

    if (!container) {
      return;
    }

    const updateConnectorOverlay = () => {
      const containerRect = container.getBoundingClientRect();
      const valuePaths = valueGroupConnections.flatMap((connection) => {
        const valueNode = container.querySelector<HTMLElement>(
          `[data-provenance-value-node="${connection.valueNodeId}"]`,
        );
        const groupNode = container.querySelector<HTMLElement>(
          `[data-provenance-group-node="${connection.groupNodeId}"]`,
        );

        if (!valueNode || !groupNode) {
          return [];
        }

        const valueRect = valueNode.getBoundingClientRect();
        const groupRect = groupNode.getBoundingClientRect();
        const start =
          connection.side === "left"
            ? {
                x: valueRect.right - containerRect.left + container.scrollLeft,
                y: valueRect.top - containerRect.top + container.scrollTop + valueRect.height / 2,
              }
            : {
                x: valueRect.left - containerRect.left + container.scrollLeft,
                y: valueRect.top - containerRect.top + container.scrollTop + valueRect.height / 2,
              };
        const end =
          connection.side === "left"
            ? {
                x: groupRect.left - containerRect.left + container.scrollLeft,
                y: groupRect.top - containerRect.top + container.scrollTop + groupRect.height / 2,
              }
            : {
                x: groupRect.right - containerRect.left + container.scrollLeft,
                y: groupRect.top - containerRect.top + container.scrollTop + groupRect.height / 2,
              };
        const bend = Math.max(48, Math.abs(end.x - start.x) / 2);
        const controlStartX = connection.side === "left" ? start.x + bend : start.x - bend;
        const controlEndX = connection.side === "left" ? end.x - bend : end.x + bend;

        return [
          {
            id: connection.id,
            side: connection.side,
            path: `M ${start.x} ${start.y} C ${controlStartX} ${start.y}, ${controlEndX} ${end.y}, ${end.x} ${end.y}`,
            start,
            end,
          },
        ];
      });
      const groupPaths = summaries.flatMap((summary) => {
        const sourceNode = container.querySelector<HTMLElement>(
          `[data-provenance-group-node="${groupNodeId("left", summary.sourceGroup.id)}"]`,
        );
        const targetNode = container.querySelector<HTMLElement>(
          `[data-provenance-group-node="${groupNodeId("right", summary.targetGroup.id)}"]`,
        );

        if (!sourceNode || !targetNode) {
          return [];
        }

        const sourceRect = sourceNode.getBoundingClientRect();
        const targetRect = targetNode.getBoundingClientRect();
        const start = {
          x: sourceRect.right - containerRect.left + container.scrollLeft,
          y: sourceRect.top - containerRect.top + container.scrollTop + sourceRect.height / 2,
        };
        const end = {
          x: targetRect.left - containerRect.left + container.scrollLeft,
          y: targetRect.top - containerRect.top + container.scrollTop + targetRect.height / 2,
        };
        const bend = Math.max(72, Math.abs(end.x - start.x) / 2);

        return [
          {
            id: summary.id,
            path: `M ${start.x} ${start.y} C ${start.x + bend} ${start.y}, ${end.x - bend} ${end.y}, ${end.x} ${end.y}`,
            start,
            end,
            sourceGroupId: summary.sourceGroup.id,
            targetGroupId: summary.targetGroup.id,
            sourceLabel: summary.sourceGroup.label,
            targetLabel: summary.targetGroup.label,
          },
        ];
      });
      const itemPaths = selectedConnectionSummary
        ? selectedConnectionSummary.links.flatMap((link) => {
            const sourceNode = container.querySelector<HTMLElement>(
              `[data-provenance-item-node="${itemNodeId("left", link.source.id)}"]`,
            );
            const targetNode = container.querySelector<HTMLElement>(
              `[data-provenance-item-node="${itemNodeId("right", link.target.id)}"]`,
            );

            if (!sourceNode || !targetNode) {
              return [];
            }

            const sourceRect = sourceNode.getBoundingClientRect();
            const targetRect = targetNode.getBoundingClientRect();
            const start = {
              x: sourceRect.right - containerRect.left + container.scrollLeft,
              y: sourceRect.top - containerRect.top + container.scrollTop + sourceRect.height / 2,
            };
            const end = {
              x: targetRect.left - containerRect.left + container.scrollLeft,
              y: targetRect.top - containerRect.top + container.scrollTop + targetRect.height / 2,
            };
            const bend = Math.max(64, Math.abs(end.x - start.x) / 2);

            return [
              {
                id: link.id,
                path: `M ${start.x} ${start.y} C ${start.x + bend} ${start.y}, ${end.x - bend} ${end.y}, ${end.x} ${end.y}`,
                sourceGroupId: selectedConnectionSummary.sourceGroup.id,
                targetGroupId: selectedConnectionSummary.targetGroup.id,
                sourceName: link.source.name,
                targetName: link.target.name,
              },
            ];
          })
        : [];
      const nextOverlay = {
        width: container.scrollWidth,
        height: container.scrollHeight,
        valuePaths,
        groupPaths,
        itemPaths,
      };

      setConnectorOverlay((current) =>
        JSON.stringify(current) === JSON.stringify(nextOverlay) ? current : nextOverlay,
      );
    };

    updateConnectorOverlay();
    container.addEventListener("scroll", updateConnectorOverlay, { passive: true });
    window.addEventListener("resize", updateConnectorOverlay);
    const resizeObserver =
      typeof ResizeObserver === "undefined" ? undefined : new ResizeObserver(updateConnectorOverlay);
    resizeObserver?.observe(container);
    Array.from(
      container.querySelectorAll("[data-provenance-value-node], [data-provenance-group-node], [data-provenance-item-node]"),
    ).forEach((element) => resizeObserver?.observe(element));

    return () => {
      container.removeEventListener("scroll", updateConnectorOverlay);
      window.removeEventListener("resize", updateConnectorOverlay);
      resizeObserver?.disconnect();
    };
  });

  const renderGroup = (side: ProvenanceSide, group: ProvenanceGroup) => {
    const selected =
      side === "left" ? props.selectedSourceGroupId === group.id : props.selectedTargetGroupId === group.id;
    const expandedByConnection =
      selectedConnectionSummary !== undefined &&
      (side === "left"
        ? selectedConnectionSummary.sourceGroup.id === group.id
        : selectedConnectionSummary.targetGroup.id === group.id);
    const expanded =
      expandedByConnection ||
      (props.detail?.kind === "group" && props.detail.side === side && props.detail.groupId === group.id);
    const dragTarget = dragOverGroup?.side === side && dragOverGroup.groupId === group.id;

    return (
      <article
        className={[
          "swt:flex swt:flex-col swt:gap-3 swt:rounded swt:border swt:bg-base-100 swt:p-3 swt:shadow-sm",
          selected ? "swt:border-primary swt:ring-2 swt:ring-primary/20" : "swt:border-base-content/10",
          dragTarget ? "swt:ring-2 swt:ring-accent/50" : "",
        ].join(" ")}
        data-provenance-group-node={groupNodeId(side, group.id)}
        data-testid={`ProvenanceGrouping-group-${side}-${group.id}`}
        key={group.id}
        onDragLeave={() => setDragOverGroup(undefined)}
        onDragOver={(event) => {
          const draggedValue = readDraggedParameterValue(event);

          if (draggedValue?.side !== side) {
            return;
          }

          event.preventDefault();
          event.dataTransfer.dropEffect = "copy";
          setDragOverGroup({ side, groupId: group.id });
        }}
        onDrop={(event) => {
          const draggedValue = readDraggedParameterValue(event);
          setDragOverGroup(undefined);

          if (draggedValue?.side !== side) {
            return;
          }

          event.preventDefault();
          props.onAssignParameterValue(side, draggedValue.key, draggedValue.value, group.id);
        }}
      >
        <div className="swt:flex swt:items-start swt:justify-between swt:gap-3">
          <div className="swt:min-w-0">
            <h3 className="swt:text-sm swt:font-semibold swt:leading-5">{group.label}</h3>
            <div className="swt:mt-1 swt:text-xs swt:text-base-content/60">
              {group.items.length} {group.items.length === 1 ? "entry" : "entries"}
            </div>
          </div>
          <div className="swt:flex swt:shrink-0 swt:gap-1">
            <button
              className={["swt:btn swt:btn-xs", selected ? "swt:btn-primary" : "swt:btn-outline"].join(" ")}
              data-testid="ProvenanceGrouping-group-select"
              type="button"
              onClick={() => props.onSelectGroup(side, group.id)}
            >
              <i className="swt:iconify swt:fluent--cursor-click-20-regular swt:size-3" />
              Select
            </button>
            <button
              aria-label={expanded ? `Hide ${group.label} entries` : `Show ${group.label} entries`}
              className="swt:btn swt:btn-ghost swt:btn-xs swt:btn-square"
              data-testid="ProvenanceGrouping-group-details"
              type="button"
              onClick={() => props.onOpenDetail({ kind: "group", side, groupId: group.id })}
            >
              <i
                className={[
                  "swt:iconify swt:size-4",
                  expanded ? "swt:fluent--chevron-up-20-regular" : "swt:fluent--chevron-down-20-regular",
                ].join(" ")}
              />
            </button>
          </div>
        </div>
        <GroupEditor
          editor={editor}
          group={group}
          side={side}
          onCancel={() => setEditor(undefined)}
          onStartEditor={setEditor}
          onSubmit={(submitSide, groupId, key, value) => {
            props.onUpdateParameter(submitSide, groupId, key, value);
            setEditor(undefined);
          }}
        />
        {expanded ? (
          <div
            className="swt:flex swt:max-h-64 swt:flex-col swt:gap-2 swt:overflow-auto swt:rounded swt:border swt:border-base-content/10 swt:bg-base-200 swt:p-2"
            data-testid="ProvenanceGrouping-group-inline-detail"
          >
            {group.items.map((item) => (
              <div
                className="swt:rounded swt:bg-base-100 swt:p-2 swt:text-xs"
                data-provenance-item-node={itemNodeId(side, item.id)}
                key={item.id}
              >
                <div className="swt:font-medium">{item.name}</div>
                <div className="swt:mt-1 swt:flex swt:flex-wrap swt:gap-1">
                  {item.parameters.length === 0 ? (
                    <span className="swt:text-base-content/60">No parameters</span>
                  ) : (
                    item.parameters.map((parameter) => (
                      <span className="swt:badge swt:badge-outline swt:badge-sm" key={`${item.id}-${parameter.key}`}>
                        {parameter.key}: {parameter.value}
                      </span>
                    ))
                  )}
                </div>
              </div>
            ))}
          </div>
        ) : null}
      </article>
    );
  };

  return (
    <div
      className="swt:flex swt:h-full swt:min-h-0 swt:flex-col swt:overflow-hidden swt:rounded swt:border swt:border-base-content/10 swt:bg-base-100"
      data-testid="ProvenanceGrouping-root"
    >
      <header className="swt:flex swt:flex-wrap swt:items-center swt:justify-between swt:gap-3 swt:border-b swt:border-base-content/10 swt:bg-base-100 swt:p-3">
        <div className="swt:min-w-0">
          <h2 className="swt:text-base swt:font-semibold">{leftLayer.label} -&gt; {rightLayer.label}</h2>
          <div className="swt:text-xs swt:text-base-content/60">
            Grouped provenance editor
          </div>
        </div>
        <div className="swt:flex swt:flex-wrap swt:items-center swt:gap-2">
          {adjacentPairs(props.layers).map(([left, right]) => {
            const active = left.id === props.leftLayerId && right.id === props.rightLayerId;
            return (
              <button
                className={["swt:btn swt:btn-sm", active ? "swt:btn-primary" : "swt:btn-outline"].join(" ")}
                key={`${left.id}-${right.id}`}
                type="button"
                onClick={() => props.onSelectPair(left.id, right.id)}
              >
                {left.label} -&gt; {right.label}
              </button>
            );
          })}
          <button
            className="swt:btn swt:btn-sm swt:btn-outline"
            data-testid="ProvenanceGrouping-add-layer"
            type="button"
            onClick={props.onAddLayer}
          >
            <i className="swt:iconify swt:fluent--add-square-multiple-20-regular swt:size-4" />
            Layer
          </button>
        </div>
      </header>

      {props.error ? (
        <div
          className="swt:flex swt:items-center swt:justify-between swt:gap-3 swt:border-b swt:border-error/20 swt:bg-error/10 swt:p-3 swt:text-sm swt:text-error"
          data-testid="ProvenanceGrouping-error"
        >
          <span>{props.error}</span>
          <button
            className="swt:btn swt:btn-ghost swt:btn-xs"
            data-testid="ProvenanceGrouping-error-dismiss"
            type="button"
            onClick={props.onDismissError}
          >
            Dismiss
          </button>
        </div>
      ) : null}

      <main
        className="swt:relative swt:grid swt:min-h-0 swt:flex-1 swt:grid-cols-1 swt:gap-4 swt:overflow-auto swt:p-3 swt:xl:grid-cols-[300px_112px_minmax(300px,1fr)_220px_minmax(300px,1fr)_112px_300px]"
        ref={mainRef}
      >
        <svg
          aria-hidden="true"
          className="swt:pointer-events-none swt:absolute swt:left-0 swt:top-0 swt:z-0"
          data-testid="ProvenanceGrouping-value-connectors"
          height={connectorOverlay.height}
          width={connectorOverlay.width}
        >
          {connectorOverlay.valuePaths.map((connector) => (
            <g key={connector.id}>
              <path
                d={connector.path}
                fill="none"
                stroke={connector.side === "left" ? "rgba(37, 99, 235, 0.42)" : "rgba(5, 150, 105, 0.42)"}
                strokeLinecap="round"
                strokeWidth="2"
              />
              <circle
                cx={connector.start.x}
                cy={connector.start.y}
                fill={connector.side === "left" ? "rgba(37, 99, 235, 0.65)" : "rgba(5, 150, 105, 0.65)"}
                r="3"
              />
              <circle
                cx={connector.end.x}
                cy={connector.end.y}
                fill={connector.side === "left" ? "rgba(37, 99, 235, 0.65)" : "rgba(5, 150, 105, 0.65)"}
                r="3"
              />
            </g>
          ))}
        </svg>
        <svg
          aria-label="Group connectors"
          className="swt:pointer-events-none swt:absolute swt:left-0 swt:top-0 swt:z-20"
          data-testid="ProvenanceGrouping-group-connectors"
          height={connectorOverlay.height}
          width={connectorOverlay.width}
        >
          {connectorOverlay.groupPaths.map((connector) => {
            const selected =
              props.detail?.kind === "connection" &&
              props.detail.sourceGroupId === connector.sourceGroupId &&
              props.detail.targetGroupId === connector.targetGroupId;

            if (selected) {
              return null;
            }

            return (
              <g
                aria-label={`${connector.sourceLabel} to ${connector.targetLabel}`}
                className="swt:pointer-events-auto swt:cursor-pointer"
                data-testid="ProvenanceGrouping-group-connector"
                key={connector.id}
                role="button"
                tabIndex={0}
                onClick={() =>
                  props.onOpenDetail({
                    kind: "connection",
                    sourceGroupId: connector.sourceGroupId,
                    targetGroupId: connector.targetGroupId,
                  })
                }
                onKeyDown={(event) => {
                  if (event.key !== "Enter" && event.key !== " ") {
                    return;
                  }

                  event.preventDefault();
                  props.onOpenDetail({
                    kind: "connection",
                    sourceGroupId: connector.sourceGroupId,
                    targetGroupId: connector.targetGroupId,
                  });
                }}
              >
                <path
                  d={connector.path}
                  fill="none"
                  stroke="transparent"
                  strokeLinecap="round"
                  strokeWidth="8"
                />
                <path
                  d={connector.path}
                  fill="none"
                  stroke={selected ? "rgba(79, 70, 229, 0.95)" : "rgba(15, 23, 42, 0.46)"}
                  strokeLinecap="round"
                  strokeWidth={selected ? "4" : "3"}
                />
                <circle
                  cx={connector.start.x}
                  cy={connector.start.y}
                  fill={selected ? "rgba(79, 70, 229, 0.95)" : "rgba(15, 23, 42, 0.68)"}
                  r={selected ? "4" : "3"}
                />
                <circle
                  cx={connector.end.x}
                  cy={connector.end.y}
                  fill={selected ? "rgba(79, 70, 229, 0.95)" : "rgba(15, 23, 42, 0.68)"}
                  r={selected ? "4" : "3"}
                />
              </g>
            );
          })}
          {connectorOverlay.itemPaths.map((connector) => (
            <g
              aria-label={`${connector.sourceName} to ${connector.targetName}`}
              className="swt:pointer-events-auto swt:cursor-pointer"
              data-testid="ProvenanceGrouping-individual-connector"
              key={connector.id}
              role="button"
              tabIndex={0}
              onClick={() =>
                props.onOpenDetail({
                  kind: "connection",
                  sourceGroupId: connector.sourceGroupId,
                  targetGroupId: connector.targetGroupId,
                })
              }
              onKeyDown={(event) => {
                if (event.key !== "Enter" && event.key !== " ") {
                  return;
                }

                event.preventDefault();
                props.onOpenDetail({
                  kind: "connection",
                  sourceGroupId: connector.sourceGroupId,
                  targetGroupId: connector.targetGroupId,
                });
              }}
            >
              <path
                d={connector.path}
                fill="none"
                stroke="transparent"
                strokeLinecap="round"
                strokeWidth="7"
              />
              <path
                d={connector.path}
                fill="none"
                stroke="rgba(79, 70, 229, 0.86)"
                strokeLinecap="round"
                strokeWidth="2.5"
              />
            </g>
          ))}
        </svg>
        <ParameterRail
          entries={parameterRails.left}
          groups={leftGroups}
          items={props.items}
          layer={leftLayer}
          parameterValues={leftParameterValues}
          side="left"
          onCreateParameter={props.onCreateParameter}
          onCreateParameterValue={props.onCreateParameterValue}
          onMoveParameter={props.onMoveParameter}
          onToggleGrouping={props.onToggleGrouping}
        />

        <div className="swt:hidden swt:min-w-0 swt:xl:block" data-testid="ProvenanceGrouping-value-lane-left" />

        <section className="swt:relative swt:z-10 swt:flex swt:min-w-0 swt:flex-col swt:gap-3">
          <div className="swt:flex swt:flex-wrap swt:items-center swt:justify-between swt:gap-2">
            <div className="swt:flex swt:items-center swt:gap-2">
              <h3 className="swt:text-sm swt:font-semibold">{leftLayer.label} groups</h3>
              <span className="swt:badge swt:badge-outline swt:badge-sm">{leftGroups.length}</span>
            </div>
            <GroupSortControl
              layerId={props.leftLayerId}
              options={leftSortOptions}
              side="left"
              value={leftSortKey}
              onSortLayer={props.onSortLayer}
            />
          </div>
          <CreateItemControl layerId={props.leftLayerId} side="left" onCreateItem={props.onCreateItem} />
          <div className="swt:flex swt:flex-col swt:gap-3">
            {leftGroups.length === 0 ? (
              <div className="swt:rounded swt:border swt:border-dashed swt:border-base-content/20 swt:p-4 swt:text-sm swt:text-base-content/60">
                No entries in this layer
              </div>
            ) : (
              leftGroups.map((group) => renderGroup("left", group))
            )}
          </div>
        </section>

        <section className="swt:pointer-events-none swt:relative swt:z-30 swt:flex swt:min-w-0 swt:flex-col swt:items-center swt:self-stretch">
          <button
            className="swt:btn swt:btn-primary swt:btn-sm swt:pointer-events-auto"
            data-testid="ProvenanceGrouping-connect-selected"
            type="button"
            onClick={props.onConnectSelectedGroups}
          >
            <i className="swt:iconify swt:fluent--plug-connected-20-regular swt:size-4" />
            Connect selected
          </button>
        </section>

        <section className="swt:relative swt:z-10 swt:flex swt:min-w-0 swt:flex-col swt:gap-3">
          <div className="swt:flex swt:flex-wrap swt:items-center swt:justify-between swt:gap-2">
            <div className="swt:flex swt:items-center swt:gap-2">
              <h3 className="swt:text-sm swt:font-semibold">{rightLayer.label} groups</h3>
              <span className="swt:badge swt:badge-outline swt:badge-sm">{rightGroups.length}</span>
            </div>
            <GroupSortControl
              layerId={props.rightLayerId}
              options={rightSortOptions}
              side="right"
              value={rightSortKey}
              onSortLayer={props.onSortLayer}
            />
          </div>
          <CreateItemControl layerId={props.rightLayerId} side="right" onCreateItem={props.onCreateItem} />
          <div className="swt:flex swt:flex-col swt:gap-3">
            {rightGroups.length === 0 ? (
              <div className="swt:rounded swt:border swt:border-dashed swt:border-base-content/20 swt:p-4 swt:text-sm swt:text-base-content/60">
                No entries in this layer
              </div>
            ) : (
              rightGroups.map((group) => renderGroup("right", group))
            )}
          </div>
        </section>

        <div className="swt:hidden swt:min-w-0 swt:xl:block" data-testid="ProvenanceGrouping-value-lane-right" />

        <ParameterRail
          entries={parameterRails.right}
          groups={rightGroups}
          items={props.items}
          layer={rightLayer}
          parameterValues={rightParameterValues}
          side="right"
          onCreateParameter={props.onCreateParameter}
          onCreateParameterValue={props.onCreateParameterValue}
          onMoveParameter={props.onMoveParameter}
          onToggleGrouping={props.onToggleGrouping}
        />
      </main>

    </div>
  );
}

export default ProvenanceGrouping;
