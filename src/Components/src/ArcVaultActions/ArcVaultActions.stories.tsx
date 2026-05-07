import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, screen, userEvent, waitFor, within, fn } from 'storybook/test';
import { Entry } from './ArcVaultActions.fs.js';

const meta = {
  title: "Components/ArcVaultActions",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'centered',
  },
  args: {
    onCopyPath: fn(),
    onOpenArcFolder: fn(),
  },
  component: Entry,
} satisfies Meta<typeof Entry>;

export default meta;

type Story = StoryObj<typeof meta>;

const ArcVaultActionsBtn = "arc-vault-actions-btn"
const ArcVaultActionsPathValue = "arc-vault-actions-path-value"
const ArcVaultActionsPathCopy = "arc-vault-actions-path-copy"
const ArcVaultActionsPathOpenFolder = "arc-vault-actions-path-open-folder"

const arcRootPath = "C:\\Users\\User\\ArcVault"

async function openActionsPopover(canvasElement: HTMLElement) {
  const canvas = within(canvasElement)
  const toggle = await canvas.findByTestId(ArcVaultActionsBtn)
  expect(toggle).toBeInTheDocument()
  await userEvent.click(toggle)
}

export const Default: Story = {
  play: async ({ canvasElement }) => {
    await openActionsPopover(canvasElement)
    const pathValue = await screen.findByTestId(ArcVaultActionsPathValue)
    expect(pathValue).toHaveTextContent(arcRootPath)
    await openActionsPopover(canvasElement) // close popover
  }
};

export const CallsOnCopyPath: Story = {
  args: {
    onCopyPath: fn(),
    onOpenArcFolder: fn(),
  },
  play: async ({ canvasElement, args }) => {
    await openActionsPopover(canvasElement)

    const copyPathButton = await screen.findByTestId(ArcVaultActionsPathCopy)
    await userEvent.click(copyPathButton)

    await waitFor(() => {
      expect(args.onCopyPath).toHaveBeenCalledTimes(1)
    })
    expect(args.onCopyPath).toHaveBeenCalledWith(arcRootPath)
  },
};

export const CallsOnOpenArcFolder: Story = {
  args: {
    onCopyPath: fn(),
    onOpenArcFolder: fn(),
  },
  play: async ({ canvasElement, args }) => {
    await openActionsPopover(canvasElement)

    const openFolderButton = await screen.findByTestId(ArcVaultActionsPathOpenFolder)
    await userEvent.click(openFolderButton)

    await waitFor(() => {
      expect(args.onOpenArcFolder).toHaveBeenCalledTimes(1)
    })
  },
};
