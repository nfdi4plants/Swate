import { createElement } from "react";
import React from "react";
import { equals, createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { empty, map, singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { ofArray, singleton as singleton_1 } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { MainPlus, Main as Main_1, MainMetadata } from "../MainComponents/FooterTabs.js";
import { ActiveView, Model__get_ActiveTable, Model__get_Tables } from "../States/Spreadsheet.js";
import { rangeDouble } from "../../../fable_modules/fable-library.4.9.0/Range.js";
import { defaultOf } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { Main as Main_2 } from "../MainComponents/Navbar.js";
import { Main as Main_3 } from "./XlsxFileView.js";
import { Main as Main_4 } from "../MainComponents/NoTablesElement.js";
import { Main as Main_5 } from "../MainComponents/AddRows.js";

function spreadsheetSelectionFooter(model, dispatch) {
    let elems_2, elems_1, elems, children;
    return createElement("div", createObj(ofArray([["style", {
        position: "sticky",
        bottom: 0,
    }], (elems_2 = [createElement("div", createObj(singleton_1((elems_1 = [createElement("div", createObj(Helpers_combineClasses("tabs", ofArray([["style", {
        overflowY: "visible",
    }], ["className", "is-boxed"], (elems = [(children = toList(delay(() => append(singleton(createElement("li", createObj(Helpers_combineClasses("", singleton_1(["style", {
        width: 20 + "px",
    }]))))), delay(() => append(singleton(createElement(MainMetadata, {
        dispatch: dispatch,
        model: model,
    })), delay(() => append(map((index) => createElement(Main_1, {
        dispatch: dispatch,
        index: index,
        model: model,
        tables: Model__get_Tables(model.SpreadsheetModel),
    }), rangeDouble(0, 1, Model__get_Tables(model.SpreadsheetModel).TableCount - 1)), delay(() => {
        const matchValue = model.SpreadsheetModel.ArcFile;
        let matchResult;
        if (matchValue != null) {
            switch (matchValue.tag) {
                case 0:
                case 1: {
                    matchResult = 0;
                    break;
                }
                default:
                    matchResult = 1;
            }
        }
        else {
            matchResult = 1;
        }
        switch (matchResult) {
            case 0:
                return singleton(defaultOf());
            default:
                return singleton(createElement(MainPlus, {
                    dispatch: dispatch,
                }));
        }
    })))))))), createElement("ul", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])));
}

export function Main(mainInputProps) {
    let elems_1;
    const dispatch = mainInputProps.dispatch;
    const model = mainInputProps.model;
    const state = model.SpreadsheetModel;
    return createElement("div", createObj(ofArray([["id", "MainWindow"], ["style", {
        display: "flex",
        flexDirection: "column",
        width: 100 + "%",
        height: 100 + "%",
    }], (elems_1 = toList(delay(() => append(singleton(createElement(Main_2, {
        model: model,
        dispatch: dispatch,
    })), delay(() => {
        let elems;
        return append(singleton(createElement("div", createObj(ofArray([["id", "TableContainer"], ["style", {
            width: "inherit",
            height: "inherit",
            overflowX: "auto",
            display: "flex",
            flexDirection: "column",
        }], (elems = toList(delay(() => {
            let matchValue;
            return append((matchValue = state.ArcFile, (matchValue != null) ? ((matchValue.tag === 2) ? singleton(createElement(Main_3, {
                dispatch: dispatch,
                model: model,
            })) : ((matchValue.tag === 1) ? singleton(createElement(Main_3, {
                dispatch: dispatch,
                model: model,
            })) : ((matchValue.tag === 0) ? singleton(createElement(Main_3, {
                dispatch: dispatch,
                model: model,
            })) : singleton(createElement(Main_3, {
                dispatch: dispatch,
                model: model,
            }))))) : singleton(createElement(Main_4, {
                dispatch: dispatch,
            }))), delay(() => ((((Model__get_Tables(state).TableCount > 0) && (Model__get_ActiveTable(state).ColumnCount > 0)) && !equals(state.ActiveView, new ActiveView(1, []))) ? singleton(createElement(Main_5, {
                dispatch: dispatch,
            })) : empty())));
        })), ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))), delay(() => ((state.ArcFile != null) ? singleton(spreadsheetSelectionFooter(model, dispatch)) : empty())));
    })))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])));
}

//# sourceMappingURL=MainWindowView.js.map
