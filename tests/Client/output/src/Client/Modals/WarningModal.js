import { createElement } from "react";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { ofArray, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export function warningModal(warning, model, dispatch, rmv) {
    let elems_2, elems_1, elms;
    const closeMsg = rmv;
    return createElement("div", createObj(Helpers_combineClasses("modal", ofArray([["className", "is-active"], (elems_2 = [createElement("div", createObj(Helpers_combineClasses("modal-background", singleton(["onClick", closeMsg])))), createElement("div", createObj(Helpers_combineClasses("notification", ofArray([["style", {
        width: 80 + "%",
        maxHeight: 80 + "%",
        overflowX: "auto",
    }], (elems_1 = [createElement("button", createObj(Helpers_combineClasses("delete", singleton(["onClick", closeMsg])))), createElement("div", createObj(Helpers_combineClasses("field", singleton(["children", warning.ModalMessage])))), (elms = singleton(createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-warning"], ["style", {
        float: "right",
    }], ["onClick", (e) => {
        closeMsg(e);
        dispatch(warning.NextMsg);
    }], ["children", "Continue"]]))))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))));
}

export function warningModalSimple(warning, rmv) {
    let elems_2, elems_1, elms;
    const closeMsg = rmv;
    return createElement("div", createObj(Helpers_combineClasses("modal", ofArray([["className", "is-active"], (elems_2 = [createElement("div", createObj(Helpers_combineClasses("modal-background", singleton(["onClick", rmv])))), createElement("div", createObj(Helpers_combineClasses("notification", ofArray([["style", {
        maxWidth: 80 + "%",
        maxHeight: 80 + "%",
        overflowX: "auto",
    }], (elems_1 = [createElement("button", createObj(Helpers_combineClasses("delete", singleton(["onClick", closeMsg])))), createElement("div", {
        className: "field",
        children: warning,
    }), (elms = singleton(createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-warning"], ["style", {
        float: "right",
    }], ["onClick", (e) => {
        closeMsg(e);
    }], ["children", "Continue"]]))))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))));
}

//# sourceMappingURL=WarningModal.js.map
