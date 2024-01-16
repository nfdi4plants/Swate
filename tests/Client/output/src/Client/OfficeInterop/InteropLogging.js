import { Record, Union } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, string_type, union_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";

export class LogIdentifier extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Debug", "Info", "Warning", "Error"];
    }
}

export function LogIdentifier_$reflection() {
    return union_type("InteropLogging.LogIdentifier", [], LogIdentifier, () => [[], [], [], []]);
}

export function LogIdentifier_ofString_Z721C83C5(str) {
    switch (str) {
        case "Debug":
            return new LogIdentifier(0, []);
        case "Info":
            return new LogIdentifier(1, []);
        case "Error":
            return new LogIdentifier(3, []);
        case "Warning":
            return new LogIdentifier(2, []);
        default:
            throw new Error(`Unable to parse ${str} to LogIdentifier.`);
    }
}

export class Msg extends Record {
    constructor(LogIdentifier, MessageTxt) {
        super();
        this.LogIdentifier = LogIdentifier;
        this.MessageTxt = MessageTxt;
    }
}

export function Msg_$reflection() {
    return record_type("InteropLogging.Msg", [], Msg, () => [["LogIdentifier", LogIdentifier_$reflection()], ["MessageTxt", string_type]]);
}

export function Msg_create(logIdent, msgTxt) {
    return new Msg(logIdent, msgTxt);
}

//# sourceMappingURL=InteropLogging.js.map
