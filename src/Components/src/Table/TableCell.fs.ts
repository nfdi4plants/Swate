import { unwrap, map, Option, value as value_15, defaultArg } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { ReactElement, createElement } from "react";
import React from "react";
import { createObj } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { empty, singleton, append, delay, toList } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { IReactProperty } from "../fable_modules/Feliz.2.9.0/Types.fs.js";
import { isNullOrWhiteSpace, join } from "../fable_modules/fable-library-ts.4.24.0/String.js";
import { reactApi } from "../fable_modules/Feliz.2.9.0/./Interop.fs.js";
import { int32 } from "../fable_modules/fable-library-ts.4.24.0/Int32.js";
import { TermModule_toOntologyAnnotation, TermModule_fromOntologyAnnotation, TableCellController } from "../Util/Types.fs.js";
import { CompositeCell_$union, CompositeCell_Term, CompositeCell_Data, CompositeCell_Unitized, CompositeCell_FreeText } from "../fable_modules/ARCtrl.Core.2.5.1/Table/CompositeCell.fs.js";
import { OntologyAnnotation } from "../fable_modules/ARCtrl.Core.2.5.1/OntologyAnnotation.fs.js";
import { Data } from "../fable_modules/ARCtrl.Core.2.5.1/Data.fs.js";
import { Option_whereNot } from "../../../Shared/Extensions.fs.js";
import { TermSearch } from "../TermSearch/TermSearch.fs.js";
import { ofArray } from "../fable_modules/fable-library-ts.4.24.0/List.js";

export function BaseCell<$a extends Iterable<IReactProperty>>(rowIndex: int32, columnIndex: int32, content: ReactElement, className?: string, props?: Option<$a>, debug?: boolean): ReactElement {
    const debug_1: boolean = defaultArg<boolean>(debug, false);
    return createElement<any>("div", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["key", `${rowIndex}-${columnIndex}`] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(debug_1 ? singleton<IReactProperty>(["data-testid", `cell-${rowIndex}-${columnIndex}`] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", join(" ", toList<string>(delay<string>((): Iterable<string> => ((className != null) ? singleton<string>(value_15(className)) : empty<string>()))))] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>((props != null) ? value_15(props) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => {
        let elems: Iterable<ReactElement>;
        return singleton<IReactProperty>((elems = [content], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any]));
    }))))))))))));
}

export function BaseActiveTableCell(baseActiveTableCellInputProps: any): ReactElement {
    const isStickyHeader: Option<boolean> = baseActiveTableCellInputProps.isStickyHeader;
    const setData: ((arg0: string) => void) = baseActiveTableCellInputProps.setData;
    const data: string = baseActiveTableCellInputProps.data;
    const ts: TableCellController = baseActiveTableCellInputProps.ts;
    const isStickyHeader_1: boolean = defaultArg<boolean>(isStickyHeader, false);
    const patternInput: [string, ((arg0: string) => void)] = reactApi.useState<string, string>(data);
    const tempData: string = patternInput[0];
    const setTempData: ((arg0: string) => void) = patternInput[1];
    const dependencies: any[] = [data];
    reactApi.useEffect((): void => {
        setTempData(data);
    }, dependencies);
    return BaseCell<Iterable<IReactProperty>>(ts.Index.y, ts.Index.x, createElement<any>("input", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["autoFocus", true] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => {
        let value_2: string;
        return append<IReactProperty>(singleton<IReactProperty>((value_2 = "swt:rounded-none swt:w-full swt:h-full swt:bg-base-100 swt:text-base-content swt:px-2 swt:py-1 swt:outline-hidden", ["className", value_2] as [string, any])), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(isStickyHeader_1 ? singleton<IReactProperty>(["style", {
            position: "sticky",
            height: 40,
            top: 40,
        }] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["defaultValue", tempData] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onChange", (ev: Event): void => {
            setTempData(ev.target.value);
        }] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onKeyDown", (e_1: KeyboardEvent): void => {
            ts.onKeyDown(e_1);
            if (e_1.code === "Enter") {
                setData(tempData);
            }
        }] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["onBlur", (e_2: FocusEvent): void => {
            ts.onBlur(e_2);
            setData(tempData);
        }] as [string, any])))))))))));
    })))))));
}

export function CompositeCellActiveRender(tableCellController: TableCellController, cell: CompositeCell_$union, setCell: ((arg0: CompositeCell_$union) => void)): ReactElement {
    switch (cell.tag) {
        case /* FreeText */ 1: {
            const txt: string = cell.fields[0];
            return createElement(BaseActiveTableCell, {
                ts: tableCellController,
                data: txt,
                setData: (t_1: string): void => {
                    setCell(CompositeCell_FreeText(t_1));
                },
            });
        }
        case /* Unitized */ 2: {
            const v: string = cell.fields[0];
            const oa_2: OntologyAnnotation = cell.fields[1];
            return createElement(BaseActiveTableCell, {
                ts: tableCellController,
                data: v,
                setData: (t_2: string): void => {
                    setCell(CompositeCell_Unitized(v, oa_2));
                },
            });
        }
        case /* Data */ 3: {
            const d: Data = cell.fields[0];
            return createElement(BaseActiveTableCell, {
                ts: tableCellController,
                data: defaultArg(d.Name, ""),
                setData: (t_3: string): void => {
                    d.Name = Option_whereNot<string>(isNullOrWhiteSpace, t_3);
                    setCell(CompositeCell_Data(d));
                },
            });
        }
        default: {
            const oa: OntologyAnnotation = cell.fields[0];
            const term: Option<Term> = oa.isEmpty() ? undefined : TermModule_fromOntologyAnnotation(oa);
            return createElement(TermSearch, {
                onTermSelect: (t: Option<Term>): void => {
                    setCell(CompositeCell_Term(defaultArg(map<Term, OntologyAnnotation>(TermModule_toOntologyAnnotation, t), new OntologyAnnotation())));
                },
                term: unwrap(term),
                onBlur: (): void => {
                    tableCellController.onBlur();
                },
                onKeyDown: (e: KeyboardEvent): void => {
                    tableCellController.onKeyDown(e);
                },
                portalTermDropdown: {
                    portal: document.body,
                    renderer: (client: ClientRect, dropdown: ReactElement): ReactElement => {
                        let elems: Iterable<ReactElement>;
                        return createElement<any>("div", createObj(ofArray([["className", "swt:absolute swt:z-50"] as [string, any], ["style", {
                            left: ~~((client.left + window.scrollX) - 2),
                            top: ~~((client.bottom + window.scrollY) + 5),
                        }] as [string, any], (elems = [dropdown], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
                    },
                },
                portalModals: document.body,
                autoFocus: true,
                classNames: {
                    inputLabel: "swt:rounded-none swt:px-1 swt:py-1 swt:w-full swt:h-full swt:bg-base-100 swt:text-base-content",
                },
            });
        }
    }
}

