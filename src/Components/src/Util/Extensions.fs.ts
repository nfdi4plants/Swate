import { getItemFromDict } from "../fable_modules/fable-library-ts.4.24.0/MapUtil.js";
import { CompositeCell_$union } from "../fable_modules/ARCtrl.Core.2.5.1/Table/CompositeCell.fs.js";
import { ArcTable } from "../fable_modules/ARCtrl.Core.2.5.1/Table/ArcTable.fs.js";
import { int32 } from "../fable_modules/fable-library-ts.4.24.0/Int32.js";
import { iterate } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { disposeSafe, getEnumerator } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { IReactProperty } from "../fable_modules/Feliz.2.9.0/Types.fs.js";
import { printf, toText } from "../fable_modules/fable-library-ts.4.24.0/String.js";

export function ARCtrl_ArcTable__ArcTable_ClearCell_Z3227AE51(this$: ArcTable, cellIndex: [int32, int32]): void {
    const c: CompositeCell_$union = getItemFromDict(this$.Values, cellIndex);
    this$.Values.set(cellIndex, c.GetEmptyCell());
}

export function ARCtrl_ArcTable__ArcTable_ClearSelectedCells_49F0F46F(this$: ArcTable, selectHandle: SelectHandle): void {
    if (selectHandle.getCount() <= 100) {
        iterate<{ x: int32, y: int32 }>((i: { x: int32, y: int32 }): void => {
            const c_2: CompositeCell_$union = getItemFromDict(this$.Values, [i.x - 1, i.y - 1] as [int32, int32]);
            this$.Values.set([i.x - 1, i.y - 1] as [int32, int32], c_2.GetEmptyCell());
        }, selectHandle.getSelectedCells());
    }
    else {
        let enumerator: any = getEnumerator(this$.Values.keys());
        try {
            while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                const forLoopVar: [int32, int32] = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
                const y: int32 = forLoopVar[1] | 0;
                const x: int32 = forLoopVar[0] | 0;
                if (selectHandle.contains({
                    x: x + 1,
                    y: y + 1,
                })) {
                    const c_4: CompositeCell_$union = getItemFromDict(this$.Values, [x, y] as [int32, int32]);
                    this$.Values.set([x, y] as [int32, int32], c_4.GetEmptyCell());
                }
            }
        }
        finally {
            disposeSafe(enumerator);
        }
    }
}

export function Feliz_prop__prop_testid_Static_Z721C83C5(value: string): IReactProperty {
    return ["data-testid", value] as [string, any];
}

export function Feliz_prop__prop_dataRow_Static_Z524259A4(value: int32): IReactProperty {
    return ["data-row", value] as [string, any];
}

export function Feliz_prop__prop_dataColumn_Static_Z524259A4(value: int32): IReactProperty {
    return ["data-column", value] as [string, any];
}

export function kbdEventCode_key(key: string): string {
    const arg: string = key.toLocaleUpperCase();
    return toText(printf("Key%s"))(arg);
}

