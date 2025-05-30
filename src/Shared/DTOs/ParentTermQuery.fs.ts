import { Record } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Types.js";
import { Option } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Option.js";
import { int32 } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Int32.js";
import { IComparable, IEquatable } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Util.js";
import { array_type, record_type, option_type, int32_type, string_type, TypeInfo } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Reflection.js";
import { Term_$reflection, Term } from "../Database.fs.js";

export class ParentTermQuery extends Record implements IEquatable<ParentTermQuery>, IComparable<ParentTermQuery> {
    readonly parentTermId: string;
    readonly limit: Option<int32>;
    constructor(parentTermId: string, limit: Option<int32>) {
        super();
        this.parentTermId = parentTermId;
        this.limit = limit;
    }
}

export function ParentTermQuery_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.DTOs.ParentTermQuery", [], ParentTermQuery, () => [["parentTermId", string_type], ["limit", option_type(int32_type)]]);
}

export function ParentTermQuery_create_3B406CA4(parentTermId: string, limit?: int32): ParentTermQuery {
    return new ParentTermQuery(parentTermId, limit);
}

export class ParentTermQueryResults extends Record implements IEquatable<ParentTermQueryResults>, IComparable<ParentTermQueryResults> {
    readonly query: ParentTermQuery;
    readonly results: Term[];
    constructor(query: ParentTermQuery, results: Term[]) {
        super();
        this.query = query;
        this.results = results;
    }
}

export function ParentTermQueryResults_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.DTOs.ParentTermQueryResults", [], ParentTermQueryResults, () => [["query", ParentTermQuery_$reflection()], ["results", array_type(Term_$reflection())]]);
}

export function ParentTermQueryResults_create_16C0BB74(query: ParentTermQuery, results: Term[]): ParentTermQueryResults {
    return new ParentTermQueryResults(query, results);
}

