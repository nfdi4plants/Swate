import { length as length_1, append as append_2, contains, cons, singleton as singleton_1, empty, ofArray } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { toString, Record, Union } from "../../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, bool_type, list_type, string_type, option_type, int32_type, union_type } from "../../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { OntologyAnnotation_$reflection } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/OntologyAnnotation.fs.js";
import { Protocol_CuratedCommunityFilter, Protocol_CuratedCommunityFilter_$reflection } from "../../Model.js";
import { collect as collect_1, length, map as map_1, empty as empty_1, singleton, append, delay, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { createElement } from "react";
import React from "react";
import { compareArrays, stringHash, comparePrimitives, safeHash, createObj, equals } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { value as value_73 } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { except, List_distinct, List_except, Array_distinct } from "../../../../fable_modules/fable-library.4.9.0/Seq2.js";
import { append as append_1, sortBy, equalsWith, sortByDescending, map, collect } from "../../../../fable_modules/fable-library.4.9.0/Array.js";
import { SorensenDice_createBigrams } from "../../../Shared/Shared.js";
import { intersect, count, FSharpSet__get_Count } from "../../../../fable_modules/fable-library.4.9.0/Set.js";
import { join } from "../../../../fable_modules/fable-library.4.9.0/String.js";
import { toString as toString_1 } from "../../../../fable_modules/fable-library.4.9.0/Date.js";
import { Msg, Protocol_Msg } from "../../Messages.js";
import { useReact_useState_FCFD9EF } from "../../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { mainFunctionContainer } from "../../SidebarComponents/LayoutHelper.js";
import { Helpdesk_get_UrlTemplateTopic } from "../../../Shared/URLs.js";
import { defaultOf } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { rangeDouble } from "../../../../fable_modules/fable-library.4.9.0/Range.js";

const curatedOrganisationNames = ofArray(["dataplant", "nfdi4plants"]);

class SearchFields extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Name", "Organisation", "Authors"];
    }
}

function SearchFields_$reflection() {
    return union_type("Protocol.Component.SearchFields", [], SearchFields, () => [[], [], []]);
}

function SearchFields_ofFieldString_Z721C83C5(str) {
    const str_1 = str.toLocaleLowerCase();
    switch (str_1) {
        case "/o":
        case "/org":
            return new SearchFields(1, []);
        case "/a":
        case "/authors":
            return new SearchFields(2, []);
        case "/n":
        case "/reset":
        case "/e":
            return new SearchFields(0, []);
        default:
            return void 0;
    }
}

function SearchFields__get_toStr(this$) {
    switch (this$.tag) {
        case 1:
            return "/org";
        case 2:
            return "/auth";
        default:
            return "/name";
    }
}

function SearchFields__get_toNameRdb(this$) {
    switch (this$.tag) {
        case 1:
            return "organisation";
        case 2:
            return "authors";
        default:
            return "template name";
    }
}

function SearchFields_GetOfQuery_Z721C83C5(query) {
    return SearchFields_ofFieldString_Z721C83C5(query);
}

class ProtocolViewState extends Record {
    constructor(DisplayedProtDetailsId, ProtocolSearchQuery, ProtocolTagSearchQuery, ProtocolFilterTags, ProtocolFilterErTags, CuratedCommunityFilter, TagFilterIsAnd, Searchfield) {
        super();
        this.DisplayedProtDetailsId = DisplayedProtDetailsId;
        this.ProtocolSearchQuery = ProtocolSearchQuery;
        this.ProtocolTagSearchQuery = ProtocolTagSearchQuery;
        this.ProtocolFilterTags = ProtocolFilterTags;
        this.ProtocolFilterErTags = ProtocolFilterErTags;
        this.CuratedCommunityFilter = CuratedCommunityFilter;
        this.TagFilterIsAnd = TagFilterIsAnd;
        this.Searchfield = Searchfield;
    }
}

function ProtocolViewState_$reflection() {
    return record_type("Protocol.Component.ProtocolViewState", [], ProtocolViewState, () => [["DisplayedProtDetailsId", option_type(int32_type)], ["ProtocolSearchQuery", string_type], ["ProtocolTagSearchQuery", string_type], ["ProtocolFilterTags", list_type(OntologyAnnotation_$reflection())], ["ProtocolFilterErTags", list_type(OntologyAnnotation_$reflection())], ["CuratedCommunityFilter", Protocol_CuratedCommunityFilter_$reflection()], ["TagFilterIsAnd", bool_type], ["Searchfield", SearchFields_$reflection()]]);
}

function ProtocolViewState_init() {
    return new ProtocolViewState(void 0, "", "", empty(), empty(), new Protocol_CuratedCommunityFilter(0, []), true, new SearchFields(0, []));
}

function queryField(model, state, setState) {
    const elms_1 = toList(delay(() => append(singleton(createElement("label", {
        className: "label",
        children: `Search by ${SearchFields__get_toNameRdb(state.Searchfield)}`,
    })), delay(() => {
        const hasSearchAddon = !equals(state.Searchfield, new SearchFields(0, []));
        return singleton(createElement("div", createObj(Helpers_combineClasses("field", toList(delay(() => append(hasSearchAddon ? singleton(["className", "has-addons"]) : empty_1(), delay(() => {
            let elems_2;
            return singleton((elems_2 = toList(delay(() => {
                let elms;
                return append(hasSearchAddon ? singleton((elms = singleton_1(createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-static"], ["children", SearchFields__get_toStr(state.Searchfield)]]))))), createElement("div", {
                    className: "control",
                    children: Interop_reactApi.Children.toArray(Array.from(elms)),
                }))) : empty_1(), delay(() => {
                    let elems_1, value_22;
                    return singleton(createElement("div", createObj(Helpers_combineClasses("control", ofArray([["className", "has-icons-right"], (elems_1 = [createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", ofArray([["placeholder", `.. ${SearchFields__get_toNameRdb(state.Searchfield)}`], ["id", "template_searchfield_main"], ["className", "is-primary"], (value_22 = state.ProtocolSearchQuery, ["ref", (e) => {
                        if (!(e == null) && !equals(e.value, value_22)) {
                            e.value = value_22;
                        }
                    }]), ["onChange", (ev) => {
                        const query = ev.target.value;
                        if (query.indexOf("/") === 0) {
                            const searchField = SearchFields_GetOfQuery_Z721C83C5(query);
                            if (searchField != null) {
                                setState(new ProtocolViewState(state.DisplayedProtDetailsId, "", state.ProtocolTagSearchQuery, state.ProtocolFilterTags, state.ProtocolFilterErTags, state.CuratedCommunityFilter, state.TagFilterIsAnd, value_73(searchField)));
                            }
                        }
                        else {
                            setState(new ProtocolViewState(void 0, query, state.ProtocolTagSearchQuery, state.ProtocolFilterTags, state.ProtocolFilterErTags, state.CuratedCommunityFilter, state.TagFilterIsAnd, state.Searchfield));
                        }
                    }]]))))), createElement("span", createObj(Helpers_combineClasses("icon", ofArray([["className", "is-small"], ["className", "is-right"], ["children", createElement("i", {
                        className: "fa-solid fa-search",
                    })]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))));
                }));
            })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))]));
        }))))))));
    }))));
    return createElement("div", {
        className: "column",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    });
}

function tagQueryField(model, state, setState) {
    let elems_4, value_14, elems_3;
    let allTags;
    const array_2 = Array_distinct(collect((x) => x.Tags, model.ProtocolState.ProtocolsAll), {
        Equals: equals,
        GetHashCode: safeHash,
    });
    allTags = array_2.filter((x_2) => !contains(x_2, state.ProtocolFilterTags, {
        Equals: equals,
        GetHashCode: safeHash,
    }));
    let allErTags;
    const array_5 = Array_distinct(collect((x_4) => x_4.EndpointRepositories, model.ProtocolState.ProtocolsAll), {
        Equals: equals,
        GetHashCode: safeHash,
    });
    allErTags = array_5.filter((x_6) => !contains(x_6, state.ProtocolFilterErTags, {
        Equals: equals,
        GetHashCode: safeHash,
    }));
    let patternInput;
    if (state.ProtocolTagSearchQuery !== "") {
        const queryBigram = SorensenDice_createBigrams(state.ProtocolTagSearchQuery);
        const getMatchingTags = (allTags_1) => {
            let array_7;
            return map((tuple_1) => tuple_1[1], sortByDescending((tuple) => tuple[0], (array_7 = map((x_8) => {
                let x_9, y_5, matchValue, matchValue_1;
                return [(x_9 = queryBigram, (y_5 = SorensenDice_createBigrams(x_8.NameText), (matchValue = (FSharpSet__get_Count(x_9) | 0), (matchValue_1 = (FSharpSet__get_Count(y_5) | 0), (matchValue === 0) ? ((matchValue_1 === 0) ? 1 : ((2 * count(intersect(x_9, y_5))) / (matchValue + matchValue_1))) : ((2 * count(intersect(x_9, y_5))) / (matchValue + matchValue_1)))))), x_8];
            }, allTags_1), array_7.filter((x_10) => {
                if (x_10[0] >= 0.3) {
                    return true;
                }
                else {
                    return x_10[1].TermAccessionShort === state.ProtocolTagSearchQuery;
                }
            })), {
                Compare: comparePrimitives,
            }));
        };
        patternInput = [getMatchingTags(allTags), getMatchingTags(allErTags)];
    }
    else {
        patternInput = [[], []];
    }
    const hitTagList = patternInput[0];
    const hitErTagList = patternInput[1];
    const elms_2 = ofArray([createElement("label", {
        className: "label",
        children: "Search for tags",
    }), createElement("div", createObj(Helpers_combineClasses("control", ofArray([["className", "has-icons-right"], (elems_4 = [createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", ofArray([["placeholder", ".. protocol tag"], ["className", "is-primary"], (value_14 = state.ProtocolTagSearchQuery, ["ref", (e) => {
        if (!(e == null) && !equals(e.value, value_14)) {
            e.value = value_14;
        }
    }]), ["onChange", (ev) => {
        setState(new ProtocolViewState(state.DisplayedProtDetailsId, state.ProtocolSearchQuery, ev.target.value, state.ProtocolFilterTags, state.ProtocolFilterErTags, state.CuratedCommunityFilter, state.TagFilterIsAnd, state.Searchfield));
    }]]))))), createElement("span", createObj(Helpers_combineClasses("icon", ofArray([["className", "is-small"], ["className", "is-right"], ["children", createElement("i", {
        className: "fa-solid fa-search",
    })]])))), createElement("div", createObj(Helpers_combineClasses("box", ofArray([["style", createObj(toList(delay(() => append(singleton(["position", "absolute"]), delay(() => append(singleton(["width", 100 + "%"]), delay(() => append(singleton(["zIndex", 10]), delay(() => (((hitTagList.length === 0) && (hitErTagList.length === 0)) ? singleton(["display", "none"]) : empty_1()))))))))))], (elems_3 = toList(delay(() => append(!equalsWith(equals, hitErTagList, []) ? append(singleton(createElement("label", {
        className: "label",
        children: "Endpoint Repositories",
    })), delay(() => {
        let elms;
        return singleton((elms = toList(delay(() => map_1((tagSuggestion) => createElement("span", createObj(Helpers_combineClasses("tag", ofArray([["className", "clickableTag"], ["className", "is-link"], ["onClick", (_arg) => {
            setState(new ProtocolViewState(void 0, state.ProtocolSearchQuery, "", state.ProtocolFilterTags, cons(tagSuggestion, state.ProtocolFilterErTags), state.CuratedCommunityFilter, state.TagFilterIsAnd, state.Searchfield));
        }], ["title", tagSuggestion.TermAccessionShort], ["children", tagSuggestion.NameText]])))), hitErTagList))), createElement("div", {
            className: "tags",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        })));
    })) : empty_1(), delay(() => (!equalsWith(equals, hitTagList, []) ? append(singleton(createElement("label", {
        className: "label",
        children: "Tags",
    })), delay(() => {
        let elms_1;
        return singleton((elms_1 = toList(delay(() => map_1((tagSuggestion_1) => createElement("span", createObj(Helpers_combineClasses("tag", ofArray([["className", "clickableTag"], ["className", "is-info"], ["onClick", (_arg_1) => {
            setState(new ProtocolViewState(void 0, state.ProtocolSearchQuery, "", cons(tagSuggestion_1, state.ProtocolFilterTags), state.ProtocolFilterErTags, state.CuratedCommunityFilter, state.TagFilterIsAnd, state.Searchfield));
        }], ["title", tagSuggestion_1.TermAccessionShort], ["children", tagSuggestion_1.NameText]])))), hitTagList))), createElement("div", {
            className: "tags",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        })));
    })) : empty_1()))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))]);
    return createElement("div", {
        className: "column",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    });
}

function tagDisplayField(model, state, setState) {
    let elems_7, elms_2, elems_4;
    return createElement("div", createObj(Helpers_combineClasses("columns", ofArray([["className", "is-mobile"], (elems_7 = [(elms_2 = singleton_1(createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "is-grouped-multiline"], (elems_4 = toList(delay(() => append(map_1((selectedTag) => {
        let elems;
        const elms = singleton_1(createElement("div", createObj(Helpers_combineClasses("tags", ofArray([["className", "has-addons"], (elems = [createElement("span", createObj(Helpers_combineClasses("tag", ofArray([["className", "is-link"], ["style", {
            borderWidth: 0,
        }], ["children", selectedTag.NameText]])))), createElement("button", createObj(Helpers_combineClasses("delete", ofArray([["className", "clickableTagDelete"], ["onClick", (_arg) => {
            setState(new ProtocolViewState(state.DisplayedProtDetailsId, state.ProtocolSearchQuery, state.ProtocolTagSearchQuery, state.ProtocolFilterTags, List_except([selectedTag], state.ProtocolFilterErTags, {
                Equals: equals,
                GetHashCode: safeHash,
            }), state.CuratedCommunityFilter, state.TagFilterIsAnd, state.Searchfield));
        }]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))));
        return createElement("div", {
            className: "control",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        });
    }, state.ProtocolFilterErTags), delay(() => map_1((selectedTag_1) => {
        let elems_2;
        const elms_1 = singleton_1(createElement("div", createObj(Helpers_combineClasses("tags", ofArray([["className", "has-addons"], (elems_2 = [createElement("span", createObj(Helpers_combineClasses("tag", ofArray([["className", "is-info"], ["style", {
            borderWidth: 0,
        }], ["children", selectedTag_1.NameText]])))), createElement("button", createObj(Helpers_combineClasses("delete", ofArray([["className", "clickableTagDelete"], ["onClick", (_arg_1) => {
            setState(new ProtocolViewState(state.DisplayedProtDetailsId, state.ProtocolSearchQuery, state.ProtocolTagSearchQuery, List_except([selectedTag_1], state.ProtocolFilterTags, {
                Equals: equals,
                GetHashCode: safeHash,
            }), state.ProtocolFilterErTags, state.CuratedCommunityFilter, state.TagFilterIsAnd, state.Searchfield));
        }]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])))));
        return createElement("div", {
            className: "control",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        });
    }, state.ProtocolFilterTags))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))), createElement("div", {
        className: "column",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    })), createElement("div", createObj(Helpers_combineClasses("column", ofArray([["className", "is-narrow"], ["title", state.TagFilterIsAnd ? "Templates contain all tags." : "Templates contain at least one tag."], ["children", createElement("input", createObj(cons(["type", "checkbox"], Helpers_combineClasses("switch", ofArray([["className", "is-dark"], ["style", {
        userSelect: "none",
    }], ["className", "is-outlined"], ["className", "is-small"], ["id", "switch-2"], ["checked", state.TagFilterIsAnd], ["onChange", (ev) => {
        const e = ev.target.checked;
        setState(new ProtocolViewState(state.DisplayedProtDetailsId, state.ProtocolSearchQuery, state.ProtocolTagSearchQuery, state.ProtocolFilterTags, state.ProtocolFilterErTags, state.CuratedCommunityFilter, !state.TagFilterIsAnd, state.Searchfield));
    }], ["children", state.TagFilterIsAnd ? createElement("b", {
        children: ["And"],
    }) : createElement("b", {
        children: ["Or"],
    })]])))))]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_7))])]))));
}

function fileSortElements(model, state, setState) {
    let elems_1;
    return createElement("div", createObj(ofArray([["style", {
        marginBottom: 0.75 + "rem",
    }], (elems_1 = toList(delay(() => {
        let elems;
        return append(singleton(createElement("div", createObj(Helpers_combineClasses("columns", ofArray([["className", "is-mobile"], ["style", {
            marginBottom: 0,
        }], (elems = [queryField(model, state, setState), tagQueryField(model, state, setState)], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))), delay(() => ((!equals(state.ProtocolFilterErTags, empty()) ? true : !equals(state.ProtocolFilterTags, empty())) ? singleton(tagDisplayField(model, state, setState)) : empty_1())));
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])));
}

const curatedTag = createElement("span", createObj(Helpers_combineClasses("tag", ofArray([["children", "curated"], ["className", "is-success"]]))));

const communitytag = createElement("span", createObj(Helpers_combineClasses("tag", ofArray([["children", "community"], ["className", "is-warning"]]))));

const curatedCommunityTag = createElement("span", createObj(Helpers_combineClasses("tag", ofArray([["style", {
    background: "linear-gradient(90deg, rgba(31,194,167,1) 50%, rgba(255,192,0,1) 50%)",
}], ["className", "is-success"], (() => {
    const elems = [createElement("span", {
        style: {
            marginRight: 0.75 + "em",
        },
        children: "cur",
    }), createElement("span", {
        style: {
            marginLeft: 0.75 + "em",
            color: "rgba(0, 0, 0, 0.7)",
        },
        children: "com",
    })];
    return ["children", Interop_reactApi.Children.toArray(Array.from(elems))];
})()]))));

export function createAuthorStringHelper(author) {
    return `${author.FirstName} ${(author.MidInitials != null) ? value_73(author.MidInitials) : ""} ${author.LastName}`;
}

export function createAuthorsStringHelper(authors) {
    return join(", ", map(createAuthorStringHelper, authors));
}

function protocolElement(i, template, model, state, dispatch, setState) {
    let elems_1, elms, children_15, elems_5, elems_4, children_11, children_5, children_1, children_3, children_9, children_7, elms_1, elms_2;
    let isActive;
    const matchValue = state.DisplayedProtDetailsId;
    let matchResult, id_1;
    if (matchValue != null) {
        if (matchValue === i) {
            matchResult = 0;
            id_1 = matchValue;
        }
        else {
            matchResult = 1;
        }
    }
    else {
        matchResult = 1;
    }
    switch (matchResult) {
        case 0: {
            isActive = true;
            break;
        }
        default:
            isActive = false;
    }
    return ofArray([createElement("tr", createObj(ofArray([["key", `${i}_${template.Id}`], ["className", join(" ", toList(delay(() => append(singleton("nonSelectText"), delay(() => (isActive ? singleton("hoverTableEle") : empty_1()))))))], ["style", {
        cursor: "pointer",
        userSelect: "none",
        color: "white",
    }], ["onClick", (e) => {
        e.preventDefault();
        setState(new ProtocolViewState(isActive ? void 0 : i, state.ProtocolSearchQuery, state.ProtocolTagSearchQuery, state.ProtocolFilterTags, state.ProtocolFilterErTags, state.CuratedCommunityFilter, state.TagFilterIsAnd, state.Searchfield));
    }], (elems_1 = [createElement("td", {
        children: [template.Name],
    }), createElement("td", {
        children: [contains(toString(template.Organisation).toLocaleLowerCase(), curatedOrganisationNames, {
            Equals: (x, y) => (x === y),
            GetHashCode: stringHash,
        }) ? curatedTag : communitytag],
    }), createElement("td", {
        style: {
            textAlign: "center",
            verticalAlign: "middle",
        },
        children: template.Version,
    }), createElement("td", {
        children: [(elms = singleton_1(createElement("i", {
            className: "fa-solid fa-chevron-down",
        })), createElement("span", {
            className: "icon",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        }))],
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))), (children_15 = singleton_1(createElement("td", createObj(ofArray([["style", createObj(toList(delay(() => append(singleton(["padding", 0]), delay(() => (isActive ? singleton(["borderBottom", (((2 + "px ") + "solid") + " ") + "black"]) : singleton(["display", "none"])))))))], ["colSpan", 4], (elems_5 = [createElement("div", createObj(Helpers_combineClasses("box", ofArray([["style", {
        borderRadius: 0,
    }], (elems_4 = [(children_11 = ofArray([createElement("div", {
        children: [template.Description],
    }), (children_5 = ofArray([(children_1 = ofArray([createElement("b", {
        children: ["Author: "],
    }), createElement("span", {
        children: [createAuthorsStringHelper(template.Authors)],
    })]), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children_1)),
    })), (children_3 = ofArray([createElement("b", {
        children: ["Created: "],
    }), createElement("span", {
        children: [toString_1(template.LastUpdated, "yyyy/MM/dd")],
    })]), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children_3)),
    }))]), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children_5)),
    })), (children_9 = singleton_1((children_7 = ofArray([createElement("b", {
        children: ["Organisation: "],
    }), createElement("span", {
        children: [toString(template.Organisation)],
    })]), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children_7)),
    }))), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children_9)),
    }))]), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children_11)),
    })), (elms_1 = toList(delay(() => map_1((tag) => createElement("span", createObj(Helpers_combineClasses("tag", ofArray([["className", "is-link"], ["children", tag.NameText], ["title", tag.TermAccessionShort]])))), template.EndpointRepositories))), createElement("div", {
        className: "tags",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), (elms_2 = toList(delay(() => map_1((tag_1) => createElement("span", createObj(Helpers_combineClasses("tag", ofArray([["className", "is-info"], ["children", tag_1.NameText], ["title", tag_1.TermAccessionShort]])))), template.Tags))), createElement("div", {
        className: "tags",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    })), createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", (_arg) => {
        dispatch(new Msg(10, [new Protocol_Msg(5, [template])]));
    }], ["className", "is-fullwidth"], ["className", "is-success"], ["children", "select"]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))])])))), createElement("tr", {
        children: Interop_reactApi.Children.toArray(Array.from(children_15)),
    }))]);
}

function curatedCommunityFilterDropdownItem(filter, child, state, setState) {
    return createElement("a", createObj(Helpers_combineClasses("dropdown-item", ofArray([["onClick", (e) => {
        e.preventDefault();
        setState(new ProtocolViewState(state.DisplayedProtDetailsId, state.ProtocolSearchQuery, state.ProtocolTagSearchQuery, state.ProtocolFilterTags, state.ProtocolFilterErTags, filter, state.TagFilterIsAnd, state.Searchfield));
    }], ["children", child]]))));
}

function curatedCommunityFilterElement(state, setState) {
    let elems_4, elms, matchValue, elms_1;
    return createElement("div", createObj(Helpers_combineClasses("dropdown", ofArray([["className", "is-hoverable"], (elems_4 = [(elms = singleton_1(createElement("button", createObj(Helpers_combineClasses("button", ofArray([["className", "is-small"], ["className", "is-outlined"], ["className", "is-white"], ["style", {
        padding: 0,
    }], ["children", (matchValue = state.CuratedCommunityFilter, (matchValue.tag === 2) ? communitytag : ((matchValue.tag === 1) ? curatedTag : curatedCommunityTag))]]))))), createElement("div", {
        className: "dropdown-trigger",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), createElement("div", createObj(Helpers_combineClasses("dropdown-menu", ofArray([["style", {
        minWidth: "unset",
        fontWeight: "normal",
    }], ["children", (elms_1 = ofArray([curatedCommunityFilterDropdownItem(new Protocol_CuratedCommunityFilter(0, []), curatedCommunityTag, state, setState), curatedCommunityFilterDropdownItem(new Protocol_CuratedCommunityFilter(1, []), curatedTag, state, setState), curatedCommunityFilterDropdownItem(new Protocol_CuratedCommunityFilter(2, []), communitytag, state, setState)]), createElement("div", {
        className: "dropdown-content",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    }))]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))));
}

export function ProtocolContainer(protocolContainerInputProps) {
    let protocol, protocol_2, protocol_1, matchValue_4, query, queryBigram, createScore, array_2, elms_3, elms, elms_2, elms_1, children_7, children_1, children_3, children_5, elems_4, children_16, children_14, children_12, children_18;
    const dispatch = protocolContainerInputProps.dispatch;
    const model = protocolContainerInputProps.model;
    const patternInput = useReact_useState_FCFD9EF(ProtocolViewState_init);
    const state = patternInput[0];
    const setState = patternInput[1];
    const sortedTable = sortBy((template_1) => [template_1.Name, template_1.Organisation], (protocol = ((protocol_2 = ((protocol_1 = model.ProtocolState.ProtocolsAll, (!equals(state.ProtocolFilterTags, empty()) ? true : !equals(state.ProtocolFilterErTags, empty())) ? protocol_1.filter((x_2) => {
        const tags = Array_distinct(append_1(x_2.Tags, x_2.EndpointRepositories), {
            Equals: equals,
            GetHashCode: safeHash,
        });
        const filterTags = List_distinct(append_2(state.ProtocolFilterTags, state.ProtocolFilterErTags), {
            Equals: equals,
            GetHashCode: safeHash,
        });
        const filteredTags = except(filterTags, tags, {
            Equals: equals,
            GetHashCode: safeHash,
        });
        if (state.TagFilterIsAnd) {
            return length(filteredTags) === (tags.length - length_1(filterTags));
        }
        else {
            return length(filteredTags) < tags.length;
        }
    }) : protocol_1)), (matchValue_4 = state.CuratedCommunityFilter, (matchValue_4.tag === 1) ? protocol_2.filter((x_6) => contains(toString(x_6.Organisation).toLocaleLowerCase(), curatedOrganisationNames, {
        Equals: (x_7, y_6) => (x_7 === y_6),
        GetHashCode: stringHash,
    })) : ((matchValue_4.tag === 2) ? protocol_2.filter((x_8) => !contains(toString(x_8.Organisation).toLocaleLowerCase(), curatedOrganisationNames, {
        Equals: (x_9, y_7) => (x_9 === y_7),
        GetHashCode: stringHash,
    })) : protocol_2)))), (query = state.ProtocolSearchQuery.trim(), ((query !== "") && !(query.indexOf("/") === 0)) ? ((queryBigram = SorensenDice_createBigrams(query), (createScore = ((str) => {
        const x = queryBigram;
        const y_1 = SorensenDice_createBigrams(str);
        const matchValue = FSharpSet__get_Count(x) | 0;
        const matchValue_1 = FSharpSet__get_Count(y_1) | 0;
        let matchResult, xCount, yCount;
        if (matchValue === 0) {
            if (matchValue_1 === 0) {
                matchResult = 0;
            }
            else {
                matchResult = 1;
                xCount = matchValue;
                yCount = matchValue_1;
            }
        }
        else {
            matchResult = 1;
            xCount = matchValue;
            yCount = matchValue_1;
        }
        switch (matchResult) {
            case 0:
                return 1;
            default:
                return (2 * count(intersect(x, y_1))) / (xCount + yCount);
        }
    }), map((tuple_1) => tuple_1[1], sortByDescending((tuple) => tuple[0], (array_2 = map((template) => {
        let matchValue_3, query$0027, scores, array;
        return [(matchValue_3 = state.Searchfield, (matchValue_3.tag === 1) ? createScore(toString(template.Organisation)) : ((matchValue_3.tag === 2) ? ((query$0027 = query.toLocaleLowerCase(), (scores = ((array = template.Authors, array.filter((author) => {
            if (createAuthorStringHelper(author).toLocaleLowerCase().indexOf(query$0027) >= 0) {
                return true;
            }
            else if (author.ORCID != null) {
                return value_73(author.ORCID) === query;
            }
            else {
                return false;
            }
        }))), (scores.length === 0) ? 0 : 1))) : createScore(template.Name))), template];
    }, protocol), array_2.filter((tupledArg) => (tupledArg[0] > 0.1))), {
        Compare: comparePrimitives,
    }))))) : protocol)), {
        Compare: compareArrays,
    });
    return mainFunctionContainer([(elms_3 = ofArray([(elms = ofArray([createElement("b", {
        children: ["Search for templates."],
    }), createElement("span", {
        children: [" For more information you can look "],
    }), createElement("a", {
        href: "https://nfdi4plants.org/nfdi4plants.knowledgebase/docs/implementation/SwateManual/index.html",
        target: "_Blank",
        children: "here",
    }), createElement("span", {
        children: [". If you find any problems with a template or have other suggestions you can contact us "],
    }), createElement("a", {
        href: Helpdesk_get_UrlTemplateTopic(),
        target: "_Blank",
        children: "here",
    }), createElement("span", {
        children: ["."],
    })]), createElement("p", {
        className: "help",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), (elms_2 = ofArray([createElement("span", {
        children: ["You can search by template name, organisation and authors. Just type:"],
    }), (elms_1 = singleton_1((children_7 = ofArray([(children_1 = ofArray([createElement("code", {
        children: ["/a"],
    }), createElement("span", {
        children: [" to search authors."],
    })]), createElement("li", {
        children: Interop_reactApi.Children.toArray(Array.from(children_1)),
    })), (children_3 = ofArray([createElement("code", {
        children: ["/o"],
    }), createElement("span", {
        children: [" to search organisations."],
    })]), createElement("li", {
        children: Interop_reactApi.Children.toArray(Array.from(children_3)),
    })), (children_5 = ofArray([createElement("code", {
        children: ["/n"],
    }), createElement("span", {
        children: [" to search template names."],
    })]), createElement("li", {
        children: Interop_reactApi.Children.toArray(Array.from(children_5)),
    }))]), createElement("ul", {
        children: Interop_reactApi.Children.toArray(Array.from(children_7)),
    }))), createElement("div", {
        className: "content",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    }))]), createElement("p", {
        className: "help",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    }))]), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
    })), fileSortElements(model, state, setState), createElement("table", createObj(Helpers_combineClasses("table", ofArray([["className", "is-fullwidth"], ["className", "is-striped"], (elems_4 = [(children_16 = singleton_1((children_14 = ofArray([createElement("th", {
        children: ["Template Name"],
    }), (children_12 = singleton_1(curatedCommunityFilterElement(state, setState)), createElement("th", {
        children: Interop_reactApi.Children.toArray(Array.from(children_12)),
    })), createElement("th", {
        children: ["Template Version"],
    }), createElement("th", {
        children: [defaultOf()],
    })]), createElement("tr", {
        children: Interop_reactApi.Children.toArray(Array.from(children_14)),
    }))), createElement("thead", {
        children: Interop_reactApi.Children.toArray(Array.from(children_16)),
    })), (children_18 = toList(delay(() => collect_1((i) => protocolElement(i, sortedTable[i], model, state, dispatch, setState), rangeDouble(0, 1, sortedTable.length - 1)))), createElement("tbody", {
        children: Interop_reactApi.Children.toArray(Array.from(children_18)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))]);
}

//# sourceMappingURL=ProtocolSearchViewComponent.js.map
