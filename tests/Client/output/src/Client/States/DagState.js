import { Union, Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { union_type, tuple_type, array_type, record_type, option_type, string_type, bool_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { BuildingBlock_$reflection } from "../../Shared/OfficeInteropTypes.js";

export class Model extends Record {
    constructor(Loading, DagHtml) {
        super();
        this.Loading = Loading;
        this.DagHtml = DagHtml;
    }
}

export function Model_$reflection() {
    return record_type("Dag.Model", [], Model, () => [["Loading", bool_type], ["DagHtml", option_type(string_type)]]);
}

export function Model_init() {
    return new Model(false, void 0);
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["UpdateLoading", "ParseTablesOfficeInteropRequest", "ParseTablesDagServerRequest", "ParseTablesDagServerResponse"];
    }
}

export function Msg_$reflection() {
    return union_type("Dag.Msg", [], Msg, () => [[["Item", bool_type]], [], [["Item", array_type(tuple_type(string_type, array_type(BuildingBlock_$reflection())))]], [["dagHtml", string_type]]]);
}

//# sourceMappingURL=DagState.js.map
