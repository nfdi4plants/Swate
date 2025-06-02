import { Record, Union } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Types.js";
import { list_type, int64_type, bool_type, record_type, class_type, string_type, union_type, TypeInfo } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Reflection.js";
import { IComparable, IEquatable } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Util.js";
import { int64 } from "../Components/src/fable_modules/fable-library-ts.4.24.0/BigInt.js";
import { FSharpList } from "../Components/src/fable_modules/fable-library-ts.4.24.0/List.js";

export type FullTextSearch_$union = 
    | FullTextSearch<0>
    | FullTextSearch<1>
    | FullTextSearch<2>
    | FullTextSearch<3>

export type FullTextSearch_$cases = {
    0: ["Exact", []],
    1: ["Complete", []],
    2: ["PerformanceComplete", []],
    3: ["Fuzzy", []]
}

export function FullTextSearch_Exact() {
    return new FullTextSearch<0>(0, []);
}

export function FullTextSearch_Complete() {
    return new FullTextSearch<1>(1, []);
}

export function FullTextSearch_PerformanceComplete() {
    return new FullTextSearch<2>(2, []);
}

export function FullTextSearch_Fuzzy() {
    return new FullTextSearch<3>(3, []);
}

export class FullTextSearch<Tag extends keyof FullTextSearch_$cases> extends Union<Tag, FullTextSearch_$cases[Tag][0]> {
    constructor(readonly tag: Tag, readonly fields: FullTextSearch_$cases[Tag][1]) {
        super();
    }
    cases() {
        return ["Exact", "Complete", "PerformanceComplete", "Fuzzy"];
    }
}

export function FullTextSearch_$reflection(): TypeInfo {
    return union_type("Swate.Components.Shared.Database.FullTextSearch", [], FullTextSearch, () => [[], [], [], []]);
}

export class Ontology extends Record implements IEquatable<Ontology>, IComparable<Ontology> {
    readonly Name: string;
    readonly Version: string;
    readonly LastUpdated: Date;
    readonly Author: string;
    constructor(Name: string, Version: string, LastUpdated: Date, Author: string) {
        super();
        this.Name = Name;
        this.Version = Version;
        this.LastUpdated = LastUpdated;
        this.Author = Author;
    }
}

export function Ontology_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.Database.Ontology", [], Ontology, () => [["Name", string_type], ["Version", string_type], ["LastUpdated", class_type("System.DateTime")], ["Author", string_type]]);
}

export function Ontology_create(name: string, version: string, lastUpdated: Date, authors: string): Ontology {
    return new Ontology(name, version, lastUpdated, authors);
}

export class Term extends Record implements IEquatable<Term>, IComparable<Term> {
    readonly Accession: string;
    readonly Name: string;
    readonly Description: string;
    readonly IsObsolete: boolean;
    readonly FK_Ontology: string;
    constructor(Accession: string, Name: string, Description: string, IsObsolete: boolean, FK_Ontology: string) {
        super();
        this.Accession = Accession;
        this.Name = Name;
        this.Description = Description;
        this.IsObsolete = IsObsolete;
        this.FK_Ontology = FK_Ontology;
    }
}

export function Term_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.Database.Term", [], Term, () => [["Accession", string_type], ["Name", string_type], ["Description", string_type], ["IsObsolete", bool_type], ["FK_Ontology", string_type]]);
}

export function Term_createTerm_Z1F90B1F6(accession: string, name: string, description: string, isObsolete: boolean, ontologyName: string): Term {
    return new Term(accession, name, description, isObsolete, ontologyName);
}

export class TermRelationship extends Record implements IEquatable<TermRelationship>, IComparable<TermRelationship> {
    readonly TermID: int64;
    readonly Relationship: string;
    readonly RelatedTermID: int64;
    constructor(TermID: int64, Relationship: string, RelatedTermID: int64) {
        super();
        this.TermID = TermID;
        this.Relationship = Relationship;
        this.RelatedTermID = RelatedTermID;
    }
}

export function TermRelationship_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.Database.TermRelationship", [], TermRelationship, () => [["TermID", int64_type], ["Relationship", string_type], ["RelatedTermID", int64_type]]);
}

export class TreeTypes_TreeTerm extends Record implements IEquatable<TreeTypes_TreeTerm>, IComparable<TreeTypes_TreeTerm> {
    readonly NodeId: int64;
    readonly Term: Term;
    constructor(NodeId: int64, Term: Term) {
        super();
        this.NodeId = NodeId;
        this.Term = Term;
    }
}

export function TreeTypes_TreeTerm_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.Database.TreeTypes.TreeTerm", [], TreeTypes_TreeTerm, () => [["NodeId", int64_type], ["Term", Term_$reflection()]]);
}

export class TreeTypes_TreeRelationship extends Record implements IEquatable<TreeTypes_TreeRelationship>, IComparable<TreeTypes_TreeRelationship> {
    readonly RelationshipId: int64;
    readonly StartNodeId: int64;
    readonly EndNodeId: int64;
    readonly Type: string;
    constructor(RelationshipId: int64, StartNodeId: int64, EndNodeId: int64, Type: string) {
        super();
        this.RelationshipId = RelationshipId;
        this.StartNodeId = StartNodeId;
        this.EndNodeId = EndNodeId;
        this.Type = Type;
    }
}

export function TreeTypes_TreeRelationship_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.Database.TreeTypes.TreeRelationship", [], TreeTypes_TreeRelationship, () => [["RelationshipId", int64_type], ["StartNodeId", int64_type], ["EndNodeId", int64_type], ["Type", string_type]]);
}

export class TreeTypes_Tree extends Record implements IEquatable<TreeTypes_Tree>, IComparable<TreeTypes_Tree> {
    readonly Nodes: FSharpList<TreeTypes_TreeTerm>;
    readonly Relationships: FSharpList<TreeTypes_TreeRelationship>;
    constructor(Nodes: FSharpList<TreeTypes_TreeTerm>, Relationships: FSharpList<TreeTypes_TreeRelationship>) {
        super();
        this.Nodes = Nodes;
        this.Relationships = Relationships;
    }
}

export function TreeTypes_Tree_$reflection(): TypeInfo {
    return record_type("Swate.Components.Shared.Database.TreeTypes.Tree", [], TreeTypes_Tree, () => [["Nodes", list_type(TreeTypes_TreeTerm_$reflection())], ["Relationships", list_type(TreeTypes_TreeRelationship_$reflection())]]);
}

