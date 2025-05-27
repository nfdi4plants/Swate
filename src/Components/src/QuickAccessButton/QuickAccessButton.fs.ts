import { Option, value as value_9, defaultArg } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { ReactElement, createElement } from "react";
import { createObj } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { empty, singleton, append, delay, toList } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { join } from "../fable_modules/fable-library-ts.4.24.0/String.js";
import { DaisyUIColors_$union } from "../Util/Types.fs.js";
import { IReactProperty } from "../fable_modules/Feliz.2.9.0/Types.fs.js";

export function QuickAccessButton({ desc, children, onclick, isDisabled, props, color, classes }: {desc: string, children: ReactElement, onclick: ((arg0: Event) => void), isDisabled?: boolean, props?: Iterable<IReactProperty>, color?: DaisyUIColors_$union, classes?: string }): ReactElement {
    const isDisabled_1: boolean = defaultArg<boolean>(isDisabled, false);
    return createElement<any>("button", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", join(" ", toList<string>(delay<string>((): Iterable<string> => append<string>(singleton<string>("swt:btn swt:btn-ghost swt:btn-square swt:btn-transparent swt:bg-transparent swt:border-none swt:shadow-none"), delay<string>((): Iterable<string> => {
        let matchValue: Option<DaisyUIColors_$union>;
        return append<string>((matchValue = color, (matchValue == null) ? singleton<string>("swt:hover:!text-primary") : ((value_9(matchValue).tag === /* Secondary */ 1) ? singleton<string>("swt:hover:!text-secondary") : ((value_9(matchValue).tag === /* Accent */ 2) ? singleton<string>("swt:hover:!text-accent") : ((value_9(matchValue).tag === /* Error */ 6) ? singleton<string>("swt:hover:!text-error") : ((value_9(matchValue).tag === /* Info */ 3) ? singleton<string>("swt:hover:!text-info") : ((value_9(matchValue).tag === /* Success */ 4) ? singleton<string>("swt:hover:!text-success") : ((value_9(matchValue).tag === /* Warning */ 5) ? singleton<string>("swt:hover:!text-warning") : singleton<string>("swt:hover:!text-primary")))))))), delay<string>((): Iterable<string> => append<string>((classes != null) ? singleton<string>(value_9(classes)) : empty<string>(), delay<string>((): Iterable<string> => (!isDisabled_1 ? singleton<string>("swt:text-white") : empty<string>())))));
    })))))] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["tabIndex", isDisabled_1 ? -1 : 0] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["title", desc] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["disabled", isDisabled_1] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["onClick", (arg: MouseEvent): void => {
        onclick(arg);
    }] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>((props != null) ? value_9(props) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", children] as [string, any])))))))))))))))));
}

export default QuickAccessButton;

