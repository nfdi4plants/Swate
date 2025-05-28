import { createElement, ReactElement } from "react";
import React from "react";
import { unwrap, map as map_1, value as value_31, toArray, some, defaultArg, Option } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { IRefValue$1 } from "../fable_modules/Fable.React.Types.18.4.0/Fable.React.fs.js";
import { ofArray, item, map, length, empty, FSharpList } from "../fable_modules/fable-library-ts.4.24.0/List.js";
import { createObj, IDisposable, defaultOf } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { reactApi } from "../fable_modules/Feliz.2.9.0/./Interop.fs.js";
import { float64, int32 } from "../fable_modules/fable-library-ts.4.24.0/Int32.js";
import { FloatingFocusManager, FloatingOverlay, FloatingPortal, UseInteractionsReturn, useInteractions, useTypeahead, useListNavigation, useRole, useDismiss, UseFloatingReturn, autoUpdate, shift, flip, offset, useFloating } from "@floating-ui/react";
import { useEffectWithDeps } from "../fable_modules/Feliz.2.9.0/./ReactInterop.js";
import { map as map_2, empty as empty_1, collect, singleton, append, delay, toList, iterate } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { addRangeInPlace } from "../fable_modules/fable-library-ts.4.24.0/Array.js";
import { createDisposable } from "../fable_modules/Feliz.2.9.0/./Internal.fs.js";
import { IReactProperty } from "../fable_modules/Feliz.2.9.0/Types.fs.js";
import { defaultOf as defaultOf_1 } from "../fable_modules/Feliz.2.9.0/../../Util/../fable_modules/fable-library-ts.4.24.0/Util.js";
import { rangeDouble } from "../fable_modules/fable-library-ts.4.24.0/Range.js";
import { Feliz_prop__prop_dataColumn_Static_Z524259A4, Feliz_prop__prop_dataRow_Static_Z524259A4 } from "../Util/Extensions.fs.js";
import { printf, toText } from "../fable_modules/fable-library-ts.4.24.0/String.js";

/**
 * 
 */
export function ContextMenu(contextMenuInputProps: any): ReactElement {
    const onSpawn: Option<((arg0: MouseEvent) => Option<any>)> = contextMenuInputProps.onSpawn;
    const ref: Option<IRefValue$1<Option<HTMLElement>>> = contextMenuInputProps.ref;
    const childInfo: ((arg0: any) => FSharpList<ContextMenuItem>) = contextMenuInputProps.childInfo;
    let patternInput: [any, ((arg0: any) => void)];
    const initial: any = defaultOf();
    patternInput = reactApi.useState<any, any>(initial);
    const spawnData: any = patternInput[0];
    const setSpawnData: ((arg0: any) => void) = patternInput[1];
    const patternInput_1: [FSharpList<ContextMenuItem>, ((arg0: FSharpList<ContextMenuItem>) => void)] = reactApi.useState<FSharpList<ContextMenuItem>, FSharpList<ContextMenuItem>>(empty<ContextMenuItem>());
    const setChildren: ((arg0: FSharpList<ContextMenuItem>) => void) = patternInput_1[1];
    const children: FSharpList<ContextMenuItem> = patternInput_1[0];
    const onSpawn_1: ((arg0: MouseEvent) => Option<any>) = defaultArg(onSpawn, some);
    const patternInput_2: [boolean, ((arg0: boolean) => void)] = reactApi.useState<boolean, boolean>(false);
    const setIsOpen: ((arg0: boolean) => void) = patternInput_2[1];
    const isOpen: boolean = patternInput_2[0];
    const patternInput_3: [Option<int32>, ((arg0: Option<int32>) => void)] = reactApi.useState<Option<int32>, Option<int32>>(undefined);
    const setActiveIndex: ((arg0: Option<int32>) => void) = patternInput_3[1];
    const activeIndex: Option<int32> = patternInput_3[0];
    const allowMouseUpCloseRef: IRefValue$1<boolean> = reactApi.useRef(false);
    const timeout: IRefValue$1<Option<int32>> = reactApi.useRef(undefined);
    const targetRef: IRefValue$1<Option<HTMLElement>> = reactApi.useRef(undefined);
    const listItemsRef: IRefValue$1<HTMLElement[]> = reactApi.useRef([]);
    const listContentRef: IRefValue$1<Option<string>[]> = reactApi.useRef([]);
    const floating_1: UseFloatingReturn = useFloating({
        placement: "right-start",
        strategy: "fixed",
        middleware: [offset(some({
            alignmentAxis: 4,
            mainAxis: 5,
        })), flip(some({
            fallbackPlacements: ["left-start"],
        })), shift(some({
            padding: 10,
        }))],
        open: isOpen,
        onOpenChange: setIsOpen,
        whileElementsMounted: (reference: any, floating: any, update: (() => void)): void => {
            autoUpdate(reference, floating, update);
        },
    });
    const dismiss: any = useDismiss(floating_1.context);
    const role: any = useRole(floating_1.context, {
        role: "menu",
    });
    const listNavigation: any = useListNavigation(floating_1.context, {
        listRef: listItemsRef,
        activeIndex: activeIndex,
        onNavigate: setActiveIndex,
    });
    const typeahead: any = useTypeahead(floating_1.context, {
        listRef: listContentRef,
        activeIndex: activeIndex,
        onMatch: setActiveIndex,
        enabled: isOpen,
    });
    const interactions: UseInteractionsReturn = useInteractions([role, dismiss, listNavigation, typeahead]);
    useEffectWithDeps((): IDisposable => {
        const myClearTimeout = (): void => {
            iterate<int32>((timeout_1: int32): void => {
                clearTimeout(timeout_1);
            }, toArray<int32>(timeout.current));
        };
        const onContextMenu = (e_1: Event): void => {
            let x: float64, y: float64, top: float64, left: float64, right: float64;
            const e_2 = e_1 as MouseEvent;
            const matchValue: Option<any> = onSpawn_1(e_2);
            if (matchValue == null) {
            }
            else {
                const data: any = value_31(matchValue);
                e_2.preventDefault();
                setSpawnData(data);
                const children_1: FSharpList<ContextMenuItem> = childInfo(data);
                if (length(children_1) === 0) {
                    throw new Error("Context menu must have at least one item");
                }
                setChildren(children_1);
                addRangeInPlace(map<ContextMenuItem, Option<string>>((child: ContextMenuItem): Option<string> => map_1<{ element: ReactElement, label: string }, string>((kbd: { element: ReactElement, label: string }): string => kbd.label, child.kbdbutton), children_1), listContentRef.current);
                let rect: ClientRect;
                rect = ((x = e_2.clientX, (y = e_2.clientY, (top = e_2.clientY, (left = e_2.clientX, (right = e_2.clientX, {
                    bottom: e_2.clientY,
                    height: 0,
                    left: left,
                    right: right,
                    top: top,
                    width: 0,
                    x: x,
                    y: y,
                }))))));
                const vEl: VirtualElement = {
                    getBoundingClientRect: (): ClientRect => rect,
                };
                floating_1.refs.setPositionReference(vEl);
                setIsOpen(true);
                myClearTimeout();
                allowMouseUpCloseRef.current = false;
                timeout.current = setTimeout((): void => {
                    allowMouseUpCloseRef.current = true;
                }, 300);
            }
        };
        const onMouseUp = (e_3: Event): void => {
            if (allowMouseUpCloseRef.current) {
                setIsOpen(false);
            }
        };
        if (ref == null) {
            document.addEventListener("contextmenu", onContextMenu);
            targetRef.current = (document as HTMLElement);
        }
        else {
            const ref_1: IRefValue$1<Option<HTMLElement>> = value_31(ref);
            iterate<HTMLElement>((el: HTMLElement): void => {
                el.addEventListener("contextmenu", onContextMenu);
                targetRef.current = el;
            }, toArray<HTMLElement>(ref_1.current));
        }
        document.addEventListener("mouseup", onMouseUp);
        return createDisposable((): void => {
            console.log("Effect cleanup");
            document.removeEventListener("mouseup", onMouseUp);
            iterate<HTMLElement>((el_1: HTMLElement): void => {
                el_1.removeEventListener("contextmenu", onContextMenu);
            }, toArray<HTMLElement>(targetRef.current));
            myClearTimeout();
        });
    }, [floating_1.refs]);
    const close = (): void => {
        setIsOpen(false);
        allowMouseUpCloseRef.current = true;
        iterate<int32>((timeout_2: int32): void => {
            clearTimeout(timeout_2);
        }, toArray<int32>(timeout.current));
    };
    return createElement(FloatingPortal, {
        children: isOpen ? createElement(FloatingOverlay, {
            children: createElement(FloatingFocusManager, {
                context: floating_1.context,
                children: createElement<any>("div", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["ref", floating_1.refs.setFloating] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["style", floating_1.floatingStyles] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => {
                    let o: any;
                    return append<IReactProperty>(collect<[string, any], Iterable<IReactProperty>, IReactProperty>((matchValue_1: [string, any]): Iterable<IReactProperty> => {
                        const v: any = matchValue_1[1];
                        const key_3: string = matchValue_1[0];
                        return singleton<IReactProperty>([key_3, v] as [string, any]);
                    }, (o = interactions.getFloatingProps(), Object.entries(o))), delay<IReactProperty>((): Iterable<IReactProperty> => {
                        let value_7: string;
                        return append<IReactProperty>(singleton<IReactProperty>((value_7 = "swt:grid swt:grid-cols-[auto_1fr_auto] swt:bg-base-100 swt:border-2 swt:border-base-300 swt:w-56 swt:rounded-md swt:focus:outline-hidden", ["className", value_7] as [string, any])), delay<IReactProperty>((): Iterable<IReactProperty> => {
                            let elems_1: Iterable<ReactElement>;
                            return singleton<IReactProperty>((elems_1 = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => collect<int32, Iterable<ReactElement>, ReactElement>((index: int32): Iterable<ReactElement> => {
                                let tabIndex: int32;
                                const child_1: ContextMenuItem = item(index, children);
                                const triggerEvent = (e_4: MouseEvent): void => {
                                    const d: { buttonEvent: MouseEvent, spawnData: any } = {
                                        buttonEvent: e_4,
                                        spawnData: spawnData,
                                    };
                                    iterate<((arg0: { buttonEvent: MouseEvent, spawnData: any }) => void)>((f: ((arg0: { buttonEvent: MouseEvent, spawnData: any }) => void)): void => {
                                        f(d);
                                    }, toArray<((arg0: { buttonEvent: MouseEvent, spawnData: any }) => void)>(child_1.onClick));
                                    close();
                                };
                                let props: [string, any][];
                                const o_1: any = interactions.getItemProps((tabIndex = ((((activeIndex != null) && (value_31(activeIndex) === index)) ? 0 : -1) | 0), {
                                    label: unwrap(map_1<{ element: ReactElement, label: string }, string>((_arg_2: { element: ReactElement, label: string }): string => _arg_2.label, child_1.kbdbutton)),
                                    onClick: triggerEvent,
                                    onMouseUp: triggerEvent,
                                    ref: (node: HTMLElement): void => {
                                        listItemsRef.current[index] = node;
                                    },
                                    tabIndex: tabIndex,
                                }));
                                props = Object.entries(o_1);
                                return child_1.isDivider ? singleton<ReactElement>(createElement<any>("div", {
                                    className: "swt:divider swt:my-0 swt:col-span-3",
                                })) : singleton<ReactElement>(createElement<any>("button", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["key", index] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => {
                                    let value_13: string;
                                    return append<IReactProperty>(singleton<IReactProperty>((value_13 = "swt:col-span-3 swt:grid swt:grid-cols-subgrid swt:gap-x-2 swt:text-sm /\n                                                        swt:text-base-content swt:px-2 swt:py-1 /\n                                                        swt:w-full swt:text-left /\n                                                        swt:hover:bg-base-100 /\n                                                        swt:focus:bg-base-100 swt:focus:outline-hidden swt:focus:ring-2 swt:focus:ring-primary", ["className", value_13] as [string, any])), delay<IReactProperty>((): Iterable<IReactProperty> => {
                                        let elems: Iterable<ReactElement>;
                                        return append<IReactProperty>(singleton<IReactProperty>((elems = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((child_1.icon != null) ? singleton<ReactElement>(createElement<any>("div", {
                                            className: "swt:col-start-1 swt:justify-self-start",
                                            children: value_31(child_1.icon),
                                        })) : singleton<ReactElement>(defaultOf_1()), delay<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((child_1.text != null) ? singleton<ReactElement>(createElement<any>("div", {
                                            className: "swt:col-start-2 swt:justify-self-start",
                                            children: value_31(child_1.text),
                                        })) : empty_1<ReactElement>(), delay<ReactElement>((): Iterable<ReactElement> => ((child_1.kbdbutton != null) ? singleton<ReactElement>(createElement<any>("div", {
                                            className: "swt:col-start-3 swt:justify-self-end",
                                            children: value_31(child_1.kbdbutton).element,
                                        })) : singleton<ReactElement>(defaultOf_1())))))))), ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])), delay<IReactProperty>((): Iterable<IReactProperty> => collect<[string, any], Iterable<IReactProperty>, IReactProperty>((matchValue_2: [string, any]): Iterable<IReactProperty> => {
                                            const v_1: any = matchValue_2[1];
                                            const key_17: string = matchValue_2[0];
                                            return singleton<IReactProperty>([key_17, v_1] as [string, any]);
                                        }, props)));
                                    }));
                                })))))));
                            }, rangeDouble(0, 1, length(children) - 1)))), ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any]));
                        }));
                    }));
                })))))))),
                initialFocus: some(floating_1.refs.floating),
                visuallyHiddenDismiss: true,
            }),
            lockScroll: true,
            className: "swt:z-[9999]",
        }) : defaultOf_1(),
    });
}

export function Example(): ReactElement {
    let value: string, elems: Iterable<ReactElement>;
    const containerRef: IRefValue$1<Option<HTMLElement>> = reactApi.useRef(undefined);
    return createElement<any>("div", createObj(ofArray([(value = "swt:w-full swt:h-72 swt:border swt:border-dashed swt:border-primary swt:rounded-sm swt:flex swt:items-center swt:justify-center swt:flex-col swt:gap-4", ["className", value] as [string, any]), ["ref", containerRef] as [string, any], (elems = [createElement<any>("span", {
        className: "swt:select-none",
        children: "Click here for context menu!",
    }), createElement<any>("button", createObj(ofArray([["className", "swt:btn swt:btn-primary"] as [string, any], ["children", "Example Table Cell"] as [string, any], Feliz_prop__prop_dataRow_Static_Z524259A4(12), Feliz_prop__prop_dataColumn_Static_Z524259A4(5)]))), createElement(ContextMenu, {
        childInfo: (data: any): FSharpList<ContextMenuItem> => toList<ContextMenuItem>(delay<ContextMenuItem>((): Iterable<ContextMenuItem> => map_2<int32, ContextMenuItem>((i: int32): ContextMenuItem => ({
            text: createElement<any>("span", {
                children: [`Item ${i}`],
            }),
            icon: unwrap((i === 4) ? createElement<any>("i", {
                className: "fa-solid fa-check",
            }) : undefined),
            kbdbutton: unwrap((i === 3) ? {
                element: createElement<any>("kbd", {
                    className: "swt:ml-auto swt:kbd swt:kbd-sm",
                    children: "Back",
                }),
                label: "Back",
            } : undefined),
            onClick: (e: { buttonEvent: MouseEvent, spawnData: any }): void => {
                e.buttonEvent.stopPropagation();
                const index: { x: int32, y: int32 } = e.spawnData;
                console.log([toText(printf("Item clicked: %i"))(i), index] as [string, { x: int32, y: int32 }]);
            },
        }), rangeDouble(0, 1, 5)))),
        ref: containerRef,
        onSpawn: (e_1: MouseEvent): Option<any> => {
            const target = e_1.target as HTMLElement;
            const tableCell: Option<Element> = target.closest("[data-row][data-column]");
            if (tableCell != null) {
                const cell: Element = value_31(tableCell);
                const cell_1 = cell as HTMLElement;
                const row: int32 = cell_1.dataset.row | 0;
                const col: int32 = cell_1.dataset.column | 0;
                const indices: { x: int32, y: int32 } = {
                    x: col,
                    y: row,
                };
                console.log(indices);
                return some(indices);
            }
            else {
                console.log("No table cell found");
                return undefined;
            }
        },
    })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
}

