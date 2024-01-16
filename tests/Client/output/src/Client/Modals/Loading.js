import { createElement } from "react";
import { join } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { ofArray, empty } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export const loadingComponent = createElement("i", {
    className: join(" ", ["fa-solid", "fa-spinner", "fa-spin-pulse", "fa-xl"]),
});

export const loadingModal = createElement("div", createObj(Helpers_combineClasses("modal", ofArray([["className", "is-active"], (() => {
    let elems_1;
    const elems_2 = [createElement("div", createObj(Helpers_combineClasses("modal-background", empty()))), createElement("div", createObj(Helpers_combineClasses("modal-content", ofArray([["style", {
        width: "auto",
    }], (elems_1 = [createElement("div", {
        className: "box",
        children: Interop_reactApi.Children.toArray([loadingComponent]),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))))];
    return ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))];
})()]))));

//# sourceMappingURL=Loading.js.map
