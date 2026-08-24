import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, userEvent, waitFor } from "storybook/test";
import ValidationPackageSelectorFixture from "./ValidationPackageSelector.fixture.fs.js";

const meta: Meta = {
  title: "Composite Components/ValidationPackageSelector",
  component: ValidationPackageSelectorFixture,
  tags: ["autodocs"],
  parameters: {
    layout: "centered",
  },
  decorators: [
    (Story) => (
      <div style={{ width: 1200, border: "1px solid #333", borderRadius: 8, overflow: "hidden" }}>
        <Story />
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

const loadedPackage = async (canvas: ReturnType<typeof within>) => {
  await waitFor(() => expect(canvas.getByTestId("validation-package-selector-next")).toBeInTheDocument());
};

const configValue = (canvas: ReturnType<typeof within>) =>
  (canvas.getByTestId("validation-package-selector-config") as HTMLTextAreaElement).value;

const selectDropdownOption = async (label: string) => {
  await waitFor(() => {
    const el = document.querySelector(`[data-selectoption="${label}"]`);
    expect(el).toBeTruthy();
  });
  await userEvent.click(document.querySelector(`[data-selectoption="${label}"]`) as HTMLElement);
};

const clickDocumentElement = async (testId: string) => {
  await waitFor(() => {
    const el = document.querySelector(`[data-testid="${testId}"]`);
    expect(el).toBeTruthy();
  });
  await userEvent.click(document.querySelector(`[data-testid="${testId}"]`) as HTMLElement);
};

export const LoadsPackages: Story = {
  render: () => <ValidationPackageSelectorFixture />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await loadedPackage(canvas);
    expect(canvas.getByTestId("validation-package-selector-checkbox-Package00")).toBeInTheDocument();
    expect(canvas.getByTestId("validation-package-selector-page-indicator")).toHaveTextContent("Page 1 of 2");
    await userEvent.click(canvas.getByTestId("validation-package-selector-next"));
    expect(canvas.getByTestId("validation-package-selector-checkbox-Invenio")).toBeInTheDocument();
  },
};

export const SearchFiltersByName: Story = {
  render: () => <ValidationPackageSelectorFixture />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await loadedPackage(canvas);
    const input = canvas.getByTestId("validation-package-selector-search");
    await userEvent.type(input, "invenio");
    expect(canvas.getByTestId("validation-package-selector-checkbox-Invenio")).toBeInTheDocument();
    expect(canvas.queryByTestId("validation-package-selector-checkbox-Package00")).not.toBeInTheDocument();
    expect(canvas.queryByTestId("validation-package-selector-checkbox-MySummaryPackage")).not.toBeInTheDocument();
  },
};

export const SearchScopeExtendsToSummary: Story = {
  render: () => <ValidationPackageSelectorFixture />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await loadedPackage(canvas);
    await userEvent.click(canvas.getByTestId("validation-package-selector-scope"));
    await clickDocumentElement("validation-package-selector-scope-Summary");
    const input = canvas.getByTestId("validation-package-selector-search");
    await userEvent.type(input, "Quokka");
    expect(canvas.getByTestId("validation-package-selector-checkbox-MySummaryPackage")).toBeInTheDocument();
    expect(canvas.queryByTestId("validation-package-selector-checkbox-Package00")).not.toBeInTheDocument();
  },
};

export const TagFilterNarrowsRows: Story = {
  render: () => <ValidationPackageSelectorFixture />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await loadedPackage(canvas);
    await userEvent.click(canvas.getByTestId("validation-package-selector-tag-filter"));
    await selectDropdownOption("Invenio");
    expect(canvas.getByTestId("validation-package-selector-checkbox-Invenio")).toBeInTheDocument();
    expect(canvas.queryByTestId("validation-package-selector-checkbox-Package00")).not.toBeInTheDocument();
    expect(canvas.getByTestId("validation-package-selector-page-indicator")).toHaveTextContent("Page 1 of 1");
  },
};

export const FilterChangeResetsPage: Story = {
  render: () => <ValidationPackageSelectorFixture />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await loadedPackage(canvas);
    await userEvent.click(canvas.getByTestId("validation-package-selector-next"));
    expect(canvas.getByTestId("validation-package-selector-page-indicator")).toHaveTextContent("Page 2 of 2");
    const input = canvas.getByTestId("validation-package-selector-search");
    await userEvent.type(input, "Package00");
    expect(canvas.getByTestId("validation-package-selector-page-indicator")).toHaveTextContent("Page 1 of 1");
  },
};

export const ToggleAndSubmitWritesConfig: Story = {
  render: () => <ValidationPackageSelectorFixture />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await loadedPackage(canvas);
    const submit = canvas.getByTestId("validation-package-selector-submit");
    expect(submit).toBeDisabled();
    await userEvent.click(canvas.getByTestId("validation-package-selector-checkbox-Package00"));
    expect(submit).toBeEnabled();
    await userEvent.click(submit);
    await waitFor(() => expect(configValue(canvas)).toContain("Package00"));
    await waitFor(() =>
      expect(canvas.getByTestId("validation-package-selector-submit")).toBeDisabled()
    );
  },
};

export const UpdateToLatestChangesVersion: Story = {
  render: () => <ValidationPackageSelectorFixture />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await loadedPackage(canvas);
    await userEvent.click(canvas.getByTestId("validation-package-selector-next"));
    const updateBtn = canvas.getByTestId("validation-package-selector-update-Invenio");
    await userEvent.click(updateBtn);
    await userEvent.click(canvas.getByTestId("validation-package-selector-submit"));
    await waitFor(() => expect(configValue(canvas)).toContain("Version = 1.0.0"));
    expect(configValue(canvas)).not.toContain("0.9.0");
  },
};

export const UnlistedBannerCanRemovePackages: Story = {
  render: () => <ValidationPackageSelectorFixture />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await loadedPackage(canvas);
    const banner = canvas.getByTestId("validation-package-selector-unlisted-banner");
    expect(banner).toHaveTextContent("1 Packages in current config not available online");
    await userEvent.click(canvas.getByText("Show"));
    await userEvent.click(canvas.getByTestId("validation-package-selector-remove-unlisted-LegacyPackage"));
    expect(canvas.queryByTestId("validation-package-selector-unlisted-banner")).not.toBeInTheDocument();
    await userEvent.click(canvas.getByTestId("validation-package-selector-submit"));
    await waitFor(() => expect(configValue(canvas)).not.toContain("LegacyPackage"));
  },
};
