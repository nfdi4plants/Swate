import { createElement, ReactElement } from "react";
import React from "react";
import { value as value_26, Option } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { createObj } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { empty, singleton, append, delay, toList } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { IReactProperty } from "../fable_modules/Feliz.2.9.0/Types.fs.js";
import { join } from "../fable_modules/fable-library-ts.4.24.0/String.js";
import { Components_DeleteButton_1619F1BE } from "./GenericComponents.fs.js";
import { ofArray, singleton as singleton_1 } from "../fable_modules/fable-library-ts.4.24.0/List.js";
import { reactApi } from "../fable_modules/Feliz.2.9.0/./Interop.fs.js";

export function BaseModal(baseModalInputProps: any): ReactElement {
    const debug: Option<string> = baseModalInputProps.debug;
    const footer: Option<ReactElement> = baseModalInputProps.footer;
    const contentClassInfo: Option<string> = baseModalInputProps.contentClassInfo;
    const content: Option<ReactElement> = baseModalInputProps.content;
    const modalActions: Option<ReactElement> = baseModalInputProps.modalActions;
    const header: Option<ReactElement> = baseModalInputProps.header;
    const modalClassInfo: Option<string> = baseModalInputProps.modalClassInfo;
    const rmv: ((arg0: MouseEvent) => void) = baseModalInputProps.rmv;
    return createElement<any>("div", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>((debug != null) ? singleton<IReactProperty>(["data-testid", "modal_" + value_26(debug)] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", "swt:modal swt:modal-open"] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => {
        let elems_2: Iterable<ReactElement>, elems_1: Iterable<ReactElement>;
        return singleton<IReactProperty>((elems_2 = [createElement<any>("div", {
            className: "swt:modal-backdrop",
            onClick: rmv,
        }), createElement<any>("div", createObj(ofArray([["className", join(" ", toList<string>(delay<string>((): Iterable<string> => append<string>(singleton<string>("swt:modal-box swt:w-4/5 swt:flex swt:flex-col swt:gap-2"), delay<string>((): Iterable<string> => ((modalClassInfo != null) ? singleton<string>(value_26(modalClassInfo)) : empty<string>()))))))] as [string, any], (elems_1 = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => {
            let elems: Iterable<ReactElement>;
            return append<ReactElement>(singleton<ReactElement>(createElement<any>("div", createObj(ofArray([["className", "swt:card-title"] as [string, any], (elems = toList<ReactElement>(delay<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((header != null) ? singleton<ReactElement>(value_26(header)) : empty<ReactElement>(), delay<ReactElement>((): Iterable<ReactElement> => singleton<ReactElement>(Components_DeleteButton_1619F1BE<Iterable<ReactElement>>(undefined, "swt:ml-auto", singleton_1(["onClick", rmv] as [string, any]))))))), ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])))), delay<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((modalActions != null) ? singleton<ReactElement>(createElement<any>("div", {
                children: value_26(modalActions),
            })) : empty<ReactElement>(), delay<ReactElement>((): Iterable<ReactElement> => append<ReactElement>((content != null) ? singleton<ReactElement>(createElement<any>("div", createObj(toList<IReactProperty>(delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>((debug != null) ? singleton<IReactProperty>(["data-testid", "modal_content_" + value_26(debug)] as [string, any]) : empty<IReactProperty>(), delay<IReactProperty>((): Iterable<IReactProperty> => append<IReactProperty>(singleton<IReactProperty>(["className", (contentClassInfo == null) ? "swt:overflow-y-auto swt:space-y-2 swt:py-2" : value_26(contentClassInfo)] as [string, any]), delay<IReactProperty>((): Iterable<IReactProperty> => singleton<IReactProperty>(["children", value_26(content)] as [string, any])))))))))) : empty<ReactElement>(), delay<ReactElement>((): Iterable<ReactElement> => ((footer != null) ? singleton<ReactElement>(createElement<any>("div", {
                className: "swt:card-actions",
                children: value_26(footer),
            })) : empty<ReactElement>())))))));
        })), ["children", reactApi.Children.toArray(Array.from(elems_1))] as [string, any])])))], ["children", reactApi.Children.toArray(Array.from(elems_2))] as [string, any]));
    }))))))));
}

export default BaseModal;

