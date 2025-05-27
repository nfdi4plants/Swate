import { class_type, TypeInfo } from "../fable_modules/fable-library-ts.4.24.0/Reflection.js";
import { ReactElement, createElement } from "react";
import { defaultArg } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { ARCtrl_ArcTable__ArcTable_ClearCell_Z3227AE51, ARCtrl_ArcTable__ArcTable_ClearSelectedCells_49F0F46F } from "../Util/Extensions.fs.js";
import { int32 } from "../fable_modules/fable-library-ts.4.24.0/Int32.js";
import { ArcTable } from "../fable_modules/ARCtrl.Core.2.5.1/Table/ArcTable.fs.js";
import { getItemFromDict } from "../fable_modules/fable-library-ts.4.24.0/MapUtil.js";
import { CompositeCell_$union } from "../fable_modules/ARCtrl.Core.2.5.1/Table/CompositeCell.fs.js";
import { empty, FSharpList, ofArray } from "../fable_modules/fable-library-ts.4.24.0/List.js";
import { CompositeHeader_$union } from "../fable_modules/ARCtrl.Core.2.5.1/Table/CompositeHeader.fs.js";

export class ATCMC {
    constructor() {
    }
}

export function ATCMC_$reflection(): TypeInfo {
    return class_type("Swate.Components.ATCMC", undefined, ATCMC);
}

export function ATCMC_Icon_Z721C83C5(className: string): ReactElement {
    return createElement<any>("i", {
        className: className,
    });
}

export function ATCMC_KbdHint_27AED5E3(text: string, label?: string): { element: ReactElement, label: string } {
    const label_1: string = defaultArg<string>(label, text);
    return {
        element: createElement<any>("kbd", {
            className: "swt:ml-auto swt:kbd swt:kbd-sm",
            children: text,
        }),
        label: label_1,
    };
}

export class AnnotationTableContextMenuUtil {
    constructor() {
    }
}

export function AnnotationTableContextMenuUtil_$reflection(): TypeInfo {
    return class_type("Swate.Components.AnnotationTableContextMenuUtil", undefined, AnnotationTableContextMenuUtil);
}

export function AnnotationTableContextMenuUtil_clear_22CD38E6(tableIndex: { x: int32, y: int32 }, cellIndex: [int32, int32], table: ArcTable, selectHandle: SelectHandle): ArcTable {
    if (selectHandle.contains(tableIndex)) {
        ARCtrl_ArcTable__ArcTable_ClearSelectedCells_49F0F46F(table, selectHandle);
    }
    else {
        ARCtrl_ArcTable__ArcTable_ClearCell_Z3227AE51(table, cellIndex);
    }
    return table.Copy();
}

export function AnnotationTableContextMenu_CompositeCellContent_469BA83C(index: { x: int32, y: int32 }, table: ArcTable, setTable: ((arg0: ArcTable) => void), selectHandle: SelectHandle): FSharpList<ContextMenuItem> {
    const cellIndex = [index.x - 1, index.y - 1] as [int32, int32];
    const cell: CompositeCell_$union = getItemFromDict(table.Values, cellIndex);
    return ofArray([{
        text: createElement<any>("div", {
            children: ["Details"],
        }),
        icon: ATCMC_Icon_Z721C83C5("fa-solid fa-magnifying-glass"),
        kbdbutton: ATCMC_KbdHint_27AED5E3("D"),
    }, {
        text: createElement<any>("div", {
            children: ["Fill Column"],
        }),
        icon: ATCMC_Icon_Z721C83C5("fa-solid fa-pen"),
        kbdbutton: ATCMC_KbdHint_27AED5E3("F"),
    }, {
        text: createElement<any>("div", {
            children: ["Edit"],
        }),
        icon: ATCMC_Icon_Z721C83C5("fa-solid fa-pen-to-square"),
        kbdbutton: ATCMC_KbdHint_27AED5E3("E"),
    }, {
        text: createElement<any>("div", {
            children: ["Clear"],
        }),
        icon: ATCMC_Icon_Z721C83C5("fa-solid fa-eraser"),
        kbdbutton: ATCMC_KbdHint_27AED5E3("Del"),
        onClick: (c: { buttonEvent: MouseEvent, spawnData: any }): void => {
            setTable(AnnotationTableContextMenuUtil_clear_22CD38E6(c.spawnData, cellIndex, table, selectHandle));
        },
    }, {
        isDivider: true,
    }, {
        text: createElement<any>("div", {
            children: ["Copy"],
        }),
        icon: ATCMC_Icon_Z721C83C5("fa-solid fa-copy"),
        kbdbutton: ATCMC_KbdHint_27AED5E3("C"),
    }, {
        text: createElement<any>("div", {
            children: ["Cut"],
        }),
        icon: ATCMC_Icon_Z721C83C5("fa-solid fa-scissors"),
        kbdbutton: ATCMC_KbdHint_27AED5E3("X"),
    }, {
        text: createElement<any>("div", {
            children: ["Paste"],
        }),
        icon: ATCMC_Icon_Z721C83C5("fa-solid fa-paste"),
        kbdbutton: ATCMC_KbdHint_27AED5E3("V"),
    }, {
        isDivider: true,
    }, {
        text: createElement<any>("div", {
            children: ["Delete Row"],
        }),
        icon: ATCMC_Icon_Z721C83C5("fa-solid fa-delete-left"),
        kbdbutton: ATCMC_KbdHint_27AED5E3("DelR"),
    }, {
        text: createElement<any>("div", {
            children: ["Delete Column"],
        }),
        icon: ATCMC_Icon_Z721C83C5("fa-solid fa-delete-left fa-rotate-270"),
        kbdbutton: ATCMC_KbdHint_27AED5E3("DelC"),
    }, {
        text: createElement<any>("div", {
            children: ["Move Column"],
        }),
        icon: ATCMC_Icon_Z721C83C5("fa-solid fa-arrow-right-arrow-left"),
        kbdbutton: ATCMC_KbdHint_27AED5E3("MC"),
    }]);
}

export function AnnotationTableContextMenu_CompositeHeaderContent_Z22A340EA<$b>(index: int32, table: ArcTable, setTable: ((arg0: ArcTable) => void)): FSharpList<$b> {
    const header: CompositeHeader_$union = table.Headers[index];
    return empty<$b>();
}

export function AnnotationTableContextMenu_IndexColumnContent_Z22A340EA<$a>(index: int32, table: ArcTable, setTable: ((arg0: ArcTable) => void)): FSharpList<$a> {
    return empty<$a>();
}

