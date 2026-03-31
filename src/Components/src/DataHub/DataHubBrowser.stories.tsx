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

const readCount = async (canvas: ReturnType<typeof within>, testId: string): Promise<number> => {
  const node = await canvas.findByTestId(testId);
  const text = node.textContent ?? "";
  return Number(text.split(":").pop() ?? "0");
};

const expectMockSignedIn = async (canvas: ReturnType<typeof within>, isSignedIn: boolean) => {
  await waitFor(async () => {
    expect(await canvas.findByTestId("GitLabExploreMockAuthState")).toHaveTextContent(
      `signed-in:${isSignedIn ? "true" : "false"}`,
    );
  });
};

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
    expect(nextButton).toBeDisabled();

    const prevButton = await canvas.findByTestId("GitLabExplorePrevPageButton");
    expect(prevButton).toBeDisabled();
    expect(await canvas.findByTestId("GitLabExplorePageIndicator")).toHaveTextContent("Page 1");
  },
};

export const ClonedRepoVisualAndOpenButton: Story = {
  name: "Cloned repo visual state",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabExploreRepoList")).toBeInTheDocument();
      expect(await canvas.findByTestId("GitLabRepoRow-10")).toBeInTheDocument();
    });

    expect(canvas.queryByText("cloned")).not.toBeInTheDocument();
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

export const MockLoaderRoutingFlow: Story = {
  name: "Mock loader routing flow",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(async () => {
      expect(await readCount(canvas, "GitLabExploreMockCountAll")).toBe(1);
      expect(await readCount(canvas, "GitLabExploreMockCountMostStarred")).toBe(0);
      expect(await readCount(canvas, "GitLabExploreMockCountUserRepos")).toBe(0);
      expect(await readCount(canvas, "GitLabExploreMockCountOrgGroups")).toBe(0);
      expect(await readCount(canvas, "GitLabExploreMockCountOrgRepos")).toBe(0);
    });

    await userEvent.click(await canvas.findByTestId("GitLabExploreTab-MostStarred"));

    await waitFor(async () => {
      expect(await readCount(canvas, "GitLabExploreMockCountMostStarred")).toBe(1);
    });

    await userEvent.click(await canvas.findByTestId("GitLabExploreTab-YourRepos"));
    await userEvent.click(await canvas.findByTestId("GitLabExploreTab-YourOrganisations"));

    await waitFor(async () => {
      expect(await readCount(canvas, "GitLabExploreMockCountUserRepos")).toBe(0);
      expect(await readCount(canvas, "GitLabExploreMockCountOrgGroups")).toBe(0);
      expect(await readCount(canvas, "GitLabExploreMockCountOrgRepos")).toBe(0);
    });

    await userEvent.clear(await canvas.findByTestId("GitLabExploreSearchInput"));
    await userEvent.type(await canvas.findByTestId("GitLabExploreSearchInput"), "ontology");
    await userEvent.click(await canvas.findByTestId("GitLabExploreSearchButton"));

    await waitFor(async () => {
      expect(await readCount(canvas, "GitLabExploreMockCountMostStarred")).toBeGreaterThanOrEqual(1);
    });
  },
};

export const LoggedInStateFlow: Story = {
  name: "Entry wrapper logged-in state flow",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expectMockSignedIn(canvas, false);

    const yourReposTab = await canvas.findByTestId("GitLabExploreTab-YourRepos");
    expect(yourReposTab).toHaveAttribute("aria-disabled", "true");

    await userEvent.click(await canvas.findByTestId("GitLabExploreMockLoginButton"));

    await expectMockSignedIn(canvas, true);

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabExploreTab-YourRepos")).toHaveAttribute("aria-disabled", "false");
      expect(await canvas.findByTestId("GitLabExploreTab-YourOrganisations")).toHaveAttribute("aria-disabled", "false");
    });

    await userEvent.click(await canvas.findByTestId("GitLabExploreTab-YourRepos"));

    await waitFor(async () => {
      expect((await canvas.findByTestId("GitLabExploreTab-YourRepos")).className).toMatch(/tab-active/);
      expect(await readCount(canvas, "GitLabExploreMockCountUserRepos")).toBeGreaterThanOrEqual(1);
    });
  },
};

export const LoginLogoutToggleFlow: Story = {
  name: "Entry wrapper login/logout toggle flow",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expectMockSignedIn(canvas, false);

    await userEvent.click(await canvas.findByTestId("GitLabExploreMockLoginButton"));
    await expectMockSignedIn(canvas, true);

    await userEvent.click(await canvas.findByTestId("GitLabExploreTab-YourOrganisations"));

    await waitFor(async () => {
      expect((await canvas.findByTestId("GitLabExploreTab-YourOrganisations")).className).toMatch(/tab-active/);
    });

    await userEvent.click(await canvas.findByTestId("GitLabExploreMockLogoutButton"));
    await expectMockSignedIn(canvas, false);

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabExploreTab-YourRepos")).toHaveAttribute("aria-disabled", "true");
      expect(await canvas.findByTestId("GitLabExploreTab-YourOrganisations")).toHaveAttribute("aria-disabled", "true");
      expect((await canvas.findByTestId("GitLabExploreTab-All")).className).toMatch(/tab-active/);
    });
  },
};

export const LatestResponseWinsInRaceCondition: Story = {
  name: "Latest request wins in race condition",
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabRepoRow-10")).toBeInTheDocument();
    });

    const searchInput = await canvas.findByTestId("GitLabExploreSearchInput");
    const searchButton = await canvas.findByTestId("GitLabExploreSearchButton");

    await userEvent.clear(searchInput);
    await userEvent.type(searchInput, "ontology __race_slow__");
    await userEvent.click(searchButton);

    await userEvent.clear(searchInput);
    await userEvent.type(searchInput, "arc-spec __race_fast__");
    await userEvent.click(searchButton);

    await waitFor(async () => {
      expect(await canvas.findByTestId("GitLabRepoRow-11")).toBeInTheDocument();
      expect(canvas.queryByTestId("GitLabRepoRow-10")).not.toBeInTheDocument();
      expect(canvas.queryByTestId("GitLabExploreError")).not.toBeInTheDocument();
    });

    await new Promise((resolve) => setTimeout(resolve, 1000));

    expect(await canvas.findByTestId("GitLabRepoRow-11")).toBeInTheDocument();
    expect(canvas.queryByTestId("GitLabRepoRow-10")).not.toBeInTheDocument();
    expect(canvas.queryByTestId("GitLabExploreError")).not.toBeInTheDocument();
    expect(canvas.queryByTestId("GitLabExploreLoading")).not.toBeInTheDocument();
  },
};
