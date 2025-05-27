import { Record, Union } from "../fable_modules/fable-library-ts.4.19.0/Types.js";
import { array_type, record_type, bool_type, class_type, union_type, TypeInfo } from "../fable_modules/fable-library-ts.4.19.0/Reflection.js";
import { Option_whereNot } from "../../../Shared/Extensions.fs.js";
import { isNullOrEmpty, join, printf, toText, isNullOrWhiteSpace } from "../fable_modules/fable-library-ts.4.19.0/String.js";
import { bind, map as map_2, unwrap, toArray as toArray_1, some, defaultArg, value as value_66, Option } from "../fable_modules/fable-library-ts.4.19.0/Option.js";
import { Term } from "../../../Shared/Database.fs.js";
import { IDisposable, comparePrimitives, round, createObj, int32ToString, equals, disposeSafe, getEnumerator, IEquatable } from "../fable_modules/fable-library-ts.4.19.0/Util.js";
import { float64, int32 } from "../fable_modules/fable-library-ts.4.19.0/Int32.js";
import { getSubArray, map as map_1, setItem } from "../fable_modules/fable-library-ts.4.19.0/Array.js";
import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../fable_modules/Fable.Promise.2.2.2/Promise.fs.js";
import { promise } from "../fable_modules/Fable.Promise.2.2.2/PromiseImpl.fs.js";
import { collect, iterate, empty, singleton, append, toList, map, delay as delay_4, toArray } from "../fable_modules/fable-library-ts.4.19.0/Seq.js";
import { rangeDouble } from "../fable_modules/fable-library-ts.4.19.0/Range.js";
import { startAsPromise } from "../fable_modules/fable-library-ts.4.19.0/Async.js";
import { TIBApi, SwateApi } from "../Util/Api.fs.js";
import { TermQuery_create_22AB55AC } from "../../../Shared/DTOs/TermQuery.fs.js";
import { ParentTermQueryResults, ParentTermQuery_create_3B406CA4 } from "../../../Shared/DTOs/ParentTermQuery.fs.js";
import { AdvancedSearchQuery_init, AdvancedSearchQuery } from "../../../Shared/DTOs/AdvancedSearch.fs.js";
import { useState, ReactElement, createElement } from "react";
import React from "react";
import * as react from "react";
import { reactApi } from "../fable_modules/Feliz.2.8.0/./Interop.fs.js";
import { IReactProperty } from "../fable_modules/Feliz.2.8.0/Types.fs.js";
import { Components_DeleteButton_789B7C0F, Components_CollapseButton_Z762A16CE } from "../GenericComponents/GenericComponents.fs.js";
import { empty as empty_1, FSharpList, singleton as singleton_1, ofArray } from "../fable_modules/fable-library-ts.4.19.0/List.js";
import { FSharpSet__Remove, FSharpSet__Add, empty as empty_2, FSharpSet, FSharpSet__get_IsEmpty } from "../fable_modules/fable-library-ts.4.19.0/Set.js";
import { defaultOf } from "../fable_modules/Feliz.2.8.0/../../TermSearch/../fable_modules/fable-library-ts.4.19.0/Util.js";
import { Feliz_prop__prop_testid_Static_Z721C83C5 } from "../Util/Extensions.fs.js";
import { PropHelpers_createOnKey } from "../fable_modules/Feliz.2.8.0/./Properties.fs.js";
import { key_escape, key_enter } from "../fable_modules/Feliz.2.8.0/Key.fs.js";
import { max, min } from "../fable_modules/fable-library-ts.4.19.0/Double.js";
import { IRefValue$1 } from "../fable_modules/Fable.React.Types.18.4.0/Fable.React.fs.js";
import { Impl_createRemoveOptions, Impl_adjustPassive, Impl_defaultPassive } from "../Util/./React.useListener.fs.js";
import { createDisposable, useCallbackRef } from "../fable_modules/Feliz.2.8.0/./Internal.fs.js";
import { useEffect } from "../fable_modules/Feliz.2.8.0/./ReactInterop.js";
import { Helpers_combineClasses } from "../fable_modules/Feliz.DaisyUI.4.2.1/./DaisyUI.fs.js";

type Modals_$union = 
    | Modals<0>
    | Modals<1>

type Modals_$cases = {
    0: ["AdvancedSearch", []],
    1: ["Details", []]
}

function Modals_AdvancedSearch() {
    return new Modals<0>(0, []);
}

function Modals_Details() {
    return new Modals<1>(1, []);
}

class Modals<Tag extends keyof Modals_$cases> extends Union<Tag, Modals_$cases[Tag][0]> {
    constructor(readonly tag: Tag, readonly fields: Modals_$cases[Tag][1]) {
        super();
    }
    cases() {
        return ["AdvancedSearch", "Details"];
    }
}

function Modals_$reflection(): TypeInfo {
    return union_type("Swate.Components.Modals", [], Modals, () => [[], []]);
}

function APIExtentions_optionOfString(str: string): Option<string> {
    return Option_whereNot<string>(isNullOrWhiteSpace, str);
}

function Shared_Database_Term__Term_ToComponentTerm(this$: Term): Term {
    return {
        name: APIExtentions_optionOfString(this$.Name),
        id: APIExtentions_optionOfString(this$.Accession),
        description: APIExtentions_optionOfString(this$.Description),
        source: APIExtentions_optionOfString(this$.FK_Ontology),
        isObsolete: this$.IsObsolete,
    };
}

export class TermSearchResult extends Record implements IEquatable<TermSearchResult> {
    readonly Term: Term;
    readonly IsDirectedSearchResult: boolean;
    constructor(Term: Term, IsDirectedSearchResult: boolean) {
        super();
        this.Term = Term;
        this.IsDirectedSearchResult = IsDirectedSearchResult;
    }
}

export function TermSearchResult_$reflection(): TypeInfo {
    return record_type("Swate.Components.TermSearchResult", [], TermSearchResult, () => [["Term", class_type("Swate.Components.Term", undefined, Term)], ["IsDirectedSearchResult", bool_type]]);
}

export function TermSearchResult_addSearchResults(prevResults: TermSearchResult[], newResults: TermSearchResult[]): TermSearchResult[] {
    let enumerator: any = getEnumerator(newResults);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const newResult: TermSearchResult = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            const index: int32 = prevResults.findIndex((x: TermSearchResult): boolean => equals(x.Term.id, newResult.Term.id)) | 0;
            if ((index >= 0) && newResult.IsDirectedSearchResult) {
                setItem(prevResults, index, newResult);
            }
            else if (index >= 0) {
            }
            else {
                void (prevResults.push(newResult));
            }
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    return Array.from(prevResults);
}

export type SearchState_$union = 
    | SearchState<0>
    | SearchState<1>

export type SearchState_$cases = {
    0: ["Idle", []],
    1: ["SearchDone", [TermSearchResult[]]]
}

export function SearchState_Idle() {
    return new SearchState<0>(0, []);
}

export function SearchState_SearchDone(Item: TermSearchResult[]) {
    return new SearchState<1>(1, [Item]);
}

export class SearchState<Tag extends keyof SearchState_$cases> extends Union<Tag, SearchState_$cases[Tag][0]> {
    constructor(readonly tag: Tag, readonly fields: SearchState_$cases[Tag][1]) {
        super();
    }
    cases() {
        return ["Idle", "SearchDone"];
    }
}

export function SearchState_$reflection(): TypeInfo {
    return union_type("Swate.Components.SearchState", [], SearchState, () => [[], [["Item", array_type(TermSearchResult_$reflection())]]]);
}

export function SearchState_init(): SearchState_$union {
    return SearchState_Idle();
}

export function SearchState__get_Results(this$: SearchState_$union): TermSearchResult[] {
    if (this$.tag === /* SearchDone */ 1) {
        const results: TermSearchResult[] = this$.fields[0];
        return results;
    }
    else {
        return [];
    }
}

function API_Mocks_callSearch(query: string): Promise<Term[]> {
    return PromiseBuilder__Run_212F1D4B<Term[]>(promise, PromiseBuilder__Delay_62FBFDE1<Term[]>(promise, (): Promise<Term[]> => ((new Promise(resolve => setTimeout(resolve, 1500))).then((): Promise<Term[]> => (Promise.resolve([{
        name: "Term 1",
        id: "1",
        description: "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam nec purus nec libero fermentum fermentum.",
        source: "MS",
        href: "www.test.de\'/1",
        isObsolete: false,
    }, {
        name: "Term 2",
        id: "2",
        description: "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam nec purus nec libero fermentum fermentum.",
        source: "MS",
        href: "www.test.de\'/2",
        isObsolete: false,
    }, {
        name: "Term 3",
        id: "3",
        isObsolete: false,
    }, {
        name: "Term 4 Is a Very special term with a extremely long name",
        id: "4",
        href: "www.test.de\'/3",
        isObsolete: false,
    }, {
        name: "Term 5",
        id: "5",
        href: "www.test.de\'/4",
        isObsolete: false,
    }]))))));
}

function API_Mocks_callParentSearch(parent: string, query: string): Promise<Term[]> {
    return PromiseBuilder__Run_212F1D4B<Term[]>(promise, PromiseBuilder__Delay_62FBFDE1<Term[]>(promise, (): Promise<Term[]> => ((new Promise(resolve => setTimeout(resolve, 2000))).then((): Promise<Term[]> => (Promise.resolve([{
        name: "Term 1",
        id: "1",
        href: "/term/1",
        isObsolete: false,
    }]))))));
}

function API_Mocks_callAllChildSearch(parent: string): Promise<Term[]> {
    return PromiseBuilder__Run_212F1D4B<Term[]>(promise, PromiseBuilder__Delay_62FBFDE1<Term[]>(promise, (): Promise<Term[]> => ((new Promise(resolve => setTimeout(resolve, 1500))).then((): Promise<Term[]> => (Promise.resolve(Array.from(toArray<Term>(delay_4<Term>((): Iterable<Term> => map<int32, Term>((i: int32): Term => ({
        name: toText(printf("Child %d"))(i),
        id: int32ToString(i),
        href: toText(printf("/term/%d"))(i),
        isObsolete: (i % 5) === 0,
    }), rangeDouble(0, 1, 100)))))))))));
}

function API_callSearch(query: string): Promise<Term[]> {
    const pr: Promise<Term[]> = startAsPromise(SwateApi.searchTerm(TermQuery_create_22AB55AC(query)));
    return pr.then((results: Term[]): Term[] => {
        const collection: Term[] = map_1<Term, Term>(Shared_Database_Term__Term_ToComponentTerm, results);
        return Array.from(collection);
    });
}

function API_callParentSearch(parent: string, query: string): Promise<Term[]> {
    const pr: Promise<Term[]> = startAsPromise(SwateApi.searchTerm(TermQuery_create_22AB55AC(query, undefined, parent)));
    return pr.then((results: Term[]): Term[] => {
        const collection: Term[] = map_1<Term, Term>(Shared_Database_Term__Term_ToComponentTerm, results);
        return Array.from(collection);
    });
}

function API_callAllChildSearch(parent: string): Promise<Term[]> {
    const pr: Promise<ParentTermQueryResults> = startAsPromise(SwateApi.searchChildTerms(ParentTermQuery_create_3B406CA4(parent, 300)));
    return pr.then((results: ParentTermQueryResults): Term[] => {
        const collection: Term[] = map_1<Term, Term>(Shared_Database_Term__Term_ToComponentTerm, results.results);
        return Array.from(collection);
    });
}

function API_callAdvancedSearch(dto: AdvancedSearchQuery): Promise<Term[]> {
    const pr: Promise<Term[]> = startAsPromise(SwateApi.searchTermAdvanced(dto));
    return pr.then((results: Term[]): Term[] => {
        const collection: Term[] = map_1<Term, Term>(Shared_Database_Term__Term_ToComponentTerm, results);
        return Array.from(collection);
    });
}

function TermItem(termItemInputProps: any): ReactElement {
    let elems_5: Iterable<ReactElement>, elems_1: Iterable<ReactElement>, elems: Iterable<ReactElement>, elems_4: Iterable<ReactElement>;
    const key: Option<string> = termItemInputProps.$key;
    const onTermSelect: ((arg0: Option<Term>) => void) = termItemInputProps.onTermSelect;
    const term: TermSearchResult = termItemInputProps.term;
    const patternInput: [boolean, ((arg0: boolean) => void)] = reactApi.useState<boolean, boolean>(false);
    const setCollapsed: ((arg0: boolean) => void) = patternInput[1];
    const collapsed: boolean = patternInput[0];
    const isObsolete: boolean = (term.Term.isObsolete != null) && value_66(term.Term.isObsolete);
    const isDirectedSearch: boolean = term.IsDirectedSearchResult;
    const activeClasses = "group-[.collapse-open]:bg-secondary group-[.collapse-open]:text-secondary-content";
    const gridClasses = "grid grid-cols-subgrid col-span-4";
    return createElement<any>("div", createObj(ofArray([["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("group collapse rounded-none"), delay_4<string>((): Iterable<string> => append<string>(singleton<string>(gridClasses), delay_4<string>((): Iterable<string> => (collapsed ? singleton<string>("collapse-open") : empty<string>()))))))))] as [string, any], (elems_5 = [createElement<any>("div", createObj(ofArray([["onClick", (e: any): void => {
        e.stopPropagation();
        onTermSelect(term.Term);
    }] as [string, any], ["className", join(" ", ["collapse-title p-2 min-h-fit cursor-pointer", gridClasses, activeClasses])] as [string, any], (elems_1 = [createElement<any>("div", createObj(ofArray([["className", "items-center grid col-span-4 gap-2 grid-cols-[auto,1fr,auto,auto]"] as [string, any], (elems = [createElement<any>("i", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(isObsolete ? singleton<IReactProperty>(["title", "Obsolete"] as [string, any]) : (isDirectedSearch ? singleton<IReactProperty>(["title", "Directed Search"] as [string, any]) : empty<IReactProperty>()), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("w-5"), delay_4<string>((): Iterable<string> => (isObsolete ? singleton<string>("fa-solid fa-link-slash text-error") : (isDirectedSearch ? singleton<string>("fa-solid fa-diagram-project text-primary") : empty<string>())))))))] as [string, any]))))))), createElement<any>("span", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => {
        const name_2: string = defaultArg(term.Term.name, "<no-name>");
        return append<IReactProperty>(singleton<IReactProperty>(["title", name_2] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("truncate font-bold"), delay_4<string>((): Iterable<string> => (isObsolete ? singleton<string>("line-through") : empty<string>()))))))] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", name_2] as [string, any])))));
    })))), createElement<any>("a", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => {
        const id: string = defaultArg(term.Term.id, "<no-id>");
        return append<IReactProperty>((term.Term.href != null) ? append<IReactProperty>(singleton<IReactProperty>(["onClick", (e_1: any): void => {
            e_1.stopPropagation();
        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["target", "_blank"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["href", value_66(term.Term.href)] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["className", "link link-primary"] as [string, any]))))))) : empty<IReactProperty>(), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", id] as [string, any])));
    })))), Components_CollapseButton_Z762A16CE(collapsed, setCollapsed, undefined, undefined, "btn-sm rounded justify-self-end")], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])]))), createElement<any>("div", createObj(ofArray([["className", join(" ", ["collapse-content prose-sm", "col-span-4", activeClasses])] as [string, any], (elems_4 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
        let elems_2: Iterable<ReactElement>;
        return append<ReactElement>(singleton<ReactElement>(createElement<any>("p", createObj(ofArray([["className", "text-sm"] as [string, any], (elems_2 = [defaultArg(term.Term.description, "<no-description>")], ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any])])))), delay_4<ReactElement>((): Iterable<ReactElement> => {
            let elems_3: Iterable<ReactElement>;
            return (term.Term.data != null) ? singleton<ReactElement>(createElement<any>("pre", createObj(ofArray([["className", "text-xs"] as [string, any], (elems_3 = [createElement<any>("code", {
                children: [JSON.stringify(value_66(term.Term.data), undefined, some("\t"))],
            })], ["children", reactApi.Children.toArray(Array.from(elems_3))] as [string, any])])))) : empty<ReactElement>();
        }));
    })), ["children", reactApi.Children.toArray(Array.from(elems_4))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_5))] as [string, any])])));
}

function NoResultsElement(advancedSearchToggle: Option<(() => void)>): ReactElement {
    let elems_1: Iterable<ReactElement>;
    return createElement<any>("div", createObj(ofArray([["className", "gap-y-2 py-2 px-4"] as [string, any], (elems_1 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
        children: ["No terms found matching your input."],
    })), delay_4<ReactElement>((): Iterable<ReactElement> => {
        let elems: Iterable<ReactElement>;
        return append<ReactElement>((advancedSearchToggle != null) ? singleton<ReactElement>(createElement<any>("div", createObj(singleton_1((elems = [createElement<any>("span", {
            children: ["Can\'t find the term you are looking for? "],
        }), createElement<any>("a", {
            className: "link link-primary",
            onClick: (e: any): void => {
                e.preventDefault();
                e.stopPropagation();
                value_66(advancedSearchToggle)();
            },
            children: "Try Advanced Search!",
        })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any]))))) : empty<ReactElement>(), delay_4<ReactElement>((): Iterable<ReactElement> => {
            let children: FSharpList<ReactElement>;
            return singleton<ReactElement>((children = ofArray([createElement<any>("span", {
                children: ["Can\'t find what you need? Get in "],
            }), createElement<any>("a", {
                href: "https://github.com/nfdi4plants/nfdi4plants_ontology/issues/new/choose",
                target: "_blank",
                children: "contact",
                className: "link link-primary",
            }), createElement<any>("span", {
                children: [" with us!"],
            })]), createElement<any>("div", {
                children: reactApi.Children.toArray(Array.from(children)),
            })));
        }));
    })))), ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])])));
}

function TermDropdown(onTermSelect: ((arg0: Option<Term>) => void), state: SearchState_$union, loading: FSharpSet<string>, advancedSearchToggle: Option<(() => void)>): ReactElement {
    let elems: Iterable<ReactElement>;
    return createElement<any>("div", createObj(ofArray([["style", {
        scrollbarGutter: "stable",
    }] as [string, any], ["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("min-w-[400px]"), delay_4<string>((): Iterable<string> => append<string>(singleton<string>("absolute top-[100%] left-0 right-0 z-50"), delay_4<string>((): Iterable<string> => append<string>(singleton<string>("grid grid-cols-[auto,1fr,auto,auto]"), delay_4<string>((): Iterable<string> => append<string>(singleton<string>("bg-base-200 rounded shadow-lg border-2 border-primary max-h-[400px] overflow-y-auto divide-y divide-dashed divide-base-100"), delay_4<string>((): Iterable<string> => (equals(state, SearchState_Idle()) ? singleton<string>("hidden") : empty<string>()))))))))))))] as [string, any], (elems = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
        let searchResults: TermSearchResult[], searchResults_1: TermSearchResult[], searchResults_2: TermSearchResult[];
        return (state.tag === /* SearchDone */ 1) ? (((searchResults = state.fields[0], (searchResults.length === 0) && FSharpSet__get_IsEmpty(loading))) ? ((searchResults_1 = state.fields[0], singleton<ReactElement>(NoResultsElement(advancedSearchToggle)))) : ((searchResults_2 = state.fields[0], map<TermSearchResult, ReactElement>((res: TermSearchResult): ReactElement => createElement(TermItem, {
            term: res,
            onTermSelect: onTermSelect,
        }), searchResults_2)))) : singleton<ReactElement>(defaultOf());
    })), ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
}

function IndicatorItem<$a>(indicatorPosition: string, tooltip: $a, tooltipPosition: string, icon: string, onclick: ((arg0: any) => void), isActive?: boolean, props?: FSharpList<IReactProperty>): ReactElement {
    let elems_1: Iterable<ReactElement>;
    const isActive_1: boolean = defaultArg<boolean>(isActive, true);
    return createElement<any>("span", createObj(ofArray([["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("indicator-item text-sm transition-[opacity] opacity-0"), delay_4<string>((): Iterable<string> => append<string>(singleton<string>(indicatorPosition), delay_4<string>((): Iterable<string> => (isActive_1 ? singleton<string>("!opacity-100") : empty<string>()))))))))] as [string, any], (elems_1 = [createElement<any>("button", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["data-tip", tooltip] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onClick", onclick] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(map<IReactProperty, IReactProperty>((prop: IReactProperty): IReactProperty => prop, defaultArg<FSharpList<IReactProperty>>(props, empty_1<IReactProperty>())), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", join(" ", ["btn btn-xs btn-ghost px-2", "tooltip", tooltipPosition])] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => {
        let elems: Iterable<ReactElement>;
        return singleton<IReactProperty>((elems = [createElement<any>("i", {
            className: icon,
        })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any]));
    }))))))))))))], ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])])));
}

function BaseModal(title: string, content: ReactElement, rmv: (() => void), debug?: string): ReactElement {
    return createElement<any>("div", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>((debug != null) ? singleton<IReactProperty>(Feliz_prop__prop_testid_Static_Z721C83C5(value_66(debug))) : empty<IReactProperty>(), delay_4<IReactProperty>((): Iterable<IReactProperty> => {
        let value: string;
        return append<IReactProperty>(singleton<IReactProperty>((value = "fixed top-0 left-0 right-0 bottom-0 z-50 bg-base-300 bg-opacity-50 flex items-center justify-center p-2 sm:p-10", ["className", value] as [string, any])), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onMouseDown", (_arg: any): void => {
            rmv();
        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => {
            let elems_2: Iterable<ReactElement>, value_5: string, elems_1: Iterable<ReactElement>, elems: Iterable<ReactElement>;
            return singleton<IReactProperty>((elems_2 = [createElement<any>("div", createObj(ofArray([["onMouseDown", (e: any): void => {
                e.stopPropagation();
            }] as [string, any], ["onClick", (e_1: any): void => {
                e_1.stopPropagation();
            }] as [string, any], (value_5 = "bg-base-100 rounded shadow-lg p-2 sm:p-4 flex flex-col gap-2 min-w-80 grow sm:max-w-md md:max-w-2xl max-h-[100%] overflow-hidden", ["className", value_5] as [string, any]), (elems_1 = [createElement<any>("div", createObj(ofArray([["className", "flex justify-between items-center gap-4"] as [string, any], (elems = [createElement<any>("h1", {
                className: "text-3xl font-bold",
                children: title,
            }), Components_DeleteButton_789B7C0F<Iterable<ReactElement>, FSharpList<IReactProperty>>(undefined, singleton_1(["onClick", (_arg_1: any): void => {
                rmv();
            }] as [string, any]))], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])]))), content], ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any]));
        }))));
    }))))));
}

function DetailsModal(rvm: (() => void), term: Term): ReactElement {
    let elems_1: Iterable<ReactElement>;
    const label = (str: string): ReactElement => createElement<any>("div", {
        className: "font-bold",
        children: str,
    });
    const content: ReactElement = createElement<any>("div", createObj(ofArray([["className", "grid grid-cols-1 md:grid-cols-[auto,1fr] gap-4 lg:gap-x-8"] as [string, any], (elems_1 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(label("Name")), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
        children: [defaultArg(term.name, "<no-name>")],
    })), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(label("Id")), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
        children: [defaultArg(term.id, "<no-id>")],
    })), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(label("Description")), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
        children: [defaultArg(term.description, "<no-description>")],
    })), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(label("Source")), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
        children: [defaultArg(term.source, "<no-source>")],
    })), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((term.data != null) ? append<ReactElement>(singleton<ReactElement>(label("Data")), delay_4<ReactElement>((): Iterable<ReactElement> => {
        let elems: Iterable<ReactElement>;
        return singleton<ReactElement>(createElement<any>("pre", createObj(ofArray([["className", "text-xs"] as [string, any], (elems = [createElement<any>("code", {
            children: [JSON.stringify(value_66(term.data), undefined, some("\t"))],
        })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])]))));
    })) : empty<ReactElement>(), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(((term.isObsolete != null) && value_66(term.isObsolete)) ? singleton<ReactElement>(createElement<any>("div", {
        className: "text-error",
        children: "obsolete",
    })) : empty<ReactElement>(), delay_4<ReactElement>((): Iterable<ReactElement> => ((term.href != null) ? singleton<ReactElement>(createElement<any>("a", {
        className: "link link-primary",
        href: value_66(term.href),
        target: "_blank",
        children: "Link",
    })) : empty<ReactElement>()))))))))))))))))))))))), ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])])));
    return BaseModal("Details", content, rvm);
}

function AdvancedSearchDefault(advancedSearchState: AdvancedSearchQuery, setAdvancedSearchState: ((arg0: AdvancedSearchQuery) => void)): ((arg0: { cancel: (() => void), startSearch: (() => void) }) => ReactElement) {
    return (cc: { cancel: (() => void), startSearch: (() => void) }): ReactElement => {
        let elems: Iterable<ReactElement>, children: FSharpList<ReactElement>, elems_2: Iterable<ReactElement>, elems_1: Iterable<ReactElement>, elems_4: Iterable<ReactElement>, elems_3: Iterable<ReactElement>, elems_6: Iterable<ReactElement>, elems_5: Iterable<ReactElement>;
        const xs_14: Iterable<ReactElement> = [createElement<any>("div", createObj(ofArray([["className", "prose"] as [string, any], (elems = [createElement<any>("p", {
            children: ["Use the following fields to search for terms."],
        }), (children = ofArray(["Name and Description fields follow Apache Lucene query syntax. ", createElement<any>("a", {
            href: "https://lucene.apache.org/core/2_9_4/queryparsersyntax.html",
            target: "_blank",
            children: "Learn more!",
            className: "text-xs",
        })]), createElement<any>("p", {
            children: reactApi.Children.toArray(Array.from(children)),
        }))], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])]))), createElement<any>("label", createObj(ofArray([["className", "form-control w-full"] as [string, any], (elems_2 = [createElement<any>("div", createObj(ofArray([["className", "label"] as [string, any], (elems_1 = [createElement<any>("span", {
            className: "label-text",
            children: "Term Name",
        })], ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])]))), createElement<any>("input", createObj(ofArray([Feliz_prop__prop_testid_Static_Z721C83C5("advanced-search-term-name-input"), ["className", "input input-bordered w-full"] as [string, any], ["type", "text"] as [string, any], ["autoFocus", true] as [string, any], ["value", advancedSearchState.TermName] as [string, any], ["onChange", (ev: Event): void => {
            setAdvancedSearchState(new AdvancedSearchQuery(advancedSearchState.OntologyName, ev.target.value, advancedSearchState.TermDefinition, advancedSearchState.KeepObsolete));
        }] as [string, any], ["onKeyDown", (ev_1: any): void => {
            PropHelpers_createOnKey(key_enter, (_arg: any): void => {
                cc.startSearch();
            }, ev_1);
        }] as [string, any]])))], ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any])]))), createElement<any>("label", createObj(ofArray([["className", "form-control w-full"] as [string, any], (elems_4 = [createElement<any>("div", createObj(ofArray([["className", "label"] as [string, any], (elems_3 = [createElement<any>("span", {
            className: "label-text",
            children: "Term Description",
        })], ["children", reactApi.Children.toArray(Array.from(elems_3))] as [string, any])]))), createElement<any>("input", createObj(ofArray([Feliz_prop__prop_testid_Static_Z721C83C5("advanced-search-term-description-input"), ["className", "input input-bordered w-full"] as [string, any], ["type", "text"] as [string, any], ["value", advancedSearchState.TermDefinition] as [string, any], ["onChange", (ev_2: Event): void => {
            setAdvancedSearchState(new AdvancedSearchQuery(advancedSearchState.OntologyName, advancedSearchState.TermName, ev_2.target.value, advancedSearchState.KeepObsolete));
        }] as [string, any], ["onKeyDown", (ev_3: any): void => {
            PropHelpers_createOnKey(key_enter, (_arg_1: any): void => {
                cc.startSearch();
            }, ev_3);
        }] as [string, any]])))], ["children", reactApi.Children.toArray(Array.from(elems_4))] as [string, any])]))), createElement<any>("div", createObj(ofArray([["className", "form-control max-w-xs"] as [string, any], (elems_6 = [createElement<any>("label", createObj(ofArray([["className", "label cursor-pointer"] as [string, any], (elems_5 = [createElement<any>("span", {
            className: "label-text",
            children: "Keep Obsolete",
        }), createElement<any>("input", {
            className: "checkbox",
            type: "checkbox",
            checked: advancedSearchState.KeepObsolete,
            onChange: (ev_4: Event): void => {
                setAdvancedSearchState(new AdvancedSearchQuery(advancedSearchState.OntologyName, advancedSearchState.TermName, advancedSearchState.TermDefinition, ev_4.target.checked));
            },
        })], ["children", reactApi.Children.toArray(Array.from(elems_5))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_6))] as [string, any])])))];
        return react.createElement(react.Fragment, {}, ...xs_14);
    };
}

function AdvancedSearchModal(advancedSearchModalInputProps: any): ReactElement {
    let elems_2: Iterable<ReactElement>;
    const debug: Option<boolean> = advancedSearchModalInputProps.debug;
    const onTermSelect: ((arg0: Option<Term>) => void) = advancedSearchModalInputProps.onTermSelect;
    const advancedSearch0: { form: ((arg0: { cancel: (() => void), startSearch: (() => void) }) => ReactElement), search: (() => Promise<Term[]>) } | boolean = advancedSearchModalInputProps.advancedSearch0;
    const rvm: (() => void) = advancedSearchModalInputProps.rvm;
    const patternInput: [SearchState_$union, ((arg0: SearchState_$union) => void)] = reactApi.useState<(() => SearchState_$union), SearchState_$union>(SearchState_init);
    const setSearchResults: ((arg0: SearchState_$union) => void) = patternInput[1];
    const searchResults: SearchState_$union = patternInput[0];
    const patternInput_1: [Option<int32>, ((arg0: Option<int32>) => void)] = reactApi.useState<Option<int32>, Option<int32>>(undefined);
    const tempPagination: Option<int32> = patternInput_1[0];
    const setTempPagination: ((arg0: Option<int32>) => void) = patternInput_1[1];
    const patternInput_2: [int32, ((arg0: int32) => void)] = reactApi.useState<int32, int32>(0);
    const setPagination: ((arg0: int32) => void) = patternInput_2[1];
    const pagination: int32 = patternInput_2[0] | 0;
    const patternInput_3: [AdvancedSearchQuery, ((arg0: AdvancedSearchQuery) => void)] = reactApi.useState<(() => AdvancedSearchQuery), AdvancedSearchQuery>(AdvancedSearchQuery_init);
    const setAdvancedSearchState: ((arg0: AdvancedSearchQuery) => void) = patternInput_3[1];
    const advancedSearchState: AdvancedSearchQuery = patternInput_3[0];
    let advancedSearch_1: { form: ((arg0: { cancel: (() => void), startSearch: (() => void) }) => ReactElement), search: (() => Promise<Term[]>) };
    if (typeof advancedSearch0 === "boolean") {
        advancedSearch_1 = {
            form: AdvancedSearchDefault(advancedSearchState, setAdvancedSearchState),
            search: (): Promise<Term[]> => API_callAdvancedSearch(advancedSearchState),
        };
    }
    else {
        const advancedSearch: { form: ((arg0: { cancel: (() => void), startSearch: (() => void) }) => ReactElement), search: (() => Promise<Term[]>) } = advancedSearch0;
        advancedSearch_1 = advancedSearch;
    }
    const BinSize = 20;
    let BinCount: int32;
    const dependencies_1: any[] = [searchResults];
    BinCount = reactApi.useMemo<int32>((): int32 => ~~(SearchState__get_Results(searchResults).length / BinSize), dependencies_1);
    const controller: { cancel: (() => void), startSearch: (() => void) } = {
        cancel: rvm,
        startSearch: (): void => {
            let pr_1: Promise<void>;
            const pr: Promise<Term[]> = advancedSearch_1.search();
            pr_1 = (pr.then((results: Term[]): void => {
                const results_1: TermSearchResult[] = map_1((t0: Term): TermSearchResult => (new TermSearchResult(t0, false)), results);
                setSearchResults(SearchState_SearchDone(results_1));
            }));
            pr_1.then();
        },
    };
    const dependencies_2: any[] = [pagination];
    reactApi.useEffect((): void => {
        setTempPagination(pagination + 1);
    }, dependencies_2);
    const searchFormComponent = (): ReactElement => {
        const xs_1: Iterable<ReactElement> = [advancedSearch_1.form(controller), createElement<any>("button", {
            className: "btn btn-primary",
            onClick: (_arg: any): void => {
                controller.startSearch();
            },
            children: "Submit",
        })];
        return react.createElement(react.Fragment, {}, ...xs_1);
    };
    const resultsComponent = (results_2: TermSearchResult[]): ReactElement => {
        const xs_9: Iterable<ReactElement> = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
            let children: FSharpList<ReactElement>, fmt: any;
            return append<ReactElement>(singleton<ReactElement>((children = singleton_1(((fmt = printf("Results: %i"), fmt.cont((value_5: string): ReactElement => value_5)))(results_2.length)), createElement<any>("div", {
                children: reactApi.Children.toArray(Array.from(children)),
            }))), delay_4<ReactElement>((): Iterable<ReactElement> => {
                let elems: Iterable<ReactElement>;
                return append<ReactElement>(singleton<ReactElement>(createElement<any>("div", createObj(ofArray([["className", "max-h-[50%] overflow-y-auto"] as [string, any], (elems = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => map<TermSearchResult, ReactElement>((res: TermSearchResult): ReactElement => {
                    const $key2A65033A: any = JSON.stringify(res);
                    return createElement(TermItem, {
                        term: res,
                        onTermSelect: onTermSelect,
                        key: $key2A65033A,
                        $key: $key2A65033A,
                    });
                }, getSubArray(results_2, pagination * BinSize, BinSize)))), ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])))), delay_4<ReactElement>((): Iterable<ReactElement> => {
                    let elems_1: Iterable<ReactElement>, value_17: int32;
                    return append<ReactElement>((BinCount > 1) ? singleton<ReactElement>(createElement<any>("div", createObj(ofArray([["className", "join"] as [string, any], (elems_1 = [createElement<any>("input", createObj(ofArray([["className", "input input-bordered join-item grow"] as [string, any], ["type", "number"] as [string, any], ["min", 1] as [string, any], (value_17 = (defaultArg(tempPagination, pagination) | 0), ["ref", (e: any): void => {
                        if (!(e == null) && !equals(e.value, value_17)) {
                            e.value = (value_17 | 0);
                        }
                    }] as [string, any]), ["max", BinCount] as [string, any], ["onChange", (ev: Event): void => {
                        const value_21: float64 = ev.target.valueAsNumber;
                        if (!(value_21 == null) && !Number.isNaN(value_21)) {
                            setTempPagination(min(max(round(value_21), 1), BinCount));
                        }
                    }] as [string, any]]))), createElement<any>("button", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", "btn btn-primary join-item"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => {
                        const disabled: boolean = (tempPagination == null) ? true : ((value_66(tempPagination) - 1) === pagination);
                        return append<IReactProperty>(singleton<IReactProperty>(["disabled", disabled] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onClick", (_arg_1: any): void => {
                            iterate<int32>((arg: int32): void => {
                                setPagination(arg - 1);
                            }, toArray_1<int32>(tempPagination));
                        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", "Go"] as [string, any])))));
                    })))))), createElement<any>("button", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => {
                        const disabled_1: boolean = pagination === 0;
                        return append<IReactProperty>(singleton<IReactProperty>(["className", "btn join-item"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["disabled", disabled_1] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onClick", (_arg_2: any): void => {
                            setPagination(pagination - 1);
                        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", "Previous"] as [string, any])))))));
                    })))), createElement<any>("button", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => {
                        const disabled_2: boolean = pagination === (BinCount - 1);
                        return append<IReactProperty>(singleton<IReactProperty>(["disabled", disabled_2] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", "btn join-item"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onClick", (_arg_3: any): void => {
                            setPagination(pagination + 1);
                        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", "Next"] as [string, any])))))));
                    }))))], ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])])))) : empty<ReactElement>(), delay_4<ReactElement>((): Iterable<ReactElement> => singleton<ReactElement>(createElement<any>("button", {
                        className: "btn btn-primary",
                        onClick: (_arg_4: any): void => {
                            setSearchResults(SearchState_Idle());
                        },
                        children: "Back",
                    }))));
                }));
            }));
        }));
        return react.createElement(react.Fragment, {}, ...xs_9);
    };
    const content: ReactElement = createElement<any>("div", createObj(ofArray([["className", "flex flex-col gap-2 overflow-hidden p-2"] as [string, any], (elems_2 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
        if (searchResults.tag === /* SearchDone */ 1) {
            const results_3: TermSearchResult[] = searchResults.fields[0];
            return singleton<ReactElement>(resultsComponent(results_3));
        }
        else {
            return singleton<ReactElement>(searchFormComponent());
        }
    })), ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any])])));
    return BaseModal("Advanced Search", content, rvm, unwrap(map_2<boolean, string>((_arg_5: boolean): string => "advanced-search-modal", debug)));
}

/**
 * Customizable react component for term search. Utilizing SwateDB search by default.
 */
export function TermSearch({ onTermSelect, term, parentId, termSearchQueries, parentSearchQueries, allChildrenSearchQueries, advancedSearch, showDetails, debug, disableDefaultSearch, disableDefaultParentSearch, disableDefaultAllChildrenSearch }: {onTermSelect: ((arg0: Option<Term>) => void), term: Option<Term>, parentId?: string, termSearchQueries?: [string, ((arg0: string) => Promise<Term[]>)][], parentSearchQueries?: [string, ((arg0: string) => ((arg0: string) => Promise<Term[]>))][], allChildrenSearchQueries?: [string, ((arg0: string) => Promise<Term[]>)][], advancedSearch?: { form: ((arg0: { cancel: (() => void), startSearch: (() => void) }) => ReactElement), search: (() => Promise<Term[]>) } | boolean, showDetails?: boolean, debug?: boolean, disableDefaultSearch?: boolean, disableDefaultParentSearch?: boolean, disableDefaultAllChildrenSearch?: boolean }): ReactElement {
    const showDetails_1: boolean = defaultArg<boolean>(showDetails, false);
    const debug_1: boolean = defaultArg<boolean>(debug, false);
    const patternInput: [SearchState_$union, ((arg0: ((arg0: SearchState_$union) => SearchState_$union)) => void)] = useState<SearchState_$union>(SearchState_init());
    const setSearchResults: ((arg0: ((arg0: SearchState_$union) => SearchState_$union)) => void) = patternInput[1];
    const searchResults: SearchState_$union = patternInput[0];
    const patternInput_1: [FSharpSet<string>, ((arg0: ((arg0: FSharpSet<string>) => FSharpSet<string>)) => void)] = useState<FSharpSet<string>>(empty_2<string>({
        Compare: comparePrimitives,
    }));
    const setLoading: ((arg0: ((arg0: FSharpSet<string>) => FSharpSet<string>)) => void) = patternInput_1[1];
    const loading: FSharpSet<string> = patternInput_1[0];
    const inputRef: IRefValue$1<Option<any>> = reactApi.useRef(undefined);
    const containerRef: IRefValue$1<Option<any>> = reactApi.useRef(undefined);
    const patternInput_2: [boolean, ((arg0: boolean) => void)] = reactApi.useState<boolean, boolean>(false);
    const setFocused: ((arg0: boolean) => void) = patternInput_2[1];
    const focused: boolean = patternInput_2[0];
    const cancelled: IRefValue$1<boolean> = reactApi.useRef(false);
    const patternInput_3: [Option<Modals_$union>, ((arg0: Option<Modals_$union>) => void)] = reactApi.useState<Option<Modals_$union>, Option<Modals_$union>>(undefined);
    const setModal: ((arg0: Option<Modals_$union>) => void) = patternInput_3[1];
    const modal: Option<Modals_$union> = patternInput_3[0];
    reactApi.useEffect((): void => {
        const pr: Promise<void> = PromiseBuilder__Run_212F1D4B<void>(promise, PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => (TIBApi.searchAllChildrenOf("MS:1000031").then((_arg_1: Term[]): Promise<void> => {
            const results: Term[] = _arg_1;
            console.log(some(results));
            return Promise.resolve();
        }))));
        pr.then();
    });
    const setModal_1 = (modal_1: Option<Modals_$union>): void => {
        if (modal_1 != null) {
            setSearchResults((_arg_2: SearchState_$union): SearchState_$union => SearchState_init());
        }
        setModal(modal_1);
    };
    const onTermSelect_1 = (term_1: Option<Term>): void => {
        if (inputRef.current != null) {
            const v: string = defaultArg(bind<Term, string>((t: Term): Option<string> => t.name, term_1), "");
            value_66(inputRef.current).value = v;
        }
        setSearchResults((_arg_3: SearchState_$union): SearchState_$union => SearchState_init());
        onTermSelect(term_1);
    };
    const startLoadingBy = (key: string): void => {
        setLoading((l: FSharpSet<string>): FSharpSet<string> => {
            const key_1: string = "L_" + key;
            return FSharpSet__Add(l, key_1);
        });
    };
    const stopLoadingBy = (key_2: string): void => {
        setLoading((l_1: FSharpSet<string>): FSharpSet<string> => {
            const key_3: string = "L_" + key_2;
            return FSharpSet__Remove(l_1, key_3);
        });
    };
    const createTermSearch = (id: string): ((arg0: ((arg0: string) => Promise<Term[]>)) => ((arg0: string) => Promise<void>)) => ((search: ((arg0: string) => Promise<Term[]>)): ((arg0: string) => Promise<void>) => {
        const id_1: string = "T_" + id;
        return (query: string): Promise<void> => PromiseBuilder__Run_212F1D4B<void>(promise, PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => {
            startLoadingBy(id_1);
            return search(query).then((_arg_4: Term[]): Promise<void> => {
                const termSearchResults: Term[] = _arg_4;
                const termSearchResults_1: TermSearchResult[] = map_1((t0: Term): TermSearchResult => (new TermSearchResult(t0, false)), termSearchResults);
                return (!cancelled.current ? ((setSearchResults((prevResults: SearchState_$union): SearchState_$union => SearchState_SearchDone(TermSearchResult_addSearchResults(SearchState__get_Results(prevResults), termSearchResults_1))), Promise.resolve())) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => {
                    stopLoadingBy(id_1);
                    return Promise.resolve();
                }));
            });
        }));
    });
    const createParentChildTermSearch = (id_2: string): ((arg0: ((arg0: string) => ((arg0: string) => Promise<Term[]>))) => ((arg0: string) => ((arg0: string) => Promise<void>))) => ((search_1: ((arg0: string) => ((arg0: string) => Promise<Term[]>))): ((arg0: string) => ((arg0: string) => Promise<void>)) => {
        const id_3: string = "PC_" + id_2;
        return (parentId_1: string): ((arg0: string) => Promise<void>) => ((query_1: string): Promise<void> => PromiseBuilder__Run_212F1D4B<void>(promise, PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => {
            startLoadingBy(id_3);
            return search_1(parentId_1)(query_1).then((_arg_5: Term[]): Promise<void> => {
                const termSearchResults_2: Term[] = _arg_5;
                const termSearchResults_3: TermSearchResult[] = map_1((t0_1: Term): TermSearchResult => (new TermSearchResult(t0_1, true)), termSearchResults_2);
                return (!cancelled.current ? ((setSearchResults((prevResults_2: SearchState_$union): SearchState_$union => SearchState_SearchDone(TermSearchResult_addSearchResults(SearchState__get_Results(prevResults_2), termSearchResults_3))), Promise.resolve())) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => {
                    stopLoadingBy(id_3);
                    return Promise.resolve();
                }));
            });
        })));
    });
    const createAllChildTermSearch = (id_4: string): ((arg0: ((arg0: string) => Promise<Term[]>)) => ((arg0: string) => Promise<void>)) => ((search_2: ((arg0: string) => Promise<Term[]>)): ((arg0: string) => Promise<void>) => {
        const id_5: string = "AC_" + id_4;
        return (parentId_2: string): Promise<void> => PromiseBuilder__Run_212F1D4B<void>(promise, PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => {
            startLoadingBy(id_5);
            return search_2(parentId_2).then((_arg_6: Term[]): Promise<void> => {
                const termSearchResults_4: Term[] = _arg_6;
                const termSearchResults_5: TermSearchResult[] = map_1((t0_2: Term): TermSearchResult => (new TermSearchResult(t0_2, true)), termSearchResults_4);
                return (!cancelled.current ? ((setSearchResults((prevResults_4: SearchState_$union): SearchState_$union => SearchState_SearchDone(TermSearchResult_addSearchResults(SearchState__get_Results(prevResults_4), termSearchResults_5))), Promise.resolve())) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => {
                    stopLoadingBy(id_5);
                    return Promise.resolve();
                }));
            });
        }));
    });
    const termSearchFunc = (query_2: string): void => {
        let pr_2: Promise<void[]>;
        const pr_1: FSharpList<Promise<void>> = toList<Promise<void>>(delay_4<Promise<void>>((): Iterable<Promise<void>> => append<Promise<void>>(((disableDefaultSearch != null) && value_66(disableDefaultSearch)) ? (empty<Promise<void>>()) : singleton<Promise<void>>(createTermSearch("DEFAULT_SIMPLE")(API_callSearch)(query_2)), delay_4<Promise<void>>((): Iterable<Promise<void>> => ((termSearchQueries != null) ? collect<[string, ((arg0: string) => Promise<Term[]>)], Iterable<Promise<void>>, Promise<void>>((matchValue: [string, ((arg0: string) => Promise<Term[]>)]): Iterable<Promise<void>> => {
            const termSearch: ((arg0: string) => Promise<Term[]>) = matchValue[1];
            const id_6: string = matchValue[0];
            return singleton<Promise<void>>(createTermSearch(id_6)(termSearch)(query_2));
        }, value_66(termSearchQueries)) : empty<Promise<void>>())))));
        pr_2 = (Promise.all(pr_1));
        pr_2.then();
    };
    const parentSearch_1 = (query_4: string): void => {
        let pr_4: Promise<void[]>;
        const pr_3: FSharpList<Promise<void>> = toList<Promise<void>>(delay_4<Promise<void>>((): Iterable<Promise<void>> => ((parentId != null) ? append<Promise<void>>(((disableDefaultParentSearch != null) && value_66(disableDefaultParentSearch)) ? (empty<Promise<void>>()) : singleton<Promise<void>>(createParentChildTermSearch("DEFAULT_PARENTCHILD")((parent: string): ((arg0: string) => Promise<Term[]>) => ((query_5: string): Promise<Term[]> => API_callParentSearch(parent, query_5)))(value_66(parentId))(query_4)), delay_4<Promise<void>>((): Iterable<Promise<void>> => ((parentSearchQueries != null) ? collect<[string, ((arg0: string) => ((arg0: string) => Promise<Term[]>))], Iterable<Promise<void>>, Promise<void>>((matchValue_1: [string, ((arg0: string) => ((arg0: string) => Promise<Term[]>))]): Iterable<Promise<void>> => {
            const parentSearch: ((arg0: string) => ((arg0: string) => Promise<Term[]>)) = matchValue_1[1];
            const id_7: string = matchValue_1[0];
            return singleton<Promise<void>>(createParentChildTermSearch(id_7)(parentSearch)(value_66(parentId))(query_4));
        }, value_66(parentSearchQueries)) : empty<Promise<void>>()))) : empty<Promise<void>>())));
        pr_4 = (Promise.all(pr_3));
        pr_4.then();
    };
    const allChildSearch_1 = (): void => {
        let pr_6: Promise<void[]>;
        const pr_5: FSharpList<Promise<void>> = toList<Promise<void>>(delay_4<Promise<void>>((): Iterable<Promise<void>> => ((parentId != null) ? append<Promise<void>>(((disableDefaultAllChildrenSearch != null) && value_66(disableDefaultAllChildrenSearch)) ? (empty<Promise<void>>()) : singleton<Promise<void>>(createAllChildTermSearch("DEFAULT_ALLCHILD")(API_callAllChildSearch)(value_66(parentId))), delay_4<Promise<void>>((): Iterable<Promise<void>> => ((allChildrenSearchQueries != null) ? collect<[string, ((arg0: string) => Promise<Term[]>)], Iterable<Promise<void>>, Promise<void>>((matchValue_2: [string, ((arg0: string) => Promise<Term[]>)]): Iterable<Promise<void>> => {
            const id_8: string = matchValue_2[0];
            const allChildSearch: ((arg0: string) => Promise<Term[]>) = matchValue_2[1];
            return singleton<Promise<void>>(createAllChildTermSearch(id_8)(allChildSearch)(value_66(parentId)));
        }, value_66(allChildrenSearchQueries)) : empty<Promise<void>>()))) : empty<Promise<void>>())));
        pr_6 = (Promise.all(pr_5));
        pr_6.then();
    };
    let patternInput_4: [(() => void), ((arg0: string) => void)];
    const id_9 = "DEFAULT_DEBOUNCE_SIMPLE";
    const startDebounceLoading = (): void => {
        startLoadingBy(id_9);
    };
    const stopDebounceLoading = (): void => {
        stopLoadingBy(id_9);
    };
    const func: ((arg0: string) => void) = termSearchFunc;
    const timeout: IRefValue$1<Option<int32>> = reactApi.useRef(undefined);
    const delay_1 = 500;
    let debouncedCallBack: ((arg0: string) => void);
    const dependencies_1: any[] = [func, delay_1];
    debouncedCallBack = reactApi.useCallback<string, void>((arg: string): void => {
        const later = (): void => {
            iterate<int32>((token: int32): void => {
                clearTimeout(token);
            }, toArray_1<int32>(timeout.current));
            iterate<(() => void)>((f: (() => void)): void => {
                f();
            }, toArray_1<(() => void)>(stopDebounceLoading));
            func(arg);
        };
        iterate<(() => void)>((f_1: (() => void)): void => {
            f_1();
        }, toArray_1<(() => void)>(startDebounceLoading));
        iterate<int32>((token_1: int32): void => {
            clearTimeout(token_1);
        }, toArray_1<int32>(timeout.current));
        timeout.current = setTimeout(later, delay_1);
    }, dependencies_1);
    const cancel: (() => void) = reactApi.useCallback<void, void>((): void => {
        if (timeout.current != null) {
            clearTimeout(value_66(timeout.current));
            iterate<(() => void)>((f_2: (() => void)): void => {
                f_2();
            }, toArray_1<(() => void)>(stopDebounceLoading));
        }
    }, []);
    patternInput_4 = ([cancel, debouncedCallBack] as [(() => void), ((arg0: string) => void)]);
    const search_3: ((arg0: string) => void) = patternInput_4[1];
    const cancelSearch: (() => void) = patternInput_4[0];
    let patternInput_5: [(() => void), ((arg0: string) => void)];
    const id_10 = "DEFAULT_DEBOUNCE_PARENT";
    const startDebounceLoading_1 = (): void => {
        startLoadingBy(id_10);
    };
    const stopDebounceLoading_1 = (): void => {
        stopLoadingBy(id_10);
    };
    const func_1: ((arg0: string) => void) = parentSearch_1;
    const timeout_1: IRefValue$1<Option<int32>> = reactApi.useRef(undefined);
    const delay_1_1 = 500;
    let debouncedCallBack_1: ((arg0: string) => void);
    const dependencies_1_2: any[] = [func_1, delay_1_1];
    debouncedCallBack_1 = reactApi.useCallback<string, void>((arg_1: string): void => {
        const later_1 = (): void => {
            iterate<int32>((token_2: int32): void => {
                clearTimeout(token_2);
            }, toArray_1<int32>(timeout_1.current));
            iterate<(() => void)>((f_3: (() => void)): void => {
                f_3();
            }, toArray_1<(() => void)>(stopDebounceLoading_1));
            func_1(arg_1);
        };
        iterate<(() => void)>((f_1_1: (() => void)): void => {
            f_1_1();
        }, toArray_1<(() => void)>(startDebounceLoading_1));
        iterate<int32>((token_1_1: int32): void => {
            clearTimeout(token_1_1);
        }, toArray_1<int32>(timeout_1.current));
        timeout_1.current = setTimeout(later_1, delay_1_1);
    }, dependencies_1_2);
    const cancel_1: (() => void) = reactApi.useCallback<void, void>((): void => {
        if (timeout_1.current != null) {
            clearTimeout(value_66(timeout_1.current));
            iterate<(() => void)>((f_2_1: (() => void)): void => {
                f_2_1();
            }, toArray_1<(() => void)>(stopDebounceLoading_1));
        }
    }, []);
    patternInput_5 = ([cancel_1, debouncedCallBack_1] as [(() => void), ((arg0: string) => void)]);
    const parentSearch_2: ((arg0: string) => void) = patternInput_5[1];
    const cancelParentSearch: (() => void) = patternInput_5[0];
    let patternInput_6: [(() => void), (() => void)];
    const func_2: (() => void) = allChildSearch_1;
    const timeout_2: IRefValue$1<Option<int32>> = reactApi.useRef(undefined);
    const delay_1_2 = 0;
    let debouncedCallBack_2: (() => void);
    const dependencies_1_4: any[] = [func_2, delay_1_2];
    debouncedCallBack_2 = reactApi.useCallback<void, void>((arg_2: void): void => {
        const later_2 = (): void => {
            iterate<int32>((token_3: int32): void => {
                clearTimeout(token_3);
            }, toArray_1<int32>(timeout_2.current));
            iterate<(() => void)>((f_4: (() => void)): void => {
                f_4();
            }, toArray_1<(() => void)>(undefined));
            func_2();
        };
        iterate<(() => void)>((f_1_2: (() => void)): void => {
            f_1_2();
        }, toArray_1<(() => void)>(undefined));
        iterate<int32>((token_1_2: int32): void => {
            clearTimeout(token_1_2);
        }, toArray_1<int32>(timeout_2.current));
        timeout_2.current = setTimeout(later_2, delay_1_2);
    }, dependencies_1_4);
    const cancel_2: (() => void) = reactApi.useCallback<void, void>((): void => {
        if (timeout_2.current != null) {
            clearTimeout(value_66(timeout_2.current));
            iterate<(() => void)>((f_2_2: (() => void)): void => {
                f_2_2();
            }, toArray_1<(() => void)>(undefined));
        }
    }, []);
    patternInput_6 = ([cancel_2, debouncedCallBack_2] as [(() => void), (() => void)]);
    const cancelAllChildSearch: (() => void) = patternInput_6[0];
    const allChildSearch_2: (() => void) = patternInput_6[1];
    const cancel_3 = (): void => {
        setSearchResults((_arg_7: SearchState_$union): SearchState_$union => SearchState_init());
        cancelled.current = true;
        setLoading((_arg_8: FSharpSet<string>): FSharpSet<string> => empty_2<string>({
            Compare: comparePrimitives,
        }));
        cancelSearch();
        cancelParentSearch();
        cancelAllChildSearch();
    };
    const startSearch = (query_6: string): void => {
        cancelled.current = false;
        setSearchResults((_arg_9: SearchState_$union): SearchState_$union => SearchState_init());
        search_3(query_6);
        parentSearch_2(query_6);
    };
    const startAllChildSearch = (): void => {
        cancelled.current = false;
        setSearchResults((_arg_10: SearchState_$union): SearchState_$union => SearchState_init());
        allChildSearch_2();
    };
    const elemRef: IRefValue$1<Option<any>> = containerRef;
    const callback = (_arg_11: any): void => {
        setFocused(false);
        setSearchResults((_arg_12: SearchState_$union): SearchState_$union => SearchState_init());
        cancel_3();
    };
    const dependencies_7: Option<Option<any[]>> = undefined;
    const options_1: any = defaultArg(undefined, Impl_defaultPassive);
    const action_6 = (ev: any): void => {
        let elem: any, copyOfStruct: any;
        const matchValue_3: Option<any> = elemRef.current;
        let matchResult: int32, elem_1: any;
        if (matchValue_3 != null) {
            if ((elem = value_66(matchValue_3), !((copyOfStruct = elem, copyOfStruct.contains(ev.target))))) {
                matchResult = 0;
                elem_1 = value_66(matchValue_3);
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
                callback(ev);
                break;
            }
            case 1: {
                break;
            }
        }
    };
    const options_3: Option<any> = options_1;
    const dependencies_9: Option<any[]> = dependencies_7;
    let addOptions: Option<any>;
    const dependencies_1_6: any[] = [options_3];
    addOptions = reactApi.useMemo<Option<any>>((): Option<any> => Impl_adjustPassive(options_3), dependencies_1_6);
    let removeOptions: Option<any>;
    const dependencies_1_7: any[] = [options_3];
    removeOptions = reactApi.useMemo<Option<any>>((): Option<any> => Impl_createRemoveOptions(options_3), dependencies_1_7);
    let fn: ((arg0: Event) => void);
    const dependencies_1_8: any[] = toArray<any>(delay_4<any>((): Iterable<any> => append<any>(singleton<any>(action_6), delay_4<any>((): Iterable<any> => ((dependencies_9 != null) ? value_66(dependencies_9) : empty<any>())))));
    fn = reactApi.useMemo<((arg0: Event) => void)>((): ((arg0: Event) => void) => ((arg_4: Event): void => {
        action_6(arg_4);
    }), dependencies_1_8);
    const listener: (() => IDisposable) = useCallbackRef<void, IDisposable>((): IDisposable => {
        if (addOptions == null) {
            document.addEventListener("mousedown", fn);
        }
        else {
            const options_1_1: any = value_66(addOptions);
            document.addEventListener("mousedown", fn, options_1_1);
        }
        return createDisposable((): void => {
            if (removeOptions == null) {
                document.removeEventListener("mousedown", fn);
            }
            else {
                const options_2_1: any = value_66(removeOptions);
                document.removeEventListener("mousedown", fn, options_2_1);
            }
        });
    });
    useEffect(listener);
    const action_8 = (ev_1: any): void => {
        let elem_2: any, copyOfStruct_1: any;
        const matchValue_1_1: Option<any> = elemRef.current;
        let matchResult_1: int32, elem_3: any;
        if (matchValue_1_1 != null) {
            if ((elem_2 = value_66(matchValue_1_1), !((copyOfStruct_1 = elem_2, copyOfStruct_1.contains(ev_1.target))))) {
                matchResult_1 = 0;
                elem_3 = value_66(matchValue_1_1);
            }
            else {
                matchResult_1 = 1;
            }
        }
        else {
            matchResult_1 = 1;
        }
        switch (matchResult_1) {
            case 0: {
                callback(ev_1);
                break;
            }
            case 1: {
                break;
            }
        }
    };
    const options_5: Option<any> = options_1;
    const dependencies_14: Option<any[]> = dependencies_7;
    let addOptions_1: Option<any>;
    const dependencies_1_9: any[] = [options_5];
    addOptions_1 = reactApi.useMemo<Option<any>>((): Option<any> => Impl_adjustPassive(options_5), dependencies_1_9);
    let removeOptions_1: Option<any>;
    const dependencies_1_10: any[] = [options_5];
    removeOptions_1 = reactApi.useMemo<Option<any>>((): Option<any> => Impl_createRemoveOptions(options_5), dependencies_1_10);
    let fn_1: ((arg0: Event) => void);
    const dependencies_1_11: any[] = toArray<any>(delay_4<any>((): Iterable<any> => append<any>(singleton<any>(action_8), delay_4<any>((): Iterable<any> => ((dependencies_14 != null) ? value_66(dependencies_14) : empty<any>())))));
    fn_1 = reactApi.useMemo<((arg0: Event) => void)>((): ((arg0: Event) => void) => ((arg_5: Event): void => {
        action_8(arg_5);
    }), dependencies_1_11);
    const listener_1: (() => IDisposable) = useCallbackRef<void, IDisposable>((): IDisposable => {
        if (addOptions_1 == null) {
            document.addEventListener("touchstart", fn_1);
        }
        else {
            const options_1_2: any = value_66(addOptions_1);
            document.addEventListener("touchstart", fn_1, options_1_2);
        }
        return createDisposable((): void => {
            if (removeOptions_1 == null) {
                document.removeEventListener("touchstart", fn_1);
            }
            else {
                const options_2_2: any = value_66(removeOptions_1);
                document.removeEventListener("touchstart", fn_1, options_2_2);
            }
        });
    });
    useEffect(listener_1);
    return createElement<any>("div", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(debug_1 ? append<IReactProperty>(singleton<IReactProperty>(["data-debug-loading", JSON.stringify(loading)] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["data-debug-searchresults", JSON.stringify(searchResults)] as [string, any]))) : empty<IReactProperty>(), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", "form-control"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["ref", containerRef] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => {
        let elems_2: Iterable<ReactElement>;
        return singleton<IReactProperty>((elems_2 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
            let matchValue_4: Option<Modals_$union>, onTermSelect_2: ((arg0: Option<Term>) => void);
            return append<ReactElement>((matchValue_4 = modal, (matchValue_4 != null) ? ((value_66(matchValue_4).tag === /* AdvancedSearch */ 0) ? ((advancedSearch != null) ? ((onTermSelect_2 = ((term_2: Option<Term>): void => {
                onTermSelect_1(term_2);
                setModal_1(undefined);
            }), singleton<ReactElement>(createElement(AdvancedSearchModal, {
                rvm: (): void => {
                    setModal_1(undefined);
                },
                advancedSearch0: value_66(advancedSearch),
                onTermSelect: onTermSelect_2,
                debug: debug_1,
            })))) : singleton<ReactElement>(defaultOf())) : ((term != null) ? singleton<ReactElement>(DetailsModal((): void => {
                setModal_1(undefined);
            }, value_66(term))) : singleton<ReactElement>(defaultOf()))) : singleton<ReactElement>(defaultOf())), delay_4<ReactElement>((): Iterable<ReactElement> => {
                let elems_1: Iterable<ReactElement>;
                return singleton<ReactElement>(createElement<any>("div", createObj(ofArray([["className", "indicator"] as [string, any], (elems_1 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
                    let matchValue_5: Option<Term>, term_3: Term, term_4: Term, arg_6: string, arg_7: string;
                    return append<ReactElement>((matchValue_5 = term, (matchValue_5 != null) ? (((term_3 = value_66(matchValue_5), (term_3.name != null) && (term_3.id != null))) ? ((term_4 = value_66(matchValue_5), !isNullOrWhiteSpace(value_66(term_4.id)) ? singleton<ReactElement>(IndicatorItem<string>("", (arg_6 = value_66(term_4.name), (arg_7 = value_66(term_4.id), toText(printf("%s - %s"))(arg_6)(arg_7))), "tooltip-left", "fa-solid fa-square-check text-primary", (_arg_13: any): void => {
                        setModal_1(((modal != null) && equals(value_66(modal), Modals_Details())) ? undefined : Modals_Details());
                    })) : empty<ReactElement>())) : (showDetails_1 ? singleton<ReactElement>(IndicatorItem<string>("", "Details", "tooltip-left", "fa-solid fa-circle-info text-info", (_arg_14: any): void => {
                        setModal_1(((modal != null) && equals(value_66(modal), Modals_Details())) ? undefined : Modals_Details());
                    }, focused)) : singleton<ReactElement>(defaultOf()))) : singleton<ReactElement>(defaultOf())), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((advancedSearch != null) ? singleton<ReactElement>(IndicatorItem<string>("indicator-bottom", "Advanced Search", "tooltip-left", "fa-solid fa-magnifying-glass-plus text-primary", (_arg_15: any): void => {
                        setModal_1(((modal != null) && equals(value_66(modal), Modals_AdvancedSearch())) ? undefined : Modals_AdvancedSearch());
                    }, focused, singleton_1(Feliz_prop__prop_testid_Static_Z721C83C5("advanced-search-indicator")))) : empty<ReactElement>(), delay_4<ReactElement>((): Iterable<ReactElement> => {
                        let elems: Iterable<ReactElement>;
                        return singleton<ReactElement>(createElement<any>("div", createObj(ofArray([["className", "input input-bordered flex flex-row items-center relative"] as [string, any], (elems = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("input", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(debug_1 ? singleton<IReactProperty>(Feliz_prop__prop_testid_Static_Z721C83C5("term-search-input")) : empty<IReactProperty>(), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["ref", inputRef] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["defaultValue", defaultArg(bind<Term, string>((_arg_16: Term): Option<string> => _arg_16.name, term), "")] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["placeholder", "..."] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onChange", (ev_2: Event): void => {
                            const e: string = ev_2.target.value;
                            if (isNullOrEmpty(e)) {
                                onTermSelect_1(undefined);
                                cancel_3();
                            }
                            else {
                                onTermSelect_1({
                                    name: e,
                                });
                                startSearch(e);
                            }
                        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onDoubleClick", (_arg_17: any): void => {
                            if ((parentId != null) && isNullOrEmpty(value_66(inputRef.current).value)) {
                                startAllChildSearch();
                            }
                            else if (!isNullOrEmpty(value_66(inputRef.current).value)) {
                                startSearch(value_66(inputRef.current).value);
                            }
                        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onKeyDown", (ev_3: any): void => {
                            PropHelpers_createOnKey(key_escape, (_arg_18: any): void => {
                                cancel_3();
                            }, ev_3);
                        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["onFocus", (_arg_19: any): void => {
                            setFocused(true);
                        }] as [string, any])))))))))))))))))))), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("span", createObj(Helpers_combineClasses("loading", singleton_1(["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("text-primary loading-sm"), delay_4<string>((): Iterable<string> => (FSharpSet__get_IsEmpty(loading) ? singleton<string>("invisible") : empty<string>()))))))] as [string, any]))))), delay_4<ReactElement>((): Iterable<ReactElement> => {
                            const advancedSearchToggle: Option<(() => void)> = map_2<{ form: ((arg0: { cancel: (() => void), startSearch: (() => void) }) => ReactElement), search: (() => Promise<Term[]>) } | boolean, (() => void)>((_arg_20: { form: ((arg0: { cancel: (() => void), startSearch: (() => void) }) => ReactElement), search: (() => Promise<Term[]>) } | boolean): (() => void) => ((): void => {
                                setModal_1(((modal != null) && equals(value_66(modal), Modals_AdvancedSearch())) ? undefined : Modals_AdvancedSearch());
                            }), advancedSearch);
                            return singleton<ReactElement>(TermDropdown(onTermSelect_1, searchResults, loading, advancedSearchToggle));
                        })))))), ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])]))));
                    }))));
                })), ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])]))));
            }));
        })), ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any]));
    }))))))))));
}

export default TermSearch;

