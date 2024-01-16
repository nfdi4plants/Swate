import { Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, string_type, int32_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { Convert_fromJson, Convert_serialize } from "../../../fable_modules/Fable.SimpleJson.3.24.0/Json.Converter.fs.js";
import { createTypeInfo } from "../../../fable_modules/Fable.SimpleJson.3.24.0/TypeInfo.Converter.fs.js";
import { FSharpResult$2 } from "../../../fable_modules/fable-library.4.9.0/Choice.js";
import { SimpleJson_tryParse } from "../../../fable_modules/Fable.SimpleJson.3.24.0/SimpleJson.fs.js";
import { maxBy, max } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { FSharpMap__get_Keys } from "../../../fable_modules/fable-library.4.9.0/Map.js";
import { comparePrimitives, compare } from "../../../fable_modules/fable-library.4.9.0/Util.js";

export class FooterReorderData extends Record {
    constructor(OriginOrder, OriginId) {
        super();
        this.OriginOrder = (OriginOrder | 0);
        this.OriginId = OriginId;
    }
}

export function FooterReorderData_$reflection() {
    return record_type("Spreadsheet.Types.FooterReorderData", [], FooterReorderData, () => [["OriginOrder", int32_type], ["OriginId", string_type]]);
}

export function FooterReorderData_create(o_order, o_id) {
    return new FooterReorderData(o_order, o_id);
}

export function FooterReorderData__toJson(this$) {
    return Convert_serialize(this$, createTypeInfo(FooterReorderData_$reflection()));
}

export function FooterReorderData_ofJson_Z721C83C5(json) {
    let matchValue;
    try {
        return new FSharpResult$2(0, [(matchValue = SimpleJson_tryParse(json), (matchValue != null) ? Convert_fromJson(matchValue, createTypeInfo(FooterReorderData_$reflection())) : (() => {
            throw new Error("Couldn\'t parse the input JSON string because it seems to be invalid");
        })())]);
    }
    catch (ex) {
        return new FSharpResult$2(1, [ex.message]);
    }
}

export function Map_maxKey(m) {
    return max(FSharpMap__get_Keys(m), {
        Compare: compare,
    });
}

/**
 * This function operates on a integer tuple as map key. It will return the highest int for fst and highest int for snd.
 */
export function Map_maxKeys(m) {
    return [maxBy((tuple) => tuple[0], FSharpMap__get_Keys(m), {
        Compare: comparePrimitives,
    })[0], maxBy((tuple_2) => tuple_2[1], FSharpMap__get_Keys(m), {
        Compare: comparePrimitives,
    })[1]];
}

//# sourceMappingURL=Types.js.map
