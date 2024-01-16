import { printf, toText } from "../../fable_modules/fable-library.4.9.0/String.js";
import { intersect, count, FSharpSet__get_Count, ofSeq } from "../../fable_modules/fable-library.4.9.0/Set.js";
import { sortByDescending, windowed, map } from "../../fable_modules/fable-library.4.9.0/Array.js";
import { comparePrimitives } from "../../fable_modules/fable-library.4.9.0/Util.js";
import { Record, Union } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { anonRecord_type, option_type, int32_type, obj_type, uint8_type, array_type, record_type, lambda_type, class_type, tuple_type, string_type, unit_type, union_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";
import { InsertBuildingBlock_$reflection, BuildingBlock_$reflection } from "./OfficeInteropTypes.js";
import { TreeTypes_Tree_$reflection, TermTypes_TermSearchable_$reflection, TermTypes_TermMinimal_$reflection, TermTypes_Term_$reflection, TermTypes_Ontology_$reflection } from "./TermTypes.js";
import { AdvancedSearchOptions_$reflection } from "./AdvancedSearchTypes.js";

export function Route_builder(typeName, methodName) {
    return toText(printf("/api/%s/%s"))(typeName)(methodName);
}

export function SorensenDice_createBigrams(s) {
    return ofSeq(map((inner) => {
        const arg = inner[0];
        const arg_1 = inner[1];
        return toText(printf("%c%c"))(arg)(arg_1);
    }, windowed(2, s.toUpperCase().split(""))), {
        Compare: comparePrimitives,
    });
}

export function SorensenDice_sortBySimilarity(searchStr, f, arrayToSort) {
    const searchSet = SorensenDice_createBigrams(searchStr);
    return sortByDescending((result) => {
        const x = SorensenDice_createBigrams(f(result));
        const y = searchSet;
        const matchValue = FSharpSet__get_Count(x) | 0;
        const matchValue_1 = FSharpSet__get_Count(y) | 0;
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
                return (2 * count(intersect(x, y))) / (xCount + yCount);
        }
    }, arrayToSort, {
        Compare: comparePrimitives,
    });
}

export class JsonExportType extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["ProcessSeq", "Assay", "ProtocolTemplate"];
    }
}

export function JsonExportType_$reflection() {
    return union_type("Shared.JsonExportType", [], JsonExportType, () => [[], [], []]);
}

export function JsonExportType__get_toExplanation(this$) {
    switch (this$.tag) {
        case 1:
            return "ISA assay.json";
        case 2:
            return "Schema for Swate protocol template, with template metadata and table json.";
        default:
            return "Sequence of ISA process.json.";
    }
}

export class ITestAPI extends Record {
    constructor(test, postTest) {
        super();
        this.test = test;
        this.postTest = postTest;
    }
}

export function ITestAPI_$reflection() {
    return record_type("Shared.ITestAPI", [], ITestAPI, () => [["test", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [tuple_type(string_type, string_type)]))], ["postTest", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [tuple_type(string_type, string_type)]))]]);
}

export class IServiceAPIv1 extends Record {
    constructor(getAppVersion) {
        super();
        this.getAppVersion = getAppVersion;
    }
}

export function IServiceAPIv1_$reflection() {
    return record_type("Shared.IServiceAPIv1", [], IServiceAPIv1, () => [["getAppVersion", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))]]);
}

export class IDagAPIv1 extends Record {
    constructor(parseAnnotationTablesToDagHtml) {
        super();
        this.parseAnnotationTablesToDagHtml = parseAnnotationTablesToDagHtml;
    }
}

export function IDagAPIv1_$reflection() {
    return record_type("Shared.IDagAPIv1", [], IDagAPIv1, () => [["parseAnnotationTablesToDagHtml", lambda_type(array_type(tuple_type(string_type, array_type(BuildingBlock_$reflection()))), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))]]);
}

export class IISADotNetCommonAPIv1 extends Record {
    constructor(toAssayJson, toSwateTemplateJson, toInvestigationJson, toProcessSeqJson, toAssayJsonStr, toSwateTemplateJsonStr, toInvestigationJsonStr, toProcessSeqJsonStr, testPostNumber, getTestNumber) {
        super();
        this.toAssayJson = toAssayJson;
        this.toSwateTemplateJson = toSwateTemplateJson;
        this.toInvestigationJson = toInvestigationJson;
        this.toProcessSeqJson = toProcessSeqJson;
        this.toAssayJsonStr = toAssayJsonStr;
        this.toSwateTemplateJsonStr = toSwateTemplateJsonStr;
        this.toInvestigationJsonStr = toInvestigationJsonStr;
        this.toProcessSeqJsonStr = toProcessSeqJsonStr;
        this.testPostNumber = testPostNumber;
        this.getTestNumber = getTestNumber;
    }
}

export function IISADotNetCommonAPIv1_$reflection() {
    return record_type("Shared.IISADotNetCommonAPIv1", [], IISADotNetCommonAPIv1, () => [["toAssayJson", lambda_type(array_type(uint8_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [obj_type]))], ["toSwateTemplateJson", lambda_type(array_type(uint8_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [obj_type]))], ["toInvestigationJson", lambda_type(array_type(uint8_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [obj_type]))], ["toProcessSeqJson", lambda_type(array_type(uint8_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [obj_type]))], ["toAssayJsonStr", lambda_type(array_type(uint8_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))], ["toSwateTemplateJsonStr", lambda_type(array_type(uint8_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))], ["toInvestigationJsonStr", lambda_type(array_type(uint8_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))], ["toProcessSeqJsonStr", lambda_type(array_type(uint8_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))], ["testPostNumber", lambda_type(int32_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))], ["getTestNumber", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))]]);
}

export class ISwateJsonAPIv1 extends Record {
    constructor(parseAnnotationTableToAssayJson, parseAnnotationTableToProcessSeqJson, parseAnnotationTablesToAssayJson, parseAnnotationTablesToProcessSeqJson, parseAssayJsonToBuildingBlocks, parseProcessSeqToBuildingBlocks) {
        super();
        this.parseAnnotationTableToAssayJson = parseAnnotationTableToAssayJson;
        this.parseAnnotationTableToProcessSeqJson = parseAnnotationTableToProcessSeqJson;
        this.parseAnnotationTablesToAssayJson = parseAnnotationTablesToAssayJson;
        this.parseAnnotationTablesToProcessSeqJson = parseAnnotationTablesToProcessSeqJson;
        this.parseAssayJsonToBuildingBlocks = parseAssayJsonToBuildingBlocks;
        this.parseProcessSeqToBuildingBlocks = parseProcessSeqToBuildingBlocks;
    }
}

export function ISwateJsonAPIv1_$reflection() {
    return record_type("Shared.ISwateJsonAPIv1", [], ISwateJsonAPIv1, () => [["parseAnnotationTableToAssayJson", lambda_type(tuple_type(string_type, array_type(BuildingBlock_$reflection())), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))], ["parseAnnotationTableToProcessSeqJson", lambda_type(tuple_type(string_type, array_type(BuildingBlock_$reflection())), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))], ["parseAnnotationTablesToAssayJson", lambda_type(array_type(tuple_type(string_type, array_type(BuildingBlock_$reflection()))), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))], ["parseAnnotationTablesToProcessSeqJson", lambda_type(array_type(tuple_type(string_type, array_type(BuildingBlock_$reflection()))), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))], ["parseAssayJsonToBuildingBlocks", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(tuple_type(string_type, array_type(InsertBuildingBlock_$reflection())))]))], ["parseProcessSeqToBuildingBlocks", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(tuple_type(string_type, array_type(InsertBuildingBlock_$reflection())))]))]]);
}

export class IExportAPIv1 extends Record {
    constructor(toAssayXlsx) {
        super();
        this.toAssayXlsx = toAssayXlsx;
    }
}

export function IExportAPIv1_$reflection() {
    return record_type("Shared.IExportAPIv1", [], IExportAPIv1, () => [["toAssayXlsx", lambda_type(array_type(tuple_type(string_type, array_type(BuildingBlock_$reflection()))), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(uint8_type)]))]]);
}

export class IOntologyAPIv1 extends Record {
    constructor(getTestNumber, getAllOntologies, getTermSuggestions, getTermSuggestionsByParentTerm, getAllTermsByParentTerm, getTermSuggestionsByChildTerm, getAllTermsByChildTerm, getTermsForAdvancedSearch, getUnitTermSuggestions, getTermsByNames, getTreeByAccession) {
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

export function IOntologyAPIv1_$reflection() {
    return record_type("Shared.IOntologyAPIv1", [], IOntologyAPIv1, () => [["getTestNumber", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [int32_type]))], ["getAllOntologies", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Ontology_$reflection())]))], ["getTermSuggestions", lambda_type(tuple_type(int32_type, string_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getTermSuggestionsByParentTerm", lambda_type(tuple_type(int32_type, string_type, TermTypes_TermMinimal_$reflection()), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getAllTermsByParentTerm", lambda_type(TermTypes_TermMinimal_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getTermSuggestionsByChildTerm", lambda_type(tuple_type(int32_type, string_type, TermTypes_TermMinimal_$reflection()), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getAllTermsByChildTerm", lambda_type(TermTypes_TermMinimal_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getTermsForAdvancedSearch", lambda_type(AdvancedSearchOptions_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getUnitTermSuggestions", lambda_type(tuple_type(int32_type, string_type), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getTermsByNames", lambda_type(array_type(TermTypes_TermSearchable_$reflection()), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_TermSearchable_$reflection())]))], ["getTreeByAccession", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [TreeTypes_Tree_$reflection()]))]]);
}

export class IOntologyAPIv2 extends Record {
    constructor(getTestNumber, getAllOntologies, getTermSuggestions, getTermSuggestionsByParentTerm, getAllTermsByParentTerm, getTermSuggestionsByChildTerm, getAllTermsByChildTerm, getTermsForAdvancedSearch, getUnitTermSuggestions, getTermsByNames, getTreeByAccession) {
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

export function IOntologyAPIv2_$reflection() {
    return record_type("Shared.IOntologyAPIv2", [], IOntologyAPIv2, () => [["getTestNumber", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [int32_type]))], ["getAllOntologies", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Ontology_$reflection())]))], ["getTermSuggestions", lambda_type(anonRecord_type(["n", int32_type], ["ontology", option_type(string_type)], ["query", string_type]), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getTermSuggestionsByParentTerm", lambda_type(anonRecord_type(["n", int32_type], ["parent_term", TermTypes_TermMinimal_$reflection()], ["query", string_type]), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getAllTermsByParentTerm", lambda_type(TermTypes_TermMinimal_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getTermSuggestionsByChildTerm", lambda_type(anonRecord_type(["child_term", TermTypes_TermMinimal_$reflection()], ["n", int32_type], ["query", string_type]), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getAllTermsByChildTerm", lambda_type(TermTypes_TermMinimal_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getTermsForAdvancedSearch", lambda_type(AdvancedSearchOptions_$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getUnitTermSuggestions", lambda_type(anonRecord_type(["n", int32_type], ["query", string_type]), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_Term_$reflection())]))], ["getTermsByNames", lambda_type(array_type(TermTypes_TermSearchable_$reflection()), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(TermTypes_TermSearchable_$reflection())]))], ["getTreeByAccession", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [TreeTypes_Tree_$reflection()]))]]);
}

export class ITemplateAPIv1 extends Record {
    constructor(getTemplates, getTemplateById) {
        super();
        this.getTemplates = getTemplates;
        this.getTemplateById = getTemplateById;
    }
}

export function ITemplateAPIv1_$reflection() {
    return record_type("Shared.ITemplateAPIv1", [], ITemplateAPIv1, () => [["getTemplates", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [array_type(string_type)]))], ["getTemplateById", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [string_type]))]]);
}

//# sourceMappingURL=Shared.js.map
