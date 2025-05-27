import { CompositeCellActiveRender, BaseCell } from "./TableCell.fs.js";
import { ReactElement, createElement } from "react";
import React from "react";
import * as react from "react";
import { arrayHash, equalArrays, defaultOf, createObj } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { isNullOrWhiteSpace, join } from "../fable_modules/fable-library-ts.4.24.0/String.js";
import { map, toArray, empty, singleton, append, delay, toList } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { some, unwrap, Option, value as value_15 } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { reactApi } from "../fable_modules/Feliz.2.9.0/./Interop.fs.js";
import { singleton as singleton_1, FSharpList, ofArray } from "../fable_modules/fable-library-ts.4.24.0/List.js";
import { IReactProperty } from "../fable_modules/Feliz.2.9.0/Types.fs.js";
import { TableCellController } from "../Util/Types.fs.js";
import { ArcTable } from "../fable_modules/ARCtrl.Core.2.5.1/Table/ArcTable.fs.js";
import { IRefValue$1 } from "../fable_modules/Fable.React.Types.18.4.0/Fable.React.fs.js";
import { int32 } from "../fable_modules/fable-library-ts.4.24.0/Int32.js";
import { memo } from "../fable_modules/Feliz.2.9.0/./Internal.fs.js";
import { CompositeCell, CompositeCell_$union } from "../fable_modules/ARCtrl.Core.2.5.1/Table/CompositeCell.fs.js";
import { CompositeHeader_Component, CompositeHeader_Output, IOType_Sample, CompositeHeader_Input, IOType_Source, CompositeHeader_$union } from "../fable_modules/ARCtrl.Core.2.5.1/Table/CompositeHeader.fs.js";
import { toString } from "../fable_modules/fable-library-ts.4.24.0/Types.js";
import { createPortal } from "react-dom";
import { defaultOf as defaultOf_1 } from "../fable_modules/Feliz.2.9.0/../../Util/../fable_modules/fable-library-ts.4.24.0/Util.js";
import { CompositeCellModal } from "../ArcTypeComponents/ArcTypeModals.fs.js";
import { ContextMenu } from "../GenericComponents/ContextMenu.fs.js";
import { AnnotationTableContextMenu_CompositeCellContent_469BA83C, AnnotationTableContextMenu_CompositeHeaderContent_Z22A340EA, AnnotationTableContextMenu_IndexColumnContent_Z22A340EA } from "./AnnotationTableContextMenu.fs.js";
import { Table } from "./Table.fs.js";
import { FSharpSet__get_MinimumElement, FSharpSet } from "../fable_modules/fable-library-ts.4.24.0/Set.js";
import { ARCtrl_ArcTable__ArcTable_ClearSelectedCells_49F0F46F } from "../Util/Extensions.fs.js";
import { Dictionary } from "../fable_modules/fable-library-ts.4.24.0/MutableMap.js";
import { rangeDouble } from "../fable_modules/fable-library-ts.4.24.0/Range.js";
import { OntologyAnnotation } from "../fable_modules/ARCtrl.Core.2.5.1/OntologyAnnotation.fs.js";

export function InactiveTextRender(text: string, tcc: TableCellController, icon?: ReactElement): ReactElement {
    let elems: Iterable<ReactElement>;
    return BaseCell<FSharpList<IReactProperty>>(tcc.Index.y, tcc.Index.x, createElement<any>("div", createObj(ofArray([["className", join(" ", toList<string>(delay<string>((): Iterable<string> => append<string>((!tcc.IsSelected && (tcc.Index.y === 0)) ? singleton<string>("swt:bg-base-300") : empty<string>(), delay<string>((): Iterable<string> => singleton<string>("swt:flex swt:flex-row swt:gap-2 swt:items-center swt:h-full swt:max-w-full swt:px-2 swt:py-1 swt:w-full"))))))] as [string, any], (elems = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((icon != null) ? singleton<ReactElement>(value_15(icon)) : empty<ReactElement>(), delay<ReactElement>((): Iterable<ReactElement> => singleton<ReactElement>(createElement<any>("div", {
        className: "swt:truncate",
        children: text,
    })))))), ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])]))), "swt:w-full swt:h-full", ofArray([["title", text] as [string, any], ["onClick", (e: MouseEvent): void => {
        tcc.onClick(e);
    }] as [string, any]]));
}

export function AnnotationTable(annotationTableInputProps: any): ReactElement {
    let elems: Iterable<ReactElement>, children: FSharpList<ReactElement>, xs_2: Iterable<ReactElement>;
    const debug: Option<boolean> = annotationTableInputProps.debug;
    const setArcTable: ((arg0: ArcTable) => void) = annotationTableInputProps.setArcTable;
    const arcTable: ArcTable = annotationTableInputProps.arcTable;
    const containerRef: IRefValue$1<Option<HTMLElement>> = reactApi.useRef(undefined);
    let tableRef: IRefValue$1<TableHandle>;
    const initialValue_1: TableHandle = defaultOf();
    tableRef = (reactApi.useRef(initialValue_1));
    const patternInput: [Option<{ x: int32, y: int32 }>, ((arg0: Option<{ x: int32, y: int32 }>) => void)] = reactApi.useState<Option<{ x: int32, y: int32 }>, Option<{ x: int32, y: int32 }>>(undefined);
    const setDetailsModal: ((arg0: Option<{ x: int32, y: int32 }>) => void) = patternInput[1];
    const detailsModal: Option<{ x: int32, y: int32 }> = patternInput[0];
    const cellRender: ((arg0: [TableCellController, Option<CompositeCell_$union | CompositeHeader_$union>]) => ReactElement) = memo<[TableCellController, Option<CompositeCell_$union | CompositeHeader_$union>]>((tupledArg: [TableCellController, Option<CompositeCell_$union | CompositeHeader_$union>]): ReactElement => {
        const tcc: TableCellController = tupledArg[0];
        const compositeCell: Option<CompositeCell_$union | CompositeHeader_$union> = tupledArg[1];
        if (compositeCell != null) {
            if (value_15(compositeCell) instanceof CompositeCell) {
                const cell: CompositeCell_$union = value_15(compositeCell);
                return InactiveTextRender(toString(cell), tcc, unwrap(((cell.isTerm ? true : cell.isUnitized) && !isNullOrWhiteSpace(cell.AsTerm.TermAccessionShort)) ? createElement<any>("i", {
                    className: "fa-solid fa-check swt:text-primary",
                    title: cell.AsTerm.TermAccessionShort,
                }) : undefined));
            }
            else {
                const header: CompositeHeader_$union = value_15(compositeCell);
                return InactiveTextRender(toString(header), tcc);
            }
        }
        else {
            return BaseCell<Iterable<IReactProperty>>(tcc.Index.y, tcc.Index.x, tcc.Index.y, "swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-center swt:cursor-not-allowed swt:w-full swt:h-full swt:bg-base-200");
        }
    }, undefined, undefined, (tupledArg_1: [TableCellController, Option<CompositeCell_$union | CompositeHeader_$union>]): string => {
        const tcc_1: TableCellController = tupledArg_1[0];
        return `${tcc_1.Index.x}-${tcc_1.Index.y}`;
    });
    const renderActiveCell: ((arg0: TableCellController) => ReactElement) = memo<TableCellController>((tcc_2: TableCellController): ReactElement => {
        let cell_3: { x: int32, y: int32 };
        if ((tcc_2.Index.x > 0) && (tcc_2.Index.y > 0)) {
            return CompositeCellActiveRender(tcc_2, arcTable.GetCellAt(tcc_2.Index.x - 1, tcc_2.Index.y - 1), (cell_3 = tcc_2.Index, (cc: CompositeCell_$union): void => {
                arcTable.SetCellAt(cell_3.x - 1, cell_3.y - 1, cc);
            }));
        }
        else {
            return createElement<any>("div", {
                children: ["Unknown cell type"],
            });
        }
    }, undefined, undefined, undefined);
    return createElement<any>("div", createObj(ofArray([["ref", containerRef] as [string, any], (elems = [(children = singleton_1(createElement<any>("button", {
        className: "swt:btn swt:btn-primary",
        onClick: (_arg: MouseEvent): void => {
            const iscontained: boolean = tableRef.current.SelectHandle.contains({
                x: 2,
                y: 2,
            });
            console.log(["iscontained", iscontained] as [string, boolean]);
        },
        children: "Verify 2,2",
    })), createElement<any>("div", {
        children: reactApi.Children.toArray(Array.from(children)),
    })), createPortal((xs_2 = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => {
        if (detailsModal != null) {
            const cc_1: { x: int32, y: int32 } = value_15(detailsModal);
            if (cc_1.x === 0) {
                return singleton<ReactElement>(defaultOf_1());
            }
            else if (cc_1.y === 0) {
                const header_1: CompositeHeader_$union = arcTable.Headers[cc_1.x - 1];
                return singleton<ReactElement>(defaultOf_1());
            }
            else {
                const cell_5: CompositeCell_$union = arcTable.GetCellAt(cc_1.x - 1, cc_1.y - 1);
                const header_2: CompositeHeader_$union = arcTable.Headers[cc_1.x - 1];
                return singleton<ReactElement>(createElement(CompositeCellModal, {
                    compositeCell: cell_5,
                    setCell: (cell_6: CompositeCell_$union): void => {
                        arcTable.SetCellAt(cc_1.x - 1, cc_1.y - 1, cell_6);
                        setArcTable(arcTable);
                    },
                    rmv: (): void => {
                        tableRef.current.focus();
                        setDetailsModal(undefined);
                    },
                    relevantCompositeHeader: header_2,
                }));
            }
        }
        else {
            return singleton<ReactElement>(defaultOf_1());
        }
    })), react.createElement(react.Fragment, {}, ...xs_2)), document.body), createElement(ContextMenu, {
        childInfo: (data: any): FSharpList<ContextMenuItem> => {
            const index: { x: int32, y: int32 } = data;
            return (index.x === 0) ? AnnotationTableContextMenu_IndexColumnContent_Z22A340EA<ContextMenuItem>(index.y, arcTable, setArcTable) : ((index.y === 0) ? AnnotationTableContextMenu_CompositeHeaderContent_Z22A340EA<ContextMenuItem>(index.x, arcTable, setArcTable) : AnnotationTableContextMenu_CompositeCellContent_469BA83C({
                x: index.x,
                y: index.y,
            }, arcTable, setArcTable, tableRef.current.SelectHandle));
        },
        ref: containerRef,
        onSpawn: (e: MouseEvent): Option<any> => {
            let container: HTMLElement, cell_7: Element;
            const target = e.target as HTMLElement;
            const matchValue: Option<Element> = target.closest("[data-row][data-column]");
            const matchValue_1: Option<HTMLElement> = containerRef.current;
            let matchResult: int32, cell_8: Element, container_1: HTMLElement;
            if (matchValue != null) {
                if (matchValue_1 != null) {
                    if ((container = value_15(matchValue_1), (cell_7 = value_15(matchValue), container.contains(cell_7)))) {
                        matchResult = 0;
                        cell_8 = value_15(matchValue);
                        container_1 = value_15(matchValue_1);
                    }
                    else {
                        matchResult = 1;
                    }
                }
                else {
                    matchResult = 1;
                }
            }
            else {
                matchResult = 1;
            }
            switch (matchResult) {
                case 0: {
                    const cell_9 = cell_8! as HTMLElement;
                    const row: int32 = cell_9.dataset.row | 0;
                    const indices: { x: int32, y: int32 } = {
                        x: cell_9.dataset.column,
                        y: row,
                    };
                    console.log(indices);
                    return some(indices);
                }
                default: {
                    console.log("No table cell found");
                    return undefined;
                }
            }
        },
    }), createElement(Table, {
        rowCount: arcTable.RowCount + 1,
        columnCount: arcTable.ColumnCount + 1,
        renderCell: (tcc_3: TableCellController): ReactElement => cellRender([tcc_3, (tcc_3.Index.x === 0) ? undefined : ((tcc_3.Index.y === 0) ? arcTable.Headers[tcc_3.Index.x - 1] : arcTable.GetCellAt(tcc_3.Index.x - 1, tcc_3.Index.y - 1))] as [TableCellController, Option<CompositeCell_$union | CompositeHeader_$union>]),
        renderActiveCell: renderActiveCell,
        ref: tableRef,
        onKeydown: (tupledArg_2: [KeyboardEvent, { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> }, Option<{ x: int32, y: int32 }>]): void => {
            const e_1: KeyboardEvent = tupledArg_2[0];
            const selectedCells: { SelectOrigin?: { x: int32, y: int32 }, clear: (() => void), contains: ((arg0: { x: int32, y: int32 }) => boolean), count: int32, lastAppend?: { x: int32, y: int32 }, selectAt: ((arg0: [{ x: int32, y: int32 }, boolean]) => void), selectBy: ((arg0: KeyboardEvent) => boolean), selectedCells?: { xEnd: int32, xStart: int32, yEnd: int32, yStart: int32 }, selectedCellsReducedSet: FSharpSet<{ x: int32, y: int32 }> } = tupledArg_2[1];
            if ((((e_1.ctrlKey ? true : e_1.metaKey) && (e_1.code === "Enter")) && (tupledArg_2[2] == null)) && (selectedCells.count > 0)) {
                const cell_11: { x: int32, y: int32 } = FSharpSet__get_MinimumElement(selectedCells.selectedCellsReducedSet);
                console.log(["set details modal for:", cell_11] as [string, { x: int32, y: int32 }]);
                setDetailsModal(cell_11);
            }
            else if ((e_1.code === "Delete") && (selectedCells.count > 0)) {
                ARCtrl_ArcTable__ArcTable_ClearSelectedCells_49F0F46F(arcTable, tableRef.current.SelectHandle);
                setArcTable(arcTable.Copy());
            }
        },
        enableColumnHeaderSelect: true,
    })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
}

export default AnnotationTable;

export function Entry(): ReactElement {
    const arcTable: ArcTable = new ArcTable("TestTable", [], new Dictionary<[int32, int32], CompositeCell_$union>([], {
        Equals: equalArrays,
        GetHashCode: arrayHash,
    }));
    arcTable.AddColumn(CompositeHeader_Input(IOType_Source()), toArray<CompositeCell_$union>(delay<CompositeCell_$union>((): Iterable<CompositeCell_$union> => map<int32, CompositeCell_$union>((i: int32): CompositeCell_$union => CompositeCell.createFreeText(`Source ${i}`), rangeDouble(0, 1, 100)))));
    arcTable.AddColumn(CompositeHeader_Output(IOType_Sample()), toArray<CompositeCell_$union>(delay<CompositeCell_$union>((): Iterable<CompositeCell_$union> => map<int32, CompositeCell_$union>((i_1: int32): CompositeCell_$union => CompositeCell.createFreeText(`Sample ${i_1}`), rangeDouble(0, 1, 100)))));
    arcTable.AddColumn(CompositeHeader_Component(new OntologyAnnotation("instrument model", "MS", "MS:2138970")), toArray<CompositeCell_$union>(delay<CompositeCell_$union>((): Iterable<CompositeCell_$union> => map<int32, CompositeCell_$union>((i_2: int32): CompositeCell_$union => CompositeCell.createTermFromString("SCIEX instrument model", "MS", "MS:11111231"), rangeDouble(0, 1, 100)))));
    const patternInput: [ArcTable, ((arg0: ArcTable) => void)] = reactApi.useState<ArcTable, ArcTable>(arcTable);
    return createElement(AnnotationTable, {
        arcTable: patternInput[0],
        setArcTable: patternInput[1],
    });
}

