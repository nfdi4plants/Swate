import { Record, Union } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, anonRecord_type, string_type, lambda_type, unit_type, union_type, class_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { ArcStudy_$reflection, ArcAssay_$reflection } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/ArcTypes.fs.js";

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Init", "Error", "AssayToARCitect", "StudyToARCitect", "TriggerSwateClose"];
    }
}

export function Msg_$reflection() {
    return union_type("Model.ARCitect.Msg", [], Msg, () => [[], [["Item", class_type("System.Exception")]], [["Item", ArcAssay_$reflection()]], [["Item", ArcStudy_$reflection()]], []]);
}

export class IEventHandler extends Record {
    constructor(Error$, AssayToSwate, StudyToSwate) {
        super();
        this.Error = Error$;
        this.AssayToSwate = AssayToSwate;
        this.StudyToSwate = StudyToSwate;
    }
}

export function IEventHandler_$reflection() {
    return record_type("Model.ARCitect.IEventHandler", [], IEventHandler, () => [["Error", lambda_type(class_type("System.Exception"), unit_type)], ["AssayToSwate", lambda_type(anonRecord_type(["ArcAssayJsonString", string_type]), unit_type)], ["StudyToSwate", lambda_type(anonRecord_type(["ArcStudyJsonString", string_type]), unit_type)]]);
}

//# sourceMappingURL=ARCitect.js.map
