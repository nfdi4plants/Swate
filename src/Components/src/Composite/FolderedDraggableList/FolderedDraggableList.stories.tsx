import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { DndContext, useDroppable, type DragEndEvent } from '@dnd-kit/core';
import { expect, fireEvent, userEvent, waitFor, within } from 'storybook/test';
import { FolderedDraggableList } from './FolderedDraggableList.fs.js';
import { ofArray, type FSharpList } from '../../fable_modules/fable-library-ts.5.0.0-alpha.21/List.ts';
import { ofArray as setOfArray, type FSharpSet } from '../../fable_modules/fable-library-ts.5.0.0-alpha.21/Set.ts';
import { comparePrimitives } from '../../fable_modules/fable-library-ts.5.0.0-alpha.21/Util.ts';

type PropertyPayload = {
  LayerId: string;
  Origin: string;
  Header: string;
  Connections: string[];
};

type StoryItem = {
  Id: string;
  Label: string;
  DragKey: string;
  Payload: PropertyPayload;
  Color?: string;
  Badge?: string;
  Tooltip?: string;
  Disabled: boolean;
};

type StoryFolder = {
  Id: string;
  Name: string;
  Color?: string;
  Items: FSharpList<StoryItem>;
};

type StoryListProps = {
  folders: FSharpList<StoryFolder>;
  dragId: (folder: StoryFolder, item: StoryItem) => string;
  expandedFolderIds?: FSharpSet<string>;
  defaultExpandedFolderIds?: FSharpSet<string>;
  onExpandedFolderIdsChange?: (expandedFolderIds: FSharpSet<string>) => void;
  className?: string;
  debug?: boolean;
};

type StoryDragData = {
  FolderId: string;
  FolderName: string;
  FolderColor?: string;
  ItemId: string;
  ItemLabel: string;
  ItemColor?: string;
  EffectiveColor?: string;
  Payload: PropertyPayload;
};

const StoryFolderedDraggableList = FolderedDraggableList as React.ComponentType<StoryListProps>;

const stringComparer = {
  Compare: (x: string, y: string) => comparePrimitives(x, y),
};

function fsList<T>(items: T[]): FSharpList<T> {
  return ofArray(items);
}

function fsSet(items: string[]): FSharpSet<string> {
  return setOfArray(items, stringComparer);
}

function propertyItem(
  id: string,
  label: string,
  layerId: string,
  origin: string,
  color?: string,
  badge?: string,
  disabled = false
): StoryItem {
  return {
    Id: id,
    Label: label,
    DragKey: `story-drag-${id}`,
    Payload: {
      LayerId: layerId,
      Origin: origin,
      Header: label,
      Connections: [`connection:${id}`],
    },
    Color: color,
    Badge: badge,
    Tooltip: `${origin} / ${label}`,
    Disabled: disabled,
  };
}

function folder(id: string, name: string, color: string | undefined, items: StoryItem[]): StoryFolder {
  return {
    Id: id,
    Name: name,
    Color: color,
    Items: fsList(items),
  };
}

function initialFolders() {
  return fsList([
    folder('layer-a', 'Layer A', '#2563eb', [
      propertyItem('layer-a-mass', 'Mass', 'layer-a', 'previous-table', undefined, '3'),
      propertyItem('layer-a-species', 'Species', 'layer-a', 'previous-table', '#16a34a', '2'),
    ]),
    folder('layer-b', 'Layer B', '#d97706', [
      propertyItem('layer-b-instrument', 'Instrument', 'layer-b', 'upstream-layer', undefined, '1'),
      propertyItem('layer-b-archived', 'Archived method', 'layer-b', 'upstream-layer', '#6b7280', 'disabled', true),
    ]),
  ]);
}

function overflowingFolders() {
  return fsList(
    Array.from({ length: 8 }, (_, folderIndex) =>
      folder(
        `wide-layer-${folderIndex + 1}`,
        `Wide Layer ${folderIndex + 1}`,
        folderIndex % 2 === 0 ? '#2563eb' : '#d97706',
        Array.from({ length: 8 }, (_, itemIndex) =>
          propertyItem(
            `wide-layer-${folderIndex + 1}-property-${itemIndex + 1}`,
            `Property ${itemIndex + 1}`,
            `wide-layer-${folderIndex + 1}`,
            'overflow-story',
            itemIndex % 2 === 0 ? '#16a34a' : undefined,
            `${itemIndex + 1}`
          )
        )
      )
    )
  );
}

function dragId(_folder: StoryFolder, item: StoryItem) {
  // Opaque stable identity only. Consumers must read metadata from event.active.data.current.
  return item.DragKey;
}

function DropZone() {
  const droppable = useDroppable({ id: 'outside-drop' });

  return (
    <div
      ref={droppable.setNodeRef}
      data-testid="outside-drop"
      className={`swt:min-h-16 swt:rounded swt:border swt:border-dashed swt:p-3 ${
        droppable.isOver ? 'swt:border-primary swt:bg-primary/10' : 'swt:border-base-300'
      }`}
    >
      Drop outside
    </div>
  );
}

function UpdatingHarness() {
  const [folders, setFolders] = React.useState(() => initialFolders());

  const addReplicate = () => {
    setFolders(
      fsList([
        folder('layer-a', 'Layer A', '#2563eb', [
          propertyItem('layer-a-mass', 'Mass', 'layer-a', 'previous-table', undefined, '3'),
          propertyItem('layer-a-species', 'Species', 'layer-a', 'previous-table', '#16a34a', '2'),
          propertyItem('layer-a-replicate', 'Replicate', 'layer-a', 'previous-table', '#dc2626', '4'),
        ]),
        folder('layer-b', 'Layer B', '#d97706', [
          propertyItem('layer-b-instrument', 'Instrument', 'layer-b', 'upstream-layer', undefined, '1'),
          propertyItem('layer-b-archived', 'Archived method', 'layer-b', 'upstream-layer', '#6b7280', 'disabled', true),
        ]),
      ])
    );
  };

  const removeSpecies = () => {
    setFolders(
      fsList([
        folder('layer-a', 'Layer A', '#2563eb', [
          propertyItem('layer-a-mass', 'Mass', 'layer-a', 'previous-table', undefined, '3'),
        ]),
        folder('layer-b', 'Layer B', '#d97706', [
          propertyItem('layer-b-instrument', 'Instrument', 'layer-b', 'upstream-layer', undefined, '1'),
          propertyItem('layer-b-archived', 'Archived method', 'layer-b', 'upstream-layer', '#6b7280', 'disabled', true),
        ]),
      ])
    );
  };

  const renameAndRecolorLayerA = () => {
    setFolders(
      fsList([
        folder('layer-a', 'Layer Alpha', '#7c3aed', [
          propertyItem('layer-a-mass', 'Mass', 'layer-a', 'previous-table', undefined, '3'),
          propertyItem('layer-a-species', 'Species', 'layer-a', 'previous-table', '#16a34a', '2'),
        ]),
        folder('layer-b', 'Layer B', '#d97706', [
          propertyItem('layer-b-instrument', 'Instrument', 'layer-b', 'upstream-layer', undefined, '1'),
          propertyItem('layer-b-archived', 'Archived method', 'layer-b', 'upstream-layer', '#6b7280', 'disabled', true),
        ]),
      ])
    );
  };

  return (
    <div className="swt:flex swt:flex-col swt:gap-3 swt:w-96">
      <div className="swt:flex swt:gap-2">
        <button type="button" className="swt:btn swt:btn-xs swt:btn-outline" onClick={addReplicate}>
          Add replicate
        </button>
        <button type="button" className="swt:btn swt:btn-xs swt:btn-outline" onClick={removeSpecies}>
          Remove species
        </button>
        <button type="button" className="swt:btn swt:btn-xs swt:btn-outline" onClick={renameAndRecolorLayerA}>
          Rename and recolor Layer A
        </button>
      </div>
      <DndContext>
        <StoryFolderedDraggableList folders={folders} dragId={dragId} debug />
      </DndContext>
    </div>
  );
}

function ControlledExpansionHarness() {
  const [expandedFolderIds, setExpandedFolderIds] = React.useState(() => fsSet(['layer-a']));
  const [lastExpanded, setLastExpanded] = React.useState('layer-a');

  const handleExpandedChange = (nextExpandedFolderIds: FSharpSet<string>) => {
    setExpandedFolderIds(nextExpandedFolderIds);
    setLastExpanded(Array.from(nextExpandedFolderIds).join(',') || 'none');
  };

  return (
    <div className="swt:flex swt:flex-col swt:gap-3 swt:w-96">
      <DndContext>
        <StoryFolderedDraggableList
          folders={initialFolders()}
          dragId={dragId}
          expandedFolderIds={expandedFolderIds}
          onExpandedFolderIdsChange={handleExpandedChange}
          debug
        />
      </DndContext>
      <pre data-testid="last-expanded">{lastExpanded}</pre>
    </div>
  );
}

function DragHarness() {
  const [lastDrag, setLastDrag] = React.useState('none');

  const onDragEnd = (event: DragEndEvent) => {
    const current = event.active.data.current as StoryDragData;
    setLastDrag(
      JSON.stringify({
        overId: event.over?.id ?? null,
        folderId: current.FolderId,
        folderName: current.FolderName,
        folderColor: current.FolderColor ?? null,
        itemId: current.ItemId,
        itemLabel: current.ItemLabel,
        itemColor: current.ItemColor ?? null,
        effectiveColor: current.EffectiveColor ?? null,
        layerId: current.Payload.LayerId,
        origin: current.Payload.Origin,
        header: current.Payload.Header,
      })
    );
  };

  return (
    <div className="swt:flex swt:flex-col swt:gap-3 swt:w-96">
      <DndContext onDragEnd={onDragEnd}>
        <StoryFolderedDraggableList
          folders={initialFolders()}
          dragId={dragId}
          defaultExpandedFolderIds={fsSet(['layer-a', 'layer-b'])}
          debug
        />
        <DropZone />
      </DndContext>
      <pre data-testid="last-drag">{lastDrag}</pre>
    </div>
  );
}

function getPointerGeometry(source: Element, target: Element) {
  const from = source.getBoundingClientRect();
  const to = target.getBoundingClientRect();
  const fromX = from.left + from.width / 2;
  const fromY = from.top + from.height / 2;
  const toX = to.left + to.width / 2;
  const toY = to.top + to.height / 2;
  const deltaX = toX - fromX;
  const deltaY = toY - fromY;
  const distance = Math.hypot(deltaX, deltaY) || 1;
  const activationX = fromX + (deltaX / distance) * 8;
  const activationY = fromY + (deltaY / distance) * 8;

  return { fromX, fromY, toX, toY, activationX, activationY };
}

const nextFrame = () => new Promise((resolve) => requestAnimationFrame(resolve));

async function beginDragByPointer(source: Element, target: Element = source) {
  const geometry = getPointerGeometry(source, target);

  fireEvent.pointerDown(source, {
    clientX: geometry.fromX,
    clientY: geometry.fromY,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });

  await nextFrame();

  fireEvent.pointerMove(target, {
    clientX: geometry.activationX,
    clientY: geometry.activationY,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });

  await nextFrame();

  return geometry;
}

async function dragByPointer(source: Element, target: Element) {
  const geometry = await beginDragByPointer(source, target);

  fireEvent.pointerMove(target, {
    clientX: geometry.toX,
    clientY: geometry.toY,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });

  await nextFrame();

  fireEvent.pointerUp(target, {
    clientX: geometry.toX,
    clientY: geometry.toY,
    button: 0,
    buttons: 0,
    isPrimary: true,
    pointerId: 1,
  });
}

const meta = {
  title: 'Composite Components/FolderedDraggableList',
  component: StoryFolderedDraggableList,
  parameters: { layout: 'centered' },
} satisfies Meta<typeof StoryFolderedDraggableList>;

export default meta;

type Story = StoryObj<typeof meta>;

export const TogglesFolders: Story = {
  render: () => (
    <DndContext>
      <div className="swt:w-96">
        <StoryFolderedDraggableList folders={initialFolders()} dragId={dragId} debug />
      </div>
    </DndContext>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const shelf = canvas.getByTestId('foldered-draggable-item-shelf');

    expect(shelf).toBeVisible();
    expect(within(shelf).queryByRole('button', { name: /Drag/i })).not.toBeInTheDocument();

    await userEvent.click(canvas.getByRole('button', { name: 'Expand Layer A' }));

    expect(within(shelf).getByTestId('foldered-draggable-item-layer-a-mass')).toBeVisible();
    expect(within(shelf).getByTestId('foldered-draggable-item-layer-a-species')).toBeVisible();
    expect(within(shelf).queryByTestId('foldered-draggable-item-layer-b-instrument')).not.toBeInTheDocument();
    expect(canvas.getByText('Species')).toBeVisible();
    expect(within(canvas.getByTestId('foldered-draggable-item-layer-a-species')).getByText('2')).toBeVisible();

    await userEvent.click(canvas.getByRole('button', { name: 'Expand Layer B' }));

    await waitFor(() => {
      expect(within(shelf).queryByTestId('foldered-draggable-item-layer-a-mass')).not.toBeInTheDocument();
      expect(within(shelf).getByTestId('foldered-draggable-item-layer-b-instrument')).toBeVisible();
    });

    await userEvent.click(canvas.getByRole('button', { name: 'Collapse Layer B' }));

    await waitFor(() => {
      expect(within(shelf).queryByRole('button', { name: /Drag/i })).not.toBeInTheDocument();
    });
  },
};

export const DefaultExpansionOpensInitialFolders: Story = {
  render: () => (
    <DndContext>
      <div className="swt:w-96">
        <StoryFolderedDraggableList
          folders={initialFolders()}
          dragId={dragId}
          defaultExpandedFolderIds={fsSet(['layer-a'])}
          debug
        />
      </div>
    </DndContext>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const shelf = canvas.getByTestId('foldered-draggable-item-shelf');

    expect(within(shelf).getByTestId('foldered-draggable-item-layer-a-mass')).toBeVisible();
    expect(within(shelf).queryByTestId('foldered-draggable-item-layer-b-instrument')).not.toBeInTheDocument();
  },
};

export const ControlledExpansionFollowsProps: Story = {
  render: () => <ControlledExpansionHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const shelf = canvas.getByTestId('foldered-draggable-item-shelf');

    expect(within(shelf).getByTestId('foldered-draggable-item-layer-a-mass')).toBeVisible();
    expect(within(shelf).queryByTestId('foldered-draggable-item-layer-b-instrument')).not.toBeInTheDocument();

    await userEvent.click(canvas.getByRole('button', { name: 'Expand Layer B' }));

    await waitFor(() => {
      expect(within(shelf).queryByTestId('foldered-draggable-item-layer-a-mass')).not.toBeInTheDocument();
      expect(within(shelf).getByTestId('foldered-draggable-item-layer-b-instrument')).toBeVisible();
      expect(canvas.getByTestId('last-expanded')).toHaveTextContent('layer-b');
    });
  },
};

export const HorizontalRowsScrollWhenOverflowing: Story = {
  render: () => (
    <DndContext>
      <div className="swt:w-72">
        <StoryFolderedDraggableList folders={overflowingFolders()} dragId={dragId} debug />
      </div>
    </DndContext>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const folderRow = canvas.getByTestId('foldered-draggable-folder-row');
    const shelf = canvas.getByTestId('foldered-draggable-item-shelf');

    expect(folderRow).toHaveClass('swt:flex-row');
    expect(folderRow).toHaveClass('swt:flex-nowrap');
    expect(folderRow).toHaveClass('swt:overflow-x-auto');
    expect(shelf).toBeVisible();
    expect(canvas.getAllByRole('button', { name: /Wide Layer/i })).toHaveLength(8);

    await userEvent.click(canvas.getByRole('button', { name: 'Expand Wide Layer 1' }));

    const itemRow = canvas.getByTestId('foldered-draggable-item-row');
    expect(itemRow).toHaveClass('swt:flex-row');
    expect(itemRow).toHaveClass('swt:flex-nowrap');
    expect(itemRow).toHaveClass('swt:overflow-x-auto');
    expect(within(itemRow).getAllByRole('button', { name: /Drag Property/i })).toHaveLength(8);
  },
};

export const UpdatesWhenFoldersPropChanges: Story = {
  render: () => <UpdatingHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const shelf = canvas.getByTestId('foldered-draggable-item-shelf');

    await userEvent.click(canvas.getByRole('button', { name: 'Expand Layer A' }));
    expect(within(shelf).getByTestId('foldered-draggable-item-layer-a-species')).toBeVisible();

    await userEvent.click(canvas.getByRole('button', { name: 'Add replicate' }));
    expect(await within(shelf).findByTestId('foldered-draggable-item-layer-a-replicate')).toBeVisible();
    const replicateSwatch = canvas
      .getByTestId('foldered-draggable-item-layer-a-replicate')
      .querySelector<HTMLElement>('[data-foldered-color-swatch="true"]');
    expect(replicateSwatch).not.toBeNull();
    expect(replicateSwatch!).toHaveStyle({ backgroundColor: '#dc2626' });

    await userEvent.click(canvas.getByRole('button', { name: 'Remove species' }));
    await waitFor(() => {
      expect(within(shelf).queryByTestId('foldered-draggable-item-layer-a-species')).not.toBeInTheDocument();
    });
    expect(within(shelf).getByTestId('foldered-draggable-item-layer-a-mass')).toBeVisible();

    await userEvent.click(canvas.getByRole('button', { name: 'Rename and recolor Layer A' }));
    expect(canvas.getByRole('button', { name: 'Collapse Layer Alpha' })).toBeVisible();
    const folderSwatch = canvas
      .getByTestId('foldered-draggable-folder-layer-a')
      .querySelector<HTMLElement>('[data-foldered-color-swatch="true"]');
    expect(folderSwatch).not.toBeNull();
    expect(folderSwatch!).toHaveStyle({ backgroundColor: '#7c3aed' });
  },
};

export const CarriesStructuredDragDataAndColor: Story = {
  render: () => <DragHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await dragByPointer(
      canvas.getByTestId('foldered-draggable-item-layer-a-species'),
      canvas.getByTestId('outside-drop')
    );

    await waitFor(() => {
      const lastDrag = canvas.getByTestId('last-drag');
      expect(lastDrag).toHaveTextContent('"overId":"outside-drop"');
      expect(lastDrag).toHaveTextContent('"folderId":"layer-a"');
      expect(lastDrag).toHaveTextContent('"folderName":"Layer A"');
      expect(lastDrag).toHaveTextContent('"folderColor":"#2563eb"');
      expect(lastDrag).toHaveTextContent('"itemId":"layer-a-species"');
      expect(lastDrag).toHaveTextContent('"itemColor":"#16a34a"');
      expect(lastDrag).toHaveTextContent('"effectiveColor":"#16a34a"');
      expect(lastDrag).toHaveTextContent('"layerId":"layer-a"');
      expect(lastDrag).toHaveTextContent('"origin":"previous-table"');
      expect(lastDrag).toHaveTextContent('"header":"Species"');
    });
  },
};

export const CarriesFolderFallbackColor: Story = {
  render: () => <DragHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await dragByPointer(
      canvas.getByTestId('foldered-draggable-item-layer-a-mass'),
      canvas.getByTestId('outside-drop')
    );

    await waitFor(() => {
      const lastDrag = canvas.getByTestId('last-drag');
      expect(lastDrag).toHaveTextContent('"overId":"outside-drop"');
      expect(lastDrag).toHaveTextContent('"itemId":"layer-a-mass"');
      expect(lastDrag).toHaveTextContent('"itemColor":null');
      expect(lastDrag).toHaveTextContent('"effectiveColor":"#2563eb"');
    });
  },
};

export const ShowsDragPreviewAndRestoresUnacceptedDrag: Story = {
  render: () => <DragHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const shelf = canvas.getByTestId('foldered-draggable-item-shelf');
    const source = within(shelf).getByTestId('foldered-draggable-item-layer-a-mass');
    const geometry = await beginDragByPointer(source);

    await waitFor(() => {
      expect(source).not.toBeVisible();
      expect(within(document.body).getByTestId('foldered-draggable-drag-overlay')).toBeVisible();
    });

    fireEvent.pointerUp(document, {
      clientX: geometry.activationX,
      clientY: geometry.activationY,
      button: 0,
      buttons: 0,
      isPrimary: true,
      pointerId: 1,
    });

    await waitFor(() => {
      expect(within(shelf).getByTestId('foldered-draggable-item-layer-a-mass')).toBeVisible();
      expect(within(document.body).queryByTestId('foldered-draggable-drag-overlay')).not.toBeInTheDocument();
    });
  },
};

export const DisabledItemsDoNotDrag: Story = {
  render: () => <DragHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: 'Expand Layer B' }));

    const disabledItem = canvas.getByTestId('foldered-draggable-item-layer-b-archived');
    expect(disabledItem).toBeDisabled();

    await dragByPointer(disabledItem, canvas.getByTestId('outside-drop'));

    expect(canvas.getByTestId('last-drag')).toHaveTextContent('none');
  },
};
