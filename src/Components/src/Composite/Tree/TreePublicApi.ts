import type { ReactElement, Ref, MouseEvent as ReactMouseEvent } from "react";
import { useImperativeHandle, useMemo, useRef } from "react";
import type GeneratedTree from "../../dist/Composite/Tree/Tree.fs";
import type {
  nativeOptionValue as NativeOptionValue,
  TreeItem$1 as GeneratedItem,
  TreeRenderProps$1 as GeneratedRenderProps,
  TreeDataSource$1 as GeneratedDataSource,
} from "../../dist/Composite/Tree/Types.fs";
import type {
  ContextMenuItem,
  ContextMenuClickEvent,
} from "../../dist/Primitive/ContextMenu/Types.fs";

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
  select: (
    event: Pick<
      ReactMouseEvent,
      "shiftKey" | "ctrlKey" | "metaKey" | "preventDefault" | "stopPropagation"
    >,
  ) => void;
}

export interface TreeDataSource<T> {
  getTreeItems: (item?: TreeItem<T>) => Promise<TreeItem<T>[]>;
  /** Stable identity for inline callbacks. Change this key to invalidate the source cache. */
  cacheKey?: string;
}

export interface TreeApi {
  invalidateNode: (nodeId: string) => void;
  invalidateAll: () => void;
}

export interface TreeContextMenuTarget<T> {
  event: MouseEvent;
  item?: TreeItem<T>;
}
export type TreeContextMenuItem<T = unknown> = Omit<
  ContextMenuItem,
  "onClick" | "isDivider"
> & {
  isDivider?: boolean;
  onClick?: (args: {
    buttonEvent: ContextMenuClickEvent;
    spawnData: TreeContextMenuTarget<T>;
  }) => void;
};

export interface TreeProps<T> {
  items: TreeItem<T>[];
  /** Keep getTreeItems stable, or provide a stable cacheKey for inline callbacks.
   * A changed callback/key replaces the source; a new wrapper object alone does not reset it. */
  dataSource?: TreeDataSource<T>;
  selectionMode?: TreeSelectionMode;
  /** Parent-owned value: switching mode does not rewrite it or emit onSelectionChange.
   * Single mode displays only its last ID; returning to Multiple displays the parent value again. */
  selectedIds?: string[];
  defaultSelectedIds?: string[];
  defaultExpandedIds?: string[];
  onSelectionChange?: (selectedIds: string[]) => void;
  isSelectionDisabled?: boolean;
  isNodeSelectable?: (item: TreeItem<T>) => boolean;
  enableVirtualization?: boolean;
  estimateNodeHeight?: number;
  /** Classes for the virtual scroll container; replaces the default swt:max-h-96 height. */
  viewportClassName?: string;
  onContextMenu?: (
    event: MouseEvent,
    item?: TreeItem<T>,
  ) => TreeContextMenuItem<T>[];
  /** Use a stable callback (useCallback) for selective row memoization.
   * A changed callback rerenders its output, including newly captured parent state. */
  renderNode?: (props: TreeRenderProps<T>) => ReactElement;
  leading?: (props: TreeRenderProps<T>) => ReactElement;
  trailing?: (props: TreeRenderProps<T>) => ReactElement;
  styleFn?: (item: TreeItem<T> | undefined, classes: string[]) => string[];
  onError?: (error: Error) => void;
  ref?: Ref<TreeApi>;
  ariaLabel?: string;
}

export type TreeComponent = <T>(props: TreeProps<T>) => ReactElement;

// Fable compiler/runtime versions represent exn differently; use their common error fields
// at this boundary while retaining the generated signature for every other prop.
type GeneratedTreeImplementation = <T>(
  props: Omit<Parameters<typeof GeneratedTree<T>>[0], "onError"> & {
    onError?: (error: Pick<Error, "message" | "stack">) => void;
  },
) => ReactElement;

/** Adapts the generated signature with checked assignments, without asserting an unknown component. */
export const bindTree = (
  implementation: GeneratedTreeImplementation,
  optionValue: typeof NativeOptionValue,
): TreeComponent => {
  const fromItem = <T>(node: GeneratedItem<T>): TreeItem<T> => {
    const props = { ...node.props, data: optionValue<T>(node.props.data) };
    return node.type === "leaf"
      ? { type: "leaf", props }
      : {
          type: "branch",
          props,
          children: node.children && Array.from(node.children, fromItem<T>),
        };
  };

  return function NativeTree<T>(props: TreeProps<T>) {
    const apiRef = useRef<TreeApi>({
      invalidateNode: () => {},
      invalidateAll: () => {},
    });
    const renderers = useMemo(() => {
      const adapt = (render?: (props: TreeRenderProps<T>) => ReactElement) =>
        render &&
        ((state: GeneratedRenderProps<T>) =>
          render({
            ...state,
            node: fromItem(state.node),
            error: optionValue<string>(state.error),
          }));
      return {
        renderNode: adapt(props.renderNode),
        leading: adapt(props.leading),
        trailing: adapt(props.trailing),
      };
    }, [props.renderNode, props.leading, props.trailing]);

    const dataSource = useMemo<GeneratedDataSource<T> | undefined>(
      () =>
        props.dataSource && {
          cacheKey: props.dataSource.cacheKey,
          getTreeItems: async (item) => {
            const node = optionValue<GeneratedItem<T>>(item);
            return props.dataSource!.getTreeItems(node && fromItem(node));
          },
        },
      [props.dataSource?.cacheKey, props.dataSource?.getTreeItems],
    );

    const output = implementation<T>({
      ...props,
      ...renderers,
      dataSource,
      ref: apiRef,
      onError:
        props.onError &&
        ((error) =>
          props.onError!(
            Object.assign(new Error(error.message), error, {
              stack: error.stack,
            }),
          )),
      onSelectionChange:
        props.onSelectionChange &&
        ((ids) => props.onSelectionChange!(Array.from(ids))),
      isNodeSelectable:
        props.isNodeSelectable &&
        ((item) => props.isNodeSelectable!(fromItem(item))),
      onContextMenu:
        props.onContextMenu &&
        ((event, item) => {
          const node = optionValue<GeneratedItem<T>>(item);
          return props.onContextMenu!(event, node && fromItem(node)).map(
            (entry) => ({
              ...entry,
              isDivider: entry.isDivider ?? false,
              onClick:
                entry.onClick &&
                ((
                  args: Parameters<NonNullable<ContextMenuItem["onClick"]>>[0],
                ) =>
                  entry.onClick!({
                    buttonEvent: args.buttonEvent,
                    spawnData: { event, item: node && fromItem(node) },
                  })),
            }),
          );
        }),
      styleFn:
        props.styleFn &&
        ((item, classes) => {
          const node = optionValue<GeneratedItem<T>>(item);
          return props.styleFn!(node && fromItem(node), Array.from(classes));
        }),
    });
    useImperativeHandle(props.ref, () => apiRef.current!, [apiRef]);
    return output;
  };
};
