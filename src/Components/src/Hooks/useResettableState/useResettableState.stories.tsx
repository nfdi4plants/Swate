import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, userEvent, within } from 'storybook/test';
import { SamplePaginated, SampleDatasetSelector } from './useResettableState.sample.fs.js';

const meta = {
    title: 'Hooks/useResettableState',
    parameters: {
        layout: 'centered',
    },
    component: SamplePaginated,
} satisfies Meta<typeof SamplePaginated>;

export default meta;

type Story = StoryObj<typeof meta>;

// Overload 1: (initialValue, key) — pagination resets whenever the dataset
// (a randomly generated list with random length) is replaced.
export const PaginatedListReset: Story = {
    render: () => <SamplePaginated />,
    play: async ({ canvasElement }) => {
        const canvas = within(canvasElement);
        const page = () => canvas.getByTestId('page');

        expect(page()).toHaveTextContent('1');

        await userEvent.click(canvas.getByRole('button', { name: 'Next' }));
        await userEvent.click(canvas.getByRole('button', { name: 'Next' }));
        expect(page()).toHaveTextContent('3');

        // New random dataset (length 1-8) -> pagination resets to page 1.
        await userEvent.click(canvas.getByRole('button', { name: 'New Random Dataset' }));
        expect(page()).toHaveTextContent('1');
    },
};

// Overload 2: (initialValue, dependency, compareFn) — row selection resets
// when the dataset id changes, but survives a reload of the same dataset
// (a new object instance with the same id).
export const DatasetSelectionReset: Story = {
    render: () => <SampleDatasetSelector />,
    play: async ({ canvasElement }) => {
        const canvas = within(canvasElement);
        const selectedRow = () => canvas.getByTestId('selected-row');

        expect(canvas.getByTestId('dataset-name')).toHaveTextContent('Dataset A (v0)');
        expect(selectedRow()).toHaveTextContent('undefined');

        await userEvent.click(canvas.getByRole('button', { name: 'Gamma' }));
        expect(selectedRow()).toHaveTextContent('Gamma');

        // Same dataset id, new object instance -> selection is kept.
        await userEvent.click(canvas.getByRole('button', { name: 'Reload Dataset A' }));
        expect(canvas.getByTestId('dataset-name')).toHaveTextContent('Dataset A (v1)');
        expect(selectedRow()).toHaveTextContent('Gamma');

        // Different dataset id -> selection resets to the first row.
        await userEvent.click(canvas.getByRole('button', { name: 'Load Dataset B' }));
        expect(canvas.getByTestId('dataset-name')).toHaveTextContent('Dataset B (v0)');
        expect(selectedRow()).toHaveTextContent('undefined');
    },
};
