import { Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, string_type, bool_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { substring } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { createElement } from "react";
import React from "react";
import { useFeliz_React__React_useState_Static_1505 } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { singleton as singleton_1, ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { empty, singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";

export const l = 200;

function isTooLong(str) {
    return str.length > l;
}

class UI extends Record {
    constructor(DescriptionTooLong, IsExpanded, DescriptionShort, DescriptionLong) {
        super();
        this.DescriptionTooLong = DescriptionTooLong;
        this.IsExpanded = IsExpanded;
        this.DescriptionShort = DescriptionShort;
        this.DescriptionLong = DescriptionLong;
    }
}

function UI_$reflection() {
    return record_type("Modals.TermModal.UI", [], UI, () => [["DescriptionTooLong", bool_type], ["IsExpanded", bool_type], ["DescriptionShort", string_type], ["DescriptionLong", string_type]]);
}

function UI_init_Z721C83C5(description) {
    const isTooLong_1 = isTooLong(description);
    return new UI(isTooLong_1, false, isTooLong_1 ? (substring(description, 0, l).trim() + ".. ") : description, description);
}

export function Main(mainInputProps) {
    let elems_5, elems_4, elems, elems_3, elms, children, elems_1;
    const rmv = mainInputProps.rmv;
    const dispatch = mainInputProps.dispatch;
    const term = mainInputProps.term;
    const patternInput = useFeliz_React__React_useState_Static_1505(UI_init_Z721C83C5(term.Description));
    const state = patternInput[0];
    return createElement("div", createObj(Helpers_combineClasses("modal", ofArray([["className", "is-active"], (elems_5 = [createElement("div", createObj(Helpers_combineClasses("modal-background", ofArray([["onClick", rmv], ["style", {
        backgroundColor: "transparent",
    }]])))), createElement("div", createObj(Helpers_combineClasses("modal-card", ofArray([["style", {
        maxWidth: 300,
        border: (((1 + "px ") + "solid") + " ") + "#3A3A3A",
        borderRadius: 0,
    }], (elems_4 = [createElement("header", createObj(Helpers_combineClasses("modal-card-head", ofArray([["className", "p-2"], (elems = [createElement("p", createObj(Helpers_combineClasses("modal-card-title", ofArray([["className", "is-size-6"], ["children", `${term.Name} (${term.Accession})`]])))), createElement("button", createObj(Helpers_combineClasses("delete", ofArray([["className", "is-small"], ["onClick", rmv]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))), createElement("section", createObj(Helpers_combineClasses("modal-card-body", ofArray([["className", "p-2 has-text-justified"], (elems_3 = [(elms = ofArray([(children = toList(delay(() => append(singleton(createElement("span", {
        children: [state.IsExpanded ? state.DescriptionLong : state.DescriptionShort],
    })), delay(() => ((state.DescriptionTooLong && !state.IsExpanded) ? singleton(createElement("a", {
        onClick: (_arg) => {
            patternInput[1](new UI(state.DescriptionTooLong, true, state.DescriptionShort, state.DescriptionLong));
        },
        children: "(more)",
    })) : empty()))))), createElement("p", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    })), createElement("div", createObj(ofArray([["style", {
        display: "flex",
        justifyContent: "space-between",
    }], (elems_1 = toList(delay(() => {
        let children_2;
        return append(singleton((children_2 = singleton_1(createElement("a", {
            href: "https://ontobee.org/ontology/" + term.FK_Ontology,
            children: "Source",
        })), createElement("span", {
            children: Interop_reactApi.Children.toArray(Array.from(children_2)),
        }))), delay(() => (term.IsObsolete ? singleton(createElement("span", {
            className: "has-text-danger",
            children: "obsolete",
        })) : empty())));
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))]), createElement("div", {
        className: "content",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))])]))));
}

//# sourceMappingURL=TermModal.js.map
