import { Record } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, bool_type, option_type, string_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";

export class AdvancedSearchOptions extends Record {
    constructor(OntologyName, TermName, TermDefinition, KeepObsolete) {
        super();
        this.OntologyName = OntologyName;
        this.TermName = TermName;
        this.TermDefinition = TermDefinition;
        this.KeepObsolete = KeepObsolete;
    }
}

export function AdvancedSearchOptions_$reflection() {
    return record_type("Shared.AdvancedSearchTypes.AdvancedSearchOptions", [], AdvancedSearchOptions, () => [["OntologyName", option_type(string_type)], ["TermName", string_type], ["TermDefinition", string_type], ["KeepObsolete", bool_type]]);
}

export function AdvancedSearchOptions_init() {
    return new AdvancedSearchOptions(void 0, "", "", false);
}

//# sourceMappingURL=AdvancedSearchTypes.js.map
