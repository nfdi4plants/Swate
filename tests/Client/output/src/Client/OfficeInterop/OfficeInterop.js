import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../../../fable_modules/Fable.Promise.3.2.0/Promise.fs.js";
import { promise } from "../../../fable_modules/Fable.Promise.3.2.0/PromiseImpl.fs.js";
import { sum, find, equalsWith, sort, collect, max, initialize, mapIndexed, contains, tryFind, choose, sortBy, tryHead, map } from "../../../fable_modules/fable-library.4.9.0/Array.js";
import { mapIndexed as mapIndexed_1, collect as collect_1, map as map_1, singleton as singleton_1, empty as empty_1, append, delay, toList, toArray } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { createValueMatrix, createMatrixForTables, findIndexNextNotHiddenCol, getActiveAnnotationTableName, getAllTableNames, getCustomXml } from "./HelperFunctions.js";
import { replace, split, toConsole, join, printf, toText } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { defaultArg, some, value as value_10, bind } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { numberHash, compare, safeHash, equals, comparePrimitives } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { toTermSearchable, findSelectedBuildingBlock, Shared_OfficeInteropTypes_BuildingBlock__BuildingBlock_getFromContext_Static_240B550, getBuildingBlocks } from "./Functions/BuildingBlockFunctions.js";
import { SwateColumnHeader__get_getColumnCoreName, SwateColumnHeader__get_tryGetHeaderId, ColumnCoreNames, ColumnCoreNames__get_toString, SwateColumnHeader__get_tryGetTermAccession, SwateColumnHeader__get_tryGetOntologyTerm, SwateColumnHeader__get_getFeaturedColAccession, SwateColumnHeader__get_isFeaturedCol, SwateColumnHeader__get_isUnitCol, SwateColumnHeader__get_isTSRCol, SwateColumnHeader__get_isTANCol, SwateColumnHeader__get_isSingleCol, BuildingBlockType_get_TermColumns, BuildingBlock__get_hasCompleteTSRTAN, BuildingBlock__get_hasUnit, InsertBuildingBlock__get_HasUnit, BuildingBlockType__get_isSingleColumn, InsertBuildingBlock__get_HasValues, SwateColumnHeader__get_isInputCol, BuildingBlockNamePrePrint__get_isInputColumn, BuildingBlockNamePrePrint__get_isOutputColumn, BuildingBlockNamePrePrint__toAnnotationTableHeader, SwateColumnHeader__get_toBuildingBlockNamePrePrint, SwateColumnHeader__get_isTermColumn, SwateColumnHeader__get_isMainColumn, SwateColumnHeader_create_Z721C83C5, SwateColumnHeader__get_isReference, TryFindAnnoTableResult_exactlyOneAnnotationTable_Z35CD86D0, BuildingBlockType, BuildingBlockType__get_toString, SwateColumnHeader__get_isOutputCol } from "../../Shared/OfficeInteropTypes.js";
import { tableName as tableName_2 } from "../JsBindings/HumanReadableIds.js";
import { LogIdentifier, Msg_create } from "./InteropLogging.js";
import { item, length, contains as contains_1, concat, ofArrayWithTail, map as map_2, append as append_1, toArray as toArray_2, isEmpty, cons, empty, tryFind as tryFind_1, ofArray, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Validation_updateSwateValidation, Validation_getSwateValidationForCurrentTable } from "./Functions/CustomXmlFunctions.js";
import { TableValidation_create, TableValidation, ColumnValidation, ColumnValidation_ofBuildingBlock_Z72417F1D } from "./CustomXmlTypes.js";
import { toString as toString_1, now, toUniversalTime } from "../../../fable_modules/fable-library.4.9.0/Date.js";
import { createUnit, extendName, createColumnNames } from "./Functions/Indexing.js";
import { toString } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { TermTypes_TermMinimal_ofTerm_Z5E0A7659, TermTypes_TermMinimal_create, TermTypes_TermMinimal__get_accessionToTAN, TermTypes_TermMinimal__get_accessionToTSR, TermTypes_TermMinimal__get_toNumberFormat } from "../../Shared/TermTypes.js";
import { rangeDouble } from "../../../fable_modules/fable-library.4.9.0/Range.js";
import { intersect, toList as toList_1, difference, toArray as toArray_1, ofArray as ofArray_1 } from "../../../fable_modules/fable-library.4.9.0/Set.js";
import { termAccessionUrlOfAccessionStr } from "../../Shared/URLs.js";
import { ofXmlElement, serializeXml } from "../../../fable_modules/Fable.SimpleXml.3.4.0/Generator.fs.js";

/**
 * This is not used in production and only here for development. Its content is always changing to test functions for new features.
 */
export function exampleExcelFunction1() {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (Promise.resolve("Hello World!")))));
}

/**
 * This is not used in production and only here for development. Its content is always changing to test functions for new features.
 */
export function exampleExcelFunction2() {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
        const workbook = context.workbook.load(["customXmlParts"]);
        const customXmlParts = workbook.customXmlParts.load(["items"]);
        const tables = context.workbook.tables.load(["tables"]);
        tables.load(["name", "worksheet"]);
        return context.sync().then((_arg) => map((x) => [x.name, x.worksheet.name], toArray(tables.items))).then((_arg_1) => (getCustomXml(customXmlParts, context).then((_arg_2) => (Promise.resolve(toText(printf("%A"))(_arg_1))))));
    })));
}

export function swateSync(context) {
    return context.sync().then((_arg) => {
    });
}

/**
 * Will return Some tableName if any annotationTable exists in a worksheet before the active one.
 */
export function getPrevAnnotationTable(context) {
    return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
        context.workbook.load(["tables"]);
        const activeWorksheet = context.workbook.worksheets.getActiveWorksheet().load("position");
        const tables = context.workbook.tables;
        tables.load(["items", "worksheet", "name", "position", "values"]);
        return context.sync().then((e) => {
            let array_1;
            const activeWorksheetPosition = activeWorksheet.position;
            return bind((arg) => arg[1], tryHead(sortBy((tupledArg_1) => (activeWorksheetPosition - tupledArg_1[0]), (array_1 = choose((x) => {
                if (x.name.indexOf("annotationTable") === 0) {
                    return [x.worksheet.position, x.name];
                }
                else {
                    return void 0;
                }
            }, toArray(tables.items)), array_1.filter((tupledArg) => ((activeWorksheetPosition - tupledArg[0]) > 0))), {
                Compare: comparePrimitives,
            })));
        }).then((_arg) => (Promise.resolve(_arg)));
    }));
}

export function getPrevTableOutput(context) {
    return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getPrevAnnotationTable(context).then((_arg) => {
        const prevTableName = _arg;
        return (prevTableName != null) ? (getBuildingBlocks(context, value_10(prevTableName)).then((_arg_1) => {
            const outputCol = tryFind((x) => SwateColumnHeader__get_isOutputCol(x.MainColumn.Header), _arg_1);
            const values = (outputCol != null) ? value_10(outputCol).MainColumn.Cells : [];
            return Promise.resolve(values);
        })) : (Promise.resolve([]));
    }))));
}

function createAnnotationTableAtRange(isDark, tryUseLastOutput, range, context) {
    const findNewTableName = (allTableNames_mut) => {
        findNewTableName:
        while (true) {
            const allTableNames = allTableNames_mut;
            const newTestName = `annotationTable${tableName_2()}`;
            if (allTableNames.some((x) => (x === newTestName))) {
                allTableNames_mut = allTableNames;
                continue findNewTableName;
            }
            else {
                return newTestName;
            }
            break;
        }
    };
    const style = isDark ? "TableStyleMedium15" : "TableStyleMedium7";
    const tableRange = range;
    tableRange.load(["rowIndex", "columnIndex", "rowCount", "address", "isEntireColumn", "worksheet"]);
    const activeSheet = tableRange.worksheet;
    activeSheet.load(["tables"]);
    const activeTables = activeSheet.tables.load("items");
    const r = context.runtime.load("enableEvents");
    return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => ((tryUseLastOutput ? getPrevTableOutput(context) : PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (Promise.resolve(new Array(0)))))).then((_arg) => {
        const prevTableOutput = _arg;
        const useExistingPrevOutput = tryUseLastOutput && !(prevTableOutput.length === 0);
        return getAllTableNames(context).then((_arg_1) => (context.sync().then((_arg_2) => {
            let arg_2, arg_3;
            let annoTables;
            const array_3 = map((x_1) => x_1.name, toArray(activeTables.items));
            annoTables = array_3.filter((x_2) => (x_2.indexOf("annotationTable") === 0));
            const matchValue = annoTables.length | 0;
            if (matchValue > 0) {
                throw new Error("The active worksheet contains more than zero annotationTables. Please move to a new worksheet.");
            }
            else if (matchValue === 0) {
            }
            else {
                throw new Error("The active worksheet contains a negative number of annotation tables. Obviously this cannot happen. Please report this as a bug to the developers.");
            }
            r.enableEvents = false;
            let adaptedRange;
            const rowCount = useExistingPrevOutput ? (prevTableOutput.length + 1) : (tableRange.isEntireColumn ? 21 : ((tableRange.rowCount <= 2) ? 2 : tableRange.rowCount));
            adaptedRange = activeSheet.getRangeByIndexes(tableRange.rowIndex, tableRange.columnIndex, rowCount, 2);
            const annotationTable = activeSheet.tables.add(adaptedRange, true);
            annotationTable.columns.getItemAt(0).name = BuildingBlockType__get_toString(new BuildingBlockType(4, []));
            annotationTable.columns.getItemAt(1).name = BuildingBlockType__get_toString(new BuildingBlockType(5, []));
            if (useExistingPrevOutput) {
                let newColValues;
                const collection = map((cell) => [bind(some, cell.Value)], prevTableOutput);
                newColValues = Array.from(collection);
                const col1 = annotationTable.columns.getItemAt(0);
                const body = col1.getDataBodyRange();
                body.values = newColValues;
            }
            const newName = findNewTableName(_arg_1);
            annotationTable.name = newName;
            annotationTable.style = style;
            activeSheet.getUsedRange().format.autofitColumns();
            activeSheet.getUsedRange().format.autofitRows();
            r.enableEvents = true;
            return [annotationTable, Msg_create(new LogIdentifier(1, []), (arg_2 = tableRange.address, (arg_3 = (tableRange.rowCount - 1), toText(printf("Annotation Table created in [%s] with dimensions 2c x (%.0f + 1h)r."))(arg_2)(arg_3))))];
        }).then((_arg_3) => (Promise.resolve([_arg_3[0], _arg_3[1]])))));
    }))));
}

/**
 * This function is used to create a new annotation table.
 * 'isDark' refers to the current styling of excel (darkmode, or not).
 */
export function createAnnotationTable(isDark, tryUseLastOutput) {
    return Excel.run((context) => {
        const selectedRange = context.workbook.getSelectedRange();
        return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (createAnnotationTableAtRange(isDark, tryUseLastOutput, selectedRange, context).then((_arg) => (Promise.resolve(singleton(_arg[1])))))));
    });
}

/**
 * This function is used before most excel interop messages to get the active annotationTable.
 */
export function tryFindActiveAnnotationTable() {
    return Excel.run((context) => {
        const sheet = context.workbook.worksheets.getActiveWorksheet();
        const t = sheet.load(["tables"]);
        const tableItems = t.tables.load("items");
        return context.sync().then((_arg) => {
            let array_1;
            return TryFindAnnoTableResult_exactlyOneAnnotationTable_Z35CD86D0((array_1 = map((x) => x.name, toArray(tableItems.items)), array_1.filter((x_1) => (x_1.indexOf("annotationTable") === 0))));
        });
    });
}

/**
 * This function is used to hide all reference columns and to fit rows and columns to their values.
 * The main goal is to improve readability of the table with this function.
 */
export function autoFitTable(hideRefCols, context) {
    return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => {
        const sheet = context.workbook.worksheets.getActiveWorksheet();
        const annotationTable_1 = sheet.tables.getItem(_arg);
        const allCols = annotationTable_1.columns.load(["items", "name"]);
        const annoHeaderRange = annotationTable_1.getHeaderRowRange();
        annoHeaderRange.load(["values"]);
        const r = context.runtime.load("enableEvents");
        return context.sync().then((_arg_1) => {
            r.enableEvents = false;
            const updateColumns = map((col) => {
                const r_1 = col.getRange();
                if (SwateColumnHeader__get_isReference(SwateColumnHeader_create_Z721C83C5(col.name)) && hideRefCols) {
                    r_1.columnHidden = true;
                }
                else {
                    r_1.format.autofitColumns();
                    r_1.format.autofitRows();
                }
            }, Array.from(allCols.items));
            r.enableEvents = true;
            return singleton(Msg_create(new LogIdentifier(1, []), "Autoformat Table"));
        }).then((_arg_2) => (Promise.resolve(_arg_2)));
    }))));
}

/**
 * This function is used to hide all reference columns and to fit rows and columns to their values.
 * The main goal is to improve readability of the table with this function.
 */
export function autoFitTableHide(context) {
    return autoFitTable(true, context);
}

export function autoFitTableByTable(annotationTable, context) {
    const allCols = annotationTable.columns.load(["items", "name"]);
    const annoHeaderRange = annotationTable.getHeaderRowRange();
    annoHeaderRange.load(["values"]);
    return context.sync().then((_arg) => {
        const updateColumns = map((col) => {
            const r = col.getRange();
            if (SwateColumnHeader__get_isReference(SwateColumnHeader_create_Z721C83C5(col.name))) {
                r.columnHidden = true;
            }
            else {
                r.format.autofitColumns();
                r.format.autofitRows();
            }
        }, Array.from(allCols.items));
        return singleton(Msg_create(new LogIdentifier(1, []), "Autoformat Table"));
    });
}

/**
 * This is currently used to get information about the table for the table validation feature.
 */
export function getTableRepresentation() {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => {
        const annotationTable = _arg;
        return getBuildingBlocks(context, annotationTable).then((_arg_1) => {
            const buildingBlocks = _arg_1;
            const workbook = context.workbook.load(["customXmlParts"]);
            const customXmlParts = workbook.customXmlParts.load(["items"]);
            return getCustomXml(customXmlParts, context).then((_arg_2) => {
                const currentTableValidation = Validation_getSwateValidationForCurrentTable(annotationTable, _arg_2);
                let updateCurrentTableValidationXml;
                const existingBuildingBlocks = map(ColumnValidation_ofBuildingBlock_Z72417F1D, buildingBlocks);
                if (currentTableValidation != null) {
                    const updatedNewColumnValidations = ofArray(map((newColVal) => {
                        const tryFindCurrentColVal = tryFind_1((x) => equals(x.ColumnHeader, newColVal.ColumnHeader), value_10(currentTableValidation).ColumnValidations);
                        if (tryFindCurrentColVal != null) {
                            return new ColumnValidation(newColVal.ColumnHeader, newColVal.ColumnAdress, value_10(tryFindCurrentColVal).Importance, value_10(tryFindCurrentColVal).ValidationFormat, newColVal.Unit);
                        }
                        else {
                            return newColVal;
                        }
                    }, existingBuildingBlocks));
                    const bind$0040 = value_10(currentTableValidation);
                    updateCurrentTableValidationXml = (new TableValidation(bind$0040.DateTime, bind$0040.SwateVersion, bind$0040.AnnotationTable, bind$0040.Userlist, updatedNewColumnValidations));
                }
                else {
                    updateCurrentTableValidationXml = TableValidation_create("", annotationTable, toUniversalTime(now()), empty(), ofArray(existingBuildingBlocks));
                }
                return Promise.resolve([updateCurrentTableValidationXml, buildingBlocks]);
            });
        });
    })))));
}

export function getBuildingBlocksAndSheet() {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => (getBuildingBlocks(context, _arg).then((_arg_1) => {
        const worksheet = context.workbook.worksheets.getActiveWorksheet();
        worksheet.load("name");
        return context.sync().then((_arg_2) => worksheet.name).then((_arg_3) => (Promise.resolve([_arg_3, _arg_1])));
    })))))));
}

export function getBuildingBlocksAndSheets() {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
        context.workbook.load(["tables"]);
        const tables = context.workbook.tables;
        tables.load(["items", "worksheet", "name", "values"]);
        return context.sync().then((e) => choose((x) => {
            if (x.name.indexOf("annotationTable") === 0) {
                return [x.worksheet.name, x.name];
            }
            else {
                return void 0;
            }
        }, toArray(tables.items))).then((_arg) => {
            let pr_1;
            return ((pr_1 = map((tupledArg) => Excel.run((context_1) => {
                const pr = getBuildingBlocks(context_1, tupledArg[1]);
                return pr.then((res) => [tupledArg[0], res]);
            }), _arg), Promise.all(pr_1))).then((_arg_1) => (Promise.resolve(_arg_1)));
        });
    })));
}

/**
 * Selected ranged returns indices always from a worksheet perspective but we need the related table index. This is calculated here.
 */
export function rebaseIndexToTable(selectedRange, annoHeaderRange) {
    const diff = ~~(selectedRange.columnIndex - annoHeaderRange.columnIndex) | 0;
    const maxLength = (~~annoHeaderRange.columnCount - 1) | 0;
    return (diff < 0) ? maxLength : ((diff > maxLength) ? maxLength : diff);
}

function checkIfBuildingBlockExisting(newBB, existingBuildingBlocks) {
    if (contains(newBB.ColumnHeader, choose((x) => {
        if (SwateColumnHeader__get_isMainColumn(x.MainColumn.Header) && !SwateColumnHeader__get_isTermColumn(x.MainColumn.Header)) {
            return SwateColumnHeader__get_toBuildingBlockNamePrePrint(x.MainColumn.Header);
        }
        else {
            return void 0;
        }
    }, existingBuildingBlocks), {
        Equals: equals,
        GetHashCode: safeHash,
    })) {
        throw new Error(`Swate table contains already building block "${BuildingBlockNamePrePrint__toAnnotationTableHeader(newBB.ColumnHeader)}" in worksheet.`);
    }
}

function checkHasExistingOutput(newBB, existingBuildingBlocks) {
    if (BuildingBlockNamePrePrint__get_isOutputColumn(newBB.ColumnHeader)) {
        return tryFind((x) => {
            if (SwateColumnHeader__get_isMainColumn(x.MainColumn.Header)) {
                return SwateColumnHeader__get_isOutputCol(x.MainColumn.Header);
            }
            else {
                return false;
            }
        }, existingBuildingBlocks);
    }
    else {
        return void 0;
    }
}

function checkHasExistingInput(newBB, existingBuildingBlocks) {
    if (BuildingBlockNamePrePrint__get_isInputColumn(newBB.ColumnHeader)) {
        if (tryFind((x) => {
            if (SwateColumnHeader__get_isMainColumn(x.MainColumn.Header)) {
                return SwateColumnHeader__get_isInputCol(x.MainColumn.Header);
            }
            else {
                return false;
            }
        }, existingBuildingBlocks) != null) {
            throw new Error(`Swate table contains already input building block "${BuildingBlockNamePrePrint__toAnnotationTableHeader(newBB.ColumnHeader)}" in worksheet.`);
        }
    }
}

/**
 * This function is used to add a new building block to the active annotationTable.
 */
export function addAnnotationBlock(newBB) {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => {
        const sheet = context.workbook.worksheets.getActiveWorksheet();
        const annotationTable = sheet.tables.getItem(_arg);
        const annoHeaderRange = annotationTable.getHeaderRowRange();
        annoHeaderRange.load(["values", "columnIndex", "columnCount", "rowIndex"]);
        const tableRange = annotationTable.getRange();
        tableRange.load(["columnCount", "rowCount"]);
        const selectedRange = context.workbook.getSelectedRange();
        selectedRange.load("columnIndex");
        return context.sync().then((e) => {
            const rebasedIndex = rebaseIndexToTable(selectedRange, annoHeaderRange);
            const headerVals = Array.from(annoHeaderRange.values[0]);
            return [findIndexNextNotHiddenCol(headerVals, rebasedIndex), headerVals];
        }).then((_arg_1) => {
            const rowCount = ~~tableRange.rowCount | 0;
            return context.sync().then((_arg_2) => {
                const columnNames = createColumnNames(newBB, map(toString, choose((x) => x, _arg_1[1])));
                let formatChangedMsg = empty();
                const createAllCols = mapIndexed((i, colName) => {
                    let col_1;
                    const index = _arg_1[0] + i;
                    col_1 = annotationTable.columns.add(index, createMatrixForTables(1, rowCount, ""));
                    col_1.name = colName;
                    const columnBody = col_1.getDataBodyRange();
                    columnBody.format.autofitColumns();
                    if ((newBB.UnitTerm != null) && (colName === columnNames[0])) {
                        const format = TermTypes_TermMinimal__get_toNumberFormat(value_10(newBB.UnitTerm));
                        const formats = createValueMatrix(1, rowCount - 1, format);
                        formatChangedMsg = cons(Msg_create(new LogIdentifier(1, []), `Added specified unit: ${format}`), formatChangedMsg);
                        columnBody.numberFormat = formats;
                    }
                    else {
                        const format_1 = createValueMatrix(1, rowCount - 1, "@");
                        columnBody.numberFormat = format_1;
                    }
                    if (colName !== columnNames[0]) {
                        columnBody.columnHidden = true;
                    }
                    return col_1;
                }, columnNames);
                return [columnNames[0], formatChangedMsg];
            }).then((_arg_3) => {
                const formatChangedMsg_1 = _arg_3[1];
                return autoFitTableByTable(annotationTable, context).then((_arg_4) => {
                    const createColsMsg = Msg_create(new LogIdentifier(1, []), `${_arg_3[0]} was added.`);
                    const loggingList = toList(delay(() => append(!isEmpty(formatChangedMsg_1) ? formatChangedMsg_1 : empty_1(), delay(() => singleton_1(createColsMsg)))));
                    return Promise.resolve(loggingList);
                });
            });
        });
    })))));
}

/**
 * If an output column already exists it should be replaced by the new output column type.
 */
export function replaceOutputColumn(annotationTableName, existingOutputColumn, newOutputcolumn) {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
        const sheet = context.workbook.worksheets.getActiveWorksheet();
        const annotationTable = sheet.tables.getItem(annotationTableName);
        const annoHeaderRange = annotationTable.getHeaderRowRange();
        const existingOutputColCell = annoHeaderRange.getCell(0, existingOutputColumn.MainColumn.Index);
        existingOutputColCell.load(["values"]);
        const newHeaderValues = [[some(BuildingBlockNamePrePrint__toAnnotationTableHeader(newOutputcolumn.ColumnHeader))]];
        return context.sync().then((e) => {
            existingOutputColCell.values = newHeaderValues;
        }).then(() => (autoFitTableByTable(annotationTable, context).then((_arg_1) => {
            const msg = Msg_create(new LogIdentifier(2, []), `Found existing output column "${existingOutputColumn.MainColumn.Header.SwateColumnHeader}". Changed output column to "${BuildingBlockNamePrePrint__toAnnotationTableHeader(newOutputcolumn.ColumnHeader)}".`);
            return Promise.resolve(singleton(msg));
        })));
    })));
}

/**
 * Handle any diverging functionality here. This function is also used to make sure any new building blocks comply to the swate annotation-table definition.
 */
export function addAnnotationBlockHandler(newBB) {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => {
        const annotationTableName = _arg;
        const sheet = context.workbook.worksheets.getActiveWorksheet();
        const annotationTable = sheet.tables.getItem(annotationTableName);
        return Shared_OfficeInteropTypes_BuildingBlock__BuildingBlock_getFromContext_Static_240B550(context, annotationTable).then((_arg_1) => {
            const existingBuildingBlocks = _arg_1;
            checkHasExistingInput(newBB, existingBuildingBlocks);
            checkIfBuildingBlockExisting(newBB, existingBuildingBlocks);
            const outputColOpt = checkHasExistingOutput(newBB, existingBuildingBlocks);
            return ((outputColOpt == null) ? addAnnotationBlock(newBB) : replaceOutputColumn(annotationTableName, outputColOpt, newBB)).then((_arg_2) => (Promise.resolve(_arg_2)));
        });
    })))));
}

function createColumnBodyValues(insertBB, tableRowCount) {
    const createList = (rowCount, values) => Array.from(toArray(delay(() => map_1((i) => Array.from(toArray(delay(() => ((i <= (rowCount - 1)) ? singleton_1(some(values[i])) : singleton_1(void 0))))), rangeDouble(0, 1, tableRowCount - 2)))));
    const matchValue = InsertBuildingBlock__get_HasValues(insertBB);
    if (matchValue) {
        const rowCount_1 = insertBB.Rows.length | 0;
        if (BuildingBlockType__get_isSingleColumn(insertBB.ColumnHeader.Type)) {
            return [createList(rowCount_1, map((tm) => tm.Name, insertBB.Rows))];
        }
        else if (InsertBuildingBlock__get_HasUnit(insertBB)) {
            const unitTermRowArr = initialize(rowCount_1, (_arg) => value_10(insertBB.UnitTerm));
            return [createList(rowCount_1, map((tm_1) => tm_1.Name, insertBB.Rows)), createList(rowCount_1, map((tm_2) => tm_2.Name, unitTermRowArr)), createList(rowCount_1, map(TermTypes_TermMinimal__get_accessionToTSR, unitTermRowArr)), createList(rowCount_1, map(TermTypes_TermMinimal__get_accessionToTAN, unitTermRowArr))];
        }
        else {
            return [createList(rowCount_1, map((tm_5) => tm_5.Name, insertBB.Rows)), createList(rowCount_1, map(TermTypes_TermMinimal__get_accessionToTSR, insertBB.Rows)), createList(rowCount_1, map(TermTypes_TermMinimal__get_accessionToTAN, insertBB.Rows))];
        }
    }
    else {
        return [];
    }
}

export function addAnnotationBlocksToTable(buildingBlocks, table, context) {
    return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
        const annotationTable = table;
        annotationTable.load("name");
        return Shared_OfficeInteropTypes_BuildingBlock__BuildingBlock_getFromContext_Static_240B550(context, annotationTable).then((_arg) => {
            let patternInput;
            const newSet = ofArray_1(map((x) => x.ColumnHeader, buildingBlocks), {
                Compare: compare,
            });
            const prevSet = ofArray_1(choose((x_2) => SwateColumnHeader__get_toBuildingBlockNamePrePrint(x_2.MainColumn.Header), _arg), {
                Compare: compare,
            });
            const bbsToAdd = toArray_1(difference(newSet, prevSet));
            patternInput = [buildingBlocks.filter((buildingblock) => {
                if (contains(buildingblock.ColumnHeader, bbsToAdd, {
                    Equals: equals,
                    GetHashCode: safeHash,
                })) {
                    return !(BuildingBlockNamePrePrint__get_isOutputColumn(buildingblock.ColumnHeader) && BuildingBlockNamePrePrint__get_isInputColumn(buildingblock.ColumnHeader));
                }
                else {
                    return false;
                }
            }), toList_1(intersect(newSet, prevSet))];
            const newBuildingBlocks_1 = patternInput[0];
            const alreadyExistingBBs = patternInput[1];
            const annoHeaderRange = annotationTable.getHeaderRowRange();
            annoHeaderRange.load(["values", "columnIndex", "columnCount", "rowIndex"]);
            const tableRange = annotationTable.getRange();
            tableRange.load(["columnCount", "rowCount"]);
            const selectedRange = context.workbook.getSelectedRange();
            selectedRange.load("columnIndex");
            return context.sync().then((e) => {
                const rebasedIndex = rebaseIndexToTable(selectedRange, annoHeaderRange);
                const headerVals = Array.from(annoHeaderRange.values[0]);
                return [findIndexNextNotHiddenCol(headerVals, rebasedIndex), headerVals];
            }).then((_arg_1) => {
                const startColumnCount = ~~tableRange.columnCount | 0;
                let expandByNRows;
                const nOfMissingRows = (((newBuildingBlocks_1.length === 0) ? 0 : max(map((x_5) => x_5.Rows.length, newBuildingBlocks_1, Int32Array), {
                    Compare: comparePrimitives,
                })) - (~~tableRange.rowCount - 1)) | 0;
                expandByNRows = ((nOfMissingRows > 0) ? nOfMissingRows : void 0);
                return ((expandByNRows != null) ? PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (context.sync().then((e_1) => {
                    const newRowsValues = createMatrixForTables(startColumnCount, value_10(expandByNRows), "");
                    const newRows = annotationTable.rows.add(void 0, newRowsValues);
                    const newTable = context.workbook.tables.getItem(annotationTable.name);
                    const newTableRange = annotationTable.getRange();
                    newTableRange.load(["columnCount", "rowCount"]);
                    return [annotationTable, newTableRange];
                }).then((_arg_2) => (context.sync().then((_arg_3) => ~~_arg_2[1].rowCount).then((_arg_4) => (Promise.resolve([_arg_2[0], _arg_4])))))))) : PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (Promise.resolve([annotationTable, ~~tableRange.rowCount]))))).then((_arg_5) => {
                    const expandedTable_1 = _arg_5[0];
                    const expandedRowCount_1 = _arg_5[1] | 0;
                    let nextIndex_1 = _arg_1[0];
                    let allColumnHeaders = ofArray(map(toString, choose((x_7) => x_7, _arg_1[1])));
                    return context.sync().then((_arg_6) => collect((buildingBlock_1) => {
                        const colHeadersArr = toArray_2(allColumnHeaders);
                        const buildingBlock = buildingBlock_1;
                        const currentNextIndex = nextIndex_1;
                        const columnNames = createColumnNames(buildingBlock, colHeadersArr);
                        nextIndex_1 = (currentNextIndex + columnNames.length);
                        allColumnHeaders = append_1(ofArray(columnNames), allColumnHeaders);
                        const createAllCols = mapIndexed((i, colName) => {
                            let col_1;
                            const index = currentNextIndex + i;
                            col_1 = expandedTable_1.columns.add(index, createMatrixForTables(1, expandedRowCount_1, ""));
                            col_1.name = colName;
                            const columnBody = col_1.getDataBodyRange();
                            columnBody.format.autofitColumns();
                            if ((buildingBlock.UnitTerm != null) && (colName === columnNames[0])) {
                                const formats = createValueMatrix(1, expandedRowCount_1 - 1, TermTypes_TermMinimal__get_toNumberFormat(value_10(buildingBlock.UnitTerm)));
                                columnBody.numberFormat = formats;
                            }
                            else {
                                const formats_1 = createValueMatrix(1, expandedRowCount_1 - 1, "General");
                                columnBody.numberFormat = formats_1;
                            }
                            if (InsertBuildingBlock__get_HasValues(buildingBlock)) {
                                const values = createColumnBodyValues(buildingBlock, expandedRowCount_1);
                                columnBody.values = values[i];
                            }
                            if (colName !== columnNames[0]) {
                                columnBody.columnHidden = true;
                            }
                            return col_1;
                        }, columnNames);
                        return columnNames;
                    }, newBuildingBlocks_1)).then((_arg_7) => (autoFitTableByTable(expandedTable_1, context).then((_arg_8) => {
                        const createColsMsg = Msg_create(new LogIdentifier(1, []), isEmpty(alreadyExistingBBs) ? "Added protocol building blocks successfully." : (`Insert completed successfully, but Swate found already existing building blocks in table. Building blocks must be unique. Skipped the following "${join(", ", map_2(BuildingBlockNamePrePrint__toAnnotationTableHeader, alreadyExistingBBs))}".`));
                        const logging = toList(delay(() => append(_arg_8, delay(() => singleton_1(createColsMsg)))));
                        return Promise.resolve(logging);
                    })));
                });
            });
        });
    }));
}

export function addAnnotationBlocks(buildingBlocks) {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (tryFindActiveAnnotationTable().then((_arg) => {
        let arg;
        const tryTable = _arg;
        const sheet = context.workbook.worksheets.getActiveWorksheet();
        return ((tryTable.tag === 1) ? createAnnotationTableAtRange(false, false, (() => {
            try {
                return context.workbook.getSelectedRange();
            }
            catch (e_1) {
                return sheet.getUsedRange();
            }
        })(), context) : ((arg = [sheet.tables.getItem(tryTable.fields[0]), Msg_create(new LogIdentifier(1, []), "Found annotation table for template insert!")], Promise.resolve(arg)))).then((_arg_1) => (addAnnotationBlocksToTable(buildingBlocks, _arg_1[0], context).then((_arg_2) => (Promise.resolve(cons(_arg_1[1], _arg_2))))));
    })))));
}

export function addAnnotationBlocksInNewSheet(activateWorksheet, worksheetName, buildingBlocks) {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
        const newWorksheet = context.workbook.worksheets.add(worksheetName);
        const worksheetRange = newWorksheet.getUsedRange();
        return createAnnotationTableAtRange(false, false, worksheetRange, context).then((_arg) => (addAnnotationBlocksToTable(buildingBlocks, _arg[0], context).then((_arg_1) => ((activateWorksheet ? ((newWorksheet.activate(), Promise.resolve())) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1(promise, () => {
            const newSheetLogging = Msg_create(new LogIdentifier(1, []), `Create new worksheet: ${worksheetName}`);
            return Promise.resolve(ofArrayWithTail([newSheetLogging, _arg[1]], _arg_1));
        }))))));
    })));
}

export function addAnnotationBlocksInNewSheets(annotationTablesToAdd) {
    let pr_1;
    const pr = mapIndexed((i, x) => addAnnotationBlocksInNewSheet(i === (annotationTablesToAdd.length - 1), x[0], x[1]), annotationTablesToAdd);
    pr_1 = (Promise.all(pr));
    return pr_1.then(concat);
}

/**
 * This function is used to add unit reference columns to an existing building block without unit reference columns
 */
export function updateUnitForCells(unitTerm) {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => {
        const annotationTableName = _arg;
        const sheet = context.workbook.worksheets.getActiveWorksheet();
        const annotationTable = sheet.tables.getItem(annotationTableName);
        annotationTable.columns.load(["items"]);
        const selectedRange = context.workbook.getSelectedRange();
        selectedRange.load(["values", "rowIndex", "rowCount"]);
        const annoHeaderRange = annotationTable.getHeaderRowRange();
        annoHeaderRange.load(["values"]);
        const tableRange = annotationTable.getRange();
        tableRange.load(["rowCount"]);
        return findSelectedBuildingBlock(context, annotationTableName).then((_arg_1) => {
            const selectedBuildingBlock = _arg_1;
            return context.sync().then((e) => Array.from(annoHeaderRange.values[0])).then((_arg_2) => ((BuildingBlock__get_hasUnit(selectedBuildingBlock) ? context.sync().then((_arg_3) => {
                const format = TermTypes_TermMinimal__get_toNumberFormat(unitTerm);
                const formats = createValueMatrix(1, ~~selectedRange.rowCount, format);
                selectedRange.numberFormat = formats;
                return Msg_create(new LogIdentifier(1, []), `Updated specified cells with unit: ${format}.`);
            }) : ((BuildingBlock__get_hasCompleteTSRTAN(selectedBuildingBlock) && !BuildingBlock__get_hasUnit(selectedBuildingBlock)) ? context.sync().then((_arg_4) => {
                const unitColName = extendName(map(toString, choose((x) => x, _arg_2)), createUnit());
                const unitColumn = annotationTable.columns.add(selectedBuildingBlock.MainColumn.Index + 1);
                unitColumn.name = unitColName;
                const mainCol = annotationTable.columns.items[selectedBuildingBlock.MainColumn.Index].getDataBodyRange();
                const format_1 = TermTypes_TermMinimal__get_toNumberFormat(unitTerm);
                const formats_1 = createValueMatrix(1, ~~tableRange.rowCount - 1, format_1);
                mainCol.numberFormat = formats_1;
                return Msg_create(new LogIdentifier(1, []), `Created Unit Column ${unitColName} for building block ${selectedBuildingBlock.MainColumn.Header.SwateColumnHeader}.`);
            }) : (() => {
                throw new Error(`You can only add unit to building blocks of the type: ${BuildingBlockType_get_TermColumns()}`);
            })())).then((_arg_5) => (autoFitTable(true, context).then((_arg_6) => (Promise.resolve(singleton(_arg_5))))))));
        });
    })))));
}

/**
 * This function removes a given building block from a given annotation table.
 * It returns the affected column indices.
 */
export function removeAnnotationBlock(tableName, annotationBlock, context) {
    return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
        const sheet = context.workbook.worksheets.getActiveWorksheet();
        const table = sheet.tables.getItem(tableName);
        table.load("columns");
        const tableCols = table.columns.load("items");
        let targetedColIndices;
        const refColIndices = BuildingBlock__get_hasUnit(annotationBlock) ? (new Int32Array([value_10(annotationBlock.Unit).Index, value_10(annotationBlock.TAN).Index, value_10(annotationBlock.TSR).Index])) : (BuildingBlock__get_hasCompleteTSRTAN(annotationBlock) ? (new Int32Array([value_10(annotationBlock.TAN).Index, value_10(annotationBlock.TSR).Index])) : (new Int32Array([])));
        targetedColIndices = sort(toArray(delay(() => append(singleton_1(annotationBlock.MainColumn.Index), delay(() => refColIndices)))), {
            Compare: comparePrimitives,
        });
        return context.sync().then((e) => map((targetIndex) => {
            tableCols.items[targetIndex].delete();
        }, targetedColIndices)).then((_arg) => (Promise.resolve(targetedColIndices)));
    }));
}

export function removeSelectedAnnotationBlock() {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => {
        const annotationTable = _arg;
        return findSelectedBuildingBlock(context, annotationTable).then((_arg_1) => {
            const selectedBuildingBlock = _arg_1;
            return removeAnnotationBlock(annotationTable, selectedBuildingBlock, context).then((_arg_2) => {
                const resultMsg = Msg_create(new LogIdentifier(1, []), `Delete Building Block ${selectedBuildingBlock.MainColumn.Header.SwateColumnHeader} (Cols: ${_arg_2})`);
                return autoFitTableHide(context).then((_arg_3) => (Promise.resolve(singleton(resultMsg))));
            });
        });
    })))));
}

export function getAnnotationBlockDetails() {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => (findSelectedBuildingBlock(context, _arg).then((_arg_1) => {
        const searchTerms = toTermSearchable(_arg_1);
        return Promise.resolve(searchTerms);
    })))))));
}

export function checkForDeprecation(buildingBlocks) {
    let buildingBlocks_1, hasDataFileNameCol, message;
    let msgList = empty();
    (buildingBlocks_1 = buildingBlocks, (hasDataFileNameCol = tryFind((x) => (x.MainColumn.Header.SwateColumnHeader === BuildingBlockType__get_toString(new BuildingBlockType(6, []))), buildingBlocks_1), (hasDataFileNameCol == null) ? buildingBlocks_1 : ((message = Msg_create(new LogIdentifier(2, []), `Found deprecated output column "${BuildingBlockType__get_toString(new BuildingBlockType(6, []))}". Obsolete since v0.6.0.
                It is recommend to replace "${BuildingBlockType__get_toString(new BuildingBlockType(6, []))}" with "${BuildingBlockType__get_toString(new BuildingBlockType(7, []))}"
                or "${BuildingBlockType__get_toString(new BuildingBlockType(8, []))}".`), (msgList = cons(message, msgList), buildingBlocks_1)))));
    return msgList;
}

export function getAllAnnotationBlockDetails() {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => (getBuildingBlocks(context, _arg).then((_arg_1) => {
        const buildingBlocks = _arg_1;
        const deprecationMsgs = checkForDeprecation(buildingBlocks);
        const searchTerms = collect(toTermSearchable, buildingBlocks);
        return Promise.resolve([searchTerms, deprecationMsgs]);
    })))))));
}

/**
 * This function is used to parse a selected header to a TermMinimal type, used for relationship directed term search.
 */
export function getTermFromHeaderValues(headerValues, selectedHeaderColIndex) {
    let headerIndexPlus1, headerIndexPlus2;
    const header = SwateColumnHeader_create_Z721C83C5(toString(value_10(headerValues[selectedHeaderColIndex])));
    if (((SwateColumnHeader__get_isSingleCol(header) ? true : SwateColumnHeader__get_isTANCol(header)) ? true : SwateColumnHeader__get_isTSRCol(header)) ? true : SwateColumnHeader__get_isUnitCol(header)) {
        return void 0;
    }
    else if (SwateColumnHeader__get_isFeaturedCol(header)) {
        return TermTypes_TermMinimal_create(header.SwateColumnHeader, SwateColumnHeader__get_getFeaturedColAccession(header));
    }
    else if (SwateColumnHeader__get_tryGetOntologyTerm(header) != null) {
        return TermTypes_TermMinimal_create(value_10(SwateColumnHeader__get_tryGetOntologyTerm(header)), defaultArg((headerIndexPlus1 = SwateColumnHeader_create_Z721C83C5(toString(defaultArg(headerValues[selectedHeaderColIndex + 1], ""))), (headerIndexPlus2 = SwateColumnHeader_create_Z721C83C5(toString(defaultArg(headerValues[selectedHeaderColIndex + 2], ""))), (!SwateColumnHeader__get_isUnitCol(headerIndexPlus1) && SwateColumnHeader__get_isTSRCol(headerIndexPlus1)) ? SwateColumnHeader__get_tryGetTermAccession(headerIndexPlus1) : ((SwateColumnHeader__get_isUnitCol(headerIndexPlus1) && SwateColumnHeader__get_isTSRCol(headerIndexPlus2)) ? SwateColumnHeader__get_tryGetTermAccession(headerIndexPlus2) : void 0))), ""));
    }
    else {
        return void 0;
    }
}

/**
 * This function will parse the header of a selected column to check for a parent ontology, which will then be used for a isA-directed term search.
 * Any found parent ontology will also be displayed in a static field before the term search input field.
 */
export function getParentTerm() {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => {
        const sheet = context.workbook.worksheets.getActiveWorksheet();
        const annotationTable_1 = sheet.tables.getItem(_arg);
        const tableRange = annotationTable_1.getRange();
        tableRange.load(["columnIndex", "rowIndex", "values"]);
        const range = context.workbook.getSelectedRange();
        range.load(["columnIndex", "rowIndex"]);
        return context.sync().then((_arg_1) => {
            let newColIndex;
            const tableRangeColIndex = tableRange.columnIndex;
            newColIndex = ~~(range.columnIndex - tableRangeColIndex);
            let newRowIndex;
            const tableRangeRowIndex = tableRange.rowIndex;
            newRowIndex = ~~(range.rowIndex - tableRangeRowIndex);
            const colHeaderVals = tableRange.values[0];
            const rowVals = tableRange.values;
            return ((((newColIndex < 0) ? true : (newColIndex > (colHeaderVals.length - 1))) ? true : (newRowIndex < 0)) ? true : (newRowIndex > (rowVals.length - 1))) ? void 0 : getTermFromHeaderValues(colHeaderVals, newColIndex);
        }).then((_arg_2) => (Promise.resolve(_arg_2)));
    }))).catch((_arg_3) => (Promise.resolve(void 0)))))));
}

/**
 * This is used to insert terms into selected cells.
 * 'term' is the value that will be written into the main column.
 * 'termBackground' needs to be spearate from 'term' in case the user uses the fill function for a custom term.
 * Should the user write a real term with this function 'termBackground'.isSome and can be used to fill TSR and TAN.
 */
export function insertOntologyTerm(term) {
    return Excel.run((context) => {
        const range = context.workbook.getSelectedRange();
        range.load(["values", "columnIndex", "rowIndex", "columnCount", "rowCount"]);
        const nextColsRange = range.getColumnsAfter(2);
        nextColsRange.load(["values", "columnIndex", "columnCount"]);
        const r = context.runtime.load("enableEvents");
        return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (tryFindActiveAnnotationTable().then((_arg) => {
            const tryTable = _arg;
            return ((tryTable.tag === 1) ? Promise.resolve(void 0) : PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
                const sheet = context.workbook.worksheets.getActiveWorksheet();
                const table_1 = sheet.tables.getItem(tryTable.fields[0]);
                const tableRange = table_1.getRange();
                tableRange.load(["rowIndex", "rowCount", "columnIndex", "columnCount"]);
                return context.sync().then((_arg_1) => [range.rowIndex, range.rowIndex + range.rowCount, range.columnIndex, range.columnIndex + 2]).then((_arg_2) => {
                    let source;
                    const lastInputRow = _arg_2[1];
                    const lastInputColumn = _arg_2[3];
                    const inputRow = _arg_2[0];
                    const inputColumn = _arg_2[2];
                    const lastColumnIndex = tableRange.columnIndex + tableRange.columnCount;
                    const lastRowIndex = tableRange.rowIndex + tableRange.rowCount;
                    const isInBodyRows = ((inputRow >= tableRange.rowIndex) ? true : (lastInputRow >= tableRange.rowIndex)) && ((inputRow <= lastRowIndex) ? true : (lastInputRow <= lastRowIndex));
                    const isInBodyColumns = ((inputColumn >= tableRange.columnIndex) ? true : (lastInputColumn >= tableRange.columnIndex)) && ((inputColumn <= lastColumnIndex) ? true : (lastInputColumn <= lastColumnIndex));
                    return (((source = toList(rangeDouble(inputRow, 1, lastInputRow)), contains_1(tableRange.rowIndex, source, {
                        Equals: (x, y) => (x === y),
                        GetHashCode: numberHash,
                    }))) ? (isInBodyColumns ? (((() => {
                        throw new Error("Cannot insert ontology term into annotation table header row. If you want to create new building blocks, please use the Add Building Block function.");
                    })(), Promise.resolve())) : (Promise.resolve())) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1(promise, () => (((inputRow === lastRowIndex) ? (isInBodyColumns ? (((() => {
                        throw new Error("Cannot insert ontology term directly underneath an annotation table!");
                    })(), Promise.resolve())) : (Promise.resolve())) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1(promise, () => (((((inputColumn === lastColumnIndex) ? true : (inputColumn === (tableRange.columnIndex - 2))) ? true : (inputColumn === (tableRange.columnIndex - 1))) ? (isInBodyRows ? (((() => {
                        throw new Error("Cannot insert ontology term directly next to an annotation table!");
                    })(), Promise.resolve())) : (Promise.resolve())) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1(promise, () => (Shared_OfficeInteropTypes_BuildingBlock__BuildingBlock_getFromContext_Static_240B550(context, table_1).then((_arg_3) => {
                        const mainColumnIndices = map((x_2) => x_2.MainColumn.Index, _arg_3.filter((x_1) => {
                            if (!SwateColumnHeader__get_isOutputCol(x_1.MainColumn.Header)) {
                                return !SwateColumnHeader__get_isInputCol(x_1.MainColumn.Header);
                            }
                            else {
                                return false;
                            }
                        }), Int32Array);
                        const isInsideTable = isInBodyRows && isInBodyColumns;
                        return (isInsideTable ? ((toConsole(printf("mainColumnIndices: %A"))(mainColumnIndices), (contains(~~(inputColumn - tableRange.columnIndex), mainColumnIndices, {
                            Equals: (x_3, y_1) => (x_3 === y_1),
                            GetHashCode: numberHash,
                        }) === false) ? (((() => {
                            throw new Error("Cannot insert ontology term to input/output/reference columns of an annotation table!");
                        })(), Promise.resolve())) : (Promise.resolve()))) : (Promise.resolve())).then(() => PromiseBuilder__Delay_62FBFDE1(promise, () => (Promise.resolve(void 0))));
                    }))))))))));
                });
            }))).then(() => (context.sync().then((_arg_5) => {
                let arg_2;
                if (range.columnCount > 1) {
                    throw new Error("Cannot insert Terms in more than one column at a time.");
                }
                r.enableEvents = false;
                const newVals = Array.from(toList(delay(() => collect_1((arr) => {
                    const tmp = map_1((_arg_6) => some(term.Name), arr);
                    return singleton_1(Array.from(tmp));
                }, range.values))));
                const nextNewVals = Array.from(toList(delay(() => collect_1((ind) => {
                    const tmp_1 = mapIndexed_1((i, _arg_7) => {
                        const matchValue = term.TermAccession === "";
                        let matchResult;
                        switch (i) {
                            case 0: {
                                if (matchValue) {
                                    matchResult = 0;
                                }
                                else {
                                    matchResult = 1;
                                }
                                break;
                            }
                            case 1: {
                                if (matchValue) {
                                    matchResult = 0;
                                }
                                else {
                                    matchResult = 2;
                                }
                                break;
                            }
                            default:
                                matchResult = 3;
                        }
                        switch (matchResult) {
                            case 0:
                                return some("user-specific");
                            case 1:
                                return some(TermTypes_TermMinimal__get_accessionToTSR(term));
                            case 2:
                                return some(TermTypes_TermMinimal__get_accessionToTAN(term));
                            default: {
                                r.enableEvents = true;
                                throw new Error("The insert should never add more than two extra columns.");
                            }
                        }
                    }, nextColsRange.values[ind]);
                    return singleton_1(Array.from(tmp_1));
                }, rangeDouble(0, 1, nextColsRange.values.length - 1)))));
                range.values = newVals;
                nextColsRange.values = nextNewVals;
                r.enableEvents = true;
                return ["Info", (arg_2 = (nextColsRange.values.length | 0), toText(printf("Insert %A %Ax"))(term)(arg_2))];
            }).then((_arg_8) => (((tryTable.tag === 1) ? Promise.resolve(empty()) : autoFitTableHide(context)).then((_arg_9) => (Promise.resolve(_arg_8)))))));
        }))));
    });
}

/**
 * This function will be executed after the SearchTerm types from 'createSearchTermsFromTable' where send to the server to search the database for them.
 * Here the results will be written into the table by the stored col and row indices.
 */
export function UpdateTableByTermsSearchable(terms) {
    return Excel.run((context) => {
        const createCellValueInput = (str) => [[some(str)]];
        const createMainColName = (coreName, searchResultName, columnHeaderId) => (`${coreName} [${searchResultName}${columnHeaderId}]`);
        const createTSRColName = (searchResultTermAccession, columnHeaderId_1) => (`${ColumnCoreNames__get_toString(new ColumnCoreNames(0, []))} (${searchResultTermAccession}${columnHeaderId_1})`);
        const createTANColName = (searchResultTermAccession_1, columnHeaderId_2) => (`${ColumnCoreNames__get_toString(new ColumnCoreNames(1, []))} (${searchResultTermAccession_1}${columnHeaderId_2})`);
        return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => {
            const annotationTableName = _arg;
            const sheet = context.workbook.worksheets.getActiveWorksheet();
            const annotationTable = sheet.tables.getItem(annotationTableName);
            const tableBodyRange = annotationTable.getDataBodyRange();
            tableBodyRange.load(["values"]);
            const tableHeaderRange = annotationTable.getHeaderRowRange();
            tableHeaderRange.load(["values"]);
            const r = context.runtime.load("enableEvents");
            const bodyTerms = terms.filter((x) => !equalsWith((x_1, y) => (x_1 === y), x.RowIndices, new Int32Array([0])));
            const headerTerms = terms.filter((x_2) => equalsWith((x_3, y_1) => (x_3 === y_1), x_2.RowIndices, new Int32Array([0])));
            return getBuildingBlocks(context, annotationTableName).then((_arg_1) => (context.sync().then((_arg_2) => {
                r.enableEvents = false;
                const numberOfUpdatedHeaderTerms = map((headerTerm) => {
                    const relBuildingBlock = find((bb) => (bb.MainColumn.Index === headerTerm.ColIndex), _arg_1);
                    let columnHeaderId_3;
                    const opt = SwateColumnHeader__get_tryGetHeaderId(relBuildingBlock.MainColumn.Header);
                    columnHeaderId_3 = ((opt == null) ? "" : opt);
                    let columnCoreName;
                    const opt_1 = SwateColumnHeader__get_getColumnCoreName(relBuildingBlock.MainColumn.Header);
                    if (opt_1 == null) {
                        throw new Error(`Could not get Swate compatible column name from ${relBuildingBlock.MainColumn.Header.SwateColumnHeader}.`);
                    }
                    else {
                        columnCoreName = opt_1;
                    }
                    if (headerTerm.SearchResultTerm != null) {
                        const hasSearchResult_1 = headerTerm;
                        const searchResult = TermTypes_TermMinimal_ofTerm_Z5E0A7659(value_10(hasSearchResult_1.SearchResultTerm));
                        const mainColName = createMainColName(columnCoreName, searchResult.Name, columnHeaderId_3);
                        const tsrColName = createTSRColName(searchResult.TermAccession, columnHeaderId_3);
                        const tanColName = createTANColName(searchResult.TermAccession, columnHeaderId_3);
                        const baseShift = BuildingBlock__get_hasUnit(relBuildingBlock) ? (headerTerm.ColIndex + 1) : (headerTerm.ColIndex + 0);
                        if ((relBuildingBlock.MainColumnTerm != null) && equals(value_10(relBuildingBlock.MainColumnTerm), searchResult)) {
                            return 0;
                        }
                        else if ((relBuildingBlock.MainColumnTerm != null) && !equals(value_10(relBuildingBlock.MainColumnTerm), searchResult)) {
                            const mainColHeader = tableHeaderRange.getCell(0, headerTerm.ColIndex);
                            const tsrColHeader = tableHeaderRange.getCell(0, baseShift + 1);
                            const tanColHeader = tableHeaderRange.getCell(0, baseShift + 2);
                            mainColHeader.values = createCellValueInput(mainColName);
                            tsrColHeader.values = createCellValueInput(tsrColName);
                            tanColHeader.values = createCellValueInput(tanColName);
                            return 1;
                        }
                        else {
                            throw new Error(`Swate enocuntered an unknown term pattern. Found term ${hasSearchResult_1.Term} for building block ${relBuildingBlock.MainColumn.Header.SwateColumnHeader}`);
                        }
                    }
                    else if (headerTerm.SearchResultTerm == null) {
                        const hasNoSearchResult_1 = headerTerm;
                        if ((((relBuildingBlock.MainColumnTerm != null) && (value_10(relBuildingBlock.MainColumnTerm).Name === hasNoSearchResult_1.Term.Name)) && (value_10(relBuildingBlock.MainColumnTerm).Name !== "")) && (value_10(relBuildingBlock.MainColumnTerm).TermAccession === "")) {
                            return 0;
                        }
                        else if (((relBuildingBlock.MainColumnTerm != null) && (value_10(relBuildingBlock.MainColumnTerm).Name === hasNoSearchResult_1.Term.Name)) && (value_10(relBuildingBlock.MainColumnTerm).Name !== "")) {
                            const mainColName_1 = createMainColName(columnCoreName, hasNoSearchResult_1.Term.Name, columnHeaderId_3);
                            const tsrColName_1 = createTSRColName("", columnHeaderId_3);
                            const tanColName_1 = createTANColName("", columnHeaderId_3);
                            const baseShift_1 = BuildingBlock__get_hasUnit(relBuildingBlock) ? (headerTerm.ColIndex + 1) : (headerTerm.ColIndex + 0);
                            const mainColHeader_1 = tableHeaderRange.getCell(0, headerTerm.ColIndex);
                            const tsrColHeader_1 = tableHeaderRange.getCell(0, baseShift_1 + 1);
                            const tanColHeader_1 = tableHeaderRange.getCell(0, baseShift_1 + 2);
                            mainColHeader_1.values = createCellValueInput(mainColName_1);
                            tsrColHeader_1.values = createCellValueInput(tsrColName_1);
                            tanColHeader_1.values = createCellValueInput(tanColName_1);
                            return 1;
                        }
                        else {
                            throw new Error(`Swate enocuntered an unknown term pattern. Found no term in database for building block ${relBuildingBlock.MainColumn.Header.SwateColumnHeader}`);
                        }
                    }
                    else {
                        throw new Error(`Swate encountered an unknown term pattern. Search result: ${headerTerm} for buildingBlock ${relBuildingBlock.MainColumn.Header.SwateColumnHeader}`);
                    }
                }, headerTerms, Int32Array);
                const numberOfUpdatedBodyTerms = map((term) => {
                    let patternInput_1;
                    if (term.SearchResultTerm != null) {
                        const t_1 = value_10(term.SearchResultTerm);
                        let patternInput;
                        const splitAccession = split(t_1.Accession, [":"], void 0, 0);
                        const tan = termAccessionUrlOfAccessionStr(t_1.Accession);
                        patternInput = [splitAccession[0], tan];
                        patternInput_1 = [t_1.Name, patternInput[0], patternInput[1]];
                    }
                    else if (term.Term.Name === "") {
                        patternInput_1 = ["", "", ""];
                    }
                    else if (equals(term.SearchResultTerm, void 0)) {
                        patternInput_1 = [term.Term.Name, "user-specific", "user-specific"];
                    }
                    else {
                        throw new Error(`Swate could not parse database search results for term: ${term.Term.Name}.`);
                    }
                    const arr = term.RowIndices;
                    for (let idx = 0; idx <= (arr.length - 1); idx++) {
                        const termNameIndex = term.IsUnit ? (term.ColIndex + 1) : term.ColIndex;
                        const tableBodyRowIndex = arr[idx] - 1;
                        const mainColumnCell = tableBodyRange.getCell(tableBodyRowIndex, termNameIndex);
                        const tsrColumnCell = tableBodyRange.getCell(tableBodyRowIndex, termNameIndex + 1);
                        const tanColumnCell = tableBodyRange.getCell(tableBodyRowIndex, termNameIndex + 2);
                        mainColumnCell.values = createCellValueInput(patternInput_1[0]);
                        tsrColumnCell.values = createCellValueInput(patternInput_1[1]);
                        tanColumnCell.values = createCellValueInput(patternInput_1[2]);
                    }
                    return 1;
                }, bodyTerms, Int32Array);
                r.enableEvents = true;
                return Msg_create(new LogIdentifier(1, []), `Updated ${sum(numberOfUpdatedBodyTerms, {
                    GetZero: () => 0,
                    Add: (x_4, y_2) => (x_4 + y_2),
                })} terms in table body. Updated ${sum(numberOfUpdatedHeaderTerms, {
                    GetZero: () => 0,
                    Add: (x_5, y_3) => (x_5 + y_3),
                })} terms in table column headers.`);
            }).then((_arg_3) => (Promise.resolve(singleton(_arg_3))))));
        }))));
    });
}

/**
 * This function is used to insert file names into the selected range.
 */
export function insertFileNamesFromFilePicker(fileNameList) {
    return Excel.run((context) => {
        const range = context.workbook.getSelectedRange();
        range.load(["values", "columnIndex", "columnCount"]);
        const r = context.runtime.load("enableEvents");
        return context.sync().then((_arg) => {
            let arg;
            if (range.columnCount > 1) {
                throw new Error("Cannot insert Terms in more than one column at a time.");
            }
            r.enableEvents = false;
            const newVals = Array.from(toList(delay(() => collect_1((rowInd) => {
                const tmp = map_1((prevValue) => {
                    if ((length(fileNameList) - 1) < rowInd) {
                        return void 0;
                    }
                    else {
                        return some(item(rowInd, fileNameList));
                    }
                }, range.values[rowInd]);
                return singleton_1(Array.from(tmp));
            }, rangeDouble(0, 1, range.values.length - 1)))));
            range.values = newVals;
            range.format.autofitColumns();
            r.enableEvents = true;
            return ["Info", (arg = (range.values.length | 0), toText(printf("%A, %A"))(arg)(newVals))];
        });
    });
}

export function getTableMetaData() {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getActiveAnnotationTableName(context).then((_arg) => {
        const sheet = context.workbook.worksheets.getActiveWorksheet();
        const annotationTable_1 = sheet.tables.getItem(_arg);
        annotationTable_1.columns.load("count");
        annotationTable_1.rows.load("count");
        const rowRange = annotationTable_1.getRange();
        rowRange.load(["address", "columnCount", "rowCount"]);
        const headerRange = annotationTable_1.getHeaderRowRange();
        headerRange.load(["address", "columnCount", "rowCount"]);
        return context.sync().then((_arg_1) => {
            let arg, arg_1, arg_2, arg_3, arg_4, arg_5, arg_6, arg_7;
            const matchValue = annotationTable_1.columns.count;
            const matchValue_1 = annotationTable_1.rows.count;
            const matchValue_2 = rowRange.address;
            const matchValue_3 = rowRange.columnCount;
            const matchValue_4 = rowRange.rowCount;
            const matchValue_5 = headerRange.address;
            const matchValue_6 = headerRange.columnCount;
            const matchValue_7 = headerRange.rowCount;
            return ["info", (arg = (~~matchValue | 0), (arg_1 = (~~matchValue_1 | 0), (arg_2 = replace(matchValue_2, "Sheet", ""), (arg_3 = (~~matchValue_3 | 0), (arg_4 = (~~matchValue_4 | 0), (arg_5 = replace(matchValue_5, "Sheet", ""), (arg_6 = (~~matchValue_6 | 0), (arg_7 = (~~matchValue_7 | 0), toText(printf("Table Metadata: [Table] : %ic x %ir ; [TotalRange] : %s : %ic x %ir ; [HeaderRowRange] : %s : %ic x %ir "))(arg)(arg_1)(arg_2)(arg_3)(arg_4)(arg_5)(arg_6)(arg_7)))))))))];
        }).then((_arg_2) => (Promise.resolve(_arg_2)));
    })))));
}

export function deleteAllCustomXml() {
    return Excel.run((context) => {
        const workbook = context.workbook.load(["customXmlParts"]);
        const customXmlParts = workbook.customXmlParts.load(["items"]);
        return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (context.sync().then((e) => Array.from(map_1((x) => {
            x.delete();
        }, customXmlParts.items))).then((_arg) => (Promise.resolve(["Info", "Deleted All Custom Xml!"]))))));
    });
}

export function getSwateCustomXml() {
    return Excel.run((context) => {
        const workbook = context.workbook.load(["customXmlParts"]);
        const customXmlParts = workbook.customXmlParts.load(["items"]);
        return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (context.sync().then((e) => Array.from(map_1((x) => x.getXml(), customXmlParts.items))).then((_arg) => (context.sync().then((e_1) => join("\n", map((x_1) => x_1.value, _arg))).then((_arg_1) => (Promise.resolve(_arg_1))))))));
    });
}

export function updateSwateCustomXml(newXmlString) {
    return Excel.run((context) => {
        const workbook = context.workbook.load(["customXmlParts"]);
        const customXmlParts = workbook.customXmlParts.load(["items"]);
        return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (context.sync().then((e) => Array.from(map_1((x) => {
            x.delete();
        }, customXmlParts.items))).then((_arg) => (context.sync().then((e_1) => customXmlParts.add(newXmlString)).then((_arg_1) => (Promise.resolve(["Info", "Custom xml update successful"]))))))));
    });
}

export function writeTableValidationToXml(tableValidation, currentSwateVersion) {
    return Excel.run((context) => {
        const newTableValidation = new TableValidation(toUniversalTime(now()), currentSwateVersion, tableValidation.AnnotationTable, tableValidation.Userlist, tableValidation.ColumnValidations);
        const workbook = context.workbook.load(["customXmlParts"]);
        const customXmlParts = workbook.customXmlParts.load(["items"]);
        return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getCustomXml(customXmlParts, context).then((_arg) => {
            const nextCustomXmlString = serializeXml(ofXmlElement(Validation_updateSwateValidation(newTableValidation, _arg)));
            return context.sync().then((e) => Array.from(map_1((x) => {
                x.delete();
            }, customXmlParts.items))).then((_arg_2) => (context.sync().then((e_1) => customXmlParts.add(nextCustomXmlString)).then((_arg_3) => {
                let arg_1;
                return Promise.resolve(["Info", (arg_1 = toString_1(newTableValidation.DateTime, "yyyy-MM-dd HH:mm"), toText(printf("Update Validation Scheme with \'%s\' @%s"))(newTableValidation.AnnotationTable)(arg_1))]);
            })));
        }))));
    });
}

//# sourceMappingURL=OfficeInterop.js.map
