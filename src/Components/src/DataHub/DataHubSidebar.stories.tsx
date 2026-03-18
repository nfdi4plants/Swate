import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, userEvent, waitFor } from "storybook/test";
import { Entry as DataHubSidebarEntry } from "./DataHubSidebar.fs.js";

const meta: Meta<typeof DataHubSidebarEntry> = {
  title: "Components/DataHubSidebar",
  component: DataHubSidebarEntry,
  tags: ["autodocs"],
  parameters: {
    layout: "centered",
  },
  decorators: [
    (Story) => (
      <div style={{ width: 340, border: "1px solid #333", borderRadius: 8, overflow: "hidden" }}>
        <Story />
      </div>
    ),
  ],
};

// export default meta;
type Story = StoryObj<typeof DataHubSidebarEntry>;

// Helpers

async function connectAndWait(canvas: ReturnType<typeof within>) {
  const connectBtn = await canvas.findByTestId("ConnectDataHubButton");
  await userEvent.click(connectBtn);
  await waitFor(
    async () => {
      const badge = await canvas.findByTestId("DataHubStatusBadge");
      expect(badge).toHaveTextContent(/^connected$/i);
      expect(await canvas.findByTestId("DisconnectButton")).toBeVisible();
    },
    { timeout: 10000 },
  );
}

async function waitForBrowserItems(canvas: ReturnType<typeof within>, testId = "ARCBrowserItem-1") {
  await waitFor(
    async () => {
      const item = await canvas.findByTestId(testId);
      expect(item).toBeInTheDocument();
    },
    { timeout: 5000 },
  );
}

// Default (disconnected)
export const Default: Story = {
  name: "Default (Disconnected)",
};

// Connect, Select, Save
export const ConnectSelectSave: Story = {
  name: "Connect, Select, Save",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await connectAndWait(canvas);
    await waitForBrowserItems(canvas, "ARCBrowserItem-1");

    const yourArcsTab = canvas.getByTestId("ARCBrowserTab-YourARCs");
    expect(yourArcsTab.className).toMatch(/tab-active/);

    await userEvent.click(await canvas.findByTestId("ARCBrowserItem-1"));

    await waitFor(async () => {
      expect(canvas.getByTestId("SelectedProjectName")).toHaveTextContent("Metabolomics Study 2026");
    });

    await waitFor(async () => {
      expect(canvas.getByTestId("ChangedFilesList")).toBeVisible();
    });

    const saveBtn = await canvas.findByTestId("SaveToDataHubButton");
    expect(saveBtn).toBeEnabled();
    await userEvent.click(saveBtn);

    await waitFor(
      async () => {
        expect(canvas.getByTestId("OperationSuccess")).toBeVisible();
      },
      { timeout: 5000 },
    );
  },
};

// Discard File, Save All
export const DiscardAndSave: Story = {
  name: "Discard File, Save All",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await connectAndWait(canvas);
    await waitForBrowserItems(canvas, "ARCBrowserItem-1");

    await userEvent.click(await canvas.findByTestId("ARCBrowserItem-1"));

    await waitFor(async () => {
      expect(canvas.getByTestId("ChangedFilesList")).toBeVisible();
    });

    await waitFor(async () => {
      expect(canvas.getByTestId("ChangedFileItem-assays/metabolomics/isa.assay.xlsx")).toBeVisible();
      expect(canvas.getByTestId("ChangedFileItem-studies/drought/protocols/extraction.md")).toBeVisible();
      expect(canvas.getByTestId("ChangedFileItem-runs/old-run/result.csv")).toBeVisible();
      expect(canvas.getByTestId("ChangedFileItem-workflows/analysis.cwl")).toBeVisible();
    });

    const discardBtn = await canvas.findByTestId("DiscardFileButton-runs/old-run/result.csv");
    await userEvent.click(discardBtn);

    await waitFor(async () => {
      expect(canvas.queryByTestId("ChangedFileItem-runs/old-run/result.csv")).not.toBeInTheDocument();
    });

    const saveBtn = await canvas.findByTestId("SaveToDataHubButton");
    expect(saveBtn).toBeEnabled();
    await userEvent.click(saveBtn);

    await waitFor(
      async () => {
        expect(canvas.getByTestId("OperationSuccess")).toHaveTextContent(/saved/i);
      },
      { timeout: 5000 },
    );
  },
};

// ARC Browser Mode Switch
export const BrowserModeSwitch: Story = {
  name: "ARC Browser Mode Switch",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await connectAndWait(canvas);
    await waitForBrowserItems(canvas, "ARCBrowserItem-1");

    expect(canvas.getByTestId("ARCBrowserTab-YourARCs").className).toMatch(/tab-active/);

    await userEvent.click(canvas.getByTestId("ARCBrowserTab-Latest"));

    await waitFor(async () => {
      expect(canvas.getByTestId("ARCBrowserTab-Latest").className).toMatch(/tab-active/);
    });

    await waitFor(
      async () => {
        expect(canvas.getByTestId("ARCBrowserItem-10")).toBeVisible();
      },
      { timeout: 5000 },
    );

    await userEvent.click(canvas.getByTestId("ARCBrowserTab-Featured"));

    await waitFor(async () => {
      expect(canvas.getByTestId("ARCBrowserTab-Featured").className).toMatch(/tab-active/);
    });

    await waitFor(
      async () => {
        expect(canvas.getByTestId("ARCBrowserItem-20")).toBeVisible();
      },
      { timeout: 5000 },
    );
  },
};

// Share link
export const ShareLink: Story = {
  name: "Connect, Select, Share",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await connectAndWait(canvas);
    await waitForBrowserItems(canvas, "ARCBrowserItem-2");

    await userEvent.click(await canvas.findByTestId("ARCBrowserItem-2"));
    await waitFor(async () => {
      expect(canvas.getByTestId("SelectedProjectName")).toHaveTextContent("RNAseq Drought Stress");
    });

    await userEvent.click(await canvas.findByTestId("ShareARCButton"));

    await waitFor(async () => {
      expect(canvas.getByTestId("OperationSuccess")).toHaveTextContent(/Link copied/);
    });
  },
};

// Disconnect
export const DisconnectFlow: Story = {
  name: "Connect, Disconnect",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await connectAndWait(canvas);

    await userEvent.click(await canvas.findByTestId("DisconnectButton"));

    await waitFor(async () => {
      expect(canvas.getByTestId("DataHubStatusBadge")).toHaveTextContent("not connected");
    });
  },
};
