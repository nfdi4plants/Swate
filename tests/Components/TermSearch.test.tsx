import { describe, expect, test, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { Fragment } from "react/jsx-runtime";
import TermSearch from './output/TermSearchV2'

describe('TermSearch', () => {
  test('placeholder', () => {
    // render(
    //   <QuickAccessButton
    //     desc="Click me"
    //     onclick={() => {}}
    //   >
    //     <span>Click Here</span>
    //   </QuickAccessButton>
    // );

    // const button = screen.getByRole('button');
    // expect(button).toBeInTheDocument();
    // expect(button).toHaveAttribute('title', 'Click me');
    // expect(button).toHaveTextContent('Click Here');
  });
});
