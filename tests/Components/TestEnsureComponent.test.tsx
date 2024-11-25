import { describe, expect, test } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import TestEnsureComponent from "./TestEnsureComponent";

describe('TestEnsureComponent', () => {
  test('updates input value on typing', () => {
    render(<TestEnsureComponent />);

    const input = screen.getByTestId('input-field');
    fireEvent.change(input, { target: { value: 'hello' } });

    expect(input).toHaveValue('hello');
  });

  test('shows message when button is clicked', () => {
    render(<TestEnsureComponent />);

    const button = screen.getByTestId('button');
    fireEvent.click(button);

    expect(screen.getByTestId('message')).toBeInTheDocument();
  });
});
