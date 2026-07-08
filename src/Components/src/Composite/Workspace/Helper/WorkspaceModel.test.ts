import { describe, it, expect } from 'vitest';
import {
    update,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_Init_Static_Z435CFF61 as WorkspaceModel_Init,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_AddTab_Static as WorkspaceModel_AddTab,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_RemoveTab_Static as WorkspaceModel_RemoveTab,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_RemoveOtherTabs_Static as WorkspaceModel_RemoveOtherTabs,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_RemoveAllTabs_Static_5649D743 as WorkspaceModel_RemoveAllTabs,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_FocusTab_Static as WorkspaceModel_FocusTab,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_MoveTab_Static as WorkspaceModel_MoveTab,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_ReorderTabs_Static as WorkspaceModel_ReorderTabs,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_SplitPane_Static as WorkspaceModel_SplitPane,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_ClosePane_Static as WorkspaceModel_ClosePane,
    Swate_Components_Composite_Workspace_Types_WorkspaceModel$1__WorkspaceModel$1_CleanupEmptyPanes_Static_5649D743 as WorkspaceModel_CleanupEmptyPanes,
    ensureEdgeSplitAllowed,
} from './WorkspaceModel.fs.ts';
import {
    Tab,
    TabId,
    PaneId,
    Layout_Single,
    Layout_Split,
    Msg$1_AddTab as AddTab,
    Msg$1_RemoveTab as RemoveTab,
    Msg$1_RemoveOtherTabs as RemoveOtherTabs,
    Msg$1_RemoveAllTabs as RemoveAllTabs,
    Msg$1_FocusTab as FocusTab,
    Msg$1_MoveTab as MoveTab,
    Msg$1_ReorderTabs as ReorderTabs,
    Msg$1_SplitPaneByTabMove as SplitPaneByTabMove,
    Msg$1_ClosePane as ClosePane,
    Msg$1_SetSplitRatio as SetSplitRatio,
    type Tab as TabType,
    type WorkspaceModel$1 as WorkspaceModel,
    type PaneId,
    type TabId,
    type EdgeDirection,
    type SplitDirection,
    type Msg$1_$union as Msg,
} from '../Types.fs.ts';

function createTab(id: string, label: string = id, payload: string = '{}'): TabType<string> {
    return new Tab<string>(id, label, payload);
}

function getLayoutLeafCount(layout: any): number {
    function count(l: any): number {
        if (l.tag === 0) return 1;
        if (l.tag === 1) return count(l.fields[2]) + count(l.fields[3]);
        return 0;
    }
    return count(layout);
}

describe('WorkspaceModel.Init', () => {
    it('creates model with Layout.Single', () => {
        const model = WorkspaceModel_Init<string>();
        expect(model.Layout?.tag).toBe(0);
        expect(model.PanesMap.size).toBe(1);
        expect(model.FocusedPane).toBeTruthy();
    });

    it('initializes with tabs', () => {
        const tabs = [createTab('a'), createTab('b')];
        const model = WorkspaceModel_Init<string>(tabs);
        const pane = [...model.PanesMap.values()][0];
        expect(pane.Tabs.length).toBe(2);
    });

    it('initializes with active tab', () => {
        const tabs = [createTab('a'), createTab('b')];
        const model = WorkspaceModel_Init<string>(tabs, 'a');
        const pane = [...model.PanesMap.values()][0];
        expect(pane.FocusedTab).toBe('a');
    });
});

describe('AddTab', () => {
    it('adds tab to focused pane', () => {
        const model = WorkspaceModel_Init<string>();
        const updated = WorkspaceModel_AddTab(createTab('new'), model);
        const pane = [...updated.PanesMap.values()][0];
        expect(pane.Tabs.length).toBe(1);
        expect(pane.FocusedTab).toBe('new');
    });
});

describe('RemoveTab', () => {
    it('removes tab from pane', () => {
        const model = WorkspaceModel_Init<string>([createTab('a'), createTab('b')], 'a');
        const updated = WorkspaceModel_RemoveTab('a', model);
        const pane = [...updated.PanesMap.values()][0];
        expect(pane.Tabs.length).toBe(1);
    });

    it('CleanupEmptyPanes collapses single empty pane', () => {
        const model = WorkspaceModel_Init<string>([createTab('a')]);
        const afterRemove = WorkspaceModel_RemoveTab('a', model);
        const cleaned = WorkspaceModel_CleanupEmptyPanes<string>(afterRemove);
        expect(cleaned.PanesMap.size).toBe(0);
        expect(cleaned.Layout?.tag).toBe(0); // back to Single
    });
});

describe('FocusTab', () => {
    it('updates focused tab', () => {
        const model = WorkspaceModel_Init<string>([createTab('a'), createTab('b')], 'a');
        const updated = WorkspaceModel_FocusTab('b', model);
        const pane = [...updated.PanesMap.values()][0];
        expect(pane.FocusedTab).toBe('b');
        expect(updated.FocusedPane).toBeTruthy();
    });
});

describe('MoveTab', () => {
    it('moves tab between panes', () => {
        const model = WorkspaceModel_Init<string>([createTab('a'), createTab('b')]);
        const afterSplit = WorkspaceModel_SplitPane('a', 'right' as EdgeDirection, model);
        const afterMove = WorkspaceModel_MoveTab('b', afterSplit.FocusedPane, afterSplit);
        const srcPane = [...afterMove.PanesMap.values()].find((p: any) => p.Tabs.length === 1)!;
        expect(srcPane.Tabs[0].Id).toBe('a');
    });
});

describe('ReorderTabs', () => {
    it('moves tab from index 0 to 1', () => {
        const model = WorkspaceModel_Init<string>([createTab('a'), createTab('b'), createTab('c')]);
        const updated = WorkspaceModel_ReorderTabs(model.FocusedPane, 0, 1, model);
        const pane = [...updated.PanesMap.values()][0];
        expect(pane.Tabs[0].Id).toBe('b');
        expect(pane.Tabs[1].Id).toBe('a');
        expect(pane.Tabs[2].Id).toBe('c');
    });
});

describe('SplitPane', () => {
    it('creates new pane and updates layout', () => {
        const model = WorkspaceModel_Init<string>([createTab('a')]);
        const afterSplit = WorkspaceModel_SplitPane(model.FocusedPane, 'right' as EdgeDirection, model);
        expect(afterSplit.PanesMap.size).toBe(2);
        expect(afterSplit.Layout?.tag).toBe(1);
        expect(afterSplit.FocusedPane).not.toBe(model.FocusedPane);
    });
});

describe('ClosePane', () => {
    it('removes all tabs from pane', () => {
        const model = WorkspaceModel_Init<string>([createTab('a'), createTab('b')]);
        const afterSplit = WorkspaceModel_SplitPane('a', 'right' as EdgeDirection, model);
        const closed = WorkspaceModel_ClosePane(afterSplit.FocusedPane, afterSplit);
        const cleaned = WorkspaceModel_CleanupEmptyPanes<string>(closed);
        expect(cleaned.PanesMap.size).toBe(1);
    });
});

describe('RemoveOtherTabs', () => {
    it('keeps only specified tab', () => {
        const model = WorkspaceModel_Init<string>([createTab('a'), createTab('b'), createTab('c')]);
        const updated = WorkspaceModel_RemoveOtherTabs('b', model);
        const pane = [...updated.PanesMap.values()][0];
        expect(pane.Tabs.length).toBe(1);
        expect(pane.Tabs[0].Id).toBe('b');
    });
});

describe('RemoveAllTabs', () => {
    it('resets to single empty pane', () => {
        const model = WorkspaceModel_Init<string>([createTab('a'), createTab('b')]);
        const updated = WorkspaceModel_RemoveAllTabs<string>(model);
        expect(updated.PanesMap.size).toBe(1);
        const pane = [...updated.PanesMap.values()][0];
        expect(pane.Tabs.length).toBe(0);
    });
});

describe('update function', () => {
    it('AddTab via update', () => {
        const model = WorkspaceModel_Init<string>();
        const next = update(model, AddTab(createTab('x')));
        const pane = [...next.PanesMap.values()][0];
        expect(pane.Tabs[0].Id).toBe('x');
    });

    it('RemoveTab via update triggers cleanup', () => {
        const model = WorkspaceModel_Init<string>([createTab('a')]);
        const next = update(model, RemoveTab('a'));
        const pane = [...next.PanesMap.values()][0];
        expect(pane.Tabs.length).toBe(0);
    });
});
