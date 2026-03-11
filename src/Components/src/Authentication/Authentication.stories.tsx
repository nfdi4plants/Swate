import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor, fireEvent } from 'storybook/test';
import { Entry as AuthenticationEntry } from './Authentication.fs.js';

const meta = {
  title: 'Components/Authentication',
  tags: ['autodocs'],
  component: AuthenticationEntry,
} satisfies Meta<typeof AuthenticationEntry>;

export default meta;

type Story = StoryObj<typeof meta>;

const openUserMenu = async (canvas: ReturnType<typeof within>) => {
  const toggleBtn = await canvas.findByTestId('UserButtonToggle');
  await userEvent.click(toggleBtn);
  await waitFor(async () => {
    const dropdownContent = await canvas.queryByTestId('UserDropdownContent');
    expect(dropdownContent).toBeVisible()
  }, { timeout: 3000 });
};

const signInWithPat = async (canvas: ReturnType<typeof within>) => {
  await openUserMenu(canvas);

  const tokenInput = await canvas.findByTestId('PersonalAccessTokenInput');
  await userEvent.type(tokenInput, 'fake-test-token');

  const signInButton = await canvas.findByTestId('SignInButton');
  await userEvent.click(signInButton);
};

const expectLoggedInDataHub = async (canvas: ReturnType<typeof within>, expectedDataHub: string) => {
  await waitFor(async () => {
    const signedInInfo = await canvas.findByTestId('SignedInInfo');
    expect(signedInInfo).toHaveTextContent('Signed In: true');
  }, { timeout: 4000 });

  await openUserMenu(canvas);

  const loggedInDataHub = await canvas.findByTestId('LoggedInDataHub');
  expect(loggedInDataHub).toHaveTextContent(`DataHub: ${expectedDataHub}`);
};

export const Default: Story = {};

export const SignInFlow: Story = {
  name: 'Sign In flow',
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await signInWithPat(canvas);

    await waitFor(async () => {
      const signedInInfo = await canvas.findByTestId('SignedInInfo');
      expect(signedInInfo).toHaveTextContent('Signed In: true');
    }, { timeout: 4000 });

    await openUserMenu(canvas);

    const logoutButton = await canvas.findByTestId('LogoutButton');
    expect(logoutButton).toBeInTheDocument();
    expect(canvas.getByText(/john doe/i)).toBeInTheDocument();
  },
};

export const SignInRequiresPatFlow: Story = {
  name: 'Sign In requires PAT',
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await openUserMenu(canvas);

    const signInButton = await canvas.findByTestId('SignInButton');
    expect(signInButton).toBeDisabled();

    const tokenInput = await canvas.findByTestId('PersonalAccessTokenInput');
    await userEvent.type(tokenInput, 'fake-test-token');

    expect(signInButton).toBeEnabled();
  },
};

export const SwitchToSupportedDataHubFlow: Story = {
  name: 'Switch to supported DataHub',
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const selectedDataHub = 'https://datahub.rz.rptu.de/';

    await openUserMenu(canvas);

    const optionsToggle = await canvas.findByTestId('DataHubOptionsToggle');
    await userEvent.click(optionsToggle);

    const supportedRadio = await canvas.findByTestId('DataHubRadio-datahub.rz.rptu.de');
    await userEvent.click(supportedRadio);

    await waitFor(async () => {
      const patLink = await canvas.findByTestId('GeneratePatLink');
      expect(patLink).toHaveAttribute('href', expect.stringContaining('https://datahub.rz.rptu.de/-/user_settings/personal_access_tokens'));
    });

    const tokenInput = await canvas.findByTestId('PersonalAccessTokenInput');
    await userEvent.type(tokenInput, 'fake-test-token');
    const signInButton = await canvas.findByTestId('SignInButton');
    await userEvent.click(signInButton);

    await expectLoggedInDataHub(canvas, selectedDataHub);
  },
};

export const SwitchToCustomDataHubFlow: Story = {
  name: 'Switch to custom DataHub',
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const selectedDataHub = 'https://gitlab.example.org/';

    await openUserMenu(canvas);

    const optionsToggle = await canvas.findByTestId('DataHubOptionsToggle');
    await userEvent.click(optionsToggle);

    const customInput = await canvas.findByTestId('CustomDataHubInput');
    fireEvent.input(customInput, { target: { value: selectedDataHub } });

    const customRadio = await canvas.findByTestId('CustomDataHubRadio');
    await userEvent.click(customRadio);

    await waitFor(async () => {
      const patLink = await canvas.findByTestId('GeneratePatLink');
      expect(patLink).toHaveAttribute('href', expect.stringContaining('https://gitlab.example.org/-/user_settings/personal_access_tokens'));
    });

    const tokenInput = await canvas.findByTestId('PersonalAccessTokenInput');
    await userEvent.type(tokenInput, 'fake-test-token');
    const signInButton = await canvas.findByTestId('SignInButton');
    await userEvent.click(signInButton);

    await expectLoggedInDataHub(canvas, selectedDataHub);
  },
};

export const LogoutFlow: Story = {
  name: 'Logout flow',
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await signInWithPat(canvas);

    await waitFor(async () => {
      const signedInInfo = await canvas.findByTestId('SignedInInfo');
      expect(signedInInfo).toHaveTextContent('Signed In: true');
    }, { timeout: 4000 });

    await openUserMenu(canvas);
    const logoutButton = await canvas.findByTestId('LogoutButton');
    expect(logoutButton).toBeInTheDocument();

    await userEvent.click(logoutButton);

    await waitFor(async () => {
      let signedInInfo = await canvas.findByTestId('SignedInInfo');
      expect(signedInInfo).toHaveTextContent('Signed In: false');
    });
  },
};
