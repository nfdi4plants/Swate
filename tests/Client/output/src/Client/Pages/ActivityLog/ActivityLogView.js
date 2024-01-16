import { createElement } from "react";
import { createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Msg } from "../../Messages.js";
import { map, ofArray } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { LogItem_get_toTableRow } from "../../Model.js";

export function debugBox(model, dispatch) {
    const elms = ofArray([createElement("button", createObj(Helpers_combineClasses("button", ofArray([["onClick", (e) => {
        dispatch(new Msg(23, []));
    }], ["children", "Test api"]])))), createElement("button", createObj(Helpers_combineClasses("button", ofArray([["onClick", (e_1) => {
        dispatch(new Msg(24, []));
    }], ["children", "Test post api"]]))))]);
    return createElement("div", {
        className: "box",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

export function activityLogComponent(model, dispatch) {
    let elems_1, children;
    const children_2 = ofArray([createElement("label", {
        className: "label",
        children: "Activity Log",
    }), createElement("label", {
        className: "label",
        children: "Display all recorded activities of this session.",
    }), createElement("div", createObj(ofArray([["style", {
        borderLeft: (((5 + "px ") + "solid") + " ") + "#1FC2A7",
        padding: ((0.25 + "rem") + " ") + (1 + "rem"),
        marginBottom: 1 + "rem",
    }], (elems_1 = [createElement("table", createObj(Helpers_combineClasses("table", ofArray([["className", "is-fullwidth"], ["children", (children = map(LogItem_get_toTableRow(), model.DevState.Log), createElement("tbody", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    }))]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))]);
    return createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children_2)),
    });
}

//# sourceMappingURL=ActivityLogView.js.map
