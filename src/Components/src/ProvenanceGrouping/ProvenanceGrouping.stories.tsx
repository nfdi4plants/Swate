import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, fireEvent, userEvent, waitFor, within } from "storybook/test";
import {
  ProvenanceGrouping,
  addParameterValue,
  buildGroups,
  connectGroups,
  createAdjacentValueResolver,
  updateParameterInGroup,
  type ProvenanceConnection,
  type ProvenanceDetail,
  type ProvenanceGroup,
  type ProvenanceItem,
  type ProvenanceLayer,
  type ProvenanceLayerPair,
} from "./ProvenanceGrouping";

type Side = "left" | "right";

type MockModel = {
  layers: ProvenanceLayer[];
  items: ProvenanceItem[];
  connections: ProvenanceConnection[];
  layerPairs: ProvenanceLayerPair[];
  visibleItemIdsByPair: Record<string, Record<string, string[]>>;
  leftLayerId: string;
  rightLayerId: string;
  groupingByLayer: Record<string, string[]>;
  sortingByLayer: Record<string, string | undefined>;
  parameterValuesByLayer: Record<string, Record<string, string[]>>;
  parameterRailByKey: Record<string, Side>;
  selectedSourceGroupIds: string[];
  selectedTargetGroupIds: string[];
  detail?: ProvenanceDetail;
  error?: string;
};

const initialLayers: ProvenanceLayer[] = [
  { id: "inputs", label: "Inputs" },
  { id: "outputs", label: "Outputs" },
];

const initialItems: ProvenanceItem[] = [
  {
    id: "input-a",
    name: "Input A",
    layerId: "inputs",
    parameters: [
      { key: "Species", value: "Arabidopsis" },
      { key: "Temperature", value: "12 C" },
      { key: "Replicate", value: "1" },
    ],
  },
  {
    id: "input-b",
    name: "Input B",
    layerId: "inputs",
    parameters: [
      { key: "Species", value: "Arabidopsis" },
      { key: "Temperature", value: "12 C" },
      { key: "Replicate", value: "2" },
    ],
  },
  {
    id: "input-c",
    name: "Input C",
    layerId: "inputs",
    parameters: [
      { key: "Species", value: "Arabidopsis" },
      { key: "Temperature", value: "24 C" },
      { key: "Replicate", value: "1" },
    ],
  },
  {
    id: "input-d",
    name: "Input D",
    layerId: "inputs",
    parameters: [
      { key: "Species", value: "Chlamydomonas" },
      { key: "Temperature", value: "24 C" },
      { key: "Replicate", value: "1" },
    ],
  },
  {
    id: "output-a",
    name: "Output A",
    layerId: "outputs",
    parameters: [
      { key: "Species", value: "Arabidopsis" },
      { key: "Temperature", value: "12 C" },
      { key: "Replicate", value: "1" },
      { key: "Analysis", value: "Mass Spectrometry" },
    ],
  },
  {
    id: "output-b",
    name: "Output B",
    layerId: "outputs",
    parameters: [
      { key: "Species", value: "Arabidopsis" },
      { key: "Temperature", value: "12 C" },
      { key: "Replicate", value: "1" },
      { key: "Replicate", value: "2" },
      { key: "Analysis", value: "Mass Spectrometry" },
    ],
  },
  {
    id: "output-c",
    name: "Output C",
    layerId: "outputs",
    parameters: [
      { key: "Species", value: "Arabidopsis" },
      { key: "Temperature", value: "24 C" },
      { key: "Replicate", value: "1" },
      { key: "Analysis", value: "LC-MS" },
    ],
  },
  {
    id: "output-d",
    name: "Output D",
    layerId: "outputs",
    parameters: [
      { key: "Species", value: "Chlamydomonas" },
      { key: "Temperature", value: "24 C" },
      { key: "Replicate", value: "1" },
      { key: "Analysis", value: "Proteomics" },
    ],
  },
  {
    id: "output-e",
    name: "Output E",
    layerId: "outputs",
    parameters: [{ key: "Analysis", value: "Imaging" }],
  },
];

const initialConnections: ProvenanceConnection[] = [
  { sourceId: "input-a", targetId: "output-a" },
  { sourceId: "input-a", targetId: "output-b" },
  { sourceId: "input-b", targetId: "output-b" },
  { sourceId: "input-c", targetId: "output-c" },
  { sourceId: "input-d", targetId: "output-d" },
];

function initialModel(): MockModel {
  return {
    layers: initialLayers,
    items: initialItems,
    connections: initialConnections,
    layerPairs: [{ leftLayerId: "inputs", rightLayerId: "outputs" }],
    visibleItemIdsByPair: {},
    leftLayerId: "inputs",
    rightLayerId: "outputs",
    groupingByLayer: {
      inputs: [],
      outputs: [],
    },
    sortingByLayer: {},
    parameterValuesByLayer: {
      inputs: {},
      outputs: {},
    },
    parameterRailByKey: {},
    selectedSourceGroupIds: [],
    selectedTargetGroupIds: [],
  };
}

function normalizedKey(key: string): string {
  return key.trim().toLowerCase();
}

function hasGroupingKey(keys: string[], key: string): boolean {
  const normalized = normalizedKey(key);
  return keys.some((existingKey) => normalizedKey(existingKey) === normalized);
}

function addGroupingKey(keys: string[], key: string): string[] {
  return hasGroupingKey(keys, key) ? keys : [...keys, key];
}

function removeGroupingKey(keys: string[], key: string): string[] {
  const normalized = normalizedKey(key);
  return keys.filter((existingKey) => normalizedKey(existingKey) !== normalized);
}

function pairKey(leftLayerId: string, rightLayerId: string): string {
  return `${leftLayerId}->${rightLayerId}`;
}

function currentPairKey(model: MockModel): string {
  return pairKey(model.leftLayerId, model.rightLayerId);
}

function currentVisibleItemIdsByLayer(model: MockModel): Record<string, string[]> | undefined {
  return model.visibleItemIdsByPair[currentPairKey(model)];
}

function visibleItemsForCurrentPair(model: MockModel): ProvenanceItem[] {
  const visibleItemIdsByLayer = currentVisibleItemIdsByLayer(model);

  if (!visibleItemIdsByLayer) {
    return model.items;
  }

  return model.items.filter((item) => {
    const visibleIds = visibleItemIdsByLayer[item.layerId];
    return visibleIds === undefined || visibleIds.includes(item.id);
  });
}

function findGroup(
  items: ProvenanceItem[],
  connections: ProvenanceConnection[],
  layerId: string,
  connectedLayerId: string,
  side: Side,
  groupingKeys: string[],
  groupId: string,
): ProvenanceGroup | undefined {
  return buildGroups(
    items,
    layerId,
    groupingKeys,
    createAdjacentValueResolver(items, connections, layerId, connectedLayerId, side),
  ).find((group) => group.id === groupId);
}

function findVisibleGroup(model: MockModel, side: Side, groupId: string): ProvenanceGroup | undefined {
  const layerId = side === "left" ? model.leftLayerId : model.rightLayerId;
  const connectedLayerId = side === "left" ? model.rightLayerId : model.leftLayerId;
  const visibleItems = visibleItemsForCurrentPair(model);
  return findGroup(visibleItems, model.connections, layerId, connectedLayerId, side, model.groupingByLayer[layerId] ?? [], groupId);
}

function parameterValueSet(item: ProvenanceItem): Set<string> {
  return new Set(item.parameters.map((parameter) => `${normalizedKey(parameter.key)}\u0000${parameter.value.trim().toLowerCase()}`));
}

function addCatalogParameter(
  catalog: Record<string, Record<string, string[]>>,
  layerId: string,
  key: string,
): Record<string, Record<string, string[]>> {
  const trimmedKey = key.trim();
  const layerCatalog = catalog[layerId] ?? {};
  const existingKey = Object.keys(layerCatalog).find((candidate) => normalizedKey(candidate) === normalizedKey(trimmedKey));

  return {
    ...catalog,
    [layerId]: {
      ...layerCatalog,
      [existingKey ?? trimmedKey]: layerCatalog[existingKey ?? trimmedKey] ?? [],
    },
  };
}

function addCatalogValue(
  catalog: Record<string, Record<string, string[]>>,
  layerId: string,
  key: string,
  value: string,
): Record<string, Record<string, string[]>> {
  const trimmedValue = value.trim();
  const withParameter = addCatalogParameter(catalog, layerId, key);
  const layerCatalog = withParameter[layerId] ?? {};
  const existingKey = Object.keys(layerCatalog).find((candidate) => normalizedKey(candidate) === normalizedKey(key)) ?? key;
  const values = layerCatalog[existingKey] ?? [];
  const hasValue = values.some((candidate) => candidate.trim().toLowerCase() === trimmedValue.toLowerCase());

  return {
    ...withParameter,
    [layerId]: {
      ...layerCatalog,
      [existingKey]: hasValue ? values : [...values, trimmedValue],
    },
  };
}

function copyMissingSourceParameters(
  items: ProvenanceItem[],
  connections: ProvenanceConnection[],
  sourceGroup: ProvenanceGroup,
  targetGroup: ProvenanceGroup,
): ProvenanceItem[] {
  const sourceParameters = new Map<string, { key: string; value: string }>();
  const sourceItems = uniqueGroupItems(sourceGroup);
  const targetItems = uniqueGroupItems(targetGroup);

  sourceItems.forEach((item) => {
    item.parameters.forEach((parameter) => {
      const normalized = `${normalizedKey(parameter.key)}\u0000${parameter.value.trim().toLowerCase()}`;
      if (!sourceParameters.has(normalized)) {
        sourceParameters.set(normalized, parameter);
      }
    });
  });

  const sourceIds = new Set(sourceItems.map((item) => item.id));
  const targetIds = new Set(targetItems.map((item) => item.id));
  const connectedTargetIds = new Set(
    connections
      .filter(
        (connection) =>
          sourceIds.has(connection.sourceId) &&
          targetIds.has(connection.targetId),
      )
      .map((connection) => connection.targetId),
  );

  return items.map((item) => {
    if (!connectedTargetIds.has(item.id)) {
      return item;
    }

    const existing = parameterValueSet(item);
    const inherited = Array.from(sourceParameters.values()).filter(
      (parameter) => !existing.has(`${normalizedKey(parameter.key)}\u0000${parameter.value.trim().toLowerCase()}`),
    );

    return inherited.length === 0
      ? item
      : { ...item, parameters: [...item.parameters, ...inherited] };
  });
}

function uniqueGroupItems(group: ProvenanceGroup): ProvenanceItem[] {
  const itemsById = new Map<string, ProvenanceItem>();

  group.members.forEach((member) => {
    if (!itemsById.has(member.item.id)) {
      itemsById.set(member.item.id, member.item);
    }
  });

  return Array.from(itemsById.values());
}

function uniqueItemsForGroups(groups: ProvenanceGroup[]): ProvenanceItem[] {
  const itemsById = new Map<string, ProvenanceItem>();

  groups.forEach((group) => {
    uniqueGroupItems(group).forEach((item) => {
      if (!itemsById.has(item.id)) {
        itemsById.set(item.id, item);
      }
    });
  });

  return Array.from(itemsById.values());
}

function toggleSelection(ids: string[], groupId: string): string[] {
  return ids.includes(groupId) ? ids.filter((id) => id !== groupId) : [...ids, groupId];
}

type SelectedSeedGroups = {
  sourceGroups: ProvenanceGroup[];
  targetGroups: ProvenanceGroup[];
};

function selectedSeedGroups(model: MockModel): SelectedSeedGroups {
  const targetGroups = model.selectedTargetGroupIds.flatMap((groupId) => {
    const group = findVisibleGroup(model, "right", groupId);
    return group ? [group] : [];
  });

  const sourceGroups = model.selectedSourceGroupIds.flatMap((groupId) => {
    const group = findVisibleGroup(model, "left", groupId);
    return group ? [group] : [];
  });

  return { sourceGroups, targetGroups };
}

function nextGeneratedLayerNumber(layers: ProvenanceLayer[]): number {
  const existingNumbers = layers.flatMap((layer) => {
    const match = /^layer-(\d+)$/.exec(layer.id);
    return match ? [Number(match[1])] : [];
  });

  return Math.max(2, ...existingNumbers) + 1;
}

function safeIdPart(value: string): string {
  return (
    value
      .trim()
      .replace(/[^A-Za-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "")
      .slice(0, 80) || "item"
  );
}

function copyItemsIntoLayer(items: ProvenanceItem[], layerId: string): ProvenanceItem[] {
  return items.map((item, itemIndex) => ({
    id: `${layerId}-${itemIndex + 1}-${safeIdPart(item.id)}`,
    name: item.name,
    layerId,
    parameters: item.parameters.map((parameter) => ({ ...parameter })),
  }));
}

function clearSelection(): Pick<MockModel, "selectedSourceGroupIds" | "selectedTargetGroupIds"> {
  return {
    selectedSourceGroupIds: [],
    selectedTargetGroupIds: [],
  };
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

function StatefulMockup() {
  const [model, setModel] = React.useState<MockModel>(() => initialModel());

  const clearError = () => setModel((current) => ({ ...current, error: undefined }));

  const toggleGrouping = (side: Side, key: string) => {
    setModel((current) => {
      const leftKeys = current.groupingByLayer[current.leftLayerId] ?? [];
      const rightKeys = current.groupingByLayer[current.rightLayerId] ?? [];
      const nextLeftKeys =
        side === "left"
          ? hasGroupingKey(leftKeys, key)
            ? removeGroupingKey(leftKeys, key)
            : addGroupingKey(leftKeys, key)
          : hasGroupingKey(leftKeys, key) && hasGroupingKey(rightKeys, key)
            ? removeGroupingKey(leftKeys, key)
            : addGroupingKey(leftKeys, key);
      const nextRightKeys =
        side === "right"
          ? hasGroupingKey(leftKeys, key) && hasGroupingKey(rightKeys, key)
            ? removeGroupingKey(rightKeys, key)
            : addGroupingKey(rightKeys, key)
          : rightKeys;

      return {
        ...current,
        groupingByLayer: {
          ...current.groupingByLayer,
          [current.leftLayerId]: nextLeftKeys,
          [current.rightLayerId]: nextRightKeys,
        },
        ...clearSelection(),
        detail: undefined,
        error: undefined,
      };
    });
  };

  const moveParameter = (key: string, side: Side) => {
    setModel((current) => ({
      ...current,
      parameterRailByKey: {
        ...current.parameterRailByKey,
        [normalizedKey(key)]: side,
      },
      error: undefined,
    }));
  };

  const sortLayer = (layerId: string, key: string | undefined) => {
    setModel((current) => ({
      ...current,
      sortingByLayer: {
        ...current.sortingByLayer,
        [layerId]: key,
      },
      detail: undefined,
      error: undefined,
    }));
  };

  const createParameter = (side: Side, key: string) => {
    setModel((current) => {
      const trimmedKey = key.trim();
      if (!trimmedKey) {
        return { ...current, error: "Enter a parameter name." };
      }

      const layerId = side === "left" ? current.leftLayerId : current.rightLayerId;
      const layerCatalog = current.parameterValuesByLayer[layerId] ?? {};
      const duplicateCatalogKey = Object.keys(layerCatalog).some(
        (candidate) => normalizedKey(candidate) === normalizedKey(trimmedKey),
      );
      const duplicateItemKey = current.items
        .filter((item) => item.layerId === layerId)
        .some((item) => item.parameters.some((parameter) => normalizedKey(parameter.key) === normalizedKey(trimmedKey)));

      if (duplicateCatalogKey || duplicateItemKey) {
        return { ...current, error: `Parameter "${trimmedKey}" already exists. Choose another parameter name.` };
      }

      return {
        ...current,
        parameterValuesByLayer: addCatalogParameter(current.parameterValuesByLayer, layerId, trimmedKey),
        parameterRailByKey: {
          ...current.parameterRailByKey,
          [normalizedKey(trimmedKey)]: side,
        },
        error: undefined,
      };
    });
  };

  const createParameterValue = (side: Side, key: string, value: string) => {
    setModel((current) => {
      const trimmedValue = value.trim();
      if (!trimmedValue) {
        return { ...current, error: "Enter a parameter value." };
      }

      const layerId = side === "left" ? current.leftLayerId : current.rightLayerId;

      return {
        ...current,
        parameterValuesByLayer: addCatalogValue(current.parameterValuesByLayer, layerId, key, trimmedValue),
        error: undefined,
      };
    });
  };

  const assignParameterValue = (side: Side, key: string, value: string, groupId: string) => {
    setModel((current) => {
      const layerId = side === "left" ? current.leftLayerId : current.rightLayerId;
      const group = findVisibleGroup(current, side, groupId);

      if (!group) {
        return { ...current, error: "The selected group no longer exists." };
      }

      const affectedIds = downstreamIds(
        current.connections,
        uniqueGroupItems(group).map((item) => item.id),
      );

      return {
        ...current,
        items: current.items.map((item) => (affectedIds.has(item.id) ? addParameterValue(item, key, value) : item)),
        parameterValuesByLayer: addCatalogValue(current.parameterValuesByLayer, layerId, key, value),
        error: undefined,
      };
    });
  };

  const selectGroup = (side: Side, groupId: string) => {
    setModel((current) => ({
      ...current,
      selectedSourceGroupIds:
        side === "left" ? toggleSelection(current.selectedSourceGroupIds, groupId) : current.selectedSourceGroupIds,
      selectedTargetGroupIds:
        side === "right" ? toggleSelection(current.selectedTargetGroupIds, groupId) : current.selectedTargetGroupIds,
      error: undefined,
    }));
  };

  const openDetail = (detail: ProvenanceDetail) => {
    setModel((current) => {
      const currentDetail = current.detail;
      const isSameParameter =
        currentDetail?.kind === "parameter" &&
        detail.kind === "parameter" &&
        currentDetail.layerId === detail.layerId &&
        currentDetail.key === detail.key;
      const isSameGroup =
        currentDetail?.kind === "group" &&
        detail.kind === "group" &&
        currentDetail.side === detail.side &&
        currentDetail.groupId === detail.groupId;
      const isSameConnection =
        currentDetail?.kind === "connection" &&
        detail.kind === "connection" &&
        currentDetail.sourceGroupId === detail.sourceGroupId &&
        currentDetail.targetGroupId === detail.targetGroupId;

      return {
        ...current,
        detail: isSameParameter || isSameGroup || isSameConnection ? undefined : detail,
        error: undefined,
      };
    });
  };

  const updateParameter = (side: Side, groupId: string, key: string, oldValue: string, value: string) => {
    setModel((current) => {
      const group = findVisibleGroup(current, side, groupId);
      if (!group) {
        return { ...current, error: "The selected group no longer exists." };
      }

      const result = updateParameterInGroup(current.items, current.connections, group, key, oldValue, value);
      return result.ok
        ? { ...current, items: result.value, error: undefined }
        : { ...current, error: result.error };
    });
  };

  const connectVisibleGroups = (sourceGroupId: string, targetGroupId: string) => {
    setModel((current) => {
      const sourceGroup = findVisibleGroup(current, "left", sourceGroupId);
      const targetGroup = findVisibleGroup(current, "right", targetGroupId);

      if (!sourceGroup || !targetGroup) {
        return { ...current, error: "The dropped groups no longer exist." };
      }

      const nextConnections = connectGroups(current.connections, sourceGroup, targetGroup);
      const nextItems = copyMissingSourceParameters(current.items, nextConnections, sourceGroup, targetGroup);

      return {
        ...current,
        connections: nextConnections,
        items: nextItems,
        error: undefined,
      };
    });
  };

  const createItem = (layerId: string, name: string) => {
    setModel((current) => {
      const existingCount = current.items.filter((item) => item.layerId === layerId).length;
      const safeName = name.trim() || `New item ${existingCount + 1}`;
      const nextItem: ProvenanceItem = {
        id: `${layerId}-${existingCount + 1}`,
        name: safeName,
        layerId,
        parameters: [],
      };
      const activePairKey = currentPairKey(current);
      const activePairScope = current.visibleItemIdsByPair[activePairKey];
      const nextVisibleItemIdsByPair =
        activePairScope?.[layerId] === undefined
          ? current.visibleItemIdsByPair
          : {
              ...current.visibleItemIdsByPair,
              [activePairKey]: {
                ...activePairScope,
                [layerId]: [...activePairScope[layerId], nextItem.id],
              },
            };

      return {
        ...current,
        items: [...current.items, nextItem],
        visibleItemIdsByPair: nextVisibleItemIdsByPair,
        error: undefined,
      };
    });
  };

  const addLayer = () => {
    setModel((current) => {
      const nextIndex = nextGeneratedLayerNumber(current.layers);
      const nextLayer: ProvenanceLayer = {
        id: `layer-${nextIndex}`,
        label: `Layer ${nextIndex}`,
      };
      const seed = selectedSeedGroups(current);
      const hasSourceSeed = seed.sourceGroups.length > 0;
      const hasTargetSeed = seed.targetGroups.length > 0;
      const hasMixedSeed = hasSourceSeed && hasTargetSeed;
      const selectedSeedItems = hasMixedSeed
        ? uniqueItemsForGroups([...seed.sourceGroups, ...seed.targetGroups])
        : hasTargetSeed
          ? uniqueItemsForGroups(seed.targetGroups)
          : hasSourceSeed
            ? uniqueItemsForGroups(seed.sourceGroups)
            : undefined;
      const selectionLayer: ProvenanceLayer | undefined = hasMixedSeed
        ? { id: `selection-${nextIndex}`, label: `Selection ${nextIndex}` }
        : undefined;
      const copiedSelectionItems =
        selectionLayer && selectedSeedItems ? copyItemsIntoLayer(selectedSeedItems, selectionLayer.id) : [];
      const selectedExistingSeedLayerId = hasTargetSeed
        ? current.rightLayerId
        : hasSourceSeed
          ? current.leftLayerId
          : current.rightLayerId;
      const seedLayerId = selectionLayer?.id ?? selectedExistingSeedLayerId;
      const seedItemIds = selectionLayer
        ? copiedSelectionItems.map((item) => item.id)
        : selectedSeedItems?.map((item) => item.id);
      const nextPair = { leftLayerId: seedLayerId, rightLayerId: nextLayer.id };
      const nextPairKey = pairKey(nextPair.leftLayerId, nextPair.rightLayerId);
      const nextVisibleItemIdsByPair =
        seedItemIds === undefined
          ? current.visibleItemIdsByPair
          : {
              ...current.visibleItemIdsByPair,
              [nextPairKey]: {
                [seedLayerId]: seedItemIds,
              },
            };

      return {
        ...current,
        layers: selectionLayer ? [...current.layers, selectionLayer, nextLayer] : [...current.layers, nextLayer],
        items: copiedSelectionItems.length === 0 ? current.items : [...current.items, ...copiedSelectionItems],
        layerPairs: [...current.layerPairs, nextPair],
        visibleItemIdsByPair: nextVisibleItemIdsByPair,
        leftLayerId: seedLayerId,
        rightLayerId: nextLayer.id,
        groupingByLayer: {
          ...current.groupingByLayer,
          ...(selectionLayer ? { [selectionLayer.id]: [] } : {}),
          [nextLayer.id]: [],
        },
        sortingByLayer: {
          ...current.sortingByLayer,
          ...(selectionLayer ? { [selectionLayer.id]: undefined } : {}),
          [nextLayer.id]: undefined,
        },
        parameterValuesByLayer: {
          ...current.parameterValuesByLayer,
          ...(selectionLayer ? { [selectionLayer.id]: {} } : {}),
          [nextLayer.id]: {},
        },
        ...clearSelection(),
        detail: undefined,
        error: undefined,
      };
    });
  };

  const selectPair = (leftLayerId: string, rightLayerId: string) => {
    setModel((current) => ({
      ...current,
      leftLayerId,
      rightLayerId,
      ...clearSelection(),
      detail: undefined,
      error: undefined,
    }));
  };

  return (
    <ProvenanceGrouping
      layers={model.layers}
      items={model.items}
      connections={model.connections}
      leftLayerId={model.leftLayerId}
      rightLayerId={model.rightLayerId}
      layerPairs={model.layerPairs}
      visibleItemIdsByLayer={currentVisibleItemIdsByLayer(model)}
      groupingByLayer={model.groupingByLayer}
      sortingByLayer={model.sortingByLayer}
      parameterValuesByLayer={model.parameterValuesByLayer}
      parameterRailByKey={model.parameterRailByKey}
      selectedSourceGroupIds={model.selectedSourceGroupIds}
      selectedTargetGroupIds={model.selectedTargetGroupIds}
      detail={model.detail}
      error={model.error}
      onToggleGrouping={toggleGrouping}
      onSortLayer={sortLayer}
      onMoveParameter={moveParameter}
      onCreateParameter={createParameter}
      onCreateParameterValue={createParameterValue}
      onAssignParameterValue={assignParameterValue}
      onSelectGroup={selectGroup}
      onConnectGroups={connectVisibleGroups}
      onOpenDetail={openDetail}
      onUpdateParameter={updateParameter}
      onCreateItem={createItem}
      onAddLayer={addLayer}
      onSelectPair={selectPair}
      onDismissError={clearError}
    />
  );
}

const meta = {
  title: "Components/ProvenanceGrouping",
  component: ProvenanceGrouping,
  tags: ["autodocs"],
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => (
      <div className="swt:h-screen swt:w-screen swt:overflow-auto swt:bg-base-200">
        <style>{`
          html,
          body,
          #storybook-root {
            width: 100vw;
            height: 100vh;
            margin: 0;
          }
        `}</style>
        <div
          className="swt:relative swt:h-screen swt:min-h-[560px] swt:min-w-[980px] swt:overflow-auto"
          data-testid="ProvenanceGrouping-preview-frame"
          style={{
            resize: "both",
            overflow: "auto",
            width: "100vw",
            maxWidth: "none",
          }}
        >
          <Story />
          <div
            aria-hidden="true"
            className="swt:pointer-events-none swt:absolute swt:bottom-1 swt:right-1 swt:flex swt:size-5 swt:items-center swt:justify-center swt:rounded swt:bg-base-100/80 swt:text-base-content/60"
          >
            <i className="swt:iconify swt:fluent--resize-large-20-regular swt:size-4" />
          </div>
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof ProvenanceGrouping>;

export default meta;
type Story = StoryObj<typeof meta>;

export const InteractiveMockup: Story = {
  name: "Interactive mockup",
  render: () => <StatefulMockup />,
};

export const InteractionFlow: Story = {
  name: "Interaction flow",
  render: () => <StatefulMockup />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(await canvas.findByTestId("ProvenanceGrouping-root")).toBeInTheDocument();
    const previewFrame = await canvas.findByTestId("ProvenanceGrouping-preview-frame");
    await expect(previewFrame).toHaveStyle({
      resize: "both",
    });
    expect(previewFrame.style.width).toBe("100vw");
    expect(Math.round(previewFrame.getBoundingClientRect().width)).toBeGreaterThanOrEqual(window.innerWidth);
    expect(Math.round((await canvas.findByTestId("ProvenanceGrouping-root")).getBoundingClientRect().left)).toBe(0);

    await waitFor(async () => {
      await expect(await canvas.findAllByTestId(/ProvenanceGrouping-group-left-/)).toHaveLength(4);
      await expect(await canvas.findAllByTestId(/ProvenanceGrouping-group-right-/)).toHaveLength(5);
      expect(canvasElement).not.toHaveTextContent("All items");
    });

    const [groupLink] = await canvas.findAllByTestId("ProvenanceGrouping-group-connector");
    await userEvent.click(groupLink);
    await waitFor(async () => {
      const expandedGroups = await canvas.findAllByTestId("ProvenanceGrouping-group-inline-detail");
      expect(expandedGroups.length).toBeGreaterThanOrEqual(2);
      await expect(canvasElement).toHaveTextContent("Input A");
      await expect(canvasElement).toHaveTextContent("Output A");
      expect((await canvas.findAllByTestId("ProvenanceGrouping-individual-connector")).length).toBeGreaterThan(0);
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-left-Temperature"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Temperature: 12 C");
      expect(canvasElement).toHaveTextContent("Temperature: 24 C");
    });
    await waitFor(async () => {
      await expect(await canvas.findAllByTestId(/ProvenanceGrouping-group-left-/)).toHaveLength(2);
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-left-Species"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Species: Arabidopsis");
      expect(canvasElement).toHaveTextContent("Species: Chlamydomonas");
    });

    const arabidopsis12Group = await canvas.findByTestId(/ProvenanceGrouping-group-left-.*12-C.*Arabidopsis/);
    await userEvent.click(within(arabidopsis12Group).getByTestId("ProvenanceGrouping-group-select"));
    await userEvent.click(within(arabidopsis12Group).getByTestId("ProvenanceGrouping-group-update-param"));
    await userEvent.selectOptions(
      within(arabidopsis12Group).getByTestId("ProvenanceGrouping-update-key-select"),
      "Temperature",
    );
    const updateValue = within(arabidopsis12Group).getByTestId("ProvenanceGrouping-param-value-input");
    await userEvent.clear(updateValue);
    await userEvent.type(updateValue, "16 C");
    await userEvent.click(within(arabidopsis12Group).getByTestId("ProvenanceGrouping-param-submit"));

    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Temperature: 16 C");
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-right-Analysis"));
    const imagingGroup = await canvas.findByTestId(/ProvenanceGrouping-group-right-.*Imaging/);
    const source24Group = await canvas.findByTestId(/ProvenanceGrouping-group-left-.*24-C.*Arabidopsis/);
    await userEvent.click(within(source24Group).getByTestId("ProvenanceGrouping-group-select"));
    await userEvent.click(within(imagingGroup).getByTestId("ProvenanceGrouping-group-select"));
    expect(canvas.queryByTestId("ProvenanceGrouping-connect-selected")).not.toBeInTheDocument();
    const groupDrop = new DataTransfer();
    fireEvent.dragStart(source24Group, { dataTransfer: groupDrop });
    fireEvent.dragOver(imagingGroup, { dataTransfer: groupDrop });
    fireEvent.drop(imagingGroup, { dataTransfer: groupDrop });

    await waitFor(async () => {
      await expect((await canvas.findAllByTestId("ProvenanceGrouping-group-connector")).length).toBeGreaterThan(0);
    });

    await userEvent.type(canvas.getByTestId("ProvenanceGrouping-create-item-right-input"), "Fresh output");
    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-create-item-right-submit"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Fresh output");
      expect(canvasElement).not.toHaveTextContent("Missing Analysis");
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-add-layer"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Selection 3 -> Layer 3");
      expect(canvasElement).toHaveTextContent("Input C");
      expect(canvasElement).toHaveTextContent("Output E");
    });
  },
};

export const GroupedConnectionDetails: Story = {
  name: "Grouped connection details",
  render: () => <StatefulMockup />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(await canvas.findByTestId("ProvenanceGrouping-root")).toBeInTheDocument();
    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-left-Species-move"));
    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-right-Species"));

    const arabidopsisConnector = await canvas.findByLabelText("Species: Arabidopsis to Species: Arabidopsis");
    await userEvent.click(arabidopsisConnector);

    await waitFor(async () => {
      await expect(await canvas.findAllByTestId("ProvenanceGrouping-individual-connector")).toHaveLength(4);
    });
  },
};

export const MultiValueAndConnectionDerivedControls: Story = {
  name: "Multi-value and connection-derived controls",
  render: () => <StatefulMockup />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(await canvas.findByTestId("ProvenanceGrouping-root")).toBeInTheDocument();

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-right-Analysis-move"));
    await userEvent.click(await canvas.findByTestId("ProvenanceGrouping-param-left-Analysis"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Analysis: Mass Spectrometry");
      expect(canvasElement).toHaveTextContent("Analysis: LC-MS");
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-left-Replicate-move"));
    await userEvent.click(await canvas.findByTestId("ProvenanceGrouping-param-right-Replicate"));

    const replicate1Group = await canvas.findByTestId(/ProvenanceGrouping-group-right-.*Replicate-1/);
    const replicate2Group = await canvas.findByTestId(/ProvenanceGrouping-group-right-.*Replicate-2/);
    await userEvent.click(within(replicate1Group).getByTestId("ProvenanceGrouping-group-details"));
    await waitFor(() => {
      expect(replicate1Group).toHaveTextContent("Output B");
    });

    await userEvent.click(within(replicate2Group).getByTestId("ProvenanceGrouping-group-details"));
    await waitFor(() => {
      expect(replicate2Group).toHaveTextContent("Output B");
    });
  },
};

export const AddLayerFromSelectedOutput: Story = {
  name: "Add layer from selected output",
  render: () => <StatefulMockup />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(await canvas.findByTestId("ProvenanceGrouping-root")).toBeInTheDocument();

    const outputBGroup = await canvas.findByTestId("ProvenanceGrouping-group-right-outputs-item-output-b");
    const outputCGroup = await canvas.findByTestId("ProvenanceGrouping-group-right-outputs-item-output-c");
    await userEvent.click(within(outputBGroup).getByTestId("ProvenanceGrouping-group-select"));
    await userEvent.click(within(outputCGroup).getByTestId("ProvenanceGrouping-group-select"));
    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-add-layer"));

    await waitFor(async () => {
      await expect(canvasElement).toHaveTextContent("Outputs -> Layer 3");
      await expect(await canvas.findAllByTestId(/ProvenanceGrouping-group-left-/)).toHaveLength(2);
      await expect(canvasElement).toHaveTextContent("Output B");
      await expect(canvasElement).toHaveTextContent("Output C");
      await expect(canvasElement).not.toHaveTextContent("Output A");
    });
  },
};

export const AddLayerFromMixedSelection: Story = {
  name: "Add layer from mixed selection",
  render: () => <StatefulMockup />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(await canvas.findByTestId("ProvenanceGrouping-root")).toBeInTheDocument();

    const inputAGroup = await canvas.findByTestId("ProvenanceGrouping-group-left-inputs-item-input-a");
    const outputBGroup = await canvas.findByTestId("ProvenanceGrouping-group-right-outputs-item-output-b");
    await userEvent.click(within(inputAGroup).getByTestId("ProvenanceGrouping-group-select"));
    await userEvent.click(within(outputBGroup).getByTestId("ProvenanceGrouping-group-select"));
    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-add-layer"));

    await waitFor(async () => {
      await expect(canvasElement).toHaveTextContent("Selection 3 -> Layer 3");
      await expect(await canvas.findAllByTestId(/ProvenanceGrouping-group-left-/)).toHaveLength(2);
      await expect(canvasElement).toHaveTextContent("Input A");
      await expect(canvasElement).toHaveTextContent("Output B");
      await expect(canvasElement).not.toHaveTextContent("Input B");
      await expect(canvasElement).not.toHaveTextContent("Output A");
    });
  },
};

export const SortByNonGroupingParameter: Story = {
  name: "Sort by non-grouping parameter",
  render: () => <StatefulMockup />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(await canvas.findByTestId("ProvenanceGrouping-root")).toBeInTheDocument();

    await userEvent.selectOptions(canvas.getByTestId("ProvenanceGrouping-sort-left"), "Temperature");
    await waitFor(() => {
      const labels = Array.from(
        canvasElement.querySelectorAll('[data-testid^="ProvenanceGrouping-group-left-"] h3'),
      ).map((element) => element.textContent ?? "");

      expect(labels).toEqual(["Input A", "Input B", "Input C", "Input D"]);
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-left-Species"));
    await waitFor(() => {
      const sortSelect = canvas.getByTestId("ProvenanceGrouping-sort-left") as HTMLSelectElement;
      expect(Array.from(sortSelect.options).map((option) => option.value)).not.toContain("Species");
    });

    await userEvent.selectOptions(canvas.getByTestId("ProvenanceGrouping-sort-left"), "Replicate");
    const arabidopsisGroup = await canvas.findByTestId(/ProvenanceGrouping-group-left-.*Arabidopsis/);
    await userEvent.click(within(arabidopsisGroup).getByTestId("ProvenanceGrouping-group-details"));

    await waitFor(async () => {
      const detailText = (await within(arabidopsisGroup).findByTestId("ProvenanceGrouping-group-inline-detail"))
        .textContent ?? "";
      expect(detailText.indexOf("Input A")).toBeLessThan(detailText.indexOf("Input C"));
      expect(detailText.indexOf("Input C")).toBeLessThan(detailText.indexOf("Input B"));
    });
  },
};
