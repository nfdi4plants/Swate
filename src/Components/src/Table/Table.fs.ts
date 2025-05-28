import { FSharpChoice$2_$union, FSharpChoice$2_Choice2Of2, FSharpChoice$2_Choice1Of2 } from "../fable_modules/fable-library-ts.4.24.0/Choice.js";
import { last, empty, exists, map as map_1, collect, toArray as toArray_2, singleton, append, delay, toList, iterate } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { int32 } from "../fable_modules/fable-library-ts.4.24.0/Int32.js";
import { singleton as singleton_1, toArray as toArray_1, ofSeq, FSharpSet__get_MinimumElement, FSharpSet } from "../fable_modules/fable-library-ts.4.24.0/Set.js";
import { value as value_99, map, defaultArg, toArray, Option } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { createElement, ReactElement } from "react";
import React from "react";
import * as react from "react";
import { IRefValue$1 } from "../fable_modules/Fable.React.Types.18.4.0/Fable.React.fs.js";
import { TableCellController_init_Z95CEA69, TableCellController } from "../Util/Types.fs.js";
import { reactApi } from "../fable_modules/Feliz.2.9.0/./Interop.fs.js";
import { VirtualItem, Virtualizer, Range as Range$, defaultRangeExtractor, useVirtualizer } from "@tanstack/react-virtual";
import { defaultOf as defaultOf_1, createObj, compare, equals, curry2, comparePrimitives } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { Feliz_React__React_useGridSelect_Static_4CE412FE } from "./KeyboardNavigation.fs.js";
import { rangeDouble } from "../fable_modules/fable-library-ts.4.24.0/Range.js";
import { IStyleAttribute, IReactProperty } from "../fable_modules/Feliz.2.9.0/Types.fs.js";
import { join } from "../fable_modules/fable-library-ts.4.24.0/String.js";
import { Feliz_prop__prop_dataColumn_Static_Z524259A4, Feliz_prop__prop_dataRow_Static_Z524259A4 } from "../Util/Extensions.fs.js";
import { BaseActiveTableCell, BaseCell } from "./TableCell.fs.js";
import { ofArray } from "../fable_modules/fable-library-ts.4.24.0/List.js";
import { defaultOf } from "../fable_modules/Feliz.2.9.0/../../Util/../fable_modules/fable-library-ts.4.24.0/Util.js";
import { memo } from "../fable_modules/Feliz.2.9.0/./Internal.fs.js";
import { item } from "../fable_modules/fable-library-ts.4.24.0/Array.js";

function TableHelper_$007CActiveTrigger$007CDefault$007C(eventCode: string): FSharpChoice$2_$union<void, void> {
    const lower: string = eventCode.toLocaleLowerCase();
    if (((lower.startsWith("key") ? true : lower.startsWith("digit")) ? true : lower.startsWith("numpad")) ? true : lower.startsWith("backspace")) {
        return FSharpChoice$2_Choice1Of2<void, void>(undefined);
    }
    else {
        return FSharpChoice$2_Choice2Of2<void, void>(undefined);
    }
}

function TableHelper_keyDownController(e: KeyboardEvent, selectedCells: { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> }, activeCellIndex: Option<{ x: int32, y: int32 }>, setActiveCellIndex: ((arg0: Option<{ x: int32, y: int32 }>) => void), onKeydown: Option<((arg0: [KeyboardEvent, { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> }, Option<{ x: int32, y: int32 }>]) => void)>): void {
    if (activeCellIndex == null) {
        const nav: boolean = selectedCells.selectBy(e);
        if (!nav && (selectedCells.count > 0)) {
            iterate<((arg0: [KeyboardEvent, { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> }, Option<{ x: int32, y: int32 }>]) => void)>((onKeydown_1: ((arg0: [KeyboardEvent, { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> }, Option<{ x: int32, y: int32 }>]) => void)): void => {
                onKeydown_1([e, selectedCells, activeCellIndex] as [KeyboardEvent, { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> }, Option<{ x: int32, y: int32 }>]);
            }, toArray<((arg0: [KeyboardEvent, { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> }, Option<{ x: int32, y: int32 }>]) => void)>(onKeydown));
            const matchValue: string = e.code;
            if (TableHelper_$007CActiveTrigger$007CDefault$007C(matchValue).tag === /* Choice2Of2 */ 1) {
            }
            else {
                setActiveCellIndex(defaultArg(selectedCells.SelectOrigin, FSharpSet__get_MinimumElement(selectedCells.selectedCellsReducedSet)));
            }
        }
    }
}

export function get_TableCellStyle(): string {
    return "swt:data-[selected=true]:text-secondary-content swt:data-[selected=true]:bg-secondary\nswt:data-[is-append-origin=true]:border swt:data-[is-append-origin=true]:border-base-content\nswt:data-[active=true]:border-2 swt:data-[active=true]:border-primary swt:data-[active=true]:!bg-transparent\nswt:cursor-pointer\nswt:select-none\nswt:p-0";
}

export function Table(tableInputProps: any): ReactElement {
    const debug: Option<boolean> = tableInputProps.debug;
    const defaultStyleSelect: Option<boolean> = tableInputProps.defaultStyleSelect;
    const enableColumnHeaderSelect: Option<boolean> = tableInputProps.enableColumnHeaderSelect;
    const onKeydown: Option<((arg0: [KeyboardEvent, { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> }, Option<{ x: int32, y: int32 }>]) => void)> = tableInputProps.onKeydown;
    const onSelect: Option<((arg0: { x: int32, y: int32 }, arg1: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void)> = tableInputProps.onSelect;
    const ref: IRefValue$1<TableHandle> = tableInputProps.ref;
    const renderActiveCell: ((arg0: TableCellController) => ReactElement) = tableInputProps.renderActiveCell;
    const renderCell: ((arg0: TableCellController) => ReactElement) = tableInputProps.renderCell;
    const columnCount: int32 = tableInputProps.columnCount;
    const rowCount: int32 = tableInputProps.rowCount;
    const debug_1: boolean = defaultArg<boolean>(debug, false);
    const enableColumnHeaderSelect_1: boolean = defaultArg<boolean>(enableColumnHeaderSelect, false);
    const defaultStyleSelect_1: boolean = defaultArg<boolean>(defaultStyleSelect, true);
    const scrollContainerRef: IRefValue$1<Option<HTMLElement>> = reactApi.useRef(undefined);
    const rowVirtualizer: Virtualizer<any, any> = useVirtualizer<any, any>({
        count: rowCount,
        getScrollElement: (): Option<HTMLElement> => scrollContainerRef.current,
        estimateSize: (_arg: int32): int32 => 40,
        scrollPaddingStart: 1.5 * 40,
        scrollPaddingEnd: 1.5 * 40,
        overscan: 2,
        rangeExtractor: (range: Range$): int32[] => {
            const next: FSharpSet<int32> = ofSeq(toList<int32>(delay<int32>((): Iterable<int32> => append<int32>(singleton<int32>(0), delay<int32>((): Iterable<int32> => defaultRangeExtractor(range))))), {
                Compare: comparePrimitives,
            });
            return toArray_1<int32>(next);
        },
        gap: 0,
    });
    const columnVirtualizer: Virtualizer<any, any> = useVirtualizer<any, any>({
        count: columnCount,
        getScrollElement: (): Option<HTMLElement> => scrollContainerRef.current,
        estimateSize: (_arg_1: int32): int32 => 200,
        scrollPaddingEnd: 1.5 * 200,
        overscan: 2,
        rangeExtractor: (range_1: Range$): int32[] => {
            const next_1: FSharpSet<int32> = ofSeq(toList<int32>(delay<int32>((): Iterable<int32> => append<int32>(singleton<int32>(0), delay<int32>((): Iterable<int32> => defaultRangeExtractor(range_1))))), {
                Compare: comparePrimitives,
            });
            return toArray_1<int32>(next_1);
        },
        horizontal: true,
        gap: 0,
    });
    const scrollTo = (coordinate: { x: int32, y: int32 }): void => {
        rowVirtualizer.scrollToIndex(coordinate.y);
        columnVirtualizer.scrollToIndex(coordinate.x);
    };
    const onSelect_2 = (cell: { x: int32, y: int32 }, newCellRange: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>): void => {
        iterate<((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))>((onSelect_1: ((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))): void => {
            onSelect_1(cell)(newCellRange);
        }, toArray<((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))>(map(curry2, onSelect)));
        scrollTo(cell);
    };
    const patternInput: [Option<{ x: int32, y: int32 }>, ((arg0: Option<{ x: int32, y: int32 }>) => void)] = reactApi.useState<Option<{ x: int32, y: int32 }>, Option<{ x: int32, y: int32 }>>(undefined);
    const setActiveCellIndex: ((arg0: Option<{ x: int32, y: int32 }>) => void) = patternInput[1];
    const activeCellIndex: Option<{ x: int32, y: int32 }> = patternInput[0];
    const GridSelect: { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> } = Feliz_React__React_useGridSelect_Static_4CE412FE(rowCount, columnCount, enableColumnHeaderSelect_1 ? 0 : 1, 1, onSelect_2);
    const dependencies_1: any[] = [GridSelect.selectedCells];
    reactApi.useImperativeHandle<TableHandle>(ref, (): TableHandle => ({
        focus: (): void => {
            value_99(scrollContainerRef.current).focus();
        },
        scrollTo: scrollTo,
        SelectHandle: {
            contains: GridSelect.contains,
            selectAt: GridSelect.selectAt,
            clear: GridSelect.clear,
            getSelectedCellRange: (): Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }> => GridSelect.selectedCells,
            getSelectedCells: (): { x: int32, y: int32 }[] => {
                const matchValue: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }> = GridSelect.selectedCells;
                if (matchValue == null) {
                    return [];
                }
                else {
                    const range_2: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 } = value_99(matchValue);
                    return Array.from(toArray_2<{ x: int32, y: int32 }>(delay<{ x: int32, y: int32 }>((): Iterable<{ x: int32, y: int32 }> => collect<int32, Iterable<{ x: int32, y: int32 }>, { x: int32, y: int32 }>((x_2: int32): Iterable<{ x: int32, y: int32 }> => map_1<int32, { x: int32, y: int32 }>((y_2: int32): { x: int32, y: int32 } => ({
                        x: x_2,
                        y: y_2,
                    }), rangeDouble(range_2.yStart, 1, range_2.yEnd)), rangeDouble(range_2.xStart, 1, range_2.xEnd)))));
                }
            },
            getCount: (): int32 => GridSelect.count,
        },
    }), dependencies_1);
    const createController = (index: { x: int32, y: int32 }): TableCellController => {
        const isSelected: boolean = GridSelect.contains(index);
        const isOrigin: boolean = exists<{ x: int32, y: int32 }>((origin: { x: int32, y: int32 }): boolean => equals(origin, index), toArray<{ x: int32, y: int32 }>(GridSelect.SelectOrigin));
        const isActive: boolean = equals(activeCellIndex, index);
        return TableCellController_init_Z95CEA69(index, isActive, isSelected, isOrigin, (e: KeyboardEvent): void => {
            const matchValue_1: string = e.code;
            switch (matchValue_1) {
                case "Enter": {
                    if (GridSelect.contains({
                        x: index.x,
                        y: index.y + 1,
                    })) {
                        setActiveCellIndex({
                            x: index.x,
                            y: index.y + 1,
                        });
                    }
                    else if (GridSelect.contains({
                        x: index.x + 1,
                        y: value_99(GridSelect.selectedCells).yStart,
                    })) {
                        setActiveCellIndex({
                            x: index.x + 1,
                            y: value_99(GridSelect.selectedCells).yStart,
                        });
                    }
                    else {
                        setActiveCellIndex(undefined);
                        value_99(scrollContainerRef.current).focus();
                        GridSelect.selectAt([index, false] as [{ x: int32, y: int32 }, boolean]);
                    }
                    break;
                }
                case "Escape": {
                    setActiveCellIndex(undefined);
                    value_99(scrollContainerRef.current).focus();
                    GridSelect.clear();
                    break;
                }
                default:
                    undefined;
            }
        }, (_arg_2: FocusEvent): void => {
            setActiveCellIndex(undefined);
        }, (e_1: MouseEvent): void => {
            if (!isActive) {
                if (e_1.detail >= 2) {
                    setActiveCellIndex(index);
                    if (GridSelect.count === 0) {
                        GridSelect.selectAt([index, false] as [{ x: int32, y: int32 }, boolean]);
                    }
                }
                else {
                    const nextSet: FSharpSet<{ x: int32, y: int32 }> = singleton_1<{ x: int32, y: int32 }>(index, {
                        Compare: compare,
                    });
                    if (GridSelect.selectedCellsReducedSet.Equals(nextSet)) {
                        GridSelect.clear();
                    }
                    else {
                        GridSelect.selectAt([index, e_1.shiftKey] as [{ x: int32, y: int32 }, boolean]);
                    }
                }
            }
        });
    };
    const xs_9: Iterable<ReactElement> = [createElement<any>("div", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["ref", scrollContainerRef] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onKeyDown", (e_2: KeyboardEvent): void => {
        TableHelper_keyDownController(e_2, GridSelect, activeCellIndex, setActiveCellIndex, onKeydown);
    }] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["tabIndex", 0] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", "swt:overflow-auto swt:h-96 swt:w-full swt:border swt:border-primary swt:rounded-sm"] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(debug_1 ? singleton<IReactProperty>(["data-testid", "virtualized-table"] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => {
        let elems_8: Iterable<ReactElement>, elems_7: Iterable<ReactElement>, elems_6: Iterable<ReactElement>, elems_2: Iterable<ReactElement>, elems_1: Iterable<ReactElement>, elems_5: Iterable<ReactElement>;
        return singleton<IReactProperty>((elems_8 = [createElement<any>("div", createObj(ofArray([["style", {
            height: rowVirtualizer.getTotalSize(),
            width: columnVirtualizer.getTotalSize(),
            position: "relative",
        }] as [string, any], (elems_7 = [createElement<any>("table", createObj(ofArray([["className", "swt:w-full swt:h-full swt:border-collapse"] as [string, any], (elems_6 = [createElement<any>("thead", createObj(ofArray([["key", "table-thead"] as [string, any], ["className", "swt:[&>tr>th]:border swt:[&>tr>th]:border-neutral"] as [string, any], (elems_2 = [createElement<any>("tr", createObj(ofArray([["key", "header"] as [string, any], ["className", "swt:sticky swt:top-0 swt:left-0 swt:z-10 swt:bg-base-100 swt:text-left"] as [string, any], ["style", {
            height: 40,
        }] as [string, any], (elems_1 = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => collect<VirtualItem, Iterable<ReactElement>, ReactElement>((virtualColumn: VirtualItem): Iterable<ReactElement> => {
            const controller: TableCellController = createController({
                x: virtualColumn.index,
                y: 0,
            });
            return singleton<ReactElement>(createElement<any>("th", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["ref", columnVirtualizer.measureElement] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["data-index", virtualColumn.index] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["key", virtualColumn.key] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", join(" ", toList<string>(delay<string>((): Iterable<string> => append<string>((virtualColumn.index !== 0) ? singleton<string>("swt:min-w-32") : singleton<string>("swt:min-w-min"), delay<string>((): Iterable<string> => append<string>(singleton<string>("swt:h-full swt:resize-x swt:overflow-x-auto"), delay<string>((): Iterable<string> => (defaultStyleSelect_1 ? singleton<string>(get_TableCellStyle()) : empty<string>()))))))))] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(Feliz_prop__prop_dataRow_Static_Z524259A4(0)), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(Feliz_prop__prop_dataColumn_Static_Z524259A4(virtualColumn.index)), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["style", {
                position: "absolute",
                top: 0,
                left: 0,
                transform: `translateX(${virtualColumn.start}px)`,
            }] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(controller.IsActive ? singleton<IReactProperty>(["data-active", true] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(controller.IsSelected ? singleton<IReactProperty>(["data-selected", true] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(controller.IsOrigin ? singleton<IReactProperty>(["data-is-append-origin", true] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => {
                let elems: Iterable<ReactElement>;
                return singleton<IReactProperty>((elems = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => ((virtualColumn.index === 0) ? singleton<ReactElement>(BaseCell<Iterable<IReactProperty>>(controller.Index.y, controller.Index.x, last<int32>(rowVirtualizer.getVirtualIndexes()), "swt:px-2 swt:py-1 swt:flex swt:items-center swt:cursor-not-allowed swt:w-full swt:h-full swt:min-w-8 /\n                                                                            swt:bg-base-200 swt:text-transparent")) : (controller.IsActive ? singleton<ReactElement>(renderActiveCell(controller)) : singleton<ReactElement>(renderCell(controller)))))), ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any]));
            })))))))))))))))))))))))));
        }, columnVirtualizer.getVirtualItems()))), ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any])]))), createElement<any>("tbody", createObj(ofArray([["style", {
            marginTop: 40,
        }] as [string, any], ["className", "swt:[&>tr>td]:border swt:[&>tr>td]:border-neutral"] as [string, any], (elems_5 = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => collect<VirtualItem, Iterable<ReactElement>, ReactElement>((virtualRow: VirtualItem): Iterable<ReactElement> => {
            let elems_4: Iterable<ReactElement>;
            return (virtualRow.index === 0) ? singleton<ReactElement>(defaultOf()) : singleton<ReactElement>(createElement<any>("tr", createObj(ofArray([["key", virtualRow.key] as [string, any], ["style", {
                top: 0,
                left: 0,
                position: "absolute",
                transform: `translateY(${virtualRow.start}px)`,
                height: virtualRow.size,
            }] as [string, any], ["className", "swt:w-full"] as [string, any], (elems_4 = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => collect<VirtualItem, Iterable<ReactElement>, ReactElement>((virtualColumn_1: VirtualItem): Iterable<ReactElement> => {
                const index_2: { x: int32, y: int32 } = {
                    x: virtualColumn_1.index,
                    y: virtualRow.index,
                };
                const controller_1: TableCellController = createController(index_2);
                return singleton<ReactElement>(createElement<any>("td", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["key", virtualColumn_1.key] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(Feliz_prop__prop_dataRow_Static_Z524259A4(virtualRow.index)), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(Feliz_prop__prop_dataColumn_Static_Z524259A4(virtualColumn_1.index)), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", join(" ", toList<string>(delay<string>((): Iterable<string> => (defaultStyleSelect_1 ? singleton<string>(get_TableCellStyle()) : empty<string>()))))] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(controller_1.IsActive ? singleton<IReactProperty>(["data-active", true] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(controller_1.IsSelected ? singleton<IReactProperty>(["data-selected", true] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(controller_1.IsOrigin ? singleton<IReactProperty>(["data-is-append-origin", true] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["style", createObj(toList<IStyleAttribute>(delay<IStyleAttribute>((): Iterable<IStyleAttribute> => append<IStyleAttribute>((virtualColumn_1.index === 0) ? append<IStyleAttribute>(singleton<IStyleAttribute>(["position", "sticky"] as [string, any]), delay<IStyleAttribute>((): Iterable<IStyleAttribute> => singleton<IStyleAttribute>(["zIndex", 10] as [string, any]))) : singleton<IStyleAttribute>(["position", "absolute"] as [string, any]), delay<IStyleAttribute>((): Iterable<IStyleAttribute> => append<IStyleAttribute>(singleton<IStyleAttribute>(["width", virtualColumn_1.size] as [string, any]), delay<IStyleAttribute>((): Iterable<IStyleAttribute> => append<IStyleAttribute>(singleton<IStyleAttribute>(["height", virtualRow.size] as [string, any]), delay<IStyleAttribute>((): Iterable<IStyleAttribute> => append<IStyleAttribute>(singleton<IStyleAttribute>(["top", 0] as [string, any]), delay<IStyleAttribute>((): Iterable<IStyleAttribute> => append<IStyleAttribute>(singleton<IStyleAttribute>(["left", 0] as [string, any]), delay<IStyleAttribute>((): Iterable<IStyleAttribute> => singleton<IStyleAttribute>(["transform", `translateX(${virtualColumn_1.start}px)`] as [string, any]))))))))))))))] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => {
                    let elems_3: Iterable<ReactElement>;
                    return singleton<IReactProperty>((elems_3 = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => (controller_1.IsActive ? singleton<ReactElement>(renderActiveCell(controller_1)) : singleton<ReactElement>(renderCell(controller_1))))), ["children", reactApi.Children.toArray(Array.from(elems_3))] as [string, any]));
                })))))))))))))))))))));
            }, columnVirtualizer.getVirtualItems()))), ["children", reactApi.Children.toArray(Array.from(elems_4))] as [string, any])]))));
        }, rowVirtualizer.getVirtualItems()))), ["children", reactApi.Children.toArray(Array.from(elems_5))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_6))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_7))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_8))] as [string, any]));
    }))))))))))))))];
    return react.createElement(react.Fragment, {}, ...xs_9);
}

export default Table;

export function Entry(): ReactElement {
    let elems: Iterable<ReactElement>;
    let TableHandler: IRefValue$1<TableHandle>;
    const initialValue: TableHandle = defaultOf_1();
    TableHandler = (reactApi.useRef(initialValue));
    const rowCount = 1000;
    const columnCount = 1000;
    let patternInput: [string[][], ((arg0: string[][]) => void)];
    const initial: string[][] = toArray_2<string[]>(delay<string[]>((): Iterable<string[]> => map_1<int32, string[]>((i: int32): string[] => toArray_2<string>(delay<string>((): Iterable<string> => map_1<int32, string>((j: int32): string => (`Row ${i}, Column ${j}`), rangeDouble(0, 1, columnCount - 1)))), rangeDouble(0, 1, rowCount - 1))));
    patternInput = reactApi.useState<string[][], string[][]>(initial);
    const setData: ((arg0: string[][]) => void) = patternInput[1];
    const data: string[][] = patternInput[0];
    const render_1: ((arg0: TableCellController) => ReactElement) = memo<TableCellController>((ts: TableCellController): ReactElement => BaseCell<Iterable<IReactProperty>>(ts.Index.y, ts.Index.x, createElement<any>("div", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["key", `${ts.Index.x}-${ts.Index.y}`] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", "swt:truncate"] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>((ts.Index.x === 0) ? singleton<IReactProperty>(["children", ts.Index.y] as [string, any]) : singleton<IReactProperty>(["children", item(ts.Index.x, item(ts.Index.y, data))] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["onClick", (e: MouseEvent): void => {
        ts.onClick(e);
    }] as [string, any])))))))))))), undefined, undefined, (ts_1: TableCellController): string => (`${ts_1.Index.x}-${ts_1.Index.y}`));
    const renderActiveCell: ((arg0: TableCellController) => ReactElement) = memo<TableCellController>((ts_2: TableCellController): ReactElement => createElement(BaseActiveTableCell, {
        ts: ts_2,
        data: item(ts_2.Index.x, item(ts_2.Index.y, data)),
        setData: (newData: string): void => {
            item(ts_2.Index.y, data)[ts_2.Index.x] = newData;
            setData(data);
        },
    }), undefined, undefined, (ts_3: TableCellController): string => (`${ts_3.Index.x}-${ts_3.Index.y}`));
    return createElement<any>("div", createObj(ofArray([["className", "swt:flex swt:flex-col swt:gap-4"] as [string, any], (elems = [createElement<any>("button", {
        className: "swt:btn swt:btn-primary",
        children: "scroll to 500, 500",
        onClick: (_arg: MouseEvent): void => {
            TableHandler.current.SelectHandle.selectAt([{
                x: 500,
                y: 500,
            }, false] as [{ x: int32, y: int32 }, boolean]);
            TableHandler.current.scrollTo({
                x: 500,
                y: 500,
            });
        },
    }), createElement(Table, {
        rowCount: rowCount,
        columnCount: columnCount,
        renderCell: render_1,
        renderActiveCell: renderActiveCell,
        ref: TableHandler,
    })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
}

