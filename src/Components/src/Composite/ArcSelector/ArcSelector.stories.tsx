import React, { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, userEvent, waitFor, within } from 'storybook/test';
import { Main as ArcSelector } from './ArcSelector.fs.js';
import { Main as Actionbar } from '../../Primitive/Actionbar/Actionbar.fs.js';

const recentARCs = [
  { name: 'Test 1', path: '/Here/Test 1', isActive: false },
  { name: 'Test 2', path: '/Here/Test 2', isActive: false },
  { name: 'Test 3', path: '/Here/Test 3', isActive: false },
  {
    name: 'An ARC name that is much too long to fit inside the selector',
    path: '/Here/An ARC name that is much too long to fit inside the selector',
    isActive: false,
  },
];

function ArcSelectorStory({ debug = true }: { debug?: boolean }) {
  const [currentlyOpenArcPath, setCurrentlyOpenArcPath] = useState<string>();
  const [action, setAction] = useState('none');
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <ArcSelector
        recentARCs={recentARCs}
        onClick={arc => setCurrentlyOpenArcPath(arc.path)}
        isOpen={isOpen}
        setIsOpen={setIsOpen}
        currentlyOpenArcPath={currentlyOpenArcPath}
        debug={debug}
        actionbar={(
          <Actionbar
            buttons={[
              {
                icon: 'swt:fluent--document-add-24-regular',
                toolTip: 'Create a new ARC',
                onClick: () => setAction('create'),
              },
              {
                icon: 'swt:fluent--folder-open-24-regular',
                toolTip: 'Open an existing ARC',
                onClick: () => setAction('open'),
              },
            ]}
            maxNumber={1}
            debug={debug}
            onActionInvoked={() => setIsOpen(false)}
            keepContextMenuPortalLocal
          />
        )}
      />
      <output data-testid="story-action-result">{action}</output>
    </>
  );
}

const meta = {
  title: 'Composite Components/ArcSelector',
  tags: ['autodocs'],
  parameters: { layout: 'fullscreen' },
  component: ArcSelectorStory,
} satisfies Meta<typeof ArcSelectorStory>;

export default meta;
type Story = StoryObj<typeof meta>;

export const DisplaySelector: Story = {};

export const ClickingArcPointerUpdatesActiveArc: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const selectorToggle = await canvas.findByTestId('selector-test');
    await userEvent.click(selectorToggle);
    await userEvent.click(await canvas.findByTestId('selector-arc-item-1'));
    await waitFor(() => expect(selectorToggle).toHaveTextContent('Test 2'));
  },
};

export const LongArcNamesAreTruncatedWithoutResizing: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const selectorToggle = await canvas.findByTestId('selector-test');
    const initialWidth = selectorToggle.getBoundingClientRect().width;

    await userEvent.click(selectorToggle);
    await userEvent.click(await canvas.findByTestId('selector-arc-item-3'));

    await waitFor(() => {
      const currentName = canvas.getByTestId('selector-current-arc-name');
      expect(selectorToggle.getBoundingClientRect().width).toBe(initialWidth);
      expect(currentName.scrollWidth).toBeGreaterThan(currentName.clientWidth);
    });
  },
};

export const ClickingArcPointerClosesDropdown: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(await canvas.findByTestId('selector-test'));
    await userEvent.click(await canvas.findByTestId('selector-arc-item-0'));
    await waitFor(() =>
      expect(canvas.queryByTestId('selector-dropdown-content')).not.toBeInTheDocument(),
    );
  },
};

export const ClickingActionClosesDropdown: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(await canvas.findByTestId('selector-test'));
    await userEvent.click(await canvas.findByTestId('button-test'));
    await waitFor(() => {
      expect(canvas.getByTestId('story-action-result')).toHaveTextContent('create');
      expect(canvas.queryByTestId('selector-dropdown-content')).not.toBeInTheDocument();
    });
  },
};

export const RestButtonShowsOptionsAndOptionsClickable: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(await canvas.findByTestId('selector-test'));
    await userEvent.click(await canvas.findByTestId('actionbar-rest-button'));

    expect(canvas.getByTestId('selector-dropdown-content')).toBeInTheDocument();

    const contextMenu = await within(document.body).findByTestId('context_menu');
    await userEvent.click(within(contextMenu).getByRole('button', { name: /open an existing arc/i }));

    await waitFor(() => {
      expect(canvas.getByTestId('story-action-result')).toHaveTextContent('open');
      expect(canvas.queryByTestId('selector-dropdown-content')).not.toBeInTheDocument();
    });
  },
};
