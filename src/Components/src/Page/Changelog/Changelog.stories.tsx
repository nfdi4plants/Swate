import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, userEvent, within } from 'storybook/test';
import Changelog from './Changelog.fs';
import ChangelogSample from './Changelog.sample.fs';

const meta = {
  title: 'Page Components/Changelog',
  component: ChangelogSample,
  parameters: { layout: 'fullscreen' },
  args: { currentRelease: '1.0.0' },
} satisfies Meta<typeof ChangelogSample>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByRole('status')).toHaveTextContent('Loading release notes...');
    await expect(canvas.getByRole('checkbox', { name: 'Use GitHub releases' })).toBeDisabled();
    await expect(await canvas.findByRole('heading', { name: 'Version 1.0.0' }, { timeout: 5000 })).toBeVisible();
    await expect(canvas.getByRole('button', { name: 'Next', exact: true })).toBeDisabled();
    await expect(canvas.getByText('2 / 2')).toBeVisible();
    await expect(canvas.getByText('The first release.')).toBeVisible();
    await userEvent.click(canvas.getByRole('button', { name: 'Previous' }));
    await expect(canvas.getByRole('heading', { name: 'Version 1.1.0' })).toBeVisible();
    await expect(canvas.getByRole('button', { name: 'Previous' })).toBeDisabled();
    await userEvent.click(canvas.getByRole('button', { name: 'Next', exact: true }));
    await expect(canvas.getByRole('heading', { name: 'Version 1.0.0' })).toBeVisible();
    await userEvent.selectOptions(canvas.getByRole('combobox', { name: 'Release version' }), '0');
    await expect(canvas.getByRole('heading', { name: 'Version 1.1.0' })).toBeVisible();
    await userEvent.type(canvas.getByRole('textbox', { name: 'GitHub repository' }), 'nfdi4plants/Swate');
    await expect(canvas.getByRole('checkbox', { name: 'Use GitHub releases' })).toBeEnabled();
    // Leave live fetching to manual experimentation; this story stays deterministic.
    await expect(canvas.getByRole('checkbox', { name: 'Use GitHub releases' })).not.toBeChecked();
  },
};

export const MissingRelease: Story = {
  args: { currentRelease: '0.9.0' },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(await canvas.findByRole('heading', { name: 'Version 1.1.0' }, { timeout: 5000 })).toBeVisible();
  },
};

const failedFetch = async () => { throw new Error('Release service unavailable.'); };

export const FetchError: Story = {
  render: () => <Changelog fetchReleases={failedFetch} currentRelease="1.0.0" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(await canvas.findByRole('alert')).toHaveTextContent('Release service unavailable.');
    await expect(canvas.getByRole('button', { name: 'Retry' })).toBeVisible();
  },
};

const emptyFetch = async () => [];

export const EmptyReleases: Story = {
  render: () => <Changelog fetchReleases={emptyFetch} currentRelease="1.0.0" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(await canvas.findByText('No published releases found.')).toBeVisible();
    await expect(canvas.queryByRole('navigation', { name: 'Changelog pagination' })).not.toBeInTheDocument();
  },
};
