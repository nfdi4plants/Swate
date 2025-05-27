import { toString, Record, Union } from "../fable_modules/fable-library-ts.4.24.0/Types.js";
import { array_type, bool_type, class_type, record_type, option_type, int32_type, union_type, TypeInfo } from "../fable_modules/fable-library-ts.4.24.0/Reflection.js";
import { Option_whereNot } from "../../../Shared/Extensions.fs.js";
import { isNullOrEmpty, join, printf, toText, isNullOrWhiteSpace } from "../fable_modules/fable-library-ts.4.24.0/String.js";
import { bind, toArray as toArray_1, some, map as map_2, defaultArg, value as value_66, unwrap, Option } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { Term } from "../../../Shared/Database.fs.js";
import { float64, int32 } from "../fable_modules/fable-library-ts.4.24.0/Int32.js";
import { IDisposable, comparePrimitives, round, createObj, int32ToString, equals, disposeSafe, getEnumerator, IComparable, IEquatable } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { TermSearchStyleModule_resolveStyle, TermModule_joinLeft } from "../Util/Types.fs.js";
import { getSubArray, map as map_1, setItem } from "../fable_modules/fable-library-ts.4.24.0/Array.js";
import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../fable_modules/Fable.Promise.2.2.2/Promise.fs.js";
import { promise } from "../fable_modules/Fable.Promise.2.2.2/PromiseImpl.fs.js";
import { iterate, collect, empty, singleton, append, toList, map, delay as delay_4, toArray } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { rangeDouble } from "../fable_modules/fable-library-ts.4.24.0/Range.js";
import { startAsPromise } from "../fable_modules/fable-library-ts.4.24.0/Async.js";
import { SwateApi } from "../Util/Api.fs.js";
import { TermQuery_create_Z6FBE353C } from "../../../Shared/DTOs/TermQuery.fs.js";
import { ParentTermQueryResults, ParentTermQuery_create_3B406CA4 } from "../../../Shared/DTOs/ParentTermQuery.fs.js";
import { AdvancedSearchQuery_init, AdvancedSearchQuery } from "../../../Shared/DTOs/AdvancedSearch.fs.js";
import { useState, ReactElement, createElement } from "react";
import React from "react";
import * as react from "react";
import { reactApi } from "../fable_modules/Feliz.2.9.0/./Interop.fs.js";
import { IRefValue$1 } from "../fable_modules/Fable.React.Types.18.4.0/Fable.React.fs.js";
import { IReactProperty } from "../fable_modules/Feliz.2.9.0/Types.fs.js";
import { empty as empty_2, cons, fold, reverse, FSharpList, singleton as singleton_1, ofArray } from "../fable_modules/fable-library-ts.4.24.0/List.js";
import { FSharpSet__Remove, FSharpSet__Add, empty as empty_1, FSharpSet, FSharpSet__get_IsEmpty } from "../fable_modules/fable-library-ts.4.24.0/Set.js";
import { defaultOf } from "../fable_modules/Feliz.2.9.0/../../Util/../fable_modules/fable-library-ts.4.24.0/Util.js";
import { BaseModal } from "../GenericComponents/BaseModal.fs.js";
import { Feliz_prop__prop_testid_Static_Z721C83C5 } from "../Util/Extensions.fs.js";
import { PropHelpers_createOnKey } from "../fable_modules/Feliz.2.9.0/./Properties.fs.js";
import { key_enter } from "../fable_modules/Feliz.2.9.0/Key.fs.js";
import { max, min } from "../fable_modules/fable-library-ts.4.24.0/Double.js";
import { useEffect, useLayoutEffectWithDeps } from "../fable_modules/Feliz.2.9.0/./ReactInterop.js";
import { useCallbackRef, createDisposable } from "../fable_modules/Feliz.2.9.0/./Internal.fs.js";
import { Impl_createRemoveOptions, Impl_adjustPassive } from "../Util/./React.useListener.fs.js";
import { createPortal } from "react-dom";

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

function Swate_Components_Shared_Database_Term__Term_ToComponentTerm(this$: Term): Term {
    return {
        name: unwrap(APIExtentions_optionOfString(this$.Name)),
        id: unwrap(APIExtentions_optionOfString(this$.Accession)),
        description: unwrap(APIExtentions_optionOfString(this$.Description)),
        source: unwrap(APIExtentions_optionOfString(this$.FK_Ontology)),
        isObsolete: this$.IsObsolete,
    };
}

class KeyboardNavigationController extends Record implements IEquatable<KeyboardNavigationController>, IComparable<KeyboardNavigationController> {
    readonly SelectedTermSearchResult: Option<int32>;
    constructor(SelectedTermSearchResult: Option<int32>) {
        super();
        this.SelectedTermSearchResult = SelectedTermSearchResult;
    }
}

function KeyboardNavigationController_$reflection(): TypeInfo {
    return record_type("Swate.Components.KeyboardNavigationController", [], KeyboardNavigationController, () => [["SelectedTermSearchResult", option_type(int32_type)]]);
}

function KeyboardNavigationController_init(): KeyboardNavigationController {
    return new KeyboardNavigationController(undefined);
}

class TermSearchResult extends Record implements IEquatable<TermSearchResult> {
    readonly Term: Term;
    readonly IsDirectedSearchResult: boolean;
    constructor(Term: Term, IsDirectedSearchResult: boolean) {
        super();
        this.Term = Term;
        this.IsDirectedSearchResult = IsDirectedSearchResult;
    }
}

function TermSearchResult_$reflection(): TypeInfo {
    return record_type("Swate.Components.TermSearchResult", [], TermSearchResult, () => [["Term", class_type("Swate.Components.Term", undefined, Term)], ["IsDirectedSearchResult", bool_type]]);
}

function TermSearchResult_addSearchResults(prevResults: TermSearchResult[], newResults: TermSearchResult[]): TermSearchResult[] {
    let enumerator: any = getEnumerator(newResults);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const newResult: TermSearchResult = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            const index: int32 = prevResults.findIndex((x: TermSearchResult): boolean => equals(x.Term.id, newResult.Term.id)) | 0;
            if (index >= 0) {
                const matchValue: TermSearchResult = prevResults[index];
                if (matchValue.IsDirectedSearchResult) {
                    if (newResult.IsDirectedSearchResult) {
                        const t2_3: Term = newResult.Term;
                        const t1_3: Term = matchValue.Term;
                        setItem(prevResults, index, new TermSearchResult(TermModule_joinLeft(t1_3, t2_3), true));
                    }
                    else {
                        const t2_1: Term = newResult.Term;
                        const t1_1: Term = matchValue.Term;
                        setItem(prevResults, index, new TermSearchResult(TermModule_joinLeft(t1_1, t2_1), true));
                    }
                }
                else if (newResult.IsDirectedSearchResult) {
                    const t2: Term = newResult.Term;
                    const t1: Term = matchValue.Term;
                    setItem(prevResults, index, new TermSearchResult(TermModule_joinLeft(t2, t1), true));
                }
                else {
                    const t2_2: Term = newResult.Term;
                    const t1_2: Term = matchValue.Term;
                    setItem(prevResults, index, new TermSearchResult(TermModule_joinLeft(t1_2, t2_2), false));
                }
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

type SearchState_$union = 
    | SearchState<0>
    | SearchState<1>

type SearchState_$cases = {
    0: ["Idle", []],
    1: ["SearchDone", [TermSearchResult[]]]
}

function SearchState_Idle() {
    return new SearchState<0>(0, []);
}

function SearchState_SearchDone(Item: TermSearchResult[]) {
    return new SearchState<1>(1, [Item]);
}

class SearchState<Tag extends keyof SearchState_$cases> extends Union<Tag, SearchState_$cases[Tag][0]> {
    constructor(readonly tag: Tag, readonly fields: SearchState_$cases[Tag][1]) {
        super();
    }
    cases() {
        return ["Idle", "SearchDone"];
    }
}

function SearchState_$reflection(): TypeInfo {
    return union_type("Swate.Components.SearchState", [], SearchState, () => [[], [["Item", array_type(TermSearchResult_$reflection())]]]);
}

function SearchState_init(): SearchState_$union {
    return SearchState_Idle();
}

function SearchState__get_Results(this$: SearchState_$union): TermSearchResult[] {
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
    const pr: Promise<Term[]> = startAsPromise(SwateApi.searchTerm(TermQuery_create_Z6FBE353C(query, 10)));
    return pr.then((results: Term[]): Term[] => {
        const collection: Term[] = map_1<Term, Term>(Swate_Components_Shared_Database_Term__Term_ToComponentTerm, results);
        return Array.from(collection);
    });
}

function API_callParentSearch(parent: string, query: string): Promise<Term[]> {
    const pr: Promise<Term[]> = startAsPromise(SwateApi.searchTerm(TermQuery_create_Z6FBE353C(query, 10, parent)));
    return pr.then((results: Term[]): Term[] => {
        const collection: Term[] = map_1<Term, Term>(Swate_Components_Shared_Database_Term__Term_ToComponentTerm, results);
        return Array.from(collection);
    });
}

function API_callAllChildSearch(parent: string): Promise<Term[]> {
    const pr: Promise<ParentTermQueryResults> = startAsPromise(SwateApi.searchChildTerms(ParentTermQuery_create_3B406CA4(parent, 300)));
    return pr.then((results: ParentTermQueryResults): Term[] => {
        const collection: Term[] = map_1<Term, Term>(Swate_Components_Shared_Database_Term__Term_ToComponentTerm, results.results);
        return Array.from(collection);
    });
}

function API_callAdvancedSearch(dto: AdvancedSearchQuery): Promise<Term[]> {
    const pr: Promise<Term[]> = startAsPromise(SwateApi.searchTermAdvanced(dto));
    return pr.then((results: Term[]): Term[] => {
        const collection: Term[] = map_1<Term, Term>(Swate_Components_Shared_Database_Term__Term_ToComponentTerm, results);
        return Array.from(collection);
    });
}

function TermItem(termItemInputProps: any): ReactElement {
    let elems: Iterable<ReactElement>;
    const key: Option<string> = termItemInputProps.$key;
    const isActive: Option<boolean> = termItemInputProps.isActive;
    const onTermSelect: ((arg0: Option<Term>) => void) = termItemInputProps.onTermSelect;
    const term: TermSearchResult = termItemInputProps.term;
    const isObsolete: boolean = (term.Term.isObsolete != null) && value_66(term.Term.isObsolete);
    const isDirectedSearch: boolean = term.IsDirectedSearchResult;
    const activeClasses = "swt:bg-secondary swt:text-secondary-content";
    const ref: IRefValue$1<Option<HTMLElement>> = reactApi.useRef(undefined);
    const dependencies: any[] = [isActive];
    reactApi.useEffect((): void => {
        if ((isActive != null) && value_66(isActive)) {
            value_66(ref.current).scrollIntoView({
                block: "nearest",
            });
        }
    }, dependencies);
    return createElement<any>("li", createObj(ofArray([["ref", ref] as [string, any], ["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("swt:list-row swt:items-center swt:cursor-pointer swt:min-w-0 swt:max-w-full swt:w-full /\r\n                swt:hover:bg-secondary swt:hover:text-secondary-content swt:transition-colors /\r\n                swt:rounded-none"), delay_4<string>((): Iterable<string> => (((isActive != null) && value_66(isActive)) ? singleton<string>(activeClasses) : empty<string>()))))))] as [string, any], ["onClick", (e: MouseEvent): void => {
        e.stopPropagation();
        onTermSelect(term.Term);
    }] as [string, any], (elems = [createElement<any>("i", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(isObsolete ? singleton<IReactProperty>(["title", "Obsolete"] as [string, any]) : (isDirectedSearch ? singleton<IReactProperty>(["title", "Directed Search"] as [string, any]) : empty<IReactProperty>()), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("swt:w-5"), delay_4<string>((): Iterable<string> => (isObsolete ? singleton<string>("fa-solid fa-link-slash swt:text-error") : (isDirectedSearch ? singleton<string>("fa-solid fa-diagram-project swt:text-primary") : empty<string>())))))))] as [string, any]))))))), createElement<any>("span", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => {
        const name_2: string = defaultArg(term.Term.name, "<no-name>");
        return append<IReactProperty>(singleton<IReactProperty>(["title", name_2] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("swt:truncate swt:font-bold"), delay_4<string>((): Iterable<string> => (isObsolete ? singleton<string>("swt:line-through") : empty<string>()))))))] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", name_2] as [string, any])))));
    })))), createElement<any>("p", {
        className: "swt:list-col-wrap swt:text-xs swt:text-wrap",
        children: defaultArg(term.Term.description, "<no-description>"),
    }), createElement<any>("a", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => {
        const id: string = defaultArg(term.Term.id, "<no-id>");
        return append<IReactProperty>((term.Term.href != null) ? append<IReactProperty>(singleton<IReactProperty>(["onClick", (e_1: MouseEvent): void => {
            e_1.stopPropagation();
        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["target", "_blank"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["href", value_66(term.Term.href)] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["className", "swt:link swt:link-primary"] as [string, any]))))))) : empty<IReactProperty>(), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", id] as [string, any])));
    }))))], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
}

function NoResultsElement(advancedSearchToggle: Option<(() => void)>): ReactElement {
    let elems_1: Iterable<ReactElement>;
    return createElement<any>("div", createObj(ofArray([["className", "swt:gap-y-2 swt:py-2 swt:px-4"] as [string, any], (elems_1 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
        children: ["No terms found matching your input."],
    })), delay_4<ReactElement>((): Iterable<ReactElement> => {
        let elems: Iterable<ReactElement>;
        return append<ReactElement>((advancedSearchToggle != null) ? singleton<ReactElement>(createElement<any>("div", createObj(singleton_1((elems = [createElement<any>("span", {
            children: ["Can\'t find the term you are looking for? "],
        }), createElement<any>("a", {
            className: "swt:link swt:link-primary",
            onClick: (e: MouseEvent): void => {
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
                className: "swt:link swt:link-primary",
            }), createElement<any>("span", {
                children: [" with us!"],
            })]), createElement<any>("div", {
                children: reactApi.Children.toArray(Array.from(children)),
            })));
        }));
    })))), ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])])));
}

function TermDropdown(termDropdownRef: IRefValue$1<Option<HTMLElement>>, onTermSelect: ((arg0: Option<Term>) => void), state: SearchState_$union, loading: FSharpSet<string>, advancedSearchToggle: Option<(() => void)>, keyboardNavState: KeyboardNavigationController): ReactElement {
    let elems: Iterable<ReactElement>;
    return createElement<any>("ul", createObj(ofArray([["ref", termDropdownRef] as [string, any], ["style", {
        scrollbarGutter: "stable",
    }] as [string, any], ["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => (equals(state, SearchState_Idle()) ? singleton<string>("swt:hidden") : singleton<string>("swt:min-w-[400px] swt:not-prose swt:absolute swt:top-[100%] swt:left-0 swt:right-0 swt:z-50 swt:bg-base-200\r\n                    swt:rounded-sm swt:shadow-lg swt:border-2 swt:border-primary swt:max-h-[400px] swt:overflow-y-auto swt:list swt:[&_.swt\\:list-row]:!p-1")))))] as [string, any], (elems = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
        let searchResults: TermSearchResult[], searchResults_1: TermSearchResult[], searchResults_2: TermSearchResult[];
        return (state.tag === /* SearchDone */ 1) ? (((searchResults = state.fields[0], (searchResults.length === 0) && FSharpSet__get_IsEmpty(loading))) ? ((searchResults_1 = state.fields[0], singleton<ReactElement>(NoResultsElement(advancedSearchToggle)))) : ((searchResults_2 = state.fields[0], collect<int32, Iterable<ReactElement>, ReactElement>((i: int32): Iterable<ReactElement> => {
            const res: TermSearchResult = searchResults_2[i];
            const isActive: Option<boolean> = map_2<int32, boolean>((x: int32): boolean => (x === i), keyboardNavState.SelectedTermSearchResult);
            return singleton<ReactElement>(createElement(TermItem, {
                term: res,
                onTermSelect: onTermSelect,
                isActive: unwrap(isActive),
            }));
        }, rangeDouble(0, 1, searchResults_2.length - 1))))) : singleton<ReactElement>(defaultOf());
    })), ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
}

function IndicatorItem(indicatorPosition: string, tooltip: string, tooltipPosition: string, icon: string, onclick: ((arg0: MouseEvent) => void), btnClasses?: string, isActive?: boolean, props?: FSharpList<IReactProperty>): ReactElement {
    const isActive_1: boolean = defaultArg<boolean>(isActive, true);
    return createElement<any>("label", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>((props != null) ? value_66(props) : empty<IReactProperty>(), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("swt:indicator-item swt:text-sm swt:transition-[swt:/opacity] swt:opacity-0 swt:z-10"), delay_4<string>((): Iterable<string> => append<string>(singleton<string>(indicatorPosition), delay_4<string>((): Iterable<string> => (isActive_1 ? singleton<string>("swt:!opacity-100") : empty<string>()))))))))] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => {
        let elems_1: Iterable<ReactElement>, elems: Iterable<ReactElement>;
        return singleton<IReactProperty>((elems_1 = [createElement<any>("button", createObj(ofArray([["data-tip", tooltip] as [string, any], ["onClick", onclick] as [string, any], ["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("swt:btn swt:btn-square swt:btn-xs swt:px-2"), delay_4<string>((): Iterable<string> => append<string>(singleton<string>("swt:tooltip"), delay_4<string>((): Iterable<string> => append<string>(singleton<string>(tooltipPosition), delay_4<string>((): Iterable<string> => ((btnClasses != null) ? singleton<string>(value_66(btnClasses)) : empty<string>()))))))))))] as [string, any], (elems = [createElement<any>("i", {
            className: icon,
        })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any]));
    }))))))));
}

function DetailsModal(detailsModalInputProps: any): ReactElement {
    let elems_1: Iterable<ReactElement>, elems_3: Iterable<ReactElement>;
    const config: FSharpList<[string, string]> = detailsModalInputProps.config;
    const term: Option<Term> = detailsModalInputProps.term;
    const rvm: ((arg0: MouseEvent) => void) = detailsModalInputProps.rvm;
    const patternInput: [boolean, ((arg0: boolean) => void)] = reactApi.useState<boolean, boolean>(false);
    const showConfig: boolean = patternInput[0];
    const setShowConfig: ((arg0: boolean) => void) = patternInput[1];
    const label = (str: string): ReactElement => createElement<any>("div", {
        className: "swt:font-bold",
        children: str,
    });
    let termContent: ReactElement;
    if (term != null) {
        const term_1: Term = value_66(term);
        termContent = createElement<any>("div", createObj(ofArray([["className", "swt:grid swt:grid-cols-1 swt:md:grid-cols-[auto,1fr] swt:gap-4 swt:lg:gap-x-8"] as [string, any], (elems_1 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(label("Name")), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
            children: [defaultArg(term_1.name, "<no-name>")],
        })), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(label("Id")), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
            children: [defaultArg(term_1.id, "<no-id>")],
        })), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(label("Description")), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
            children: [defaultArg(term_1.description, "<no-description>")],
        })), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(label("Source")), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
            children: [defaultArg(term_1.source, "<no-source>")],
        })), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((term_1.data != null) ? append<ReactElement>(singleton<ReactElement>(label("Data")), delay_4<ReactElement>((): Iterable<ReactElement> => {
            let elems: Iterable<ReactElement>;
            return singleton<ReactElement>(createElement<any>("pre", createObj(ofArray([["className", "swt:text-xs"] as [string, any], (elems = [createElement<any>("code", {
                children: [JSON.stringify(value_66(term_1.data), undefined, some("\t"))],
            })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])]))));
        })) : empty<ReactElement>(), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(((term_1.isObsolete != null) && value_66(term_1.isObsolete)) ? singleton<ReactElement>(createElement<any>("div", {
            className: "swt:text-error",
            children: "obsolete",
        })) : empty<ReactElement>(), delay_4<ReactElement>((): Iterable<ReactElement> => ((term_1.href != null) ? singleton<ReactElement>(createElement<any>("a", {
            className: "swt:link swt:link-primary",
            href: value_66(term_1.href),
            target: "_blank",
            children: "Source Link",
        })) : empty<ReactElement>()))))))))))))))))))))))), ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])])));
    }
    else {
        termContent = createElement<any>("div", {
            children: "No term selected.",
        });
    }
    const componentConfig: ReactElement = createElement<any>("div", createObj(ofArray([["className", "swt:flex swt:flex-col swt:gap-4"] as [string, any], (elems_3 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => collect<[string, string], Iterable<ReactElement>, ReactElement>((matchValue: [string, string]): Iterable<ReactElement> => {
        let elems_2: Iterable<ReactElement>;
        const value_30: string = matchValue[1];
        const key_14: string = matchValue[0];
        return singleton<ReactElement>(createElement<any>("div", createObj(ofArray([["className", "swt:flex swt:flex-row swt:items-start swt:gap-4"] as [string, any], (elems_2 = [createElement<any>("label", {
            className: "swt:w-80 swt:font-bold",
            children: key_14,
        }), createElement<any>("div", {
            children: [value_30],
        })], ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any])]))));
    }, config))), ["children", reactApi.Children.toArray(Array.from(elems_3))] as [string, any])])));
    let content: ReactElement;
    const children: FSharpList<ReactElement> = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
        let elems_6: Iterable<ReactElement>;
        return showConfig ? append<ReactElement>(singleton<ReactElement>(createElement<any>("button", createObj(ofArray([["className", "swt:btn swt:btn-xs swt:btn-outline swt:mb-2"] as [string, any], ["onClick", (_arg_1: MouseEvent): void => {
            setShowConfig(!showConfig);
        }] as [string, any], (elems_6 = [createElement<any>("i", {
            className: "fa-solid fa-arrow-left",
        }), createElement<any>("span", {
            children: ["back"],
        })], ["children", reactApi.Children.toArray(Array.from(elems_6))] as [string, any])])))), delay_4<ReactElement>((): Iterable<ReactElement> => singleton<ReactElement>(componentConfig))) : append<ReactElement>(singleton<ReactElement>(termContent), delay_4<ReactElement>((): Iterable<ReactElement> => {
            let elems_5: Iterable<ReactElement>, elems_4: Iterable<ReactElement>;
            return singleton<ReactElement>(createElement<any>("div", createObj(ofArray([["className", "swt:w-full swt:flex swt:justify-end"] as [string, any], (elems_5 = [createElement<any>("button", createObj(ofArray([["className", "swt:btn swt:btn-primary swt:btn-xs"] as [string, any], ["onClick", (_arg: MouseEvent): void => {
                setShowConfig(!showConfig);
            }] as [string, any], (elems_4 = [createElement<any>("i", {
                className: "fa-solid fa-cog",
            })], ["children", reactApi.Children.toArray(Array.from(elems_4))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_5))] as [string, any])]))));
        }));
    }));
    content = createElement<any>("div", {
        children: reactApi.Children.toArray(Array.from(children)),
    });
    return createElement(BaseModal, {
        rmv: rvm,
        header: createElement<any>("div", {
            children: ["Details"],
        }),
        content: content,
    });
}

function AdvancedSearchDefault(advancedSearchState: AdvancedSearchQuery, setAdvancedSearchState: ((arg0: AdvancedSearchQuery) => void)): ((arg0: AdvancedSearchController) => ReactElement) {
    return (cc: AdvancedSearchController): ReactElement => {
        let elems: Iterable<ReactElement>, children: FSharpList<ReactElement>, elems_2: Iterable<ReactElement>, elems_1: Iterable<ReactElement>, elems_4: Iterable<ReactElement>, elems_3: Iterable<ReactElement>, elems_6: Iterable<ReactElement>, elems_5: Iterable<ReactElement>;
        const xs_14: Iterable<ReactElement> = [createElement<any>("div", createObj(ofArray([["className", "swt:prose"] as [string, any], (elems = [createElement<any>("p", {
            children: ["Use the following fields to search for terms."],
        }), (children = ofArray(["Name and Description fields follow Apache Lucene query syntax. ", createElement<any>("a", {
            href: "https://lucene.apache.org/core/2_9_4/queryparsersyntax.html",
            target: "_blank",
            children: "Learn more!",
            className: "swt:text-xs",
        })]), createElement<any>("p", {
            children: reactApi.Children.toArray(Array.from(children)),
        }))], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])]))), createElement<any>("label", createObj(ofArray([["className", "swt:w-full"] as [string, any], (elems_2 = [createElement<any>("div", createObj(ofArray([["className", "swt:label"] as [string, any], (elems_1 = [createElement<any>("span", {
            className: "swt:label-text",
            children: "Term Name",
        })], ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])]))), createElement<any>("input", createObj(ofArray([Feliz_prop__prop_testid_Static_Z721C83C5("advanced-search-term-name-input"), ["className", "swt:input swt:w-full"] as [string, any], ["type", "text"] as [string, any], ["autoFocus", true] as [string, any], ["value", advancedSearchState.TermName] as [string, any], ["onChange", (ev: Event): void => {
            setAdvancedSearchState(new AdvancedSearchQuery(advancedSearchState.OntologyName, ev.target.value, advancedSearchState.TermDefinition, advancedSearchState.KeepObsolete));
        }] as [string, any], ["onKeyDown", (ev_1: KeyboardEvent): void => {
            PropHelpers_createOnKey(key_enter, (_arg: KeyboardEvent): void => {
                cc.startSearch();
            }, ev_1);
        }] as [string, any]])))], ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any])]))), createElement<any>("label", createObj(ofArray([["className", "swt:form-control swt:w-full"] as [string, any], (elems_4 = [createElement<any>("div", createObj(ofArray([["className", "label"] as [string, any], (elems_3 = [createElement<any>("span", {
            className: "swt:label-text",
            children: "Term Description",
        })], ["children", reactApi.Children.toArray(Array.from(elems_3))] as [string, any])]))), createElement<any>("input", createObj(ofArray([Feliz_prop__prop_testid_Static_Z721C83C5("advanced-search-term-description-input"), ["className", "swt:input swt:w-full"] as [string, any], ["type", "text"] as [string, any], ["value", advancedSearchState.TermDefinition] as [string, any], ["onChange", (ev_2: Event): void => {
            setAdvancedSearchState(new AdvancedSearchQuery(advancedSearchState.OntologyName, advancedSearchState.TermName, ev_2.target.value, advancedSearchState.KeepObsolete));
        }] as [string, any], ["onKeyDown", (ev_3: KeyboardEvent): void => {
            PropHelpers_createOnKey(key_enter, (_arg_1: KeyboardEvent): void => {
                cc.startSearch();
            }, ev_3);
        }] as [string, any]])))], ["children", reactApi.Children.toArray(Array.from(elems_4))] as [string, any])]))), createElement<any>("div", createObj(ofArray([["className", "swt:form-control swt:max-w-xs"] as [string, any], (elems_6 = [createElement<any>("label", createObj(ofArray([["className", "swt:label swt:cursor-pointer"] as [string, any], (elems_5 = [createElement<any>("span", {
            className: "swt:label-text",
            children: "Keep Obsolete",
        }), createElement<any>("input", {
            className: "swt:checkbox",
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
    const advancedSearch0: AdvancedSearch | boolean = advancedSearchModalInputProps.advancedSearch0;
    const rmv: (() => void) = advancedSearchModalInputProps.rmv;
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
    let advancedSearch_1: AdvancedSearch;
    if (typeof advancedSearch0 === "boolean") {
        advancedSearch_1 = ({
            search: (): Promise<Term[]> => API_callAdvancedSearch(advancedSearchState),
            form: AdvancedSearchDefault(advancedSearchState, setAdvancedSearchState),
        });
    }
    else {
        const advancedSearch: AdvancedSearch = advancedSearch0;
        advancedSearch_1 = advancedSearch;
    }
    const BinSize = 20;
    let BinCount: int32;
    const dependencies_1: any[] = [searchResults];
    BinCount = reactApi.useMemo<int32>((): int32 => ~~(SearchState__get_Results(searchResults).length / BinSize), dependencies_1);
    const controller: AdvancedSearchController = {
        startSearch: (): void => {
            let pr_1: Promise<void>;
            const pr: Promise<Term[]> = advancedSearch_1.search();
            pr_1 = (pr.then((results: Term[]): void => {
                const results_1: TermSearchResult[] = map_1((t0: Term): TermSearchResult => (new TermSearchResult(t0, false)), results);
                setSearchResults(SearchState_SearchDone(results_1));
            }));
            pr_1.then();
        },
        cancel: rmv,
    };
    const dependencies_2: any[] = [pagination];
    reactApi.useEffect((): void => {
        setTempPagination(pagination + 1);
    }, dependencies_2);
    const searchFormComponent = (): ReactElement => {
        const xs_1: Iterable<ReactElement> = [advancedSearch_1.form(controller), createElement<any>("button", {
            className: "swt:btn swt:btn-primary",
            onClick: (_arg: MouseEvent): void => {
                controller.startSearch();
            },
            children: "Submit",
        })];
        return react.createElement(react.Fragment, {}, ...xs_1);
    };
    const resultsComponent = (results_2: TermSearchResult[]): ReactElement => {
        const xs_10: Iterable<ReactElement> = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
            let children: FSharpList<ReactElement>, fmt: any;
            return append<ReactElement>(singleton<ReactElement>((children = singleton_1(((fmt = printf("Results: %i"), fmt.cont((value_5: string): ReactElement => value_5)))(results_2.length)), createElement<any>("div", {
                children: reactApi.Children.toArray(Array.from(children)),
            }))), delay_4<ReactElement>((): Iterable<ReactElement> => {
                let elems: Iterable<ReactElement>;
                return append<ReactElement>(singleton<ReactElement>(createElement<any>("ul", createObj(ofArray([["className", "swt:max-h-[50%] swt:overflow-y-auto swt:list"] as [string, any], (elems = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => map<TermSearchResult, ReactElement>((res: TermSearchResult): ReactElement => {
                    const $key69EE7BC8: any = JSON.stringify(res);
                    return createElement(TermItem, {
                        term: res,
                        onTermSelect: onTermSelect,
                        key: $key69EE7BC8,
                        $key: $key69EE7BC8,
                    });
                }, getSubArray(results_2, pagination * BinSize, BinSize)))), ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])))), delay_4<ReactElement>((): Iterable<ReactElement> => {
                    let elems_1: Iterable<ReactElement>, value_17: int32, value_23: string;
                    return append<ReactElement>((BinCount > 1) ? singleton<ReactElement>(createElement<any>("div", createObj(ofArray([["className", "swt:join"] as [string, any], (elems_1 = [createElement<any>("input", createObj(ofArray([["className", "swt:input swt:join-item swt:grow"] as [string, any], ["type", "number"] as [string, any], ["min", 1] as [string, any], (value_17 = (defaultArg(tempPagination, pagination) | 0), ["ref", (e: Element): void => {
                        if (!(e == null) && !equals(e.value, value_17)) {
                            e.value = (value_17 | 0);
                        }
                    }] as [string, any]), ["max", BinCount] as [string, any], ["onChange", (ev: Event): void => {
                        const value_21: float64 = ev.target.valueAsNumber;
                        if (!(value_21 == null) && !Number.isNaN(value_21)) {
                            setTempPagination(min(max(round(value_21), 1), BinCount));
                        }
                    }] as [string, any]]))), createElement<any>("div", createObj(ofArray([(value_23 = "swt:input swt:join-item swt:shrink swt:flex swt:justify-center /\r\n                                    swt:items-center swt:cursor-not-allowed swt:border-l-0 swt:select-none", ["className", value_23] as [string, any]), ["type", "text"] as [string, any], ["children", `/${BinCount}`] as [string, any]]))), createElement<any>("button", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", "swt:btn swt:btn-primary swt:join-item"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => {
                        const disabled: boolean = (tempPagination == null) ? true : ((value_66(tempPagination) - 1) === pagination);
                        return append<IReactProperty>(singleton<IReactProperty>(["disabled", disabled] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onClick", (_arg_1: MouseEvent): void => {
                            iterate<int32>((arg: int32): void => {
                                setPagination(arg - 1);
                            }, toArray_1<int32>(tempPagination));
                        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", "Go"] as [string, any])))));
                    })))))), createElement<any>("button", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => {
                        const disabled_1: boolean = pagination === 0;
                        return append<IReactProperty>(singleton<IReactProperty>(["className", "swt:btn swt:join-item"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["disabled", disabled_1] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onClick", (_arg_2: MouseEvent): void => {
                            setPagination(pagination - 1);
                        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", "Previous"] as [string, any])))))));
                    })))), createElement<any>("button", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => {
                        const disabled_2: boolean = pagination === (BinCount - 1);
                        return append<IReactProperty>(singleton<IReactProperty>(["disabled", disabled_2] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", "swt:btn swt:join-item"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onClick", (_arg_3: MouseEvent): void => {
                            setPagination(pagination + 1);
                        }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", "Next"] as [string, any])))))));
                    }))))], ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])])))) : empty<ReactElement>(), delay_4<ReactElement>((): Iterable<ReactElement> => singleton<ReactElement>(createElement<any>("button", {
                        className: "swt:btn swt:btn-primary",
                        onClick: (_arg_4: MouseEvent): void => {
                            setSearchResults(SearchState_Idle());
                        },
                        children: "Back",
                    }))));
                }));
            }));
        }));
        return react.createElement(react.Fragment, {}, ...xs_10);
    };
    const content: ReactElement = createElement<any>("div", createObj(ofArray([["className", "swt:flex swt:flex-col swt:gap-2 swt:overflow-hidden swt:p-2"] as [string, any], (elems_2 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
        if (searchResults.tag === /* SearchDone */ 1) {
            const results_3: TermSearchResult[] = searchResults.fields[0];
            return singleton<ReactElement>(resultsComponent(results_3));
        }
        else {
            return singleton<ReactElement>(searchFormComponent());
        }
    })), ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any])])));
    return createElement(BaseModal, {
        rmv: rmv,
        header: createElement<any>("div", {
            children: ["Advanced Search"],
        }),
        content: content,
        debug: unwrap(map_2<boolean, string>((_arg_5: boolean): string => "advanced-search-modal", debug)),
    });
}

/**
 * Customizable react component for term search. Utilizing SwateDB search by default.
 */
export function TermSearch(termSearchInputProps: any): ReactElement {
    let elems: Iterable<ReactElement>;
    const props: Option<FSharpList<IReactProperty>> = termSearchInputProps.props;
    const classNames: Option<TermSearchStyle> = termSearchInputProps.classNames;
    const autoFocus: Option<boolean> = termSearchInputProps.autoFocus;
    const fullwidth: Option<boolean> = termSearchInputProps.fullwidth;
    const portalModals: Option<HTMLElement> = termSearchInputProps.portalModals;
    const portalTermDropdown: Option<PortalTermDropdown> = termSearchInputProps.portalTermDropdown;
    const disableDefaultAllChildrenSearch: Option<boolean> = termSearchInputProps.disableDefaultAllChildrenSearch;
    const disableDefaultParentSearch: Option<boolean> = termSearchInputProps.disableDefaultParentSearch;
    const disableDefaultSearch: Option<boolean> = termSearchInputProps.disableDefaultSearch;
    const debug: Option<boolean> = termSearchInputProps.debug;
    const showDetails: Option<boolean> = termSearchInputProps.showDetails;
    const onKeyDown: Option<((arg0: KeyboardEvent) => void)> = termSearchInputProps.onKeyDown;
    const onBlur: Option<(() => void)> = termSearchInputProps.onBlur;
    const onFocus: Option<(() => void)> = termSearchInputProps.onFocus;
    const advancedSearch: Option<AdvancedSearch | boolean> = termSearchInputProps.advancedSearch;
    const allChildrenSearchQueries: Option<[string, ((arg0: string) => Promise<Term[]>)][]> = termSearchInputProps.allChildrenSearchQueries;
    const parentSearchQueries: Option<[string, ((arg0: [string, string]) => Promise<Term[]>)][]> = termSearchInputProps.parentSearchQueries;
    const termSearchQueries: Option<[string, ((arg0: string) => Promise<Term[]>)][]> = termSearchInputProps.termSearchQueries;
    const parentId: Option<string> = termSearchInputProps.parentId;
    const term: Option<Term> = termSearchInputProps.term;
    const onTermSelect: ((arg0: Option<Term>) => void) = termSearchInputProps.onTermSelect;
    const showDetails_1: boolean = defaultArg<boolean>(showDetails, false);
    const debug_1: boolean = defaultArg<boolean>(debug, false);
    const fullwidth_1: boolean = defaultArg<boolean>(fullwidth, false);
    const autoFocus_1: boolean = defaultArg<boolean>(autoFocus, false);
    const patternInput: [KeyboardNavigationController, ((arg0: KeyboardNavigationController) => void)] = reactApi.useState<(() => KeyboardNavigationController), KeyboardNavigationController>(KeyboardNavigationController_init);
    const setKeyboardNavState: ((arg0: KeyboardNavigationController) => void) = patternInput[1];
    const keyboardNavState: KeyboardNavigationController = patternInput[0];
    const patternInput_1: [SearchState_$union, ((arg0: ((arg0: SearchState_$union) => SearchState_$union)) => void)] = useState<SearchState_$union>(SearchState_init());
    const setSearchResults: ((arg0: ((arg0: SearchState_$union) => SearchState_$union)) => void) = patternInput_1[1];
    const searchResults: SearchState_$union = patternInput_1[0];
    const patternInput_2: [FSharpSet<string>, ((arg0: ((arg0: FSharpSet<string>) => FSharpSet<string>)) => void)] = useState<FSharpSet<string>>(empty_1<string>({
        Compare: comparePrimitives,
    }));
    const setLoading: ((arg0: ((arg0: FSharpSet<string>) => FSharpSet<string>)) => void) = patternInput_2[1];
    const loading: FSharpSet<string> = patternInput_2[0];
    const inputRef: IRefValue$1<Option<HTMLInputElement>> = reactApi.useRef(undefined);
    const containerRef: IRefValue$1<Option<HTMLElement>> = reactApi.useRef(undefined);
    const termDropdownRef: IRefValue$1<Option<HTMLElement>> = reactApi.useRef(undefined);
    const modalContainerRef: IRefValue$1<Option<HTMLElement>> = reactApi.useRef(undefined);
    const patternInput_3: [boolean, ((arg0: boolean) => void)] = reactApi.useState<boolean, boolean>(false);
    const setFocused: ((arg0: boolean) => void) = patternInput_3[1];
    const focused: boolean = patternInput_3[0];
    const cancelled: IRefValue$1<boolean> = reactApi.useRef(false);
    const patternInput_4: [Option<Modals_$union>, ((arg0: Option<Modals_$union>) => void)] = reactApi.useState<Option<Modals_$union>, Option<Modals_$union>>(undefined);
    const setModal: ((arg0: Option<Modals_$union>) => void) = patternInput_4[1];
    const modal: Option<Modals_$union> = patternInput_4[0];
    const inputText: string = defaultArg(bind<Term, string>((_arg: Term): Option<string> => _arg.name, term), "");
    useLayoutEffectWithDeps((_arg_1: any): IDisposable => {
        if (inputRef.current != null) {
            value_66(inputRef.current).value = inputText;
        }
        return createDisposable((): void => {
        });
    }, [term]);
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
    const createParentChildTermSearch = (id_2: string): ((arg0: ((arg0: [string, string]) => Promise<Term[]>)) => ((arg0: [string, string]) => Promise<void>)) => ((search_1: ((arg0: [string, string]) => Promise<Term[]>)): ((arg0: [string, string]) => Promise<void>) => {
        const id_3: string = "PC_" + id_2;
        return (tupledArg: [string, string]): Promise<void> => {
            const parentId_1: string = tupledArg[0];
            const query_1: string = tupledArg[1];
            return PromiseBuilder__Run_212F1D4B<void>(promise, PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => {
                startLoadingBy(id_3);
                return search_1([parentId_1, query_1] as [string, string]).then((_arg_5: Term[]): Promise<void> => {
                    const termSearchResults_2: Term[] = _arg_5;
                    const termSearchResults_3: TermSearchResult[] = map_1((t0_1: Term): TermSearchResult => (new TermSearchResult(t0_1, true)), termSearchResults_2);
                    return (!cancelled.current ? ((setSearchResults((prevResults_2: SearchState_$union): SearchState_$union => SearchState_SearchDone(TermSearchResult_addSearchResults(SearchState__get_Results(prevResults_2), termSearchResults_3))), Promise.resolve())) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => {
                        stopLoadingBy(id_3);
                        return Promise.resolve();
                    }));
                });
            }));
        };
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
        let pr_1: Promise<void[]>;
        const pr: FSharpList<Promise<void>> = toList<Promise<void>>(delay_4<Promise<void>>((): Iterable<Promise<void>> => append<Promise<void>>(((disableDefaultSearch != null) && value_66(disableDefaultSearch)) ? (empty<Promise<void>>()) : singleton<Promise<void>>(createTermSearch("DEFAULT_SIMPLE")(API_callSearch)(query_2)), delay_4<Promise<void>>((): Iterable<Promise<void>> => ((termSearchQueries != null) ? collect<[string, ((arg0: string) => Promise<Term[]>)], Iterable<Promise<void>>, Promise<void>>((matchValue: [string, ((arg0: string) => Promise<Term[]>)]): Iterable<Promise<void>> => {
            const termSearch: ((arg0: string) => Promise<Term[]>) = matchValue[1];
            const id_6: string = matchValue[0];
            return singleton<Promise<void>>(createTermSearch(id_6)(termSearch)(query_2));
        }, value_66(termSearchQueries)) : empty<Promise<void>>())))));
        pr_1 = (Promise.all(pr));
        pr_1.then();
    };
    const parentSearch_1 = (query_4: string): void => {
        let pr_3: Promise<void[]>;
        const pr_2: FSharpList<Promise<void>> = toList<Promise<void>>(delay_4<Promise<void>>((): Iterable<Promise<void>> => ((parentId != null) ? append<Promise<void>>(((disableDefaultParentSearch != null) && value_66(disableDefaultParentSearch)) ? (empty<Promise<void>>()) : singleton<Promise<void>>(createParentChildTermSearch("DEFAULT_PARENTCHILD")((tupledArg_1: [string, string]): Promise<Term[]> => API_callParentSearch(tupledArg_1[0], tupledArg_1[1]))([value_66(parentId), query_4] as [string, string])), delay_4<Promise<void>>((): Iterable<Promise<void>> => ((parentSearchQueries != null) ? collect<[string, ((arg0: [string, string]) => Promise<Term[]>)], Iterable<Promise<void>>, Promise<void>>((matchValue_1: [string, ((arg0: [string, string]) => Promise<Term[]>)]): Iterable<Promise<void>> => {
            const parentSearch: ((arg0: [string, string]) => Promise<Term[]>) = matchValue_1[1];
            const id_7: string = matchValue_1[0];
            return singleton<Promise<void>>(createParentChildTermSearch(id_7)(parentSearch)([value_66(parentId), query_4] as [string, string]));
        }, value_66(parentSearchQueries)) : empty<Promise<void>>()))) : empty<Promise<void>>())));
        pr_3 = (Promise.all(pr_2));
        pr_3.then();
    };
    const allChildSearch_1 = (): void => {
        let pr_5: Promise<void[]>;
        const pr_4: FSharpList<Promise<void>> = toList<Promise<void>>(delay_4<Promise<void>>((): Iterable<Promise<void>> => ((parentId != null) ? append<Promise<void>>(((disableDefaultAllChildrenSearch != null) && value_66(disableDefaultAllChildrenSearch)) ? (empty<Promise<void>>()) : singleton<Promise<void>>(createAllChildTermSearch("DEFAULT_ALLCHILD")(API_callAllChildSearch)(value_66(parentId))), delay_4<Promise<void>>((): Iterable<Promise<void>> => ((allChildrenSearchQueries != null) ? collect<[string, ((arg0: string) => Promise<Term[]>)], Iterable<Promise<void>>, Promise<void>>((matchValue_2: [string, ((arg0: string) => Promise<Term[]>)]): Iterable<Promise<void>> => {
            const id_8: string = matchValue_2[0];
            const allChildSearch: ((arg0: string) => Promise<Term[]>) = matchValue_2[1];
            return singleton<Promise<void>>(createAllChildTermSearch(id_8)(allChildSearch)(value_66(parentId)));
        }, value_66(allChildrenSearchQueries)) : empty<Promise<void>>()))) : empty<Promise<void>>())));
        pr_5 = (Promise.all(pr_4));
        pr_5.then();
    };
    let patternInput_5: [(() => void), ((arg0: string) => void)];
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
    const dependencies_1_1: any[] = [func, delay_1];
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
    }, dependencies_1_1);
    const cancel: (() => void) = reactApi.useCallback<void, void>((): void => {
        if (timeout.current != null) {
            clearTimeout(value_66(timeout.current));
            iterate<(() => void)>((f_2: (() => void)): void => {
                f_2();
            }, toArray_1<(() => void)>(stopDebounceLoading));
        }
    }, []);
    patternInput_5 = ([cancel, debouncedCallBack] as [(() => void), ((arg0: string) => void)]);
    const search_3: ((arg0: string) => void) = patternInput_5[1];
    const cancelSearch: (() => void) = patternInput_5[0];
    let patternInput_6: [(() => void), ((arg0: string) => void)];
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
    const dependencies_1_3: any[] = [func_1, delay_1_1];
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
    }, dependencies_1_3);
    const cancel_1: (() => void) = reactApi.useCallback<void, void>((): void => {
        if (timeout_1.current != null) {
            clearTimeout(value_66(timeout_1.current));
            iterate<(() => void)>((f_2_1: (() => void)): void => {
                f_2_1();
            }, toArray_1<(() => void)>(stopDebounceLoading_1));
        }
    }, []);
    patternInput_6 = ([cancel_1, debouncedCallBack_1] as [(() => void), ((arg0: string) => void)]);
    const parentSearch_2: ((arg0: string) => void) = patternInput_6[1];
    const cancelParentSearch: (() => void) = patternInput_6[0];
    let patternInput_7: [(() => void), (() => void)];
    const func_2: (() => void) = allChildSearch_1;
    const timeout_2: IRefValue$1<Option<int32>> = reactApi.useRef(undefined);
    const delay_1_2 = 0;
    let debouncedCallBack_2: (() => void);
    const dependencies_1_5: any[] = [func_2, delay_1_2];
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
    }, dependencies_1_5);
    const cancel_2: (() => void) = reactApi.useCallback<void, void>((): void => {
        if (timeout_2.current != null) {
            clearTimeout(value_66(timeout_2.current));
            iterate<(() => void)>((f_2_2: (() => void)): void => {
                f_2_2();
            }, toArray_1<(() => void)>(undefined));
        }
    }, []);
    patternInput_7 = ([cancel_2, debouncedCallBack_2] as [(() => void), (() => void)]);
    const cancelAllChildSearch: (() => void) = patternInput_7[0];
    const allChildSearch_2: (() => void) = patternInput_7[1];
    const cancel_3 = (): void => {
        setSearchResults((_arg_7: SearchState_$union): SearchState_$union => SearchState_init());
        cancelled.current = true;
        setLoading((_arg_8: FSharpSet<string>): FSharpSet<string> => empty_1<string>({
            Compare: comparePrimitives,
        }));
        cancelSearch();
        cancelParentSearch();
        cancelAllChildSearch();
        setKeyboardNavState(KeyboardNavigationController_init());
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
    const action_6 = (e: MouseEvent): void => {
        if (focused) {
            const refs: HTMLElement[] = toArray<HTMLElement>(delay_4<HTMLElement>((): Iterable<HTMLElement> => append<HTMLElement>((containerRef.current != null) ? singleton<HTMLElement>(value_66(containerRef.current)) : empty<HTMLElement>(), delay_4<HTMLElement>((): Iterable<HTMLElement> => append<HTMLElement>((modalContainerRef.current != null) ? singleton<HTMLElement>(value_66(modalContainerRef.current)) : empty<HTMLElement>(), delay_4<HTMLElement>((): Iterable<HTMLElement> => ((termDropdownRef.current != null) ? singleton<HTMLElement>(value_66(termDropdownRef.current)) : empty<HTMLElement>())))))));
            const refsContain: boolean = refs.every((el: HTMLElement): boolean => !el.contains(e.target));
            if (refsContain) {
                setFocused(false);
                setSearchResults((_arg_11: SearchState_$union): SearchState_$union => SearchState_init());
                if (onBlur != null) {
                    value_66(onBlur)();
                }
                cancel_3();
            }
        }
    };
    const options_1: Option<any> = undefined;
    const dependencies_8: Option<any[]> = undefined;
    let addOptions: Option<any>;
    const dependencies_1_7: any[] = [options_1];
    addOptions = reactApi.useMemo<Option<any>>((): Option<any> => Impl_adjustPassive(options_1), dependencies_1_7);
    let removeOptions: Option<any>;
    const dependencies_1_8: any[] = [options_1];
    removeOptions = reactApi.useMemo<Option<any>>((): Option<any> => Impl_createRemoveOptions(options_1), dependencies_1_8);
    let fn: ((arg0: Event) => void);
    const dependencies_1_9: any[] = toArray<any>(delay_4<any>((): Iterable<any> => append<any>(singleton<any>(action_6), delay_4<any>((): Iterable<any> => ((dependencies_8 != null) ? value_66(dependencies_8) : empty<any>())))));
    fn = reactApi.useMemo<((arg0: Event) => void)>((): ((arg0: Event) => void) => ((arg_3: Event): void => {
        action_6(arg_3);
    }), dependencies_1_9);
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
                const options_2: any = value_66(removeOptions);
                document.removeEventListener("mousedown", fn, options_2);
            }
        });
    });
    useEffect(listener);
    const keyboardNav = (e_1: KeyboardEvent): Promise<void> => PromiseBuilder__Run_212F1D4B<void>(promise, PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => {
        let matchValue_5: Option<int32>, i: int32, matchValue_6: Option<int32>, i_1: int32, res: TermSearchResult[], res_1: TermSearchResult[], res_2: TermSearchResult[];
        if (focused) {
            const matchValue_3: string = e_1.code;
            let matchResult: int32, res_3: TermSearchResult[], res_4: TermSearchResult[], res_5: TermSearchResult[];
            switch (matchValue_3) {
                case "Escape": {
                    matchResult = 0;
                    break;
                }
                case "ArrowUp": {
                    if (searchResults.tag === /* SearchDone */ 1) {
                        if ((res = searchResults.fields[0], res.length > 0)) {
                            matchResult = 1;
                            res_3 = searchResults.fields[0];
                        }
                        else {
                            matchResult = 5;
                        }
                    }
                    else {
                        matchResult = 5;
                    }
                    break;
                }
                case "ArrowDown": {
                    if (searchResults.tag === /* Idle */ 0) {
                        if ((inputRef.current != null) && !isNullOrWhiteSpace(value_66(inputRef.current).value)) {
                            matchResult = 3;
                        }
                        else {
                            matchResult = 5;
                        }
                    }
                    else if ((res_1 = searchResults.fields[0], res_1.length > 0)) {
                        matchResult = 2;
                        res_4 = searchResults.fields[0];
                    }
                    else {
                        matchResult = 5;
                    }
                    break;
                }
                case "Enter": {
                    if (searchResults.tag === /* SearchDone */ 1) {
                        if ((res_2 = searchResults.fields[0], keyboardNavState.SelectedTermSearchResult != null)) {
                            matchResult = 4;
                            res_5 = searchResults.fields[0];
                        }
                        else {
                            matchResult = 5;
                        }
                    }
                    else {
                        matchResult = 5;
                    }
                    break;
                }
                default:
                    matchResult = 5;
            }
            switch (matchResult) {
                case 0: {
                    cancel_3();
                    return Promise.resolve();
                }
                case 1: {
                    setKeyboardNavState(new KeyboardNavigationController((matchValue_5 = keyboardNavState.SelectedTermSearchResult, (matchValue_5 != null) ? ((value_66(matchValue_5) === 0) ? undefined : ((i = (value_66(matchValue_5) | 0), max(i - 1, 0)))) : undefined)));
                    return Promise.resolve();
                }
                case 2: {
                    setKeyboardNavState(new KeyboardNavigationController((matchValue_6 = keyboardNavState.SelectedTermSearchResult, (matchValue_6 != null) ? ((i_1 = (value_66(matchValue_6) | 0), min(i_1 + 1, SearchState__get_Results(searchResults).length - 1))) : 0)));
                    return Promise.resolve();
                }
                case 3: {
                    startSearch(value_66(inputRef.current).value);
                    return Promise.resolve();
                }
                case 4: {
                    onTermSelect_1(res_5![value_66(keyboardNavState.SelectedTermSearchResult)].Term);
                    cancel_3();
                    return Promise.resolve();
                }
                default: {
                    return Promise.resolve();
                }
            }
        }
        else {
            return Promise.resolve();
        }
    }));
    let modalContainer: ReactElement;
    const configDetails: FSharpList<[string, string]> = reverse<[string, string]>(fold<[string, Option<string>], FSharpList<[string, string]>>((acc: FSharpList<[string, string]>, tupledArg_2: [string, Option<string>]): FSharpList<[string, string]> => {
        const key_4: string = tupledArg_2[0];
        const value_8: Option<string> = tupledArg_2[1];
        if (value_8 != null) {
            const value_9: string = value_66(value_8);
            return cons([key_4, value_9] as [string, string], acc);
        }
        else {
            return acc;
        }
    }, empty_2<[string, string]>(), ofArray([["Parent Id", parentId] as [string, Option<string>], ["Disable Default Search", map_2<boolean, string>(toString, disableDefaultSearch)] as [string, Option<string>], ["Disable Default Parent Search", map_2<boolean, string>(toString, disableDefaultParentSearch)] as [string, Option<string>], ["Disable Default All Children Search", map_2<boolean, string>(toString, disableDefaultAllChildrenSearch)] as [string, Option<string>], ["Custom Term Search Queries", map_2<[string, ((arg0: string) => Promise<Term[]>)][], string>((arg_4: [string, ((arg0: string) => Promise<Term[]>)][]): string => join("; ", map<[string, ((arg0: string) => Promise<Term[]>)], string>((tuple: [string, ((arg0: string) => Promise<Term[]>)]): string => tuple[0], arg_4)), termSearchQueries)] as [string, Option<string>], ["Custom Parent Search Queries", map_2<[string, ((arg0: [string, string]) => Promise<Term[]>)][], string>((arg_5: [string, ((arg0: [string, string]) => Promise<Term[]>)][]): string => join("; ", map<[string, ((arg0: [string, string]) => Promise<Term[]>)], string>((tuple_1: [string, ((arg0: [string, string]) => Promise<Term[]>)]): string => tuple_1[0], arg_5)), parentSearchQueries)] as [string, Option<string>], ["Custom All Children Search Queries", map_2<[string, ((arg0: string) => Promise<Term[]>)][], string>((arg_6: [string, ((arg0: string) => Promise<Term[]>)][]): string => join("; ", map<[string, ((arg0: string) => Promise<Term[]>)], string>((tuple_2: [string, ((arg0: string) => Promise<Term[]>)]): string => tuple_2[0], arg_6)), allChildrenSearchQueries)] as [string, Option<string>], ["Advanced Search", map_2<AdvancedSearch | boolean, string>((_arg_12: AdvancedSearch | boolean): string => ((typeof _arg_12 === "boolean") ? "Default" : "Custom"), advancedSearch)] as [string, Option<string>]])));
    modalContainer = createElement<any>("div", createObj(ofArray([["className", "swt:z-[9999] swt:fixed swt:w-screen swt:h-screen swt:pointer-events-none"] as [string, any], ["ref", modalContainerRef] as [string, any], (elems = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
        let matchResult_1: int32;
        if (modal != null) {
            if (value_66(modal).tag === /* AdvancedSearch */ 0) {
                if (advancedSearch != null) {
                    matchResult_1 = 1;
                }
                else {
                    matchResult_1 = 2;
                }
            }
            else {
                matchResult_1 = 0;
            }
        }
        else {
            matchResult_1 = 2;
        }
        switch (matchResult_1) {
            case 0:
                return singleton<ReactElement>(createElement(DetailsModal, {
                    rvm: (_arg_13: MouseEvent): void => {
                        setModal_1(undefined);
                    },
                    term: unwrap(term),
                    config: configDetails,
                }));
            case 1: {
                const onTermSelect_2 = (term_2: Option<Term>): void => {
                    onTermSelect_1(term_2);
                    setModal_1(undefined);
                };
                return singleton<ReactElement>(createElement(AdvancedSearchModal, {
                    rmv: (): void => {
                        setModal_1(undefined);
                    },
                    advancedSearch0: value_66(advancedSearch),
                    onTermSelect: onTermSelect_2,
                    debug: debug_1,
                }));
            }
            default:
                return singleton<ReactElement>(defaultOf());
        }
    })), ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
    return createElement<any>("div", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(debug_1 ? append<IReactProperty>(singleton<IReactProperty>(["data-testid", "term-search-container"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["data-debug-loading", JSON.stringify(loading)] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["data-debug-searchresults", JSON.stringify(searchResults)] as [string, any]))))) : empty<IReactProperty>(), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("swt:form-control swt:not-prose swt:h-full"), delay_4<string>((): Iterable<string> => (fullwidth_1 ? singleton<string>("swt:w-full") : empty<string>()))))))] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>((props != null) ? map<IReactProperty, IReactProperty>((prop: IReactProperty): IReactProperty => prop, value_66(props)) : empty<IReactProperty>(), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["ref", containerRef] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => {
        let elems_3: Iterable<ReactElement>;
        return singleton<IReactProperty>((elems_3 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((portalModals != null) ? singleton<ReactElement>(createPortal(modalContainer, value_66(portalModals))) : singleton<ReactElement>(modalContainer), delay_4<ReactElement>((): Iterable<ReactElement> => {
            let elems_2: Iterable<ReactElement>;
            return singleton<ReactElement>(createElement<any>("div", createObj(ofArray([["className", "swt:indicator swt:w-full swt:h-full"] as [string, any], (elems_2 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => {
                let matchValue_7: Option<Term>, term_3: Term, term_4: Term, arg_7: string, arg_8: string;
                return append<ReactElement>((matchValue_7 = term, (matchValue_7 != null) ? (((term_3 = value_66(matchValue_7), (term_3.name != null) && (term_3.id != null))) ? ((term_4 = value_66(matchValue_7), !isNullOrWhiteSpace(value_66(term_4.id)) ? singleton<ReactElement>(IndicatorItem("", (arg_7 = value_66(term_4.name), (arg_8 = value_66(term_4.id), toText(printf("%s - %s"))(arg_7)(arg_8))), "swt:tooltip-left", "fa-solid fa-square-check", (_arg_15: MouseEvent): void => {
                    setModal_1(((modal != null) && equals(value_66(modal), Modals_Details())) ? undefined : Modals_Details());
                }, "swt:btn-primary")) : empty<ReactElement>())) : (showDetails_1 ? singleton<ReactElement>(IndicatorItem("", "Details", "swt:tooltip-left", "fa-solid fa-circle-info", (_arg_16: MouseEvent): void => {
                    setModal_1(((modal != null) && equals(value_66(modal), Modals_Details())) ? undefined : Modals_Details());
                }, "swt:btn-info", focused)) : singleton<ReactElement>(defaultOf()))) : (showDetails_1 ? singleton<ReactElement>(IndicatorItem("", "Details", "swt:tooltip-left", "fa-solid fa-circle-info", (_arg_16: MouseEvent): void => {
                    setModal_1(((modal != null) && equals(value_66(modal), Modals_Details())) ? undefined : Modals_Details());
                }, "swt:btn-info", focused)) : singleton<ReactElement>(defaultOf()))), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((advancedSearch != null) ? singleton<ReactElement>(IndicatorItem("swt:indicator-bottom", "Advanced Search", "swt:tooltip-left", "fa-solid fa-magnifying-glass-plus", (_arg_17: MouseEvent): void => {
                    setModal_1(((modal != null) && equals(value_66(modal), Modals_AdvancedSearch())) ? undefined : Modals_AdvancedSearch());
                }, "swt:btn-primary", focused, singleton_1(Feliz_prop__prop_testid_Static_Z721C83C5("advanced-search-indicator")))) : empty<ReactElement>(), delay_4<ReactElement>((): Iterable<ReactElement> => {
                    let elems_1: Iterable<ReactElement>;
                    return singleton<ReactElement>(createElement<any>("label", createObj(ofArray([["className", join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("swt:input swt:flex swt:flex-row swt:items-center swt:relative swt:w-full"), delay_4<string>((): Iterable<string> => (((classNames != null) && (value_66(classNames).inputLabel != null)) ? singleton<string>(TermSearchStyleModule_resolveStyle(value_66(value_66(classNames).inputLabel))) : empty<string>()))))))] as [string, any], (elems_1 = toList<ReactElement>(delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("i", {
                        className: join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("fa-solid fa-search swt:text-primary swt:pr-2 swt:transition-all swt:w-6 swt:overflow-x-hidden swt:opacity-100"), delay_4<string>((): Iterable<string> => ((focused ? true : ((inputRef.current != null) && !isNullOrEmpty(value_66(inputRef.current).value))) ? singleton<string>("swt:!w-0 swt:!opacity-0") : empty<string>())))))),
                    })), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("input", createObj(toList<IReactProperty>(delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", "swt:grow swt:shrink swt:min-w-[50px] swt:w-full"] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(debug_1 ? singleton<IReactProperty>(Feliz_prop__prop_testid_Static_Z721C83C5("term-search-input")) : empty<IReactProperty>(), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["ref", inputRef] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["defaultValue", inputText] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["placeholder", "..."] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["autoFocus", autoFocus_1] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onChange", (ev: Event): void => {
                        const e_2: string = ev.target.value;
                        if (isNullOrEmpty(e_2)) {
                            onTermSelect_1(undefined);
                            cancel_3();
                        }
                        else {
                            onTermSelect_1({
                                name: e_2,
                            });
                            startSearch(e_2);
                        }
                    }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onDoubleClick", (_arg_18: MouseEvent): void => {
                        if ((parentId != null) && isNullOrEmpty(value_66(inputRef.current).value)) {
                            startAllChildSearch();
                        }
                        else if (!isNullOrEmpty(value_66(inputRef.current).value)) {
                            startSearch(value_66(inputRef.current).value);
                        }
                    }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onKeyDown", (e_3: KeyboardEvent): void => {
                        e_3.stopPropagation();
                        const pr_6: Promise<void> = PromiseBuilder__Run_212F1D4B<void>(promise, PromiseBuilder__Delay_62FBFDE1<void>(promise, (): Promise<void> => (keyboardNav(e_3).then((): Promise<void> => {
                            if (onKeyDown != null) {
                                value_66(onKeyDown)(e_3);
                                return Promise.resolve();
                            }
                            else {
                                return Promise.resolve();
                            }
                        }))));
                        pr_6.then();
                    }] as [string, any]), delay_4<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["onFocus", (_arg_20: FocusEvent): void => {
                        if (onFocus != null) {
                            value_66(onFocus)();
                        }
                        setFocused(true);
                    }] as [string, any])))))))))))))))))))))))), delay_4<ReactElement>((): Iterable<ReactElement> => append<ReactElement>(singleton<ReactElement>(createElement<any>("div", {
                        className: join(" ", toList<string>(delay_4<string>((): Iterable<string> => append<string>(singleton<string>("swt:loading swt:text-primary swt:loading-sm"), delay_4<string>((): Iterable<string> => (FSharpSet__get_IsEmpty(loading) ? singleton<string>("swt:invisible") : empty<string>())))))),
                    })), delay_4<ReactElement>((): Iterable<ReactElement> => {
                        let portalTermSelectArea: PortalTermDropdown;
                        const advancedSearchToggle: Option<(() => void)> = map_2<AdvancedSearch | boolean, (() => void)>((_arg_21: AdvancedSearch | boolean): (() => void) => ((): void => {
                            setModal_1(((modal != null) && equals(value_66(modal), Modals_AdvancedSearch())) ? undefined : Modals_AdvancedSearch());
                        }), advancedSearch);
                        const matchValue_8: Option<PortalTermDropdown> = portalTermDropdown;
                        let matchResult_2: int32, portalTermSelectArea_1: PortalTermDropdown;
                        if (matchValue_8 != null) {
                            if ((portalTermSelectArea = value_66(matchValue_8), containerRef.current != null)) {
                                matchResult_2 = 0;
                                portalTermSelectArea_1 = value_66(matchValue_8);
                            }
                            else {
                                matchResult_2 = 1;
                            }
                        }
                        else {
                            matchResult_2 = 1;
                        }
                        switch (matchResult_2) {
                            case 0:
                                return singleton<ReactElement>(createPortal(portalTermSelectArea_1!.renderer(value_66(containerRef.current).getBoundingClientRect(), TermDropdown(termDropdownRef, onTermSelect_1, searchResults, loading, advancedSearchToggle, keyboardNavState)), portalTermSelectArea_1!.portal));
                            default:
                                return singleton<ReactElement>(TermDropdown(termDropdownRef, onTermSelect_1, searchResults, loading, advancedSearchToggle, keyboardNavState));
                        }
                    })))))))), ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])]))));
                }))));
            })), ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any])]))));
        })))), ["children", reactApi.Children.toArray(Array.from(elems_3))] as [string, any]));
    }))))))))))));
}

export default TermSearch;

