import { empty as empty_1, singleton, append, delay, indexed, collect, map, toArray } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { Array_distinctBy, Array_groupBy, Array_distinct, groupBy } from "../../../../fable_modules/fable-library.4.9.0/Seq2.js";
import { stringHash, int32ToString, structuralHash, safeHash, equals, comparePrimitives, numberHash } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { BuildingBlock__get_hasCompleteTSRTAN, BuildingBlock__get_hasUnit, Cell_create, SwateColumnHeader_create_Z721C83C5, Column_create, SwateColumnHeader__get_tryGetOntologyTerm, SwateColumnHeader__get_getFeaturedColTermMinimal, SwateColumnHeader__get_isFeaturedCol, SwateColumnHeader__get_tryGetTermAccession, SwateColumnHeader__get_isReference, BuildingBlock_create, SwateColumnHeader__get_isMainColumn, SwateColumnHeader__get_isSwateColumnHeader, SwateColumnHeader__get_isUnitCol, SwateColumnHeader__get_isTSRCol, BuildingBlock, SwateColumnHeader__get_isTANCol, SwateColumnHeader__get_getColumnCoreName } from "../../../Shared/OfficeInteropTypes.js";
import { bind, defaultArgWith, value as value_2 } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { reverse, toArray as toArray_1, empty, cons } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { TermTypes_TermSearchable__get_hasEmptyTerm, TermTypes_TermSearchable_create, TermTypes_TermMinimal_ofNumberFormat_Z721C83C5, TermTypes_TermMinimal_create } from "../../../Shared/TermTypes.js";
import { tryExactlyOne, contains, sortBy, choose, minBy, mapIndexed, map as map_1 } from "../../../../fable_modules/fable-library.4.9.0/Array.js";
import { toString } from "../../../../fable_modules/fable-library.4.9.0/Types.js";
import { rangeDouble } from "../../../../fable_modules/fable-library.4.9.0/Range.js";
import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../../../../fable_modules/Fable.Promise.3.2.0/Promise.fs.js";
import { promise } from "../../../../fable_modules/Fable.Promise.3.2.0/PromiseImpl.fs.js";
import { join } from "../../../../fable_modules/fable-library.4.9.0/String.js";
import { parseTermAccession } from "../../../Shared/Regex.js";

function viewRowsByColumns(rows) {
    return toArray(map((arg_1) => toArray(map((tuple_2) => tuple_2[1], arg_1[1])), groupBy((tuple) => tuple[0], collect(indexed, rows), {
        Equals: (x_1, y) => (x_1 === y),
        GetHashCode: numberHash,
    })));
}

function getBuildingBlocksPreSync(context, annotationTable) {
    context.workbook.load("tables");
    const annotationTable_1 = context.workbook.tables.getItem(annotationTable);
    const annoHeaderRange = annotationTable_1.getHeaderRowRange();
    annoHeaderRange.load(["columnIndex", "values", "columnCount"]);
    const annoBodyRange = annotationTable_1.getDataBodyRange();
    annoBodyRange.load(["values", "numberFormat"]);
    return [annoHeaderRange, annoBodyRange];
}

function getBuildingBlocksPreSyncFromTable(annotationTable) {
    const annoHeaderRange = annotationTable.getHeaderRowRange();
    annoHeaderRange.load(["columnIndex", "values", "columnCount"]);
    const annoBodyRange = annotationTable.getDataBodyRange();
    annoBodyRange.load(["values", "numberFormat"]);
    return [annoHeaderRange, annoBodyRange];
}

/**
 * It should never happen, that the nextColumn is a reference column without an existing building block.
 */
export function Aux_GetBuildingBlocksPostSync_errorMsg1(nextCol) {
    throw new Error(`Swate encountered an error while processing the active annotation table.
            Swate found a reference column (${nextCol.Header.SwateColumnHeader}) without a prior main column..`);
}

/**
 * Hidden columns do only come with certain core names. The acceptable names can be found in OfficeInterop.Types.ColumnCoreNames.
 */
export function Aux_GetBuildingBlocksPostSync_errorMsg2(nextCol) {
    throw new Error(`Swate encountered an error while processing the active annotation table.
            Swate found a reference column (${nextCol.Header.SwateColumnHeader}) with an unknown core name: ${SwateColumnHeader__get_getColumnCoreName(nextCol.Header)}`);
}

/**
 * If a columns core name already exists for the current building block, then the block is faulty and needs userinput to be corrected.
 */
export function Aux_GetBuildingBlocksPostSync_errorMsg3(nextCol, buildingBlock, assignedCol) {
    throw new Error(`Swate encountered an error while processing the active annotation table.
            Swate found a reference column (${nextCol.Header.SwateColumnHeader}) with a core name (${SwateColumnHeader__get_getColumnCoreName(nextCol.Header)}), that is already assigned to the previous building block.
            Building block main column: ${buildingBlock.MainColumn.Header.SwateColumnHeader}, already assigned column: ${assignedCol}`);
}

/**
 * Update current building block with new reference column. A ref col can be TSR, TAN and Unit.
 */
export function Aux_GetBuildingBlocksPostSync_checkForReferenceColumnType(currentBlock, nextCol) {
    let bind$0040, bind$0040_1, bind$0040_2;
    const matchValue = nextCol.Header;
    if (SwateColumnHeader__get_isTANCol(matchValue)) {
        if (currentBlock == null) {
            Aux_GetBuildingBlocksPostSync_errorMsg1(nextCol);
        }
        if (value_2(currentBlock).TAN != null) {
            Aux_GetBuildingBlocksPostSync_errorMsg3(nextCol, value_2(currentBlock), value_2(value_2(currentBlock).TAN).Header.SwateColumnHeader);
        }
        return (bind$0040 = value_2(currentBlock), new BuildingBlock(bind$0040.MainColumn, bind$0040.MainColumnTerm, bind$0040.Unit, bind$0040.TSR, nextCol));
    }
    else if (SwateColumnHeader__get_isTSRCol(matchValue)) {
        if (currentBlock == null) {
            Aux_GetBuildingBlocksPostSync_errorMsg1(nextCol);
        }
        if (value_2(currentBlock).TSR != null) {
            Aux_GetBuildingBlocksPostSync_errorMsg3(nextCol, value_2(currentBlock), value_2(value_2(currentBlock).TSR).Header.SwateColumnHeader);
        }
        return (bind$0040_1 = value_2(currentBlock), new BuildingBlock(bind$0040_1.MainColumn, bind$0040_1.MainColumnTerm, bind$0040_1.Unit, nextCol, bind$0040_1.TAN));
    }
    else if (SwateColumnHeader__get_isUnitCol(matchValue)) {
        if (currentBlock == null) {
            Aux_GetBuildingBlocksPostSync_errorMsg1(nextCol);
        }
        if (value_2(currentBlock).Unit != null) {
            Aux_GetBuildingBlocksPostSync_errorMsg3(nextCol, value_2(currentBlock), value_2(value_2(currentBlock).Unit).Header.SwateColumnHeader);
        }
        return (bind$0040_2 = value_2(currentBlock), new BuildingBlock(bind$0040_2.MainColumn, bind$0040_2.MainColumnTerm, nextCol, bind$0040_2.TSR, bind$0040_2.TAN));
    }
    else {
        return Aux_GetBuildingBlocksPostSync_errorMsg2(nextCol)(currentBlock);
    }
}

export function Aux_GetBuildingBlocksPostSync_sortColsIntoBuildingBlocks(columns) {
    const sortColsIntoBuildingBlocksREC = (index_mut, currentBlock_mut, buildingBlockList_mut) => {
        sortColsIntoBuildingBlocksREC:
        while (true) {
            const index = index_mut, currentBlock = currentBlock_mut, buildingBlockList = buildingBlockList_mut;
            if (index > (columns.length - 1)) {
                if (currentBlock != null) {
                    return cons(value_2(currentBlock), buildingBlockList);
                }
                else {
                    return buildingBlockList;
                }
            }
            else {
                const nextCol = columns[index];
                if (!SwateColumnHeader__get_isSwateColumnHeader(nextCol.Header)) {
                    index_mut = (index + 1);
                    currentBlock_mut = currentBlock;
                    buildingBlockList_mut = buildingBlockList;
                    continue sortColsIntoBuildingBlocksREC;
                }
                else if (SwateColumnHeader__get_isMainColumn(nextCol.Header)) {
                    const newBuildingBlock = BuildingBlock_create(nextCol, void 0, void 0, void 0, void 0);
                    if (currentBlock != null) {
                        index_mut = (index + 1);
                        currentBlock_mut = newBuildingBlock;
                        buildingBlockList_mut = cons(value_2(currentBlock), buildingBlockList);
                        continue sortColsIntoBuildingBlocksREC;
                    }
                    else {
                        index_mut = (index + 1);
                        currentBlock_mut = newBuildingBlock;
                        buildingBlockList_mut = buildingBlockList;
                        continue sortColsIntoBuildingBlocksREC;
                    }
                }
                else if (SwateColumnHeader__get_isReference(nextCol.Header)) {
                    index_mut = (index + 1);
                    currentBlock_mut = Aux_GetBuildingBlocksPostSync_checkForReferenceColumnType(currentBlock, nextCol);
                    buildingBlockList_mut = buildingBlockList;
                    continue sortColsIntoBuildingBlocksREC;
                }
                else {
                    throw new Error(`Unable to parse "${nextCol.Header}" into building blocks.`);
                }
            }
            break;
        }
    };
    return sortColsIntoBuildingBlocksREC(0, void 0, empty());
}

/**
 * After sorting all Columns into BuildingBlocks, we want to parse the **combined** info of BuildingBlocks related to a Term
 * (exmp.: Parameter [instrument model], Protocol Type) into a field of the BuildingBlock record type. This is done here.
 */
export function Aux_GetBuildingBlocksPostSync_getMainColumnTerm(bb) {
    let matchValue, matchValue_1, tan, tsr, tsrTermAccession, tanTermAccession, termName, termName_2, accession1, accession2, termName_1;
    return new BuildingBlock(bb.MainColumn, (matchValue = bb.TSR, (matchValue_1 = bb.TAN, (matchValue != null) ? ((matchValue_1 != null) ? ((tan = matchValue_1, (tsr = matchValue, (tsrTermAccession = SwateColumnHeader__get_tryGetTermAccession(tsr.Header), (tanTermAccession = SwateColumnHeader__get_tryGetTermAccession(tan.Header), (termName = (SwateColumnHeader__get_isFeaturedCol(bb.MainColumn.Header) ? SwateColumnHeader__get_getFeaturedColTermMinimal(bb.MainColumn.Header).Name : SwateColumnHeader__get_tryGetOntologyTerm(bb.MainColumn.Header)), (termName == null) ? ((tsrTermAccession == null) ? ((tanTermAccession == null) ? void 0 : (() => {
        throw new Error(`Swate found mismatching reference columns in building block ${bb.MainColumn.Header}: Found TSR and TAN column but no complete term accessions.`);
    })()) : ((tanTermAccession != null) ? (() => {
        throw new Error(`Swate found mismatching ontology term infor in building block ${bb.MainColumn.Header}: Found term accession in reference columns, but no ontology ref in main column.`);
    })() : (() => {
        throw new Error(`Swate found mismatching reference columns in building block ${bb.MainColumn.Header}: Found TSR and TAN column but no complete term accessions.`);
    })())) : ((tsrTermAccession == null) ? ((tanTermAccession == null) ? ((termName_2 = termName, TermTypes_TermMinimal_create(termName_2, ""))) : (() => {
        throw new Error(`Swate found mismatching reference columns in building block ${bb.MainColumn.Header}: Found TSR and TAN column but no complete term accessions.`);
    })()) : ((tanTermAccession != null) ? ((accession1 = tsrTermAccession, (accession2 = tanTermAccession, (termName_1 = termName, ((accession1 !== accession2) ? (() => {
        throw new Error(`Swate found mismatching term accession in building block ${bb.MainColumn.Header}: ${accession1}, ${accession2}`);
    })() : void 0, TermTypes_TermMinimal_create(termName_1, accession1)))))) : (() => {
        throw new Error(`Swate found mismatching reference columns in building block ${bb.MainColumn.Header}: Found TSR and TAN column but no complete term accessions.`);
    })())))))))) : (() => {
        throw new Error(`Swate found mismatching reference columns in building block ${bb.MainColumn.Header}: Found only TSR or TAN.`);
    })()) : ((matchValue_1 == null) ? void 0 : (() => {
        throw new Error(`Swate found mismatching reference columns in building block ${bb.MainColumn.Header}: Found only TSR or TAN.`);
    })()))), bb.Unit, bb.TSR, bb.TAN);
}

function getBuildingBlocksPostSync(annoHeaderRange, annoBodyRange, context) {
    return context.sync().then((_arg) => {
        const columnBodies = viewRowsByColumns(annoBodyRange.values);
        const numberFormats = viewRowsByColumns(annoBodyRange.numberFormat);
        return map_1(Aux_GetBuildingBlocksPostSync_getMainColumnTerm, toArray_1(reverse(Aux_GetBuildingBlocksPostSync_sortColsIntoBuildingBlocks(toArray(delay(() => map((ind) => Column_create(ind, SwateColumnHeader_create_Z721C83C5(toString(value_2(annoHeaderRange.values[0][ind]))), mapIndexed((i, cellVal) => {
            let unit;
            return Cell_create(i + 1, (cellVal != null) ? toString(value_2(cellVal)) : void 0, (unit = numberFormats[ind][i], ((unit != null) && (toString(value_2(unit)) !== "General")) ? (() => {
                try {
                    return TermTypes_TermMinimal_ofNumberFormat_Z721C83C5(toString(value_2(unit)));
                }
                catch (matchValue) {
                    return void 0;
                }
            })() : void 0));
        }, columnBodies[ind])), rangeDouble(0, 1, ~~annoHeaderRange.columnCount - 1))))))));
    });
}

/**
 * ExcelApi 1.1
 */
export function Shared_OfficeInteropTypes_BuildingBlock__BuildingBlock_getFromContext_Static_7330B32B(context, annotationTableName) {
    const patternInput = getBuildingBlocksPreSync(context, annotationTableName);
    return getBuildingBlocksPostSync(patternInput[0], patternInput[1], context);
}

/**
 * ExcelApi 1.1
 */
export function Shared_OfficeInteropTypes_BuildingBlock__BuildingBlock_getFromContext_Static_240B550(context, annotationTable) {
    const patternInput = getBuildingBlocksPreSyncFromTable(annotationTable);
    return getBuildingBlocksPostSync(patternInput[0], patternInput[1], context);
}

/**
 * ExcelApi 1.1
 */
export function getBuildingBlocks(context, annotationTableName) {
    const patternInput = getBuildingBlocksPreSync(context, annotationTableName);
    return getBuildingBlocksPostSync(patternInput[0], patternInput[1], context);
}

export function findSelectedBuildingBlockPreSync(context, annotationTableName) {
    const selectedRange = context.workbook.getSelectedRange();
    selectedRange.load(["values", "columnIndex", "columnCount"]);
    const patternInput = getBuildingBlocksPreSync(context, annotationTableName);
    return [selectedRange, patternInput[0], patternInput[1]];
}

export function findSelectedBuildingBlockPostSync(selectedRange, annoHeaderRange, annoBodyRange, context) {
    return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (getBuildingBlocksPostSync(annoHeaderRange, annoBodyRange, context).then((_arg) => (context.sync().then((_arg_1) => {
        if (selectedRange.columnCount !== 1) {
            throw new Error("To use this function please select a single column");
        }
        let newSelectedColIndex;
        const diff = selectedRange.columnIndex - annoHeaderRange.columnIndex;
        if (diff < 0) {
            throw new Error("To use this function please select a single column of a Swate table.");
        }
        else if (diff > (annoHeaderRange.columnCount - 1)) {
            throw new Error("To use this function please select a single column of a Swate table.");
        }
        else {
            newSelectedColIndex = diff;
        }
        return minBy((x_1) => Math.abs(x_1.MainColumn.Index - ~~newSelectedColIndex), _arg.filter((x) => (x.MainColumn.Index <= ~~newSelectedColIndex)), {
            Compare: comparePrimitives,
        });
    }).then((_arg_2) => (Promise.resolve(_arg_2))))))));
}

export function findSelectedBuildingBlock(context, annotationTableName) {
    const patternInput = findSelectedBuildingBlockPreSync(context, annotationTableName);
    return findSelectedBuildingBlockPostSync(patternInput[0], patternInput[1], patternInput[2], context);
}

export function toTermSearchable(buildingBlock) {
    const colIndex = buildingBlock.MainColumn.Index | 0;
    const parentTerm = buildingBlock.MainColumnTerm;
    const allUnits = BuildingBlock__get_hasUnit(buildingBlock) ? map_1((tupledArg_1) => TermTypes_TermSearchable_create(tupledArg_1[0], void 0, true, colIndex, Array_distinct(map_1((tuple_1) => tuple_1[1], tupledArg_1[1], Int32Array), {
        Equals: (x_1, y_1) => (x_1 === y_1),
        GetHashCode: numberHash,
    })), Array_groupBy((tuple) => tuple[0], choose((tupledArg) => {
        const unitName = tupledArg[0];
        if (unitName != null) {
            return [value_2(unitName), tupledArg[1]];
        }
        else {
            return void 0;
        }
    }, map_1((cell) => [cell.Unit, cell.Index], buildingBlock.MainColumn.Cells)), {
        Equals: equals,
        GetHashCode: safeHash,
    })) : [];
    const allTermValues = (BuildingBlock__get_hasCompleteTSRTAN(buildingBlock) && !BuildingBlock__get_hasUnit(buildingBlock)) ? map_1((tupledArg_3) => {
        let array_11;
        const cellRowIndices_1 = Array_distinct(map_1((tuple_3) => tuple_3[1], tupledArg_3[1], Int32Array), {
            Equals: (x_3, y_3) => (x_3 === y_3),
            GetHashCode: numberHash,
        });
        const tryFindAccession = Array_distinctBy((x_8) => x_8.Value, sortBy((x_6) => x_6.Index, (array_11 = value_2(buildingBlock.TAN).Cells, array_11.filter((x_4) => {
            if (contains(x_4.Index, cellRowIndices_1, {
                Equals: (x_5, y_4) => (x_5 === y_4),
                GetHashCode: numberHash,
            }) && (x_4.Value != null)) {
                return value_2(x_4.Value).trim() !== "";
            }
            else {
                return false;
            }
        })), {
            Compare: comparePrimitives,
        }), {
            Equals: equals,
            GetHashCode: structuralHash,
        });
        if (tryFindAccession.length > 1) {
            throw new Error(`Swate found different accessions for the same Term! Please check column '${buildingBlock.MainColumn.Header.SwateColumnHeader}', different accession for rows: ${join(", ", map_1((x_10) => int32ToString(x_10.Index), tryFindAccession))}.`);
        }
        return TermTypes_TermSearchable_create(TermTypes_TermMinimal_create(tupledArg_3[0], defaultArgWith(bind(parseTermAccession, bind((x_11) => x_11.Value, tryExactlyOne(tryFindAccession))), () => "")), parentTerm, false, colIndex, cellRowIndices_1);
    }, Array_groupBy((tuple_2) => tuple_2[0], choose((tupledArg_2) => {
        const valueName = tupledArg_2[0];
        if (valueName != null) {
            return [value_2(valueName), tupledArg_2[1]];
        }
        else {
            return void 0;
        }
    }, map_1((cell_1) => [cell_1.Value, cell_1.Index], buildingBlock.MainColumn.Cells)), {
        Equals: (x_2, y_2) => (x_2 === y_2),
        GetHashCode: stringHash,
    })) : [];
    const array_17 = toArray(delay(() => append((parentTerm != null) ? singleton(TermTypes_TermSearchable_create(value_2(parentTerm), void 0, false, colIndex, new Int32Array([0]))) : empty_1(), delay(() => append(allUnits, delay(() => allTermValues))))));
    return array_17.filter((x_13) => !TermTypes_TermSearchable__get_hasEmptyTerm(x_13));
}

//# sourceMappingURL=BuildingBlockFunctions.js.map
