import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, userEvent, waitFor, within } from "storybook/test";
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
} from "./ProvenanceGrouping";

type Side = "left" | "right";

type MockModel = {
  layers: ProvenanceLayer[];
  items: ProvenanceItem[];
  connections: ProvenanceConnection[];
  leftLayerId: string;
  rightLayerId: string;
  groupingByLayer: Record<string, string[]>;
  sortingByLayer: Record<string, string | undefined>;
  parameterValuesByLayer: Record<string, Record<string, string[]>>;
  parameterRailByKey: Record<string, Side>;
  selectedSourceGroupId?: string;
  selectedTargetGroupId?: string;
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
  return findGroup(model.items, model.connections, layerId, connectedLayerId, side, model.groupingByLayer[layerId] ?? [], groupId);
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
        selectedSourceGroupId: undefined,
        selectedTargetGroupId: undefined,
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
      selectedSourceGroupId: side === "left" ? groupId : current.selectedSourceGroupId,
      selectedTargetGroupId: side === "right" ? groupId : current.selectedTargetGroupId,
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

  const connectSelectedGroups = () => {
    setModel((current) => {
      if (!current.selectedSourceGroupId || !current.selectedTargetGroupId) {
        return { ...current, error: "Select one source group and one target group first." };
      }

      const sourceGroup = findVisibleGroup(current, "left", current.selectedSourceGroupId);
      const targetGroup = findVisibleGroup(current, "right", current.selectedTargetGroupId);

      if (!sourceGroup || !targetGroup) {
        return { ...current, error: "The selected groups no longer exist." };
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

      return { ...current, items: [...current.items, nextItem], error: undefined };
    });
  };

  const addLayer = () => {
    setModel((current) => {
      const nextIndex = current.layers.length + 1;
      const nextLayer: ProvenanceLayer = {
        id: `layer-${nextIndex}`,
        label: `Layer ${nextIndex}`,
      };

      return {
        ...current,
        layers: [...current.layers, nextLayer],
        leftLayerId: current.rightLayerId,
        rightLayerId: nextLayer.id,
        groupingByLayer: {
          ...current.groupingByLayer,
          [nextLayer.id]: [],
        },
        sortingByLayer: {
          ...current.sortingByLayer,
          [nextLayer.id]: undefined,
        },
        parameterValuesByLayer: {
          ...current.parameterValuesByLayer,
          [nextLayer.id]: {},
        },
        selectedSourceGroupId: undefined,
        selectedTargetGroupId: undefined,
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
      selectedSourceGroupId: undefined,
      selectedTargetGroupId: undefined,
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
      groupingByLayer={model.groupingByLayer}
      sortingByLayer={model.sortingByLayer}
      parameterValuesByLayer={model.parameterValuesByLayer}
      parameterRailByKey={model.parameterRailByKey}
      selectedSourceGroupId={model.selectedSourceGroupId}
      selectedTargetGroupId={model.selectedTargetGroupId}
      detail={model.detail}
      error={model.error}
      onToggleGrouping={toggleGrouping}
      onSortLayer={sortLayer}
      onMoveParameter={moveParameter}
      onCreateParameter={createParameter}
      onCreateParameterValue={createParameterValue}
      onAssignParameterValue={assignParameterValue}
      onSelectGroup={selectGroup}
      onOpenDetail={openDetail}
      onUpdateParameter={updateParameter}
      onConnectSelectedGroups={connectSelectedGroups}
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
    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-connect-selected"));

    await waitFor(async () => {
      await expect((await canvas.findAllByTestId("ProvenanceGrouping-group-connector")).length).toBeGreaterThan(0);
    });

    await userEvent.type(canvas.getByTestId("ProvenanceGrouping-create-item-right-input"), "Fresh output");
    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-create-item-right-submit"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Ungrouped");
      expect(canvasElement).not.toHaveTextContent("Missing Analysis");
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-add-layer"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Outputs -> Layer 3");
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
