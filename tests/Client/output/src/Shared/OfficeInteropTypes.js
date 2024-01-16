import { Record, Union } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { array_type, option_type, int32_type, record_type, union_type, string_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";
import { equalsWith, exactlyOne } from "../../fable_modules/fable-library.4.9.0/Array.js";
import { tryFind, filter, ofArray } from "../../fable_modules/fable-library.4.9.0/List.js";
import { TermTypes_TermMinimal_$reflection, TermTypes_TermMinimal_create } from "./TermTypes.js";
import { printf, toText } from "../../fable_modules/fable-library.4.9.0/String.js";
import { bind, value as value_1, map } from "../../fable_modules/fable-library.4.9.0/Option.js";
import { getId, parseTermAccession, parseSquaredTermNameBrackets, parseCoreName } from "./Regex.js";
import { equals } from "../../fable_modules/fable-library.4.9.0/Util.js";

export class TryFindAnnoTableResult extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Success", "Error"];
    }
}

export function TryFindAnnoTableResult_$reflection() {
    return union_type("Shared.OfficeInteropTypes.TryFindAnnoTableResult", [], TryFindAnnoTableResult, () => [[["Item", string_type]], [["Item", string_type]]]);
}

/**
 * This function is used on an array of table names (string []). If the length of the array is <> 1 it will trough the correct error.
 * Only returns success if annoTables.Length = 1. Does not check if the existing table names are correct/okay.
 */
export function TryFindAnnoTableResult_exactlyOneAnnotationTable_Z35CD86D0(annoTables) {
    const matchValue = annoTables.length | 0;
    if (matchValue < 1) {
        return new TryFindAnnoTableResult(1, ["Could not find annotationTable in active worksheet. Please create one before trying to execute this function."]);
    }
    else if (matchValue > 1) {
        return new TryFindAnnoTableResult(1, ["The active worksheet contains more than one annotationTable. Please move one of them to another worksheet."]);
    }
    else if (matchValue === 1) {
        return new TryFindAnnoTableResult(0, [exactlyOne(annoTables)]);
    }
    else {
        return new TryFindAnnoTableResult(1, ["Could not process message. Swate was not able to identify the given annotation tables with a known case."]);
    }
}

export class BuildingBlockType extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Parameter", "Factor", "Characteristic", "Component", "Source", "Sample", "Data", "RawDataFile", "DerivedDataFile", "ProtocolType", "ProtocolREF", "Freetext"];
    }
}

export function BuildingBlockType_$reflection() {
    return union_type("Shared.OfficeInteropTypes.BuildingBlockType", [], BuildingBlockType, () => [[], [], [], [], [], [], [], [], [], [], [], [["Item", string_type]]]);
}

export function BuildingBlockType_get_All() {
    return ofArray([new BuildingBlockType(0, []), new BuildingBlockType(1, []), new BuildingBlockType(2, []), new BuildingBlockType(3, []), new BuildingBlockType(4, []), new BuildingBlockType(5, []), new BuildingBlockType(7, []), new BuildingBlockType(8, []), new BuildingBlockType(6, []), new BuildingBlockType(9, []), new BuildingBlockType(10, [])]);
}

export function BuildingBlockType__get_isInputColumn(this$) {
    if (this$.tag === 4) {
        return true;
    }
    else {
        return false;
    }
}

export function BuildingBlockType__get_isOutputColumn(this$) {
    switch (this$.tag) {
        case 6:
        case 5:
        case 7:
        case 8:
            return true;
        default:
            return false;
    }
}

/**
 * The name "TermColumn" refers to all columns with the syntax "Parameter/Factor/etc [TERM-NAME]"
 */
export function BuildingBlockType__get_isTermColumn(this$) {
    switch (this$.tag) {
        case 0:
        case 1:
        case 2:
        case 3:
        case 9:
            return true;
        default:
            return false;
    }
}

export function BuildingBlockType_get_TermColumns() {
    return filter(BuildingBlockType__get_isTermColumn, BuildingBlockType_get_All());
}

export function BuildingBlockType_get_InputColumns() {
    return filter(BuildingBlockType__get_isInputColumn, BuildingBlockType_get_All());
}

export function BuildingBlockType_get_OutputColumns() {
    return filter(BuildingBlockType__get_isOutputColumn, BuildingBlockType_get_All());
}

/**
 * This function returns true if the BuildingBlockType is a featured column. A featured column can
 * be abstracted by Parameter/Factor/Characteristics and describes one common usecase of either.
 * Such a block will contain TSR and TAN and can be used for directed Term search.
 */
export function BuildingBlockType__get_isFeaturedColumn(this$) {
    if (this$.tag === 9) {
        return true;
    }
    else {
        return false;
    }
}

export function BuildingBlockType__get_getFeaturedColumnAccession(this$) {
    if (BuildingBlockType__get_isFeaturedColumn(this$)) {
        if (this$.tag === 9) {
            return "DPBO:1000161";
        }
        else {
            throw new Error("This cannot happen");
        }
    }
    else {
        throw new Error(`'${this$}' is not listed as featured column type! No referenced accession available.`);
    }
}

export function BuildingBlockType__get_getFeaturedColumnTermMinimal(this$) {
    if (BuildingBlockType__get_isFeaturedColumn(this$)) {
        if (this$.tag === 9) {
            return TermTypes_TermMinimal_create("protocol type", BuildingBlockType__get_getFeaturedColumnAccession(this$));
        }
        else {
            throw new Error("This cannot happen");
        }
    }
    else {
        throw new Error(`'${this$}' is not listed as featured column type! No referenced accession available.`);
    }
}

/**
 * Checks if a string matches one of the single column core names exactly.
 */
export function BuildingBlockType__get_isSingleColumn(this$) {
    switch (this$.tag) {
        case 5:
        case 4:
        case 6:
        case 7:
        case 8:
        case 10:
        case 11:
            return true;
        default:
            return false;
    }
}

export function BuildingBlockType_tryOfString_Z721C83C5(str) {
    switch (str) {
        case "Parameter":
        case "Parameter Value":
            return new BuildingBlockType(0, []);
        case "Factor":
        case "Factor Value":
            return new BuildingBlockType(1, []);
        case "Characteristics":
        case "Characteristic":
        case "Characteristics Value":
            return new BuildingBlockType(2, []);
        case "Component":
            return new BuildingBlockType(3, []);
        case "Sample Name":
            return new BuildingBlockType(5, []);
        case "Data File Name":
            return new BuildingBlockType(6, []);
        case "Raw Data File":
            return new BuildingBlockType(7, []);
        case "Derived Data File":
            return new BuildingBlockType(8, []);
        case "Source Name":
            return new BuildingBlockType(4, []);
        case "Protocol Type":
            return new BuildingBlockType(9, []);
        case "Protocol REF":
            return new BuildingBlockType(10, []);
        default:
            return new BuildingBlockType(11, [str]);
    }
}

export function BuildingBlockType_ofString_Z721C83C5(str) {
    const _arg = BuildingBlockType_tryOfString_Z721C83C5(str);
    if (_arg == null) {
        throw new Error(`Error: Unable to parse '${str}' to BuildingBlockType!`);
    }
    else {
        return _arg;
    }
}

export function BuildingBlockType__get_toString(this$) {
    switch (this$.tag) {
        case 1:
            return "Factor";
        case 2:
            return "Characteristic";
        case 3:
            return "Component";
        case 5:
            return "Sample Name";
        case 6:
            return "Data File Name";
        case 7:
            return "Raw Data File";
        case 8:
            return "Derived Data File";
        case 9:
            return "Protocol Type";
        case 4:
            return "Source Name";
        case 10:
            return "Protocol REF";
        case 11:
            return this$.fields[0];
        default:
            return "Parameter";
    }
}

/**
 * By Martin Kuhl 04.08.2022, https://github.com/Martin-Kuhl
 */
export function BuildingBlockType__get_toShortExplanation(this$) {
    switch (this$.tag) {
        case 1:
            return "Use Factor columns to describe independent variables that result in a specific output of your experiment, e.g. the light intensity under which an organism was grown.";
        case 2:
            return "Characteristics columns are used for study descriptions and describe inherent properties of the source material, e.g. a certain strain or the temperature the organism was exposed to. ";
        case 3:
            return "Use these columns to list the components of a protocol, e.g. instrument names, software names, and reagents names.";
        case 5:
            return "The Sample Name column defines the resulting biological material and thereby, the output of the annotated workflow. The value must be a unique identifier.";
        case 6:
            return "DEPRECATED: Use data columns to mark the data file name that your computational analysis produced.";
        case 7:
            return "The Raw Data File column defines untransformed and unprocessed data files";
        case 8:
            return "The Derived Data File column defines transformed and/or processed data files";
        case 4:
            return "The Source column defines the input of your table. This input value must be a unique identifier for an organism or a sample. The number of Source Name columns per table is limited to one.";
        case 9:
            return "Defines the protocol type according to your preferred endpoint repository.";
        case 10:
            return "Defines the protocol name.";
        case 11:
            throw new Error("Freetext BuildingBlockType should not be parsed");
        default:
            return "Parameter columns describe steps in your experimental workflow, e.g. the centrifugation time or the temperature used for your assay. Multiple Parameter columns form a protocol.";
    }
}

/**
 * By Martin Kuhl 04.08.2022, https://github.com/Martin-Kuhl
 */
export function BuildingBlockType__get_toLongExplanation(this$) {
    switch (this$.tag) {
        case 1:
            return "Use Factor columns to describe independent variables that result in a specific output of your experiment, \r\n                e.g. the light intensity under which an organism was grown. Factor columns are very important building blocks for your downstream computational analysis.\r\n                The combination of a container ontology (Characteristics, Parameter, Factor) and a biological or technological ontology (e.g. temperature, light intensity) gives\r\n                the flexibility to display a term as a regular process parameter or as the factor your study is based on (Parameter [temperature] or Factor [temperature]).";
        case 2:
            return "Characteristics columns are used for study descriptions and describe inherent properties of the source material, e.g. a certain strain or ecotype, but also the temperature an organism was exposed to.\r\n                There is no limitation for the number of Characteristics columns per table.  ";
        case 3:
            return "Use these columns to list the components of a protocol, e.g. instrument names, software names, and reagents names.";
        case 5:
            return "The Sample Name column defines the resulting biological material and thereby, the output of the annotated workflow. The value must be a unique identifier. The output of a table (Sample Name, Raw Data File, Derived Data File) can be used again as Source Name of a new table to illustrate an entire experimental workflow. The number of Output columns per table is limited to one.";
        case 6:
            return "DEPRECATED: The Data column describes data files that results from your experiments.\r\n                Additionally to the type of data, the annotated files must have a unique name.\r\n                Data files can be sources for computational workflows.";
        case 7:
            return "Use Raw Data File columns to define untransformed and unprocessed data files. The output of a table\r\n                (Sample Name, Raw Data File, Derived Data File) can be used again as Source Name of a new table\r\n                to illustrate an entire experimental workflow. The number of Output columns per table is limited to one.";
        case 8:
            return "Use Derived Data File columns to define transformed and/or processed data files. The output of a table\r\n                (Sample Name, Raw Data File, Derived Data File) can be used again as Source Name of a new table to illustrate an\r\n                entire experimental workflow. The number of Output columns per table is limited to one";
        case 4:
            return "The Source Name column  defines the input of your table. This input value must be a unique identifier for an organism or a sample.\r\n                The number of Source Name columns per table is limited to one. Usually, you don’t have to add this column as it is automatically\r\n                generated when you add a table to the worksheet. The output of a previous table can be used as Source Name of a new one to illustrate an entire workflow.";
        case 9:
            return "Use this column type to define the protocol type according to your preferred endpoint repository.\r\n                You can use the term search, to search through all available protocol types.";
        case 10:
            return "Use this column type to define your protocol name. Normally the Excel worksheet name is used, but it is limited to ~32 characters.";
        case 11:
            throw new Error("Freetext BuildingBlockType should not be parsed");
        default:
            return "Parameter columns describe steps in your experimental workflow, e.g. the centrifugation time or the temperature used for your assay.\r\n                Multiple Parameter columns form a protocol.There is no limitation for the number of Parameter columns per table.";
    }
}

export class BuildingBlockNamePrePrint extends Record {
    constructor(Type, Name) {
        super();
        this.Type = Type;
        this.Name = Name;
    }
}

export function BuildingBlockNamePrePrint_$reflection() {
    return record_type("Shared.OfficeInteropTypes.BuildingBlockNamePrePrint", [], BuildingBlockNamePrePrint, () => [["Type", BuildingBlockType_$reflection()], ["Name", string_type]]);
}

export function BuildingBlockNamePrePrint_init_58F0589B(t) {
    return new BuildingBlockNamePrePrint(t, "");
}

export function BuildingBlockNamePrePrint_create(t, name) {
    return new BuildingBlockNamePrePrint(t, name);
}

export function BuildingBlockNamePrePrint__toAnnotationTableHeader(this$) {
    const matchValue = this$.Type;
    switch (matchValue.tag) {
        case 1:
            return toText(printf("Factor [%s]"))(this$.Name);
        case 2:
            return toText(printf("Characteristic [%s]"))(this$.Name);
        case 3:
            return toText(printf("Component [%s]"))(this$.Name);
        case 5:
            return BuildingBlockType__get_toString(new BuildingBlockType(5, []));
        case 6:
            return BuildingBlockType__get_toString(new BuildingBlockType(6, []));
        case 7:
            return BuildingBlockType__get_toString(new BuildingBlockType(7, []));
        case 8:
            return BuildingBlockType__get_toString(new BuildingBlockType(8, []));
        case 4:
            return BuildingBlockType__get_toString(new BuildingBlockType(4, []));
        case 9:
            return BuildingBlockType__get_toString(new BuildingBlockType(9, []));
        case 10:
            return BuildingBlockType__get_toString(new BuildingBlockType(10, []));
        case 11:
            return BuildingBlockType__get_toString(new BuildingBlockType(11, [matchValue.fields[0]]));
        default:
            return toText(printf("Parameter [%s]"))(this$.Name);
    }
}

/**
 * Check if .Type is single column type
 */
export function BuildingBlockNamePrePrint__get_isSingleColumn(this$) {
    return BuildingBlockType__get_isSingleColumn(this$.Type);
}

/**
 * Check if .Type is input column type
 */
export function BuildingBlockNamePrePrint__get_isInputColumn(this$) {
    return BuildingBlockType__get_isInputColumn(this$.Type);
}

/**
 * Check if .Type is output column type
 */
export function BuildingBlockNamePrePrint__get_isOutputColumn(this$) {
    return BuildingBlockType__get_isOutputColumn(this$.Type);
}

/**
 * Check if .Type is featured column type
 */
export function BuildingBlockNamePrePrint__get_isFeaturedColumn(this$) {
    return BuildingBlockType__get_isFeaturedColumn(this$.Type);
}

/**
 * Check if .Type is term column type
 */
export function BuildingBlockNamePrePrint__get_isTermColumn(this$) {
    return BuildingBlockType__get_isTermColumn(this$.Type);
}

export class ColumnCoreNames extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["TermSourceRef", "TermAccessionNumber", "Unit"];
    }
}

export function ColumnCoreNames_$reflection() {
    return union_type("Shared.OfficeInteropTypes.ColumnCoreNames", [], ColumnCoreNames, () => [[], [], []]);
}

export function ColumnCoreNames__get_toString(this$) {
    switch (this$.tag) {
        case 1:
            return "Term Accession Number";
        case 2:
            return "Unit";
        default:
            return "Term Source REF";
    }
}

export class SwateColumnHeader extends Record {
    constructor(SwateColumnHeader) {
        super();
        this.SwateColumnHeader = SwateColumnHeader;
    }
}

export function SwateColumnHeader_$reflection() {
    return record_type("Shared.OfficeInteropTypes.SwateColumnHeader", [], SwateColumnHeader, () => [["SwateColumnHeader", string_type]]);
}

export function SwateColumnHeader_create_Z721C83C5(headerString) {
    return new SwateColumnHeader(headerString);
}

export function SwateColumnHeader__get_isMainColumn(this$) {
    const isExistingType = tryFind((t) => (this$.SwateColumnHeader.indexOf(BuildingBlockType__get_toString(t)) === 0), BuildingBlockType_get_All());
    if (isExistingType == null) {
        return false;
    }
    else {
        const t_1 = isExistingType;
        return true;
    }
}

export function SwateColumnHeader__get_isSingleCol(this$) {
    if (SwateColumnHeader__get_isMainColumn(this$)) {
        const bbType = SwateColumnHeader__get_getColumnCoreName(this$);
        if (bbType == null) {
            throw new Error(`Cannot get ColumnCoreName from ${this$.SwateColumnHeader}`);
        }
        else {
            return BuildingBlockType__get_isSingleColumn(BuildingBlockType_ofString_Z721C83C5(bbType));
        }
    }
    else {
        return false;
    }
}

export function SwateColumnHeader__get_isOutputCol(this$) {
    if (SwateColumnHeader__get_isMainColumn(this$)) {
        const bbType = SwateColumnHeader__get_getColumnCoreName(this$);
        if (bbType == null) {
            throw new Error(`Cannot get ColumnCoreName from ${this$.SwateColumnHeader}`);
        }
        else {
            return BuildingBlockType__get_isOutputColumn(BuildingBlockType_ofString_Z721C83C5(bbType));
        }
    }
    else {
        return false;
    }
}

export function SwateColumnHeader__get_isInputCol(this$) {
    if (SwateColumnHeader__get_isMainColumn(this$)) {
        const bbType = SwateColumnHeader__get_getColumnCoreName(this$);
        if (bbType == null) {
            throw new Error(`Cannot get ColumnCoreName from ${this$.SwateColumnHeader}`);
        }
        else {
            return BuildingBlockType__get_isInputColumn(BuildingBlockType_ofString_Z721C83C5(bbType));
        }
    }
    else {
        return false;
    }
}

/**
 * This function returns true if the SwateColumnHeader can be parsed to a featured column. A featured column can
 * be abstracted by Parameter/Factor/Characteristics and describes one common usecase of either.
 * Such a block will contain TSR and TAN and can be used for directed Term search.
 */
export function SwateColumnHeader__get_isFeaturedCol(this$) {
    if (SwateColumnHeader__get_isMainColumn(this$)) {
        const bbType = SwateColumnHeader__get_getColumnCoreName(this$);
        if (bbType == null) {
            throw new Error(`Cannot get ColumnCoreName from ${this$.SwateColumnHeader}`);
        }
        else {
            return BuildingBlockType__get_isFeaturedColumn(BuildingBlockType_ofString_Z721C83C5(bbType));
        }
    }
    else {
        return false;
    }
}

/**
 * The name "TermColumn" refers to all columns with the syntax "Parameter/Factor/etc [TERM-NAME]"
 */
export function SwateColumnHeader__get_isTermColumn(this$) {
    if (SwateColumnHeader__get_isMainColumn(this$)) {
        const bbType = SwateColumnHeader__get_getColumnCoreName(this$);
        if (bbType == null) {
            throw new Error(`Cannot get ColumnCoreName from ${this$.SwateColumnHeader}`);
        }
        else {
            return BuildingBlockType__get_isTermColumn(BuildingBlockType_ofString_Z721C83C5(bbType));
        }
    }
    else {
        return false;
    }
}

export function SwateColumnHeader__get_isUnitCol(this$) {
    return this$.SwateColumnHeader.indexOf(ColumnCoreNames__get_toString(new ColumnCoreNames(2, []))) === 0;
}

export function SwateColumnHeader__get_isTANCol(this$) {
    return this$.SwateColumnHeader.indexOf(ColumnCoreNames__get_toString(new ColumnCoreNames(1, []))) === 0;
}

export function SwateColumnHeader__get_isTSRCol(this$) {
    return this$.SwateColumnHeader.indexOf(ColumnCoreNames__get_toString(new ColumnCoreNames(0, []))) === 0;
}

/**
 * TSR, TAN or Unit
 */
export function SwateColumnHeader__get_isReference(this$) {
    if (SwateColumnHeader__get_isTSRCol(this$) ? true : SwateColumnHeader__get_isTANCol(this$)) {
        return true;
    }
    else {
        return SwateColumnHeader__get_isUnitCol(this$);
    }
}

export function SwateColumnHeader__get_getColumnCoreName(this$) {
    return map((x) => x.trim(), parseCoreName(this$.SwateColumnHeader));
}

export function SwateColumnHeader__get_toBuildingBlockNamePrePrint(this$) {
    const matchValue = SwateColumnHeader__get_getColumnCoreName(this$);
    const matchValue_1 = SwateColumnHeader__get_tryGetOntologyTerm(this$);
    if (matchValue == null) {
        return void 0;
    }
    else if (matchValue_1 != null) {
        const swatecore_1 = matchValue;
        const term = matchValue_1;
        const t_2 = BuildingBlockType_tryOfString_Z721C83C5(swatecore_1);
        if (t_2 != null) {
            const tv = BuildingBlockNamePrePrint_create(value_1(t_2), term);
            return BuildingBlockNamePrePrint_create(value_1(t_2), term);
        }
        else {
            return void 0;
        }
    }
    else {
        const swatecore = matchValue;
        const t = BuildingBlockType_tryOfString_Z721C83C5(swatecore);
        if (t != null) {
            return BuildingBlockNamePrePrint_create(value_1(t), "");
        }
        else {
            return void 0;
        }
    }
}

/**
 * This member returns true if the header is either a main column header ("Source Name", "Protocol Type", "Parameter [xxxx]")
 * or a reference column ("TSR", "TAN", "Unit").
 */
export function SwateColumnHeader__get_isSwateColumnHeader(this$) {
    if (SwateColumnHeader__get_isMainColumn(this$)) {
        return true;
    }
    else if (SwateColumnHeader__get_isReference(this$)) {
        return true;
    }
    else {
        return false;
    }
}

/**
 * Use this function to extract ontology term name from inside square brackets in the main column header
 */
export function SwateColumnHeader__get_tryGetOntologyTerm(this$) {
    return parseSquaredTermNameBrackets(this$.SwateColumnHeader);
}

/**
 * Get term Accession in TSR or TAN from column header
 */
export function SwateColumnHeader__get_tryGetTermAccession(this$) {
    return parseTermAccession(this$.SwateColumnHeader);
}

/**
 * Get column header hash id from main column. E.g. Parameter [Instrument Model#2]
 */
export function SwateColumnHeader__get_tryGetHeaderId(this$) {
    const brackets = parseSquaredTermNameBrackets(this$.SwateColumnHeader);
    if (brackets == null) {
        return void 0;
    }
    else {
        return bind((x) => ("#" + x), getId(brackets));
    }
}

export function SwateColumnHeader__get_getFeaturedColAccession(this$) {
    const bbType = SwateColumnHeader__get_getColumnCoreName(this$);
    if (bbType == null) {
        throw new Error(`Cannot get ColumnCoreName from ${this$.SwateColumnHeader}`);
    }
    else {
        return BuildingBlockType__get_getFeaturedColumnAccession(BuildingBlockType_ofString_Z721C83C5(bbType));
    }
}

export function SwateColumnHeader__get_getFeaturedColTermMinimal(this$) {
    const bbType = SwateColumnHeader__get_getColumnCoreName(this$);
    if (bbType == null) {
        throw new Error(`Cannot get ColumnCoreName from ${this$.SwateColumnHeader}`);
    }
    else {
        return BuildingBlockType__get_getFeaturedColumnTermMinimal(BuildingBlockType_ofString_Z721C83C5(bbType));
    }
}

export class Cell extends Record {
    constructor(Index, Value, Unit) {
        super();
        this.Index = (Index | 0);
        this.Value = Value;
        this.Unit = Unit;
    }
}

export function Cell_$reflection() {
    return record_type("Shared.OfficeInteropTypes.Cell", [], Cell, () => [["Index", int32_type], ["Value", option_type(string_type)], ["Unit", option_type(TermTypes_TermMinimal_$reflection())]]);
}

export function Cell_create(ind, value, unit) {
    return new Cell(ind, value, unit);
}

export function Cell_init_23E0C2A3(index, v, unit) {
    return new Cell(index, v, unit);
}

export class Column extends Record {
    constructor(Index, Header, Cells) {
        super();
        this.Index = (Index | 0);
        this.Header = Header;
        this.Cells = Cells;
    }
}

export function Column_$reflection() {
    return record_type("Shared.OfficeInteropTypes.Column", [], Column, () => [["Index", int32_type], ["Header", SwateColumnHeader_$reflection()], ["Cells", array_type(Cell_$reflection())]]);
}

export function Column_create(ind, headerOpt, cellsArr) {
    return new Column(ind, headerOpt, cellsArr);
}

export class BuildingBlock extends Record {
    constructor(MainColumn, MainColumnTerm, Unit, TSR, TAN) {
        super();
        this.MainColumn = MainColumn;
        this.MainColumnTerm = MainColumnTerm;
        this.Unit = Unit;
        this.TSR = TSR;
        this.TAN = TAN;
    }
}

export function BuildingBlock_$reflection() {
    return record_type("Shared.OfficeInteropTypes.BuildingBlock", [], BuildingBlock, () => [["MainColumn", Column_$reflection()], ["MainColumnTerm", option_type(TermTypes_TermMinimal_$reflection())], ["Unit", option_type(Column_$reflection())], ["TSR", option_type(Column_$reflection())], ["TAN", option_type(Column_$reflection())]]);
}

export function BuildingBlock_create(mainCol, tsr, tan, unit, mainColTerm) {
    return new BuildingBlock(mainCol, mainColTerm, unit, tsr, tan);
}

export function BuildingBlock__get_hasCompleteTSRTAN(this$) {
    const matchValue = this$.TAN;
    const matchValue_1 = this$.TSR;
    let matchResult, tan, tsr;
    if (matchValue == null) {
        if (matchValue_1 == null) {
            matchResult = 1;
        }
        else {
            matchResult = 2;
        }
    }
    else if (matchValue_1 != null) {
        matchResult = 0;
        tan = matchValue;
        tsr = matchValue_1;
    }
    else {
        matchResult = 2;
    }
    switch (matchResult) {
        case 0:
            return true;
        case 1:
            return false;
        default:
            throw new Error(toText(printf("Swate found unknown building block pattern in building block %s. Found only TSR or TAN."))(this$.MainColumn.Header.SwateColumnHeader));
    }
}

export function BuildingBlock__get_hasUnit(this$) {
    return this$.Unit != null;
}

export function BuildingBlock__get_hasTerm(this$) {
    return this$.MainColumnTerm != null;
}

export function BuildingBlock__get_hasCompleteTerm(this$) {
    if ((this$.MainColumnTerm != null) && (value_1(this$.MainColumnTerm).Name !== "")) {
        return value_1(this$.MainColumnTerm).TermAccession !== "";
    }
    else {
        return false;
    }
}

export class InsertBuildingBlock extends Record {
    constructor(ColumnHeader, ColumnTerm, UnitTerm, Rows) {
        super();
        this.ColumnHeader = ColumnHeader;
        this.ColumnTerm = ColumnTerm;
        this.UnitTerm = UnitTerm;
        this.Rows = Rows;
    }
}

export function InsertBuildingBlock_$reflection() {
    return record_type("Shared.OfficeInteropTypes.InsertBuildingBlock", [], InsertBuildingBlock, () => [["ColumnHeader", BuildingBlockNamePrePrint_$reflection()], ["ColumnTerm", option_type(TermTypes_TermMinimal_$reflection())], ["UnitTerm", option_type(TermTypes_TermMinimal_$reflection())], ["Rows", array_type(TermTypes_TermMinimal_$reflection())]]);
}

export function InsertBuildingBlock_create(header, columnTerm, unitTerm, rows) {
    return new InsertBuildingBlock(header, columnTerm, unitTerm, rows);
}

export function InsertBuildingBlock__get_HasUnit(this$) {
    return this$.UnitTerm != null;
}

export function InsertBuildingBlock__get_HasExistingTerm(this$) {
    return this$.ColumnTerm != null;
}

export function InsertBuildingBlock__get_HasCompleteTerm(this$) {
    if ((this$.ColumnTerm != null) && (value_1(this$.ColumnTerm).Name !== "")) {
        return value_1(this$.ColumnTerm).TermAccession !== "";
    }
    else {
        return false;
    }
}

export function InsertBuildingBlock__get_HasValues(this$) {
    return !equalsWith(equals, this$.Rows, new Array(0));
}

//# sourceMappingURL=OfficeInteropTypes.js.map
