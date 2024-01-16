import { singleton, cons, filter, length, tryFind } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { FSharpMap__get_Item } from "../../../../fable_modules/fable-library.4.9.0/Map.js";
import { parseElement, findElementsByName } from "../../../../fable_modules/Fable.SimpleXml.3.4.0/SimpleXml.fs.js";
import { value as value_1 } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { TableValidation__get_toXml, TableValidation_ofXml_Z721C83C5 } from "../CustomXmlTypes.js";
import { ofXmlElement, serializeXml } from "../../../../fable_modules/Fable.SimpleXml.3.4.0/Generator.fs.js";
import { XmlElement } from "../../../../fable_modules/Fable.SimpleXml.3.4.0/AST.fs.js";

export function getActiveTableXml(tableName, completeCustomXmlParsed) {
    const tablexml = tryFind((swateTableXml) => (FSharpMap__get_Item(swateTableXml.Attributes, "Table") === tableName), findElementsByName("SwateTable", completeCustomXmlParsed));
    if (tablexml != null) {
        return value_1(tablexml);
    }
    else {
        return void 0;
    }
}

export function Validation_getSwateValidationForCurrentTable(tableName, xmlParsed) {
    const activeTableXml = getActiveTableXml(tableName, xmlParsed);
    if (activeTableXml == null) {
        return void 0;
    }
    else {
        const v = findElementsByName("TableValidation", value_1(activeTableXml));
        if (length(v) > 1) {
            throw new Error(`Swate found multiple '<${"TableValidation"}>' xml elements. Please contact the developer.`);
        }
        if (length(v) === 0) {
            return void 0;
        }
        else {
            return TableValidation_ofXml_Z721C83C5(serializeXml(ofXmlElement(value_1(activeTableXml))));
        }
    }
}

function Validation_updateRemoveSwateValidation(tableValidation, previousCompleteCustomXml, remove) {
    let newValidationXml, filteredChildren, bind$0040, swateTableXmlEle;
    const currentTableXml = getActiveTableXml(tableValidation.AnnotationTable, previousCompleteCustomXml);
    return new XmlElement(previousCompleteCustomXml.Namespace, previousCompleteCustomXml.Name, previousCompleteCustomXml.Attributes, previousCompleteCustomXml.Content, cons((newValidationXml = parseElement(TableValidation__get_toXml(tableValidation)), (currentTableXml != null) ? ((filteredChildren = filter((x) => (x.Name !== "TableValidation"), value_1(currentTableXml).Children), (bind$0040 = value_1(currentTableXml), new XmlElement(bind$0040.Namespace, bind$0040.Name, bind$0040.Attributes, bind$0040.Content, remove ? filteredChildren : cons(newValidationXml, filteredChildren), bind$0040.SelfClosing, bind$0040.IsTextNode, bind$0040.IsComment)))) : ((swateTableXmlEle = parseElement(`<SwateTable Table="${tableValidation.AnnotationTable}"></SwateTable>`), new XmlElement(swateTableXmlEle.Namespace, swateTableXmlEle.Name, swateTableXmlEle.Attributes, swateTableXmlEle.Content, singleton(newValidationXml), swateTableXmlEle.SelfClosing, swateTableXmlEle.IsTextNode, swateTableXmlEle.IsComment)))), filter((x_1) => !((x_1.Name === "SwateTable") && (FSharpMap__get_Item(x_1.Attributes, "Table") === tableValidation.AnnotationTable)), previousCompleteCustomXml.Children)), previousCompleteCustomXml.SelfClosing, previousCompleteCustomXml.IsTextNode, previousCompleteCustomXml.IsComment);
}

export function Validation_updateSwateValidation(tableValidation, previousCompleteCustomXml) {
    return Validation_updateRemoveSwateValidation(tableValidation, previousCompleteCustomXml, false);
}

export function Validation_removeSwateValidation(tableValidation, previousCompleteCustomXml) {
    return Validation_updateRemoveSwateValidation(tableValidation, previousCompleteCustomXml, true);
}

//# sourceMappingURL=CustomXmlFunctions.js.map
