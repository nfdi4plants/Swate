import { toString, Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { lambda_type, tuple_type, option_type, int32_type, array_type, record_type, bool_type, string_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { TermTypes_TermMinimal_fromOntologyAnnotation_Z4C0FE73C, TermTypes_TermMinimal_$reflection } from "../../Shared/TermTypes.js";
import { AdvancedSearch_Msg, BuildingBlock_Msg, Msg, TermSearch_Msg, Msg_$reflection } from "../Messages.js";
import { collect, map } from "../../../fable_modules/fable-library.4.9.0/Array.js";
import { cons, singleton as singleton_1, ofArray, append } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { printf, toText } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { createElement } from "react";
import * as react from "react";
import { append as append_1, empty, singleton, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { keyValueList } from "../../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { CSSProp, HTMLAttr, DOMAttr } from "../../../fable_modules/Fable.React.9.3.0/Fable.React.Props.fs.js";
import { advancedSearchModal, createLinkOfAccession } from "./AdvancedSearch.js";
import { equals, createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Msg as Msg_1 } from "../States/CytoscapeState.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { Helpdesk_get_UrlOntologyTopic } from "../../Shared/URLs.js";
import { loadingComponent } from "../Modals/Loading.js";
import { Colorfull_white, Colorfull_gray40 } from "../Colors/ExcelColors.js";
import { bind, value as value_36 } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { Model__get_headerIsSelected, Model__get_getSelectedColumnHeader } from "../States/Spreadsheet.js";
import { Msg as Msg_2 } from "../OfficeInterop/OfficeInteropState.js";

export class AutocompleteSuggestion$1 extends Record {
    constructor(Name, ID, TooltipText, Status, StatusIsWarning, Data) {
        super();
        this.Name = Name;
        this.ID = ID;
        this.TooltipText = TooltipText;
        this.Status = Status;
        this.StatusIsWarning = StatusIsWarning;
        this.Data = Data;
    }
}

export function AutocompleteSuggestion$1_$reflection(gen0) {
    return record_type("SidebarComponents.AutocompleteSearch.AutocompleteSuggestion`1", [gen0], AutocompleteSuggestion$1, () => [["Name", string_type], ["ID", string_type], ["TooltipText", string_type], ["Status", string_type], ["StatusIsWarning", bool_type], ["Data", gen0]]);
}

export function AutocompleteSuggestion$1_ofTerm_Z5E0A7659(term) {
    return new AutocompleteSuggestion$1(term.Name, term.Accession, term.Description, term.IsObsolete ? "obsolete" : "", term.IsObsolete, term);
}

export function AutocompleteSuggestion$1_ofOntology_Z25762112(ont) {
    return new AutocompleteSuggestion$1(ont.Name, ont.Version, "", "", false, ont);
}

export class AutocompleteParameters$1 extends Record {
    constructor(ModalId, InputId, StateBinding, Suggestions, MaxItems, DropDownIsVisible, DropDownIsLoading, OnInputChangeMsg, OnSuggestionSelect, HasAdvancedSearch, AdvancedSearchLinkText, OnAdvancedSearch) {
        super();
        this.ModalId = ModalId;
        this.InputId = InputId;
        this.StateBinding = StateBinding;
        this.Suggestions = Suggestions;
        this.MaxItems = (MaxItems | 0);
        this.DropDownIsVisible = DropDownIsVisible;
        this.DropDownIsLoading = DropDownIsLoading;
        this.OnInputChangeMsg = OnInputChangeMsg;
        this.OnSuggestionSelect = OnSuggestionSelect;
        this.HasAdvancedSearch = HasAdvancedSearch;
        this.AdvancedSearchLinkText = AdvancedSearchLinkText;
        this.OnAdvancedSearch = OnAdvancedSearch;
    }
}

export function AutocompleteParameters$1_$reflection(gen0) {
    return record_type("SidebarComponents.AutocompleteSearch.AutocompleteParameters`1", [gen0], AutocompleteParameters$1, () => [["ModalId", string_type], ["InputId", string_type], ["StateBinding", string_type], ["Suggestions", array_type(AutocompleteSuggestion$1_$reflection(gen0))], ["MaxItems", int32_type], ["DropDownIsVisible", bool_type], ["DropDownIsLoading", bool_type], ["OnInputChangeMsg", lambda_type(tuple_type(string_type, option_type(TermTypes_TermMinimal_$reflection())), Msg_$reflection())], ["OnSuggestionSelect", lambda_type(gen0, Msg_$reflection())], ["HasAdvancedSearch", bool_type], ["AdvancedSearchLinkText", string_type], ["OnAdvancedSearch", lambda_type(gen0, Msg_$reflection())]]);
}

export function AutocompleteParameters$1_ofTermSearchState_Z6DDF587B(state) {
    return new AutocompleteParameters$1("TermSearch_ID", "TermSearchInput_ID", state.TermSearchText, map(AutocompleteSuggestion$1_ofTerm_Z5E0A7659, state.TermSuggestions), 5, state.ShowSuggestions, state.HasSuggestionsLoading, (arg) => {
        let tupledArg;
        return new Msg(4, [(tupledArg = arg, new TermSearch_Msg(1, [tupledArg[0], tupledArg[1]]))]);
    }, (term_1) => (new Msg(4, [new TermSearch_Msg(2, [term_1])])), true, "Cant find the Term you are looking for?", (term_2) => (new Msg(4, [new TermSearch_Msg(2, [term_2])])));
}

export function AutocompleteParameters$1_ofAddBuildingBlockUnit2State_32E8F41A(state) {
    return new AutocompleteParameters$1("Unit2Search_ID", "Unit2SearchInput_ID", state.Unit2TermSearchText, map(AutocompleteSuggestion$1_ofTerm_Z5E0A7659, state.Unit2TermSuggestions), 5, state.ShowUnit2TermSuggestions, state.HasUnit2TermSuggestionsLoading, (tupledArg) => (new Msg(9, [new BuildingBlock_Msg(10, [tupledArg[0]])])), (sugg) => (new Msg(9, [new BuildingBlock_Msg(11, [sugg])])), true, "Can\'t find the unit you are looking for?", (sugg_1) => (new Msg(9, [new BuildingBlock_Msg(11, [sugg_1])])));
}

export function createAutocompleteSuggestions(dispatch, autocompleteParams, model) {
    let children_25, children_31, children_29, children_37, children_35, props_41;
    return append((autocompleteParams.Suggestions.length > 0) ? ofArray(collect((sugg) => {
        let children_14, children_2, props_4, props_8, children_8, children_6, children_12, elems_4, elms, elms_1, children_21, children_19, elms_2;
        const id = toText(printf("isHidden_%s"))(sugg.ID);
        return [(children_14 = [(children_2 = [react.createElement("b", {}, sugg.Name)], react.createElement("td", {}, ...children_2)), (props_4 = toList(delay(() => (sugg.StatusIsWarning ? singleton(["style", {
            color: "red",
        }]) : empty()))), react.createElement("td", keyValueList(props_4, 1), sugg.Status)), (props_8 = [new DOMAttr(40, [(e_1) => {
            e_1.stopPropagation();
        }]), ["style", {
            fontWeight: "light",
        }]], (children_8 = [(children_6 = [createLinkOfAccession(sugg.ID)], react.createElement("small", {}, ...children_6))], react.createElement("td", keyValueList(props_8, 1), ...children_8))), (children_12 = [createElement("div", createObj(Helpers_combineClasses("buttons", ofArray([["className", "is-right"], (elems_4 = [createElement("a", createObj(Helpers_combineClasses("button", ofArray([["title", "Show Term Tree"], ["className", "is-small"], ["className", "is-success"], ["className", "is-inverted"], ["onClick", (e_2) => {
            e_2.preventDefault();
            e_2.stopPropagation();
            dispatch(new Msg(14, [new Msg_1(0, [sugg.ID])]));
        }], ["children", (elms = singleton_1(createElement("i", {
            className: "fa-solid fa-tree",
        })), createElement("span", {
            className: "icon",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        }))]])))), createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-small"], ["className", "is-black"], ["className", "is-inverted"], ["onClick", (e_3) => {
            let vis;
            e_3.preventDefault();
            e_3.stopPropagation();
            const ele = document.getElementById(id);
            if ((vis = toString(ele.style.visibility), (vis === "collapse") ? true : (vis === ""))) {
                ele.style.visibility = "visible";
            }
            else {
                ele.style.visibility = "collapse";
            }
        }], ["children", (elms_1 = singleton_1(createElement("i", {
            className: "fa-solid fa-chevron-down",
        })), createElement("span", {
            className: "icon",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        }))]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))], react.createElement("td", {}, ...children_12))], react.createElement("tr", {
            onClick: (_arg) => {
                const e = document.getElementById(autocompleteParams.InputId);
                e.value = sugg.Name;
                dispatch(autocompleteParams.OnSuggestionSelect(sugg.Data));
            },
            onKeyDown: (k) => {
                if (k.key === "Enter") {
                    dispatch(autocompleteParams.OnSuggestionSelect(sugg.Data));
                }
            },
            tabIndex: 0,
            className: "suggestion",
        }, ...children_14)), (children_21 = [(children_19 = [(elms_2 = ofArray([react.createElement("b", {}, "Definition: "), (sugg.TooltipText === "") ? "No definition found" : sugg.TooltipText]), createElement("div", {
            className: "content",
            children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
        }))], react.createElement("td", {
            colSpan: 4,
        }, ...children_19))], react.createElement("tr", {
            onClick: (e_4) => {
                e_4.stopPropagation();
            },
            id: id,
            className: "suggestion-details",
        }, ...children_21))];
    }, autocompleteParams.Suggestions)) : singleton_1((children_25 = [react.createElement("td", {}, "No terms found matching your input.")], react.createElement("tr", {}, ...children_25))), append(singleton_1((children_31 = [(children_29 = [toText(printf("%s "))(autocompleteParams.AdvancedSearchLinkText), "Try ", react.createElement("a", {
        onClick: (_arg_1) => {
            dispatch(new Msg(5, [new AdvancedSearch_Msg(0, [autocompleteParams.ModalId])]));
        },
    }, "Advanced Search"), "!"], react.createElement("td", {
        colSpan: 4,
    }, ...children_29))], react.createElement("tr", {
        className: "suggestion",
    }, ...children_31))), singleton_1((children_37 = [(children_35 = ["Still can\'t find what you need? Get in ", (props_41 = [new HTMLAttr(94, [Helpdesk_get_UrlOntologyTopic()]), new HTMLAttr(157, ["_Blank"])], react.createElement("a", keyValueList(props_41, 1), "contact")), " with us!"], react.createElement("td", {
        colSpan: 4,
    }, ...children_35))], react.createElement("tr", {
        className: "suggestion",
    }, ...children_37)))));
}

export function autocompleteDropdownComponent(dispatch, isVisible, isLoading, suggestions) {
    let props_12, children_8;
    const props_14 = [["style", {
        position: "relative",
    }]];
    const children_10 = [(props_12 = [["style", keyValueList(toList(delay(() => append_1(isVisible ? singleton(new CSSProp(125, ["block"])) : singleton(new CSSProp(125, ["none"])), delay(() => append_1(singleton(new CSSProp(404, ["20"])), delay(() => append_1(singleton(new CSSProp(395, ["100%"])), delay(() => append_1(singleton(new CSSProp(248, ["400px"])), delay(() => append_1(singleton(new CSSProp(291, ["absolute"])), delay(() => append_1(singleton(new CSSProp(226, ["-0.5rem"])), delay(() => append_1(singleton(new CSSProp(272, ["auto"])), delay(() => append_1(singleton(new CSSProp(82, ["0 0.5px 0.5px 0.5px"])), delay(() => singleton(new CSSProp(75, ["solid"])))))))))))))))))))), 1)]], (children_8 = [createElement("table", createObj(Helpers_combineClasses("table", toList(delay(() => append_1(singleton(["className", "is-fullwidth"]), delay(() => {
        let props_6, children_4, children_2, props_2, children;
        return isLoading ? singleton(["children", (props_6 = [["style", {
            height: "75px",
        }]], (children_4 = [(children_2 = [(props_2 = [["style", {
            textAlign: "center",
        }]], (children = [loadingComponent, react.createElement("br", {})], react.createElement("td", keyValueList(props_2, 1), ...children)))], react.createElement("tr", {}, ...children_2))], react.createElement("tbody", keyValueList(props_6, 1), ...children_4)))]) : singleton(["children", react.createElement("tbody", {}, ...suggestions)]);
    })))))))], react.createElement("div", keyValueList(props_12, 1), ...children_8)))];
    return react.createElement("div", keyValueList(props_14, 1), ...children_10);
}

export function autocompleteTermSearchComponentInputComponent(dispatch, isDisabled, inputPlaceholderText, inputSize, autocompleteParams) {
    return createElement("p", createObj(Helpers_combineClasses("control", ofArray([["className", "is-expanded"], ["children", createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", toList(delay(() => append_1(singleton(["style", createObj(toList(delay(() => (isDisabled ? singleton(["borderColor", Colorfull_gray40]) : empty()))))]), delay(() => append_1(singleton(["disabled", isDisabled]), delay(() => append_1(singleton(["placeholder", inputPlaceholderText]), delay(() => {
        let value_8;
        return append_1(singleton((value_8 = autocompleteParams.StateBinding, ["ref", (e) => {
            if (!(e == null) && !equals(e.value, value_8)) {
                e.value = value_8;
            }
        }])), delay(() => {
            let matchValue;
            return append_1((matchValue = inputSize, (matchValue != null) ? ((value_36(matchValue), empty())) : (empty())), delay(() => append_1(singleton(["onDoubleClick", (e_1) => {
                const v = document.getElementById(autocompleteParams.InputId);
                dispatch(autocompleteParams.OnInputChangeMsg(v.value));
            }]), delay(() => append_1(singleton(["onChange", (ev) => {
                dispatch(autocompleteParams.OnInputChangeMsg([ev.target.value, void 0]));
            }]), delay(() => singleton(["id", autocompleteParams.InputId])))))));
        }));
    }))))))))))))]]))));
}

export function autocompleteTermSearchComponentOfParentOntology(dispatch, model, inputPlaceholderText, inputSize, autocompleteParams) {
    let elems_2;
    let useParentTerm;
    const matchValue_1 = model.PersistentStorageState.Host;
    let matchResult;
    if (matchValue_1 != null) {
        switch (matchValue_1.tag) {
            case 1: {
                matchResult = 0;
                break;
            }
            case 0: {
                if (!Model__get_headerIsSelected(model.SpreadsheetModel)) {
                    matchResult = 1;
                }
                else {
                    matchResult = 2;
                }
                break;
            }
            default:
                matchResult = 2;
        }
    }
    else {
        matchResult = 2;
    }
    switch (matchResult) {
        case 0: {
            useParentTerm = (model.TermSearchState.ParentOntology != null);
            break;
        }
        case 1: {
            const header = Model__get_getSelectedColumnHeader(model.SpreadsheetModel);
            if (header == null) {
                useParentTerm = false;
            }
            else {
                const h = header;
                useParentTerm = h.IsTermColumn;
            }
            break;
        }
        default:
            useParentTerm = false;
    }
    let parentTerm_1;
    const matchValue_2 = model.PersistentStorageState.Host;
    let matchResult_1;
    if (matchValue_2 != null) {
        switch (matchValue_2.tag) {
            case 1: {
                matchResult_1 = 0;
                break;
            }
            case 0: {
                matchResult_1 = 1;
                break;
            }
            default:
                matchResult_1 = 2;
        }
    }
    else {
        matchResult_1 = 2;
    }
    switch (matchResult_1) {
        case 0: {
            parentTerm_1 = model.TermSearchState.ParentOntology;
            break;
        }
        case 1: {
            parentTerm_1 = bind((header_2) => {
                if (header_2.IsTermColumn) {
                    return TermTypes_TermMinimal_fromOntologyAnnotation_Z4C0FE73C(header_2.ToTerm());
                }
                else {
                    return void 0;
                }
            }, Model__get_getSelectedColumnHeader(model.SpreadsheetModel));
            break;
        }
        default:
            parentTerm_1 = void 0;
    }
    const elms = ofArray([advancedSearchModal(model, autocompleteParams.ModalId, autocompleteParams.InputId, dispatch, autocompleteParams.OnAdvancedSearch), createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "has-addons"], (elems_2 = toList(delay(() => {
        let parenTermText;
        return append_1((useParentTerm && model.TermSearchState.SearchByParentOntology) ? singleton((parenTermText = value_36(parentTerm_1).Name, createElement("p", createObj(Helpers_combineClasses("control", ofArray([["style", {
            maxWidth: 40 + "%",
        }], ["title", parenTermText], ["children", createElement("button", createObj(Helpers_combineClasses("button", toList(delay(() => append_1(singleton(["style", {
            backgroundColor: Colorfull_white,
        }]), delay(() => append_1(singleton(["className", "is-static"]), delay(() => {
            let matchValue;
            return append_1((matchValue = inputSize, (matchValue != null) ? singleton(matchValue) : (empty())), delay(() => singleton(["children", parenTermText])));
        })))))))))]])))))) : empty(), delay(() => singleton(createElement("p", createObj(Helpers_combineClasses("control", ofArray([["className", "is-expanded"], ["children", createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", toList(delay(() => append_1(singleton(["id", autocompleteParams.InputId]), delay(() => append_1(singleton(["placeholder", inputPlaceholderText]), delay(() => {
            let value_22;
            return append_1(singleton((value_22 = autocompleteParams.StateBinding, ["ref", (e) => {
                if (!(e == null) && !equals(e.value, value_22)) {
                    e.value = value_22;
                }
            }])), delay(() => {
                let matchValue_3;
                return append_1((matchValue_3 = inputSize, (matchValue_3 != null) ? singleton(matchValue_3) : (empty())), delay(() => append_1(singleton(["onFocus", (e_1) => {
                    const matchValue_4 = model.PersistentStorageState.Host;
                    let matchResult_2;
                    if (matchValue_4 != null) {
                        if (matchValue_4.tag === 1) {
                            matchResult_2 = 0;
                        }
                        else {
                            matchResult_2 = 1;
                        }
                    }
                    else {
                        matchResult_2 = 1;
                    }
                    switch (matchResult_2) {
                        case 0: {
                            dispatch(new Msg(6, [new Msg_2(10, [])]));
                            const el = document.getElementById(autocompleteParams.InputId);
                            el.focus();
                            break;
                        }
                        case 1: {
                            break;
                        }
                    }
                }]), delay(() => append_1(singleton(["onDoubleClick", (e_2) => {
                    if (useParentTerm && (model.TermSearchState.TermSearchText === "")) {
                        dispatch(new Msg(4, [new TermSearch_Msg(5, [value_36(parentTerm_1)])]));
                    }
                    else {
                        const parenTerm = useParentTerm ? parentTerm_1 : void 0;
                        const v = document.getElementById(autocompleteParams.InputId);
                        dispatch(autocompleteParams.OnInputChangeMsg([v.value, parenTerm]));
                    }
                }]), delay(() => singleton(["onChange", (ev) => {
                    dispatch(autocompleteParams.OnInputChangeMsg([ev.target.value, useParentTerm ? parentTerm_1 : void 0]));
                }])))))));
            }));
        }))))))))))]])))))));
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])))), autocompleteDropdownComponent(dispatch, autocompleteParams.DropDownIsVisible, autocompleteParams.DropDownIsLoading, createAutocompleteSuggestions(dispatch, autocompleteParams, model))]);
    return createElement("div", {
        className: "control",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

//# sourceMappingURL=AutocompleteSearch.js.map
