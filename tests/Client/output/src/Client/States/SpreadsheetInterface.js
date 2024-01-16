import { Union } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { Swatehost_$reflection } from "../Host.js";
import { union_type, list_type, string_type, array_type, bool_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { CompositeColumn_$reflection } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeColumn.fs.js";
import { ARCtrlHelper_ArcFiles_$reflection } from "../../Shared/ARCtrl.Helper.js";
import { OntologyAnnotation_$reflection } from "../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/OntologyAnnotation.fs.js";
import { TermTypes_TermSearchable_$reflection } from "../../Shared/TermTypes.js";

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Initialize", "CreateAnnotationTable", "RemoveBuildingBlock", "AddAnnotationBlock", "AddAnnotationBlocks", "ImportFile", "EditBuildingBlock", "InsertOntologyTerm", "InsertFileNames", "ExportJsonTable", "ExportJsonTables", "ParseTablesToDag", "UpdateTermColumns", "UpdateTermColumnsResponse"];
    }
}

export function Msg_$reflection() {
    return union_type("SpreadsheetInterface.Msg", [], Msg, () => [[["Item", Swatehost_$reflection()]], [["tryUsePrevOutput", bool_type]], [], [["Item", CompositeColumn_$reflection()]], [["Item", array_type(CompositeColumn_$reflection())]], [["Item", ARCtrlHelper_ArcFiles_$reflection()]], [], [["Item", OntologyAnnotation_$reflection()]], [["Item", list_type(string_type)]], [], [], [], [], [["Item", array_type(TermTypes_TermSearchable_$reflection())]]]);
}

//# sourceMappingURL=SpreadsheetInterface.js.map
