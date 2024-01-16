import { createElement } from "react";
import React from "react";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { QuickAccessButton_create_Z9F8EBC5, QuickAccessButton__toReactElement } from "../SharedComponents/QuickAccessButton.js";
import { empty, ofArray, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { Model__NextPositionIsValid_Z524259A4 } from "../States/LocalHistory.js";
import { Msg } from "../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../Messages.js";
import { value as value_56 } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { renderModal } from "../Modals/Controller.js";
import { Main as Main_1 } from "../Modals/ResetTable.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { join } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { append, singleton as singleton_1, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { send } from "../ARCitect/ARCitect.js";
import { Msg as Msg_2 } from "../States/ARCitect.js";
import { defaultOf } from "../../../fable_modules/fable-library.4.9.0/Util.js";

export function quickAccessButtonListStart(state, dispatch) {
    let elems_2, elms, elms_1;
    return createElement("div", createObj(ofArray([["style", {
        display: "flex",
        flexDirection: "row",
    }], (elems_2 = [QuickAccessButton__toReactElement(QuickAccessButton_create_Z9F8EBC5("Back", singleton((elms = singleton(createElement("i", {
        className: "fa-solid fa-rotate-left",
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))), (_arg) => {
        const newPosition = (state.HistoryCurrentPosition + 1) | 0;
        if (Model__NextPositionIsValid_Z524259A4(state, newPosition)) {
            dispatch(new Msg_1(15, [new Msg(7, [newPosition])]));
        }
    }, Model__NextPositionIsValid_Z524259A4(state, state.HistoryCurrentPosition + 1))), QuickAccessButton__toReactElement(QuickAccessButton_create_Z9F8EBC5("Forward", singleton((elms_1 = singleton(createElement("i", {
        className: "fa-solid fa-rotate-right",
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    }))), (_arg_1) => {
        const newPosition_1 = (state.HistoryCurrentPosition - 1) | 0;
        if (Model__NextPositionIsValid_Z524259A4(state, newPosition_1)) {
            dispatch(new Msg_1(15, [new Msg(7, [newPosition_1])]));
        }
    }, Model__NextPositionIsValid_Z524259A4(state, state.HistoryCurrentPosition - 1)))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])));
}

export function quickAccessButtonListEnd(model, dispatch) {
    let elems_2, elms, elms_1;
    return createElement("div", createObj(ofArray([["style", {
        display: "flex",
        flexDirection: "row",
    }], (elems_2 = [QuickAccessButton__toReactElement(QuickAccessButton_create_Z9F8EBC5("Save as xlsx", singleton((elms = singleton(createElement("i", {
        className: "fa-solid fa-floppy-disk",
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))), (_arg) => {
        dispatch(new Msg_1(15, [new Msg(32, [value_56(model.SpreadsheetModel.ArcFile)])]));
    })), QuickAccessButton__toReactElement(QuickAccessButton_create_Z9F8EBC5("Reset", singleton((elms_1 = singleton(createElement("i", {
        className: "fa-sharp fa-solid fa-trash",
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    }))), (_arg_1) => {
        renderModal("ResetTableWarning", (rmv) => Main_1(dispatch, rmv));
    }, void 0, ofArray([["className", "is-danger"], ["className", "is-inverted"], ["className", "is-outlined"]])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])));
}

export function Main(mainInputProps) {
    let elems_6, elems_5;
    const dispatch = mainInputProps.dispatch;
    const model = mainInputProps.model;
    return createElement("nav", createObj(Helpers_combineClasses("navbar", ofArray([["className", "myNavbarSticky"], ["id", "swate-mainNavbar"], ["role", join(" ", ["navigation"])], ["aria-label", "main navigation"], ["style", {
        flexWrap: "wrap",
        alignItems: "stretch",
        display: "flex",
        minHeight: 3.25 + "rem",
    }], (elems_6 = [createElement("div", createObj(Helpers_combineClasses("navbar-brand", empty()))), createElement("div", createObj(ofArray([["style", {
        display: "flex",
        flexGrow: 1,
        flexShrink: 0,
        alignItems: "stretch",
    }], ["aria-label", "menu"], (elems_5 = toList(delay(() => {
        let elems_2, elems_1, elms, elems_3;
        const matchValue = model.PersistentStorageState.Host;
        return (matchValue != null) ? ((matchValue.tag === 2) ? singleton_1(createElement("div", createObj(Helpers_combineClasses("navbar-start", ofArray([["style", {
            display: "flex",
            alignItems: "stretch",
            justifyContent: "flex-start",
            marginRight: "auto",
        }], (elems_2 = [createElement("div", createObj(ofArray([["style", {
            display: "flex",
            flexDirection: "row",
        }], (elems_1 = [QuickAccessButton__toReactElement(QuickAccessButton_create_Z9F8EBC5("Return to ARCitect", singleton((elms = singleton(createElement("i", {
            className: "fa-solid fa-circle-left",
        })), createElement("span", {
            className: "icon",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        }))), (_arg) => {
            send(new Msg_2(4, []));
        })), QuickAccessButton__toReactElement(QuickAccessButton_create_Z9F8EBC5("Alpha State", singleton(createElement("span", {
            children: ["ALPHA STATE"],
        })), (e) => {
        }, false))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))))) : append(singleton_1(createElement("div", createObj(Helpers_combineClasses("navbar-start", ofArray([["style", {
            display: "flex",
            alignItems: "stretch",
            justifyContent: "flex-start",
            marginRight: "auto",
        }], (elems_3 = [quickAccessButtonListStart(model.History, dispatch)], ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])]))))), delay(() => {
            let elems_4;
            return singleton_1(createElement("div", createObj(Helpers_combineClasses("navbar-end", ofArray([["style", {
                display: "flex",
                alignItems: "stretch",
                justifyContent: "flex-end",
                marginLeft: "auto",
            }], (elems_4 = [quickAccessButtonListEnd(model, dispatch)], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])])))));
        }))) : singleton_1(defaultOf());
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_6))])]))));
}

//# sourceMappingURL=Navbar.js.map
