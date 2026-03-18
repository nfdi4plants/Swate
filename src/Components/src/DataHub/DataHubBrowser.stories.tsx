import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, userEvent, waitFor } from "storybook/test";
import { Entry as DataHubBrowserEntry } from "./DataHubBrowser.fs.js";

const meta: Meta<typeof DataHubBrowserEntry> = {
  title: "Components/DataHubBrowser",
  component: DataHubBrowserEntry,
  tags: ["autodocs"],
  parameters: {
    layout: "centered",
  },
  decorators: [
    (Story) => (
      <div style={{ width: 920, border: "1px solid #333", borderRadius: 8, overflow: "hidden" }}>
        <Story />
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof DataHubBrowserEntry>;

export const Default: Story = {
  name: "Default",
};

export const TabSwitchFlow: Story = {
  name: "Tab switch flow",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const allTab = await canvas.findByTestId("GitLabExploreTab-All");
    expect(allTab.className).toMatch(/tab-active/);

    const mostStarredTab = await canvas.findByTestId("GitLabExploreTab-MostStarred");
    await userEvent.click(mostStarredTab);

    await waitFor(async () => {
      expect((await canvas.findByTestId("GitLabExploreTab-MostStarred")).className).toMatch(/tab-active/);
      expect(await canvas.findByTestId("GitLabRepoRow-10")).toBeInTheDocument();
    });

    const yourReposTab = await canvas.findByTestId("GitLabExploreTab-YourRepos");
    await userEvent.click(yourReposTab);

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabExploreTab-YourRepos")).toHaveAttribute("aria-disabled", "true");
      expect((await canvas.findByTestId("GitLabExploreTab-MostStarred")).className).toMatch(/tab-active/);
    });
  },
};

export const SearchCombinedWithTabFilter: Story = {
  name: "Search combined with tab filter",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const searchInput = await canvas.findByTestId("GitLabExploreSearchInput");
    await userEvent.clear(searchInput);
    await userEvent.type(searchInput, "ontology");
    await userEvent.click(await canvas.findByTestId("GitLabExploreSearchButton"));

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabRepoRow-10")).toBeInTheDocument();
    });

    const mostStarredTab = await canvas.findByTestId("GitLabExploreTab-MostStarred");
    await userEvent.click(mostStarredTab);

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabRepoRow-10")).toBeInTheDocument();
    });
  },
};

export const OrganisationFilteringFlow: Story = {
  name: "Organisation tab restricted when logged out",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const orgTab = await canvas.findByTestId("GitLabExploreTab-YourOrganisations");
    expect(orgTab).toHaveAttribute("aria-disabled", "true");
    await userEvent.click(orgTab);

    await waitFor(async () => {
      expect((await canvas.findByTestId("GitLabExploreTab-All")).className).toMatch(/tab-active/);
    });
  },
};

export const PaginationFlow: Story = {
  name: "Pagination flow",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(await canvas.findByTestId("GitLabExplorePageIndicator")).toHaveTextContent("Page 1");

    const nextButton = await canvas.findByTestId("GitLabExploreNextPageButton");
    await userEvent.click(nextButton);

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabExplorePageIndicator")).toHaveTextContent("Page 2");
    });

    const prevButton = await canvas.findByTestId("GitLabExplorePrevPageButton");
    await userEvent.click(prevButton);

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabExplorePageIndicator")).toHaveTextContent("Page 1");
    });
  },
};

export const ClonedRepoVisualAndOpenButton: Story = {
  name: "Cloned repo visual state",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(async () => {
      expect(await canvas.findByText("cloned")).toBeInTheDocument();
    });
  },
};

export const EmptyStateFlow: Story = {
  name: "Empty state flow",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const searchInput = await canvas.findByTestId("GitLabExploreSearchInput");
    await userEvent.clear(searchInput);
    await userEvent.type(searchInput, "zzzz-no-match");
    await userEvent.click(await canvas.findByTestId("GitLabExploreSearchButton"));

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabExploreEmpty")).toBeInTheDocument();
    });
  },
};
