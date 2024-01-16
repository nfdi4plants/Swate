import { Union, Record } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { union_type, int32_type, class_type, array_type, record_type, list_type, bool_type, option_type, string_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";
import { value as value_1 } from "../../fable_modules/fable-library.4.9.0/Option.js";
import { ofArray, map, isEmpty, empty } from "../../fable_modules/fable-library.4.9.0/List.js";
import { replace } from "../../fable_modules/fable-library.4.9.0/String.js";
import { curry2 } from "../../fable_modules/fable-library.4.9.0/Util.js";
import { InsertBuildingBlock_$reflection } from "./OfficeInteropTypes.js";
import { TermTypes_TermMinimal_$reflection } from "./TermTypes.js";

export class Metadata_MetadataField extends Record {
    constructor(Key, ExtendedNameKey, Description, List, Children) {
        super();
        this.Key = Key;
        this.ExtendedNameKey = ExtendedNameKey;
        this.Description = Description;
        this.List = List;
        this.Children = Children;
    }
}

export function Metadata_MetadataField_$reflection() {
    return record_type("Shared.TemplateTypes.Metadata.MetadataField", [], Metadata_MetadataField, () => [["Key", string_type], ["ExtendedNameKey", string_type], ["Description", option_type(string_type)], ["List", bool_type], ["Children", list_type(Metadata_MetadataField_$reflection())]]);
}

export function Metadata_MetadataField_create_26F95468(key, extKey, desc, islist, children) {
    return new Metadata_MetadataField(key, (extKey != null) ? value_1(extKey) : "", desc, (islist != null) && value_1(islist), (children != null) ? value_1(children) : empty());
}

export function Metadata_MetadataField_createParentKey(parentKey, newKey) {
    const nk = replace(newKey, "#", "");
    return (`${parentKey} ${nk}`).trim();
}

/**
 * Loop through all children to create ExtendedNameKey for all MetaDataField types
 */
export function Metadata_MetadataField__get_extendedNameKeys(this$) {
    const extendName = (parentKey, metadata) => {
        const nextParentKey = Metadata_MetadataField_createParentKey(parentKey, metadata.Key);
        return new Metadata_MetadataField(metadata.Key, nextParentKey, metadata.Description, metadata.List, !isEmpty(metadata.Children) ? map(curry2(extendName)(nextParentKey), metadata.Children) : metadata.Children);
    };
    return extendName("", this$);
}

export function Metadata_MetadataField_createWithExtendedKeys_26F95468(key, extKey, desc, islist, children) {
    return Metadata_MetadataField__get_extendedNameKeys(new Metadata_MetadataField(key, (extKey != null) ? value_1(extKey) : "", desc, (islist != null) && value_1(islist), (children != null) ? value_1(children) : empty()));
}

const Metadata_tsr = Metadata_MetadataField_create_26F95468("Term Source REF");

const Metadata_tan = Metadata_MetadataField_create_26F95468("Term Accession Number");

const Metadata_annotationValue = Metadata_MetadataField_create_26F95468("#");

const Metadata_id = Metadata_MetadataField_create_26F95468("Id", void 0, "The unique identifier of this template. It will be auto generated.");

const Metadata_name = Metadata_MetadataField_create_26F95468("Name", void 0, "The name of the Swate template.");

const Metadata_version = Metadata_MetadataField_create_26F95468("Version", void 0, "The current version of this template in SemVer notation.");

const Metadata_description = Metadata_MetadataField_create_26F95468("Description", void 0, "The description of this template. Use few sentences for succinctness.");

const Metadata_organisation = Metadata_MetadataField_create_26F95468("Organisation", void 0, "The name of the template associated organisation. \"DataPLANT\" will trigger the \"DataPLANT\" batch of honor for the template.");

const Metadata_table = Metadata_MetadataField_create_26F95468("Table", void 0, "The name of the Swate annotation table in the workbook of the template\'s excel file.");

const Metadata_er = Metadata_MetadataField_create_26F95468("ER", void 0, "A list of all ERs (endpoint repositories) targeted with this template. ERs are realized as Terms.", true, ofArray([Metadata_annotationValue, Metadata_tan, Metadata_tsr]));

const Metadata_tags = Metadata_MetadataField_create_26F95468("Tags", void 0, "A list of all tags associated with this template. Tags are realized as Terms.", true, ofArray([Metadata_annotationValue, Metadata_tan, Metadata_tsr]));

const Metadata_lastName = Metadata_MetadataField_create_26F95468("Last Name");

const Metadata_firstName = Metadata_MetadataField_create_26F95468("First Name");

const Metadata_midIntiials = Metadata_MetadataField_create_26F95468("Mid Initials");

const Metadata_email = Metadata_MetadataField_create_26F95468("Email");

const Metadata_phone = Metadata_MetadataField_create_26F95468("Phone");

const Metadata_fax = Metadata_MetadataField_create_26F95468("Fax");

const Metadata_address = Metadata_MetadataField_create_26F95468("Address");

const Metadata_affiliation = Metadata_MetadataField_create_26F95468("Affiliation");

const Metadata_orcid = Metadata_MetadataField_create_26F95468("ORCID");

const Metadata_roleAnnotationValue = Metadata_MetadataField_create_26F95468("Role");

const Metadata_roleTAN = Metadata_MetadataField_create_26F95468("Role Term Accession Number");

const Metadata_roleTSR = Metadata_MetadataField_create_26F95468("Role Term Source REF");

const Metadata_authors = Metadata_MetadataField_create_26F95468("Authors", void 0, "The author(s) of this template.", true, ofArray([Metadata_lastName, Metadata_firstName, Metadata_midIntiials, Metadata_email, Metadata_phone, Metadata_fax, Metadata_address, Metadata_affiliation, Metadata_orcid, Metadata_roleAnnotationValue, Metadata_roleTAN, Metadata_roleTSR]));

export const Metadata_root = Metadata_MetadataField_createWithExtendedKeys_26F95468("", void 0, void 0, void 0, ofArray([Metadata_id, Metadata_name, Metadata_version, Metadata_description, Metadata_organisation, Metadata_table, Metadata_er, Metadata_tags, Metadata_authors]));

export class Template extends Record {
    constructor(Id, Name, Description, Organisation, Version, Authors, Er_Tags, Tags, TemplateBuildingBlocks, LastUpdated, Used, Rating) {
        super();
        this.Id = Id;
        this.Name = Name;
        this.Description = Description;
        this.Organisation = Organisation;
        this.Version = Version;
        this.Authors = Authors;
        this.Er_Tags = Er_Tags;
        this.Tags = Tags;
        this.TemplateBuildingBlocks = TemplateBuildingBlocks;
        this.LastUpdated = LastUpdated;
        this.Used = (Used | 0);
        this.Rating = (Rating | 0);
    }
}

export function Template_$reflection() {
    return record_type("Shared.TemplateTypes.Template", [], Template, () => [["Id", string_type], ["Name", string_type], ["Description", string_type], ["Organisation", string_type], ["Version", string_type], ["Authors", string_type], ["Er_Tags", array_type(string_type)], ["Tags", array_type(string_type)], ["TemplateBuildingBlocks", list_type(InsertBuildingBlock_$reflection())], ["LastUpdated", class_type("System.DateTime")], ["Used", int32_type], ["Rating", int32_type]]);
}

export function Template_create(id, name, describtion, organisation, version, lastUpdated, author, ertags, tags, templateBuildingBlocks, used, rating) {
    return new Template(id, name, describtion, organisation, version, author, ertags, tags, templateBuildingBlocks, lastUpdated, used, rating);
}

export class Organisation extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["DataPLANT", "Other"];
    }
}

export function Organisation_$reflection() {
    return union_type("Shared.TemplateTypes.Organisation", [], Organisation, () => [[], [["Item", string_type]]]);
}

export class Author extends Record {
    constructor(LastName, FirstName, MidInitials, Email, Phone, Fax, Adress, Affiliation, ORCID, Role) {
        super();
        this.LastName = LastName;
        this.FirstName = FirstName;
        this.MidInitials = MidInitials;
        this.Email = Email;
        this.Phone = Phone;
        this.Fax = Fax;
        this.Adress = Adress;
        this.Affiliation = Affiliation;
        this.ORCID = ORCID;
        this.Role = Role;
    }
}

export function Author_$reflection() {
    return record_type("Shared.TemplateTypes.Author", [], Author, () => [["LastName", string_type], ["FirstName", string_type], ["MidInitials", string_type], ["Email", string_type], ["Phone", string_type], ["Fax", string_type], ["Adress", string_type], ["Affiliation", string_type], ["ORCID", string_type], ["Role", TermTypes_TermMinimal_$reflection()]]);
}

export class TemplateForm extends Record {
    constructor(Id, Name, Version, Description, Organisation, Table, ER_List, Tags, Authors) {
        super();
        this.Id = Id;
        this.Name = Name;
        this.Version = Version;
        this.Description = Description;
        this.Organisation = Organisation;
        this.Table = Table;
        this.ER_List = ER_List;
        this.Tags = Tags;
        this.Authors = Authors;
    }
}

export function TemplateForm_$reflection() {
    return record_type("Shared.TemplateTypes.TemplateForm", [], TemplateForm, () => [["Id", class_type("System.Guid")], ["Name", string_type], ["Version", string_type], ["Description", string_type], ["Organisation", Organisation_$reflection()], ["Table", string_type], ["ER_List", array_type(TermTypes_TermMinimal_$reflection())], ["Tags", array_type(TermTypes_TermMinimal_$reflection())], ["Authors", array_type(Author_$reflection())]]);
}

//# sourceMappingURL=TemplateTypes.js.map
