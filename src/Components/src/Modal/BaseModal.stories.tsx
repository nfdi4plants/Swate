import type { Meta, StoryObj } from '@storybook/react';
import { within, expect, userEvent, waitFor } from '@storybook/test';
import BaseModal from "./BaseModal.fs.js";
import { useState } from 'react';
import React from 'react';

interface ButtonProps {
  className?: string;
  style?: React.CSSProperties;
  children: React.ReactNode; // Accepts text, icons, or elements
  onClick?: () => void;
}

const BaseButton: React.FC<ButtonProps> = ({ className, style, children, onClick }) => {
  return (
    <button
      className={className}
      style={style}
      onClick={onClick}>
      {children}
    </button>
  );
};

const ButtonWithModal = ({header, modalClassInfo, modalActivity, content}) => {
  const [open, setOpen] = useState(false);
  const rmv=() => setOpen(false) // Close modal when needed
  const openModal=() => setOpen(true) // Close modal when needed
  const submitButton =
    <BaseButton
      className={"btn btn-primary"}
      style={{ marginLeft: "auto"}}
      onClick={rmv}
    >Submit</BaseButton>
  return (
    <div>
      {
        <BaseButton
          className={"btn btn-primary"}
          style={{ marginLeft: "auto"}}
          onClick={openModal}
          >Open Modal</BaseButton>
      }
      {open && (
        <BaseModal
          rmv={rmv}
          header={header}
          modalClassInfo={modalClassInfo}
          modalActivity={modalActivity}
          content={content}
          footer={submitButton}
          debug={true}
        />
      )}
    </div>
  )
}

const meta = {
  title: "Components/BaseModal",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'centered',
  },
  component: ButtonWithModal,
} satisfies Meta<typeof ButtonWithModal>;

export default meta;

type Story = StoryObj<typeof meta>;

const simpleHeader: JSX.Element = <>Simple Header</>;
const modalActivity: JSX.Element = <>Simple Modal Activity</>;
const list: JSX.Element[] =
  [ <>Simple Content 0</>, <>Simple Content 1</>, <>Simple Content 2</>, <>Simple Content 3</>, <>Simple Content 4</>,
    <>Simple Content 5</>, <>Simple Content 6</>, <>Simple Content 7</>, <>Simple Content 8</>, <>Simple Content 9</>,
    <>Simple Content 10</>, <>Simple Content 11</>, <>Simple Content 12</>, <>Simple Content 13</>, <>Simple Content 14</>
  ];
const content =
  list.map((item, index) => (
      <div key={index} style={{ padding: "8px", borderBottom: "1px solid #ddd" }}>
          {item}
        </div>
      ));
const modalClassInfo: string = "max-w-none";

export const CompleteModal: Story = {
  args: {
    header: simpleHeader,
    modalClassInfo: undefined,
    modalActivity: modalActivity,
    content: content
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Find the button and click to open the modal
    const button = canvas.getByRole("button", { name: /open modal/i });
    await userEvent.click(button);

    // Wait for the modal content to load and check for an item in content
    const modalContent = await canvas.getByTestId('modal-content');

    // Verify that the modal is open (checking for modal content)
    const item0 = canvas.getByText("Simple Content 0", { selector: 'div' });
    const item1 = canvas.getByText("Simple Content 14", { selector: 'div' });

    expect(item0).toBeInTheDocument();
    expect(item1).toBeInTheDocument;

    // Get the element's position in the viewport
    const rect0 = item1.getBoundingClientRect();
    // Ensure that the element is not yet visible (its position should be outside the viewport)
    await waitFor(() => {
      expect(rect0.top).toBeGreaterThan(window.innerHeight); // Element is out of the viewport
    });

    // Scroll the modal content to make sure the item is not initially visible
    const scrollableContainer = canvas.getByTestId('modal-content');
    await userEvent.click(scrollableContainer); // Trigger scroll (if necessary)
    scrollableContainer.scrollTop = 1000; // Manually scroll down

    // Get the element's position in the viewport
    const rect1 = item0.getBoundingClientRect();
    // Ensure that the element is no longer visible (its position should be outside the viewport)
    await waitFor(() => {
      expect(rect1.top).toBeLessThan(window.innerHeight); // Element is out of the viewport
    });

    // Find the submit button (or trigger) and click to close the modal
    const closeButton = canvas.getByRole("button", { name: /Submit/i });
    await userEvent.click(closeButton);

    // Verify that the modal is closed (the content should no longer be in the document)
    await waitFor(() => expect(canvas.queryByText("Simple Content 10")).not.toBeInTheDocument());
  },
}

export const WideCompleteModal: Story = {
  args: {
    header: simpleHeader,
    modalClassInfo: modalClassInfo,
    modalActivity: modalActivity,
    content: content
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Find the button and click to open the modal
    const button = canvas.getByRole("button", { name: /open modal/i });
    await userEvent.click(button);

    // Wait for the modal content to load and check for an item in content
    const modalContent = await canvas.getByTestId('modal-content');

    // Verify that the modal is open (checking for modal content)
    const item0 = canvas.getByText("Simple Content 0", { selector: 'div' });
    const item1 = canvas.getByText("Simple Content 14", { selector: 'div' });

    expect(item0).toBeInTheDocument();
    expect(item1).toBeInTheDocument;

    // Get the element's position in the viewport
    const rect0 = item1.getBoundingClientRect();
    // Ensure that the element is not yet visible (its position should be outside the viewport)
    await waitFor(() => {
      expect(rect0.top).toBeGreaterThan(window.innerHeight); // Element is out of the viewport
    });

    // Scroll the modal content to make sure the item is not initially visible
    const scrollableContainer = canvas.getByTestId('modal-content');
    await userEvent.click(scrollableContainer); // Trigger scroll (if necessary)
    scrollableContainer.scrollTop = 1000; // Manually scroll down

    // Get the element's position in the viewport
    const rect1 = item0.getBoundingClientRect();
    // Ensure that the element is no longer visible (its position should be outside the viewport)
    await waitFor(() => {
      expect(rect1.top).toBeLessThan(window.innerHeight); // Element is out of the viewport
    });

    // Find the submit button (or trigger) and click to close the modal
    const closeButton = canvas.getByRole("button", { name: /Submit/i });
    await userEvent.click(closeButton);

    // Verify that the modal is closed (the content should no longer be in the document)
    await waitFor(() => expect(canvas.queryByText("Simple Content 10")).not.toBeInTheDocument());
  },
}

export const SmallWindowedCompleteModal: Story = {
  args: {
    header: simpleHeader,
    modalClassInfo: undefined,
    modalActivity: modalActivity,
    content: content
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Simulate resizing the window to a specific size
    global.innerWidth = 1024; // Set the width to 1024px (desktop size)
    global.innerHeight = 768; // Set the height to 768px (desktop size)
    global.dispatchEvent(new Event('resize')); // Dispatch a resize event to ensure the new size is applied

    // Find the button and click to open the modal
    const button = canvas.getByRole("button", { name: /open modal/i });
    await userEvent.click(button);

    // Wait for the modal content to load and check for an item in content
    const modalContent = await canvas.getByTestId('modal-content');

    // Verify that the modal is open (checking for modal content)
    const header = canvas.getByText("Simple Header");
    const fooder = canvas.getByRole("button", { name: /Submit/i });

    expect(header).toBeInTheDocument();
    expect(fooder).toBeInTheDocument;

    // Get the element's position in the viewport
    const rect0 = header.getBoundingClientRect();
    // Ensure that the element is not yet visible (its position should be outside the viewport)
    await waitFor(() => {
      expect(rect0.top).toBeLessThan(global.innerHeight); // Element is out of the viewport
    });

    // Get the element's position in the viewport
    const rect1 = fooder.getBoundingClientRect();
    // Ensure that the element is no longer visible (its position should be outside the viewport)
    await waitFor(() => {
      expect(rect1.top).toBeLessThan(global.innerHeight); // Element is out of the viewport
    });
  }
}
