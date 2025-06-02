import { replace, create, match } from "../Components/src/fable_modules/fable-library-ts.4.24.0/RegExp.js";
import { value as value_2, Option } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Option.js";
import { printf, toText } from "../Components/src/fable_modules/fable-library-ts.4.24.0/String.js";
import { intersect, count, FSharpSet__get_Count, FSharpSet, ofSeq } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Set.js";
import { sortByDescending, windowed, item, map } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Array.js";
import { IComparable, IEquatable, comparePrimitives } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Util.js";
import { float64, int32 } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Int32.js";
import { Record } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Types.js";
import { Async } from "../Components/src/fable_modules/fable-library-ts.4.24.0/AsyncBuilder.js";
import { TermQueryResults_$reflection, TermQuery_$reflection, TermQueryResults, TermQuery } from "./DTOs/TermQuery.fs.js";
import { TreeTypes_Tree_$reflection, Ontology_$reflection, TreeTypes_Tree, Ontology, Term_$reflection, Term } from "./Database.fs.js";
import { ParentTermQueryResults_$reflection, ParentTermQuery_$reflection, ParentTermQueryResults, ParentTermQuery } from "./DTOs/ParentTermQuery.fs.js";
import { AdvancedSearchQuery_$reflection, AdvancedSearchQuery } from "./DTOs/AdvancedSearch.fs.js";
import { anonRecord_type, bool_type, tuple_type, record_type, option_type, string_type, array_type, lambda_type, class_type, int32_type, unit_type, TypeInfo } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Reflection.js";

/**
 * (|Regex|_|) pattern input
 */
export function Regex_$007CRegex$007C_$007C(pattern: string, input: string): Option<any> {
    const m: any = match(create(pattern), input);
    if (m != null) {
        return m;
    }
    else {
        return undefined;
    }
}

export function Route_builder(typeName: string, methodName: string): string {
    return toText(printf("%s/api/%s/%s"))("https://swate-alpha.nfdi4plants.org")(typeName)(methodName);
}

export function SorensenDice_createBigrams(s: string): FSharpSet<string> {
    return ofSeq(map<string[], string>((inner: string[]): string => {
        const arg: string = item(0, inner);
        const arg_1: string = item(1, inner);
        return toText(printf("%c%c"))(arg)(arg_1);
    }, windowed<string>(2, s.toUpperCase().split(""))), {
        Compare: comparePrimitives,
    });
}

export function SorensenDice_sortBySimilarity<a>(searchStr: string, f: ((arg0: a) => string), arrayToSort: a[]): a[] {
    const searchSet: FSharpSet<string> = SorensenDice_createBigrams(searchStr);
    return sortByDescending<a, float64>((result: a): float64 => {
        const x: FSharpSet<string> = SorensenDice_createBigrams(f(result));
        const y: FSharpSet<string> = searchSet;
        const matchValue: int32 = FSharpSet__get_Count(x) | 0;
        const matchValue_1: int32 = FSharpSet__get_Count(y) | 0;
        let matchResult: int32, xCount: int32, yCount: int32;
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
                return (2 * count<string>(intersect<string>(x, y))) / (xCount! + yCount!);
        }
    }, arrayToSort, {
        Compare: comparePrimitives,
    });
}

export class IOntologyAPIv3 extends Record {
    readonly getTestNumber: (() => Async<int32>);
    readonly searchTerm: ((arg0: TermQuery) => Async<Term[]>);
    readonly searchTerms: ((arg0: TermQuery[]) => Async<TermQueryResults[]>);
    readonly getTermById: ((arg0: string) => Async<Option<Term>>);
    readonly searchChildTerms: ((arg0: ParentTermQuery) => Async<ParentTermQueryResults>);
    readonly searchTermAdvanced: ((arg0: AdvancedSearchQuery) => Async<Term[]>);
    constructor(getTestNumber: (() => Async<int32>), searchTerm: ((arg0: TermQuery) => Async<Term[]>), searchTerms: ((arg0: TermQuery[]) => Async<TermQueryResults[]>), getTermById: ((arg0: string) => Async<Option<Term>>), searchChildTerms: ((arg0: ParentTermQuery) => Async<ParentTermQueryResults>), searchTermAdvanced: ((arg0: AdvancedSearchQuery) => Async<Term[]>)) {
        super();
        this.getTestNumber = getTestNumber;
        this.searchTerm = searchTerm;
        this.searchTerms = searchTerms;
        this.getTermById = getTermById;
        this.searchChildTerms = searchChildTerms;
        this.searchTermAdvanced = searchTermAdvanced;
    }
}

export function IOntologyAPIv3_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.IOntologyAPIv3", [], IOntologyAPIv3, () => [["getTestNumber", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [int32_type]))], ["searchTerm", lambda_type(TermQuery_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["searchTerms", lambda_type(array_type(TermQuery_$reflection()), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermQueryResults_$reflection())]))], ["getTermById", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [option_type(Term_$reflection())]))], ["searchChildTerms", lambda_type(ParentTermQuery_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [ParentTermQueryResults_$reflection()]))], ["searchTermAdvanced", lambda_type(AdvancedSearchQuery_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))]]);
}

export class ITestAPI extends Record {
    readonly test: (() => Async<[string, string]>);
    readonly postTest: ((arg0: string) => Async<[string, string]>);
    constructor(test: (() => Async<[string, string]>), postTest: ((arg0: string) => Async<[string, string]>)) {
        super();
        this.test = test;
        this.postTest = postTest;
    }
}

export function ITestAPI_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.ITestAPI", [], ITestAPI, () => [["test", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [tuple_type(string_type, string_type)]))], ["postTest", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [tuple_type(string_type, string_type)]))]]);
}

export class IServiceAPIv1 extends Record {
    readonly getAppVersion: (() => Async<string>);
    constructor(getAppVersion: (() => Async<string>)) {
        super();
        this.getAppVersion = getAppVersion;
    }
}

export function IServiceAPIv1_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.IServiceAPIv1", [], IServiceAPIv1, () => [["getAppVersion", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))]]);
}

export class ITemplateAPIv1 extends Record {
    readonly getTemplates: (() => Async<string>);
    readonly getTemplateById: ((arg0: string) => Async<string>);
    constructor(getTemplates: (() => Async<string>), getTemplateById: ((arg0: string) => Async<string>)) {
        super();
        this.getTemplates = getTemplates;
        this.getTemplateById = getTemplateById;
    }
}

export function ITemplateAPIv1_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.ITemplateAPIv1", [], ITemplateAPIv1, () => [["getTemplates", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))], ["getTemplateById", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))]]);
}

export const SwateObsolete_Regex_Pattern_TermAnnotationShortPattern = `(?<${"idspace"}>\\w+?):(?<${"localid"}>\\w+)`;

export const SwateObsolete_Regex_Pattern_TermAnnotationURIPattern = `.*\\/(?<${"idspace"}>\\w+?)[:_](?<${"localid"}>\\w+)`;

export function SwateObsolete_Regex_parseSquaredTermNameBrackets(headerStr: string): Option<string> {
    const activePatternResult: Option<any> = Regex_$007CRegex$007C_$007C("\\[.*\\]", headerStr);
    if (activePatternResult != null) {
        const value: any = value_2(activePatternResult);
        return replace(value[0].trim().slice(1, (value[0].length - 2) + 1), "#\\d+", "");
    }
    else {
        return undefined;
    }
}

export function SwateObsolete_Regex_parseCoreName(headerStr: string): Option<string> {
    const activePatternResult: Option<any> = Regex_$007CRegex$007C_$007C("^[^[(]*", headerStr);
    if (activePatternResult != null) {
        const value: any = value_2(activePatternResult);
        return value[0].trim();
    }
    else {
        return undefined;
    }
}

/**
 * This function can be used to extract `IDSPACE:LOCALID` (or: `Term Accession` from Swate header strings or obofoundry conform URI strings.
 */
export function SwateObsolete_Regex_parseTermAccession(headerStr: string): Option<string> {
    const matchValue: string = headerStr.trim();
    const activePatternResult: Option<any> = Regex_$007CRegex$007C_$007C(SwateObsolete_Regex_Pattern_TermAnnotationShortPattern, matchValue);
    if (activePatternResult != null) {
        const value: any = value_2(activePatternResult);
        return value[0].trim();
    }
    else {
        const activePatternResult_1: Option<any> = Regex_$007CRegex$007C_$007C(SwateObsolete_Regex_Pattern_TermAnnotationURIPattern, matchValue);
        if (activePatternResult_1 != null) {
            const value_1: any = value_2(activePatternResult_1);
            return (((value_1.groups && value_1.groups.idspace) || "") + ":") + ((value_1.groups && value_1.groups.localid) || "");
        }
        else {
            return undefined;
        }
    }
}

export function SwateObsolete_Regex_parseDoubleQuotes(headerStr: string): Option<string> {
    const activePatternResult: Option<any> = Regex_$007CRegex$007C_$007C("\"(.*?)\"", headerStr);
    if (activePatternResult != null) {
        const value: any = value_2(activePatternResult);
        return value[0].slice(1, (value[0].length - 2) + 1).trim();
    }
    else {
        return undefined;
    }
}

export function SwateObsolete_Regex_getId(headerStr: string): Option<string> {
    const activePatternResult: Option<any> = Regex_$007CRegex$007C_$007C("#\\d+", headerStr);
    if (activePatternResult != null) {
        const value: any = value_2(activePatternResult);
        return value[0].trim().slice(1, value[0].trim().length);
    }
    else {
        return undefined;
    }
}

export class SwateObsolete_TermMinimal extends Record implements IEquatable<SwateObsolete_TermMinimal>, IComparable<SwateObsolete_TermMinimal> {
    readonly Name: string;
    readonly TermAccession: string;
    constructor(Name: string, TermAccession: string) {
        super();
        this.Name = Name;
        this.TermAccession = TermAccession;
    }
}

export function SwateObsolete_TermMinimal_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.SwateObsolete.TermMinimal", [], SwateObsolete_TermMinimal, () => [["Name", string_type], ["TermAccession", string_type]]);
}

export function SwateObsolete_TermMinimal_create(name: string, tan: string): SwateObsolete_TermMinimal {
    return new SwateObsolete_TermMinimal(name, tan);
}

export class SwateObsolete_TermSearchable extends Record implements IEquatable<SwateObsolete_TermSearchable>, IComparable<SwateObsolete_TermSearchable> {
    readonly Term: SwateObsolete_TermMinimal;
    readonly ParentTerm: Option<SwateObsolete_TermMinimal>;
    readonly IsUnit: boolean;
    readonly ColIndex: int32;
    readonly RowIndices: int32[];
    readonly SearchResultTerm: Option<Term>;
    constructor(Term: SwateObsolete_TermMinimal, ParentTerm: Option<SwateObsolete_TermMinimal>, IsUnit: boolean, ColIndex: int32, RowIndices: int32[], SearchResultTerm: Option<Term>) {
        super();
        this.Term = Term;
        this.ParentTerm = ParentTerm;
        this.IsUnit = IsUnit;
        this.ColIndex = (ColIndex | 0);
        this.RowIndices = RowIndices;
        this.SearchResultTerm = SearchResultTerm;
    }
}

export function SwateObsolete_TermSearchable_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.SwateObsolete.TermSearchable", [], SwateObsolete_TermSearchable, () => [["Term", SwateObsolete_TermMinimal_$reflection()], ["ParentTerm", option_type(SwateObsolete_TermMinimal_$reflection())], ["IsUnit", bool_type], ["ColIndex", int32_type], ["RowIndices", array_type(int32_type)], ["SearchResultTerm", option_type(Term_$reflection())]]);
}

export class IOntologyAPIv1 extends Record {
    readonly getTestNumber: (() => Async<int32>);
    readonly getAllOntologies: (() => Async<Ontology[]>);
    readonly getTermSuggestions: ((arg0: [int32, string]) => Async<Term[]>);
    readonly getTermSuggestionsByParentTerm: ((arg0: [int32, string, SwateObsolete_TermMinimal]) => Async<Term[]>);
    readonly getAllTermsByParentTerm: ((arg0: SwateObsolete_TermMinimal) => Async<Term[]>);
    readonly getTermSuggestionsByChildTerm: ((arg0: [int32, string, SwateObsolete_TermMinimal]) => Async<Term[]>);
    readonly getAllTermsByChildTerm: ((arg0: SwateObsolete_TermMinimal) => Async<Term[]>);
    readonly getTermsForAdvancedSearch: ((arg0: AdvancedSearchQuery) => Async<Term[]>);
    readonly getUnitTermSuggestions: ((arg0: [int32, string]) => Async<Term[]>);
    readonly getTermsByNames: ((arg0: SwateObsolete_TermSearchable[]) => Async<SwateObsolete_TermSearchable[]>);
    readonly getTreeByAccession: ((arg0: string) => Async<TreeTypes_Tree>);
    constructor(getTestNumber: (() => Async<int32>), getAllOntologies: (() => Async<Ontology[]>), getTermSuggestions: ((arg0: [int32, string]) => Async<Term[]>), getTermSuggestionsByParentTerm: ((arg0: [int32, string, SwateObsolete_TermMinimal]) => Async<Term[]>), getAllTermsByParentTerm: ((arg0: SwateObsolete_TermMinimal) => Async<Term[]>), getTermSuggestionsByChildTerm: ((arg0: [int32, string, SwateObsolete_TermMinimal]) => Async<Term[]>), getAllTermsByChildTerm: ((arg0: SwateObsolete_TermMinimal) => Async<Term[]>), getTermsForAdvancedSearch: ((arg0: AdvancedSearchQuery) => Async<Term[]>), getUnitTermSuggestions: ((arg0: [int32, string]) => Async<Term[]>), getTermsByNames: ((arg0: SwateObsolete_TermSearchable[]) => Async<SwateObsolete_TermSearchable[]>), getTreeByAccession: ((arg0: string) => Async<TreeTypes_Tree>)) {
        super();
        this.getTestNumber = getTestNumber;
        this.getAllOntologies = getAllOntologies;
        this.getTermSuggestions = getTermSuggestions;
        this.getTermSuggestionsByParentTerm = getTermSuggestionsByParentTerm;
        this.getAllTermsByParentTerm = getAllTermsByParentTerm;
        this.getTermSuggestionsByChildTerm = getTermSuggestionsByChildTerm;
        this.getAllTermsByChildTerm = getAllTermsByChildTerm;
        this.getTermsForAdvancedSearch = getTermsForAdvancedSearch;
        this.getUnitTermSuggestions = getUnitTermSuggestions;
        this.getTermsByNames = getTermsByNames;
        this.getTreeByAccession = getTreeByAccession;
    }
}

export function IOntologyAPIv1_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.IOntologyAPIv1", [], IOntologyAPIv1, () => [["getTestNumber", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [int32_type]))], ["getAllOntologies", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Ontology_$reflection())]))], ["getTermSuggestions", lambda_type(tuple_type(int32_type, string_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getTermSuggestionsByParentTerm", lambda_type(tuple_type(int32_type, string_type, SwateObsolete_TermMinimal_$reflection()), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getAllTermsByParentTerm", lambda_type(SwateObsolete_TermMinimal_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getTermSuggestionsByChildTerm", lambda_type(tuple_type(int32_type, string_type, SwateObsolete_TermMinimal_$reflection()), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getAllTermsByChildTerm", lambda_type(SwateObsolete_TermMinimal_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getTermsForAdvancedSearch", lambda_type(AdvancedSearchQuery_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getUnitTermSuggestions", lambda_type(tuple_type(int32_type, string_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getTermsByNames", lambda_type(array_type(SwateObsolete_TermSearchable_$reflection()), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(SwateObsolete_TermSearchable_$reflection())]))], ["getTreeByAccession", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [TreeTypes_Tree_$reflection()]))]]);
}

export class IOntologyAPIv2 extends Record {
    readonly getTestNumber: (() => Async<int32>);
    readonly getAllOntologies: (() => Async<Ontology[]>);
    readonly getTermSuggestions: ((arg0: { n: int32, ontology?: string, query: string }) => Async<Term[]>);
    readonly getTermSuggestionsByParentTerm: ((arg0: { n: int32, parent_term: SwateObsolete_TermMinimal, query: string }) => Async<Term[]>);
    readonly getAllTermsByParentTerm: ((arg0: SwateObsolete_TermMinimal) => Async<Term[]>);
    readonly getTermSuggestionsByChildTerm: ((arg0: { child_term: SwateObsolete_TermMinimal, n: int32, query: string }) => Async<Term[]>);
    readonly getAllTermsByChildTerm: ((arg0: SwateObsolete_TermMinimal) => Async<Term[]>);
    readonly getTermsForAdvancedSearch: ((arg0: AdvancedSearchQuery) => Async<Term[]>);
    readonly getUnitTermSuggestions: ((arg0: { n: int32, query: string }) => Async<Term[]>);
    readonly getTermsByNames: ((arg0: SwateObsolete_TermSearchable[]) => Async<SwateObsolete_TermSearchable[]>);
    readonly getTreeByAccession: ((arg0: string) => Async<TreeTypes_Tree>);
    constructor(getTestNumber: (() => Async<int32>), getAllOntologies: (() => Async<Ontology[]>), getTermSuggestions: ((arg0: { n: int32, ontology?: string, query: string }) => Async<Term[]>), getTermSuggestionsByParentTerm: ((arg0: { n: int32, parent_term: SwateObsolete_TermMinimal, query: string }) => Async<Term[]>), getAllTermsByParentTerm: ((arg0: SwateObsolete_TermMinimal) => Async<Term[]>), getTermSuggestionsByChildTerm: ((arg0: { child_term: SwateObsolete_TermMinimal, n: int32, query: string }) => Async<Term[]>), getAllTermsByChildTerm: ((arg0: SwateObsolete_TermMinimal) => Async<Term[]>), getTermsForAdvancedSearch: ((arg0: AdvancedSearchQuery) => Async<Term[]>), getUnitTermSuggestions: ((arg0: { n: int32, query: string }) => Async<Term[]>), getTermsByNames: ((arg0: SwateObsolete_TermSearchable[]) => Async<SwateObsolete_TermSearchable[]>), getTreeByAccession: ((arg0: string) => Async<TreeTypes_Tree>)) {
        super();
        this.getTestNumber = getTestNumber;
        this.getAllOntologies = getAllOntologies;
        this.getTermSuggestions = getTermSuggestions;
        this.getTermSuggestionsByParentTerm = getTermSuggestionsByParentTerm;
        this.getAllTermsByParentTerm = getAllTermsByParentTerm;
        this.getTermSuggestionsByChildTerm = getTermSuggestionsByChildTerm;
        this.getAllTermsByChildTerm = getAllTermsByChildTerm;
        this.getTermsForAdvancedSearch = getTermsForAdvancedSearch;
        this.getUnitTermSuggestions = getUnitTermSuggestions;
        this.getTermsByNames = getTermsByNames;
        this.getTreeByAccession = getTreeByAccession;
    }
}

export function IOntologyAPIv2_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.IOntologyAPIv2", [], IOntologyAPIv2, () => [["getTestNumber", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [int32_type]))], ["getAllOntologies", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Ontology_$reflection())]))], ["getTermSuggestions", lambda_type(anonRecord_type(["n", int32_type], ["ontology", option_type(string_type)], ["query", string_type]), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getTermSuggestionsByParentTerm", lambda_type(anonRecord_type(["n", int32_type], ["parent_term", SwateObsolete_TermMinimal_$reflection()], ["query", string_type]), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getAllTermsByParentTerm", lambda_type(SwateObsolete_TermMinimal_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getTermSuggestionsByChildTerm", lambda_type(anonRecord_type(["child_term", SwateObsolete_TermMinimal_$reflection()], ["n", int32_type], ["query", string_type]), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getAllTermsByChildTerm", lambda_type(SwateObsolete_TermMinimal_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getTermsForAdvancedSearch", lambda_type(AdvancedSearchQuery_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getUnitTermSuggestions", lambda_type(anonRecord_type(["n", int32_type], ["query", string_type]), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(Term_$reflection())]))], ["getTermsByNames", lambda_type(array_type(SwateObsolete_TermSearchable_$reflection()), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(SwateObsolete_TermSearchable_$reflection())]))], ["getTreeByAccession", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [TreeTypes_Tree_$reflection()]))]]);
}

