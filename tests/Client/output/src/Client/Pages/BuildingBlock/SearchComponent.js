import { join, endsWith, printf, toText } from "../../../../fable_modules/fable-library.4.9.0/String.js";
import { createElement } from "react";
import React from "react";
import { curry2, equals, stringHash, createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { cons, append as append_1, ofArray, singleton } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { empty, singleton as singleton_1, append, delay, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { advancedSearchModal, createLinkOfAccession } from "../../SidebarComponents/AdvancedSearch.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Msg } from "../../States/CytoscapeState.js";
import { AdvancedSearch_Msg, BuildingBlock_Msg, Msg as Msg_1 } from "../../Messages.js";
import { toString } from "../../../../fable_modules/fable-library.4.9.0/Types.js";
import { collect } from "../../../../fable_modules/fable-library.4.9.0/Array.js";
import { TermSearch_TermSearchUIState_init, BuildingBlock_BuildingBlockUIState_init, AdvancedSearch_Model_get_BuildingBlockBodyId, AdvancedSearch_Model_get_BuildingBlockHeaderId, BuildingBlock_BuildingBlockUIState, BuildingBlock_BodyCellType, TermSearch_TermSearchUIController, TermSearch_TermSearchUIState } from "../../Model.js";
import { Array_distinctBy } from "../../../../fable_modules/fable-library.4.9.0/Seq2.js";
import { Helpdesk_get_UrlOntologyTopic } from "../../../Shared/URLs.js";
import { loadingComponent } from "../../Modals/Loading.js";
import { isValidColumn, createCellFromUiStateAndOA, hasVerifiedCell, selectBody as selectBody_1, hasVerifiedTermHeader, selectHeader as selectHeader_1 } from "./Helper.js";
import { ARCtrl_ISA_CompositeCell__CompositeCell_ToTerm, ARCtrl_ISA_OntologyAnnotation__OntologyAnnotation_ToTermMinimal, ARCtrl_ISA_CompositeCell__CompositeCell_UpdateWithOA_Z4C0FE73C, ARCtrl_ISA_OntologyAnnotation__OntologyAnnotation_fromTerm_Static_Z5E0A7659, ARCtrl_ISA_CompositeHeader__CompositeHeader_UpdateWithOA_Z4C0FE73C } from "../../../Shared/ARCtrl.Helper.js";
import { OntologyAnnotation } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/OntologyAnnotation.fs.js";
import { fromSeconds } from "../../../../fable_modules/fable-library.4.9.0/TimeSpan.js";
import { Main as Main_1 } from "./Dropdown.js";
import { value as value_64 } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { useReact_useState_FCFD9EF, useFeliz_React__React_useState_Static_1505 } from "../../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { CompositeColumn } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeColumn.fs.js";
import { Msg as Msg_2 } from "../../States/SpreadsheetInterface.js";
import { mainFunctionContainer } from "../../SidebarComponents/LayoutHelper.js";

function AutocompleteComponents_createTermElement_Main(term, selectMsg, dispatch) {
    let elems_6, children, elems, children_4, elems_5, elems_2, elms, elems_4, elms_1, elems_9, elems_8, elms_2;
    const id = term.Accession;
    const hiddenId = toText(printf("isHidden_%s"))(id);
    return [createElement("tr", createObj(ofArray([["key", id], ["onClick", (_arg) => {
        selectMsg();
    }], ["onKeyDown", (k) => {
        if (k.key === "Enter") {
            selectMsg();
        }
    }], ["tabIndex", 0], ["className", "suggestion"], (elems_6 = [(children = singleton(createElement("b", {
        children: [term.Name],
    })), createElement("td", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    })), createElement("td", createObj(toList(delay(() => (term.IsObsolete ? append(singleton_1(["className", "has-text-danger"]), delay(() => singleton_1(["children", "obsolete"]))) : empty()))))), createElement("td", createObj(ofArray([["onClick", (e) => {
        e.stopPropagation();
    }], ["style", {
        fontWeight: "lighter",
    }], (elems = [createLinkOfAccession(term.Accession)], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))), (children_4 = singleton(createElement("div", createObj(Helpers_combineClasses("buttons", ofArray([["className", "is-right"], (elems_5 = [createElement("a", createObj(Helpers_combineClasses("button", ofArray([["title", "Show Term Tree"], ["className", "is-small"], ["className", "is-success"], ["className", "is-inverted"], ["onClick", (e_1) => {
        e_1.preventDefault();
        e_1.stopPropagation();
        dispatch(new Msg_1(14, [new Msg(0, [term.Accession])]));
    }], (elems_2 = [(elms = singleton(createElement("i", {
        className: "fa-solid fa-tree",
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])))), createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-small"], ["className", "is-black"], ["className", "is-inverted"], ["onClick", (e_2) => {
        let vis;
        e_2.preventDefault();
        e_2.stopPropagation();
        const ele = document.getElementById(hiddenId);
        if ((vis = toString(ele.style.display), (vis === "none") ? true : (vis === ""))) {
            ele.style.display = "table-row";
        }
        else {
            ele.style.display = "none";
        }
    }], (elems_4 = [(elms_1 = singleton(createElement("i", {
        className: "fa-solid fa-chevron-down",
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))])]))))), createElement("td", {
        children: Interop_reactApi.Children.toArray(Array.from(children_4)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_6))])]))), createElement("tr", createObj(ofArray([["onClick", (e_3) => {
        e_3.stopPropagation();
    }], ["id", hiddenId], ["key", hiddenId], ["className", "suggestion-details"], (elems_9 = [createElement("td", createObj(ofArray([["colSpan", 4], (elems_8 = [(elms_2 = ofArray([createElement("b", {
        children: ["Definition: "],
    }), createElement("p", {
        children: [(term.Description === "") ? "No definition found" : term.Description],
    })]), createElement("div", {
        className: "content",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_8))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_9))])])))];
}

function AutocompleteComponents_createAutocompleteSuggestions(termSuggestions, selectMsg, state, setState, dispatch) {
    let children, elems_1, elems, elems_3, elems_2;
    return append_1((termSuggestions.length > 0) ? ofArray(collect((t_1) => AutocompleteComponents_createTermElement_Main(t_1, (e) => {
        setState(new TermSearch_TermSearchUIState(false, state.SearchIsLoading));
        selectMsg(t_1);
    }, dispatch), Array_distinctBy((t) => t.Accession, termSuggestions, {
        Equals: (x, y) => (x === y),
        GetHashCode: stringHash,
    }))) : singleton((children = singleton(createElement("td", {
        children: ["No terms found matching your input."],
    })), createElement("tr", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    }))), ofArray([createElement("tr", createObj(ofArray([["className", "suggestion"], (elems_1 = [createElement("td", createObj(ofArray([["colSpan", 4], (elems = [createElement("span", {
        children: ["Cant find the Term you are looking for? Try "],
    }), createElement("a", {
        onClick: (_arg) => {
            throw new Error("Not implemented in createAutocompleteSuggestions!");
        },
        children: "Advanced Search",
    }), createElement("span", {
        children: ["!"],
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))), createElement("tr", createObj(ofArray([["className", "suggestion"], (elems_3 = [createElement("td", createObj(ofArray([["colSpan", 4], (elems_2 = [createElement("span", {
        children: ["Still can\'t find what you need? Get in "],
    }), createElement("a", {
        href: Helpdesk_get_UrlOntologyTopic(),
        target: "_Blank",
        children: "contact",
    }), createElement("span", {
        children: [" with us!"],
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])])))]));
}

function AutocompleteComponents_closeElement(state, setState) {
    return createElement("div", {
        style: {
            position: "fixed",
            left: 0,
            top: 0,
            width: 100 + "%",
            height: 100 + "%",
            zIndex: 19,
            backgroundColor: "transparent",
        },
        onClick: (_arg) => {
            setState(new TermSearch_TermSearchUIState(false, state.SearchIsLoading));
        },
    });
}

function AutocompleteComponents_autocompleteDropdownComponent(termSuggestions, selectMsg, state, setState, model, dispatch) {
    let elems_4, elems_3, elems_2;
    const searchResults = AutocompleteComponents_createAutocompleteSuggestions(termSuggestions, selectMsg, state, setState, dispatch);
    return createElement("div", createObj(ofArray([["style", {
        position: "relative",
    }], (elems_4 = [AutocompleteComponents_closeElement(state, setState), createElement("div", createObj(ofArray([["style", {
        zIndex: 20,
        width: 100 + "%",
        maxHeight: 400,
        position: "absolute",
        marginTop: -0.5 + "rem",
        overflowY: "auto",
        borderWidth: "0 0.5px 0.5px 0.5px",
        borderStyle: "solid",
    }], (elems_3 = [createElement("table", createObj(Helpers_combineClasses("table", ofArray([["className", "is-fullwidth"], (elems_2 = toList(delay(() => {
        let elems_1, children, elems;
        return state.SearchIsLoading ? singleton_1(createElement("tbody", createObj(ofArray([["style", {
            height: 75,
        }], (elems_1 = [(children = singleton(createElement("td", createObj(ofArray([["style", {
            textAlign: "center",
        }], (elems = [loadingComponent, createElement("br", {})], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))), createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children)),
        }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))) : singleton_1(createElement("tbody", {
            children: Interop_reactApi.Children.toArray(Array.from(searchResults)),
        }));
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])])));
}

function basicTermSearchElement(inputId, onChangeMsg, onDoubleClickMsg, isVerified, state, setState, valueOrDefault) {
    let elems;
    return createElement("p", createObj(Helpers_combineClasses("control", ofArray([["className", "has-icons-right"], (elems = toList(delay(() => append(singleton_1(createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", toList(delay(() => append(isVerified ? singleton_1(["className", "is-success"]) : empty(), delay(() => append(singleton_1(["id", inputId]), delay(() => append(singleton_1(["key", inputId]), delay(() => append(singleton_1(["autoFocus", endsWith(inputId, "Main")]), delay(() => append(singleton_1(["placeholder", "Start typing to search"]), delay(() => {
        let value_12;
        return append(singleton_1((value_12 = valueOrDefault, ["ref", (e) => {
            if (!(e == null) && !equals(e.value, value_12)) {
                e.value = value_12;
            }
        }])), delay(() => append(singleton_1(["onDoubleClick", (e_1) => {
            onDoubleClickMsg(e_1.target.value);
        }]), delay(() => append(singleton_1(["onKeyDown", (e_2) => {
            if (e_2.which === 27) {
                setState(new TermSearch_TermSearchUIState(false, state.SearchIsLoading));
            }
        }]), delay(() => singleton_1(["onChange", (ev) => {
            onChangeMsg(ev.target.value);
        }])))))));
    }))))))))))))))))), delay(() => (isVerified ? singleton_1(createElement("span", createObj(Helpers_combineClasses("icon", ofArray([["className", "is-right"], ["className", "is-small"], ["className", "fas fa-check"]]))))) : empty()))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))));
}

export function header_searchElement(inputId, ui, setUi, ui_search, setUi_search, state, dispatch) {
    let elems;
    const onChangeMsg = (v) => {
        const triggerNewSearch = v.length > 2;
        dispatch(selectHeader_1(ui, setUi, ARCtrl_ISA_CompositeHeader__CompositeHeader_UpdateWithOA_Z4C0FE73C(state.Header, OntologyAnnotation.fromString(v))));
        if (triggerNewSearch) {
            setUi_search(new TermSearch_TermSearchUIState(true, true));
            const msg = new Msg_1(9, [new BuildingBlock_Msg(1, [v, new TermSearch_TermSearchUIController(ui_search, setUi_search)])]);
            dispatch(new Msg_1(0, [[fromSeconds(0.5), "msg", msg]]));
        }
    };
    return createElement("div", createObj(Helpers_combineClasses("control", ofArray([["className", "is-expanded"], (elems = toList(delay(() => {
        const valueOrDefault = state.Header.ToTerm().NameText;
        return singleton_1(basicTermSearchElement(inputId, onChangeMsg, onChangeMsg, hasVerifiedTermHeader(state.Header), ui_search, setUi_search, valueOrDefault));
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))));
}

function chooseBuildingBlock_element(ui, setUi, ui_search, setUi_search, model, dispatch) {
    const state = model.AddBuildingBlockState;
    const elms_1 = toList(delay(() => {
        let elems_2;
        return append(singleton_1(createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "has-addons"], (elems_2 = toList(delay(() => append(singleton_1(Main_1(ui, setUi, ui_search, setUi_search, model, dispatch)), delay(() => {
            let elems_1, elms, value_8;
            return (state.Header.IsTermColumn && !state.Header.IsFeaturedColumn) ? singleton_1(header_searchElement("BuildingBlock_InputMain", ui, setUi, ui_search, setUi_search, state, dispatch)) : (state.Header.IsIOType ? singleton_1(createElement("div", createObj(Helpers_combineClasses("control", ofArray([["className", "is-expanded"], (elems_1 = [(elms = singleton(createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", ofArray([["className", "has-background-grey-lighter"], ["readOnly", true], (value_8 = toString(value_64(state.Header.TryIOType())), ["ref", (e) => {
                if (!(e == null) && !equals(e.value, value_8)) {
                    e.value = value_8;
                }
            }])])))))), createElement("p", {
                className: "control",
                children: Interop_reactApi.Children.toArray(Array.from(elms)),
            }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))))) : empty());
        })))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))))), delay(() => (ui_search.SearchIsActive ? singleton_1(AutocompleteComponents_autocompleteDropdownComponent(model.AddBuildingBlockState.HeaderSearchResults, (term) => {
            dispatch(selectHeader_1(ui, setUi, ARCtrl_ISA_CompositeHeader__CompositeHeader_UpdateWithOA_Z4C0FE73C(model.AddBuildingBlockState.Header, ARCtrl_ISA_OntologyAnnotation__OntologyAnnotation_fromTerm_Static_Z5E0A7659(term))));
        }, ui_search, setUi_search, model, dispatch)) : empty())));
    }));
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    });
}

function BodyTerm_termOrUnit_switch(uiState, setUiState) {
    let elems;
    return createElement("div", createObj(Helpers_combineClasses("buttons", ofArray([["className", "has-addons"], ["style", {
        flexWrap: "nowrap",
        marginBottom: 0,
        marginRight: 1 + "rem",
    }], (elems = [createElement("a", createObj(Helpers_combineClasses("button", toList(delay(() => {
        const isActive = equals(uiState.BodyCellType, new BuildingBlock_BodyCellType(0, []));
        return append(isActive ? singleton_1(["className", "is-success"]) : empty(), delay(() => append(singleton_1(["onClick", (_arg) => {
            setUiState(new BuildingBlock_BuildingBlockUIState(uiState.DropdownIsActive, uiState.DropdownPage, new BuildingBlock_BodyCellType(0, [])));
        }]), delay(() => append(singleton_1(["className", join(" ", toList(delay(() => append(singleton_1("pr-2 pl-2 mb-0"), delay(() => (isActive ? singleton_1("is-selected") : empty()))))))]), delay(() => singleton_1(["children", "Term"])))))));
    }))))), createElement("a", createObj(Helpers_combineClasses("button", toList(delay(() => {
        const isActive_1 = equals(uiState.BodyCellType, new BuildingBlock_BodyCellType(1, []));
        return append(isActive_1 ? singleton_1(["className", "is-success"]) : empty(), delay(() => append(singleton_1(["onClick", (_arg_1) => {
            setUiState(new BuildingBlock_BuildingBlockUIState(uiState.DropdownIsActive, uiState.DropdownPage, new BuildingBlock_BodyCellType(1, [])));
        }]), delay(() => append(singleton_1(["className", join(" ", toList(delay(() => append(singleton_1("pr-2 pl-2 mb-0"), delay(() => (isActive_1 ? singleton_1("is-selected") : empty()))))))]), delay(() => singleton_1(["children", "Unit"])))))));
    })))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))));
}

function BodyTerm_isDirectedSearch_toggle(state, setState) {
    const elms_1 = singleton(createElement("a", createObj(Helpers_combineClasses("button", toList(delay(() => append(singleton_1(["title", "Toggle child search"]), delay(() => append(state ? singleton_1(["className", "is-success"]) : empty(), delay(() => append(singleton_1(["onClick", (_arg) => {
        setState(!state);
    }]), delay(() => {
        let elems_1, elms;
        return singleton_1((elems_1 = [(elms = singleton(createElement("i", {
            className: "fa-solid fa-diagram-project",
        })), createElement("span", {
            className: "icon",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))]));
    }))))))))))));
    return createElement("div", {
        className: "control",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    });
}

function BodyTerm_body_searchElement(bodyTerm_body_searchElementInputProps) {
    let elems_1;
    const dispatch = bodyTerm_body_searchElementInputProps.dispatch;
    const model = bodyTerm_body_searchElementInputProps.model;
    const setState = bodyTerm_body_searchElementInputProps.setState;
    const state = bodyTerm_body_searchElementInputProps.state;
    const inputId = bodyTerm_body_searchElementInputProps.inputId;
    const patternInput = useFeliz_React__React_useState_Static_1505(true);
    const state_isDirectedSearchMode = patternInput[0];
    const onChangeMsg = (isClicked, v) => {
        const updateSearchState = (msg) => {
            setState(new TermSearch_TermSearchUIState(true, true));
            dispatch(new Msg_1(0, [[fromSeconds(0.5), "msg", msg]]));
        };
        if (!isClicked) {
            dispatch(selectBody_1(ARCtrl_ISA_CompositeCell__CompositeCell_UpdateWithOA_Z4C0FE73C(model.AddBuildingBlockState.BodyCell, OntologyAnnotation.fromString(v))));
        }
        const matchValue = model.AddBuildingBlockState.Header.IsTermColumn;
        let matchResult, any_8, any_9;
        if (isClicked) {
            if (state_isDirectedSearchMode) {
                if (matchValue) {
                    if (v === "") {
                        matchResult = 0;
                    }
                    else if (v.length > 2) {
                        matchResult = 1;
                        any_8 = v;
                    }
                    else if (v.length > 2) {
                        matchResult = 2;
                        any_9 = v;
                    }
                    else {
                        matchResult = 3;
                    }
                }
                else if (v.length > 2) {
                    matchResult = 2;
                    any_9 = v;
                }
                else {
                    matchResult = 3;
                }
            }
            else if (v.length > 2) {
                matchResult = 2;
                any_9 = v;
            }
            else {
                matchResult = 3;
            }
        }
        else if (state_isDirectedSearchMode) {
            if (matchValue) {
                if (v.length > 2) {
                    matchResult = 1;
                    any_8 = v;
                }
                else if (v.length > 2) {
                    matchResult = 2;
                    any_9 = v;
                }
                else {
                    matchResult = 3;
                }
            }
            else if (v.length > 2) {
                matchResult = 2;
                any_9 = v;
            }
            else {
                matchResult = 3;
            }
        }
        else if (v.length > 2) {
            matchResult = 2;
            any_9 = v;
        }
        else {
            matchResult = 3;
        }
        switch (matchResult) {
            case 0: {
                updateSearchState(new Msg_1(9, [new BuildingBlock_Msg(7, [ARCtrl_ISA_OntologyAnnotation__OntologyAnnotation_ToTermMinimal(model.AddBuildingBlockState.Header.ToTerm()), new TermSearch_TermSearchUIController(state, setState)])]));
                break;
            }
            case 1: {
                updateSearchState(new Msg_1(9, [new BuildingBlock_Msg(6, [v, ARCtrl_ISA_OntologyAnnotation__OntologyAnnotation_ToTermMinimal(model.AddBuildingBlockState.Header.ToTerm()), new TermSearch_TermSearchUIController(state, setState)])]));
                break;
            }
            case 2: {
                updateSearchState(new Msg_1(9, [new BuildingBlock_Msg(5, [v, new TermSearch_TermSearchUIController(state, setState)])]));
                break;
            }
            case 3: {
                break;
            }
        }
    };
    return createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "has-addons"], ["style", {
        flexGrow: 1,
    }], (elems_1 = [BodyTerm_isDirectedSearch_toggle(state_isDirectedSearchMode, patternInput[1]), createElement("div", createObj(Helpers_combineClasses("control", toList(delay(() => append(singleton_1(["className", "is-expanded"]), delay(() => append((state_isDirectedSearchMode && !model.AddBuildingBlockState.Header.IsTermColumn) ? singleton_1(["title", "No parent term selected"]) : (((state_isDirectedSearchMode && hasVerifiedTermHeader(model.AddBuildingBlockState.Header)) && (model.AddBuildingBlockState.BodySearchText === "")) ? singleton_1(["title", "Double click to show all children"]) : empty()), delay(() => append(singleton_1(["style", createObj(toList(delay(() => ((state_isDirectedSearchMode && hasVerifiedTermHeader(model.AddBuildingBlockState.Header)) ? singleton_1(["boxShadow", (((2 + "px ") + 2) + "px ") + "#4cceb9"]) : empty()))))]), delay(() => {
        let elems;
        return singleton_1((elems = toList(delay(() => {
            const valueOrDefault = ARCtrl_ISA_CompositeCell__CompositeCell_ToTerm(model.AddBuildingBlockState.BodyCell).NameText;
            return singleton_1(basicTermSearchElement(inputId, curry2(onChangeMsg)(false), curry2(onChangeMsg)(true), hasVerifiedCell(model.AddBuildingBlockState.BodyCell), state, setState, valueOrDefault));
        })), ["children", Interop_reactApi.Children.toArray(Array.from(elems))]));
    })))))))))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

function BodyTerm_Main(uiState, setState, searchState, setSearchState, model, dispatch) {
    const elms = toList(delay(() => {
        let elems;
        return append(singleton_1(createElement("div", createObj(Helpers_combineClasses("field", ofArray([["style", {
            display: "flex",
            justifyContent: "space-between",
        }], (elems = [BodyTerm_termOrUnit_switch(uiState, setState), createElement(BodyTerm_body_searchElement, {
            inputId: "BuildingBlock_BodyInput",
            state: searchState,
            setState: setSearchState,
            model: model,
            dispatch: dispatch,
        })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))), delay(() => (searchState.SearchIsActive ? singleton_1(AutocompleteComponents_autocompleteDropdownComponent(model.AddBuildingBlockState.BodySearchResults, (term) => {
            dispatch(new Msg_1(9, [new BuildingBlock_Msg(9, [createCellFromUiStateAndOA(uiState, ARCtrl_ISA_OntologyAnnotation__OntologyAnnotation_fromTerm_Static_Z5E0A7659(term))])]));
        }, searchState, setSearchState, model, dispatch)) : empty())));
    }));
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

function add_button(ui, model, dispatch) {
    const state = model.AddBuildingBlockState;
    const elms = singleton(createElement("button", createObj(Helpers_combineClasses("button", toList(delay(() => {
        const header = state.Header;
        return append(isValidColumn(header) ? append(singleton_1(["className", "is-success"]), delay(() => singleton_1(["className", "is-active"]))) : append(singleton_1(["className", "is-danger"]), delay(() => singleton_1(["disabled", true]))), delay(() => append(singleton_1(["className", "is-fullwidth"]), delay(() => append(singleton_1(["onClick", (_arg) => {
            dispatch(new Msg_1(17, [new Msg_2(3, [CompositeColumn.create(header, [state.BodyCell])])]));
        }]), delay(() => singleton_1(["children", "Add Column"])))))));
    }))))));
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

function AdvancedSearch_modal_container(uiState, setUiState, model, dispatch) {
    const children = toList(delay(() => append(singleton_1(advancedSearchModal(model, AdvancedSearch_Model_get_BuildingBlockHeaderId(), "", dispatch, (term) => (new Msg_1(21, [[new Msg_1(9, [new BuildingBlock_Msg(0, [term.Name])]), selectHeader_1(uiState, setUiState, ARCtrl_ISA_CompositeHeader__CompositeHeader_UpdateWithOA_Z4C0FE73C(model.AddBuildingBlockState.Header, ARCtrl_ISA_OntologyAnnotation__OntologyAnnotation_fromTerm_Static_Z5E0A7659(term)))]])))), delay(() => singleton_1(advancedSearchModal(model, AdvancedSearch_Model_get_BuildingBlockBodyId(), "", dispatch, (term_1) => (new Msg_1(21, [[new Msg_1(9, [new BuildingBlock_Msg(4, [term_1.Name])]), new Msg_1(9, [new BuildingBlock_Msg(9, [createCellFromUiStateAndOA(uiState, ARCtrl_ISA_OntologyAnnotation__OntologyAnnotation_fromTerm_Static_Z5E0A7659(term_1))])])]]))))))));
    return createElement("span", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    });
}

function AdvancedSearch_links_container(bb_type, dispatch) {
    const children = toList(delay(() => {
        let elems;
        return append(!bb_type.IsFeaturedColumn ? singleton_1(createElement("p", createObj(Helpers_combineClasses("help", ofArray([["style", {
            display: "inline",
        }], (elems = [createElement("a", {
            onClick: (_arg) => {
                dispatch(new Msg_1(5, [new AdvancedSearch_Msg(0, [AdvancedSearch_Model_get_BuildingBlockHeaderId()])]));
            },
            children: "Use advanced search header",
        })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))) : empty(), delay(() => {
            let elems_1;
            return singleton_1(createElement("p", createObj(Helpers_combineClasses("help", ofArray([["style", {
                display: "inline",
                float: "right",
            }], (elems_1 = [createElement("a", {
                onClick: (_arg_1) => {
                    dispatch(new Msg_1(5, [new AdvancedSearch_Msg(0, [AdvancedSearch_Model_get_BuildingBlockBodyId()])]));
                },
                children: "Use advanced search body",
            })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))));
        }));
    }));
    return createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    });
}

export function Main(mainInputProps) {
    const dispatch = mainInputProps.dispatch;
    const model = mainInputProps.model;
    const patternInput = useReact_useState_FCFD9EF(BuildingBlock_BuildingBlockUIState_init);
    const state_bb = patternInput[0];
    const setState_bb = patternInput[1];
    const patternInput_1 = useReact_useState_FCFD9EF(TermSearch_TermSearchUIState_init);
    const patternInput_2 = useReact_useState_FCFD9EF(TermSearch_TermSearchUIState_init);
    return mainFunctionContainer(toList(delay(() => append(singleton_1(chooseBuildingBlock_element(state_bb, setState_bb, patternInput_1[0], patternInput_1[1], model, dispatch)), delay(() => append(model.AddBuildingBlockState.Header.IsTermColumn ? append(singleton_1(BodyTerm_Main(state_bb, setState_bb, patternInput_2[0], patternInput_2[1], model, dispatch)), delay(() => append(singleton_1(AdvancedSearch_modal_container(state_bb, setState_bb, model, dispatch)), delay(() => singleton_1(AdvancedSearch_links_container(model.AddBuildingBlockState.Header, dispatch)))))) : empty(), delay(() => singleton_1(add_button(state_bb, model, dispatch)))))))));
}

//# sourceMappingURL=SearchComponent.js.map
