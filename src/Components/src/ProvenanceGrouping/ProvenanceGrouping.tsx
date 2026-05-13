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

export type ConnectionCoverage = "none" | "partial" | "full";

export type ModelMutationResult<T> =
  | { ok: true; value: T }
  | { ok: false; error: string };

export type ProvenanceSide = "left" | "right";

export type ProvenanceDetail =
  | { kind: "group"; side: ProvenanceSide; groupId: string }
  | { kind: "parameter"; layerId: string; key: string };

export type ProvenanceGroupingProps = {
  layers: ProvenanceLayer[];
  items: ProvenanceItem[];
  connections: ProvenanceConnection[];
  leftLayerId: string;
  rightLayerId: string;
  groupingByLayer: Record<string, string[]>;
  selectedSourceGroupId?: string;
  selectedTargetGroupId?: string;
  detail?: ProvenanceDetail;
  error?: string;
  onToggleGrouping: (layerId: string, key: string) => void;
  onSelectGroup: (side: ProvenanceSide, groupId: string) => void;
  onOpenDetail: (detail: ProvenanceDetail) => void;
  onAddParameter: (side: ProvenanceSide, groupId: string, key: string, value: string) => void;
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
  coverage: ConnectionCoverage;
};

type EditorState = {
  side: ProvenanceSide;
  groupId: string;
  mode: "add" | "update";
};

const missingValue = (key: string) => `Missing ${key}`;

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
    return [
      {
        id: `${layerId}-all-items`,
        layerId,
        label: "All items",
        groupingKeys: [],
        labelParts: [],
        items: layerItems,
      },
    ];
  }

  const groups = new Map<string, ProvenanceGroup>();

  layerItems.forEach((item) => {
    const labelParts = groupingKeys.map((key) => ({
      key,
      value: getParameterValue(item, key) ?? missingValue(key),
    }));
    const id = `${layerId}-${labelParts.map((part) => `${slug(part.key)}-${slug(part.value)}`).join("-")}`;
    const existing = groups.get(id);

    if (existing) {
      existing.items.push(item);
    } else {
      groups.set(id, {
        id,
        layerId,
        label: labelParts.map((part) => `${part.key}: ${part.value}`).join(", "),
        groupingKeys,
        labelParts,
        items: [item],
      });
    }
  });

  return Array.from(groups.values()).sort((left, right) => left.label.localeCompare(right.label));
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

  const coveredSources = new Set(links.map((connection) => connection.sourceId));
  const coveredTargets = new Set(links.map((connection) => connection.targetId));

  return coveredSources.size === sourceIds.size && coveredTargets.size === targetIds.size
    ? "full"
    : "partial";
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

export function addParameterToGroup(
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

  const affectedIds = downstreamIds(connections, group.items.map((item) => item.id));
  const duplicate = items.find((item) => affectedIds.has(item.id) && hasParameter(item, trimmedKey));

  if (duplicate) {
    return {
      ok: false,
      error: `Choose another parameter name. "${trimmedKey}" already exists in the affected group or downstream chain.`,
    };
  }

  return {
    ok: true,
    value: items.map((item) => (affectedIds.has(item.id) ? setParameter(item, trimmedKey, trimmedValue) : item)),
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
  const count = Math.max(sourceGroup.items.length, targetGroup.items.length);

  for (let index = 0; index < count; index += 1) {
    const source = sourceGroup.items[index % sourceGroup.items.length];
    const target = targetGroup.items[index % targetGroup.items.length];
    const next = { sourceId: source.id, targetId: target.id };
    const key = connectionKey(next);

    if (!existing.has(key)) {
      existing.add(key);
      nextConnections.push(next);
    }
  }

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
      const coverage = classifyGroupConnection(sourceGroup, targetGroup, connections);
      if (coverage !== "none") {
        summaries.push({
          id: `${sourceGroup.id}-${targetGroup.id}`,
          sourceGroup,
          targetGroup,
          coverage,
        });
      }
    });
  });

  return summaries;
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
  onSubmit: (side: ProvenanceSide, groupId: string, key: string, value: string, mode: "add" | "update") => void;
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

    if (props.editor?.mode === "update") {
      setKeyDraft(defaultUpdateKey);
      setValueDraft("");
    } else {
      setKeyDraft("");
      setValueDraft("");
    }
  }, [active, defaultUpdateKey, props.editor?.mode]);

  if (!active) {
    return (
      <div className="swt:flex swt:flex-wrap swt:gap-2">
        <button
          className="swt:btn swt:btn-xs swt:btn-outline"
          data-testid="ProvenanceGrouping-group-add-param"
          type="button"
          onClick={(event) => {
            event.stopPropagation();
            props.onStartEditor({ side: props.side, groupId: props.group.id, mode: "add" });
          }}
        >
          <i className="swt:iconify swt:fluent--add-20-regular swt:size-3" />
          Add
        </button>
        <button
          className="swt:btn swt:btn-xs swt:btn-outline"
          data-testid="ProvenanceGrouping-group-update-param"
          type="button"
          onClick={(event) => {
            event.stopPropagation();
            props.onStartEditor({ side: props.side, groupId: props.group.id, mode: "update" });
          }}
        >
          <i className="swt:iconify swt:fluent--edit-20-regular swt:size-3" />
          Update
        </button>
      </div>
    );
  }

  const isUpdate = props.editor?.mode === "update";

  return (
    <form
      className="swt:flex swt:flex-col swt:gap-2 swt:rounded swt:border swt:border-base-content/10 swt:bg-base-200 swt:p-2"
      onSubmit={(event) => {
        event.preventDefault();
        props.onSubmit(props.side, props.group.id, keyDraft, valueDraft, isUpdate ? "update" : "add");
      }}
    >
      {isUpdate ? (
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
      ) : (
        <input
          className="swt:input swt:input-xs swt:w-full"
          data-testid="ProvenanceGrouping-param-key-input"
          placeholder="Parameter"
          value={keyDraft}
          onChange={(event) => setKeyDraft(event.currentTarget.value)}
        />
      )}
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

function ParameterRail(props: {
  side: ProvenanceSide;
  layer: ProvenanceLayer;
  items: ProvenanceItem[];
  keys: string[];
  activeKeys: string[];
  detail?: ProvenanceDetail;
  onToggleGrouping: (layerId: string, key: string) => void;
  onOpenDetail: (detail: ProvenanceDetail) => void;
}) {
  return (
    <aside className="swt:flex swt:min-w-0 swt:flex-col swt:gap-2">
      <div className="swt:text-xs swt:font-semibold swt:uppercase swt:tracking-normal swt:text-base-content/60">
        {props.layer.label} parameters
      </div>
      <div className="swt:flex swt:flex-col swt:gap-2">
        {props.keys.length === 0 ? (
          <div className="swt:rounded swt:border swt:border-dashed swt:border-base-content/20 swt:p-3 swt:text-sm swt:text-base-content/60">
            No parameters
          </div>
        ) : (
          props.keys.map((key) => {
            const active = props.activeKeys.includes(key);
            const expanded =
              props.detail?.kind === "parameter" &&
              props.detail.layerId === props.layer.id &&
              props.detail.key === key;
            const layerItems = props.items.filter((item) => item.layerId === props.layer.id);

            return (
              <div
                className={[
                  "swt:flex swt:flex-col swt:gap-2 swt:rounded swt:border swt:p-2",
                  active ? "swt:border-primary swt:bg-primary/10" : "swt:border-base-content/10 swt:bg-base-100",
                ].join(" ")}
                data-testid={`ProvenanceGrouping-param-${props.side}-${key}-block`}
                key={key}
              >
                <div className="swt:flex swt:items-center swt:gap-2">
                  <button
                    className="swt:flex swt:min-w-0 swt:flex-1 swt:items-center swt:gap-2 swt:text-left swt:text-sm swt:font-medium"
                    data-testid={`ProvenanceGrouping-param-${props.side}-${key}`}
                    type="button"
                    onClick={() => props.onToggleGrouping(props.layer.id, key)}
                  >
                    <i
                      className={[
                        "swt:iconify swt:size-4 swt:shrink-0",
                        active ? "swt:fluent--group-list-24-filled" : "swt:fluent--group-list-24-regular",
                      ].join(" ")}
                    />
                    <span className="swt:truncate">{key}</span>
                  </button>
                  <button
                    aria-label={expanded ? `Hide ${key} values` : `Show ${key} values`}
                    className="swt:btn swt:btn-ghost swt:btn-xs swt:btn-square"
                    data-testid={`ProvenanceGrouping-param-${props.side}-${key}-details`}
                    type="button"
                    onClick={() => props.onOpenDetail({ kind: "parameter", layerId: props.layer.id, key })}
                  >
                    <i
                      className={[
                        "swt:iconify swt:size-4",
                        expanded
                          ? "swt:fluent--chevron-up-20-regular"
                          : "swt:fluent--chevron-down-20-regular",
                      ].join(" ")}
                    />
                  </button>
                </div>
                {expanded ? (
                  <div
                    className="swt:flex swt:max-h-56 swt:flex-col swt:gap-1 swt:overflow-auto swt:rounded swt:bg-base-200 swt:p-2"
                    data-testid={`ProvenanceGrouping-param-${props.side}-${key}-inline-detail`}
                  >
                    {layerItems.map((item) => (
                      <div
                        className="swt:grid swt:grid-cols-[minmax(0,1fr)_auto] swt:gap-2 swt:rounded swt:bg-base-100 swt:px-2 swt:py-1 swt:text-xs"
                        key={`${item.id}-${key}`}
                      >
                        <span className="swt:truncate" title={item.name}>
                          {item.name}
                        </span>
                        <span className="swt:font-medium swt:text-base-content/70">
                          {getParameterValue(item, key) ?? missingValue(key)}
                        </span>
                      </div>
                    ))}
                  </div>
                ) : null}
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

export function ProvenanceGrouping(props: ProvenanceGroupingProps): React.ReactElement {
  const [editor, setEditor] = React.useState<EditorState | undefined>();

  const leftLayer = findLayer(props.layers, props.leftLayerId);
  const rightLayer = findLayer(props.layers, props.rightLayerId);
  const leftGroupingKeys = props.groupingByLayer[props.leftLayerId] ?? [];
  const rightGroupingKeys = props.groupingByLayer[props.rightLayerId] ?? [];
  const leftGroups = buildGroups(props.items, props.leftLayerId, leftGroupingKeys);
  const rightGroups = buildGroups(props.items, props.rightLayerId, rightGroupingKeys);
  const leftParameterKeys = availableParameterKeys(props.items, props.leftLayerId);
  const rightParameterKeys = availableParameterKeys(props.items, props.rightLayerId);
  const summaries = groupConnectionSummaries(leftGroups, rightGroups, props.connections);

  const renderGroup = (side: ProvenanceSide, group: ProvenanceGroup) => {
    const selected =
      side === "left" ? props.selectedSourceGroupId === group.id : props.selectedTargetGroupId === group.id;
    const expanded =
      props.detail?.kind === "group" && props.detail.side === side && props.detail.groupId === group.id;

    return (
      <article
        className={[
          "swt:flex swt:flex-col swt:gap-3 swt:rounded swt:border swt:bg-base-100 swt:p-3 swt:shadow-sm",
          selected ? "swt:border-primary swt:ring-2 swt:ring-primary/20" : "swt:border-base-content/10",
        ].join(" ")}
        data-testid={`ProvenanceGrouping-group-${side}-${group.id}`}
        key={group.id}
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
          onSubmit={(submitSide, groupId, key, value, mode) => {
            if (mode === "add") {
              props.onAddParameter(submitSide, groupId, key, value);
            } else {
              props.onUpdateParameter(submitSide, groupId, key, value);
            }
            setEditor(undefined);
          }}
        />
        {expanded ? (
          <div
            className="swt:flex swt:max-h-64 swt:flex-col swt:gap-2 swt:overflow-auto swt:rounded swt:border swt:border-base-content/10 swt:bg-base-200 swt:p-2"
            data-testid="ProvenanceGrouping-group-inline-detail"
          >
            {group.items.map((item) => (
              <div className="swt:rounded swt:bg-base-100 swt:p-2 swt:text-xs" key={item.id}>
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

      <main className="swt:grid swt:min-h-0 swt:flex-1 swt:grid-cols-1 swt:gap-3 swt:overflow-auto swt:p-3 swt:xl:grid-cols-[220px_minmax(260px,1fr)_220px_minmax(260px,1fr)_220px]">
        <ParameterRail
          activeKeys={leftGroupingKeys}
          detail={props.detail}
          items={props.items}
          keys={leftParameterKeys}
          layer={leftLayer}
          side="left"
          onOpenDetail={props.onOpenDetail}
          onToggleGrouping={props.onToggleGrouping}
        />

        <section className="swt:flex swt:min-w-0 swt:flex-col swt:gap-3">
          <div className="swt:flex swt:items-center swt:justify-between swt:gap-2">
            <h3 className="swt:text-sm swt:font-semibold">{leftLayer.label} groups</h3>
            <span className="swt:badge swt:badge-outline swt:badge-sm">{leftGroups.length}</span>
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

        <section className="swt:flex swt:min-w-0 swt:flex-col swt:gap-3 swt:self-stretch">
          <button
            className="swt:btn swt:btn-primary swt:btn-sm"
            data-testid="ProvenanceGrouping-connect-selected"
            type="button"
            onClick={props.onConnectSelectedGroups}
          >
            <i className="swt:iconify swt:fluent--plug-connected-20-regular swt:size-4" />
            Connect selected
          </button>
          <div className="swt:flex swt:min-h-0 swt:flex-1 swt:flex-col swt:gap-2 swt:rounded swt:border swt:border-base-content/10 swt:bg-base-200 swt:p-3">
            <div className="swt:text-xs swt:font-semibold swt:uppercase swt:tracking-normal swt:text-base-content/60">
              Group links
            </div>
            {summaries.length === 0 ? (
              <div className="swt:flex swt:flex-1 swt:items-center swt:justify-center swt:rounded swt:border swt:border-dashed swt:border-base-content/20 swt:p-4 swt:text-center swt:text-sm swt:text-base-content/60">
                No group-level connections
              </div>
            ) : (
              <div className="swt:flex swt:flex-col swt:gap-2">
                {summaries.map((summary) => (
                  <div
                    className={[
                      "swt:rounded swt:border swt:bg-base-100 swt:p-2 swt:text-xs",
                      summary.coverage === "full" ? "swt:border-success/40" : "swt:border-warning/60",
                    ].join(" ")}
                    data-testid={
                      summary.coverage === "full"
                        ? "ProvenanceGrouping-connection-full"
                        : "ProvenanceGrouping-connection-partial"
                    }
                    key={summary.id}
                  >
                    <div className="swt:flex swt:items-center swt:gap-2">
                      <span className="swt:h-px swt:flex-1 swt:bg-base-content/30" />
                      <span
                        className={[
                          "swt:badge swt:badge-sm",
                          summary.coverage === "full" ? "swt:badge-success" : "swt:badge-warning",
                        ].join(" ")}
                      >
                        {summary.coverage === "full" ? "full coverage" : "partial"}
                      </span>
                      <span className="swt:h-px swt:flex-1 swt:bg-base-content/30" />
                    </div>
                    <div className="swt:mt-2 swt:grid swt:grid-cols-[1fr_auto_1fr] swt:items-center swt:gap-2">
                      <span className="swt:truncate" title={summary.sourceGroup.label}>
                        {summary.sourceGroup.label}
                      </span>
                      <i className="swt:iconify swt:fluent--arrow-right-20-regular swt:size-4" />
                      <span className="swt:truncate swt:text-right" title={summary.targetGroup.label}>
                        {summary.targetGroup.label}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </section>

        <section className="swt:flex swt:min-w-0 swt:flex-col swt:gap-3">
          <div className="swt:flex swt:items-center swt:justify-between swt:gap-2">
            <h3 className="swt:text-sm swt:font-semibold">{rightLayer.label} groups</h3>
            <span className="swt:badge swt:badge-outline swt:badge-sm">{rightGroups.length}</span>
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

        <ParameterRail
          activeKeys={rightGroupingKeys}
          detail={props.detail}
          items={props.items}
          keys={rightParameterKeys}
          layer={rightLayer}
          side="right"
          onOpenDetail={props.onOpenDetail}
          onToggleGrouping={props.onToggleGrouping}
        />
      </main>

    </div>
  );
}

export default ProvenanceGrouping;
