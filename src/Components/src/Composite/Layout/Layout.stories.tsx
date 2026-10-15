import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor } from 'storybook/test';
import Layout from "./Layout.fs.js";
import {LayoutBtn, LeftSidebarToggleBtn, RightSidebarToggleBtn} from "./Layout.fs.js";
import { Main as NavbarMain } from '../GenericComponents/Navbar.fs.js';

const meta = {
  title: "Composite Components/Layout",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'fullscreen',
  },
  component: Layout,
} satisfies Meta<typeof Layout>;

export default meta;

type Story = StoryObj<typeof meta>;

const getLeftSidebar = (canvas: ReturnType<typeof within>) =>
  canvas.getByTestId('layout-main-left-sidebar');

const getRightSidebar = (canvas: ReturnType<typeof within>) =>
  canvas.getByTestId('layout-main-right-sidebar');

const getLeftSidebarToggle = (canvas: ReturnType<typeof within>) =>
  canvas.getByRole('button', { name: 'Toggle left sidebar' });

const getRightSidebarToggle = (canvas: ReturnType<typeof within>) =>
  canvas.getByRole('button', { name: 'Toggle right sidebar' });

const ensureSidebarIsOpen = async (
  sidebar: HTMLElement,
  toggleButton: HTMLElement,
) => {
  if (sidebar.style.width === '0px') {
    await userEvent.click(toggleButton);
  }

  await waitFor(() => {
    expect(sidebar).not.toHaveStyle({ width: '0px' });
  });
};

export const Default: Story = {
  args: {
    navbar: <div className="swt:flex swt:items-center swt:justify-center swt:h-full swt:grow">
      Navbar
      </div>,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
    leftSidebar: <ul className="swt:menu swt:flex-nowrap">
      <li className="menu-title">Left Sidebar</li>
      {
        Array.from({ length: 100 }, (_, i) => (
          <li key={i}><a>Item {i + 1}</a></li>
        ))
      }
    </ul>,
    rightSidebar: <ul className="swt:menu swt:flex-nowrap">
      <li className="menu-title">Right Sidebar</li>
      {
        Array.from({ length: 100 }, (_, i) => (
          <li key={i}><a>Item {i + 1}</a></li>
        ))
      }
    </ul>,
    leftActions: <>
      <LayoutBtn
        iconClassName="swt:fluent--search-24-regular"
        tooltip="Search"
        onClick={() => {}} />
      <LayoutBtn
        iconClassName="swt:fluent--info-24-regular"
        tooltip="Info"
        onClick={() => {}} />
    </>,
    rightActions: <>
      <LayoutBtn
        iconClassName="swt:fluent--settings-24-regular"
        tooltip="Settings"
        onClick={() => {}} />
    </>,
    rightSidebarDefaultOpen: true,
    leftSidebarDefaultOpen: true,
  }
}

export const Content: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>
  }
}

export const ContentNavbar: Story = {
  name: "Content + Navbar",
  args: {
    navbar: <div className="swt:flex swt:items-center swt:justify-center swt:h-full swt:grow">
      Navbar
      </div>,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
  }
}

export const ContentLeftSidebar: Story = {
  name: "Content + Left Sidebar",
  args: {
    navbar: <div className="swt:flex swt:items-center swt:justify-center swt:h-full swt:grow">
      <div className='swt:grow-0'>
        <LeftSidebarToggleBtn />
      </div>
      <div className='swt:grow '>
        Navbar
      </div>
      </div>,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
    leftSidebar: <ul className="swt:menu swt:flex-nowrap">
      <li className="menu-title">Left Sidebar</li>
      {
        Array.from({ length: 100 }, (_, i) => (
          <li key={i}><a>Item {i + 1}</a></li>
        ))
      }
    </ul>,
    leftSidebarDefaultOpen: true,
  }
}

export const ContentLeftSidebarActions: Story = {
  name: "Content + Left Sidebar + Left Actions",
  args: {
    navbar: <div className="swt:flex swt:items-center swt:justify-center swt:h-full swt:grow">
        Navbar
      </div>,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
    leftSidebar: <ul className="swt:menu swt:flex-nowrap">
      <li className="menu-title">Left Sidebar</li>
      {
        Array.from({ length: 100 }, (_, i) => (
          <li key={i}><a>Item {i + 1}</a></li>
        ))
      }
    </ul>,
    leftSidebarDefaultOpen: true,
    leftActions: <>
      <LeftSidebarToggleBtn activeBorderStyle/>
      <LayoutBtn
        iconClassName="swt:fluent--search-24-regular"
        tooltip="Search"
        onClick={() => {}} />
      <LayoutBtn
        iconClassName="swt:fluent--info-24-regular"
        tooltip="Info"
        onClick={() => {}} />
    </>,
  }
}

// A button with an indicator shows a badge on its top right corner. The badge stays inside the action column,
// so the column does not grow and nothing cuts the badge.
export const LeftActionIndicator: Story = {
  name: "Left Action With Indicator",
  args: {
    navbar: <div className="swt:flex swt:items-center swt:justify-center swt:h-full swt:grow">
        Navbar
      </div>,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
    leftSidebar: <ul className="swt:menu swt:flex-nowrap">
      <li className="menu-title">Left Sidebar</li>
    </ul>,
    leftSidebarDefaultOpen: true,
    leftActions: <>
      <LayoutBtn
        iconClassName="swt:fluent--search-24-regular"
        tooltip="Search"
        onClick={() => {}} />
      <LayoutBtn
        iconClassName="swt:fluent--flowchart-24-regular"
        tooltip="Flow"
        indicator="new"
        onClick={() => {}} />
      <LayoutBtn
        iconClassName="swt:fluent--info-24-regular"
        tooltip="Info"
        onClick={() => {}} />
    </>,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const column = canvas.getByTestId('layout-main-left-navbar');
    const badge = canvas.getByTestId('layout-btn-indicator');
    const button = canvas.getByRole('button', { name: 'Flow' });

    // Only the button with an indicator has a badge, and the badge belongs to that button.
    await expect(canvas.getAllByTestId('layout-btn-indicator')).toHaveLength(1);
    await expect(badge.parentElement).toContainElement(button);

    const columnBox = column.getBoundingClientRect();
    const badgeBox = badge.getBoundingClientRect();
    const buttonBox = button.getBoundingClientRect();

    await expect(badgeBox.width).toBeGreaterThan(0);
    await expect(badgeBox.left).toBeGreaterThanOrEqual(columnBox.left);
    await expect(badgeBox.right).toBeLessThanOrEqual(columnBox.right);
    await expect(badgeBox.top).toBeGreaterThanOrEqual(columnBox.top);

    // The badge sits on the top right of the button, and the column is as wide as a column without badge.
    await expect(badgeBox.right).toBeGreaterThan(buttonBox.right - 2);
    await expect(badgeBox.top).toBeLessThan(buttonBox.top + buttonBox.height / 2);
    await expect(columnBox.width).toBeLessThan(buttonBox.width + 16);
  },
}

export const ContentRightSidebar: Story = {
  name: "Content + Right Sidebar",
  args: {
    navbar: <div className="swt:flex swt:items-center swt:justify-center swt:h-full swt:grow">
      Navbar
      </div>,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
    rightSidebar: <ul className="swt:menu swt:flex-nowrap">
      <li className="menu-title">Right Sidebar</li>
      {
        Array.from({ length: 100 }, (_, i) => (
          <li key={i}><a>Item {i + 1}</a></li>
        ))
      }
    </ul>,
    rightActions: <RightSidebarToggleBtn />,
    rightSidebarDefaultOpen: true,
  }
}

export const ToggleButtonsControlOwnSide: Story = {
  name: "Toggle Buttons Control Own Side",
  parameters: { isolated: true },
  args: {
    navbar: <div className="swt:flex swt:items-center swt:justify-end swt:h-full swt:grow swt:gap-2 swt:px-4">
      <LeftSidebarToggleBtn />
      <RightSidebarToggleBtn />
    </div>,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
    leftSidebar: <ul className="swt:menu swt:flex-nowrap">
      <li className="menu-title">Left Sidebar</li>
      {
        Array.from({ length: 8 }, (_, i) => (
          <li key={i}><a>Left Item {i + 1}</a></li>
        ))
      }
    </ul>,
    rightSidebar: <ul className="swt:menu swt:flex-nowrap">
      <li className="menu-title">Right Sidebar</li>
      {
        Array.from({ length: 8 }, (_, i) => (
          <li key={i}><a>Right Item {i + 1}</a></li>
        ))
      }
    </ul>,
    leftSidebarDefaultOpen: true,
    rightSidebarDefaultOpen: true,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const leftSidebar = getLeftSidebar(canvas);
    const rightSidebar = getRightSidebar(canvas);
    const leftToggle = getLeftSidebarToggle(canvas);
    const rightToggle = getRightSidebarToggle(canvas);

    await ensureSidebarIsOpen(leftSidebar, leftToggle);
    await ensureSidebarIsOpen(rightSidebar, rightToggle);
    await expect(canvas.getByText('Left Item 1')).toBeVisible();

    await userEvent.click(leftToggle);

    await waitFor(() => {
      expect(leftSidebar).toHaveStyle({ width: '0px' });
      expect(rightSidebar).not.toHaveStyle({ width: '0px' });
    });

    await userEvent.click(leftToggle);
    await ensureSidebarIsOpen(leftSidebar, leftToggle);
    await expect(canvas.getByText('Left Item 1')).toBeVisible();

    await userEvent.click(rightToggle);

    await waitFor(() => {
      expect(rightSidebar).toHaveStyle({ width: '0px' });
      expect(leftSidebar).not.toHaveStyle({ width: '0px' });
    });
  },
}
