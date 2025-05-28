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

const TESTID_BASE_MODAL_CONTENT = "modal_content_base"
const TESTID_SUBMIT_BUTTON = "submit_button_base"


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

const ButtonWithModal = ({header, modalClassInfo, modalActions, content}) => {
  const [open, setOpen] = useState(false);
  const rmv=() => setOpen(false) // Close modal when needed
  const openModal=() => setOpen(true) // Close modal when needed
  const submitButton =
    <BaseButton
      className={"swt:btn swt:btn-primary"}
      style={{ marginLeft: "auto"}}
      onClick={rmv}
      data-testid={TESTID_BASE_MODAL_CONTENT}
    >Submit</BaseButton>
  return (
    <div>
      {
        <BaseButton
          className={"swt:btn swt:btn-primary"}
          style={{ marginLeft: "auto"}}
          onClick={openModal}
          >Open Modal</BaseButton>
      }
      {open && (
        <BaseModal
          rmv={rmv}
          header={header}
          modalClassInfo={modalClassInfo}
          modalActions={modalActions}
          content={content}
          footer={submitButton}
          debug={"base"}
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
    viewport: {defaultViewport:'responsive'}
  },
  component: ButtonWithModal,
} satisfies Meta<typeof ButtonWithModal>;

export default meta;

type Story = StoryObj<typeof meta>;

const simpleHeader: JSX.Element = <>Simple Header</>;
const modalActivity: JSX.Element = <>Simple Modal Activity</>;
const list: JSX.Element[] = Array.from({ length: 500 }, (_, index) => <>Simple Content {index}</>);
const content =
  list.map((item, index) => (
      <div key={index} style={{ padding: "8px", borderBottom: "1px solid #ddd" }}>
          {item}
        </div>
      ));
const modalClassInfo: string = "swt:max-w-none";

export const CompleteModal: Story = {
  args: {
    header: simpleHeader,
    modalClassInfo: undefined,
    modalActions: modalActivity,
    content: content
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Find the button and click to open the modal
    const button = canvas.getByRole("button", { name: /open modal/i });
    await userEvent.click(button);

    // Wait for the modal content to load and check for an item in content
    const modalContent = canvas.getByTestId(TESTID_BASE_MODAL_CONTENT);

    // Verify that the modal is open (checking for modal content)
    const item0 = canvas.getByText("Simple Content 0", { selector: 'div' });
    const item1 = canvas.getByText("Simple Content 400", { selector: 'div' });

    expect(item0).toBeInTheDocument();
    expect(item1).toBeInTheDocument;

     // Element is scrollable
     await waitFor(() => {
       expect(modalContent.scrollHeight).toBeGreaterThan(modalContent.clientHeight);
     });

     // Find the submit button (or trigger) and click to close the modal
     const closeButton = canvas.getByTestId(TESTID_SUBMIT_BUTTON);
     await userEvent.click(closeButton);

     // Verify that the modal is closed (the content should no longer be in the document)
     await waitFor(() => expect(canvas.queryByText("Simple Content 10")).not.toBeInTheDocument());
  },
}

export const WideCompleteModal: Story = {
  args: {
    header: simpleHeader,
    modalClassInfo: modalClassInfo,
    modalActions: modalActivity,
    content: content
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Find the button and click to open the modal
    const button = canvas.getByRole("button", { name: /open modal/i });
    await userEvent.click(button);

    // Wait for the modal content to load and check for an item in content
    const modalContent = canvas.getByTestId(TESTID_BASE_MODAL_CONTENT);

    // Verify that the modal is open (checking for modal content)
    const item0 = canvas.getByText("Simple Content 0", { selector: 'div' });
    const item1 = canvas.getByText("Simple Content 400", { selector: 'div' });

    expect(item0).toBeInTheDocument();
    expect(item1).toBeInTheDocument;

     // Element is scrollable
     await waitFor(() => {
       expect(modalContent.scrollHeight).toBeGreaterThan(modalContent.clientHeight);
     });

     // Find the submit button (or trigger) and click to close the modal
     const closeButton = canvas.getByTestId(TESTID_SUBMIT_BUTTON);
     await userEvent.click(closeButton);

     // Verify that the modal is closed (the content should no longer be in the document)
     await waitFor(() => expect(canvas.queryByText("Simple Content 10")).not.toBeInTheDocument());
  },
}

export const SmallWindowedCompleteModal: Story = {
  args: {
    header: simpleHeader,
    modalClassInfo: undefined,
    modalActions: modalActivity,
    content: content
  },
  parameters: {
    viewport: {
      defaultViewport: "mobile1"
    }
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Find the button and click to open the modal
    const button = canvas.getByRole("button", { name: /open modal/i });
    await userEvent.click(button);

    // Wait for the modal content to load and check for an item in content
    const modalContent = canvas.getByTestId(TESTID_BASE_MODAL_CONTENT);

    // Verify that the modal is open (checking for modal content)
    const header = canvas.getByText("Simple Header");
    const fooder = canvas.getByTestId(TESTID_SUBMIT_BUTTON)

    await waitFor(() => {
      expect(header).toBeInTheDocument();
      expect(fooder).toBeInTheDocument();
    });

    // Get the element's position in the viewport
    const rect0 = header.getBoundingClientRect();
    // Ensure that the element is not yet visible (its position should be outside the viewport)
    await waitFor(() => {
      expect(rect0.top).toBeLessThan(window.innerHeight); // Element is out of the viewport
    });

    // Get the element's position in the viewport
    const rect1 = fooder.getBoundingClientRect();
    // Ensure that the element is no longer visible (its position should be outside the viewport)
    await waitFor(() => {
      expect(rect1.top).toBeLessThan(window.innerHeight); // Element is out of the viewport
    });
  }
}
