import { createElement } from "react";
import React from "react";
import * as react from "react";
import { createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Route__get_toStringRdbl, Route } from "../../Routing.js";
import { Protocol_Msg, Msg } from "../../Messages.js";
import { ofArray, singleton } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { useReact_useEffectOnce_3A5B6456 } from "../../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { empty, singleton as singleton_1, append, delay, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { loadingModal } from "../../Modals/Loading.js";
import { ProtocolContainer } from "./ProtocolSearchViewComponent.js";

export function breadcrumbEle(model, dispatch) {
    let elems, children_2, children;
    return createElement("nav", createObj(Helpers_combineClasses("breadcrumb", ofArray([["className", "has-arrow-separator"], (elems = [(children_2 = ofArray([(children = singleton(createElement("a", {
        onClick: (_arg) => {
            dispatch(new Msg(19, [new Route(5, [])]));
        },
        children: Route__get_toStringRdbl(new Route(5, [])),
    })), createElement("li", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    })), createElement("li", {
        className: "is-active",
        children: createElement("a", {
            onClick: (_arg_1) => {
                dispatch(new Msg(19, [new Route(5, [])]));
            },
            children: Route__get_toStringRdbl(new Route(6, [])),
        }),
    })]), createElement("ul", {
        children: Interop_reactApi.Children.toArray(Array.from(children_2)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))));
}

export function ProtocolSearchView(protocolSearchViewInputProps) {
    const dispatch = protocolSearchViewInputProps.dispatch;
    const model = protocolSearchViewInputProps.model;
    useReact_useEffectOnce_3A5B6456(() => {
        dispatch(new Msg(10, [new Protocol_Msg(3, [])]));
    });
    const isEmpty = (model.ProtocolState.ProtocolsAll == null) ? true : (model.ProtocolState.ProtocolsAll.length === 0);
    const isLoading = model.ProtocolState.Loading;
    const children = toList(delay(() => append(singleton_1(breadcrumbEle(model, dispatch)), delay(() => {
        let value_3;
        return append((isEmpty && !isLoading) ? singleton_1(createElement("p", createObj(Helpers_combineClasses("help", ofArray([["className", "is-danger"], (value_3 = "No templates were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer.", ["children", value_3])]))))) : empty(), delay(() => append(isLoading ? singleton_1(loadingModal) : empty(), delay(() => append(singleton_1(createElement("label", {
            className: "label",
            children: "Search the database for protocol templates.",
        })), delay(() => (!isEmpty ? singleton_1(createElement(ProtocolContainer, {
            model: model,
            dispatch: dispatch,
        })) : empty())))))));
    }))));
    return react.createElement("div", {
        onSubmit: (e) => {
            e.preventDefault();
        },
        onKeyDown: (k) => {
            if (k.key === "Enter") {
                k.preventDefault();
            }
        },
    }, ...children);
}

//# sourceMappingURL=ProtocolSearchView.js.map
