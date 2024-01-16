import { AdvancedSearch_Model_init, AdvancedSearch_AdvancedSearchSubpages, AdvancedSearch_Model } from "../Model.js";
import { Cmd_none } from "../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { AdvancedSearch_Msg, Msg_$reflection, Msg, ApiMsg, ApiRequestMsg } from "../Messages.js";
import { empty as empty_1, sortBy, ofArray, cons, contains, map, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { empty, singleton as singleton_1, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { termAccessionUrlOfAccessionStr } from "../../Shared/URLs.js";
import { DOMAttr, HTMLAttr } from "../../../fable_modules/Fable.React.9.3.0/Fable.React.Props.fs.js";
import { createElement } from "react";
import React from "react";
import * as react from "react";
import { keyValueList } from "../../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { value as value_74 } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { TermTypes_Term_$reflection } from "../../Shared/TermTypes.js";
import { record_type, lambda_type, unit_type, int32_type, list_type, string_type, array_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { comparePrimitives, equals, curry2, stringHash, int32ToString, createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { rangeDouble } from "../../../fable_modules/fable-library.4.9.0/Range.js";
import { min, max } from "../../../fable_modules/fable-library.4.9.0/Double.js";
import { map as map_1, chunkBySize, collect } from "../../../fable_modules/fable-library.4.9.0/Array.js";
import { printf, toText } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { List_except } from "../../../fable_modules/fable-library.4.9.0/Seq2.js";
import { useFeliz_React__React_useState_Static_1505 } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { AdvancedSearchOptions } from "../../Shared/AdvancedSearchTypes.js";
import { loadingComponent } from "../Modals/Loading.js";
import { PropHelpers_createOnKey } from "../../../fable_modules/Feliz.2.7.0/Properties.fs.js";
import { key_enter } from "../../../fable_modules/Feliz.2.7.0/Key.fs.js";

export function update(advancedTermSearchMsg, currentState) {
    switch (advancedTermSearchMsg.tag) {
        case 0:
            return [new AdvancedSearch_Model(advancedTermSearchMsg.fields[0], currentState.AdvancedSearchOptions, currentState.AdvancedSearchTermResults, currentState.AdvancedTermSearchSubpage, !currentState.HasModalVisible, currentState.HasOntologyDropdownVisible, currentState.HasAdvancedSearchResultsLoading), Cmd_none()];
        case 1:
            return [new AdvancedSearch_Model(currentState.ModalId, currentState.AdvancedSearchOptions, currentState.AdvancedSearchTermResults, currentState.AdvancedTermSearchSubpage, currentState.HasModalVisible, !currentState.HasOntologyDropdownVisible, currentState.HasAdvancedSearchResultsLoading), Cmd_none()];
        case 4:
            return [new AdvancedSearch_Model(currentState.ModalId, advancedTermSearchMsg.fields[0], currentState.AdvancedSearchTermResults, currentState.AdvancedTermSearchSubpage, currentState.HasModalVisible, false, currentState.HasAdvancedSearchResultsLoading), Cmd_none()];
        case 5:
            return [new AdvancedSearch_Model(currentState.ModalId, currentState.AdvancedSearchOptions, currentState.AdvancedSearchTermResults, new AdvancedSearch_AdvancedSearchSubpages(1, []), currentState.HasModalVisible, currentState.HasOntologyDropdownVisible, true), singleton((dispatch) => {
                dispatch(new Msg(2, [new ApiMsg(0, [new ApiRequestMsg(3, [currentState.AdvancedSearchOptions])])]));
            })];
        case 3:
            return [AdvancedSearch_Model_init(), Cmd_none()];
        case 6:
            return [new AdvancedSearch_Model(currentState.ModalId, currentState.AdvancedSearchOptions, advancedTermSearchMsg.fields[0], new AdvancedSearch_AdvancedSearchSubpages(1, []), currentState.HasModalVisible, currentState.HasOntologyDropdownVisible, false), Cmd_none()];
        default:
            return [new AdvancedSearch_Model(currentState.ModalId, currentState.AdvancedSearchOptions, currentState.AdvancedSearchTermResults, advancedTermSearchMsg.fields[0], currentState.HasModalVisible, currentState.HasOntologyDropdownVisible, currentState.HasAdvancedSearchResultsLoading), Cmd_none()];
    }
}

export function createLinkOfAccession(accession) {
    const props = toList(delay(() => append(singleton_1(new HTMLAttr(94, [termAccessionUrlOfAccessionStr(accession)])), delay(() => singleton_1(new HTMLAttr(157, ["_Blank"]))))));
    return react.createElement("a", keyValueList(props, 1), accession);
}

function isValidAdancedSearchOptions(opt) {
    return (opt.TermName.length + opt.TermDefinition.length) > 0;
}

function ontologyDropdownItem(model, dispatch, ontOpt) {
    const str = (ontOpt != null) ? value_74(ontOpt).Name : "All Ontologies";
    return react.createElement("option", {
        tabIndex: 0,
        value: str,
    }, str);
}

class ResultsTable_TableModel extends Record {
    constructor(Data, ActiveDropdowns, ElementsPerPage, PageIndex, Dispatch, RelatedInputId, ResultHandler) {
        super();
        this.Data = Data;
        this.ActiveDropdowns = ActiveDropdowns;
        this.ElementsPerPage = (ElementsPerPage | 0);
        this.PageIndex = (PageIndex | 0);
        this.Dispatch = Dispatch;
        this.RelatedInputId = RelatedInputId;
        this.ResultHandler = ResultHandler;
    }
}

function ResultsTable_TableModel_$reflection() {
    return record_type("SidebarComponents.AdvancedSearch.ResultsTable.TableModel", [], ResultsTable_TableModel, () => [["Data", array_type(TermTypes_Term_$reflection())], ["ActiveDropdowns", list_type(string_type)], ["ElementsPerPage", int32_type], ["PageIndex", int32_type], ["Dispatch", lambda_type(Msg_$reflection(), unit_type)], ["RelatedInputId", string_type], ["ResultHandler", lambda_type(TermTypes_Term_$reflection(), Msg_$reflection())]]);
}

function ResultsTable_createPaginationLinkFromIndex(updatePageIndex, pageIndex, currentPageinationIndex) {
    const isActve = pageIndex === currentPageinationIndex;
    const children = singleton(createElement("a", createObj(Helpers_combineClasses("pagination-link", toList(delay(() => append(isActve ? singleton_1(["className", "is-current"]) : empty(), delay(() => append(singleton_1(["style", createObj(toList(delay(() => append(isActve ? singleton_1(["color", "white"]) : empty(), delay(() => append(singleton_1(["backgroundColor", "#1FC2A7"]), delay(() => singleton_1(["borderColor", "#1FC2A7"]))))))))]), delay(() => append(singleton_1(["onClick", (_arg) => {
        updatePageIndex(pageIndex);
    }]), delay(() => singleton_1(["children", int32ToString(pageIndex + 1)])))))))))))));
    return createElement("li", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    });
}

function ResultsTable_pageinateDynamic(updatePageIndex, currentPageinationIndex, pageCount) {
    return map((index) => ResultsTable_createPaginationLinkFromIndex(updatePageIndex, index, currentPageinationIndex), toList(rangeDouble(max(1, currentPageinationIndex - 2), 1, min(currentPageinationIndex + 2, pageCount - 1))));
}

function ResultsTable_createAdvancedTermSearchResultRows(state, setState) {
    let children_22;
    if (!(state.Data.length === 0)) {
        return collect((sugg) => {
            let elems_3, elems_5, children_18, elms_1;
            const id = toText(printf("isHidden_advanced_%s"))(sugg.Accession);
            return [createElement("tr", createObj(ofArray([["onClick", (e) => {
                e.stopPropagation();
                e.preventDefault();
                if (state.RelatedInputId !== "") {
                    const relInput = document.getElementById(state.RelatedInputId);
                    relInput.value = sugg.Name;
                }
                state.Dispatch(state.ResultHandler(sugg));
                state.Dispatch(new Msg(5, [new AdvancedSearch_Msg(3, [])]));
            }], ["tabIndex", 0], ["className", "suggestion"], (elems_3 = toList(delay(() => {
                let children_2;
                return append(singleton_1((children_2 = [react.createElement("b", {}, sugg.Name)], react.createElement("td", {}, ...children_2))), delay(() => {
                    let props_4;
                    return append(sugg.IsObsolete ? singleton_1((props_4 = [["style", {
                        color: "red",
                    }]], react.createElement("td", keyValueList(props_4, 1), "obsolete"))) : singleton_1(react.createElement("td", {})), delay(() => {
                        let props_10, children_10, children_8;
                        return append(singleton_1((props_10 = [new DOMAttr(40, [(e_1) => {
                            e_1.stopPropagation();
                        }]), ["style", {
                            fontWeight: "light",
                        }]], (children_10 = [(children_8 = [createLinkOfAccession(sugg.Accession)], react.createElement("small", {}, ...children_8))], react.createElement("td", keyValueList(props_10, 1), ...children_10)))), delay(() => {
                            let children_13, elems_2, elems_1, elms;
                            return singleton_1((children_13 = [createElement("div", createObj(Helpers_combineClasses("buttons", ofArray([["className", "is-right"], (elems_2 = [createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-small"], ["className", "is-black"], ["className", "is-inverted"], ["onClick", (e_2) => {
                                e_2.preventDefault();
                                e_2.stopPropagation();
                                if (contains(id, state.ActiveDropdowns, {
                                    Equals: (x, y) => (x === y),
                                    GetHashCode: stringHash,
                                })) {
                                    setState(new ResultsTable_TableModel(state.Data, List_except([id], state.ActiveDropdowns, {
                                        Equals: (x_1, y_1) => (x_1 === y_1),
                                        GetHashCode: stringHash,
                                    }), state.ElementsPerPage, state.PageIndex, state.Dispatch, state.RelatedInputId, state.ResultHandler));
                                }
                                else {
                                    setState(new ResultsTable_TableModel(state.Data, cons(id, state.ActiveDropdowns), state.ElementsPerPage, state.PageIndex, state.Dispatch, state.RelatedInputId, state.ResultHandler));
                                }
                            }], (elems_1 = [(elms = singleton(createElement("i", {
                                className: "fa-solid fa-chevron-down",
                            })), createElement("span", {
                                className: "icon",
                                children: Interop_reactApi.Children.toArray(Array.from(elms)),
                            }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))))], react.createElement("td", {}, ...children_13)));
                        }));
                    }));
                }));
            })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])]))), createElement("tr", createObj(ofArray([["onClick", (e_3) => {
                e_3.stopPropagation();
            }], ["id", id], ["className", "suggestion-details"], ["style", createObj(toList(delay(() => (contains(id, state.ActiveDropdowns, {
                Equals: (x_2, y_2) => (x_2 === y_2),
                GetHashCode: stringHash,
            }) ? singleton_1(["visibility", "visible"]) : singleton_1(["visibility", "collapse"])))))], (elems_5 = [(children_18 = [(elms_1 = ofArray([react.createElement("b", {}, "Definition: "), sugg.Description]), createElement("div", {
                className: "content",
                children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
            }))], react.createElement("td", {
                colSpan: 4,
            }, ...children_18))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))])])))];
        }, state.Data);
    }
    else {
        return [(children_22 = singleton(react.createElement("td", {}, "No terms found matching your input.")), createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children_22)),
        }))];
    }
}

function ResultsTable_paginatedTableComponent(resultsTable_paginatedTableComponentInputProps) {
    let elems, children_2, elems_2, elms;
    const data = resultsTable_paginatedTableComponentInputProps.data;
    const model = resultsTable_paginatedTableComponentInputProps.model;
    const patternInput = useFeliz_React__React_useState_Static_1505(data);
    const setState = patternInput[1];
    const handlerState = patternInput[0];
    const updatePageIndex = (model_1, ind) => {
        setState(new ResultsTable_TableModel(model_1.Data, model_1.ActiveDropdowns, model_1.ElementsPerPage, ind, model_1.Dispatch, model_1.RelatedInputId, model_1.ResultHandler));
    };
    if (data.Data.length > 0) {
        const currentPageinationIndex = handlerState.PageIndex | 0;
        const chunked = chunkBySize(data.ElementsPerPage, ResultsTable_createAdvancedTermSearchResultRows(handlerState, setState));
        const len = chunked.length | 0;
        const elms_1 = ofArray([createElement("table", createObj(Helpers_combineClasses("table", ofArray([["className", "is-fullwidth"], (elems = [react.createElement("thead", {}), (children_2 = ofArray(chunked[currentPageinationIndex]), react.createElement("tbody", {}, ...children_2))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))), createElement("nav", createObj(Helpers_combineClasses("pagination", ofArray([["className", "is-centered"], (elems_2 = [createElement("button", createObj(Helpers_combineClasses("pagination-previous", ofArray([["style", {
            cursor: "pointer",
        }], ["onClick", (_arg) => {
            updatePageIndex(handlerState, max(currentPageinationIndex - 1, 0));
        }], ["disabled", currentPageinationIndex === 0], ["children", "Prev"]])))), (elms = toList(delay(() => append(singleton_1(ResultsTable_createPaginationLinkFromIndex(curry2(updatePageIndex)(handlerState), 0, currentPageinationIndex)), delay(() => {
            let children_4;
            return append(((len > 5) && (currentPageinationIndex > 3)) ? singleton_1((children_4 = singleton(createElement("span", createObj(Helpers_combineClasses("pagination-ellipsis", singleton(["dangerouslySetInnerHTML", {
                __html: "&hellip;",
            }]))))), createElement("li", {
                children: Interop_reactApi.Children.toArray(Array.from(children_4)),
            }))) : empty(), delay(() => append(ResultsTable_pageinateDynamic(curry2(updatePageIndex)(handlerState), currentPageinationIndex, len - 1), delay(() => {
                let children_6;
                return append(((len > 5) && (currentPageinationIndex < (len - 4))) ? singleton_1((children_6 = singleton(createElement("span", createObj(Helpers_combineClasses("pagination-ellipsis", singleton(["dangerouslySetInnerHTML", {
                    __html: "&hellip;",
                }]))))), createElement("li", {
                    children: Interop_reactApi.Children.toArray(Array.from(children_6)),
                }))) : empty(), delay(() => ((len > 1) ? singleton_1(ResultsTable_createPaginationLinkFromIndex(curry2(updatePageIndex)(handlerState), len - 1, currentPageinationIndex)) : empty())));
            }))));
        })))), createElement("ul", {
            className: "pagination-list",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        })), createElement("button", createObj(Helpers_combineClasses("pagination-next", ofArray([["style", {
            cursor: "pointer",
        }], ["onClick", (_arg_1) => {
            const next = min(currentPageinationIndex + 1, len - 1) | 0;
            updatePageIndex(handlerState, next);
        }], ["disabled", currentPageinationIndex === (len - 1)], ["children", "Next"]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))))]);
        return createElement("div", {
            className: "container",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        });
    }
    else {
        return createElement("div", {});
    }
}

function keepObsoleteCheckradioElement(model, dispatch, keepObsolete, modalId) {
    const id = toText(printf("%s_%A_%A"))("keepObsolete_checkradio")(keepObsolete)(modalId);
    const elms = ofArray([createElement("input", createObj(cons(["type", "checkbox"], Helpers_combineClasses("is-checkradio", ofArray([["name", "keepObsolete_checkradio"], ["id", id], ["checked", model.AdvancedSearchState.AdvancedSearchOptions.KeepObsolete === keepObsolete], ["onChange", (ev) => {
        let bind$0040;
        const e = ev.target.checked;
        dispatch(new Msg(5, [new AdvancedSearch_Msg(4, [(bind$0040 = model.AdvancedSearchState.AdvancedSearchOptions, new AdvancedSearchOptions(bind$0040.OntologyName, bind$0040.TermName, bind$0040.TermDefinition, keepObsolete))])]));
    }]]))))), createElement("label", {
        htmlFor: id,
        children: keepObsolete ? "yes" : "no",
    })]);
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

function inputFormPage(modalId, model, dispatch) {
    let elms_2, elms_1, elms, value_10, elms_5, elms_4, elms_3, value_34, elms_8, elms_7, elms_6, elems_7, elms_9, children_10;
    const children_13 = ofArray([(elms_2 = ofArray([createElement("label", {
        className: "label",
        children: "Term name keywords:",
    }), (elms_1 = singleton((elms = singleton(createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", ofArray([["placeholder", "... search term name"], ["className", "is-small"], ["onChange", (ev) => {
        let bind$0040;
        dispatch(new Msg(5, [new AdvancedSearch_Msg(4, [(bind$0040 = model.AdvancedSearchState.AdvancedSearchOptions, new AdvancedSearchOptions(bind$0040.OntologyName, ev.target.value, bind$0040.TermDefinition, bind$0040.KeepObsolete))])]));
    }], (value_10 = model.AdvancedSearchState.AdvancedSearchOptions.TermName, ["ref", (e_1) => {
        if (!(e_1 == null) && !equals(e_1.value, value_10)) {
            e_1.value = value_10;
        }
    }]), ["onKeyDown", (e_2) => {
        if (e_2.which === 13) {
            e_2.preventDefault();
            e_2.stopPropagation();
            if (isValidAdancedSearchOptions(model.AdvancedSearchState.AdvancedSearchOptions)) {
                dispatch(new Msg(5, [new AdvancedSearch_Msg(5, [])]));
            }
        }
    }]])))))), createElement("div", {
        className: "control",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    }))]), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    })), (elms_5 = ofArray([createElement("label", {
        className: "label",
        children: "Term definition keywords:",
    }), (elms_4 = singleton((elms_3 = singleton(createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", ofArray([["placeholder", "... search term definition"], ["className", "is-small"], ["onChange", (ev_1) => {
        let bind$0040_1;
        dispatch(new Msg(5, [new AdvancedSearch_Msg(4, [(bind$0040_1 = model.AdvancedSearchState.AdvancedSearchOptions, new AdvancedSearchOptions(bind$0040_1.OntologyName, bind$0040_1.TermName, ev_1.target.value, bind$0040_1.KeepObsolete))])]));
    }], ["onKeyDown", (e_4) => {
        if (e_4.which === 13) {
            e_4.preventDefault();
            e_4.stopPropagation();
            if (isValidAdancedSearchOptions(model.AdvancedSearchState.AdvancedSearchOptions)) {
                dispatch(new Msg(5, [new AdvancedSearch_Msg(5, [])]));
            }
        }
    }], (value_34 = model.AdvancedSearchState.AdvancedSearchOptions.TermDefinition, ["ref", (e_5) => {
        if (!(e_5 == null) && !equals(e_5.value, value_34)) {
            e_5.value = value_34;
        }
    }])])))))), createElement("div", {
        className: "control",
        children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
    }))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_4)),
    }))]), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_5)),
    })), (elms_8 = ofArray([createElement("label", {
        className: "label",
        children: "Ontology",
    }), (elms_7 = singleton((elms_6 = singleton(createElement("select", createObj(toList(delay(() => append(singleton_1(["placeholder", "All Ontologies"]), delay(() => append((model.AdvancedSearchState.AdvancedSearchOptions.OntologyName != null) ? singleton_1(["value", value_74(model.AdvancedSearchState.AdvancedSearchOptions.OntologyName)]) : empty(), delay(() => append(singleton_1(["onChange", (ev_2) => {
        let bind$0040_2;
        const e_6 = ev_2.target.value;
        dispatch(new Msg(5, [new AdvancedSearch_Msg(4, [(bind$0040_2 = model.AdvancedSearchState.AdvancedSearchOptions, new AdvancedSearchOptions((e_6 === "All Ontologies") ? void 0 : e_6, bind$0040_2.TermName, bind$0040_2.TermDefinition, bind$0040_2.KeepObsolete))])]));
    }]), delay(() => {
        let elems_6;
        return singleton_1((elems_6 = toList(delay(() => append(singleton_1(ontologyDropdownItem(model, dispatch, void 0)), delay(() => map((ont) => ontologyDropdownItem(model, dispatch, ont), sortBy((o) => o.Name, ofArray(map_1((tuple) => tuple[1], model.PersistentStorageState.SearchableOntologies)), {
            Compare: comparePrimitives,
        })))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_6))]));
    }))))))))))), createElement("div", createObj(ofArray([["className", "select"], (elems_7 = [createElement("select", {
        children: Interop_reactApi.Children.toArray(Array.from(elms_6)),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_7))])]))))), createElement("div", {
        className: "control",
        children: Interop_reactApi.Children.toArray(Array.from(elms_7)),
    }))]), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_8)),
    })), (elms_9 = ofArray([createElement("label", {
        className: "label",
        children: "Keep obsolete terms",
    }), (children_10 = ofArray([keepObsoleteCheckradioElement(model, dispatch, true, modalId), keepObsoleteCheckradioElement(model, dispatch, false, modalId)]), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children_10)),
    }))]), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_9)),
    }))]);
    return createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children_13)),
    });
}

function resultsPage(relatedInputId, resultHandler, model, dispatch) {
    const elms = toList(delay(() => append(singleton_1(createElement("label", {
        className: "label",
        children: "Results:",
    })), delay(() => {
        if (equals(model.AdvancedSearchState.AdvancedTermSearchSubpage, new AdvancedSearch_AdvancedSearchSubpages(1, []))) {
            if (model.AdvancedSearchState.HasAdvancedSearchResultsLoading) {
                return singleton_1(createElement("div", {
                    style: {
                        width: 100 + "%",
                        display: "flex",
                        justifyContent: "center",
                    },
                    children: loadingComponent,
                }));
            }
            else {
                const init = new ResultsTable_TableModel(model.AdvancedSearchState.AdvancedSearchTermResults, empty_1(), 10, 0, dispatch, relatedInputId, resultHandler);
                return singleton_1(createElement(ResultsTable_paginatedTableComponent, {
                    model: model,
                    data: init,
                }));
            }
        }
        else {
            return empty();
        }
    }))));
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

export function advancedSearchModal(model, modalId, relatedInputId, dispatch, resultHandler) {
    return createElement("div", createObj(Helpers_combineClasses("modal", toList(delay(() => append((model.AdvancedSearchState.HasModalVisible && (model.AdvancedSearchState.ModalId === modalId)) ? singleton_1(["className", "is-active"]) : empty(), delay(() => append(singleton_1(["id", modalId]), delay(() => {
        let elems_8, elems_7, elems, elems_2, elms_3, elems_5;
        return singleton_1((elems_8 = [createElement("div", createObj(Helpers_combineClasses("modal-background", singleton(["onClick", (e) => {
            dispatch(new Msg(5, [new AdvancedSearch_Msg(3, [])]));
        }])))), createElement("div", createObj(Helpers_combineClasses("modal-card", ofArray([["style", {
            width: 90 + "%",
            maxWidth: 600 + "px",
            height: 80 + "%",
            maxHeight: 600 + "px",
        }], (elems_7 = [createElement("header", createObj(Helpers_combineClasses("modal-card-head", singleton((elems = [createElement("p", {
            className: "modal-card-title",
            children: "Advanced Search",
        }), createElement("button", createObj(Helpers_combineClasses("delete", singleton(["onClick", (_arg) => {
            dispatch(new Msg(5, [new AdvancedSearch_Msg(3, [])]));
        }]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))]))))), createElement("section", createObj(Helpers_combineClasses("modal-card-body", singleton((elems_2 = toList(delay(() => {
            let elms, value_27;
            return append(singleton_1((elms = singleton(createElement("p", createObj(Helpers_combineClasses("help", ofArray([["style", {
                textAlign: "justify",
            }], (value_27 = "Swate advanced search uses the Apache Lucene query parser syntax. Feel free to read the related Swate documentation [wip] for guidance on how to use it.", ["children", value_27])]))))), createElement("div", {
                className: "field",
                children: Interop_reactApi.Children.toArray(Array.from(elms)),
            }))), delay(() => ((model.AdvancedSearchState.AdvancedTermSearchSubpage.tag === 1) ? singleton_1(resultsPage(relatedInputId, resultHandler, model, dispatch)) : singleton_1(inputFormPage(modalId, model, dispatch)))));
        })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))]))))), (elms_3 = singleton(createElement("form", createObj(ofArray([["onSubmit", (e_1) => {
            e_1.preventDefault();
        }], ["onKeyDown", (ev) => {
            PropHelpers_createOnKey(key_enter, (k) => {
                k.preventDefault();
            }, ev);
        }], ["style", {
            width: 100 + "%",
        }], (elems_5 = toList(delay(() => {
            let elms_1;
            return append(!equals(model.AdvancedSearchState.AdvancedTermSearchSubpage, new AdvancedSearch_AdvancedSearchSubpages(0, [])) ? singleton_1((elms_1 = singleton(createElement("button", createObj(Helpers_combineClasses("button", ofArray([["className", "is-danger"], ["className", "is-fullwidth"], ["onClick", (e_2) => {
                e_2.stopPropagation();
                e_2.preventDefault();
                dispatch(new Msg(5, [new AdvancedSearch_Msg(2, [new AdvancedSearch_AdvancedSearchSubpages(0, [])])]));
            }], ["children", "Back"]]))))), createElement("div", {
                className: "level-item",
                children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
            }))) : empty(), delay(() => {
                let elms_2;
                return equals(model.AdvancedSearchState.AdvancedTermSearchSubpage, new AdvancedSearch_AdvancedSearchSubpages(0, [])) ? singleton_1((elms_2 = singleton(createElement("button", createObj(Helpers_combineClasses("button", toList(delay(() => append(isValidAdancedSearchOptions(model.AdvancedSearchState.AdvancedSearchOptions) ? append(singleton_1(["className", "is-success"]), delay(() => singleton_1(["className", "is-active"]))) : append(singleton_1(["className", "is-danger"]), delay(() => singleton_1(["disabled", true]))), delay(() => append(singleton_1(["className", "is-fullwidth"]), delay(() => append(singleton_1(["onClick", (e_3) => {
                    e_3.preventDefault();
                    e_3.stopPropagation();
                    dispatch(new Msg(5, [new AdvancedSearch_Msg(5, [])]));
                }]), delay(() => singleton_1(["children", "Start advanced search"]))))))))))))), createElement("div", {
                    className: "level-item",
                    children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
                }))) : empty();
            }));
        })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))])])))), createElement("footer", {
            className: "modal-card-foot",
            children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
        }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_7))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_8))]));
    })))))))));
}

//# sourceMappingURL=AdvancedSearch.js.map
