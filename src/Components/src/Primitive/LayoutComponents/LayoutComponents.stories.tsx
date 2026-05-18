import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, userEvent, within } from 'storybook/test';
import {
  LayoutComponents_BoxedField_Z142863CA as BoxedField,
  LayoutComponents_Collapse as Collapse,
  LayoutComponents_CollapseTitle_Z2FC25A28 as CollapseTitle,
  LayoutComponents_FieldTitle_Z721C83C5 as FieldTitle,
  LayoutComponents_Section_701EED78 as Section,
} from './LayoutComponents.fs.js';

const LayoutComponentsExample = () => (
  <Section>
    {[
      <div key="layout-main" className="swt:w-full swt:max-w-2xl swt:space-y-4">
        <FieldTitle title="Metadata" />
        <BoxedField title="Study Information" description="Basic metadata details" />
        <Collapse
          title={[
            <CollapseTitle
              key="collapse-title"
              title="Advanced settings"
              subtitle="Optional controls"
            />,
          ]}
          content={[
            <p key="collapse-content">Hidden content</p>,
          ]}
        />
      </div>,
    ]}
  </Section>
);

const meta = {
  title: 'Primitive Components/LayoutComponents',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: LayoutComponentsExample,
} satisfies Meta<typeof LayoutComponentsExample>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <LayoutComponentsExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const checkbox = canvas.getByRole('checkbox');

    expect(checkbox).not.toBeChecked();
    await userEvent.click(checkbox);

    expect(checkbox).toBeChecked();
    expect(canvas.getByText(/hidden content/i)).toBeInTheDocument();
  },
};
