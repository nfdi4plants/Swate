import { Record } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Types.js";
import { Option } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Option.js";
import { int32 } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Int32.js";
import { FSharpList } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/List.js";
import { Term_$reflection, Term, FullTextSearch_$reflection, FullTextSearch_$union } from "../Database.fs.js";
import { IComparable, IEquatable } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Util.js";
import { array_type, record_type, list_type, option_type, int32_type, string_type, TypeInfo } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Reflection.js";

export class TermQuery extends Record implements IEquatable<TermQuery>, IComparable<TermQuery> {
    readonly query: string;
    readonly limit: Option<int32>;
    readonly parentTermId: Option<string>;
    readonly ontologies: Option<FSharpList<string>>;
    readonly searchMode: Option<FullTextSearch_$union>;
    constructor(query: string, limit: Option<int32>, parentTermId: Option<string>, ontologies: Option<FSharpList<string>>, searchMode: Option<FullTextSearch_$union>) {
        super();
        this.query = query;
        this.limit = limit;
        this.parentTermId = parentTermId;
        this.ontologies = ontologies;
        this.searchMode = searchMode;
    }
}

export function TermQuery_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.DTOs.TermQuery", [], TermQuery, () => [["query", string_type], ["limit", option_type(int32_type)], ["parentTermId", option_type(string_type)], ["ontologies", option_type(list_type(string_type))], ["searchMode", option_type(FullTextSearch_$reflection())]]);
}

export function TermQuery_create_Z6FBE353C(query: string, limit?: int32, parentTermId?: string, ontologies?: FSharpList<string>, searchMode?: FullTextSearch_$union): TermQuery {
    return new TermQuery(query, limit, parentTermId, ontologies, searchMode);
}

export class TermQueryResults extends Record implements IEquatable<TermQueryResults>, IComparable<TermQueryResults> {
    readonly query: TermQuery;
    readonly results: Term[];
    constructor(query: TermQuery, results: Term[]) {
        super();
        this.query = query;
        this.results = results;
    }
}

export function TermQueryResults_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.DTOs.TermQueryResults", [], TermQueryResults, () => [["query", TermQuery_$reflection()], ["results", array_type(Term_$reflection())]]);
}

export function TermQueryResults_create_Z2D254D78(query: TermQuery, results: Term[]): TermQueryResults {
    return new TermQueryResults(query, results);
}

