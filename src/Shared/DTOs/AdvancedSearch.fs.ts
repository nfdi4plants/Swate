import { Record } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Types.js";
import { Option } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Option.js";
import { IComparable, IEquatable } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Util.js";
import { record_type, bool_type, option_type, string_type, TypeInfo } from "../../Components/src/fable_modules/fable-library-ts.4.24.0/Reflection.js";

export class AdvancedSearchQuery extends Record implements IEquatable<AdvancedSearchQuery>, IComparable<AdvancedSearchQuery> {
    readonly OntologyName: Option<string>;
    readonly TermName: string;
    readonly TermDefinition: string;
    readonly KeepObsolete: boolean;
    constructor(OntologyName: Option<string>, TermName: string, TermDefinition: string, KeepObsolete: boolean) {
        super();
        this.OntologyName = OntologyName;
        this.TermName = TermName;
        this.TermDefinition = TermDefinition;
        this.KeepObsolete = KeepObsolete;
    }
}

export function AdvancedSearchQuery_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.DTOs.AdvancedSearchQuery", [], AdvancedSearchQuery, () => [["OntologyName", option_type(string_type)], ["TermName", string_type], ["TermDefinition", string_type], ["KeepObsolete", bool_type]]);
}

export function AdvancedSearchQuery_init(): AdvancedSearchQuery {
    return new AdvancedSearchQuery(undefined, "", "", false);
}

