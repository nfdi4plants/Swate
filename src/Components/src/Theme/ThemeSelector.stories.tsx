import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor } from 'storybook/test';
import { Entry as ThemeSelector } from './ThemeSelector.fs.js';


const meta = {
  title: 'CompositeComponents/ThemeSelector',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
  },
  component: ThemeSelector,
} satisfies Meta<typeof ThemeSelector>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {};