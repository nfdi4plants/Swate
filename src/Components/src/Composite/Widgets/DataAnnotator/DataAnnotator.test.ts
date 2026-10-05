// @vitest-environment jsdom
import React from 'react';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import DataAnnotator from './DataAnnotator.fs.ts';
import { FSharpResult$2_Ok, FSharpResult$2_Error$ } from '../../../fable_modules/fable-library-ts.5.0.0-alpha.21/Result.ts';
import type { AnnotationInput } from './Types.fs.ts';

// jsdom has no layout; give the real virtualized preview a visible viewport.
beforeEach(() => {
  vi.spyOn(HTMLElement.prototype, 'offsetWidth', 'get').mockReturnValue(800);
  vi.spyOn(HTMLElement.prototype, 'offsetHeight', 'get').mockReturnValue(600);
});

test('inserts highlighted selectors in list order and supports repeated destinations', async () => {
  const submitted: AnnotationInput[] = [];
  const onInsert = (input: AnnotationInput) => {
    submitted.push(input);
    return FSharpResult$2_Ok<number, string>(input.Selectors.length);
  };
  const view = render(React.createElement(DataAnnotator, { onInsert, canInsert: false }));
  await upload();
  await selectTargets('first', 'second', 'third');
  expect(submitted).toHaveLength(0);
  expect((screen.getByRole('button', { name: 'Insert selectors' }) as HTMLButtonElement).disabled).toBe(true);
  view.rerender(React.createElement(DataAnnotator, { onInsert, canInsert: true }));
  fireEvent.click(screen.getByTestId('sortable-list-row-cell=3,1'));
  fireEvent.click(screen.getByTestId('sortable-list-row-cell=2,1'));
  fireEvent.click(screen.getByTestId('sortable-list-down-cell=2,1'));
  fireEvent.click(screen.getByTestId('sortable-list-down-cell=2,1'));
  expect(screen.getByTestId('sortable-list-row-cell=2,1').classList.contains('swt:bg-base-300')).toBe(true);
  fireEvent.click(screen.getByRole('button', { name: 'Insert selected' }));
  expect(submitted[0].Selectors).toEqual(['cell=3,1', 'cell=2,1']);
  expect(submitted[0].FileName).toBe('data.csv');
  fireEvent.click(screen.getByRole('button', { name: 'Insert selected' }));
  expect(submitted).toHaveLength(2);
  expect(screen.getAllByTestId(/^sortable-list-row-/)).toHaveLength(3);
  fireEvent.click(screen.getByRole('button', { name: 'Clear Selected' }));
  fireEvent.click(screen.getByRole('button', { name: 'Insert selectors' }));
  expect(submitted[2].Selectors).toEqual(['cell=2,2']);
});

test('removing a highlighted selector clears its selection and a new file resets the list', async () => {
  render(React.createElement(DataAnnotator));
  await upload();
  await selectTargets('first', 'second');
  fireEvent.click(screen.getByTestId('sortable-list-row-cell=2,1'));
  fireEvent.click(screen.getByTestId('sortable-list-remove-cell=2,1'));
  expect(screen.queryByRole('button', { name: 'Clear Selected' })).toBeNull();
  expect(screen.getByRole('button', { name: 'Clear' })).toBeTruthy();
  await upload('other.csv');
  expect(screen.queryAllByTestId(/^sortable-list-row-/)).toHaveLength(0);
  await selectTargets('third');
  expect(screen.getAllByTestId(/^sortable-list-row-/).map(row => row.textContent)).toEqual(['cell=3,1']);
});

test('shows an insertion error and leaves selectors available for retry', async () => {
  render(React.createElement(DataAnnotator, {
    onInsert: () => FSharpResult$2_Error$<number, string>('Select a destination cell.'),
  }));
  await upload();
  await selectTargets('first');
  fireEvent.click(screen.getByRole('button', { name: 'Insert selectors' }));
  expect(screen.getByRole('alert').textContent).toBe('Select a destination cell.');
  expect(screen.getByTestId('sortable-list-row-cell=2,1')).toBeTruthy();
});
afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

async function upload(name = 'data.csv') {
  const file = new File(['a,b\nfirst,second\nthird,fourth'], name, { type: 'text/csv' });
  // File.text is unavailable in jsdom.
  Object.defineProperty(file, 'text', { value: async () => 'a,b\nfirst,second\nthird,fourth' });
  fireEvent.change(screen.getByLabelText(/Upload a CSV or TSV/), { target: { files: { 0: file, length: 1, item: (index: number) => index === 0 ? file : null } } });
  await screen.findByRole('button', { name: 'Preview and Select Targets' });
}

async function selectTargets(...values: string[]) {
  fireEvent.click(screen.getByRole('button', { name: 'Preview and Select Targets' }));
  for (const value of values) fireEvent.click(await screen.findByText(value, { exact: true }));
  fireEvent.click(screen.getByRole('button', { name: /^(Submit|Add selectors)$/ }));
}

test('adding targets again appends unique selectors and preserves their existing order', async () => {
  render(React.createElement(DataAnnotator));
  await upload();
  await selectTargets('first', 'second');
  fireEvent.click(screen.getByTestId('sortable-list-down-cell=2,1'));
  await selectTargets('first', 'third');
  expect(screen.getAllByTestId(/^sortable-list-row-/).map(row => row.textContent)).toEqual([
    'cell=2,2', 'cell=2,1', 'cell=3,1',
  ]);
});
