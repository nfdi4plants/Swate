import { Record } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { list_type, array_type, int32_type, option_type, int64_type, bool_type, record_type, class_type, string_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";
import { parseDoubleQuotes } from "./Regex.js";
import { value } from "../../fable_modules/fable-library.4.9.0/Option.js";
import { replace, split } from "../../fable_modules/fable-library.4.9.0/String.js";

export class TermTypes_Ontology extends Record {
    constructor(Name, Version, LastUpdated, Author) {
        super();
        this.Name = Name;
        this.Version = Version;
        this.LastUpdated = LastUpdated;
        this.Author = Author;
    }
}

export function TermTypes_Ontology_$reflection() {
    return record_type("Shared.TermTypes.Ontology", [], TermTypes_Ontology, () => [["Name", string_type], ["Version", string_type], ["LastUpdated", class_type("System.DateTime")], ["Author", string_type]]);
}

export function TermTypes_Ontology_create(name, version, lastUpdated, authors) {
    return new TermTypes_Ontology(name, version, lastUpdated, authors);
}

export class TermTypes_Term extends Record {
    constructor(Accession, Name, Description, IsObsolete, FK_Ontology) {
        super();
        this.Accession = Accession;
        this.Name = Name;
        this.Description = Description;
        this.IsObsolete = IsObsolete;
        this.FK_Ontology = FK_Ontology;
    }
}

export function TermTypes_Term_$reflection() {
    return record_type("Shared.TermTypes.Term", [], TermTypes_Term, () => [["Accession", string_type], ["Name", string_type], ["Description", string_type], ["IsObsolete", bool_type], ["FK_Ontology", string_type]]);
}

export function TermTypes_createTerm(accession, name, description, isObsolete, ontologyName) {
    return new TermTypes_Term(accession, name, description, isObsolete, ontologyName);
}

export class TermTypes_TermRelationship extends Record {
    constructor(TermID, Relationship, RelatedTermID) {
        super();
        this.TermID = TermID;
        this.Relationship = Relationship;
        this.RelatedTermID = RelatedTermID;
    }
}

export function TermTypes_TermRelationship_$reflection() {
    return record_type("Shared.TermTypes.TermRelationship", [], TermTypes_TermRelationship, () => [["TermID", int64_type], ["Relationship", string_type], ["RelatedTermID", int64_type]]);
}

export class TermTypes_TermMinimal extends Record {
    constructor(Name, TermAccession) {
        super();
        this.Name = Name;
        this.TermAccession = TermAccession;
    }
}

export function TermTypes_TermMinimal_$reflection() {
    return record_type("Shared.TermTypes.TermMinimal", [], TermTypes_TermMinimal, () => [["Name", string_type], ["TermAccession", string_type]]);
}

export function TermTypes_TermMinimal_create(name, termAccession) {
    return new TermTypes_TermMinimal(name, termAccession);
}

export function TermTypes_TermMinimal_ofTerm_Z5E0A7659(term) {
    return new TermTypes_TermMinimal(term.Name, term.Accession);
}

export function TermTypes_TermMinimal_get_empty() {
    return new TermTypes_TermMinimal("", "");
}

export function TermTypes_TermMinimal_fromOntologyAnnotation_Z4C0FE73C(oa) {
    return TermTypes_TermMinimal_create(oa.NameText, oa.TermAccessionShort);
}

/**
 * The numberFormat attribute in Excel allows to create automatic unit extensions.
 * It uses a special input format which is created by this function and should be used for unit terms.
 */
export function TermTypes_TermMinimal__get_toNumberFormat(this$) {
    return `0.00 "${this$.Name}"`;
}

/**
 * This still returns only minimal information, but in term format
 */
export function TermTypes_TermMinimal__get_toTerm(this$) {
    return TermTypes_createTerm(this$.TermAccession, this$.Name, "", false, "");
}

/**
 * The numberFormat attribute in Excel allows to create automatic unit extensions.
 * The format is created as $"0.00 \"{MinimalTerm.Name}\"", this function is meant to reverse this, altough term accession is lost.
 */
export function TermTypes_TermMinimal_ofNumberFormat_Z721C83C5(formatStr) {
    const unitNameOpt = parseDoubleQuotes(formatStr);
    return TermTypes_TermMinimal_create((unitNameOpt == null) ? (() => {
        throw new Error(`Unable to parse given string ${formatStr} to TermMinimal.Name in numberFormat.`);
    })() : value(unitNameOpt), "");
}

/**
 * Returns empty string if no accession is found
 */
export function TermTypes_TermMinimal__get_accessionToTSR(this$) {
    if (this$.TermAccession === "") {
        return "";
    }
    else {
        try {
            return split(this$.TermAccession, [":"], void 0, 0)[0];
        }
        catch (exn) {
            return "";
        }
    }
}

/**
 * Returns empty string if no accession is found
 */
export function TermTypes_TermMinimal__get_accessionToTAN(this$) {
    if (this$.TermAccession === "") {
        return "";
    }
    else {
        return "http://purl.obolibrary.org/obo/" + replace(this$.TermAccession, ":", "_");
    }
}

export class TermTypes_TermSearchable extends Record {
    constructor(Term, ParentTerm, IsUnit, ColIndex, RowIndices, SearchResultTerm) {
        super();
        this.Term = Term;
        this.ParentTerm = ParentTerm;
        this.IsUnit = IsUnit;
        this.ColIndex = (ColIndex | 0);
        this.RowIndices = RowIndices;
        this.SearchResultTerm = SearchResultTerm;
    }
}

export function TermTypes_TermSearchable_$reflection() {
    return record_type("Shared.TermTypes.TermSearchable", [], TermTypes_TermSearchable, () => [["Term", TermTypes_TermMinimal_$reflection()], ["ParentTerm", option_type(TermTypes_TermMinimal_$reflection())], ["IsUnit", bool_type], ["ColIndex", int32_type], ["RowIndices", array_type(int32_type)], ["SearchResultTerm", option_type(TermTypes_Term_$reflection())]]);
}

export function TermTypes_TermSearchable_create(term, parentTerm, isUnit, colInd, rowIndices) {
    return new TermTypes_TermSearchable(term, parentTerm, isUnit, colInd, rowIndices, void 0);
}

export function TermTypes_TermSearchable__get_hasEmptyTerm(this$) {
    if (this$.Term.Name === "") {
        return this$.Term.TermAccession === "";
    }
    else {
        return false;
    }
}

export class TreeTypes_TreeTerm extends Record {
    constructor(NodeId, Term) {
        super();
        this.NodeId = NodeId;
        this.Term = Term;
    }
}

export function TreeTypes_TreeTerm_$reflection() {
    return record_type("Shared.TreeTypes.TreeTerm", [], TreeTypes_TreeTerm, () => [["NodeId", int64_type], ["Term", TermTypes_Term_$reflection()]]);
}

export class TreeTypes_TreeRelationship extends Record {
    constructor(RelationshipId, StartNodeId, EndNodeId, Type) {
        super();
        this.RelationshipId = RelationshipId;
        this.StartNodeId = StartNodeId;
        this.EndNodeId = EndNodeId;
        this.Type = Type;
    }
}

export function TreeTypes_TreeRelationship_$reflection() {
    return record_type("Shared.TreeTypes.TreeRelationship", [], TreeTypes_TreeRelationship, () => [["RelationshipId", int64_type], ["StartNodeId", int64_type], ["EndNodeId", int64_type], ["Type", string_type]]);
}

export class TreeTypes_Tree extends Record {
    constructor(Nodes, Relationships) {
        super();
        this.Nodes = Nodes;
        this.Relationships = Relationships;
    }
}

export function TreeTypes_Tree_$reflection() {
    return record_type("Shared.TreeTypes.Tree", [], TreeTypes_Tree, () => [["Nodes", list_type(TreeTypes_TreeTerm_$reflection())], ["Relationships", list_type(TreeTypes_TreeRelationship_$reflection())]]);
}

//# sourceMappingURL=TermTypes.js.map
