import React, { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, userEvent, waitFor, within } from 'storybook/test';
import { PersonsInput } from './PersonsInput.fs.ts';
import { Person } from '../../../../fable_modules/ARCtrl.Core.3.0.0-beta.12/Person.fs.ts';

const orcid = '0000-0002-1825-0097';
const fullName = 'Jane Smith';

function PersonEditor() {
  const [persons, setPersons] = useState(() => [new Person()]);
  return (
    <div className="swt:max-w-3xl swt:mx-auto swt:p-4">
      <PersonsInput persons={persons} setter={(next: Person[]) => setPersons([...next])} label="Persons" />
    </div>
  );
}

function installPersonSearch() {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async (input) => {
    const url = new URL(String(input));
    if (url.hostname !== 'pub.orcid.org') throw new Error(`Unexpected person search URL: ${url}`);
    if (url.pathname === '/v3.0/expanded-search/') {
      return Response.json({
        'expanded-result': [{
          'orcid-id': orcid, 'given-names': 'Jane', 'family-names': 'Smith',
          'credit-name': null, email: ['jane@example.org'], 'institution-name': ['Plant Research Institute'],
        }],
        'num-found': 1,
      });
    }
    if (url.pathname === `/v3.0/${orcid}/record`) {
      return Response.json({
        'orcid-identifier': { path: orcid },
        person: {
          name: { 'given-names': { value: 'Jane' }, 'family-name': { value: 'Smith' } },
          emails: { email: [{ email: 'jane@example.org', visibility: 'PUBLIC' }] },
        },
        'activities-summary': {
          employments: { 'affiliation-group': [{ summaries: [{ 'employment-summary': { organization: { name: 'Plant Research Institute' } } }] }] },
        },
      });
    }
    throw new Error(`Unexpected ORCID endpoint: ${url}`);
  };
  return () => { globalThis.fetch = originalFetch; };
}

const meta = {
  title: 'Page Components/Metadata/PersonsInput',
  component: PersonEditor,
  tags: ['autodocs'],
  parameters: { layout: 'padded' },
  beforeEach: installPersonSearch,
} satisfies Meta<typeof PersonEditor>;

export default meta;
type Story = StoryObj<typeof meta>;

async function openSearch(canvasElement: HTMLElement, mode: 'ORCID' | 'Name' = 'ORCID') {
  const canvas = within(canvasElement);
  await userEvent.click(canvas.getAllByRole('checkbox')[0]);
  await userEvent.click(canvas.getByRole('button', { name: mode === 'ORCID' ? 'Search by ORCID' : 'Search by name' }));
  const portal = within(canvasElement.ownerDocument.body);
  return { canvas, portal, dialog: within(await portal.findByRole('dialog', { name: 'Search person' })) };
}

async function previewPerson(canvasElement: HTMLElement, mode: 'ORCID' | 'Name', query: string) {
  const context = await openSearch(canvasElement, mode);
  const { dialog } = context;
  await expect(dialog.getByRole('button', { name: mode, pressed: true })).toBeInTheDocument();
  await userEvent.type(dialog.getByRole('textbox', { name: mode === 'ORCID' ? 'ORCID iD or URL' : 'Person name' }), `${query}{Enter}`);
  await expect(await dialog.findByRole('heading', { name: 'Autofill preview' })).toBeInTheDocument();
  await expect(dialog.getAllByText(fullName)).toHaveLength(1);
  await expect(dialog.queryByRole('button', { name: `Preview ${fullName}` })).not.toBeInTheDocument();
  return context;
}

async function confirmPerson(canvasElement: HTMLElement, mode: 'ORCID' | 'Name', query: string) {
  const { canvas, portal, dialog } = await previewPerson(canvasElement, mode, query);
  await expect(canvas.queryByDisplayValue('Jane')).not.toBeInTheDocument();
  await userEvent.click(dialog.getByRole('button', { name: 'Apply person' }));
  await waitFor(() => expect(portal.queryByRole('dialog')).not.toBeInTheDocument());
  await expect(canvas.getByDisplayValue('Jane')).toBeInTheDocument();
  await expect(canvas.getByDisplayValue('Smith')).toBeInTheDocument();
  await expect(canvas.getByDisplayValue(orcid)).toBeInTheDocument();
  await expect(canvas.getByDisplayValue('jane@example.org')).toBeInTheDocument();
  await expect(canvas.getByDisplayValue('Plant Research Institute')).toBeInTheDocument();
  await expect(canvas.getByRole('heading', { name: fullName })).toBeInTheDocument();
}

export const ManualEntry: Story = {};

export const OrcidIdLookup: Story = {
  play: async ({ canvasElement }) => confirmPerson(canvasElement, 'ORCID', orcid),
};

export const OrcidUrlLookup: Story = {
  play: async ({ canvasElement }) => confirmPerson(canvasElement, 'ORCID', `https://orcid.org/${orcid}`),
};

export const NameSearch: Story = {
  play: async ({ canvasElement }) => confirmPerson(canvasElement, 'Name', fullName),
};

export const CancelPreview: Story = {
  play: async ({ canvasElement }) => {
    const { canvas, portal, dialog } = await previewPerson(canvasElement, 'ORCID', orcid);
    await userEvent.click(dialog.getByRole('button', { name: 'Cancel' }));
    await waitFor(() => expect(portal.queryByRole('dialog')).not.toBeInTheDocument());
    await expect(canvas.queryByDisplayValue('Jane')).not.toBeInTheDocument();
    await expect(canvas.getByRole('heading', { name: '<name>' })).toBeInTheDocument();
  },
};
