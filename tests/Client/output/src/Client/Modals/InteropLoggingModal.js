import { ofArray, map, singleton, empty } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Msg, DevMsg } from "../Messages.js";
import { createElement } from "react";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { LogItem_get_toTableRow } from "../Model.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export function interopLoggingModal(model, dispatch, rmv) {
    let elems_4, elems_3, elms, elems, children, elms_1;
    const closeMsg = (e) => {
        rmv(e);
        dispatch(new Msg(3, [new DevMsg(4, [empty()])]));
    };
    return createElement("div", createObj(Helpers_combineClasses("modal", ofArray([["className", "is-active"], (elems_4 = [createElement("div", createObj(Helpers_combineClasses("modal-background", singleton(["onClick", closeMsg])))), createElement("div", createObj(Helpers_combineClasses("notification", ofArray([["style", {
        width: 80 + "%",
        maxHeight: 80 + "%",
    }], (elems_3 = [createElement("button", createObj(Helpers_combineClasses("delete", singleton(["onClick", closeMsg])))), (elms = singleton(createElement("table", createObj(Helpers_combineClasses("table", ofArray([["className", "is-fullwidth"], (elems = [(children = map(LogItem_get_toTableRow(), model.DisplayLogList), createElement("tbody", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), (elms_1 = singleton(createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-warning"], ["style", {
        float: "right",
    }], ["onClick", closeMsg], ["children", "Continue"]]))))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))));
}

//# sourceMappingURL=InteropLoggingModal.js.map
