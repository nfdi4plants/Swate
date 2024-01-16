import { Union, Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { union_type, record_type, option_type, obj_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { Metadata_MetadataField_$reflection } from "../../Shared/TemplateTypes.js";

export class Model extends Record {
    constructor(Default, MetadataFields) {
        super();
        this.Default = Default;
        this.MetadataFields = MetadataFields;
    }
}

export function Model_$reflection() {
    return record_type("TemplateMetadata.Model", [], Model, () => [["Default", obj_type], ["MetadataFields", option_type(Metadata_MetadataField_$reflection())]]);
}

export function Model_init() {
    return new Model("", void 0);
}

export class Msg extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["CreateTemplateMetadataWorksheet"];
    }
}

export function Msg_$reflection() {
    return union_type("TemplateMetadata.Msg", [], Msg, () => [[["Item", Metadata_MetadataField_$reflection()]]]);
}

//# sourceMappingURL=TemplateMetadataState.js.map
