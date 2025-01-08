import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect } from 'vitest';

// Assuming TestInput is the transpiled component from Fable
import Example from './POC.fs.js'; // Update the import path as needed

describe('TestInput Component', () => {
  it('renders with initial number and children', () => {
    render(
      <Example number={42}>
        <span>Child Element</span>
      </Example>
    );

    // Check for child element
    expect(screen.getByText('Child Element')).toBeInTheDocument();

    // Check for initial number
    expect(screen.getByText('Number: 42')).toBeInTheDocument();
  });

  it('renders with default number (0) when no number prop is provided', () => {
    render(
      <Example>
        <span>Default Number</span>
      </Example>
    );

    // Check for default number
    expect(screen.getByText('Number: 0')).toBeInTheDocument();
  });

  it('updates number when input value changes', async () => {
    render(<Example number={10} />);

    // Get the number input
    const input = screen.getByRole('spinbutton'); // Default ARIA role for number inputs

    // Simulate changing the input value
    fireEvent.change(input, { target: { value: '25' } });

    // Check if the number is updated
    expect(screen.getByText('Number: 25')).toBeInTheDocument();
  });

  it('handles non-numeric input gracefully', async () => {
    render(<Example />);

    // Get the number input
    const input = screen.getByRole('spinbutton');

    // Simulate entering non-numeric value
    fireEvent.change(input, { target: { value: 'not-a-number' } });

    // Check if the number remains 0 (default state)
    expect(screen.getByText('Number: 0')).toBeInTheDocument();
  });
});