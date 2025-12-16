import type { Meta, StoryObj } from '@storybook/react-vite';
import { screen, fn, within, expect, userEvent, waitFor, fireEvent } from 'storybook/test';
import Layout from "./Layout.fs.js";
import {LayoutBtn, LeftSidebarToggleBtn} from "./Layout.fs.js";
import React from 'react';
import { Entry as SelectorEntry } from '../GenericComponents/Selector.fs.js';
import { Main as NavbarMain } from '../GenericComponents/Navbar.fs.js';

const meta = {
  title: "Components/Layout",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'fullscreen',
  },
  component: Layout,
} satisfies Meta<typeof Layout>;

export default meta;

type Story = StoryObj<typeof meta>;

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
    sidebarRightDefault: true,
    sidebarLeftDefault: true,
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
    sidebarLeftDefault: true,
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
    sidebarLeftDefault: true,
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
    sidebarRightDefault: true,
  }
}

export const ARCitectNavbarIsVisible: Story = {
  name: "ARCitect Navbar is visible",
  args: {
    navbar: < NavbarMain debug={true} />,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(async () => {
      const navbarTest = await canvas.findByTestId("navbar-test");
      expect(navbarTest).toBeVisible();
    });
  },
}

export const SelectorIsVisible: Story = {
  name: "Selector is visible",
  args: {
    navbar: <SelectorEntry maxNumber = {3} debug={true} />,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(async () => {
      const selectorTest = await canvas.findByTestId("selector-test");
      expect(selectorTest).toBeVisible();
    });
  },
}

export const SelectorActionbarisVisible: Story = {
  name: "Selector Actionbar is visible",
  args: {
    navbar: <SelectorEntry maxNumber = {3} debug={true} />,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const selectorTest = await canvas.findByTestId("selector-test");

    await userEvent.click(selectorTest);

    await waitFor(async () => {
      const actionbarTest = await canvas.findByTestId("actionbar-test");
      expect(actionbarTest).toBeVisible();
    });
  },
}

export const RestElementButtonInActionbarIsVisible: Story = {
  name: "RestElementbutton in Actionbar is visible",
  args: {
    navbar: <SelectorEntry maxNumber = {3} debug={true} />,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const selectorTest = await canvas.findByTestId("selector-test");

    await userEvent.click(selectorTest);

    await waitFor(async () => {
      const restElementButton = await canvas.findByTestId("button-test");
      expect(restElementButton).toBeVisible();
    });
  },
}

export const RestElementButtonInActionbarIsNotVisible: Story = {
  name: "RestElementbutton in Actionbar is not visible",
  args: {
    navbar: <SelectorEntry maxNumber = {5} debug={true} />,
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      Main Content
    </div>,
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const selectorTest = await canvas.findByTestId("selector-test");

    await userEvent.click(selectorTest);

    await waitFor(async () => {
      const restElementButton = await canvas.findByTestId("button-test");
      !expect(restElementButton).toBeVisible();
    });
  },
}
