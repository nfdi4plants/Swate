import { createElement } from "react";
import React from "react";
import { useFeliz_React__React_useState_Static_1505 } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { round, createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { singleton, cons, ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Msg } from "../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../Messages.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export function Main(mainInputProps) {
    let elems_3, elems_2, elems_1, elms;
    const dispatch = mainInputProps.dispatch;
    const patternInput = useFeliz_React__React_useState_Static_1505(1);
    const setState_rows = patternInput[1];
    return createElement("div", createObj(ofArray([["id", "ExpandTable"], ["title", "Add rows"], ["style", {
        flexGrow: 1,
        justifyContent: "center",
        display: "inherit",
        padding: 1 + "rem",
        position: "sticky",
        left: 0,
    }], (elems_3 = [createElement("div", createObj(ofArray([["style", {
        height: "max-content",
        display: "flex",
    }], (elems_2 = [createElement("input", createObj(cons(["type", "number"], Helpers_combineClasses("input", ofArray([["id", "n_row_input"], ["min", 1], ["onChange", (ev) => {
        const value_22 = ev.target.valueAsNumber;
        if (!(value_22 == null) && !Number.isNaN(value_22)) {
            setState_rows(round(value_22));
        }
    }], ["defaultValue", 1], ["style", {
        width: 50,
    }]]))))), createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-rounded"], ["onClick", (_arg) => {
        const inp = document.getElementById("n_row_input");
        inp.Value = 1;
        setState_rows(1);
        dispatch(new Msg_1(15, [new Msg(8, [patternInput[0]])]));
    }], (elems_1 = [(elms = singleton(createElement("i", {
        className: "fa-solid fa-plus",
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])])));
}

//# sourceMappingURL=AddRows.js.map
