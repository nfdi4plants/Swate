import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor, screen } from 'storybook/test';
import {
  SingleEntry,
  QueuedEntry,
  CancelableEntry,
  BatchEntry,
} from "./ErrorModalProvider.fs.js";

const meta = {
  title: "Components/ErrorModal",
  tags: ["autodocs"],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' }
  },
  component: SingleEntry,
} satisfies Meta<typeof SingleEntry>;

export default meta;

type Story = StoryObj<typeof meta>;

export const SingleError: Story = {
  render: () => <SingleEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.queryByRole("button", { name: /queue errors/i })).not.toBeInTheDocument();
    expect(canvas.queryByRole("button", { name: /show cancelable error/i })).not.toBeInTheDocument();
    expect(canvas.queryByRole("button", { name: /show multiple errors/i })).not.toBeInTheDocument();

    await userEvent.click(canvas.getByRole("button", { name: /show error/i }));

    expect(await screen.findByText("Sample runtime error")).toBeInTheDocument();
    expect(screen.getByText("The renderer could not finish the requested operation.")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /^ok$/i }));

    await waitFor(() => {
      expect(screen.queryByText("Sample runtime error")).not.toBeInTheDocument();
    });
  }
};

export const QueuedErrors: Story = {
  render: () => <QueuedEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.queryByRole("button", { name: /show error/i })).not.toBeInTheDocument();
    expect(canvas.queryByRole("button", { name: /show cancelable error/i })).not.toBeInTheDocument();
    expect(canvas.queryByRole("button", { name: /show multiple errors/i })).not.toBeInTheDocument();

    await userEvent.click(canvas.getByRole("button", { name: /queue errors/i }));

    expect(await screen.findByText("Queued error 1")).toBeInTheDocument();
    expect(screen.getByText("There are 1 more error modal(s) waiting.")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /^dismiss all errors$/i }));

    await waitFor(() => {
      expect(screen.queryByText("Queued error 1")).not.toBeInTheDocument();
    });
  }
};

export const CancelableError: Story = {
  render: () => <CancelableEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.queryByRole("button", { name: /show error/i })).not.toBeInTheDocument();
    expect(canvas.queryByRole("button", { name: /queue errors/i })).not.toBeInTheDocument();
    expect(canvas.queryByRole("button", { name: /show multiple errors/i })).not.toBeInTheDocument();

    await userEvent.click(canvas.getByRole("button", { name: /show cancelable error/i }));

    expect(await screen.findByText("Cancelable error")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /^cancel$/i })).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /^cancel$/i }));

    await waitFor(() => {
      expect(screen.queryByText("Cancelable error")).not.toBeInTheDocument();
    });
  }
};

export const MultipleErrorsAtOnce: Story = {
  render: () => <BatchEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.queryByRole("button", { name: /show error/i })).not.toBeInTheDocument();
    expect(canvas.queryByRole("button", { name: /queue errors/i })).not.toBeInTheDocument();
    expect(canvas.queryByRole("button", { name: /show cancelable error/i })).not.toBeInTheDocument();

    await userEvent.click(canvas.getByRole("button", { name: /show multiple errors/i }));

    expect(await screen.findByText("Multiple errors at once")).toBeInTheDocument();
    expect(screen.getByText("Visible error 1")).toBeInTheDocument();
    expect(screen.getByText("Visible error 2")).toBeInTheDocument();
    expect(screen.getByText("Visible error 3")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /dismiss error 2/i }));

    await waitFor(() => {
      expect(screen.queryByText("Visible error 2")).not.toBeInTheDocument();
    });

    expect(screen.getByText("Visible error 1")).toBeInTheDocument();
    expect(screen.getByText("Visible error 3")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /dismiss visible errors/i }));

    await waitFor(() => {
      expect(screen.queryByText("Multiple errors at once")).not.toBeInTheDocument();
    });
  }
};
