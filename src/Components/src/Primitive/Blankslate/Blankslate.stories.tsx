import type { Meta, StoryObj } from '@storybook/react-vite';
import React, { useState } from 'react';
import { expect, userEvent, within } from 'storybook/test';
import { Blankslate } from './Blankslate.fs.js';
import { ofArray } from '../../fable_modules/fable-library-ts.5.0.0-alpha.21/List.ts';

const BasicExample = () => (
  <div className="swt:w-[420px] swt:max-w-full">
    <Blankslate
      title="No files found"
      description="Add files to this workspace to continue."
      iconClassName="swt:fluent--document-folder-24-regular"
      fullHeight={false}
      testId="BlankslateBasic"
    />
  </div>
);

const ActionExample = () => {
  const [lastAction, setLastAction] = useState('none');

  const actions = ofArray([
    {
      Label: 'Create file',
      OnClick: () => setLastAction('Create file'),
      IconClassName: 'swt:fluent--document-add-24-regular',
      Disabled: false,
      Kind: "primary",
    },
    {
      Label: 'Open docs',
      OnClick: () => setLastAction('Open docs'),
      IconClassName: 'swt:fluent--book-information-24-regular',
      Disabled: false,
      Kind:"secondary",
    },
  ]) as any;

  return (
    <div className="swt:w-[420px] swt:max-w-full swt:space-y-3">
      <Blankslate
        title="Repository is empty"
        description="Create the first file or open docs for setup guidance."
        iconClassName="swt:fluent--branch-fork-24-regular"
        textSize="large"
        actions={actions}
        infoText="You need write permissions to push changes."
        fullHeight={false}
      />
      <span>Last action: {lastAction}</span>
    </div>
  );
};

const meta = {
  title: 'Primitive Components/Blankslate',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: BasicExample,
} satisfies Meta<typeof BasicExample>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <BasicExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.getByText('No files found')).toBeInTheDocument();
    expect(canvas.getByText('Add files to this workspace to continue.')).toBeInTheDocument();
  },
};

export const WithActions: Story = {
  render: () => <ActionExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: /create file/i }));

    expect(canvas.getByText('Last action: Create file')).toBeInTheDocument();
    expect(canvas.getByText('You need write permissions to push changes.')).toBeInTheDocument();
  },
};