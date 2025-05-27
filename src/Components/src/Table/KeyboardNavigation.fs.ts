import { toArray, map, defaultArg, unwrap, value, Option } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { toText, printf, toFail } from "../fable_modules/fable-library-ts.4.24.0/String.js";
import { int32 } from "../fable_modules/fable-library-ts.4.24.0/Int32.js";
import { FSharpSet__get_Count, singleton, FSharpSet__get_MaximumElement, FSharpSet__get_MinimumElement, FSharpSet__get_IsEmpty, FSharpSet, empty, ofSeq } from "../fable_modules/fable-library-ts.4.24.0/Set.js";
import { curry2, equals, compare } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { class_type, TypeInfo } from "../fable_modules/fable-library-ts.4.24.0/Reflection.js";
import { min as min_1, max as max_1 } from "../fable_modules/fable-library-ts.4.24.0/Double.js";
import { iterate } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { reactApi } from "../fable_modules/Feliz.2.9.0/./Interop.fs.js";
import { IRefValue$1 } from "../fable_modules/Fable.React.Types.18.4.0/Fable.React.fs.js";

export type GridSelectModule_Kbd = 
    | "arrowUp"
    | "arrowDown"
    | "arrowLeft"
    | "arrowRight"

export function GridSelectModule_Kbd_tryFromKey_Z721C83C5(key: string): Option<GridSelectModule_Kbd> {
    switch (key) {
        case "ArrowUp":
            return "arrowUp";
        case "ArrowDown":
            return "arrowDown";
        case "ArrowLeft":
            return "arrowLeft";
        case "ArrowRight":
            return "arrowRight";
        default:
            return undefined;
    }
}

export function GridSelectModule_Kbd_fromKey_Z721C83C5(key: string): GridSelectModule_Kbd {
    const matchValue: Option<GridSelectModule_Kbd> = GridSelectModule_Kbd_tryFromKey_Z721C83C5(key);
    if (matchValue == null) {
        return toFail(printf("Unknown key: %s"))(key);
    }
    else {
        const kbd: GridSelectModule_Kbd = value(matchValue);
        return kbd;
    }
}

export function GridSelectModule_SelectedCellRange_make<$a, $b, $c, $d>(xStart: $a, yStart: $b, xEnd: $c, yEnd: $d): { xEnd: $c, xStart: $a, yEnd: $d, yStart: $b } {
    return {
        xEnd: xEnd,
        xStart: xStart,
        yEnd: yEnd,
        yStart: yStart,
    };
}

export function GridSelectModule_SelectedCellRange_create<$a, $b, $c, $d>(xStart: $a, yStart: $b, xEnd: $c, yEnd: $d): { xEnd: $c, xStart: $a, yEnd: $d, yStart: $b } {
    return {
        xEnd: xEnd,
        xStart: xStart,
        yEnd: yEnd,
        yStart: yStart,
    };
}

export function GridSelectModule_SelectedCellRange_singleton(cell: { x: int32, y: int32 }): { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 } {
    return {
        xEnd: cell.x,
        xStart: cell.x,
        yEnd: cell.y,
        yStart: cell.y,
    };
}

export function GridSelectModule_SelectedCellRange_toReducedSet(range: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>): FSharpSet<{ x: int32, y: int32 }> {
    if (range != null) {
        const range_1: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 } = value(range);
        return ofSeq([{
            x: range_1.xStart,
            y: range_1.yStart,
        }, {
            x: range_1.xEnd,
            y: range_1.yEnd,
        }], {
            Compare: compare,
        });
    }
    else {
        return empty<{ x: int32, y: int32 }>({
            Compare: compare,
        });
    }
}

export function GridSelectModule_SelectedCellRange_fromSet(selectedCells: FSharpSet<{ x: int32, y: int32 }>): Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }> {
    if (FSharpSet__get_IsEmpty(selectedCells)) {
        return undefined;
    }
    else {
        const min: { x: int32, y: int32 } = FSharpSet__get_MinimumElement(selectedCells);
        const max: { x: int32, y: int32 } = FSharpSet__get_MaximumElement(selectedCells);
        return {
            xEnd: max.x,
            xStart: min.x,
            yEnd: max.y,
            yStart: min.y,
        };
    }
}

export function GridSelectModule_SelectedCellRange_count(selectedCellRange: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>): int32 {
    if (selectedCellRange == null) {
        return 0;
    }
    else {
        const range: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 } = value(selectedCellRange);
        return (((range.xEnd - range.xStart) + 1) * ((range.yEnd - range.yStart) + 1)) | 0;
    }
}

export function GridSelectModule_SelectedCellRange_toString(range: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>): string {
    if (range == null) {
        return "None";
    }
    else {
        const range_1: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 } = value(range);
        const xStart: int32 = range_1.xStart | 0;
        const yStart: int32 = range_1.yStart | 0;
        const xEnd: int32 = range_1.xEnd | 0;
        const yEnd: int32 = range_1.yEnd | 0;
        return toText(printf("x: %d-%d, y: %d-%d"))(xStart)(xEnd)(yStart)(yEnd);
    }
}

export class GridSelect {
    "Origin@": Option<{ x: int32, y: int32 }>;
    "LastAppend@": Option<{ x: int32, y: int32 }>;
    constructor() {
        this["Origin@"] = undefined;
        this["LastAppend@"] = undefined;
    }
}

export function GridSelect_$reflection(): TypeInfo {
    return class_type("Swate.Components.GridSelect", undefined, GridSelect);
}

export function GridSelect_$ctor(): GridSelect {
    return new GridSelect();
}

export function GridSelect__get_Origin(__: GridSelect): Option<{ x: int32, y: int32 }> {
    return __["Origin@"];
}

export function GridSelect__set_Origin_75FBA3FA(__: GridSelect, v: Option<{ x: int32, y: int32 }>): void {
    __["Origin@"] = v;
}

export function GridSelect__get_LastAppend(__: GridSelect): Option<{ x: int32, y: int32 }> {
    return __["LastAppend@"];
}

export function GridSelect__set_LastAppend_75FBA3FA(__: GridSelect, v: Option<{ x: int32, y: int32 }>): void {
    __["LastAppend@"] = v;
}

function GridSelect__GetNextIndex_710D035D(this$: GridSelect, kbd: GridSelectModule_Kbd, jump: boolean, selectedCells: FSharpSet<{ x: int32, y: int32 }>, maxRow: int32, maxCol: int32, minRow: int32, minCol: int32): { x: int32, y: int32 } {
    let current: { x: int32, y: int32 };
    const matchValue: Option<{ x: int32, y: int32 }> = GridSelect__get_LastAppend(this$);
    const matchValue_1: Option<{ x: int32, y: int32 }> = GridSelect__get_Origin(this$);
    if (matchValue == null) {
        if (matchValue_1 == null) {
            const isIncrease: boolean = equals(kbd, "arrowDown") ? true : equals(kbd, "arrowRight");
            current = (isIncrease ? FSharpSet__get_MaximumElement(selectedCells) : FSharpSet__get_MinimumElement(selectedCells));
        }
        else {
            const origin: { x: int32, y: int32 } = value(matchValue_1);
            current = origin;
        }
    }
    else {
        const lastAppend: { x: int32, y: int32 } = value(matchValue);
        current = lastAppend;
    }
    if (jump) {
        switch (kbd) {
            case "arrowUp":
                return {
                    x: current.x,
                    y: minRow,
                };
            case "arrowLeft":
                return {
                    x: minCol,
                    y: current.y,
                };
            case "arrowRight":
                return {
                    x: maxCol,
                    y: current.y,
                };
            default:
                return {
                    x: current.x,
                    y: maxRow,
                };
        }
    }
    else {
        switch (kbd) {
            case "arrowUp":
                return {
                    x: current.x,
                    y: max_1(current.y - 1, minRow),
                };
            case "arrowLeft":
                return {
                    x: max_1(current.x - 1, minCol),
                    y: current.y,
                };
            case "arrowRight":
                return {
                    x: min_1(current.x + 1, maxCol),
                    y: current.y,
                };
            default:
                return {
                    x: current.x,
                    y: min_1(current.y + 1, maxRow),
                };
        }
    }
}

export function GridSelect__SelectBy_65E00AD4(this$: GridSelect, e: KeyboardEvent, selectedCells: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>, setter: ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void), maxRow: int32, maxCol: int32, minRow: Option<int32>, minCol: Option<int32>, onSelect: Option<((arg0: { x: int32, y: int32 }, arg1: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void)>): boolean {
    const kbd: Option<GridSelectModule_Kbd> = GridSelectModule_Kbd_tryFromKey_Z721C83C5(e.key);
    if (kbd == null) {
        return false;
    }
    else {
        const kbd_1: GridSelectModule_Kbd = value(kbd);
        e.preventDefault();
        const jump: boolean = e.ctrlKey ? true : e.metaKey;
        GridSelect__SelectBy_Z7448804D(this$, kbd_1, jump, e.shiftKey, selectedCells, setter, maxRow, maxCol, unwrap(minRow), unwrap(minCol), unwrap(onSelect));
        return true;
    }
}

export function GridSelect__SelectBy_Z7448804D(this$: GridSelect, kbd: GridSelectModule_Kbd, jump: boolean, isAppend: boolean, selectedCells: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>, setter: ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void), maxRow: int32, maxCol: int32, minRow: Option<int32>, minCol: Option<int32>, onSelect: Option<((arg0: { x: int32, y: int32 }, arg1: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void)>): void {
    const minRow_1: int32 = defaultArg<int32>(minRow, 0) | 0;
    const minCol_1: int32 = defaultArg<int32>(minCol, 0) | 0;
    if (selectedCells == null) {
        throw new Error("No selected cells");
    }
    if (!isAppend) {
        GridSelect__set_LastAppend_75FBA3FA(this$, undefined);
        GridSelect__set_Origin_75FBA3FA(this$, undefined);
    }
    const selectedCellsSet: FSharpSet<{ x: int32, y: int32 }> = GridSelectModule_SelectedCellRange_toReducedSet(selectedCells);
    const nextIndex: { x: int32, y: int32 } = GridSelect__GetNextIndex_710D035D(this$, kbd, jump, selectedCellsSet, maxRow, maxCol, minRow_1, minCol_1);
    GridSelect__SelectAt_53B3F404(this$, nextIndex, isAppend, selectedCells, setter, unwrap(onSelect));
}

export function GridSelect__SelectAt_53B3F404(this$: GridSelect, nextIndex: { x: int32, y: int32 }, isAppend: boolean, selectedCellRange: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>, setter: ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void), onSelect: Option<((arg0: { x: int32, y: int32 }, arg1: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void)>): void {
    let isEmpty: FSharpSet<{ x: int32, y: int32 }>;
    const matchValue: Option<{ x: int32, y: int32 }> = GridSelect__get_Origin(this$);
    if (isAppend) {
        if (matchValue == null) {
            GridSelect__set_Origin_75FBA3FA(this$, defaultArg(map<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, { x: int32, y: int32 }>((r: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }): { x: int32, y: int32 } => ({
                x: r.xStart,
                y: r.yStart,
            }), selectedCellRange), nextIndex));
            GridSelect__set_LastAppend_75FBA3FA(this$, nextIndex);
        }
        else {
            GridSelect__set_LastAppend_75FBA3FA(this$, nextIndex);
        }
    }
    else {
        GridSelect__set_Origin_75FBA3FA(this$, undefined);
        GridSelect__set_LastAppend_75FBA3FA(this$, undefined);
    }
    let newCellRange_1: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>;
    if (!isAppend) {
        newCellRange_1 = GridSelectModule_SelectedCellRange_fromSet(singleton<{ x: int32, y: int32 }>(nextIndex, {
            Compare: compare,
        }));
    }
    else {
        let selectedCells_1: FSharpSet<{ x: int32, y: int32 }>;
        const _arg: FSharpSet<{ x: int32, y: int32 }> = GridSelectModule_SelectedCellRange_toReducedSet(selectedCellRange);
        if ((isEmpty = _arg, FSharpSet__get_Count(isEmpty) === 0)) {
            const isEmpty_1: FSharpSet<{ x: int32, y: int32 }> = _arg;
            selectedCells_1 = singleton<{ x: int32, y: int32 }>(nextIndex, {
                Compare: compare,
            });
        }
        else {
            const x_2: FSharpSet<{ x: int32, y: int32 }> = _arg;
            selectedCells_1 = x_2;
        }
        const origin: { x: int32, y: int32 } = value(GridSelect__get_Origin(this$));
        const lastAppend: { x: int32, y: int32 } = defaultArg<{ x: int32, y: int32 }>(GridSelect__get_LastAppend(this$), value(GridSelect__get_Origin(this$)));
        const minRow: int32 = ((nextIndex.y <= origin.y) ? nextIndex.y : ((origin.y < lastAppend.y) ? origin.y : FSharpSet__get_MinimumElement(selectedCells_1).y)) | 0;
        const maxRow: int32 = ((nextIndex.y >= origin.y) ? nextIndex.y : ((origin.y > lastAppend.y) ? origin.y : FSharpSet__get_MaximumElement(selectedCells_1).y)) | 0;
        const minCol: int32 = ((nextIndex.x <= origin.x) ? nextIndex.x : ((origin.x < lastAppend.x) ? origin.x : FSharpSet__get_MinimumElement(selectedCells_1).x)) | 0;
        const maxCol: int32 = ((nextIndex.x >= origin.x) ? nextIndex.x : ((origin.x > lastAppend.x) ? origin.x : FSharpSet__get_MaximumElement(selectedCells_1).x)) | 0;
        const newCellRange: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 } = {
            xEnd: maxCol,
            xStart: minCol,
            yEnd: maxRow,
            yStart: minRow,
        };
        newCellRange_1 = newCellRange;
    }
    setter(newCellRange_1);
    iterate<((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))>((f: ((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))): void => {
        f(nextIndex)(newCellRange_1);
    }, toArray<((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))>(map(curry2, onSelect)));
}

export function GridSelect__Clear(this$: GridSelect): void {
    GridSelect__set_Origin_75FBA3FA(this$, undefined);
    GridSelect__set_LastAppend_75FBA3FA(this$, undefined);
}

export function Feliz_React__React_useGridSelect_Static_4CE412FE(rowCount: int32, columnCount: int32, minRow?: int32, minCol?: int32, onSelect?: ((arg0: { x: int32, y: int32 }, arg1: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void), seed?: { x: int32, y: int32 }[]): { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> } {
    const minRow_1: int32 = defaultArg<int32>(minRow, 0) | 0;
    const minCol_1: int32 = defaultArg<int32>(minCol, 0) | 0;
    const seed_2: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }> = map<FSharpSet<{ x: int32, y: int32 }>, { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>((seed_1: FSharpSet<{ x: int32, y: int32 }>): { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 } => {
        const xStart: int32 = FSharpSet__get_MinimumElement(seed_1).x | 0;
        const yStart: int32 = FSharpSet__get_MinimumElement(seed_1).y | 0;
        return {
            xEnd: FSharpSet__get_MaximumElement(seed_1).x,
            xStart: xStart,
            yEnd: FSharpSet__get_MaximumElement(seed_1).y,
            yStart: yStart,
        };
    }, map<{ x: int32, y: int32 }[], FSharpSet<{ x: int32, y: int32 }>>((elements: { x: int32, y: int32 }[]): FSharpSet<{ x: int32, y: int32 }> => ofSeq<{ x: int32, y: int32 }>(elements, {
        Compare: compare,
    }), seed));
    const patternInput: [Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>, ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void)] = reactApi.useState<Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>, Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>>(seed_2);
    const setSelectedCells: ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void) = patternInput[1];
    const selectedCells: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }> = patternInput[0];
    let select: IRefValue$1<GridSelect>;
    const initialValue: GridSelect = GridSelect_$ctor();
    select = (reactApi.useRef(initialValue));
    const selectBy = (e: KeyboardEvent): boolean => {
        if (selectedCells == null) {
            return false;
        }
        else {
            return GridSelect__SelectBy_65E00AD4(select.current, e, selectedCells, setSelectedCells, rowCount - 1, columnCount - 1, minRow_1, minCol_1, (newIndex: { x: int32, y: int32 }, newCellRange: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>): void => {
                iterate<((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))>((onSelect_1: ((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))): void => {
                    onSelect_1(newIndex)(newCellRange);
                }, toArray<((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))>(map(curry2, onSelect)));
            });
        }
    };
    const selectAt = (tupledArg: [{ x: int32, y: int32 }, boolean]): void => {
        const newIndex_1: { x: int32, y: int32 } = tupledArg[0];
        const isAppend: boolean = tupledArg[1];
        GridSelect__SelectAt_53B3F404(select.current, newIndex_1, isAppend, selectedCells, setSelectedCells, (newIndex_2: { x: int32, y: int32 }, newCellRange_1: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>): void => {
            iterate<((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))>((onSelect_2: ((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))): void => {
                onSelect_2(newIndex_2)(newCellRange_1);
            }, toArray<((arg0: { x: int32, y: int32 }) => ((arg0: Option<{ xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }>) => void))>(map(curry2, onSelect)));
        });
    };
    const contains = (cell: { x: int32, y: int32 }): boolean => {
        if ((((selectedCells != null) && (cell.x <= value(selectedCells).xEnd)) && (cell.x >= value(selectedCells).xStart)) && (cell.y <= value(selectedCells).yEnd)) {
            return cell.y >= value(selectedCells).yStart;
        }
        else {
            return false;
        }
    };
    const SelectOrigin: Option<{ x: int32, y: int32 }> = GridSelect__get_Origin(select.current);
    const lastAppend: Option<{ x: int32, y: int32 }> = GridSelect__get_LastAppend(select.current);
    const selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> = GridSelectModule_SelectedCellRange_toReducedSet(selectedCells);
    return {
        SelectOrigin: unwrap(SelectOrigin),
        clear: (): void => {
            GridSelect__Clear(select.current);
            setSelectedCells(undefined);
        },
        contains: contains,
        count: GridSelectModule_SelectedCellRange_count(selectedCells),
        lastAppend: unwrap(lastAppend),
        selectAt: selectAt,
        selectBy: selectBy,
        selectedCells: unwrap(selectedCells),
        selectedCellsReducedSet: selectedCellsReducedSet,
    };
}

