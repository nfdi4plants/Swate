import { Record, toString, Union } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { list_type, class_type, record_type, option_type, int32_type, union_type, string_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { split, join, replace, printf, toText } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { SwateColumnHeader_create_Z721C83C5, SwateColumnHeader_$reflection } from "../../Shared/OfficeInteropTypes.js";
import { some, value } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { parse, toString as toString_1, now, toUniversalTime } from "../../../fable_modules/fable-library.4.9.0/Date.js";
import { map as map_1, ofSeq, ofArray, empty } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { leaf, attr_value_Z384F8060, node, serializeXml } from "../../../fable_modules/Fable.SimpleXml.3.4.0/Generator.fs.js";
import { map, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { int32ToString } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { parseElement, tryFindElementByName } from "../../../fable_modules/Fable.SimpleXml.3.4.0/SimpleXml.fs.js";
import { FSharpMap__get_Item } from "../../../fable_modules/fable-library.4.9.0/Map.js";
import { parse as parse_1 } from "../../../fable_modules/fable-library.4.9.0/Int32.js";

export class ContentType extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["OntologyTerm", "UnitTerm", "Text", "Url", "Boolean", "Number", "Int"];
    }
}

export function ContentType_$reflection() {
    return union_type("OfficeInterop.CustomXmlTypes.Validation.ContentType", [], ContentType, () => [[["Item", string_type]], [["Item", string_type]], [], [], [], [], []]);
}

export function ContentType__get_toReadableString(this$) {
    switch (this$.tag) {
        case 0:
            return toText(printf("Ontology [%s]"))(this$.fields[0]);
        case 1:
            return toText(printf("Unit [%s]"))(this$.fields[0]);
        default:
            return toString(this$);
    }
}

export function ContentType_ofString_Z721C83C5(str) {
    if (str.indexOf("OntologyTerm ") === 0) {
        return new ContentType(0, [replace(replace(str, "OntologyTerm ", ""), "\"", "")]);
    }
    else if (str.indexOf("UnitTerm ") === 0) {
        return new ContentType(1, [replace(replace(str, "UnitTerm ", ""), "\"", "")]);
    }
    else {
        switch (str) {
            case "Text":
                return new ContentType(2, []);
            case "Url":
                return new ContentType(3, []);
            case "Boolean":
                return new ContentType(4, []);
            case "Number":
                return new ContentType(5, []);
            case "Int":
                return new ContentType(6, []);
            default:
                throw new Error(toText(printf("Tried parsing \'%s\' to ContenType. No match found."))(str));
        }
    }
}

export class ColumnValidation extends Record {
    constructor(ColumnHeader, ColumnAdress, Importance, ValidationFormat, Unit) {
        super();
        this.ColumnHeader = ColumnHeader;
        this.ColumnAdress = ColumnAdress;
        this.Importance = Importance;
        this.ValidationFormat = ValidationFormat;
        this.Unit = Unit;
    }
}

export function ColumnValidation_$reflection() {
    return record_type("OfficeInterop.CustomXmlTypes.Validation.ColumnValidation", [], ColumnValidation, () => [["ColumnHeader", SwateColumnHeader_$reflection()], ["ColumnAdress", option_type(int32_type)], ["Importance", option_type(int32_type)], ["ValidationFormat", option_type(ContentType_$reflection())], ["Unit", option_type(string_type)]]);
}

export function ColumnValidation_create(colHeader, colAdress, importance, validationFormat, unit) {
    return new ColumnValidation(colHeader, colAdress, importance, validationFormat, unit);
}

export function ColumnValidation_init_6B93DA67(colHeader, colAdress) {
    return new ColumnValidation(SwateColumnHeader_create_Z721C83C5(colHeader), (colAdress != null) ? value(colAdress) : void 0, void 0, void 0, void 0);
}

export function ColumnValidation_ofBuildingBlock_Z72417F1D(buildingBlock) {
    return ColumnValidation_init_6B93DA67(buildingBlock.MainColumn.Header.SwateColumnHeader, some(buildingBlock.MainColumn.Index));
}

export class TableValidation extends Record {
    constructor(DateTime, SwateVersion, AnnotationTable, Userlist, ColumnValidations) {
        super();
        this.DateTime = DateTime;
        this.SwateVersion = SwateVersion;
        this.AnnotationTable = AnnotationTable;
        this.Userlist = Userlist;
        this.ColumnValidations = ColumnValidations;
    }
}

export function TableValidation_$reflection() {
    return record_type("OfficeInterop.CustomXmlTypes.Validation.TableValidation", [], TableValidation, () => [["DateTime", class_type("System.DateTime")], ["SwateVersion", string_type], ["AnnotationTable", string_type], ["Userlist", list_type(string_type)], ["ColumnValidations", list_type(ColumnValidation_$reflection())]]);
}

export function TableValidation_create(swateVersion, tableName, dateTime, userlist, colValidations) {
    return new TableValidation(dateTime, swateVersion, tableName, userlist, colValidations);
}

export function TableValidation_init_Z30026FB0(swateVersion, worksheetName, tableName, dateTime, userList) {
    const SwateVersion = (swateVersion != null) ? value(swateVersion) : "";
    const AnnotationTable = (tableName != null) ? value(tableName) : "";
    return new TableValidation((dateTime != null) ? value(dateTime) : toUniversalTime(now()), SwateVersion, AnnotationTable, (userList != null) ? value(userList) : empty(), empty());
}

export function TableValidation__get_toXml(this$) {
    return serializeXml(node("TableValidation", ofArray([attr_value_Z384F8060("SwateVersion", this$.SwateVersion), attr_value_Z384F8060("TableName", this$.AnnotationTable), attr_value_Z384F8060("DateTime", toString_1(this$.DateTime, "yyyy-MM-dd HH:mm")), attr_value_Z384F8060("Userlist", join("; ", this$.Userlist))]), toList(delay(() => map((column) => leaf("ColumnValidation", ofArray([attr_value_Z384F8060("ColumnHeader", column.ColumnHeader.SwateColumnHeader), attr_value_Z384F8060("ColumnAdress", (column.ColumnAdress != null) ? int32ToString(value(column.ColumnAdress)) : "None"), attr_value_Z384F8060("Importance", (column.Importance != null) ? int32ToString(value(column.Importance)) : "None"), attr_value_Z384F8060("ValidationFormat", (column.ValidationFormat != null) ? toString(value(column.ValidationFormat)) : "None"), attr_value_Z384F8060("Unit", (column.Unit != null) ? value(column.Unit) : "None")])), this$.ColumnValidations)))));
}

export function TableValidation_ofXml_Z721C83C5(xmlString) {
    const tableValidationOpt = tryFindElementByName("TableValidation", parseElement(xmlString));
    if (tableValidationOpt != null) {
        const tableValidation = tableValidationOpt;
        return TableValidation_create(FSharpMap__get_Item(tableValidation.Attributes, "SwateVersion"), FSharpMap__get_Item(tableValidation.Attributes, "TableName"), parse(FSharpMap__get_Item(tableValidation.Attributes, "DateTime")), ofSeq(split(FSharpMap__get_Item(tableValidation.Attributes, "Userlist"), ["; "], void 0, 1)), map_1((column) => {
            let x, x_1, x_2, x_3;
            return ColumnValidation_create(SwateColumnHeader_create_Z721C83C5(FSharpMap__get_Item(column.Attributes, "ColumnHeader")), (x = FSharpMap__get_Item(column.Attributes, "ColumnAdress"), (x === "None") ? void 0 : parse_1(x, 511, false, 32)), (x_1 = FSharpMap__get_Item(column.Attributes, "Importance"), (x_1 === "None") ? void 0 : parse_1(x_1, 511, false, 32)), (x_2 = FSharpMap__get_Item(column.Attributes, "ValidationFormat"), (x_2 === "None") ? void 0 : ContentType_ofString_Z721C83C5(x_2)), (x_3 = FSharpMap__get_Item(column.Attributes, "Unit"), (x_3 === "None") ? void 0 : x_3));
        }, tableValidation.Children));
    }
    else {
        throw new Error(toText(printf("Could not find existing <%s> tag."))("TableValidation"));
    }
}

//# sourceMappingURL=CustomXmlTypes.js.map
