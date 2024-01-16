import { toString, Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, string_type, bool_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { compareArrays, createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { map, collect, empty, singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { createElement } from "react";
import React from "react";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { empty as empty_2, cons, ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { FSharpSet__Contains, empty as empty_1, ofSeq, ofList, FSharpSet__get_MinimumElement } from "../../../fable_modules/fable-library.4.9.0/Set.js";
import { max, min } from "../../../fable_modules/fable-library.4.9.0/Double.js";
import { rangeDouble } from "../../../fable_modules/fable-library.4.9.0/Range.js";
import { ActiveView__get_TableIndex, Model__get_ActiveTable, Msg } from "../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../Messages.js";
import { useFeliz_React__React_useState_Static_1505 } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { CompositeHeader } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeHeader.fs.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { value as value_22 } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { onContextMenu } from "./ContextMenu.js";
import { ARCtrl_ISA_CompositeCell__CompositeCell_ToTerm, ARCtrl_ISA_CompositeCell__CompositeCell_UpdateMainField_Z721C83C5 } from "../../Shared/ARCtrl.Helper.js";

class CellState extends Record {
    constructor(Active, Value) {
        super();
        this.Active = Active;
        this.Value = Value;
    }
}

function CellState_$reflection() {
    return record_type("Spreadsheet.Cells.CellState", [], CellState, () => [["Active", bool_type], ["Value", string_type]]);
}

function CellState_init() {
    return new CellState(false, "");
}

function CellState_init_Z721C83C5(v) {
    return new CellState(false, v);
}

function cellStyle(specificStyle) {
    return ["style", createObj(toList(delay(() => append(singleton(["minWidth", 100]), delay(() => append(singleton(["height", 22]), delay(() => append(singleton(["border", ((((1 + "px") + " ") + "solid") + " ") + "darkgrey"]), delay(() => specificStyle)))))))))];
}

function cellInnerContainerStyle(specificStyle) {
    return ["style", createObj(toList(delay(() => append(singleton(["display", "flex"]), delay(() => append(singleton(["justifyContent", "space-between"]), delay(() => append(singleton(["height", 100 + "%"]), delay(() => append(singleton(["minHeight", 35]), delay(() => append(singleton(["width", 100 + "%"]), delay(() => append(singleton(["alignItems", "center"]), delay(() => specificStyle)))))))))))))))];
}

function cellInputElement(isHeader, updateMainStateTable, setState_cell, state_cell, cell_value) {
    return createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", ofArray([["autoFocus", true], ["style", createObj(toList(delay(() => append(isHeader ? singleton(["fontWeight", "bold"]) : empty(), delay(() => append(singleton(["width", 100 + "%"]), delay(() => append(singleton(["height", "unset"]), delay(() => append(singleton(["borderRadius", 0]), delay(() => append(singleton(["border", (((0 + "px ") + "none") + " ") + ""]), delay(() => append(singleton(["backgroundColor", "transparent"]), delay(() => append(singleton(["margin", 0]), delay(() => singleton(["padding", ((0.5 + "em") + " ") + (0.75 + "em")]))))))))))))))))))], ["onBlur", (_arg) => {
        updateMainStateTable();
    }], ["onKeyDown", (e) => {
        const matchValue = e.which;
        switch (matchValue) {
            case 13: {
                updateMainStateTable();
                break;
            }
            case 27: {
                setState_cell(new CellState(false, cell_value));
                break;
            }
            default:
                0;
        }
    }], ["onChange", (ev) => {
        setState_cell(new CellState(state_cell.Active, ev.target.value));
    }], ["defaultValue", cell_value]])))));
}

function basicValueDisplayCell(v) {
    return createElement("span", {
        style: {
            flexGrow: 1,
            padding: ((0.5 + "em") + " ") + (0.75 + "em"),
        },
        children: v,
    });
}

function EventPresets_onClickSelect(index, state_cell, selectedCells, dispatch, e) {
    let tupledArg;
    if (!state_cell.Active) {
        if (e.ctrlKey) {
            const source = FSharpSet__get_MinimumElement(selectedCells);
            const target = index;
            dispatch(new Msg_1(15, [new Msg(3, [(tupledArg = [min(source[0], target[0]), max(source[0], target[0]), min(source[1], target[1]), max(source[1], target[1])], ofList(toList(delay(() => collect((c) => map((r) => [c, r], rangeDouble(tupledArg[2], 1, tupledArg[3])), rangeDouble(tupledArg[0], 1, tupledArg[1])))), {
                Compare: compareArrays,
            }))])]));
        }
        else {
            dispatch(new Msg_1(15, [new Msg(3, [selectedCells.Equals(ofSeq([index], {
                Compare: compareArrays,
            })) ? empty_1({
                Compare: compareArrays,
            }) : ofSeq([index], {
                Compare: compareArrays,
            })])]));
        }
    }
}

export function HeaderCell(headerCellInputProps) {
    let elems_1, elems;
    const dispatch = headerCellInputProps.dispatch;
    const model = headerCellInputProps.model;
    const setState_extend = headerCellInputProps.setState_extend;
    const state_extend = headerCellInputProps.state_extend;
    const columnIndex = headerCellInputProps.columnIndex;
    const state = model.SpreadsheetModel;
    const cellValue = toString(Model__get_ActiveTable(state).Headers[columnIndex]);
    const patternInput = useFeliz_React__React_useState_Static_1505(CellState_init_Z721C83C5(cellValue));
    const state_cell = patternInput[0];
    const setState_cell = patternInput[1];
    return createElement("th", createObj(ofArray([["key", `Header_${ActiveView__get_TableIndex(state.ActiveView)}-${columnIndex}`], cellStyle(empty_2()), (elems_1 = [createElement("div", createObj(ofArray([cellInnerContainerStyle(empty_2()), ["onDoubleClick", (e) => {
        e.preventDefault();
        e.stopPropagation();
        dispatch(new Msg_1(15, [new Msg(3, [empty_1({
            Compare: compareArrays,
        })])]));
        if (!state_cell.Active) {
            setState_cell(new CellState(true, state_cell.Value));
        }
    }], (elems = toList(delay(() => (state_cell.Active ? singleton(cellInputElement(true, () => {
        if (state_cell.Value !== cellValue) {
            dispatch(new Msg_1(15, [new Msg(1, [columnIndex, CompositeHeader.OfHeaderString(state_cell.Value)])]));
        }
        setState_cell(new CellState(false, state_cell.Value));
    }, setState_cell, state_cell, cellValue)) : singleton(basicValueDisplayCell(cellValue))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])));
}

export function BodyCell(bodyCellInputProps) {
    let objectArg, tupledArg, elems_1, elems;
    const dispatch = bodyCellInputProps.dispatch;
    const model = bodyCellInputProps.model;
    const setState_extend = bodyCellInputProps.setState_extend;
    const state_extend = bodyCellInputProps.state_extend;
    const index = bodyCellInputProps.index;
    const state = model.SpreadsheetModel;
    const cell = value_22((objectArg = Model__get_ActiveTable(state), (tupledArg = index, objectArg.TryGetCellAt(tupledArg[0], tupledArg[1]))));
    const cellValue = cell.GetContent()[0];
    const patternInput = useFeliz_React__React_useState_Static_1505(CellState_init_Z721C83C5(cellValue));
    const state_cell = patternInput[0];
    const setState_cell = patternInput[1];
    const isSelected = FSharpSet__Contains(state.SelectedCells, index);
    return createElement("td", createObj(ofArray([["key", `Cell_${ActiveView__get_TableIndex(state.ActiveView)}-${index[0]}-${index[1]}`], cellStyle(toList(delay(() => (isSelected ? singleton(["backgroundColor", "#d2f3ed"]) : empty())))), ["onContextMenu", (e) => {
        onContextMenu(index, model, dispatch, e);
    }], (elems_1 = [createElement("div", createObj(ofArray([cellInnerContainerStyle(empty_2()), ["onDoubleClick", (e_1) => {
        e_1.preventDefault();
        e_1.stopPropagation();
        dispatch(new Msg_1(15, [new Msg(3, [empty_1({
            Compare: compareArrays,
        })])]));
        if (!state_cell.Active) {
            setState_cell(new CellState(true, state_cell.Value));
        }
    }], ["onClick", (e_2) => {
        EventPresets_onClickSelect(index, state_cell, state.SelectedCells, dispatch, e_2);
    }], (elems = toList(delay(() => (state_cell.Active ? singleton(cellInputElement(false, () => {
        if (state_cell.Value !== cellValue) {
            dispatch(new Msg_1(15, [new Msg(0, [index, ARCtrl_ISA_CompositeCell__CompositeCell_UpdateMainField_Z721C83C5(cell, state_cell.Value)])]));
        }
        setState_cell(new CellState(false, state_cell.Value));
    }, setState_cell, state_cell, cellValue)) : singleton(basicValueDisplayCell(cell.isUnitized ? ((cellValue === "") ? "" : ((cellValue + " ") + ARCtrl_ISA_CompositeCell__CompositeCell_ToTerm(cell).NameText)) : cellValue))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])));
}

//# sourceMappingURL=Cells.js.map
