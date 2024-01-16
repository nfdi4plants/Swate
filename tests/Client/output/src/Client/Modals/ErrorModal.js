import { createElement } from "react";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { ofArray, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { System_Exception__Exception_GetPropagatedError } from "../Messages.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

/**
 * This modal is used to display errors from for example api communication
 */
export function errorModal(error, rmv) {
    let elems_1, elems;
    const closeMsg = rmv;
    return createElement("div", createObj(Helpers_combineClasses("modal", ofArray([["className", "is-active"], (elems_1 = [createElement("div", createObj(Helpers_combineClasses("modal-background", singleton(["onClick", closeMsg])))), createElement("div", createObj(Helpers_combineClasses("notification", ofArray([["className", "is-danger"], ["style", {
        width: 90 + "%",
        maxHeight: 80 + "%",
        overflow: "auto",
    }], (elems = [createElement("button", createObj(Helpers_combineClasses("delete", singleton(["onClick", closeMsg])))), createElement("span", {
        children: [System_Exception__Exception_GetPropagatedError(error)],
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

//# sourceMappingURL=ErrorModal.js.map
