import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, fireEvent, userEvent, waitFor, within } from 'storybook/test';
import { Main as TutorialOverlay } from './TutorialOverlay.fs.js';
import {
  Exports_step as step,
  Exports_advanceManual as advanceManual,
  Exports_advanceOnEvent as advanceOnEvent,
  type TutorialStep,
} from './Types.fs.js';

/** Tiny host UI the tutorial walks through. */
function DemoPanel() {
  const [waterCount, setWaterCount] = React.useState(0);
  const [name, setName] = React.useState('');

  return (
    <div className="swt:flex swt:h-full swt:flex-col swt:gap-4 swt:bg-base-200 swt:p-6">
      <h2 className="swt:text-lg swt:font-semibold">Plant care demo</h2>
      <button
        type="button"
        data-tutorial="demo-water"
        className="swt:btn swt:btn-primary swt:w-fit"
        onClick={() => setWaterCount((count) => count + 1)}
      >
        Water plant ({waterCount})
      </button>
      <label className="swt:flex swt:w-fit swt:flex-col swt:gap-1">
        <span className="swt:text-sm">Plant name</span>
        <input
          data-tutorial="demo-name"
          className="swt:input swt:input-bordered swt:input-sm"
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
      </label>
    </div>
  );
}

const demoSteps: TutorialStep[] = [
  step(
    'intro',
    'Welcome',
    'This short tour shows the demo panel. Use the list on the right to jump around.',
    undefined,
    undefined,
    advanceManual(),
    undefined,
  ),
  // Each task step names its own checkpoint: entering it (from any direction)
  // remounts the demo panel, so the task always starts from a known state.
  step(
    'water',
    'Watering',
    'The highlighted button waters the plant and counts how often you did.',
    "[data-tutorial='demo-water']",
    'Click the Water plant button.',
    advanceOnEvent('click', "[data-tutorial='demo-water']"),
    'water-task',
  ),
  step(
    'name',
    'Naming',
    'Every plant deserves a name; type one into the highlighted field.',
    "[data-tutorial='demo-name']",
    undefined,
    advanceManual(),
    'name-task',
  ),
  step(
    'outro',
    'Done',
    'That is all there is - happy planting!',
    undefined,
    undefined,
    advanceManual(),
    undefined,
  ),
];

function Harness() {
  const [closed, setClosed] = React.useState(false);

  if (closed) {
    return <p data-testid="tutorial-closed">Tutorial closed</p>;
  }

  return (
    <div className="swt:h-screen">
      <TutorialOverlay
        steps={demoSteps}
        onClose={() => setClosed(true)}
        title="Plant care tour"
        debug={true}
        render={() => <DemoPanel />}
      />
    </div>
  );
}

const meta = {
  title: 'Composite Components/TutorialOverlay',
  component: TutorialOverlay,
  parameters: { layout: 'fullscreen' },
} satisfies Meta<typeof TutorialOverlay>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => <Harness />,
};

export const SidebarListsStepsAndJumpsDirectly: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const sidebar = within(await canvas.findByTestId('tutorial-sidebar'));
    expect(sidebar.getByText('Plant care tour')).toBeInTheDocument();
    expect(sidebar.getByText('Welcome')).toBeInTheDocument();
    expect(sidebar.getByText('0 of 4 features explored')).toBeInTheDocument();

    // Jumping straight to a feature explanation from the list.
    await userEvent.click(canvas.getByTestId('tutorial-sidebar-step-name'));
    const card = within(canvas.getByTestId('tutorial-step-card'));
    expect(card.getByText('Naming')).toBeInTheDocument();
    expect(card.getByText('Step 3 of 4')).toBeInTheDocument();

    // The spotlight tracks the step target.
    await waitFor(() => expect(canvas.getByTestId('tutorial-spotlight')).toBeInTheDocument());
  },
};

export const TaskStepCompletesOnUserInteraction: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(await canvas.findByTestId('tutorial-sidebar-step-water'));
    const task = within(canvas.getByTestId('tutorial-task'));
    expect(task.getByText(/Click the Water plant button/)).toBeInTheDocument();
    expect(task.getByText('Try it:')).toBeInTheDocument();
    expect(canvas.getByTestId('tutorial-next')).toHaveTextContent('Skip');

    // The highlighted control stays interactive; performing the task marks
    // the step completed without moving on by itself.
    await waitFor(() => expect(canvas.getByTestId('tutorial-spotlight')).toBeInTheDocument());
    await userEvent.click(canvas.getByText(/Water plant \(0\)/));
    expect(canvas.getByText(/Water plant \(1\)/)).toBeInTheDocument();

    await waitFor(() => expect(canvas.getByTestId('tutorial-next')).toHaveTextContent('Next'));
    expect(within(canvas.getByTestId('tutorial-step-card')).getByText('Watering')).toBeInTheDocument();
    expect(within(canvas.getByTestId('tutorial-task')).getByText('Completed:')).toBeInTheDocument();
    expect(canvas.getByText('1 of 4 features explored')).toBeInTheDocument();

    // The user moves on themselves.
    await userEvent.click(canvas.getByTestId('tutorial-next'));
    expect(within(canvas.getByTestId('tutorial-step-card')).getByText('Naming')).toBeInTheDocument();
  },
};

export const SkipMovesOnAndCloseExits: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const card = within(await canvas.findByTestId('tutorial-step-card'));
    expect(card.getByText('Welcome')).toBeInTheDocument();

    // Manual step advances via Next, task step via Skip.
    await userEvent.click(canvas.getByTestId('tutorial-next'));
    expect(card.getByText('Watering')).toBeInTheDocument();
    expect(canvas.getByTestId('tutorial-next')).toHaveTextContent('Skip');
    await userEvent.click(canvas.getByTestId('tutorial-next'));
    expect(card.getByText('Naming')).toBeInTheDocument();

    await userEvent.click(canvas.getByTestId('tutorial-close'));
    expect(canvas.getByTestId('tutorial-closed')).toBeInTheDocument();
  },
};

export const RevisitingATaskStepRestoresItsCheckpoint: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    // Complete the watering task, changing the underlying demo state.
    await userEvent.click(await canvas.findByTestId('tutorial-sidebar-step-water'));
    await userEvent.click(canvas.getByText(/Water plant \(0\)/));
    expect(canvas.getByText(/Water plant \(1\)/)).toBeInTheDocument();

    // Leaving for a step with another checkpoint and coming back remounts the
    // content at the watering step's checkpoint: the task is doable again.
    await userEvent.click(canvas.getByTestId('tutorial-next'));
    expect(within(canvas.getByTestId('tutorial-step-card')).getByText('Naming')).toBeInTheDocument();
    await userEvent.click(canvas.getByTestId('tutorial-back'));
    expect(canvas.getByText(/Water plant \(0\)/)).toBeInTheDocument();

    // Steps without their own checkpoint keep the running state: moving from
    // Naming to Done (which inherits Naming's checkpoint) preserves the input.
    await userEvent.click(canvas.getByTestId('tutorial-sidebar-step-name'));
    await userEvent.type(canvas.getByTestId('tutorial-overlay').querySelector('input')!, 'Fern');
    await userEvent.click(canvas.getByTestId('tutorial-next'));
    expect(within(canvas.getByTestId('tutorial-step-card')).getByText('Done')).toBeInTheDocument();
    expect(canvas.getByDisplayValue('Fern')).toBeInTheDocument();
  },
};

export const StepCardIsDraggable: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const card = await canvas.findByTestId('tutorial-step-card');
    const handle = canvas.getByTestId('tutorial-card-handle');
    const before = card.getBoundingClientRect();

    const handleRect = handle.getBoundingClientRect();
    const startX = handleRect.left + 10;
    const startY = handleRect.top + 5;
    fireEvent.pointerDown(handle, { pointerId: 1, clientX: startX, clientY: startY });
    fireEvent.pointerMove(handle, { pointerId: 1, clientX: startX + 120, clientY: startY + 80 });
    fireEvent.pointerUp(handle, { pointerId: 1, clientX: startX + 120, clientY: startY + 80 });

    await waitFor(() => {
      const after = card.getBoundingClientRect();
      expect(after.left).toBeCloseTo(before.left + 120, 0);
      expect(after.top).toBeCloseTo(before.top + 80, 0);
    });
  },
};

export const PlayFromStartRestartsTheTour: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    // Make some progress first: skip past Welcome, then jump to the end.
    await userEvent.click(await canvas.findByTestId('tutorial-next'));
    await userEvent.click(canvas.getByTestId('tutorial-sidebar-step-outro'));
    expect(canvas.getByText('1 of 4 features explored')).toBeInTheDocument();

    // Restarting returns to the first step and resets the progress.
    await userEvent.click(canvas.getByTestId('tutorial-play'));
    const card = within(canvas.getByTestId('tutorial-step-card'));
    expect(card.getByText('Welcome')).toBeInTheDocument();
    expect(canvas.getByText('0 of 4 features explored')).toBeInTheDocument();
  },
};
