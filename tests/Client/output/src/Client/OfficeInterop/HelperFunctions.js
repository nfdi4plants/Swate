import { SwateColumnHeader_create_Z721C83C5, SwateColumnHeader__get_isReference, TryFindAnnoTableResult_exactlyOneAnnotationTable_Z35CD86D0 } from "../../Shared/OfficeInteropTypes.js";
import { indexed, choose, fill, map } from "../../../fable_modules/fable-library.4.9.0/Array.js";
import { singleton, collect, toList, map as map_1, delay, toArray } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { rangeDouble } from "../../../fable_modules/fable-library.4.9.0/Range.js";
import { value as value_2, some } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { toString } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../../../fable_modules/Fable.Promise.3.2.0/Promise.fs.js";
import { promise } from "../../../fable_modules/Fable.Promise.3.2.0/PromiseImpl.fs.js";
import { printf, toText, join } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { tryParseManyElements, parseElement, tryParseElement } from "../../../fable_modules/Fable.SimpleXml.3.4.0/SimpleXml.fs.js";
import { tryFind } from "../../../fable_modules/fable-library.4.9.0/List.js";

/**
 * ExcelApi 1.1
 */
export function getActiveAnnotationTableName(context) {
    const sheet = context.workbook.worksheets.getActiveWorksheet();
    const t = sheet.load(["tables"]);
    const tableItems = t.tables.load("items");
    return context.sync().then((_arg) => {
        let array_1;
        const res = TryFindAnnoTableResult_exactlyOneAnnotationTable_Z35CD86D0((array_1 = map((x) => x.name, toArray(tableItems.items)), array_1.filter((x_1) => (x_1.indexOf("annotationTable") === 0))));
        if (res.tag === 1) {
            throw new Error(res.fields[0]);
        }
        else {
            return res.fields[0];
        }
    });
}

/**
 * This function returns the names of all annotationTables in all worksheets.
 * This function is used to pass a list of all table names to e.g. the 'createAnnotationTable()' function.
 */
export function getAllTableNames(context) {
    const tables = context.workbook.tables.load(["tables"]);
    tables.load("name");
    return context.sync().then((_arg) => map((x) => x.name, toArray(tables.items)));
}

export function createMatrixForTables(colCount, rowCount, value) {
    return toArray(delay(() => map_1((i) => toArray(delay(() => map_1((i_1) => value, rangeDouble(0, 1, colCount - 1)))), rangeDouble(0, 1, rowCount - 1))));
}

export function createValueMatrix(colCount, rowCount, value) {
    return Array.from(toList(delay(() => collect((outer) => {
        const tmp = map_1((_arg) => some(value), fill(new Array(colCount), 0, colCount, null));
        return singleton(Array.from(tmp));
    }, rangeDouble(0, 1, rowCount - 1)))));
}

/**
 * This function needs an array of the column headers as input. Takes as such:
 * `let annoHeaderRange = annotationTable.getHeaderRowRange()`
 * `annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"|])) |> ignore`
 * `let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq`
 */
export function findIndexNextNotHiddenCol(headerVals, startIndex) {
    const indexedHiddenCols = choose((tupledArg) => {
        const x = tupledArg[1];
        if (x != null) {
            if (SwateColumnHeader__get_isReference(SwateColumnHeader_create_Z721C83C5(toString(value_2(x))))) {
                return tupledArg[0];
            }
            else {
                return void 0;
            }
        }
        else {
            return void 0;
        }
    }, indexed(headerVals), Float64Array);
    const loopingCheckSkipHiddenCols = (newInd_mut) => {
        loopingCheckSkipHiddenCols:
        while (true) {
            const newInd = newInd_mut;
            if (indexedHiddenCols.some((i_1) => (i_1 === (newInd + 1)))) {
                newInd_mut = (newInd + 1);
                continue loopingCheckSkipHiddenCols;
            }
            else {
                return newInd + 1;
            }
            break;
        }
    };
    return loopingCheckSkipHiddenCols(startIndex);
}

export function getCustomXml(customXmlParts, context) {
    return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (context.sync().then((e) => Array.from(map_1((x) => x.getXml(), customXmlParts.items))).then((_arg) => (context.sync().then((e_1) => join("\n", map((x_1) => x_1.value, _arg))).then((_arg_1) => {
        const xml_1 = _arg_1;
        let xmlParsed;
        const isRootElement = tryParseElement(xml_1);
        if (xml_1 === "") {
            xmlParsed = parseElement("<customXml></customXml>");
        }
        else if (isRootElement != null) {
            xmlParsed = value_2(isRootElement);
        }
        else {
            const isManyRootElements = tryParseManyElements(xml_1);
            if (isManyRootElements != null) {
                const customXmlOpt = tryFind((ele) => (ele.Name === "customXml"), value_2(isManyRootElements));
                if (customXmlOpt != null) {
                    xmlParsed = value_2(customXmlOpt);
                }
                else {
                    throw new Error("Swate could not find expected \'<customXml>..</customXml>\' root tag.");
                }
            }
            else {
                throw new Error("Swate could not parse Workbook Custom Xml Parts. Had neither one root nor many root elements. Please contact the developer.");
            }
        }
        return ((xmlParsed.Name !== "customXml") ? (((() => {
            throw new Error(toText(printf("Swate found unexpected root xml element: %s"))(xmlParsed.Name));
        })(), Promise.resolve())) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1(promise, () => (Promise.resolve(xmlParsed))));
    }))))));
}

//# sourceMappingURL=HelperFunctions.js.map
