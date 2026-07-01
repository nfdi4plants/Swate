import { describe, it, expect } from 'vitest';
import {
  Swate_Components_Composite_Workspace_Types_WorkspacePaneState__WorkspacePaneState_addTab_Static as WorkspacePaneState_addTab,
  Swate_Components_Composite_Workspace_Types_WorkspacePaneState__WorkspacePaneState_removeTab_Static as WorkspacePaneState_removeTab,
  Swate_Components_Composite_Workspace_Types_WorkspacePaneState__WorkspacePaneState_removeAllExcept_Static as WorkspacePaneState_removeAllExcept,
  Swate_Components_Composite_Workspace_Types_WorkspacePaneState__WorkspacePaneState_removeAll_Static_Z48EBFF10 as WorkspacePaneState_removeAll,
  Swate_Components_Composite_Workspace_Types_WorkspacePaneState__WorkspacePaneState_reorderTab_Static as WorkspacePaneState_reorderTab,
} from './PaneState.fs.ts';
import { WorkspacePaneState } from '../Types.fs.ts';

function tab(id: string, label: string = id, icon?: string) {
  return { Id: id, Label: label, Icon: icon ?? null };
}

function paneState(tabs: ReturnType<typeof tab>[], activeTabId?: string) {
  const tabOrder = tabs.map(t => t.Id);
  return new WorkspacePaneState(tabs, tabOrder, activeTabId ?? (tabs.length > 0 ? tabs[0].Id : null) as any);
}

describe('PaneState.addTab', () => {
  it('adds tab to empty state and sets it active', () => {
    const state = paneState([]);
    const result = WorkspacePaneState_addTab(state, tab('a'));
    expect(result.tabs).toHaveLength(1);
    expect(result.tabs[0].Id).toBe('a');
    expect(result.activeTabId).toBe('a');
  });

  it('adds tab to existing state and sets it active', () => {
    const state = paneState([tab('a')]);
    const result = WorkspacePaneState_addTab(state, tab('b'));
    expect(result.tabs).toHaveLength(2);
    expect(result.activeTabId).toBe('b');
  });
});

describe('PaneState.removeTab', () => {
  it('removes the only tab', () => {
    const state = paneState([tab('a')]);
    const result = WorkspacePaneState_removeTab(state, 'a');
    expect(result.tabs).toHaveLength(0);
    expect(result.activeTabId).toBeUndefined();
  });

  it('removes active tab and activates nearest', () => {
    const state = paneState([tab('a'), tab('b'), tab('c')], 'b');
    const result = WorkspacePaneState_removeTab(state, 'b');
    expect(result.tabs.map((t: {Id: string}) => t.Id)).toEqual(['a', 'c']);
    expect(result.activeTabId).toBe('c');
  });

  it('removes non-active tab and preserves active', () => {
    const state = paneState([tab('a'), tab('b'), tab('c')], 'b');
    const result = WorkspacePaneState_removeTab(state, 'a');
    expect(result.tabs.map((t: {Id: string}) => t.Id)).toEqual(['b', 'c']);
    expect(result.activeTabId).toBe('b');
  });

  it('removes first tab and activates next', () => {
    const state = paneState([tab('a'), tab('b'), tab('c')], 'a');
    const result = WorkspacePaneState_removeTab(state, 'a');
    expect(result.activeTabId).toBe('b');
  });
});

describe('PaneState.removeAllExcept', () => {
  it('keeps only the specified tab', () => {
    const state = paneState([tab('a'), tab('b'), tab('c')], 'c');
    const result = WorkspacePaneState_removeAllExcept(state, 'a');
    expect(result.tabs).toHaveLength(1);
    expect(result.tabs[0].Id).toBe('a');
    expect(result.activeTabId).toBe('a');
  });
});

describe('PaneState.removeAll', () => {
  it('clears all tabs', () => {
    const state = paneState([tab('a'), tab('b')], 'a');
    const result = WorkspacePaneState_removeAll(state);
    expect(result.tabs).toHaveLength(0);
    expect(result.activeTabId).toBeUndefined();
  });
});

describe('PaneState.reorderTab', () => {
  it('moves tab from position 0 to 2', () => {
    const state = paneState([tab('a'), tab('b'), tab('c')]);
    const result = WorkspacePaneState_reorderTab(state, 0, 2);
    expect(result.tabOrder).toEqual(['b', 'c', 'a']);
  });

  it('moves tab from position 2 to 0', () => {
    const state = paneState([tab('a'), tab('b'), tab('c')]);
    const result = WorkspacePaneState_reorderTab(state, 2, 0);
    expect(result.tabOrder).toEqual(['c', 'a', 'b']);
  });
});
