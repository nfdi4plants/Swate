import { TermSearch_Model_init, TermSearch_Model } from "../../Model.js";
import { fromSeconds } from "../../../../fable_modules/fable-library.4.9.0/TimeSpan.js";
import { AdvancedSearch_Msg, DevMsg, curry, TermSearch_Msg, Msg, ApiMsg, ApiRequestMsg } from "../../Messages.js";
import { cons, ofArray, singleton } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Cmd_none } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { Cmd_OfAsync_start, Cmd_OfAsyncWith_either } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { api } from "../../Api.js";
import { pageHeader, mainFunctionContainer } from "../../SidebarComponents/LayoutHelper.js";
import { AutocompleteParameters$1_ofTermSearchState_Z6DDF587B, autocompleteTermSearchComponentOfParentOntology } from "../../SidebarComponents/AutocompleteSearch.js";
import { createElement } from "react";
import * as react from "react";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { empty, singleton as singleton_1, append, delay as delay_1, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { value as value_76 } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { OntologyAnnotation } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/OntologyAnnotation.fs.js";
import { Msg as Msg_1 } from "../../States/SpreadsheetInterface.js";
import { responsiveReturnEle, triggerResponsiveReturnEle } from "../../SidebarComponents/ResponsiveFA.js";
import { split, join } from "../../../../fable_modules/fable-library.4.9.0/String.js";
import { termAccessionUrlOfAccessionStr } from "../../../Shared/URLs.js";

export function update(termSearchMsg, currentState) {
    let msg, termMin, tupledArg, bind$0040;
    switch (termSearchMsg.tag) {
        case 1: {
            const parentTerm = termSearchMsg.fields[1];
            const newTerm = termSearchMsg.fields[0];
            const triggerNewSearch = newTerm.trim() !== "";
            return [new TermSearch_Model(newTerm, void 0, currentState.TermSuggestions, currentState.ParentOntology, currentState.SearchByParentOntology, true, triggerNewSearch), (msg = (new Msg(0, [[fromSeconds(1), "GetNewTermSuggestions", triggerNewSearch ? ((parentTerm == null) ? (new Msg(2, [new ApiMsg(0, [new ApiRequestMsg(0, [newTerm])])])) : (currentState.SearchByParentOntology ? ((termMin = parentTerm, new Msg(2, [new ApiMsg(0, [(tupledArg = [newTerm, termMin], new ApiRequestMsg(1, [tupledArg[0], tupledArg[1]]))])]))) : (new Msg(2, [new ApiMsg(0, [new ApiRequestMsg(0, [newTerm])])])))) : (new Msg(25, []))]])), singleton((dispatch) => {
                dispatch(msg);
            }))];
        }
        case 2: {
            const suggestion = termSearchMsg.fields[0];
            return [(bind$0040 = TermSearch_Model_init(), new TermSearch_Model(suggestion.Name, suggestion, bind$0040.TermSuggestions, bind$0040.ParentOntology, currentState.SearchByParentOntology, bind$0040.HasSuggestionsLoading, bind$0040.ShowSuggestions)), Cmd_none()];
        }
        case 3:
            return [new TermSearch_Model(currentState.TermSearchText, currentState.SelectedTerm, termSearchMsg.fields[0], currentState.ParentOntology, currentState.SearchByParentOntology, false, true), Cmd_none()];
        case 4:
            return [new TermSearch_Model(currentState.TermSearchText, currentState.SelectedTerm, currentState.TermSuggestions, termSearchMsg.fields[0], currentState.SearchByParentOntology, currentState.HasSuggestionsLoading, currentState.ShowSuggestions), Cmd_none()];
        case 5:
            return [new TermSearch_Model(currentState.TermSearchText, currentState.SelectedTerm, currentState.TermSuggestions, currentState.ParentOntology, currentState.SearchByParentOntology, true, currentState.ShowSuggestions), Cmd_OfAsyncWith_either((x) => {
                Cmd_OfAsync_start(x);
            }, api.getAllTermsByParentTerm, termSearchMsg.fields[0], (arg_4) => (new Msg(4, [new TermSearch_Msg(6, [arg_4])])), (arg_5) => (new Msg(3, [curry((tupledArg_1) => (new DevMsg(3, [tupledArg_1[0], tupledArg_1[1]])), Cmd_none(), arg_5)])))];
        case 6:
            return [new TermSearch_Model(currentState.TermSearchText, currentState.SelectedTerm, termSearchMsg.fields[0], currentState.ParentOntology, currentState.SearchByParentOntology, false, true), Cmd_none()];
        default:
            return [new TermSearch_Model(currentState.TermSearchText, currentState.SelectedTerm, currentState.TermSuggestions, currentState.ParentOntology, !currentState.SearchByParentOntology, currentState.HasSuggestionsLoading, currentState.ShowSuggestions), Cmd_none()];
    }
}

export function simpleSearchComponent(model, dispatch) {
    let elms, children_3, elems_1, elems_8;
    return mainFunctionContainer([(elms = singleton(autocompleteTermSearchComponentOfParentOntology(dispatch, model, "Start typing to search for terms", ["className", "is-large"], AutocompleteParameters$1_ofTermSearchState_Z6DDF587B(model.TermSearchState))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), (children_3 = [createElement("div", createObj(ofArray([["style", {
        display: "inline",
        float: "left",
    }], (elems_1 = toList(delay_1(() => append(singleton_1(createElement("input", createObj(cons(["type", "checkbox"], Helpers_combineClasses("switch", ofArray([["className", "is-primary"], ["id", "switch-1"], ["checked", model.TermSearchState.SearchByParentOntology], ["onChange", (ev) => {
        const e = ev.target.checked;
        dispatch(new Msg(4, [new TermSearch_Msg(0, [])]));
        const inpId = AutocompleteParameters$1_ofTermSearchState_Z6DDF587B(model.TermSearchState).InputId;
        const e_1 = document.getElementById(inpId);
        e_1.focus();
    }]])))))), delay_1(() => singleton_1(createElement("label", {
        htmlFor: "switch-1",
        children: "Use related term directed search.",
    })))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))), createElement("p", createObj(Helpers_combineClasses("help", ofArray([["style", {
        display: "inline",
        float: "right",
    }], ["children", react.createElement("a", {
        onClick: (_arg) => {
            dispatch(new Msg(5, [new AdvancedSearch_Msg(0, [AutocompleteParameters$1_ofTermSearchState_Z6DDF587B(model.TermSearchState).ModalId])]));
        },
    }, "Use advanced search")]]))))], react.createElement("div", {}, ...children_3)), createElement("div", createObj(Helpers_combineClasses("columns", ofArray([["className", "is-mobile"], ["style", {
        width: 100 + "%",
        marginRight: 0,
        marginLeft: 0,
    }], (elems_8 = toList(delay_1(() => {
        let elms_2, elms_1;
        return append(singleton_1(createElement("div", createObj(Helpers_combineClasses("column", ofArray([["style", createObj(toList(delay_1(() => append(singleton_1(["paddingLeft", 0]), delay_1(() => ((model.TermSearchState.SelectedTerm == null) ? singleton_1(["paddingRight", 0]) : empty()))))))], ["children", (elms_2 = singleton((elms_1 = singleton(createElement("a", createObj(Helpers_combineClasses("button", toList(delay_1(() => {
            const hasText = model.TermSearchState.TermSearchText.length > 0;
            return append(hasText ? singleton_1(["className", "is-success"]) : append(singleton_1(["className", "is-danger"]), delay_1(() => singleton_1(["disabled", true]))), delay_1(() => append(singleton_1(["className", "is-fullwidth"]), delay_1(() => append(singleton_1(["onClick", (_arg_1) => {
                let t;
                if (hasText) {
                    dispatch(new Msg(17, [new Msg_1(7, [(model.TermSearchState.SelectedTerm != null) ? ((t = value_76(model.TermSearchState.SelectedTerm), OntologyAnnotation.fromString(t.Name, t.FK_Ontology, t.Accession))) : OntologyAnnotation.fromString(model.TermSearchState.TermSearchText)])]));
                }
            }]), delay_1(() => singleton_1(["children", "Fill selected cells with this term"])))))));
        })))))), createElement("div", {
            className: "control",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        }))), createElement("div", {
            className: "field",
            children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
        }))]]))))), delay_1(() => ((model.TermSearchState.SelectedTerm != null) ? singleton_1(createElement("div", createObj(Helpers_combineClasses("column", ofArray([["className", "pr-0"], ["className", "is-narrow"], ["children", createElement("a", createObj(Helpers_combineClasses("button", ofArray([["title", "Copy to Clipboard"], ["className", "is-info"], ["onClick", (e_2) => {
            triggerResponsiveReturnEle("clipboard_termsearch");
            const t_1 = value_76(model.TermSearchState.SelectedTerm);
            const txt = join("\n", [t_1.Name, termAccessionUrlOfAccessionStr(t_1.Accession), split(t_1.Accession, [":"], void 0, 0)[0]]);
            const textArea = document.createElement("textarea");
            textArea.value = txt;
            textArea.style.top = "0";
            textArea.style.left = "0";
            textArea.style.position = "fixed";
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            const t_2 = document.execCommand("copy");
            document.body.removeChild(textArea);
        }], ["children", responsiveReturnEle("clipboard_termsearch", "fa-regular fa-clipboard", "fa-solid fa-check")]]))))]]))))) : empty())));
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_8))])]))))]);
}

export function termSearchComponent(model, dispatch) {
    const children = [pageHeader("Ontology term search"), createElement("label", {
        className: "label",
        children: "Search for an ontology term to fill into the selected field(s)",
    }), simpleSearchComponent(model, dispatch)];
    return react.createElement("div", {
        onSubmit: (e) => {
            e.preventDefault();
        },
        onKeyDown: (k) => {
            if (~~k.which === 13) {
                k.preventDefault();
            }
        },
    }, ...children);
}

//# sourceMappingURL=TermSearchView.js.map
