import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, userEvent, waitFor, within } from "storybook/test";
import {
  ProvenanceGrouping,
  addParameterToGroup,
  buildGroups,
  connectGroups,
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
  };
}

function findGroup(
  items: ProvenanceItem[],
  layerId: string,
  groupingKeys: string[],
  groupId: string,
): ProvenanceGroup | undefined {
  return buildGroups(items, layerId, groupingKeys).find((group) => group.id === groupId);
}

function findVisibleGroup(model: MockModel, side: Side, groupId: string): ProvenanceGroup | undefined {
  const layerId = side === "left" ? model.leftLayerId : model.rightLayerId;
  return findGroup(model.items, layerId, model.groupingByLayer[layerId] ?? [], groupId);
}

function parameterMap(item: ProvenanceItem): Map<string, string> {
  return new Map(item.parameters.map((parameter) => [parameter.key.toLowerCase(), parameter.value]));
}

function copyMissingSourceParameters(
  items: ProvenanceItem[],
  connections: ProvenanceConnection[],
  sourceGroup: ProvenanceGroup,
  targetGroup: ProvenanceGroup,
): ProvenanceItem[] {
  const sourceParameters = new Map<string, { key: string; value: string }>();
  sourceGroup.items.forEach((item) => {
    item.parameters.forEach((parameter) => {
      const normalizedKey = parameter.key.toLowerCase();
      if (!sourceParameters.has(normalizedKey)) {
        sourceParameters.set(normalizedKey, parameter);
      }
    });
  });

  const connectedTargetIds = new Set(
    connections
      .filter(
        (connection) =>
          sourceGroup.items.some((item) => item.id === connection.sourceId) &&
          targetGroup.items.some((item) => item.id === connection.targetId),
      )
      .map((connection) => connection.targetId),
  );

  return items.map((item) => {
    if (!connectedTargetIds.has(item.id)) {
      return item;
    }

    const existing = parameterMap(item);
    const inherited = Array.from(sourceParameters.values()).filter(
      (parameter) => !existing.has(parameter.key.toLowerCase()),
    );

    return inherited.length === 0
      ? item
      : { ...item, parameters: [...item.parameters, ...inherited] };
  });
}

function StatefulMockup() {
  const [model, setModel] = React.useState<MockModel>(() => initialModel());

  const clearError = () => setModel((current) => ({ ...current, error: undefined }));

  const toggleGrouping = (layerId: string, key: string) => {
    setModel((current) => {
      const currentKeys = current.groupingByLayer[layerId] ?? [];
      const nextKeys = currentKeys.includes(key)
        ? currentKeys.filter((existingKey) => existingKey !== key)
        : [...currentKeys, key];

      return {
        ...current,
        groupingByLayer: {
          ...current.groupingByLayer,
          [layerId]: nextKeys,
        },
        selectedSourceGroupId: undefined,
        selectedTargetGroupId: undefined,
        detail: undefined,
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
    setModel((current) => ({ ...current, detail, error: undefined }));
  };

  const addParameter = (side: Side, groupId: string, key: string, value: string) => {
    setModel((current) => {
      const group = findVisibleGroup(current, side, groupId);
      if (!group) {
        return { ...current, error: "The selected group no longer exists." };
      }

      const result = addParameterToGroup(current.items, current.connections, group, key, value);
      return result.ok
        ? { ...current, items: result.value, error: undefined }
        : { ...current, error: result.error };
    });
  };

  const updateParameter = (side: Side, groupId: string, key: string, value: string) => {
    setModel((current) => {
      const group = findVisibleGroup(current, side, groupId);
      if (!group) {
        return { ...current, error: "The selected group no longer exists." };
      }

      const result = updateParameterInGroup(current.items, current.connections, group, key, value);
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
      selectedSourceGroupId={model.selectedSourceGroupId}
      selectedTargetGroupId={model.selectedTargetGroupId}
      detail={model.detail}
      error={model.error}
      onToggleGrouping={toggleGrouping}
      onSelectGroup={selectGroup}
      onOpenDetail={openDetail}
      onAddParameter={addParameter}
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
      <div className="swt:h-screen swt:bg-base-200 swt:p-4">
        <Story />
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

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-left-Temperature"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Temperature: 12 C");
      expect(canvasElement).toHaveTextContent("Temperature: 24 C");
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-left-Species"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Species: Arabidopsis");
      expect(canvasElement).toHaveTextContent("Species: Chlamydomonas");
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-param-left-Species-details"));
    await waitFor(async () => {
      await expect(await canvas.findByTestId("ProvenanceGrouping-detail-panel")).toHaveTextContent("Input A");
      await expect(await canvas.findByTestId("ProvenanceGrouping-detail-panel")).toHaveTextContent("Arabidopsis");
    });

    const arabidopsis12Group = await canvas.findByTestId(/ProvenanceGrouping-group-left-.*12-C.*Arabidopsis/);
    await userEvent.click(within(arabidopsis12Group).getByTestId("ProvenanceGrouping-group-select"));
    await userEvent.click(within(arabidopsis12Group).getByTestId("ProvenanceGrouping-group-add-param"));
    await userEvent.type(within(arabidopsis12Group).getByTestId("ProvenanceGrouping-param-key-input"), "Species");
    await userEvent.type(within(arabidopsis12Group).getByTestId("ProvenanceGrouping-param-value-input"), "Lemna");
    await userEvent.click(within(arabidopsis12Group).getByTestId("ProvenanceGrouping-param-submit"));

    await waitFor(async () => {
      await expect(await canvas.findByTestId("ProvenanceGrouping-error")).toHaveTextContent(
        "Choose another parameter name",
      );
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-error-dismiss"));
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
      await expect((await canvas.findAllByTestId("ProvenanceGrouping-connection-full")).length).toBeGreaterThan(0);
    });

    await userEvent.type(canvas.getByTestId("ProvenanceGrouping-create-item-right-input"), "Fresh output");
    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-create-item-right-submit"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Analysis: Missing Analysis");
    });

    await userEvent.click(canvas.getByTestId("ProvenanceGrouping-add-layer"));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent("Outputs -> Layer 3");
    });
  },
};
