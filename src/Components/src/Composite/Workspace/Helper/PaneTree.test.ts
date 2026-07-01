import { describe, it, expect } from 'vitest';
import {
  Swate_Components_Composite_Workspace_Types_Pane__Pane_leafIds_Static_3BB588A2 as Pane_leafIds,
  Swate_Components_Composite_Workspace_Types_Pane__Pane_depth_Static_3BB588A2 as Pane_depth,
  Swate_Components_Composite_Workspace_Types_Pane__Pane_canSplitLeaf_Static as Pane_canSplitLeaf,
  Swate_Components_Composite_Workspace_Types_Pane__Pane_splitLeaf_Static as Pane_splitLeaf,
  Swate_Components_Composite_Workspace_Types_Pane__Pane_removeLeaf_Static as Pane_removeLeaf,
} from './PaneTree.fs.ts';
import { Pane_Leaf, Pane_Split, SplitDirection, Pane_$union, Pane } from '../Types.fs.ts';

function leaf(id: string): Pane_$union {
  return Pane_Leaf(id);
}

function hSplit(first: Pane_$union, second: Pane_$union, ratio: number = 0.5): Pane_$union {
  return Pane_Split("horizontal" as SplitDirection, first, second, ratio);
}

function vSplit(first: Pane_$union, second: Pane_$union, ratio: number = 0.5): Pane_$union {
  return Pane_Split("vertical" as SplitDirection, first, second, ratio);
}

describe('PaneTree.leafIds', () => {
  it('returns single id for root leaf', () => {
    const ids = Pane_leafIds(leaf('root'));
    // FSharpList is array-like, convert to array for comparison
    expect([...ids]).toEqual(['root']);
  });

  it('returns all ids from a split tree', () => {
    const tree = hSplit(leaf('a'), leaf('b'));
    const ids = [...Pane_leafIds(tree)].sort();
    expect(ids).toEqual(['a', 'b']);
  });

  it('returns 4 ids from 2x2 grid', () => {
    const tree = hSplit(
      vSplit(leaf('a'), leaf('b')),
      vSplit(leaf('c'), leaf('d'))
    );
    const ids = [...Pane_leafIds(tree)].sort();
    expect(ids).toEqual(['a', 'b', 'c', 'd']);
  });
});

describe('PaneTree.depth', () => {
  it('returns 0 for root leaf', () => {
    expect(Pane_depth(leaf('root'))).toBe(0);
  });

  it('returns 1 for single split', () => {
    expect(Pane_depth(hSplit(leaf('a'), leaf('b')))).toBe(1);
  });

  it('returns 2 for nested split', () => {
    expect(Pane_depth(hSplit(vSplit(leaf('a'), leaf('b')), leaf('c')))).toBe(2);
  });
});

describe('PaneTree.canSplitLeaf', () => {
  it('allows any split on root leaf', () => {
    expect(Pane_canSplitLeaf(leaf('root'), 'root', "horizontal" as SplitDirection)).toBe(true);
    expect(Pane_canSplitLeaf(leaf('root'), 'root', "vertical" as SplitDirection)).toBe(true);
  });

  it('allows orthogonal split on child of horizontal split', () => {
    const tree = hSplit(leaf('a'), leaf('b'));
    expect(Pane_canSplitLeaf(tree, 'a', "vertical" as SplitDirection)).toBe(true);
    expect(Pane_canSplitLeaf(tree, 'a', "horizontal" as SplitDirection)).toBe(false);
  });

  it('denies any split at max depth', () => {
    const tree = hSplit(vSplit(leaf('a'), leaf('b')), leaf('c'));
    expect(Pane_canSplitLeaf(tree, 'a', "horizontal" as SplitDirection)).toBe(false);
    expect(Pane_canSplitLeaf(tree, 'a', "vertical" as SplitDirection)).toBe(false);
  });

  it('allows split on non-child leaf at depth 1', () => {
    const tree = hSplit(leaf('a'), leaf('b'));
    expect(Pane_canSplitLeaf(tree, 'b', "vertical" as SplitDirection)).toBe(true);
  });

  it('returns false for non-existent leaf id', () => {
    expect(Pane_canSplitLeaf(leaf('root'), 'nonexistent', "horizontal" as SplitDirection)).toBe(false);
  });
});

describe('PaneTree.splitLeaf', () => {
  it('splits a root leaf horizontally creating two children', () => {
    const result = Pane_splitLeaf(leaf('root'), 'root', "horizontal" as SplitDirection, 'new');
    expect(result).toBeDefined();
    const ids = [...Pane_leafIds(result!)].sort();
    expect(ids).toEqual(['new', 'root']);
    expect(Pane_depth(result!)).toBe(1);
  });

  it('splits a child leaf vertically creating 3-pane layout', () => {
    const tree = hSplit(leaf('a'), leaf('b'));
    const result = Pane_splitLeaf(tree, 'a', "vertical" as SplitDirection, 'new');
    expect(result).toBeDefined();
    const ids = [...Pane_leafIds(result!)].sort();
    expect(ids).toEqual(['a', 'b', 'new']);
  });

  it('returns None when leaf id is not found', () => {
    const result = Pane_splitLeaf(leaf('root'), 'wrong', "horizontal" as SplitDirection, 'new');
    expect(result).toBeUndefined();
  });

  it('returns None when split would exceed depth constraints', () => {
    const tree = hSplit(vSplit(leaf('a'), leaf('b')), leaf('c'));
    const result = Pane_splitLeaf(tree, 'a', "horizontal" as SplitDirection, 'new');
    expect(result).toBeUndefined();
  });
});

describe('PaneTree.removeLeaf', () => {
  it('reduces a 2-pane split to a single leaf', () => {
    const tree = hSplit(leaf('a'), leaf('b'));
    const result = Pane_removeLeaf(tree, 'a');
    expect(result).toBeDefined();
    expect([...Pane_leafIds(result!)]).toEqual(['b']);
  });

  it('reduces a 3-pane layout to a 2-pane layout', () => {
    const tree = hSplit(vSplit(leaf('a'), leaf('b')), leaf('c'));
    const result = Pane_removeLeaf(tree, 'a');
    expect(result).toBeDefined();
    const ids = [...Pane_leafIds(result!)].sort();
    expect(ids).toEqual(['b', 'c']);
  });

  it('returns None for a single root leaf', () => {
    const result = Pane_removeLeaf(leaf('root'), 'root');
    expect(result).toBeUndefined();
  });

  it('returns original tree for non-existent leaf', () => {
    const tree = hSplit(leaf('a'), leaf('b'));
    const result = Pane_removeLeaf(tree, 'nonexistent');
    expect(result).toBeDefined();
    expect([...Pane_leafIds(result!)].sort()).toEqual(['a', 'b']);
  });
});
