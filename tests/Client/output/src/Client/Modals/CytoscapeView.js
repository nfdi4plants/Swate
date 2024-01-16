import { createElement } from "react";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { ofArray, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export function view(rmv) {
    let elems_2, elems_1, elms;
    return createElement("div", createObj(Helpers_combineClasses("modal", ofArray([["className", "is-active"], (elems_2 = [createElement("div", createObj(Helpers_combineClasses("modal-background", singleton(["onClick", rmv])))), createElement("div", createObj(Helpers_combineClasses("modal-content", ofArray([["style", {
        maxWidth: 80 + "%",
        maxHeight: 80 + "%",
        overflowX: "auto",
    }], (elems_1 = [(elms = singleton(createElement("div", {
        id: "cy",
    })), createElement("div", {
        className: "box",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))));
}

//# sourceMappingURL=CytoscapeView.js.map
