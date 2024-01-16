import { createElement } from "react";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { ofArray, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { Msg } from "../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../Messages.js";

export function Main(dispatch, rmv) {
    let elems_4, elms_3, elms, elms_1, elms_2;
    return createElement("div", createObj(Helpers_combineClasses("modal", ofArray([["className", "is-active"], (elems_4 = [createElement("div", createObj(Helpers_combineClasses("modal-background", singleton(["onClick", rmv])))), (elms_3 = ofArray([(elms = ofArray([createElement("p", {
        className: "modal-card-title",
        children: "Attention!",
    }), createElement("button", createObj(Helpers_combineClasses("delete", singleton(["onClick", rmv]))))]), createElement("header", {
        className: "modal-card-head",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), (elms_1 = ofArray([createElement("div", createObj(Helpers_combineClasses("field", singleton(["dangerouslySetInnerHTML", {
        __html: "Careful, this will delete <b>all</b> tables and <b>all</b> table history!",
    }])))), createElement("div", createObj(Helpers_combineClasses("field", singleton(["dangerouslySetInnerHTML", {
        __html: "There is no option to recover any information deleted in this way.",
    }]))))]), createElement("section", {
        className: "modal-card-body",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), (elms_2 = ofArray([createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", rmv], ["className", "is-info"], ["children", "Back"]])))), createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", (e) => {
        dispatch(new Msg_1(15, [new Msg(19, [])]));
        rmv(e);
    }], ["className", "is-danger"], ["children", "Delete"]]))))]), createElement("footer", {
        className: "modal-card-foot",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    }))]), createElement("div", {
        className: "modal-card",
        children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))));
}

//# sourceMappingURL=ResetTable.js.map
