import { Remoting_buildProxy_64DC51C } from "../fable_modules/Fable.Remoting.Client.7.32.0/./Remoting.fs.js";
import { RemotingModule_createApi, RemotingModule_withRouteBuilder } from "../fable_modules/Fable.Remoting.Client.7.32.0/Remoting.fs.js";
import { IOntologyAPIv3, IOntologyAPIv3_$reflection, Route_builder } from "../../../Shared/Shared.fs.js";
import { toString } from "../fable_modules/fable-library-ts.4.24.0/Types.js";
import { join } from "../fable_modules/fable-library-ts.4.24.0/String.js";
import { ofArray, singleton, FSharpList, map } from "../fable_modules/fable-library-ts.4.24.0/List.js";
import { int32 } from "../fable_modules/fable-library-ts.4.24.0/Int32.js";
import { tryFind, map as map_1 } from "../fable_modules/fable-library-ts.4.24.0/Array.js";
import { unwrap, value as value_2, Option, map as map_2, defaultArg } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { Types_Response, Types_RequestProperties_Headers, Types_IHttpRequestHeaders, Types_RequestProperties_Method, fetch$ } from "../fable_modules/Fable.Fetch.2.7.0/Fetch.fs.js";
import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../fable_modules/Fable.Promise.2.2.2/Promise.fs.js";
import { promise } from "../fable_modules/Fable.Promise.2.2.2/PromiseImpl.fs.js";
import { empty, singleton as singleton_1, append, delay, toList } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { class_type, TypeInfo } from "../fable_modules/fable-library-ts.4.24.0/Reflection.js";

export const SwateApi: IOntologyAPIv3 = Remoting_buildProxy_64DC51C<IOntologyAPIv3>(RemotingModule_withRouteBuilder(Route_builder, RemotingModule_createApi()), IOntologyAPIv3_$reflection());

function makeQueryParam(name: string, value: any): string {
    return (encodeURIComponent(name) + "=") + encodeURIComponent(toString(value));
}

function makeQueryParamStr(queryParams: FSharpList<[string, any]>): string {
    return "?" + join("&", map<[string, any], string>((tupledArg: [string, any]): string => {
        const name: string = tupledArg[0];
        const value: any = tupledArg[1];
        return makeQueryParam(name, value);
    }, queryParams));
}

function appendQueryParams(url: string, queryParams: FSharpList<[string, any]>): string {
    return url + makeQueryParamStr(queryParams);
}

export interface TIBTypes_Term {
    description: string[],
    hasChildren?: boolean,
    iri: string,
    is_obsolete?: boolean,
    label: string,
    lang: string,
    obo_id: string,
    ontology_iri: string,
    ontology_name: string,
    ontology_prefix?: string,
    short_form: string,
    synonyms: string[],
    term_replaced_by?: string
}

export interface TIBTypes_TermArray {
    terms: TIBTypes_Term[]
}

export interface TIBTypes_TermApi {
    _embedded: TIBTypes_TermArray
}

export interface TIBTypes_SearchResults {
    docs: TIBTypes_Term[],
    numFound: int32,
    start: int32
}

export interface TIBTypes_SearchApi {
    response: TIBTypes_SearchResults
}

export interface TIBTypes_SchemaValuesApi {
    content: string[],
    numberOfElements: int32
}

/**
 * This function is used to transform TIB term type into the Swate compatible Term type.
 */
export function Swate_Components_Api_TIBTypes_SearchApi__SearchApi_ToMyTerm(this$: TIBTypes_SearchApi): Term[] {
    return map_1<TIBTypes_Term, Term>((t: TIBTypes_Term): Term => ({
        name: t.label,
        id: t.obo_id,
        description: join(";", t.description),
        source: t.ontology_name,
        href: t.iri,
        isObsolete: defaultArg(t.is_obsolete, false),
    }), this$.response.docs);
}

export class TIBApi {
    constructor() {
    }
    static tryGetIRIFromOboId(oboId: string): Promise<Option<string>> {
        const pr_1: Promise<Types_Response> = fetch$(appendQueryParams(`${"https://api.terminology.tib.eu/api"}/terms`, singleton(["obo_id", oboId] as [string, any])), ofArray([Types_RequestProperties_Method("GET"), Types_RequestProperties_Headers({
            Accept: "application/json",
        } as Types_IHttpRequestHeaders)]));
        return pr_1.then((response: Types_Response): Promise<Option<string>> => {
            const pr: Promise<TIBTypes_TermApi> = response.json<TIBTypes_TermApi>();
            return pr.then((termApi: TIBTypes_TermApi): Option<string> => map_2<TIBTypes_Term, string>((term_1: TIBTypes_Term): string => term_1.iri, tryFind<TIBTypes_Term>((term: TIBTypes_Term): boolean => (term.obo_id === oboId), termApi._embedded.terms)));
        });
    }
    static search(q: string, rows?: int32, obsoletes?: boolean, queryFields?: string[], childrenOf?: string, collection?: string): Promise<Term[]> {
        return PromiseBuilder__Run_212F1D4B<Term[]>(promise, PromiseBuilder__Delay_62FBFDE1<Term[]>(promise, (): Promise<Term[]> => {
            const baseUrl = `${"https://api.terminology.tib.eu/api"}/search`;
            let childrenOf_: Option<string> = undefined;
            return ((childrenOf != null) ? (TIBApi.tryGetIRIFromOboId(value_2(childrenOf)).then((_arg: Option<string>): Promise<void> => {
                const parentIri: Option<string> = _arg;
                childrenOf_ = parentIri;
                if ((childrenOf != null) && (childrenOf_ == null)) {
                    throw new Error("Could not find parent IRI for childrenOf: " + value_2(childrenOf));
                    return Promise.resolve();
                }
                else {
                    return Promise.resolve();
                }
            })) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1<Term[]>(promise, (): Promise<Term[]> => {
                let pr_1: Promise<TIBTypes_SearchApi>, pr: Promise<Types_Response>;
                const queryParams: FSharpList<[string, any]> = toList<[string, any]>(delay<[string, any]>((): Iterable<[string, any]> => append<[string, any]>(singleton_1<[string, any]>(["q", q] as [string, any]), delay<[string, any]>((): Iterable<[string, any]> => append<[string, any]>((rows != null) ? singleton_1<[string, any]>(["rows", value_2(rows)] as [string, any]) : empty<[string, any]>(), delay<[string, any]>((): Iterable<[string, any]> => append<[string, any]>((obsoletes != null) ? singleton_1<[string, any]>(["obsoletes", value_2(obsoletes)] as [string, any]) : empty<[string, any]>(), delay<[string, any]>((): Iterable<[string, any]> => append<[string, any]>((queryFields != null) ? singleton_1<[string, any]>(["queryFields", join(",", value_2(queryFields))] as [string, any]) : empty<[string, any]>(), delay<[string, any]>((): Iterable<[string, any]> => append<[string, any]>((childrenOf_ != null) ? singleton_1<[string, any]>(["childrenOf", value_2(childrenOf_)] as [string, any]) : empty<[string, any]>(), delay<[string, any]>((): Iterable<[string, any]> => ((collection != null) ? append<[string, any]>(singleton_1<[string, any]>(["schema", "collection"] as [string, any]), delay<[string, any]>((): Iterable<[string, any]> => singleton_1<[string, any]>(["classification", value_2(collection)] as [string, any]))) : empty<[string, any]>())))))))))))));
                const url: string = appendQueryParams(baseUrl, queryParams);
                return ((pr_1 = ((pr = fetch$(url, ofArray([Types_RequestProperties_Method("GET"), Types_RequestProperties_Headers({
                    Accept: "application/json",
                } as Types_IHttpRequestHeaders)])), pr.then((response: Types_Response): Promise<TIBTypes_SearchApi> => response.json<TIBTypes_SearchApi>()))), pr_1.then((searchApi: TIBTypes_SearchApi): Term[] => {
                    const collection_1: Term[] = Swate_Components_Api_TIBTypes_SearchApi__SearchApi_ToMyTerm(searchApi);
                    return Array.from(collection_1);
                })));
            }));
        }));
    }
    static defaultSearch(q: string, rows?: int32, collection?: string): Promise<Term[]> {
        const rows_1: int32 = defaultArg<int32>(rows, 10) | 0;
        return TIBApi.search(q, rows_1, false, ["label"], undefined, unwrap(collection));
    }
    static searchChildrenOf(q: string, parentOboId: string, rows?: int32, collection?: string): Promise<Term[]> {
        const rows_1: int32 = defaultArg<int32>(rows, 10) | 0;
        return TIBApi.search(q, rows_1, undefined, undefined, parentOboId, unwrap(collection));
    }
    static searchAllChildrenOf(parentOboId: string, rows?: int32, collection?: string): Promise<Term[]> {
        const rows_1: int32 = defaultArg<int32>(rows, 500) | 0;
        return TIBApi.search("*", rows_1, undefined, undefined, parentOboId, unwrap(collection));
    }
    static getCollections(): Promise<TIBTypes_SchemaValuesApi> {
        const url = `${"https://api.terminology.tib.eu/api"}/ontologies/schemavalues?schema=collection&lang=en`;
        const pr: Promise<Types_Response> = fetch$(url, ofArray([Types_RequestProperties_Method("GET"), Types_RequestProperties_Headers({
            Accept: "application/json",
        } as Types_IHttpRequestHeaders)]));
        return pr.then((response: Types_Response): Promise<TIBTypes_SchemaValuesApi> => response.json<TIBTypes_SchemaValuesApi>());
    }
}

export function TIBApi_$reflection(): TypeInfo {
    return class_type("Swate.Components.Api.TIBApi", undefined, TIBApi);
}

