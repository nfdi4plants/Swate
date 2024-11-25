import { describe, expect, test, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import QuickAccessButton from "./output/QuickAccessButton";

describe('QuickAccessButton', () => {
  test('renders the button with correct title and children', () => {
    render(
      <QuickAccessButton
        desc="Click me"
        onclick={() => {}}
      >
        Click Here
      </QuickAccessButton>
    );

    const button = screen.getByRole('button');
    expect(button).toBeInTheDocument();
    expect(button).toHaveAttribute('title', 'Click me');
    expect(button).toHaveTextContent('Click Here');
  });

  test('calls the onclick handler when clicked', async () => {
    const handleClick = vi.fn();
    render(
      <QuickAccessButton
        desc="Click me"
        onclick={handleClick}
      >
        Click Here
      </QuickAccessButton>
    );

    const button = screen.getByRole('button');
    fireEvent.click(button);

    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  test('disables the button when isDisabled is true', () => {
    render(
      <QuickAccessButton
        desc="Disabled button"
        isDisabled={true}
        onclick={() => {}}
      >
        Click Here
      </QuickAccessButton>
    );

    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
  });

  test('does not disable the button when isDisabled is false', () => {
    render(
      <QuickAccessButton
        desc="Enabled button"
        isDisabled={false}
        onclick={() => {}}
      >
        Click Here
      </QuickAccessButton>
    );

    const button = screen.getByRole('button');
    expect(button).not.toBeDisabled();
  });
});
