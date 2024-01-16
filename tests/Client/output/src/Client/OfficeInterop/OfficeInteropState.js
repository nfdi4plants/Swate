import { Record, Union } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { list_type, tuple_type, string_type, array_type, record_type, bool_type, union_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { InsertBuildingBlock_$reflection, TryFindAnnoTableResult_$reflection } from "../../Shared/OfficeInteropTypes.js";
import { TermTypes_TermSearchable_$reflection, TermTypes_TermMinimal_$reflection } from "../../Shared/TermTypes.js";

export class FillHiddenColsState extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Inactive", "ExcelCheckHiddenCols", "ServerSearchDatabase", "ExcelWriteFoundTerms"];
    }
}

export function FillHiddenColsState_$reflection() {
    return union_type("OfficeInterop.FillHiddenColsState", [], FillHiddenColsState, () => [[], [], [], []]);
}

export function FillHiddenColsState__get_toReadableString(this$) {
    switch (this$.tag) {
        case 1:
            return "Check Hidden Cols";
        case 2:
            return "Search Database";
        case 3:
            return "Write Terms";
        default:
            return "";
    }
}

export class Model extends Record {
    constructor(HasAnnotationTable, FillHiddenColsStateStore) {
        super();
        this.HasAnnotationTable = HasAnnotationTable;
        this.FillHiddenColsStateStore = FillHiddenColsStateStore;
    }
}

export function Model_$reflection() {
    return record_type("OfficeInterop.Model", [], Model, () => [["HasAnnotationTable", bool_type], ["FillHiddenColsStateStore", FillHiddenColsState_$reflection()]]);
}

export function Model_init() {
    return new Model(false, new FillHiddenColsState(0, []));
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["CreateAnnotationTable", "AnnotationtableCreated", "AnnotationTableExists", "InsertOntologyTerm", "AddAnnotationBlock", "AddAnnotationBlocks", "ImportFile", "RemoveBuildingBlock", "UpdateUnitForCells", "AutoFitTable", "GetParentTerm", "FillHiddenColsRequest", "FillHiddenColumns", "UpdateFillHiddenColsState", "GetSelectedBuildingBlockTerms", "InsertFileNames", "TryExcel", "TryExcel2"];
    }
}

export function Msg_$reflection() {
    return union_type("OfficeInterop.Msg", [], Msg, () => [[["tryUsePrevOutput", bool_type]], [], [["Item", TryFindAnnoTableResult_$reflection()]], [["Item", TermTypes_TermMinimal_$reflection()]], [["Item", InsertBuildingBlock_$reflection()]], [["Item", array_type(InsertBuildingBlock_$reflection())]], [["Item", array_type(tuple_type(string_type, array_type(InsertBuildingBlock_$reflection())))]], [], [["unitTerm", TermTypes_TermMinimal_$reflection()]], [["hideRefCols", bool_type]], [], [], [["Item", array_type(TermTypes_TermSearchable_$reflection())]], [["Item", FillHiddenColsState_$reflection()]], [], [["fileNameList", list_type(string_type)]], [], []]);
}

//# sourceMappingURL=OfficeInteropState.js.map
