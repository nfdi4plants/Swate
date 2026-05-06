import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor } from 'storybook/test';
import { Entry as SettingsPage } from './SettingsPage.fs.js';


const meta = {
  title: 'PageComponents/SettingsPage',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: SettingsPage,
} satisfies Meta<typeof SettingsPage>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};