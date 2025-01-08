import { describe, test, expect } from 'vitest';

describe('Native DOM', () => {
  test('creates an element in the DOM', () => {
    const div = document.createElement('div');
    expect(div).toBeDefined();
  });


  test('environment should be jsdom', () => {
    expect(globalThis.navigator).toBeDefined();
    expect(globalThis.navigator.userAgent).toContain('jsdom');
  });

});
