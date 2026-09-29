import type { ReactElement, Ref } from "react";

export type TreeSelectionMode = "single" | "multiple";

export interface TreeItemProps<T> {
  id: string;
  label: string;
  data?: T;
  icon?: ReactElement;
  tooltip?: string;
  leading?: ReactElement;
  trailing?: ReactElement;
  className?: string;
}

export type TreeItem<T> =
  | { type: "leaf"; props: TreeItemProps<T> }
  | { type: "branch"; props: TreeItemProps<T>; children?: TreeItem<T>[] };

export interface TreeRenderProps<T> {
  node: TreeItem<T>;
  depth: number;
  isExpanded: boolean;
  isSelected: boolean;
  isActive: boolean;
  isFocused: boolean;
  isLoading: boolean;
  error?: string;
  toggle: () => void;
  select: (event: MouseEvent) => void;
}

export interface TreeDataSource<T> {
  getTreeItems: (item?: TreeItem<T>) => Promise<TreeItem<T>[]>;
}

export interface TreeApi {
  invalidateNode: (nodeId: string) => void;
  invalidateAll: () => void;
}

export interface TreeContextMenuItem {
  text?: ReactElement;
  icon?: ReactElement;
  label?: string;
  kbdbutton?: { element: ReactElement; label: string };
  isDivider?: boolean;
  onClick?: (args: { buttonEvent: MouseEvent; spawnData: unknown }) => void;
}

export interface TreeProps<T> {
  items: TreeItem<T>[];
  dataSource?: TreeDataSource<T>;
  selectionMode?: TreeSelectionMode;
  selectedIds?: string[];
  defaultSelectedIds?: string[];
  defaultExpandedIds?: string[];
  onSelectionChange?: (selectedIds: string[]) => void;
  isSelectionDisabled?: boolean;
  isNodeSelectable?: (item: TreeItem<T>) => boolean;
  enableVirtualization?: boolean;
  estimateNodeHeight?: number;
  onContextMenu?: (
    event: MouseEvent,
    item?: TreeItem<T>,
  ) => TreeContextMenuItem[];
  renderNode?: (props: TreeRenderProps<T>) => ReactElement;
  leading?: (props: TreeRenderProps<T>) => ReactElement;
  trailing?: (props: TreeRenderProps<T>) => ReactElement;
  styleFn?: (item: TreeItem<T> | undefined, classes: string[]) => string[];
  onError?: (error: Error) => void;
  ref?: Ref<TreeApi>;
  ariaLabel?: string;
  debug?: boolean;
}

export type TreeComponent = <T>(props: TreeProps<T>) => ReactElement;

/** Applies the native TypeScript boundary to the Fable-generated implementation. */
export const bindTree = (implementation: unknown): TreeComponent =>
  implementation as TreeComponent;
