import { Union, Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { union_type, record_type, option_type, string_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { TreeTypes_Tree_$reflection } from "../../Shared/TermTypes.js";
import { value } from "../../../fable_modules/fable-library.4.9.0/Option.js";

export class Model extends Record {
    constructor(TargetAccession, CyTermTree) {
        super();
        this.TargetAccession = TargetAccession;
        this.CyTermTree = CyTermTree;
    }
}

export function Model_$reflection() {
    return record_type("Cytoscape.Model", [], Model, () => [["TargetAccession", string_type], ["CyTermTree", option_type(TreeTypes_Tree_$reflection())]]);
}

export function Model_init_6DFDD678(accession) {
    return new Model((accession != null) ? value(accession) : "", void 0);
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["GetTermTree", "GetTermTreeResponse"];
    }
}

export function Msg_$reflection() {
    return union_type("Cytoscape.Msg", [], Msg, () => [[["accession", string_type]], [["tree", TreeTypes_Tree_$reflection()]]]);
}

//# sourceMappingURL=CytoscapeState.js.map
