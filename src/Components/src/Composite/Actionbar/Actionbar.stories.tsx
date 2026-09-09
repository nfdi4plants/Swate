import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, screen, userEvent, waitFor, within } from 'storybook/test';
import { Main as Actionbar, Entry as ActionbarEntry } from './Actionbar.fs.js';

function InteractiveActionbar() {
  const [action, setAction] = useState('none');

  return (
    <>
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
        maxNumber={0}
        debug
      />
      <output data-testid="action-result">{action}</output>
    </>
  );
}

const meta = {
  title: "Composite Components/Actionbar",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'fullscreen',
  },
  component: ActionbarEntry,
} satisfies Meta<typeof ActionbarEntry>;

export default meta;

type Story = StoryObj<typeof meta>;

export const DisplayActionbar: Story = {
  args: {
    maxNumber: 3,
    debug: true,
  }
}

export const DisplayActionbarWithoutRestElementButton: Story = {
  args: {
    maxNumber: 5,
    debug: true
  }
}

export const KeyboardOpensOverflowAndSelectsAction: Story = {
  render: () => <InteractiveActionbar />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const trigger = canvas.getByTestId('actionbar-rest-button');

    trigger.focus();
    await userEvent.keyboard('{Enter}');

    const menu = await screen.findByRole('menu');
    expect(menu).toBeInTheDocument();

    await userEvent.keyboard('o{Enter}');
    await waitFor(() => {
      expect(canvas.getByTestId('action-result')).toHaveTextContent('open');
      expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    });
  },
};
