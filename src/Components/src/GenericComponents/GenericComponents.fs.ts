import { IReactProperty } from "../fable_modules/Feliz.2.9.0/Types.fs.js";
import { class_type, TypeInfo } from "../fable_modules/fable-library-ts.4.24.0/Reflection.js";
import { ReactElement, createElement } from "react";
import { createObj } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { empty, singleton, append, delay, toList } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { join } from "../fable_modules/fable-library-ts.4.24.0/String.js";
import { Option, value as value_17 } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { reactApi } from "../fable_modules/Feliz.2.9.0/./Interop.fs.js";
import { FSharpList, ofArray } from "../fable_modules/fable-library-ts.4.24.0/List.js";

export function Feliz_DaisyUI_modal__modal_get_active_Static(): IReactProperty {
    return ["className", "swt:modal swt:modal-open"] as [string, any];
}

export class Components {
    constructor() {
    }
}

export function Components_$reflection(): TypeInfo {
    return class_type("Swate.Components.Components", undefined, Components);
}

export function Components_DeleteButton_1619F1BE<$a extends Iterable<ReactElement>>(children?: Option<$a>, className?: string, props?: FSharpList<IReactProperty>): ReactElement {
    return createElement<any>("button", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", join(" ", toList<string>(delay<string>((): Iterable<string> => append<string>(singleton<string>("swt:btn swt:btn-square"), delay<string>((): Iterable<string> => ((className != null) ? singleton<string>(value_17(className)) : empty<string>()))))))] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>((props != null) ? value_17(props) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => {
        let elems_1: Iterable<ReactElement>;
        return singleton<IReactProperty>((elems_1 = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((children != null) ? value_17(children) : empty<ReactElement>(), delay<ReactElement>((): Iterable<ReactElement> => {
            let elems: Iterable<ReactElement>;
            return singleton<ReactElement>(createElement<any>("svg", createObj(ofArray([["xmlns", "http://www.w3.org/2000/svg"] as [string, any], ["className", "swt:h-6 swt:w-6"] as [string, any], ["fill", "none"] as [string, any], ["viewBox", (((((0 + " ") + 0) + " ") + 24) + " ") + 24] as [string, any], ["stroke", "currentColor"] as [string, any], (elems = [createElement<any>("path", {
                strokeLinecap: "round",
                strokeLinejoin: "round",
                strokeWidth: 2,
                d: "M6 18L18 6M6 6l12 12",
            })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])]))));
        })))), ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any]));
    }))))))));
}

export function Components_CollapseButton_Z762A16CE(isCollapsed: boolean, setIsCollapsed: ((arg0: boolean) => void), collapsedIcon?: Option<void>, collapseIcon?: Option<void>, classes?: string): ReactElement {
    let elems: Iterable<ReactElement>;
    return createElement<any>("label", createObj(ofArray([["className", join(" ", toList<string>(delay<string>((): Iterable<string> => append<string>(singleton<string>("swt:btn swt:btn-square swt:swap swt:swap-rotate swt:grow-0"), delay<string>((): Iterable<string> => ((classes != null) ? singleton<string>(value_17(classes)) : empty<string>()))))))] as [string, any], ["onClick", (e: MouseEvent): void => {
        e.preventDefault();
        e.stopPropagation();
        setIsCollapsed(!isCollapsed);
    }] as [string, any], (elems = [createElement<any>("input", {
        type: "checkbox",
        checked: isCollapsed,
        onChange: (ev: Event): void => {
            const _arg: boolean = ev.target.checked;
        },
    }), createElement<any>("i", {
        className: join(" ", toList<string>(delay<string>((): Iterable<string> => append<string>(singleton<string>("swt:swap-off fa-solid"), delay<string>((): Iterable<string> => {
            if (collapsedIcon != null) {
                value_17(collapsedIcon);
                return empty<string>();
            }
            else {
                return singleton<string>("fa-solid fa-chevron-down");
            }
        }))))),
    }), createElement<any>("i", {
        className: join(" ", toList<string>(delay<string>((): Iterable<string> => append<string>(singleton<string>("swt:swap-on"), delay<string>((): Iterable<string> => {
            if (collapseIcon != null) {
                value_17(collapseIcon);
                return empty<string>();
            }
            else {
                return singleton<string>("fa-solid fa-x");
            }
        }))))),
    })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
}

