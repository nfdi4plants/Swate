import { createElement } from "react";
import React from "react";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { ofArray, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { map, collect, singleton as singleton_1, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { Model__get_ActiveTable } from "../States/Spreadsheet.js";
import { HeaderCell, BodyCell } from "./Cells.js";
import { rangeDouble } from "../../../fable_modules/fable-library.4.9.0/Range.js";
import { useFeliz_React__React_useState_Static_1505 } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { empty } from "../../../fable_modules/fable-library.4.9.0/Set.js";
import { createObj, comparePrimitives } from "../../../fable_modules/fable-library.4.9.0/Util.js";

export function cellPlaceholder(c_opt) {
    const tableCell = (children) => {
        const children_1 = singleton(createElement("div", {
            style: {
                minHeight: 30 + "px",
                minWidth: 100 + "px",
            },
            children: Interop_reactApi.Children.toArray(Array.from(children)),
        }));
        return createElement("td", {
            children: Interop_reactApi.Children.toArray(Array.from(children_1)),
        });
    };
    const children_3 = toList(delay(() => {
        if (c_opt == null) {
            return singleton_1(tableCell(singleton(createElement("span", {
                children: [""],
            }))));
        }
        else {
            const c = c_opt;
            return singleton_1(tableCell(singleton(createElement("span", {
                children: [c.GetContent()[0]],
            }))));
        }
    }));
    return createElement("td", {
        children: Interop_reactApi.Children.toArray(Array.from(children_3)),
    });
}

function bodyRow(rowIndex, state, setState, model, dispatch) {
    const table = Model__get_ActiveTable(model.SpreadsheetModel);
    const children = toList(delay(() => collect((columnIndex) => singleton_1(createElement(BodyCell, {
        index: [columnIndex, rowIndex],
        state_extend: state,
        setState_extend: setState,
        model: model,
        dispatch: dispatch,
    })), rangeDouble(0, 1, table.ColumnCount - 1))));
    return createElement("tr", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    });
}

function bodyRows(state, setState, model, dispatch) {
    const children = toList(delay(() => map((rowInd) => bodyRow(rowInd, state, setState, model, dispatch), rangeDouble(0, 1, Model__get_ActiveTable(model.SpreadsheetModel).RowCount - 1))));
    return createElement("tbody", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    });
}

function headerRow(state, setState, model, dispatch) {
    const table = Model__get_ActiveTable(model.SpreadsheetModel);
    const children = toList(delay(() => map((columnIndex) => createElement(HeaderCell, {
        columnIndex: columnIndex,
        state_extend: state,
        setState_extend: setState,
        model: model,
        dispatch: dispatch,
    }), rangeDouble(0, 1, table.ColumnCount - 1))));
    return createElement("tr", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    });
}

export function Main(mainInputProps) {
    let elems_1, elems, children;
    const dispatch = mainInputProps.dispatch;
    const model = mainInputProps.model;
    const patternInput = useFeliz_React__React_useState_Static_1505(empty({
        Compare: comparePrimitives,
    }));
    const state = patternInput[0];
    const setState = patternInput[1];
    return createElement("div", createObj(ofArray([["style", {
        border: (((1 + "px ") + "solid") + " ") + "grey",
        width: "min-content",
        marginRight: 10 + "vw",
    }], (elems_1 = [createElement("table", createObj(ofArray([["className", "fixed_headers"], (elems = [(children = singleton(headerRow(state, setState, model, dispatch)), createElement("thead", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    })), bodyRows(state, setState, model, dispatch)], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])));
}

//# sourceMappingURL=SpreadsheetView.js.map
