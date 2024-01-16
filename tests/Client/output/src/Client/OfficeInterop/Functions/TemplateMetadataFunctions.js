import { map, singleton, collect as collect_1, delay, toArray as toArray_1, iterate } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { equals } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { toArray, cons, isEmpty, collect } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Metadata_MetadataField } from "../../../Shared/TemplateTypes.js";
import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../../../../fable_modules/Fable.Promise.3.2.0/Promise.fs.js";
import { promise } from "../../../../fable_modules/Fable.Promise.3.2.0/PromiseImpl.fs.js";
import { rangeDouble } from "../../../../fable_modules/fable-library.4.9.0/Range.js";
import { newGuid } from "../../../../fable_modules/fable-library.4.9.0/Guid.js";
import { iterateIndexed, findIndex } from "../../../../fable_modules/fable-library.4.9.0/Array.js";
import { value as value_2, some } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { Excel_Shade10, Excel_Tint40, Excel_Primary } from "../../Colors/ExcelColors.js";

function colorOuterBordersWhite(borderSeq) {
    iterate((border) => {
        if (((equals(border.sideIndex, "EdgeBottom") ? true : equals(border.sideIndex, "EdgeLeft")) ? true : equals(border.sideIndex, "EdgeRight")) ? true : equals(border.sideIndex, "EdgeTop")) {
            border.color = "#FEFEFE";
        }
    }, borderSeq);
}

function colorTopBottomBordersWhite(borderSeq) {
    iterate((border) => {
        if (equals(border.sideIndex, "EdgeBottom") ? true : equals(border.sideIndex, "EdgeTop")) {
            border.color = "#FEFEFE";
        }
    }, borderSeq);
}

export function extendMetadataFields(metadatafields) {
    const children = collect(extendMetadataFields, metadatafields.Children);
    if (((metadatafields.Key !== "") && !isEmpty(metadatafields.Children)) && metadatafields.List) {
        return cons(new Metadata_MetadataField(metadatafields.Key, `#${metadatafields.ExtendedNameKey.toLocaleUpperCase()} list`, metadatafields.Description, metadatafields.List, metadatafields.Children), children);
    }
    else if ((metadatafields.Key !== "") && !isEmpty(metadatafields.Children)) {
        return cons(new Metadata_MetadataField(metadatafields.Key, "#" + metadatafields.ExtendedNameKey.toLocaleUpperCase(), metadatafields.Description, metadatafields.List, metadatafields.Children), children);
    }
    else if (metadatafields.Key !== "") {
        return cons(metadatafields, children);
    }
    else {
        return children;
    }
}

export function createTemplateMetadataWorksheet(metadatafields) {
    return Excel.run((context) => PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => {
        const extended = toArray(extendMetadataFields(metadatafields));
        const rowLength = extended.length;
        return context.sync().then((e) => context.workbook.worksheets.add("SwateTemplateMetadata")).then((_arg) => {
            const newWorksheet = _arg;
            return context.sync().then((e_1) => {
                const fst = newWorksheet.getRangeByIndexes(0, 0, rowLength, 1);
                fst.format.borders.load("items");
                const fstCells = toArray_1(delay(() => collect_1((i) => {
                    const cell = fst.getCell(i, 0);
                    cell.format.borders.load("items");
                    return singleton(cell);
                }, rangeDouble(0, 1, rowLength - 1))));
                const sndCells = toArray_1(delay(() => collect_1((i_1) => {
                    const cell_1 = fst.getCell(i_1, 1);
                    cell_1.format.borders.load("items");
                    return singleton(cell_1);
                }, rangeDouble(0, 1, rowLength - 1))));
                return [fst, fstCells, newWorksheet.getRangeByIndexes(0, 1, rowLength, 1), sndCells];
            }).then((_arg_1) => {
                const sndColumnCells = _arg_1[3];
                const sndColumn = _arg_1[2];
                const fstColumnCells = _arg_1[1];
                const firstColumn = _arg_1[0];
                const newIdent = newGuid();
                const idValueIndex = findIndex((x) => (x.Key === "Id"), extended) | 0;
                const descriptionValueIndex = findIndex((x_1) => (x_1.Key === "Description"), extended) | 0;
                const columnValues = Array.from(toArray_1(delay(() => map((i_2) => [some(extended[i_2].ExtendedNameKey)], rangeDouble(0, 1, ~~rowLength - 1)))));
                return context.sync().then((e_2) => {
                    firstColumn.values = columnValues;
                    sndColumnCells[idValueIndex].values = [[some(newIdent)]];
                    firstColumn.format.autofitColumns();
                    firstColumn.format.autofitRows();
                    firstColumn.format.font.bold = true;
                    firstColumn.format.font.color = "whitesmoke";
                    colorOuterBordersWhite(firstColumn.format.borders.items);
                    iterate((border) => {
                        if (equals(border.sideIndex, "EdgeRight")) {
                            border.weight = "Thick";
                        }
                    }, firstColumn.format.borders.items);
                    sndColumnCells.forEach((cell_2) => {
                        colorOuterBordersWhite(cell_2.format.borders.items);
                    });
                    firstColumn.format.verticalAlignment = "Top";
                    sndColumn.format.verticalAlignment = "Top";
                    const sndColStyling = iterateIndexed((i_3, info) => {
                        if (isEmpty(info.Children)) {
                            fstColumnCells[i_3].format.fill.color = Excel_Primary;
                            sndColumnCells[i_3].format.fill.color = Excel_Tint40;
                        }
                        else {
                            fstColumnCells[i_3].format.fill.color = Excel_Shade10;
                            colorTopBottomBordersWhite(sndColumnCells[i_3].format.borders.items);
                            sndColumnCells[i_3].format.fill.color = Excel_Shade10;
                        }
                    }, extended);
                    sndColumnCells[idValueIndex].format.fill.color = "#C21F3A";
                    const newComments = iterateIndexed((i_4, info_1) => {
                        if ((info_1.Description != null) && (value_2(info_1.Description) !== "")) {
                            const targetCellRange = fstColumnCells[i_4];
                            const content = value_2(info_1.Description);
                            if (i_4 === idValueIndex) {
                                const comment = context.workbook.comments.add(targetCellRange, content, "Plain");
                                comment.replies.add(`id=${newIdent}`, "Plain");
                            }
                            else {
                                const comment_1 = context.workbook.comments.add(targetCellRange, content, "Plain");
                            }
                        }
                    }, extended);
                    sndColumn.format.columnWidth = 300;
                    sndColumnCells[descriptionValueIndex].format.rowHeight = 50;
                    sndColumn.format.wrapText = true;
                    newWorksheet.activate();
                }).then(() => (Promise.resolve(["Info", "Created new template metadata sheet!"])));
            });
        });
    })));
}

//# sourceMappingURL=TemplateMetadataFunctions.js.map
